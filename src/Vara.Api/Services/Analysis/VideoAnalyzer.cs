using Microsoft.Extensions.Caching.Memory;
using Vara.Api.Services.YouTube;

namespace Vara.Api.Services.Analysis;

public class VideoAnalyzer(
    IYouTubeClient youtube,
    IMemoryCache cache,
    ILogger<VideoAnalyzer> logger) : IVideoAnalyzer
{
    private static string CacheKey(string keyword, int sampleSize) =>
        $"va:{keyword.ToLowerInvariant()}:{sampleSize}";

    public async Task<VideoAnalysisResult> AnalyzeAsync(
        string keyword,
        int sampleSize = 20,
        CancellationToken ct = default)
    {
        var key = CacheKey(keyword, sampleSize);

        if (cache.TryGetValue(key, out VideoAnalysisResult? cached))
        {
            logger.LogDebug("Cache hit for video analysis '{Keyword}' sampleSize={SampleSize}", keyword, sampleSize);
            return cached!;
        }

        logger.LogDebug("Cache miss for video analysis '{Keyword}' — fetching YouTube data", keyword);

        var videos = await youtube.SearchAsync(keyword, maxResults: sampleSize, ct);

        var (avgTitle, minTitle, maxTitle, stdDev) = CalculateTitleStats(videos);
        var (avgDur, minDur, maxDur)               = CalculateDurationStats(videos);
        var uploadsByDay                            = CalculateUploadsByDay(videos);

        var result = new VideoAnalysisResult(
            Keyword:            keyword,
            SampleSize:         videos.Count,
            AvgTitleLength:     avgTitle,
            MinTitleLength:     minTitle,
            MaxTitleLength:     maxTitle,
            TitleLengthStdDev:  stdDev,
            AvgDurationSeconds: avgDur,
            MinDurationSeconds: minDur,
            MaxDurationSeconds: maxDur,
            AvgViewCount:       videos.Count > 0 ? Math.Round(videos.Average(v => (double)v.ViewCount), 2) : 0,
            AvgEngagementRate:  CalculateAvgEngagement(videos),
            UploadsByDayOfWeek: uploadsByDay,
            Patterns:           [],
            AnalyzedAt:         DateTime.UtcNow);

        // Patterns computed after the record is built so they can reference it
        result = result with { Patterns = ExtractPatterns(result) };

        cache.Set(key, result, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
        });

        return result;
    }

    // -------------------------------------------------------------------------
    // Statistical helpers
    // -------------------------------------------------------------------------

    private static (double avg, int min, int max, double stdDev) CalculateTitleStats(
        List<VideoMetadata> videos)
    {
        var lengths = videos
            .Where(v => !string.IsNullOrEmpty(v.Title))
            .Select(v => (double)v.Title!.Length)
            .ToList();

        if (lengths.Count == 0)
            return (0, 0, 0, 0);

        var avg = lengths.Average();
        return (
            Math.Round(avg, 2),
            (int)lengths.Min(),
            (int)lengths.Max(),
            Math.Round(StdDev(lengths, avg), 2));
    }

    private static (double? avg, int? min, int? max) CalculateDurationStats(
        List<VideoMetadata> videos)
    {
        var durations = videos
            .Where(v => v.DurationSeconds.HasValue)
            .Select(v => v.DurationSeconds!.Value)
            .ToList();

        if (durations.Count == 0)
            return (null, null, null);

        return (
            Math.Round(durations.Average(), 0),
            durations.Min(),
            durations.Max());
    }

    private static double CalculateAvgEngagement(List<VideoMetadata> videos)
    {
        if (videos.Count == 0) return 0;
        return Math.Round(videos.Average(v =>
            (v.LikeCount + v.CommentCount) / (double)Math.Max(v.ViewCount, 1) * 100), 2);
    }

    private static IReadOnlyDictionary<string, int> CalculateUploadsByDay(
        List<VideoMetadata> videos)
    {
        return videos
            .Where(v => v.UploadDate.HasValue)
            .GroupBy(v => v.UploadDate!.Value.DayOfWeek.ToString())
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private static IReadOnlyList<string> ExtractPatterns(VideoAnalysisResult r)
    {
        var patterns = new List<string>();

        if (r.AvgTitleLength > 50)
            patterns.Add($"Titles average {r.AvgTitleLength:F0} chars — longer titles dominate this niche");

        if (r.AvgDurationSeconds.HasValue && r.AvgDurationSeconds < 600)
            patterns.Add($"Most videos are under 10 minutes ({r.AvgDurationSeconds:F0}s avg)");

        if (r.AvgDurationSeconds.HasValue && r.AvgDurationSeconds > 1200)
            patterns.Add($"Long-form content dominates ({r.AvgDurationSeconds / 60:F0} min avg)");

        if (r.AvgEngagementRate > 5)
            patterns.Add($"High engagement niche — {r.AvgEngagementRate:F1}% avg engagement rate");

        if (r.UploadsByDayOfWeek.Count > 0)
        {
            var topDay = r.UploadsByDayOfWeek.MaxBy(kv => kv.Value);
            patterns.Add($"Most uploads on {topDay.Key} ({topDay.Value} of {r.SampleSize} videos)");
        }

        if (patterns.Count == 0)
            patterns.Add("No strong patterns detected in this sample");

        return patterns;
    }

    private static double StdDev(List<double> values, double avg)
    {
        var variance = values.Average(x => Math.Pow(x - avg, 2));
        return Math.Sqrt(variance);
    }
}
