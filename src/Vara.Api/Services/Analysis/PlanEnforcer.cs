using Vara.Api.Data;

namespace Vara.Api.Services.Analysis;

public sealed class FeatureAccessDeniedException(string message) : Exception(message);

public interface IPlanEnforcer
{
    Task EnforceAsync(Guid userId, string feature, CancellationToken ct = default);
}

public class PlanEnforcer(VaraContext db) : IPlanEnforcer
{
    private static readonly Dictionary<string, string[]> TierFeatures = new()
    {
        ["free"]    = ["keyword_research", "video_metadata"],
        ["creator"] = ["keyword_research", "video_metadata", "transcripts",
                       "llm_insights", "niche_comparison", "outlier_insights"]
    };

    public async Task EnforceAsync(Guid userId, string feature, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([userId], ct)
            ?? throw new InvalidOperationException($"User {userId} not found.");

        var allowed = TierFeatures.GetValueOrDefault(user.SubscriptionTier, []);

        if (!allowed.Contains(feature))
            throw new FeatureAccessDeniedException(
                $"Feature '{feature}' requires the Creator tier. Upgrade at $7/month per channel.");
    }
}
