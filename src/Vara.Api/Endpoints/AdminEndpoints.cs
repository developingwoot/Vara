using Microsoft.EntityFrameworkCore;
using Vara.Api.Data;

namespace Vara.Api.Endpoints;

public static class AdminEndpoints
{
    public static RouteGroupBuilder MapAdminCostEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetCostReport)
            .WithTags("Admin")
            .WithSummary("Aggregate LLM cost report by billing period (admin only)");

        group.MapGet("/breakdown", GetCostBreakdown)
            .WithTags("Admin")
            .WithSummary("Per-user, per-task LLM cost breakdown for a billing period (admin only)");

        return group;
    }

    // -------------------------------------------------------------------------
    // GET /api/admin/costs?period=2026-03
    // Returns total cost + summary rows grouped by provider and task type.
    // -------------------------------------------------------------------------

    private static async Task<IResult> GetCostReport(
        VaraContext db,
        string? period = null)
    {
        var billing = ParsePeriod(period);

        var rows = await db.LlmCostLogs
            .AsNoTracking()
            .Where(l => l.BillingPeriod == billing)
            .GroupBy(l => new { l.Provider, l.TaskType })
            .Select(g => new
            {
                Provider          = g.Key.Provider,
                TaskType          = g.Key.TaskType,
                Calls             = g.Count(),
                TotalPromptTokens = g.Sum(l => l.PromptTokens),
                TotalCompTokens   = g.Sum(l => l.CompletionTokens),
                TotalCostUsd      = g.Sum(l => l.CostUsd)
            })
            .OrderByDescending(r => r.TotalCostUsd)
            .ToListAsync();

        return Results.Ok(new
        {
            Period       = billing.ToString("yyyy-MM"),
            TotalCostUsd = rows.Sum(r => r.TotalCostUsd),
            TotalCalls   = rows.Sum(r => r.Calls),
            Breakdown    = rows
        });
    }

    // -------------------------------------------------------------------------
    // GET /api/admin/costs/breakdown?period=2026-03
    // Per-user cost rows — useful for identifying expensive accounts.
    // -------------------------------------------------------------------------

    private static async Task<IResult> GetCostBreakdown(
        VaraContext db,
        string? period = null)
    {
        var billing = ParsePeriod(period);

        var rows = await db.LlmCostLogs
            .AsNoTracking()
            .Where(l => l.BillingPeriod == billing)
            .GroupBy(l => new { l.UserId, l.TaskType, l.Provider, l.Model })
            .Select(g => new
            {
                UserId            = g.Key.UserId,
                TaskType          = g.Key.TaskType,
                Provider          = g.Key.Provider,
                Model             = g.Key.Model,
                Calls             = g.Count(),
                TotalPromptTokens = g.Sum(l => l.PromptTokens),
                TotalCompTokens   = g.Sum(l => l.CompletionTokens),
                TotalCostUsd      = g.Sum(l => l.CostUsd)
            })
            .OrderByDescending(r => r.TotalCostUsd)
            .ToListAsync();

        return Results.Ok(new
        {
            Period       = billing.ToString("yyyy-MM"),
            TotalCostUsd = rows.Sum(r => r.TotalCostUsd),
            Rows         = rows
        });
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>Parses "yyyy-MM" → first day of that month. Defaults to current month.</summary>
    private static DateOnly ParsePeriod(string? period)
    {
        if (!string.IsNullOrEmpty(period) &&
            DateOnly.TryParseExact(period, "yyyy-MM", out var parsed))
            return new DateOnly(parsed.Year, parsed.Month, 1);

        var now = DateTime.UtcNow;
        return new DateOnly(now.Year, now.Month, 1);
    }
}
