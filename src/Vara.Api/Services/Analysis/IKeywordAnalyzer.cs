namespace Vara.Api.Services.Analysis;

public interface IKeywordAnalyzer
{
    Task<KeywordAnalysisResult> AnalyzeAsync(
        string keyword,
        string? niche = null,
        CancellationToken ct = default);
}

public record KeywordAnalysisResult(
    string Keyword,
    string? Niche,
    short SearchVolumeRelative,
    short CompetitionScore,
    string TrendDirection,
    string KeywordIntent,
    DateTime AnalyzedAt);
