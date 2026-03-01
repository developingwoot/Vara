namespace Vara.Api.Services.Llm;

public class LlmOrchestrator(
    IEnumerable<ILlmProvider> providers,
    IConfiguration config,
    ILogger<LlmOrchestrator> logger)
    : ILlmOrchestrator
{
    private readonly Dictionary<string, ILlmProvider> _providers =
        providers.ToDictionary(p => p.ProviderName, StringComparer.OrdinalIgnoreCase);

    public async Task<LlmResponse> ExecuteAsync(
        string taskType,
        string prompt,
        LlmOptions? options = null,
        CancellationToken ct = default)
    {
        if (_providers.Count == 0)
            throw new LlmException("No LLM providers are configured. Add at least one provider API key.");

        var defaultProviderName = config["Llm:DefaultProvider"] ?? "Anthropic";
        var mappedProviderName  = config[$"Llm:TaskProviderMapping:{taskType}"] ?? defaultProviderName;

        // Try the mapped provider, fall back to default
        var preferred = _providers.GetValueOrDefault(mappedProviderName);
        var fallback  = mappedProviderName != defaultProviderName
            ? _providers.GetValueOrDefault(defaultProviderName)
            : null;

        // If preferred is not registered, use the default directly
        if (preferred is null)
        {
            logger.LogWarning(
                "Provider '{Mapped}' not registered for task '{TaskType}', falling back to '{Default}'",
                mappedProviderName, taskType, defaultProviderName);
            preferred = _providers.GetValueOrDefault(defaultProviderName);
            fallback = null;
        }

        if (preferred is null)
            throw new LlmException($"No configured provider available. Wanted '{mappedProviderName}' or '{defaultProviderName}'.");

        // Attempt preferred provider
        try
        {
            logger.LogInformation("LLM task={TaskType} → provider={Provider}", taskType, preferred.ProviderName);
            return await preferred.GenerateAsync(prompt, options, ct);
        }
        catch (Exception ex) when (fallback is not null)
        {
            logger.LogWarning(ex,
                "Provider '{Provider}' failed for task '{TaskType}', falling back to '{Fallback}'",
                preferred.ProviderName, taskType, fallback.ProviderName);
        }

        // Attempt fallback provider
        try
        {
            logger.LogInformation("LLM fallback task={TaskType} → provider={Provider}", taskType, fallback!.ProviderName);
            return await fallback.GenerateAsync(prompt, options, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "All LLM providers failed for task '{TaskType}'", taskType);
            throw new LlmException($"All configured LLM providers failed for task '{taskType}'.");
        }
    }
}
