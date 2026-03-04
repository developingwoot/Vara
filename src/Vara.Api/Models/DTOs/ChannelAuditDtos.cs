namespace Vara.Api.Models.DTOs;

// ─── Quick Scan ──────────────────────────────────────────────────────────────

public record ChannelQuickScanResult(
    Guid ChannelId,
    string? ChannelName,
    bool HasVideos,
    bool IsSynced,
    int TotalVideos,
    int OverallScore,
    string VaraAssessment,
    ChannelMetricComparison ViewsComparison,
    ChannelMetricComparison EngagementComparison,
    PostingFrequencyStats PostingStats,
    List<ActionablePriority> Priorities,
    List<ChannelBadge> Badges,
    List<VideoSnapshotDto> RecentVideos,
    List<VideoSnapshotDto> TopVideos,
    DateTime GeneratedAt);

public record ChannelMetricComparison(
    double RecentAvg,
    double TopAvg,
    double GapPercent,
    string Trend);         // "above" | "on-par" | "below"

public record PostingFrequencyStats(
    double PostsPerMonth,
    DateTime? LastUploadDate,
    int DaysSinceLastUpload,
    string Consistency);   // "regular" | "irregular" | "inactive"

public record ActionablePriority(
    int Rank,
    string Severity,       // "critical" | "improve" | "maintain"
    string Title,
    string Description);

public record ChannelBadge(
    string Id,
    string Name,
    string Description,
    string Category,       // "achievement" | "performance"
    string Tier,           // "bronze" | "silver" | "gold"
    string Icon,
    bool Earned,
    DateTime? EarnedAt);

public record VideoSnapshotDto(
    string YoutubeId,
    string? Title,
    long ViewCount,
    int LikeCount,
    int CommentCount,
    int? DurationSeconds,
    DateTime? UploadDate,
    string? ThumbnailUrl,
    double EngagementRate);

// ─── Deep Audit ──────────────────────────────────────────────────────────────

public record ChannelDeepAuditResult(
    Guid ChannelId,
    string? RecentVideoTitle,
    string? TopVideoTitle,
    bool TranscriptsAvailable,
    string? LlmAnalysis,
    DateTime GeneratedAt);

// ─── Video Comparison ────────────────────────────────────────────────────────

public record CompareVideosRequest(string Video1Id, string Video2Id);

public record VideoComparisonResult(
    string Video1Id,
    string? Video1Title,
    string Video2Id,
    string? Video2Title,
    bool TranscriptsAvailable,
    string? LlmAnalysis,
    DateTime GeneratedAt);
