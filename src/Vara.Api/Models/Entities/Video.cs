using System.ComponentModel.DataAnnotations;

namespace Vara.Api.Models.Entities;

public class Video
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    [Required]
    [MaxLength(11)]
    public string YoutubeId { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Title { get; set; }

    public string? Description { get; set; }

    [MaxLength(255)]
    public string? ChannelName { get; set; }

    [MaxLength(24)]
    public string? ChannelId { get; set; }

    public int? DurationSeconds { get; set; }

    public DateTime? UploadDate { get; set; }

    public long ViewCount { get; set; }

    public int LikeCount { get; set; }

    public int CommentCount { get; set; }

    [MaxLength(512)]
    public string? ThumbnailUrl { get; set; }

    public string? TranscriptText { get; set; }

    public DateTime? MetadataFetchedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
