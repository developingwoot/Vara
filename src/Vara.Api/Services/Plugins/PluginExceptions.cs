namespace Vara.Api.Services.Plugins;

public sealed class PluginNotFoundException(string message) : Exception(message);
public sealed class PluginDisabledException(string message) : Exception(message);
