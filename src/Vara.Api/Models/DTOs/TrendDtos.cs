namespace Vara.Api.Models.DTOs;

public record TrendingKeywordDto(
    string Keyword,
    string? Niche,
    long CurrentVolume,
    long PreviousVolume,
    double GrowthRate,
    double MomentumScore,
    string Lifecycle,
    DateTime LastCaptured);

public record FindTrendingResponse(
    IReadOnlyList<TrendingKeywordDto> Rising,
    IReadOnlyList<TrendingKeywordDto> Declining,
    IReadOnlyList<TrendingKeywordDto> New,
    DateTime GeneratedAt);
