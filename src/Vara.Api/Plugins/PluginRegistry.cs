namespace Vara.Api.Plugins;

public class PluginRegistry
{
    private readonly Dictionary<string, IPlugin> _plugins = new();

    public void Register(IPlugin plugin) => _plugins[plugin.PluginId] = plugin;

    public IPlugin? Get(string pluginId) => _plugins.GetValueOrDefault(pluginId);

    public IReadOnlyCollection<string> PluginIds => _plugins.Keys;
}
