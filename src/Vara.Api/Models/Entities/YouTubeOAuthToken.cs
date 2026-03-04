using System.ComponentModel.DataAnnotations;

namespace Vara.Api.Models.Entities;

public class YouTubeOAuthToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    [MaxLength(64)]
    public string YoutubeChannelId { get; set; } = string.Empty;

    // Stored in plaintext for now; production should use AES encryption at rest.
    public string AccessToken { get; set; } = string.Empty;

    public string? RefreshToken { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
