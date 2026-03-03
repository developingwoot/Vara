namespace Vara.Api.Models.DTOs;

public record AddChannelRequest(string HandleOrUrl, bool IsOwner, string? Niche);

public record ChannelResponse(
    Guid Id,
    string YoutubeChannelId,
    string? Handle,
    string? DisplayName,
    string? ThumbnailUrl,
    long? SubscriberCount,
    int? VideoCount,
    long? TotalViewCount,
    bool IsOwner,
    bool IsVerified,
    DateTime? LastSyncedAt,
    DateTime AddedAt,
    string? NicheRaw,
    string? NicheName);

public record VideoSummary(string YoutubeId, string? Title, long ViewCount);

public record ChannelStatsResponse(
    double AvgViews,
    double MedianViews,
    List<VideoSummary> TopVideos,
    List<VideoSummary> BottomVideos,
    double PostsPerMonth);
