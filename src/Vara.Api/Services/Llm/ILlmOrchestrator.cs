namespace Vara.Api.Services.Llm;

public interface ILlmOrchestrator
{
    Task<LlmResponse> ExecuteAsync(
        string taskType,
        string prompt,
        LlmOptions? options = null,
        CancellationToken ct = default);
}

public sealed class LlmException(string message) : Exception(message);
