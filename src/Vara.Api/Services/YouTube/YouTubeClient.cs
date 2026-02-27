using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;

namespace Vara.Api.Services.YouTube;

public class YouTubeClient(
    IHttpClientFactory httpClientFactory,
    ITranscriptFetcher transcriptFetcher,
    IConfiguration config,
    ILogger<YouTubeClient> logger)
    : IYouTubeClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private string ApiKey => config["YouTube:ApiKey"]
        ?? throw new InvalidOperationException("YouTube:ApiKey is not configured.");

    // -------------------------------------------------------------------------
    // Search
    // -------------------------------------------------------------------------

    public async Task<List<VideoMetadata>> SearchAsync(
        string keyword,
        int maxResults = 10,
        CancellationToken ct = default)
    {
        var client = httpClientFactory.CreateClient("YouTube");

        var url = $"https://www.googleapis.com/youtube/v3/search" +
                  $"?q={Uri.EscapeDataString(keyword)}" +
                  $"&key={ApiKey}" +
                  $"&maxResults={maxResults}" +
                  $"&part=snippet" +
                  $"&type=video";

        logger.LogInformation("YouTube search: keyword={Keyword} maxResults={MaxResults}", keyword, maxResults);

        var response = await client.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<YouTubeSearchResponse>(json, JsonOptions);

        if (result?.Items is null)
            return [];

        // Search only returns snippet — fetch full stats in a second call
        var videoIds = result.Items.Select(i => i.Id.VideoId).ToList();
        return await GetVideoBatchAsync(videoIds, ct);
    }

    // -------------------------------------------------------------------------
    // Single video
    // -------------------------------------------------------------------------

    public async Task<VideoMetadata?> GetVideoAsync(
        string videoId,
        CancellationToken ct = default)
    {
        var results = await GetVideoBatchAsync([videoId], ct);
        return results.FirstOrDefault();
    }

    // -------------------------------------------------------------------------
    // Transcript — delegated to ITranscriptFetcher
    // -------------------------------------------------------------------------

    public Task<string?> GetTranscriptAsync(string videoId, CancellationToken ct = default)
        => transcriptFetcher.FetchAsync(videoId, ct);

    // -------------------------------------------------------------------------
    // Internal helpers
    // -------------------------------------------------------------------------

    private async Task<List<VideoMetadata>> GetVideoBatchAsync(
        List<string> videoIds,
        CancellationToken ct)
    {
        if (videoIds.Count == 0)
            return [];

        var client = httpClientFactory.CreateClient("YouTube");
        var ids = string.Join(",", videoIds);

        var url = $"https://www.googleapis.com/youtube/v3/videos" +
                  $"?id={ids}" +
                  $"&key={ApiKey}" +
                  $"&part=snippet,statistics,contentDetails";

        var response = await client.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<YouTubeVideoListResponse>(json, JsonOptions);

        return result?.Items?.Select(MapToMetadata).ToList() ?? [];
    }

    private static VideoMetadata MapToMetadata(YouTubeVideoItem item)
    {
        var snippet = item.Snippet;
        var stats = item.Statistics;

        int? durationSeconds = null;
        if (item.ContentDetails?.Duration is string iso)
        {
            try { durationSeconds = (int)XmlConvert.ToTimeSpan(iso).TotalSeconds; }
            catch { /* leave null if unparseable */ }
        }

        return new VideoMetadata(
            YoutubeId: item.Id,
            Title: snippet?.Title ?? string.Empty,
            Description: snippet?.Description,
            ChannelName: snippet?.ChannelTitle,
            ChannelId: snippet?.ChannelId,
            DurationSeconds: durationSeconds,
            UploadDate: snippet?.PublishedAt,
            ViewCount: long.TryParse(stats?.ViewCount, out var v) ? v : 0,
            LikeCount: int.TryParse(stats?.LikeCount, out var l) ? l : 0,
            CommentCount: int.TryParse(stats?.CommentCount, out var c) ? c : 0,
            ThumbnailUrl: snippet?.Thumbnails?.High?.Url
        );
    }

    // -------------------------------------------------------------------------
    // Response shapes (YouTube Data API v3)
    // -------------------------------------------------------------------------

    private sealed record YouTubeSearchResponse(
        [property: JsonPropertyName("items")] List<YouTubeSearchItem>? Items);

    private sealed record YouTubeSearchItem(
        [property: JsonPropertyName("id")] SearchItemId Id);

    private sealed record SearchItemId(
        [property: JsonPropertyName("videoId")] string VideoId);

    private sealed record YouTubeVideoListResponse(
        [property: JsonPropertyName("items")] List<YouTubeVideoItem>? Items);

    private sealed record YouTubeVideoItem(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("snippet")] VideoSnippet? Snippet,
        [property: JsonPropertyName("statistics")] VideoStatistics? Statistics,
        [property: JsonPropertyName("contentDetails")] ContentDetails? ContentDetails);

    private sealed record VideoSnippet(
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("channelTitle")] string? ChannelTitle,
        [property: JsonPropertyName("channelId")] string? ChannelId,
        [property: JsonPropertyName("publishedAt")] DateTime? PublishedAt,
        [property: JsonPropertyName("thumbnails")] ThumbnailSet? Thumbnails);

    private sealed record ThumbnailSet(
        [property: JsonPropertyName("high")] Thumbnail? High);

    private sealed record Thumbnail(
        [property: JsonPropertyName("url")] string? Url);

    private sealed record VideoStatistics(
        [property: JsonPropertyName("viewCount")] string? ViewCount,
        [property: JsonPropertyName("likeCount")] string? LikeCount,
        [property: JsonPropertyName("commentCount")] string? CommentCount);

    private sealed record ContentDetails(
        [property: JsonPropertyName("duration")] string? Duration);
}
