# VARA: Plugin & LLM Architecture
## Extensible System Design

---

## Part 1: Plugin Architecture

### Design Philosophy

Plugins are first-class citizens in VARA. Community members can extend functionality without modifying core code. The plugin system is designed for:

- **Ease of creation:** Simple manifest + entry point
- **Isolation:** Plugins can't break core functionality
- **Discoverability:** Plugins are listed and described
- **Composability:** Multiple plugins can work together on same analysis
- **Tier support:** Plugins can be free or premium

### Plugin Discovery Flow

```
┌─────────────────────────────┐
│  Application Startup        │
│  (Program.cs)               │
└──────────────┬──────────────┘
               │
      ┌────────▼─────────┐
      │ PluginDiscovery  │
      │ .DiscoverAsync() │
      └────────┬─────────┘
               │
     ┌─────────┴─────────┐
     │                   │
┌────▼────────┐  ┌──────▼──────┐
│ /plugins    │  │ GitHub Repo  │
│ directory   │  │ (future)     │
└────┬────────┘  └──────┬──────┘
     │                  │
     └────────┬─────────┘
              │
      ┌───────▼────────┐
      │ Load plugin.json│
      │ Validate       │
      │ Store in DB    │
      └────────┬───────┘
               │
      ┌────────▼─────────────────┐
      │ Plugin registry ready    │
      │ (available at runtime)   │
      └─────────────────────────┘
```

### Plugin Manifest (plugin.json)

Every plugin must have a `plugin.json` manifest in its root directory:

```json
{
  "id": "text-sentiment",
  "name": "Sentiment Analysis",
  "version": "1.0.0",
  "author": "VARA Community",
  "description": "Analyzes sentiment of video transcripts using Claude",
  "tier": "pro",
  "requiredLlmProviders": ["Anthropic"],
  "compatibleAnalysisTypes": ["video", "transcript"],
  "inputSchema": {
    "type": "object",
    "properties": {
      "videoIds": {
        "type": "array",
        "items": { "type": "string" },
        "description": "YouTube video IDs to analyze"
      },
      "sentimentModel": {
        "type": "string",
        "enum": ["basic", "detailed"],
        "description": "Sentiment analysis depth"
      }
    },
    "required": ["videoIds"]
  },
  "outputSchema": {
    "type": "object",
    "properties": {
      "overallSentiment": {
        "type": "string",
        "enum": ["very_negative", "negative", "neutral", "positive", "very_positive"],
        "description": "Overall tone of the video"
      },
      "sentimentScore": {
        "type": "number",
        "description": "Score from -1.0 to 1.0"
      },
      "keyEmotions": {
        "type": "array",
        "items": { "type": "string" },
        "description": "Emotions detected"
      },
      "recommendations": {
        "type": "array",
        "items": { "type": "string" },
        "description": "How to improve sentiment/engagement"
      }
    }
  },
  "uiComponent": "SentimentAnalysisCard",
  "entryPoint": "./dist/plugin.js",
  "dependencies": {
    "dotnet": "10.0.0"
  },
  "cost": {
    "llmCallsPerAnalysis": 1,
    "estimatedCostPerCall": 0.01,
    "freeTierLimit": 0
  }
}
```

### Plugin Interface (C#)

```csharp
public interface IPlugin
{
    /// <summary>
    /// Unique identifier for this plugin
    /// </summary>
    string PluginId { get; }
    
    /// <summary>
    /// Executes the plugin with given input
    /// </summary>
    /// <param name="context">Provides access to services (DB, LLM, YouTube, etc)</param>
    /// <param name="input">Plugin-specific input (validated against inputSchema)</param>
    /// <returns>Result object (validated against outputSchema)</returns>
    Task<object> ExecuteAsync(
        IAnalysisContext context,
        object input,
        CancellationToken ct = default);
}

public interface IAnalysisContext
{
    /// <summary>
    /// Current user ID
    /// </summary>
    Guid UserId { get; }
    
    /// <summary>
    /// Get video by YouTube ID (cached)
    /// </summary>
    Task<Video> GetVideoAsync(string youtubeId, CancellationToken ct = default);
    
    /// <summary>
    /// Get transcript (cached)
    /// </summary>
    Task<string> GetTranscriptAsync(string videoId, CancellationToken ct = default);
    
    /// <summary>
    /// Call LLM via the orchestrator. Execution context carries userId, task type,
    /// and (in Phase 3) an optional BYOT key. During MVP, byotApiKey is always null.
    /// </summary>
    Task<LlmResponse> CallLlmAsync(
        string prompt,
        LlmExecutionContext executionContext,
        CancellationToken ct = default);
    
    /// <summary>
    /// Query keywords from database
    /// </summary>
    Task<List<Keyword>> QueryKeywordsAsync(
        string niche,
        int limit = 100,
        CancellationToken ct = default);
    
    /// <summary>
    /// Store plugin result for future retrieval
    /// </summary>
    Task SaveResultAsync(
        Guid analysisId,
        string pluginId,
        object resultData,
        CancellationToken ct = default);
}
```

### Example Plugin: Sentiment Analysis

**Directory structure:**
```
plugins/sentiment-analysis/
├── plugin.json
├── Program.cs                # Entry point, DI setup
├── SentimentPlugin.cs        # IPlugin implementation
├── Models/
│   ├── SentimentRequest.cs
│   └── SentimentResponse.cs
└── Prompts/
    └── SentimentPrompt.cs
```

**SentimentPlugin.cs:**
```csharp
public class SentimentPlugin : IPlugin
{
    public string PluginId => "sentiment-analysis";
    
    private readonly ILogger<SentimentPlugin> _logger;
    
    public SentimentPlugin(ILogger<SentimentPlugin> logger)
    {
        _logger = logger;
    }
    
    public async Task<object> ExecuteAsync(
        IAnalysisContext context,
        object input,
        CancellationToken ct = default)
    {
        var request = JsonConvert.DeserializeObject<SentimentRequest>(
            input.ToString());
        
        var results = new List<SentimentResponse>();
        
        foreach (var videoId in request.VideoIds)
        {
            _logger.LogInformation($"Analyzing sentiment for {videoId}");
            
            // Get transcript
            var transcript = await context.GetTranscriptAsync(videoId, ct);
            
            // Analyze with LLM
            var sentiment = await context.CallLlmAsync(
                "SentimentAnalysis",
                SentimentPrompt.Generate(transcript),
                ct);
            
            // Parse response
            var response = ParseSentimentResponse(sentiment.Content);
            response.VideoId = videoId;
            
            results.Add(response);
        }
        
        return new { analyses = results, timestamp = DateTime.UtcNow };
    }
    
    private SentimentResponse ParseSentimentResponse(string llmOutput)
    {
        // Parse LLM JSON response
        return JsonConvert.DeserializeObject<SentimentResponse>(llmOutput);
    }
}
```

### Plugin Loading at Runtime

```csharp
public class PluginDiscoveryService
{
    private readonly ILogger<PluginDiscoveryService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IPluginMetadataRepository _pluginRepo;
    
    public async Task<List<PluginMetadata>> DiscoverAsync(
        string pluginDirectory)
    {
        var plugins = new List<PluginMetadata>();
        var manifestPaths = Directory.GetFiles(
            pluginDirectory,
            "plugin.json",
            SearchOption.AllDirectories);
        
        foreach (var manifestPath in manifestPaths)
        {
            try
            {
                var json = await File.ReadAllTextAsync(manifestPath);
                var metadata = JsonConvert.DeserializeObject<PluginMetadata>(json);
                
                // Validate manifest
                ValidateManifest(metadata);
                
                metadata.PluginDirectory = 
                    Path.GetDirectoryName(manifestPath);
                
                // Store in database
                await _pluginRepo.UpsertAsync(metadata);
                
                plugins.Add(metadata);
                _logger.LogInformation(
                    $"Discovered plugin: {metadata.PluginId} v{metadata.Version}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    $"Failed to load plugin from {manifestPath}");
            }
        }
        
        return plugins;
    }
    
    public async Task<object> ExecutePluginAsync(
        string pluginId,
        object input,
        Guid userId,
        CancellationToken ct = default)
    {
        var metadata = await _pluginRepo.GetByIdAsync(pluginId);
        
        if (metadata == null)
            throw new PluginNotFoundException($"Plugin {pluginId} not found");
        
        if (!metadata.Enabled)
            throw new PluginDisabledException($"Plugin {pluginId} is disabled");
        
        // Load plugin assembly
        var assemblyPath = Path.Combine(
            metadata.PluginDirectory,
            "bin/Release/net10.0");
        
        var assembly = AssemblyLoadContext.Default.LoadFromAssemblyName(
            new AssemblyName(Path.GetFileNameWithoutExtension(
                Directory.GetFiles(assemblyPath, "*.dll")[0])));
        
        // Create plugin instance
        var pluginType = assembly.GetTypes()
            .FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t));
        
        if (pluginType == null)
            throw new PluginLoadException(
                $"No IPlugin implementation found in {pluginId}");
        
        var plugin = ActivatorUtilities.CreateInstance(
            _serviceProvider, pluginType) as IPlugin;
        
        // Execute with context
        var context = new AnalysisContext(_serviceProvider, userId);
        var result = await plugin.ExecuteAsync(context, input, ct);
        
        return result;
    }
    
    private void ValidateManifest(PluginMetadata metadata)
    {
        if (string.IsNullOrEmpty(metadata.PluginId))
            throw new InvalidPluginException("Plugin ID is required");
        
        if (string.IsNullOrEmpty(metadata.Name))
            throw new InvalidPluginException("Plugin name is required");
        
        if (metadata.InputSchema == null)
            throw new InvalidPluginException("Input schema is required");
        
        if (metadata.OutputSchema == null)
            throw new InvalidPluginException("Output schema is required");
    }
}
```

### Plugin Tier Integration

```csharp
// PluginAccessService.cs
public class PluginAccessService
{
    public async Task<bool> CanAccessPluginAsync(
        Guid userId,
        string pluginId)
    {
        var user = await _userRepo.GetAsync(userId);
        var plugin = await _pluginRepo.GetByIdAsync(pluginId);
        
        return plugin.Tier switch
        {
            "free" => true,
            "pro" => user.SubscriptionTier is "pro" or "enterprise",
            "enterprise" => user.SubscriptionTier == "enterprise",
            _ => false
        };
    }
    
    public async Task<List<PluginMetadata>> GetAccessiblePluginsAsync(
        Guid userId)
    {
        var user = await _userRepo.GetAsync(userId);
        var allPlugins = await _pluginRepo.GetAllAsync();
        
        return allPlugins
            .Where(p => p.Tier switch
            {
                "free" => true,
                "pro" => user.SubscriptionTier is "pro" or "enterprise",
                "enterprise" => user.SubscriptionTier == "enterprise",
                _ => false
            })
            .Where(p => p.Enabled)
            .ToList();
    }
}
```

### Plugin Results Storage

```csharp
// Store plugin results for retrieval
public class PluginResultService
{
    public async Task SaveResultAsync(
        Guid analysisId,
        string pluginId,
        object resultData)
    {
        var result = new PluginResult
        {
            AnalysisId = analysisId,
            PluginId = pluginId,
            ResultData = JObject.FromObject(resultData),
            CreatedAt = DateTime.UtcNow
        };
        
        await _db.PluginResults.AddAsync(result);
        await _db.SaveChangesAsync();
    }
    
    public async Task<List<PluginResult>> GetResultsForAnalysisAsync(
        Guid analysisId)
    {
        return await _db.PluginResults
            .Where(pr => pr.AnalysisId == analysisId)
            .ToListAsync();
    }
}
```

---

## Part 2: LLM Architecture

### Provider Abstraction

The LLM layer is designed so that adding new providers requires minimal code change.

```csharp
// ILlmProvider interface
public interface ILlmProvider
{
    string ProviderName { get; }
    string DefaultModel { get; }
    
    Task<LlmResponse> GenerateAsync(
        string prompt,
        LlmOptions options = null,
        CancellationToken ct = default);
    
    Task<decimal> EstimateCostAsync(
        string prompt,
        string model = null);
}

public class LlmResponse
{
    public string Content { get; set; }
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public decimal CostUsd { get; set; }
    public TimeSpan Latency { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class LlmOptions
{
    public string Model { get; set; }
    public int? MaxTokens { get; set; }
    public double? Temperature { get; set; }
    public Dictionary<string, object> Extra { get; set; }
}
```

### Provider Factory (BYOT Seam)

```csharp
/// <summary>
/// Creates ILlmProvider instances with either VARA-managed or user-supplied API keys.
/// During MVP, only CreateManaged() is called. Phase 3 adds CreateWithUserKey().
/// </summary>
public interface ILlmProviderFactory
{
    /// <summary>
    /// Returns a provider using VARA's managed API key for the given provider name.
    /// Used for all managed (non-BYOT) inference.
    /// </summary>
    ILlmProvider CreateManaged(string providerName);

    /// <summary>
    /// Returns a provider using the caller-supplied API key.
    /// Used for BYOT inference in Phase 3. Not implemented during MVP.
    /// </summary>
    ILlmProvider CreateWithUserKey(string providerName, string apiKey);
}

// MVP implementation — CreateWithUserKey throws NotImplementedException
// Phase 3 replaces the throw with real instantiation logic
public class LlmProviderFactory : ILlmProviderFactory
{
    private readonly Dictionary<string, ILlmProvider> _managedProviders;
    private readonly IConfiguration _config;

    public LlmProviderFactory(
        IEnumerable<ILlmProvider> managedProviders,
        IConfiguration config)
    {
        _managedProviders = managedProviders.ToDictionary(p => p.ProviderName);
        _config = config;
    }

    public ILlmProvider CreateManaged(string providerName)
    {
        if (!_managedProviders.TryGetValue(providerName, out var provider))
            throw new InvalidOperationException($"No managed provider registered: {providerName}");
        return provider;
    }

    public ILlmProvider CreateWithUserKey(string providerName, string apiKey)
    {
        // Phase 3: instantiate provider with user-supplied key
        // MVP: not implemented
        throw new NotImplementedException(
            "BYOT provider instantiation is implemented in Phase 3.");
    }
}
```

### Provider Selection Logic

```csharp
public class LlmOrchestratorConfiguration
{
    public Dictionary<string, string> TaskProviderMapping { get; set; }
    
    // Example configuration:
    // {
    //   "KeywordInsights": "Anthropic",
    //   "QuickSummary": "Groq",
    //   "StrategicAdvice": "Anthropic",
    //   "TranscriptAnalysis": "Anthropic",
    //   "Default": "OpenAi"
    // }
}

/// <summary>
/// Passed to LlmOrchestrator.ExecuteAsync for every LLM call.
/// The byotApiKey field is null during MVP and populated in Phase 3 when
/// the user has configured their own key.
/// </summary>
public class LlmExecutionContext
{
    public Guid UserId { get; init; }
    public string TaskType { get; init; }
    public string? ByotApiKey { get; init; }    // null = use VARA managed key
    public string? ByotProvider { get; init; }  // null = use task routing config
}

public class LlmOrchestrator
{
    private readonly ILlmProviderFactory _providerFactory;
    private readonly LlmOrchestratorConfiguration _config;
    private readonly ILogger<LlmOrchestrator> _logger;
    private readonly ILlmCostRepository _costRepo;
    private readonly ILlmCacheService _cache;

    public LlmOrchestrator(
        ILlmProviderFactory providerFactory,
        LlmOrchestratorConfiguration config,
        ILogger<LlmOrchestrator> logger,
        ILlmCostRepository costRepo,
        ILlmCacheService cache)
    {
        _providerFactory = providerFactory;
        _config = config;
        _logger = logger;
        _costRepo = costRepo;
        _cache = cache;
    }

    public async Task<LlmResponse> ExecuteAsync(
        string prompt,
        LlmExecutionContext context,
        LlmOptions options = null,
        CancellationToken ct = default)
    {
        // Check cache first (shared across all users for same input)
        var cacheKey = BuildLlmCacheKey(context.TaskType, prompt);
        var cached = await _cache.GetAsync(cacheKey);
        if (cached != null)
        {
            _logger.LogInformation("LLM cache hit for task {TaskType}", context.TaskType);
            return cached;
        }

        // MVP: ByotApiKey is always null — CreateWithUserKey is never called
        // Phase 3: if ByotApiKey is set, use CreateWithUserKey instead
        var providerName = context.ByotProvider
            ?? SelectProvider(context.TaskType);

        var provider = context.ByotApiKey is not null
            ? _providerFactory.CreateWithUserKey(providerName, context.ByotApiKey)
            : _providerFactory.CreateManaged(providerName);

        _logger.LogInformation(
            "Executing task {TaskType} via {Provider} (byot={IsByot})",
            context.TaskType, providerName, context.ByotApiKey is not null);

        try
        {
            var response = await provider.GenerateAsync(prompt, options, ct);

            await LogCostAsync(context, providerName, response,
                isByot: context.ByotApiKey is not null);

            await _cache.SetAsync(cacheKey, response, CacheTtls.KeywordInsights);

            return response;
        }
        catch (RateLimitException)
        {
            _logger.LogWarning("Rate limited by {Provider}, attempting fallback", providerName);
            return await FallbackExecuteAsync(prompt, context, providerName, options, ct);
        }
    }

    private string BuildLlmCacheKey(string taskType, string prompt)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(prompt));
        return $"llm:{taskType}:{Convert.ToHexString(hash)[..16]}";
    }

    private string SelectProvider(string taskType) =>
        _config.TaskProviderMapping.GetValueOrDefault(taskType,
            _config.TaskProviderMapping["Default"]);

    private async Task LogCostAsync(
        LlmExecutionContext context,
        string providerName,
        LlmResponse response,
        bool isByot)
    {
        await _costRepo.AddAsync(new LlmCost
        {
            UserId = context.UserId,
            TaskType = context.TaskType,
            Provider = providerName,
            PromptTokens = response.PromptTokens,
            CompletionTokens = response.CompletionTokens,
            CostUsd = isByot ? 0m : response.CostUsd, // VARA cost is $0 for BYOT calls
            IsByot = isByot,
            CreatedAt = DateTime.UtcNow
        });
    }

    private async Task<LlmResponse> FallbackExecuteAsync(
        string prompt,
        LlmExecutionContext context,
        string failedProvider,
        LlmOptions options,
        CancellationToken ct)
    {
        // Never fall back to a managed provider if user supplied their own key
        // If their key fails, surface the error directly
        if (context.ByotApiKey is not null)
            throw new LlmException(
                $"Your API key for {failedProvider} failed. Please check your key in Settings.");

        var fallbackOrder = new[] { "Anthropic", "OpenAi", "Groq" }
            .Where(p => p != failedProvider)
            .ToList();

        foreach (var name in fallbackOrder)
        {
            try
            {
                var provider = _providerFactory.CreateManaged(name);
                var response = await provider.GenerateAsync(prompt, options, ct);
                _logger.LogInformation("Fallback successful: {From} → {To}",
                    failedProvider, name);
                await LogCostAsync(context, name, response, isByot: false);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fallback to {Provider} failed", name);
            }
        }

        throw new LlmException("All LLM providers failed.");
    }
}
```

### Prompt Template System

```csharp
public static class PromptTemplates
{
    // Each template includes system prompt + user prompt pattern
    
    public static (string system, string user) KeywordInsights(
        KeywordAnalysis analysis)
    {
        var system = @"You are a YouTube strategy expert with 10 years of experience.
Your role is to provide actionable, specific insights that help creators grow their channels.
Be direct and avoid generic advice.";
        
        var user = $@"Analyze this keyword research data:

Keyword: {analysis.Keyword}
Search Volume: {analysis.SearchVolume}/100
Competition: {analysis.CompetitionScore}/100
Trend: {analysis.TrendDirection}
Intent: {analysis.KeywordIntent}

Provide exactly:
1. Why this keyword matters (1-2 sentences)
2. Positioning strategy to stand out (2-3 specific tactics)
3. Content gap you can exploit (1 specific idea)
4. 3 unique video angles

Be specific. Avoid generic advice.";
        
        return (system, user);
    }
    
    public static (string system, string user) TranscriptAnalysis(
        string transcript,
        bool detailed = false)
    {
        var system = @"You are a content analyst specializing in YouTube videos.
Analyze transcripts objectively and identify actionable insights.";
        
        var user = detailed ?
            $@"Provide deep analysis of this transcript:

{transcript}

Identify:
1. Core topics (3-5)
2. Key takeaways for viewers
3. Engagement hooks (moments that grab attention)
4. Gaps in coverage
5. Unique positioning vs competitors

Format as JSON with keys: topics, takeaways, hooks, gaps, positioning" :
            
            $@"Quick analysis of this transcript:

{transcript}

Summarize: main topic, key message, target audience";
        
        return (system, user);
    }
}
```

### Cost Tracking & Reporting

```csharp
public class LlmCostAnalyticsService
{
    public async Task<LlmCostSummary> GetUserCostSummaryAsync(
        Guid userId,
        DateRange period)
    {
        var costs = await _costRepo.GetByUserAsync(userId, period);
        
        return new LlmCostSummary
        {
            TotalCost = costs.Sum(c => c.CostUsd),
            TotalTokens = costs.Sum(c => c.PromptTokens + c.CompletionTokens),
            CostByProvider = costs
                .GroupBy(c => c.Provider)
                .ToDictionary(
                    g => g.Key,
                    g => new ProviderCost
                    {
                        TotalCost = g.Sum(c => c.CostUsd),
                        CallCount = g.Count(),
                        AvgCostPerCall = g.Average(c => c.CostUsd)
                    }),
            CostByTask = costs
                .GroupBy(c => c.TaskType)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(c => c.CostUsd)),
            AvgLatency = TimeSpan.FromMilliseconds(
                costs.Average(c => c.Latency.TotalMilliseconds))
        };
    }
}

public class LlmCostSummary
{
    public decimal TotalCost { get; set; }
    public int TotalTokens { get; set; }
    public Dictionary<string, ProviderCost> CostByProvider { get; set; }
    public Dictionary<string, decimal> CostByTask { get; set; }
    public TimeSpan AvgLatency { get; set; }
}
```

### LLM as a Service (Dependency Injection)

```csharp
// In Program.cs
builder.Services.AddSingleton<ILlmProvider>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new OpenAiProvider(
        config["Llm:Providers:OpenAi:ApiKey"],
        new HttpClientFactory().CreateClient());
});

builder.Services.AddSingleton<ILlmProvider>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new AnthropicProvider(
        config["Llm:Providers:Anthropic:ApiKey"],
        new HttpClientFactory().CreateClient());
});

builder.Services.AddSingleton<LlmOrchestrator>(sp =>
{
    var providers = sp.GetServices<ILlmProvider>()
        .ToDictionary(p => p.ProviderName);
    var config = sp.GetRequiredService<IConfiguration>()
        .GetSection("Llm").Get<LlmOrchestratorConfiguration>();
    
    return new LlmOrchestrator(providers, config);
});

// Any service can now inject LlmOrchestrator
public class KeywordAnalyzerService
{
    public KeywordAnalyzerService(LlmOrchestrator llmOrchestrator)
    {
        _llmOrchestrator = llmOrchestrator;
    }
}
```

---

## Part 3: Plugin + LLM Integration

### Example: Sentiment Plugin Using LLM

```csharp
public class SentimentPlugin : IPlugin
{
    public string PluginId => "sentiment-analysis";
    
    private readonly ILogger<SentimentPlugin> _logger;
    
    public async Task<object> ExecuteAsync(
        IAnalysisContext context,
        object input,
        CancellationToken ct = default)
    {
        var request = input as SentimentRequest;
        var results = new List<object>();
        
        foreach (var videoId in request.VideoIds)
        {
            // Get transcript
            var transcript = await context.GetTranscriptAsync(videoId, ct);
            
            // Use LLM orchestrator (via context)
            var sentimentResult = await context.CallLlmAsync(
                "SentimentAnalysis",
                PromptTemplates.AnalyzeSentiment(transcript),
                ct);
            
            results.Add(new
            {
                videoId,
                sentiment = sentimentResult.Content,
                llmProvider = sentimentResult.Provider,
                cost = sentimentResult.CostUsd
            });
        }
        
        return new { analyses = results };
    }
}
```

### Niche Comparison Using Plugins

```csharp
public class NicheComparisonService
{
    private readonly PluginDiscoveryService _pluginDiscovery;
    
    public async Task<NicheComparisonResult> CompareAsync(
        Guid userId,
        string primaryNiche,
        string[] comparisonNiches)
    {
        var result = new NicheComparisonResult();
        
        foreach (var niche in new[] { primaryNiche }.Concat(comparisonNiches))
        {
            var profile = new NicheProfile { Niche = niche };
            
            // Base analysis
            profile.TopKeywords = await _keywordService.GetTopAsync(niche);
            profile.VideoPatterns = await _videoAnalyzer.AnalyzeNicheAsync(niche);
            
            // Run all enabled plugins for this niche
            var plugins = await _pluginDiscovery.GetAccessiblePluginsAsync(userId);
            var nichePlugins = plugins
                .Where(p => p.CompatibleAnalysisTypes.Contains("niche"))
                .ToList();
            
            profile.PluginResults = new Dictionary<string, object>();
            
            foreach (var plugin in nichePlugins)
            {
                try
                {
                    var pluginResult = await _pluginDiscovery.ExecutePluginAsync(
                        plugin.PluginId,
                        new { niche },
                        userId);
                    
                    profile.PluginResults[plugin.PluginId] = pluginResult;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        $"Plugin {plugin.PluginId} failed for niche {niche}");
                }
            }
            
            result.NicheProfiles.Add(profile);
        }
        
        // Perform gap analysis using plugin results
        result.GapAnalysis = PerformGapAnalysis(result.NicheProfiles);
        
        return result;
    }
}
```

---

## Summary

This architecture enables:

✅ **Extensibility:** Community builds plugins without touching core code  
✅ **Flexibility:** Choose best LLM per task (cost, quality, speed)  
✅ **Observability:** Track every LLM call and cost  
✅ **Reliability:** Fallback providers if one fails  
✅ **Composability:** Plugins can call LLMs intelligently  
✅ **Tierability:** Different plugins for different subscription tiers
