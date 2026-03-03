using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vara.Api.Data;
using Vara.Api.Models.Entities;
using Vara.Api.Plugins;
using Vara.Api.Services.Analysis;
using Vara.Api.Services.Llm;
using Vara.Api.Services.YouTube;

namespace Vara.Api.Services.Plugins;

public record PluginExecutionResult(object Result, Guid AnalysisId, bool FromCache);

public class PluginExecutionService(
    PluginRegistry registry,
    VaraContext db,
    IYouTubeClient youtube,
    ILlmOrchestrator llm,
    IPlanEnforcer planEnforcer,
    IUsageMeter usageMeter,
    ILogger<PluginExecutionService> logger)
{
    // Cache TTL: results from the same input within this window are returned as-is.
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

    public async Task<PluginExecutionResult> ExecuteAsync(
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

        var inputHash = ComputeHash(input.GetRawText());

        // Cache hit: return the most recent matching result within the TTL window.
        var cutoff = DateTime.UtcNow - CacheTtl;
        var cached = await db.PluginResults
            .Where(r => r.UserId == userId
                     && r.PluginId == pluginId
                     && r.InputHash == inputHash
                     && r.CreatedAt >= cutoff)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (cached is not null)
        {
            logger.LogInformation(
                "Cache hit for plugin {PluginId}, user {UserId}, analysis {AnalysisId}",
                pluginId, userId, cached.AnalysisId);

            var cachedResult = JsonSerializer.Deserialize<object>(cached.ResultDataJson)!;
            return new PluginExecutionResult(cachedResult, cached.AnalysisId, FromCache: true);
        }

        // Cache miss: enforce quota before the (potentially expensive) execution.
        if (metadata.UnitsPerRun.HasValue)
            await planEnforcer.EnforceUnitsAsync(userId, metadata.UnitsPerRun.Value, ct);

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
            InputHash      = inputHash,
            ResultDataJson = JsonSerializer.Serialize(result),
            CreatedAt      = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);

        // Record unit consumption after a successful execution.
        if (metadata.UnitsPerRun.HasValue)
            await usageMeter.RecordPluginRunAsync(userId, pluginId, metadata.UnitsPerRun.Value, ct);

        logger.LogInformation(
            "Plugin {PluginId} executed for user {UserId}, analysis {AnalysisId}",
            pluginId, userId, analysisId);

        return new PluginExecutionResult(result, analysisId, FromCache: false);
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }
}
