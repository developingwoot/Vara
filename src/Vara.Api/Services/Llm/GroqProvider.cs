using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vara.Api.Services.Llm;

public class GroqProvider(
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    ILogger<GroqProvider> logger)
    : ILlmProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public string ProviderName => "Groq";

    private string ApiKey => config["Llm:Providers:Groq:ApiKey"]
        ?? throw new InvalidOperationException("Llm:Providers:Groq:ApiKey is not configured.");

    private string DefaultModel => config["Llm:Providers:Groq:DefaultModel"] ?? "llama-3.3-70b-versatile";

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

        var client = httpClientFactory.CreateClient("Groq");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {ApiKey}");

        var json = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        logger.LogInformation("Groq request: model={Model} maxTokens={MaxTokens}", model, maxTokens);

        var response = await client.PostAsync("https://api.groq.com/openai/v1/chat/completions", content, ct);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<GroqChatResponse>(responseJson, JsonOptions)
            ?? throw new InvalidOperationException("Groq returned an empty response.");

        var text = result.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
        var promptTokens = result.Usage?.PromptTokens ?? 0;
        var completionTokens = result.Usage?.CompletionTokens ?? 0;
        var cost = LlmCostCalculator.Calculate(config, model, promptTokens, completionTokens);

        logger.LogInformation(
            "Groq response: model={Model} promptTokens={Input} completionTokens={Output} cost={Cost:C6}",
            model, promptTokens, completionTokens, cost);

        return new LlmResponse(text, promptTokens, completionTokens, cost, ProviderName, model, DateTime.UtcNow);
    }

    // -------------------------------------------------------------------------
    // Response shapes (Groq uses OpenAI-compatible format)
    // -------------------------------------------------------------------------

    private sealed record GroqChatResponse(
        [property: JsonPropertyName("choices")] List<GroqChoice>? Choices,
        [property: JsonPropertyName("usage")]   GroqUsage? Usage);

    private sealed record GroqChoice(
        [property: JsonPropertyName("message")] GroqMessage? Message);

    private sealed record GroqMessage(
        [property: JsonPropertyName("content")] string? Content);

    private sealed record GroqUsage(
        [property: JsonPropertyName("prompt_tokens")]     int PromptTokens,
        [property: JsonPropertyName("completion_tokens")] int CompletionTokens);
}
