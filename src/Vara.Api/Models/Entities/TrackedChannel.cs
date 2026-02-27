using System.ComponentModel.DataAnnotations;

namespace Vara.Api.Models.Entities;

public class TrackedChannel
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    [Required]
    [MaxLength(24)]
    public string YoutubeChannelId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Handle { get; set; }

    [MaxLength(255)]
    public string? DisplayName { get; set; }

    [MaxLength(512)]
    public string? ThumbnailUrl { get; set; }

    public long? SubscriberCount { get; set; }

    public int? VideoCount { get; set; }

    public long? TotalViewCount { get; set; }

    public bool IsOwner { get; set; }

    public bool IsVerified { get; set; }

    public DateTime? LastSyncedAt { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
