namespace Vara.Api.Models.Entities;

public class UsageLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Feature { get; set; } = string.Empty;
    public int UnitCount { get; set; } = 1;
    public DateOnly BillingPeriod { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
