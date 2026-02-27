using System.ComponentModel.DataAnnotations;

namespace Vara.Api.Models.Entities;

public class Keyword
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Text { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Niche { get; set; }

    public short? SearchVolumeRelative { get; set; }

    public short? CompetitionScore { get; set; }

    [MaxLength(20)]
    public string? TrendDirection { get; set; }

    [MaxLength(50)]
    public string? KeywordIntent { get; set; }

    public DateTime? LastAnalyzed { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
