using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vara.Api.Models.Entities;

public class User
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? FullName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    public string SubscriptionTier { get; set; } = "free";

    public DateTime? SubscriptionExpiresAt { get; set; }

    [Column(TypeName = "jsonb")]
    public string Settings { get; set; } = "{}";
}
