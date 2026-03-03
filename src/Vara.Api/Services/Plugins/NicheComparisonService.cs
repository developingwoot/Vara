using System.Text.Json;
using Vara.Api.Models.DTOs;
using Vara.Api.Plugins.OutlierDetection;
using Vara.Api.Services.Analysis;

namespace Vara.Api.Services.Plugins;

public interface INicheComparisonService
{
    Task<NicheComparisonResult> CompareAsync(
        Guid userId, string niche, bool includeInsights = false, CancellationToken ct = default);
}

public record NicheComparisonResult(
    string Niche,
    IReadOnlyList<TrendingKeywordDto> TrendingKeywords,
    OutlierResult TopOutliers,
    DateTime GeneratedAt);

public class NicheComparisonService(
    PluginExecutionService plugins,
    ITrendDetector trends,
    ILogger<NicheComparisonService> logger) : INicheComparisonService
{
    public async Task<NicheComparisonResult> CompareAsync(
        Guid userId, string niche, bool includeInsights = false, CancellationToken ct = default)
    {
        logger.LogInformation("Running niche comparison for {Niche}, user {UserId}", niche, userId);

        var trendTask = trends.FindTrendingAsync(userId, niche, ct: ct);

        // Build OutlierRequest JSON for the top keyword in this niche
        var outlierReqJson = JsonSerializer.SerializeToElement(new OutlierRequest(
            Keyword: niche,
            IncludeLlmInsights: includeInsights));

        var outlierTask = plugins.ExecuteAsync("outlier-detection", userId, outlierReqJson, ct);

        await Task.WhenAll(trendTask, outlierTask);

        var trendResult  = await trendTask;
        var outlierExec   = await outlierTask;
        var outlierResult = outlierExec.Result is OutlierResult typed
            ? typed
            : JsonSerializer.Deserialize<OutlierResult>(
                JsonSerializer.Serialize(outlierExec.Result))!;

        var allTrending = trendResult.Rising
            .Concat(trendResult.New)
            .Select(t => new TrendingKeywordDto(
                t.Keyword, t.Niche, t.CurrentVolume, t.PreviousVolume,
                t.GrowthRate, t.MomentumScore, t.Lifecycle, t.LastCaptured))
            .ToList();

        return new NicheComparisonResult(niche, allTrending, outlierResult, DateTime.UtcNow);
    }
}
