namespace Vara.Api.Models.DTOs;

public record GenerateRequest(
    string TaskType,
    string Prompt,
    int? MaxTokens = null,
    double? Temperature = null);

public record GenerateResponse(
    string Content,
    int PromptTokens,
    int CompletionTokens,
    decimal CostUsd,
    string ProviderName,
    string ModelUsed,
    DateTime GeneratedAt);
