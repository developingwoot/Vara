using Vara.Api.Data;
using Vara.Api.Models.Entities;

namespace Vara.Api.Services.Analysis;

public static class LlmCallWeights
{
    public static readonly Dictionary<string, int> ByTaskType = new()
    {
        ["KeywordInsights"]    = 1,
        ["VideoInsights"]      = 4,
        ["TranscriptAnalysis"] = 8
    };
}

public interface IUsageMeter
{
    Task RecordLlmCallAsync(Guid userId, string taskType, CancellationToken ct = default);
}

public class UsageMeter(VaraContext db) : IUsageMeter
{
    public async Task RecordLlmCallAsync(Guid userId, string taskType, CancellationToken ct = default)
    {
        var weight = LlmCallWeights.ByTaskType.GetValueOrDefault(taskType, 1);

        db.UsageLogs.Add(new UsageLog
        {
            UserId        = userId,
            Feature       = "llm_call",
            UnitCount     = weight,
            BillingPeriod = DateOnly.FromDateTime(DateTime.UtcNow),
            CreatedAt     = DateTime.UtcNow
        });

        await db.SaveChangesAsync(ct);
    }
}
