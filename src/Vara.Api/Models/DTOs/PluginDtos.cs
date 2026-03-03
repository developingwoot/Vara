using Vara.Api.Plugins.OutlierDetection;

namespace Vara.Api.Models.DTOs;

public record PluginListItem(
    string PluginId,
    string Name,
    string Version,
    string Author,
    string Description,
    string Tier,
    bool Enabled,
    int? UnitsPerRun);

public record ExecutePluginResponse(
    string PluginId,
    Guid AnalysisId,
    object Result,
    DateTime ExecutedAt,
    bool FromCache = false);

public record PluginResultSummary(
    Guid AnalysisId,
    string PluginId,
    object Result,
    DateTime ExecutedAt);

public record NicheComparisonRequest(
    string Niche,
    bool IncludeInsights = false);

public record NicheComparisonResponse(
    string Niche,
    IReadOnlyList<TrendingKeywordDto> TrendingKeywords,
    OutlierResult TopOutliers,
    DateTime GeneratedAt);
