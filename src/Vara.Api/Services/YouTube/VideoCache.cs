using Microsoft.Extensions.Caching.Memory;

namespace Vara.Api.Services.YouTube;

/// <summary>
/// Caching decorator for IYouTubeClient. Wraps the real client and serves
/// search results and video metadata from memory before hitting the API.
/// Transcripts are not cached — they are fetched on demand only.
/// </summary>
public class VideoCache(
    IYouTubeClient inner,
    IMemoryCache cache,
    ILogger<VideoCache> logger)
    : IYouTubeClient
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    public async Task<List<VideoMetadata>> SearchAsync(
        string keyword,
        int maxResults = 10,
        CancellationToken ct = default)
    {
        var key = $"search:{keyword.ToLowerInvariant()}:{maxResults}";

        if (cache.TryGetValue(key, out List<VideoMetadata>? cached))
        {
            logger.LogDebug("Cache hit: search '{Keyword}'", keyword);
            return cached!;
        }

        var results = await inner.SearchAsync(keyword, maxResults, ct);
        cache.Set(key, results, CacheDuration);

        logger.LogDebug("Cache miss: search '{Keyword}' — {Count} results stored", keyword, results.Count);
        return results;
    }

    public async Task<VideoMetadata?> GetVideoAsync(
        string videoId,
        CancellationToken ct = default)
    {
        var key = $"video:{videoId}";

        if (cache.TryGetValue(key, out VideoMetadata? cached))
        {
            logger.LogDebug("Cache hit: video {VideoId}", videoId);
            return cached;
        }

        var result = await inner.GetVideoAsync(videoId, ct);

        if (result is not null)
            cache.Set(key, result, CacheDuration);

        logger.LogDebug("Cache miss: video {VideoId}", videoId);
        return result;
    }

    // Transcripts are not cached — too large and fetched on demand only
    public Task<string?> GetTranscriptAsync(string videoId, CancellationToken ct = default)
        => inner.GetTranscriptAsync(videoId, ct);
}
