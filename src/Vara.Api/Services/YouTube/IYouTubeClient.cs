namespace Vara.Api.Services.YouTube;

public interface IYouTubeClient
{
    /// <summary>
    /// Search YouTube for videos matching a keyword. Returns up to maxResults items.
    /// Costs 100 quota units per call.
    /// </summary>
    Task<List<VideoMetadata>> SearchAsync(
        string keyword,
        int maxResults = 10,
        CancellationToken ct = default);

    /// <summary>
    /// Fetch full metadata for a single video by its YouTube ID.
    /// Costs 1 quota unit per call.
    /// </summary>
    Task<VideoMetadata?> GetVideoAsync(
        string videoId,
        CancellationToken ct = default);

    /// <summary>
    /// Attempt to retrieve the auto-generated transcript for a video.
    /// Returns null if no transcript is available.
    /// </summary>
    Task<string?> GetTranscriptAsync(
        string videoId,
        CancellationToken ct = default);
}

public record VideoMetadata(
    string YoutubeId,
    string Title,
    string? Description,
    string? ChannelName,
    string? ChannelId,
    int? DurationSeconds,
    DateTime? UploadDate,
    long ViewCount,
    int LikeCount,
    int CommentCount,
    string? ThumbnailUrl
);
