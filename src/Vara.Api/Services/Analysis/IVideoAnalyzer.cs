namespace Vara.Api.Services.Analysis;

public interface IVideoAnalyzer
{
    Task<VideoAnalysisResult> AnalyzeAsync(
        string keyword,
        int sampleSize = 20,
        CancellationToken ct = default);
}

public record VideoAnalysisResult(
    string Keyword,
    int SampleSize,
    double AvgTitleLength,
    int MinTitleLength,
    int MaxTitleLength,
    double TitleLengthStdDev,
    double? AvgDurationSeconds,
    int? MinDurationSeconds,
    int? MaxDurationSeconds,
    double AvgViewCount,
    double AvgEngagementRate,
    IReadOnlyDictionary<string, int> UploadsByDayOfWeek,
    IReadOnlyList<string> Patterns,
    DateTime AnalyzedAt);
