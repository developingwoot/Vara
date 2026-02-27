namespace Vara.Api.Models.DTOs;

public record AnalyzeKeywordRequest(string Keyword, string? Niche);

public record KeywordResponse(
    Guid Id,
    string Keyword,
    string? Niche,
    short? SearchVolumeRelative,
    short? CompetitionScore,
    string? TrendDirection,
    string? KeywordIntent,
    DateTime? LastAnalyzed,
    DateTime CreatedAt);

public record KeywordAnalysisResponse(
    Guid Id,
    string Keyword,
    string? Niche,
    short SearchVolumeRelative,
    short CompetitionScore,
    string TrendDirection,
    string KeywordIntent,
    DateTime AnalyzedAt);
