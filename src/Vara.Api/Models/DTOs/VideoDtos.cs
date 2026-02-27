namespace Vara.Api.Models.DTOs;

public record SaveVideoRequest(string YoutubeId);

public record VideoResponse(
    Guid Id,
    string YoutubeId,
    string? Title,
    string? Description,
    string? ChannelName,
    string? ChannelId,
    int? DurationSeconds,
    DateTime? UploadDate,
    long ViewCount,
    int LikeCount,
    int CommentCount,
    string? ThumbnailUrl,
    DateTime CreatedAt);

public record VideoSearchResult(
    string YoutubeId,
    string? Title,
    string? ChannelName,
    string? ChannelId,
    int? DurationSeconds,
    long ViewCount,
    int LikeCount,
    int CommentCount,
    string? ThumbnailUrl,
    DateTime? UploadDate);
