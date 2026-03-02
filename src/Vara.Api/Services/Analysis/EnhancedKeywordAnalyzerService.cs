using Vara.Api.Services.Llm;

namespace Vara.Api.Services.Analysis;

public interface IEnhancedKeywordAnalyzer
{
    Task<KeywordAnalysisResult> AnalyzeAsync(
        Guid userId,
        string keyword,
        string? niche = null,
        bool includeInsights = false,
        CancellationToken ct = default);
}

public class EnhancedKeywordAnalyzerService(
    IKeywordAnalyzer baseAnalyzer,
    ILlmOrchestrator llm,
    IPlanEnforcer planEnforcer,
    IUsageMeter usageMeter,
    ILogger<EnhancedKeywordAnalyzerService> logger) : IEnhancedKeywordAnalyzer
{
    public async Task<KeywordAnalysisResult> AnalyzeAsync(
        Guid userId,
        string keyword,
        string? niche = null,
        bool includeInsights = false,
        CancellationToken ct = default)
    {
        var analysis = await baseAnalyzer.AnalyzeAsync(keyword, niche, ct);

        if (!includeInsights)
            return analysis;

        await planEnforcer.EnforceAsync(userId, "llm_insights", ct);

        var prompt = PromptTemplates.KeywordInsights(analysis);
        var llmResponse = await llm.ExecuteAsync("KeywordInsights", prompt, ct: ct);

        await usageMeter.RecordLlmCallAsync(userId, "KeywordInsights", ct);

        logger.LogInformation(
            "LLM keyword insights generated for '{Keyword}' (user {UserId}), cost ${Cost:F4}",
            keyword, userId, llmResponse.CostUsd);

        return analysis with { LlmInsights = llmResponse.Content, LlmEnhanced = true };
    }
}
