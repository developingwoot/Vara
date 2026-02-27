using Microsoft.Extensions.Caching.Memory;
using Vara.Api.Services.YouTube;

namespace Vara.Api.Services.Analysis;

public class KeywordAnalyzer(
    IYouTubeClient youtube,
    IMemoryCache cache,
    ILogger<KeywordAnalyzer> logger) : IKeywordAnalyzer
{
    private static string CacheKey(string keyword, string? niche) =>
        $"kw:{keyword.ToLowerInvariant()}:{niche?.ToLowerInvariant() ?? ""}";

    public async Task<KeywordAnalysisResult> AnalyzeAsync(
        string keyword,
        string? niche = null,
        CancellationToken ct = default)
    {
        var key = CacheKey(keyword, niche);

        if (cache.TryGetValue(key, out KeywordAnalysisResult? cached))
        {
            logger.LogDebug("Cache hit for keyword '{Keyword}' niche '{Niche}'", keyword, niche);
            return cached!;
        }

        logger.LogDebug("Cache miss for keyword '{Keyword}' niche '{Niche}' — fetching YouTube data", keyword, niche);

        var videos = await youtube.SearchAsync(keyword, maxResults: 10, ct);

        var result = new KeywordAnalysisResult(
            Keyword: keyword,
            Niche: niche,
            SearchVolumeRelative: CalculateSearchVolume(videos),
            CompetitionScore: CalculateCompetition(videos),
            TrendDirection: CalculateTrend(videos),
            KeywordIntent: ClassifyIntent(keyword),
            AnalyzedAt: DateTime.UtcNow);

        cache.Set(key, result, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
        });

        return result;
    }

    // -------------------------------------------------------------------------
    // Scoring algorithms
    // -------------------------------------------------------------------------

    private static short CalculateSearchVolume(List<VideoMetadata> videos)
    {
        if (videos.Count == 0) return 0;
        var totalViews = videos.Sum(v => v.ViewCount);
        return (short)Math.Min(totalViews / 1_000_000, 100);
    }

    private static short CalculateCompetition(List<VideoMetadata> videos)
    {
        if (videos.Count == 0) return 0;

        var avgEngagement = videos.Average(v =>
        {
            var total = (double)(v.LikeCount + v.CommentCount);
            return total / Math.Max(v.ViewCount, 1) * 100;
        });

        var dated = videos.Where(v => v.UploadDate.HasValue).ToList();
        var ageScore = 0;
        if (dated.Count > 0)
        {
            var avgAge = dated.Average(v => (DateTime.UtcNow - v.UploadDate!.Value).TotalDays);
            ageScore = Math.Min((int)(avgAge / 10), 50);
        }

        return (short)Math.Min((int)(avgEngagement + ageScore), 100);
    }

    private static string CalculateTrend(List<VideoMetadata> videos)
    {
        var dated = videos.Where(v => v.UploadDate.HasValue).ToList();
        if (dated.Count == 0) return "new";

        var recent = dated.Where(v => (DateTime.UtcNow - v.UploadDate!.Value).TotalDays < 30).ToList();
        var older  = dated.Where(v => (DateTime.UtcNow - v.UploadDate!.Value).TotalDays >= 30).ToList();

        if (older.Count == 0) return "new";

        var recentAvg = recent.Count > 0 ? recent.Average(v => (double)v.ViewCount) : 0;
        var olderAvg  = older.Average(v => (double)v.ViewCount);

        if (olderAvg == 0) return "new";

        var growth = (recentAvg - olderAvg) / olderAvg;
        return growth > 0.2 ? "rising" : growth < -0.2 ? "declining" : "flat";
    }

    private static string ClassifyIntent(string keyword)
    {
        keyword = keyword.ToLower();
        return keyword switch
        {
            _ when keyword.Contains("tutorial") || keyword.Contains("how to") => "how-to",
            _ when keyword.Contains("review")                                  => "opinion",
            _ when keyword.Contains("news")                                    => "news",
            _ when keyword.Contains("best")                                    => "entertainment",
            _                                                                  => "educational"
        };
    }
}
