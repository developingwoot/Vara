namespace Vara.Api.Services.Llm;

public interface ILlmProvider
{
    string ProviderName { get; }

    Task<LlmResponse> GenerateAsync(
        string prompt,
        LlmOptions? options = null,
        CancellationToken ct = default);
}

public record LlmResponse(
    string Content,
    int PromptTokens,
    int CompletionTokens,
    decimal CostUsd,
    string ProviderName,
    string ModelUsed,
    DateTime GeneratedAt);

public record LlmOptions(
    string? Model = null,
    int? MaxTokens = null,
    double? Temperature = null);
