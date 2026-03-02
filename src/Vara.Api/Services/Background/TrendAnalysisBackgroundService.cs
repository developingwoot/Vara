using Microsoft.EntityFrameworkCore;
using Vara.Api.Data;
using Vara.Api.Models.Entities;
using Vara.Api.Services.Monitoring;
using Vara.Api.Services.YouTube;

namespace Vara.Api.Services.Background;

public class TrendAnalysisBackgroundService(
    IServiceProvider serviceProvider,
    BackgroundJobHealthMonitor healthMonitor,
    ILogger<TrendAnalysisBackgroundService> logger) : BackgroundService
{
    private const string JobName = "TrendAnalysis";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("TrendAnalysisBackgroundService starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNext2Am();
            logger.LogInformation("Next trend collection run in {Delay:hh\\:mm\\:ss}", delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            healthMonitor.RecordStart(JobName);
            try
            {
                await CollectTrendDataAsync(stoppingToken);
                await RunDataRetentionAsync(stoppingToken);
                healthMonitor.RecordSuccess(JobName);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Trend collection run failed — will retry at next scheduled time");
                healthMonitor.RecordFailure(JobName, ex.Message);
                // Don't rethrow — keep the loop alive for the next scheduled run
            }
        }

        logger.LogInformation("TrendAnalysisBackgroundService stopped");
    }

    internal async Task CollectTrendDataAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var db      = scope.ServiceProvider.GetRequiredService<VaraContext>();
        var youtube = scope.ServiceProvider.GetRequiredService<IYouTubeClient>();

        var seedKeywords = await db.SeedKeywords
            .Where(k => k.IsActive)
            .OrderBy(k => k.Priority)
            .ToListAsync(ct);

        logger.LogInformation("Trend collection starting — {Count} seed keywords", seedKeywords.Count);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var saved = 0;

        foreach (var seed in seedKeywords)
        {
            if (ct.IsCancellationRequested) break;

            var alreadyRecorded = await db.KeywordVolumeHistory
                .AnyAsync(h => h.Keyword == seed.Keyword
                            && h.Niche == seed.Niche
                            && h.RecordedDate == today
                            && h.Source == "seed", ct);

            if (alreadyRecorded) continue;

            try
            {
                var videos = await youtube.SearchAsync(seed.Keyword, maxResults: 10, ct);
                var volume = CalculateVolume(videos);

                db.KeywordVolumeHistory.Add(new KeywordVolumeHistory
                {
                    Keyword      = seed.Keyword,
                    Niche        = seed.Niche,
                    Volume       = volume,
                    Source       = "seed",
                    RecordedDate = today
                });

                saved++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Failed to collect data for keyword '{Keyword}'", seed.Keyword);
            }
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Trend collection complete — {Saved} records written", saved);
    }

    internal async Task RunDataRetentionAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VaraContext>();

        var now = DateTime.UtcNow;

        // keyword_snapshots: keep 90 days (trend analysis only needs ~30 days)
        var snapshotCutoff = now.AddDays(-90);
        var snapshots = await db.KeywordSnapshots
            .Where(s => s.CapturedAt < snapshotCutoff)
            .ExecuteDeleteAsync(ct);

        // usage_logs: keep 13 months for billing history
        var usageLogCutoff = new DateOnly(now.Year, now.Month, 1).AddMonths(-13);
        var usageLogs = await db.UsageLogs
            .Where(l => l.BillingPeriod < usageLogCutoff)
            .ExecuteDeleteAsync(ct);

        // keyword_volume_history: keep 365 days for year-over-year trend analysis
        var volumeCutoff = DateOnly.FromDateTime(now.AddDays(-365));
        var volumeHistory = await db.KeywordVolumeHistory
            .Where(h => h.RecordedDate < volumeCutoff)
            .ExecuteDeleteAsync(ct);

        logger.LogInformation(
            "Data retention complete — snapshots: -{Snapshots}, usage logs: -{UsageLogs}, volume history: -{VolumeHistory}",
            snapshots, usageLogs, volumeHistory);
    }

    internal static int CalculateVolume(List<VideoMetadata> videos)
    {
        if (videos.Count == 0) return 0;
        var totalViews = videos.Sum(v => v.ViewCount);
        return (int)Math.Clamp(totalViews / 1_000_000L, 0, 100);
    }

    internal static TimeSpan GetDelayUntilNext2Am()
    {
        var now     = DateTime.UtcNow;
        var next2Am = now.Date.AddDays(now.Hour >= 2 ? 1 : 0).AddHours(2);
        return next2Am - now;
    }
}
