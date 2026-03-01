namespace Vara.Api.Models.Entities;

public class KeywordSnapshot
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Keyword { get; set; } = string.Empty;
    public string? Niche { get; set; }
    public short SearchVolumeRelative { get; set; }
    public short CompetitionScore { get; set; }
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
