using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vara.Api.Services.Llm;

public class AnthropicProvider(
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    ILogger<AnthropicProvider> logger)
    : ILlmProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public string ProviderName => "Anthropic";

    private string ApiKey => config["Llm:Providers:Anthropic:ApiKey"]
        ?? throw new InvalidOperationException("Llm:Providers:Anthropic:ApiKey is not configured.");

    private string DefaultModel => config["Llm:Providers:Anthropic:DefaultModel"] ?? "claude-sonnet-4-6";

    public async Task<LlmResponse> GenerateAsync(
        string prompt,
        LlmOptions? options = null,
        CancellationToken ct = default)
    {
        var model = options?.Model ?? DefaultModel;
        var maxTokens = options?.MaxTokens ?? 1024;

        var requestBody = new
        {
            model,
            max_tokens = maxTokens,
            messages = new[] { new { role = "user", content = prompt } }
        };

        var client = httpClientFactory.CreateClient("Anthropic");
        client.DefaultRequestHeaders.TryAddWithoutValidation("x-api-key", ApiKey);
        client.DefaultRequestHeaders.TryAddWithoutValidation("anthropic-version", "2023-06-01");

        var json = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        logger.LogInformation("Anthropic request: model={Model} maxTokens={MaxTokens}", model, maxTokens);

        var response = await client.PostAsync("https://api.anthropic.com/v1/messages", content, ct);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<AnthropicMessageResponse>(responseJson, JsonOptions)
            ?? throw new InvalidOperationException("Anthropic returned an empty response.");

        var text = result.Content?.FirstOrDefault()?.Text ?? string.Empty;
        var inputTokens = result.Usage?.InputTokens ?? 0;
        var outputTokens = result.Usage?.OutputTokens ?? 0;
        var cost = LlmCostCalculator.Calculate(config, model, inputTokens, outputTokens);

        logger.LogInformation(
            "Anthropic response: model={Model} inputTokens={Input} outputTokens={Output} cost={Cost:C6}",
            model, inputTokens, outputTokens, cost);

        return new LlmResponse(text, inputTokens, outputTokens, cost, ProviderName, model, DateTime.UtcNow);
    }

    // -------------------------------------------------------------------------
    // Response shapes (Anthropic Messages API)
    // -------------------------------------------------------------------------

    private sealed record AnthropicMessageResponse(
        [property: JsonPropertyName("content")] List<AnthropicContentBlock>? Content,
        [property: JsonPropertyName("usage")] AnthropicUsage? Usage);

    private sealed record AnthropicContentBlock(
        [property: JsonPropertyName("text")] string? Text);

    private sealed record AnthropicUsage(
        [property: JsonPropertyName("input_tokens")]  int InputTokens,
        [property: JsonPropertyName("output_tokens")] int OutputTokens);
}
