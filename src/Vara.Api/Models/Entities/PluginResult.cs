using System.ComponentModel.DataAnnotations;

namespace Vara.Api.Models.Entities;

public class PluginResult
{
    public Guid Id { get; set; }

    public Guid AnalysisId { get; set; }

    [MaxLength(100)]
    public string PluginId { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public string ResultDataJson { get; set; } = "{}";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
