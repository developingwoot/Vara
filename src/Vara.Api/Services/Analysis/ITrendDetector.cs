namespace Vara.Api.Services.Analysis;

public interface ITrendDetector
{
    Task<FindTrendingResult> FindTrendingAsync(
        Guid userId,
        string? niche = null,
        int minSnapshots = 2,
        CancellationToken ct = default);
}

public record TrendingKeyword(
    string Keyword,
    string? Niche,
    long CurrentVolume,
    long PreviousVolume,
    double GrowthRate,
    double MomentumScore,
    string Lifecycle,
    DateTime LastCaptured);

public record FindTrendingResult(
    IReadOnlyList<TrendingKeyword> Rising,
    IReadOnlyList<TrendingKeyword> Declining,
    IReadOnlyList<TrendingKeyword> New,
    DateTime GeneratedAt);
