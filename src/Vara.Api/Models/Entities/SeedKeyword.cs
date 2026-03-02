namespace Vara.Api.Models.Entities;

public class SeedKeyword
{
    public int Id { get; set; }
    public string Keyword { get; set; } = string.Empty;
    public string Niche { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;  // "foundational" | "popular" | "emerging"
    public int Priority { get; set; } = 100;               // lower = processed first
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
