using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vara.Api.Services.YouTube;

public interface ITranscriptFetcher
{
    /// <summary>
    /// Attempt to fetch the English auto-generated transcript for a video.
    /// Returns null if captions are unavailable or the request fails.
    /// </summary>
    Task<string?> FetchAsync(string videoId, CancellationToken ct = default);
}

public class TranscriptFetcher(IHttpClientFactory httpClientFactory, ILogger<TranscriptFetcher> logger)
    : ITranscriptFetcher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<string?> FetchAsync(string videoId, CancellationToken ct = default)
    {
        // YouTube has no official transcript API. The timedtext endpoint is
        // unofficial but widely used. Returns null when captions are unavailable.
        var client = httpClientFactory.CreateClient("YouTube");
        var url = $"https://www.youtube.com/api/timedtext?v={videoId}&lang=en&fmt=json3";

        try
        {
            var response = await client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogDebug("Transcript not available for {VideoId}: {StatusCode}", videoId, response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var timed = JsonSerializer.Deserialize<TimedTextResponse>(json, JsonOptions);

            if (timed?.Events is null || timed.Events.Count == 0)
                return null;

            var lines = timed.Events
                .Where(e => e.Segs is not null)
                .SelectMany(e => e.Segs!)
                .Select(s => s.Utf8 ?? string.Empty);

            var transcript = string.Join(" ", lines).Trim();

            logger.LogDebug("Fetched transcript for {VideoId}: {Length} chars", videoId, transcript.Length);
            return transcript.Length > 0 ? transcript : null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch transcript for video {VideoId}", videoId);
            return null;
        }
    }

    private sealed record TimedTextResponse(
        [property: JsonPropertyName("events")] List<TimedEvent>? Events);

    private sealed record TimedEvent(
        [property: JsonPropertyName("segs")] List<TextSegment>? Segs);

    private sealed record TextSegment(
        [property: JsonPropertyName("utf8")] string? Utf8);
}
