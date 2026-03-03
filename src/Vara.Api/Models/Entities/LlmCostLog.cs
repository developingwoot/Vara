using System.ComponentModel.DataAnnotations;

namespace Vara.Api.Models.Entities;

public class LlmCostLog
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    [MaxLength(100)]
    public string TaskType { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Provider { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Model { get; set; } = string.Empty;

    public int PromptTokens { get; set; }

    public int CompletionTokens { get; set; }

    public decimal CostUsd { get; set; }

    public DateOnly BillingPeriod { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
