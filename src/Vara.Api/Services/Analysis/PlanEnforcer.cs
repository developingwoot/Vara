using Microsoft.EntityFrameworkCore;
using Vara.Api.Data;

namespace Vara.Api.Services.Analysis;

public sealed class FeatureAccessDeniedException(string message) : Exception(message);
public sealed class QuotaExceededException(string message) : Exception(message);

public interface IPlanEnforcer
{
    Task EnforceAsync(Guid userId, string feature, CancellationToken ct = default);
}

public class PlanEnforcer(VaraContext db, IConfiguration config) : IPlanEnforcer
{
    private static readonly Dictionary<string, string[]> TierFeatures = new()
    {
        ["free"]    = ["keyword_research", "video_metadata"],
        ["creator"] = ["keyword_research", "video_metadata", "transcripts",
                       "llm_insights", "niche_comparison", "outlier_insights"]
    };

    // Features that consume LLM credits and count against the monthly quota
    private static readonly HashSet<string> LlmFeatures =
        ["llm_insights", "transcripts", "niche_comparison", "outlier_insights"];

    public async Task EnforceAsync(Guid userId, string feature, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([userId], ct)
            ?? throw new InvalidOperationException($"User {userId} not found.");

        // Treat expired subscriptions as free tier
        var effectiveTier = user.SubscriptionTier;
        if (user.SubscriptionExpiresAt.HasValue && user.SubscriptionExpiresAt.Value < DateTime.UtcNow)
            effectiveTier = "free";

        var allowed = TierFeatures.GetValueOrDefault(effectiveTier, []);

        if (!allowed.Contains(feature))
            throw new FeatureAccessDeniedException(
                $"Feature '{feature}' requires the Creator tier. Upgrade at $7/month per channel.");

        // Check monthly LLM credit quota for features that consume credits
        if (LlmFeatures.Contains(feature))
            await EnforceLlmQuotaAsync(userId, effectiveTier, ct);
    }

    private async Task EnforceLlmQuotaAsync(Guid userId, string tier, CancellationToken ct)
    {
        var limit = config.GetValue<int>($"Plans:{tier}:MonthlyLlmUnits", 500);

        var now          = DateTime.UtcNow;
        var firstOfMonth = new DateOnly(now.Year, now.Month, 1);

        var usedUnits = await db.UsageLogs
            .Where(l => l.UserId == userId && l.BillingPeriod >= firstOfMonth)
            .SumAsync(l => l.UnitCount, ct);

        if (usedUnits >= limit)
            throw new QuotaExceededException(
                $"Monthly LLM quota of {limit} units reached. Resets on the 1st of next month.");
    }
}
