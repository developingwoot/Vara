using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vara.Api.Data;
using Vara.Api.Models.Entities;

namespace Vara.Api.Services.Plugins;

public class PluginDiscoveryService(VaraContext db, ILogger<PluginDiscoveryService> logger)
{
    public async Task DiscoverAsync(string pluginsDirectory, CancellationToken ct = default)
    {
        if (!Directory.Exists(pluginsDirectory))
        {
            logger.LogWarning("Plugins directory not found: {Dir}", pluginsDirectory);
            return;
        }

        var manifests = Directory.GetFiles(pluginsDirectory, "plugin.json", SearchOption.AllDirectories);
        foreach (var path in manifests)
        {
            try
            {
                var json = await File.ReadAllTextAsync(path, ct);
                var doc  = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var pluginId = root.GetProperty("id").GetString()!;
                ValidateManifest(pluginId, root);

                var existing = await db.PluginMetadata
                    .FirstOrDefaultAsync(p => p.PluginId == pluginId, ct);

                if (existing is null)
                {
                    db.PluginMetadata.Add(new PluginMetadata
                    {
                        PluginId        = pluginId,
                        Name            = root.GetProperty("name").GetString()!,
                        Version         = root.GetProperty("version").GetString()!,
                        Author          = root.GetProperty("author").GetString()!,
                        Description     = root.GetProperty("description").GetString()!,
                        Tier            = root.GetProperty("tier").GetString()!,
                        PluginDirectory = Path.GetDirectoryName(path)!,
                        UnitsPerRun     = root.TryGetProperty("quotaProfile", out var qp)
                            ? qp.GetProperty("unitsPerRun").GetInt32() : null,
                        Enabled         = true,
                        DiscoveredAt    = DateTime.UtcNow
                    });
                }
                else
                {
                    existing.Version   = root.GetProperty("version").GetString()!;
                    existing.UpdatedAt = DateTime.UtcNow;
                }

                logger.LogInformation("Discovered plugin: {PluginId}", pluginId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load plugin manifest at {Path}", path);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static void ValidateManifest(string pluginId, JsonElement root)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            throw new InvalidOperationException("Plugin 'id' is required");
        if (!root.TryGetProperty("name", out _))
            throw new InvalidOperationException($"Plugin '{pluginId}' missing 'name'");
        if (!root.TryGetProperty("tier", out _))
            throw new InvalidOperationException($"Plugin '{pluginId}' missing 'tier'");
    }
}
