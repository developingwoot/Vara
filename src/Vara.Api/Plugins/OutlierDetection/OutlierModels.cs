namespace Vara.Api.Plugins.OutlierDetection;

public record OutlierRequest(
    string Keyword,
    double MinOutlierRatio = 5,
    long MaxChannelSize = 500_000,
    int MaxResults = 20,
    int MaxAgeDays = 730,
    bool IncludeLlmInsights = false);

public record OutlierCandidate(
    string VideoId,
    string Title,
    string? ChannelName,
    long SubscriberCount,
    long ViewCount,
    DateTime? UploadDate);

public record OutlierVideo(
    string VideoId,
    string Title,
    string? ChannelName,
    long SubscriberCount,
    long ViewCount,
    double OutlierRatio,
    int OutlierScore,
    string OutlierStrength,
    DateTime? UploadDate,
    string? LlmInsight);

public record OutlierSummary(
    int TotalAnalyzed,
    int OutliersFound,
    int StrongOutliers,
    double AvgOutlierRatio,
    string? TopOpportunityTitle,
    IReadOnlyList<string> CommonPatterns);

public record OutlierResult(
    IReadOnlyList<OutlierVideo> Outliers,
    OutlierSummary Summary,
    int QuotaUsed);
