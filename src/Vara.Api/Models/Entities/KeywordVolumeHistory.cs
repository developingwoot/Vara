namespace Vara.Api.Models.Entities;

public class KeywordVolumeHistory
{
    public int Id { get; set; }
    public string Keyword { get; set; } = string.Empty;
    public string Niche { get; set; } = string.Empty;
    public int Volume { get; set; }                        // 0–100 normalized scale
    public string Source { get; set; } = "seed";           // "seed" | "user_custom"
    public DateOnly RecordedDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
