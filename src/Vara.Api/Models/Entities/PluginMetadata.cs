using System.ComponentModel.DataAnnotations;

namespace Vara.Api.Models.Entities;

public class PluginMetadata
{
    public Guid Id { get; set; }

    [MaxLength(100)]
    public string PluginId { get; set; } = string.Empty;

    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Version { get; set; } = string.Empty;

    [MaxLength(255)]
    public string Author { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Tier { get; set; } = "free";

    public bool Enabled { get; set; } = true;

    [MaxLength(500)]
    public string PluginDirectory { get; set; } = string.Empty;

    public int? UnitsPerRun { get; set; }

    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
