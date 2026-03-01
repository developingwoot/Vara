using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Vara.Api.Data;

namespace Vara.Api.Services.Analysis;

public class TrendDetectionService(
    VaraContext db,
    IMemoryCache cache,
    ILogger<TrendDetectionService> logger)
    : ITrendDetector
{
    public async Task<FindTrendingResult> FindTrendingAsync(
        Guid userId,
        string? niche = null,
        int minSnapshots = 2,
        CancellationToken ct = default)
    {
        var cacheKey = $"trends:{userId}:{niche ?? "all"}:{minSnapshots}";
        if (cache.TryGetValue(cacheKey, out FindTrendingResult? cached))
        {
            logger.LogInformation("Trend cache hit for {CacheKey}", cacheKey);
            return cached!;
        }

        var now = DateTime.UtcNow;
        var currentStart = now.AddDays(-7);
        var previousStart = now.AddDays(-14);

        var query = db.KeywordSnapshots
            .Where(s => s.UserId == userId && s.CapturedAt >= previousStart);

        if (niche is not null)
            query = query.Where(s => s.Niche == niche);

        var snapshots = await query
            .OrderByDescending(s => s.CapturedAt)
            .ToListAsync(ct);

        var rising = new List<TrendingKeyword>();
        var declining = new List<TrendingKeyword>();
        var newKeywords = new List<TrendingKeyword>();

        var groups = snapshots.GroupBy(s => (s.Keyword, s.Niche));

        foreach (var group in groups)
        {
            var currentWindow = group.Where(s => s.CapturedAt >= currentStart).ToList();
            var previousWindow = group.Where(s => s.CapturedAt < currentStart).ToList();

            if (currentWindow.Count < minSnapshots) continue;

            var currentVolume = (long)currentWindow.Average(s => (double)s.SearchVolumeRelative);
            var lastCaptured = currentWindow.Max(s => s.CapturedAt);

            if (!previousWindow.Any())
            {
                newKeywords.Add(new TrendingKeyword(
                    group.Key.Keyword, group.Key.Niche,
                    currentVolume, 0, 0, 0, "New", lastCaptured));
                continue;
            }

            var previousVolume = (long)previousWindow.Average(s => (double)s.SearchVolumeRelative);
            var growthRate = Math.Round(
                ((currentVolume - previousVolume) / (double)Math.Max(previousVolume, 1)) * 100, 2);
            var momentum = Math.Round(growthRate * Math.Log(currentVolume + 1), 2);

            var lifecycle = growthRate switch
            {
                > 10 => "Rising",
                < -10 => "Declining",
                _ => "Stable"
            };

            var trend = new TrendingKeyword(
                group.Key.Keyword, group.Key.Niche,
                currentVolume, previousVolume, growthRate, momentum, lifecycle, lastCaptured);

            if (lifecycle == "Rising") rising.Add(trend);
            else if (lifecycle == "Declining") declining.Add(trend);
        }

        var result = new FindTrendingResult(
            Rising: rising.OrderByDescending(t => t.MomentumScore).ToList(),
            Declining: declining.OrderBy(t => t.GrowthRate).ToList(),
            New: newKeywords.OrderByDescending(t => t.CurrentVolume).ToList(),
            GeneratedAt: DateTime.UtcNow);

        cache.Set(cacheKey, result, TimeSpan.FromMinutes(15));

        logger.LogInformation(
            "Trend analysis for user {UserId}: {Rising} rising, {Declining} declining, {New} new",
            userId, result.Rising.Count, result.Declining.Count, result.New.Count);

        return result;
    }
}
