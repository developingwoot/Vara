using System.Runtime.CompilerServices;
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
    // Channel resolver
    // -------------------------------------------------------------------------

    public async Task<ChannelMetadata?> GetChannelAsync(
        string handleOrId,
        CancellationToken ct = default)
    {
        var client = httpClientFactory.CreateClient("YouTube");
        var (isChannelId, value) = ParseHandleOrId(handleOrId);
        var param = isChannelId ? $"id={Uri.EscapeDataString(value)}" : $"forHandle={Uri.EscapeDataString(value)}";

        var url = $"https://www.googleapis.com/youtube/v3/channels" +
                  $"?part=snippet,statistics" +
                  $"&{param}" +
                  $"&key={ApiKey}";

        logger.LogInformation("YouTube channel lookup: {HandleOrId}", handleOrId);

        var response = await client.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<YouTubeChannelListResponse>(json, JsonOptions);

        var item = result?.Items?.FirstOrDefault();
        if (item is null)
            return null;

        return new ChannelMetadata(
            YoutubeChannelId: item.Id,
            Handle: item.Snippet?.CustomUrl,
            DisplayName: item.Snippet?.Title,
            ThumbnailUrl: item.Snippet?.Thumbnails?.High?.Url,
            SubscriberCount: long.TryParse(item.Statistics?.SubscriberCount, out var sub) ? sub : null,
            VideoCount: int.TryParse(item.Statistics?.VideoCount, out var vid) ? vid : null,
            TotalViewCount: long.TryParse(item.Statistics?.ViewCount, out var views) ? views : null
        );
    }

    // -------------------------------------------------------------------------
    // Channel video IDs (uploads playlist pagination)
    // -------------------------------------------------------------------------

    public async IAsyncEnumerable<string> GetChannelVideoIdsAsync(
        string channelId,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var uploadsPlaylistId = await FetchUploadsPlaylistIdAsync(channelId, ct);
        if (uploadsPlaylistId is null)
            yield break;

        string? pageToken = null;
        do
        {
            var client = httpClientFactory.CreateClient("YouTube");
            var url = $"https://www.googleapis.com/youtube/v3/playlistItems" +
                      $"?part=snippet" +
                      $"&playlistId={Uri.EscapeDataString(uploadsPlaylistId)}" +
                      $"&maxResults=50" +
                      $"&key={ApiKey}" +
                      (pageToken is not null ? $"&pageToken={Uri.EscapeDataString(pageToken)}" : string.Empty);

            var response = await client.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var page = JsonSerializer.Deserialize<PlaylistItemsResponse>(json, JsonOptions);

            if (page?.Items is null)
                yield break;

            foreach (var item in page.Items)
            {
                var videoId = item.Snippet?.ResourceId?.VideoId;
                if (videoId is not null)
                    yield return videoId;
            }

            pageToken = page.NextPageToken;
        }
        while (pageToken is not null);
    }

    private async Task<string?> FetchUploadsPlaylistIdAsync(string channelId, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("YouTube");
        var url = $"https://www.googleapis.com/youtube/v3/channels" +
                  $"?part=contentDetails" +
                  $"&id={Uri.EscapeDataString(channelId)}" +
                  $"&key={ApiKey}";

        var response = await client.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<YouTubeChannelListResponse>(json, JsonOptions);

        return result?.Items?.FirstOrDefault()?.ContentDetails?.RelatedPlaylists?.Uploads;
    }

    private static (bool isChannelId, string value) ParseHandleOrId(string input)
    {
        var value = input.Trim();

        foreach (var prefix in new[] { "https://www.youtube.com/", "https://youtube.com/" })
        {
            if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                value = value[prefix.Length..];
                break;
            }
        }

        // "UC" prefix + 24 chars is the YouTube channel ID format
        return value.StartsWith("UC", StringComparison.Ordinal) && value.Length == 24
            ? (true, value)
            : (false, value);
    }

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

    // ---- Channels API ----

    private sealed record YouTubeChannelListResponse(
        [property: JsonPropertyName("items")] List<YouTubeChannelItem>? Items);

    private sealed record YouTubeChannelItem(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("snippet")] ChannelSnippet? Snippet,
        [property: JsonPropertyName("statistics")] ChannelStatistics? Statistics,
        [property: JsonPropertyName("contentDetails")] ChannelContentDetails? ContentDetails);

    private sealed record ChannelSnippet(
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("customUrl")] string? CustomUrl,
        [property: JsonPropertyName("thumbnails")] ThumbnailSet? Thumbnails);

    private sealed record ChannelStatistics(
        [property: JsonPropertyName("viewCount")] string? ViewCount,
        [property: JsonPropertyName("subscriberCount")] string? SubscriberCount,
        [property: JsonPropertyName("videoCount")] string? VideoCount);

    private sealed record ChannelContentDetails(
        [property: JsonPropertyName("relatedPlaylists")] RelatedPlaylists? RelatedPlaylists);

    private sealed record RelatedPlaylists(
        [property: JsonPropertyName("uploads")] string? Uploads);

    // ---- PlaylistItems API ----

    private sealed record PlaylistItemsResponse(
        [property: JsonPropertyName("nextPageToken")] string? NextPageToken,
        [property: JsonPropertyName("items")] List<PlaylistItem>? Items);

    private sealed record PlaylistItem(
        [property: JsonPropertyName("snippet")] PlaylistItemSnippet? Snippet);

    private sealed record PlaylistItemSnippet(
        [property: JsonPropertyName("resourceId")] ResourceId? ResourceId);

    private sealed record ResourceId(
        [property: JsonPropertyName("videoId")] string? VideoId);
}
