using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vara.Api.Data;
using Vara.Api.Models.Entities;
using Vara.Api.Services.Analysis;
using Vara.Api.Services.Llm;
using Vara.Api.Services.YouTube;

namespace Vara.Api.Plugins;

public class AnalysisContext(
    IYouTubeClient youtube,
    VaraContext db,
    ILlmOrchestrator llm,
    IPlanEnforcer planEnforcer,
    IUsageMeter usageMeter) : IAnalysisContext
{
    public Guid UserId { get; set; }

    public Task<VideoMetadata?> GetVideoAsync(string youtubeId, CancellationToken ct = default)
        => youtube.GetVideoAsync(youtubeId, ct);

    public Task<List<VideoMetadata>> SearchVideosAsync(string keyword, int maxResults = 10, CancellationToken ct = default)
        => youtube.SearchAsync(keyword, maxResults, ct);

    public Task<ChannelMetadata?> GetChannelAsync(string channelId, CancellationToken ct = default)
        => youtube.GetChannelAsync(channelId, ct);

    public Task<string?> GetTranscriptAsync(string videoId, CancellationToken ct = default)
        => youtube.GetTranscriptAsync(videoId, ct);

    public async Task<LlmResponse> CallLlmAsync(string prompt, LlmExecutionContext executionContext, CancellationToken ct = default)
    {
        await planEnforcer.EnforceAsync(executionContext.UserId, "outlier_insights", ct);
        var response = await llm.ExecuteAsync(executionContext.TaskType, prompt, null, ct);
        await usageMeter.RecordLlmCallAsync(executionContext.UserId, executionContext.TaskType, ct);
        await usageMeter.RecordLlmCostAsync(
            executionContext.UserId, executionContext.TaskType,
            response.ProviderName, response.ModelUsed,
            response.PromptTokens, response.CompletionTokens, response.CostUsd, ct);
        return response;
    }

    public Task<List<Keyword>> QueryKeywordsAsync(string niche, int limit = 100, CancellationToken ct = default)
        => db.Keywords
             .Where(k => k.UserId == UserId && k.Niche == niche)
             .Take(limit)
             .ToListAsync(ct);

    public async Task SaveResultAsync(Guid analysisId, string pluginId, object resultData, CancellationToken ct = default)
    {
        db.PluginResults.Add(new PluginResult
        {
            AnalysisId     = analysisId,
            PluginId       = pluginId,
            UserId         = UserId,
            ResultDataJson = JsonSerializer.Serialize(resultData),
            CreatedAt      = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }
}
