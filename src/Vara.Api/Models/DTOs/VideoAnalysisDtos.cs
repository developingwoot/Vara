namespace Vara.Api.Models.DTOs;

public record AnalyzeVideosRequest(string Keyword, int SampleSize = 20);

public record VideoAnalysisResponse(
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

public record AnalyzeTranscriptRequest(bool IncludeInsights = false);

public record TranscriptAnalysisResponse(
    string VideoId,
    string? Title,
    string? ChannelName,
    int WordCount,
    int SentenceCount,
    int EstimatedTokens,
    double ReadingTimeMinutes,
    bool TranscriptAvailable,
    string? LlmInsights,
    bool LlmEnhanced,
    DateTime AnalyzedAt);

public record VideoExportRow(
    string YoutubeId,
    string Title,
    string? ChannelName,
    int? DurationSeconds,
    long ViewCount,
    int LikeCount,
    int CommentCount,
    double EngagementRate,
    DateTime? UploadDate);
