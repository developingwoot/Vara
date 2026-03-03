using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vara.Api.Data;
using Vara.Api.Models.DTOs;
using Vara.Api.Plugins;
using Vara.Api.Services.Plugins;

namespace Vara.Api.Endpoints;

public static class PluginEndpoints
{
    public static RouteGroupBuilder MapPluginEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", ListPlugins)
            .WithTags("Plugins")
            .WithSummary("List all available plugins");

        group.MapGet("/{pluginId}", GetPlugin)
            .WithTags("Plugins")
            .WithSummary("Get plugin details");

        group.MapPost("/{pluginId}/execute", ExecutePlugin)
            .WithTags("Plugins")
            .WithSummary("Execute a plugin")
            .RequireRateLimiting("plugin-execute");

        group.MapGet("/results", GetResults)
            .WithTags("Plugins")
            .WithSummary("List recent plugin execution results for the authenticated user");

        group.MapPost("/discover", Discover)
            .WithTags("Plugins")
            .WithSummary("Re-scan plugin manifests (admin)");

        group.MapPost("/{pluginId}/enable", EnablePlugin)
            .WithTags("Plugins")
            .WithSummary("Enable a plugin (admin)");

        group.MapPost("/{pluginId}/disable", DisablePlugin)
            .WithTags("Plugins")
            .WithSummary("Disable a plugin (admin)");

        return group;
    }

    public static RouteGroupBuilder MapNicheEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/compare", CompareNiche)
            .WithTags("Niche Analysis")
            .WithSummary("Compare niche: trending keywords + outlier video opportunities");

        return group;
    }

    // -------------------------------------------------------------------------
    // GET /api/plugins
    // -------------------------------------------------------------------------

    private static async Task<IResult> ListPlugins(VaraContext db)
    {
        var plugins = await db.PluginMetadata
            .AsNoTracking()
            .Select(p => new PluginListItem(
                p.PluginId, p.Name, p.Version, p.Author,
                p.Description, p.Tier, p.Enabled, p.UnitsPerRun))
            .ToListAsync();

        return Results.Ok(plugins);
    }

    // -------------------------------------------------------------------------
    // GET /api/plugins/{pluginId}
    // -------------------------------------------------------------------------

    private static async Task<IResult> GetPlugin(string pluginId, VaraContext db)
    {
        var plugin = await db.PluginMetadata
            .FirstOrDefaultAsync(p => p.PluginId == pluginId);

        if (plugin is null)
            return Results.NotFound(new { message = $"Plugin '{pluginId}' not found" });

        return Results.Ok(new PluginListItem(
            plugin.PluginId, plugin.Name, plugin.Version, plugin.Author,
            plugin.Description, plugin.Tier, plugin.Enabled, plugin.UnitsPerRun));
    }

    // -------------------------------------------------------------------------
    // POST /api/plugins/{pluginId}/execute
    // -------------------------------------------------------------------------

    private static async Task<IResult> ExecutePlugin(
        string pluginId,
        JsonElement body,
        PluginExecutionService executionService,
        ClaimsPrincipal user)
    {
        var userId = Guid.Parse(user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var exec   = await executionService.ExecuteAsync(pluginId, userId, body);

        return Results.Ok(new ExecutePluginResponse(
            pluginId, exec.AnalysisId, exec.Result, DateTime.UtcNow, exec.FromCache));
    }

    // -------------------------------------------------------------------------
    // GET /api/plugins/results?pluginId=&limit=
    // -------------------------------------------------------------------------

    private static async Task<IResult> GetResults(
        ClaimsPrincipal user,
        VaraContext db,
        string? pluginId = null,
        int limit = 20)
    {
        var userId = Guid.Parse(user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier)!);
        limit = Math.Clamp(limit, 1, 100);

        var query = db.PluginResults
            .AsNoTracking()
            .Where(r => r.UserId == userId);

        if (!string.IsNullOrEmpty(pluginId))
            query = query.Where(r => r.PluginId == pluginId);

        var rows = await query
            .OrderByDescending(r => r.CreatedAt)
            .Take(limit)
            .ToListAsync();

        var results = rows.Select(r => new PluginResultSummary(
            r.AnalysisId,
            r.PluginId,
            JsonSerializer.Deserialize<object>(r.ResultDataJson)!,
            r.CreatedAt));

        return Results.Ok(results);
    }

    // -------------------------------------------------------------------------
    // POST /api/plugins/discover
    // -------------------------------------------------------------------------

    private static async Task<IResult> Discover(
        PluginDiscoveryService discoveryService,
        IConfiguration configuration,
        IWebHostEnvironment env)
    {
        var pluginsDir = Path.GetFullPath(
            Path.Combine(env.ContentRootPath,
                         configuration["Plugins:Directory"] ?? "../../plugins"));

        await discoveryService.DiscoverAsync(pluginsDir);
        return Results.Ok(new { message = "Plugin discovery complete", directory = pluginsDir });
    }

    // -------------------------------------------------------------------------
    // POST /api/plugins/{pluginId}/enable
    // -------------------------------------------------------------------------

    private static async Task<IResult> EnablePlugin(string pluginId, VaraContext db)
    {
        var plugin = await db.PluginMetadata
            .FirstOrDefaultAsync(p => p.PluginId == pluginId);

        if (plugin is null)
            return Results.NotFound(new { message = $"Plugin '{pluginId}' not found" });

        plugin.Enabled   = true;
        plugin.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(new { message = $"Plugin '{pluginId}' enabled" });
    }

    // -------------------------------------------------------------------------
    // POST /api/plugins/{pluginId}/disable
    // -------------------------------------------------------------------------

    private static async Task<IResult> DisablePlugin(string pluginId, VaraContext db)
    {
        var plugin = await db.PluginMetadata
            .FirstOrDefaultAsync(p => p.PluginId == pluginId);

        if (plugin is null)
            return Results.NotFound(new { message = $"Plugin '{pluginId}' not found" });

        plugin.Enabled   = false;
        plugin.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(new { message = $"Plugin '{pluginId}' disabled" });
    }

    // -------------------------------------------------------------------------
    // POST /api/analysis/niche/compare
    // -------------------------------------------------------------------------

    private static async Task<IResult> CompareNiche(
        NicheComparisonRequest req,
        INicheComparisonService nicheService,
        ClaimsPrincipal user)
    {
        var userId = Guid.Parse(user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await nicheService.CompareAsync(userId, req.Niche, req.IncludeInsights);

        return Results.Ok(new NicheComparisonResponse(
            result.Niche,
            result.TrendingKeywords,
            result.TopOutliers,
            result.GeneratedAt));
    }
}
