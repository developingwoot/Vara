using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vara.Api.Data;
using Vara.Api.Models.Entities;
using Vara.Api.Plugins;
using Vara.Api.Services.Analysis;
using Vara.Api.Services.Llm;
using Vara.Api.Services.YouTube;

namespace Vara.Api.Services.Plugins;

public class PluginExecutionService(
    PluginRegistry registry,
    VaraContext db,
    IYouTubeClient youtube,
    ILlmOrchestrator llm,
    IPlanEnforcer planEnforcer,
    IUsageMeter usageMeter,
    ILogger<PluginExecutionService> logger)
{
    public async Task<object> ExecuteAsync(
        string pluginId, Guid userId, JsonElement input, CancellationToken ct = default)
    {
        var metadata = await db.PluginMetadata
            .FirstOrDefaultAsync(p => p.PluginId == pluginId, ct)
            ?? throw new PluginNotFoundException($"Plugin '{pluginId}' not found");

        if (!metadata.Enabled)
            throw new PluginDisabledException($"Plugin '{pluginId}' is disabled");

        var user = await db.Users.FindAsync([userId], ct)
            ?? throw new InvalidOperationException($"User {userId} not found");

        if (metadata.Tier == "creator" && user.SubscriptionTier != "creator")
            throw new FeatureAccessDeniedException(
                $"Plugin '{metadata.Name}' requires the Creator tier.");

        var plugin = registry.Get(pluginId)
            ?? throw new PluginNotFoundException($"Plugin '{pluginId}' not registered");

        var context = new AnalysisContext(youtube, db, llm, planEnforcer, usageMeter)
            { UserId = userId };

        var analysisId = Guid.NewGuid();
        var result = await plugin.ExecuteAsync(context, input, ct);

        db.PluginResults.Add(new PluginResult
        {
            AnalysisId     = analysisId,
            PluginId       = pluginId,
            UserId         = userId,
            ResultDataJson = JsonSerializer.Serialize(result),
            CreatedAt      = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Plugin {PluginId} executed for user {UserId}, analysis {AnalysisId}",
            pluginId, userId, analysisId);

        return result;
    }
}
