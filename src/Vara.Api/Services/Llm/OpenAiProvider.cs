using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vara.Api.Services.Llm;

public class OpenAiProvider(
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    ILogger<OpenAiProvider> logger)
    : ILlmProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public string ProviderName => "OpenAI";

    private string ApiKey => config["Llm:Providers:OpenAi:ApiKey"]
        ?? throw new InvalidOperationException("Llm:Providers:OpenAi:ApiKey is not configured.");

    private string DefaultModel => config["Llm:Providers:OpenAi:DefaultModel"] ?? "gpt-4o-mini";

    public async Task<LlmResponse> GenerateAsync(
        string prompt,
        LlmOptions? options = null,
        CancellationToken ct = default)
    {
        var model = options?.Model ?? DefaultModel;
        var maxTokens = options?.MaxTokens ?? 1024;
        var temperature = options?.Temperature ?? 0.7;

        var requestBody = new
        {
            model,
            messages = new[] { new { role = "user", content = prompt } },
            max_tokens = maxTokens,
            temperature
        };

        var client = httpClientFactory.CreateClient("OpenAI");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {ApiKey}");

        var json = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        logger.LogInformation("OpenAI request: model={Model} maxTokens={MaxTokens}", model, maxTokens);

        var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content, ct);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<OpenAiChatResponse>(responseJson, JsonOptions)
            ?? throw new InvalidOperationException("OpenAI returned an empty response.");

        var text = result.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
        var promptTokens = result.Usage?.PromptTokens ?? 0;
        var completionTokens = result.Usage?.CompletionTokens ?? 0;
        var cost = LlmCostCalculator.Calculate(config, model, promptTokens, completionTokens);

        logger.LogInformation(
            "OpenAI response: model={Model} promptTokens={Input} completionTokens={Output} cost={Cost:C6}",
            model, promptTokens, completionTokens, cost);

        return new LlmResponse(text, promptTokens, completionTokens, cost, ProviderName, model, DateTime.UtcNow);
    }

    // -------------------------------------------------------------------------
    // Response shapes (OpenAI Chat Completions API)
    // -------------------------------------------------------------------------

    private sealed record OpenAiChatResponse(
        [property: JsonPropertyName("choices")] List<OpenAiChoice>? Choices,
        [property: JsonPropertyName("usage")]   OpenAiUsage? Usage);

    private sealed record OpenAiChoice(
        [property: JsonPropertyName("message")] OpenAiMessage? Message);

    private sealed record OpenAiMessage(
        [property: JsonPropertyName("content")] string? Content);

    private sealed record OpenAiUsage(
        [property: JsonPropertyName("prompt_tokens")]     int PromptTokens,
        [property: JsonPropertyName("completion_tokens")] int CompletionTokens);
}
