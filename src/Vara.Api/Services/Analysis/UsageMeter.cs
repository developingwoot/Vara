using Vara.Api.Data;
using Vara.Api.Models.Entities;

namespace Vara.Api.Services.Analysis;

public static class LlmCallWeights
{
    public static readonly Dictionary<string, int> ByTaskType = new()
    {
        ["KeywordInsights"]    = 1,
        ["VideoInsights"]      = 4,
        ["TranscriptAnalysis"] = 8,
        ["OutlierInsights"]    = 2
    };
}

public interface IUsageMeter
{
    Task RecordLlmCallAsync(Guid userId, string taskType, CancellationToken ct = default);
    Task RecordPluginRunAsync(Guid userId, string pluginId, int units, CancellationToken ct = default);

    Task RecordLlmCostAsync(
        Guid userId, string taskType,
        string provider, string model,
        int promptTokens, int completionTokens, decimal costUsd,
        CancellationToken ct = default);
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

    public async Task RecordPluginRunAsync(Guid userId, string pluginId, int units, CancellationToken ct = default)
    {
        db.UsageLogs.Add(new UsageLog
        {
            UserId        = userId,
            Feature       = $"plugin:{pluginId}",
            UnitCount     = units,
            BillingPeriod = DateOnly.FromDateTime(DateTime.UtcNow),
            CreatedAt     = DateTime.UtcNow
        });

        await db.SaveChangesAsync(ct);
    }

    public async Task RecordLlmCostAsync(
        Guid userId, string taskType,
        string provider, string model,
        int promptTokens, int completionTokens, decimal costUsd,
        CancellationToken ct = default)
    {
        db.LlmCostLogs.Add(new LlmCostLog
        {
            UserId            = userId,
            TaskType          = taskType,
            Provider          = provider,
            Model             = model,
            PromptTokens      = promptTokens,
            CompletionTokens  = completionTokens,
            CostUsd           = costUsd,
            BillingPeriod     = DateOnly.FromDateTime(DateTime.UtcNow),
            CreatedAt         = DateTime.UtcNow
        });

        await db.SaveChangesAsync(ct);
    }
}
