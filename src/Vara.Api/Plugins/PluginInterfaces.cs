using Vara.Api.Models.Entities;
using Vara.Api.Services.Llm;
using Vara.Api.Services.YouTube;

namespace Vara.Api.Plugins;

public interface IPlugin
{
    string PluginId { get; }
    Task<object> ExecuteAsync(IAnalysisContext context, object input, CancellationToken ct = default);
}

public interface IAnalysisContext
{
    Guid UserId { get; }
    Task<VideoMetadata?> GetVideoAsync(string youtubeId, CancellationToken ct = default);
    Task<List<VideoMetadata>> SearchVideosAsync(string keyword, int maxResults = 10, CancellationToken ct = default);
    Task<ChannelMetadata?> GetChannelAsync(string channelId, CancellationToken ct = default);
    Task<string?> GetTranscriptAsync(string videoId, CancellationToken ct = default);
    Task<LlmResponse> CallLlmAsync(string prompt, LlmExecutionContext executionContext, CancellationToken ct = default);
    Task<List<Keyword>> QueryKeywordsAsync(string niche, int limit = 100, CancellationToken ct = default);
    Task SaveResultAsync(Guid analysisId, string pluginId, object resultData, CancellationToken ct = default);
}

public record LlmExecutionContext
{
    public required Guid UserId { get; init; }
    public required string TaskType { get; init; }
}
