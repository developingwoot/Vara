using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Vara.Api.Services.Llm;

namespace Vara.Tests.Services.Llm;

public class LlmOrchestratorTests
{
    private static readonly LlmResponse FakeResponse = new(
        Content: "test response",
        PromptTokens: 10,
        CompletionTokens: 20,
        CostUsd: 0.001m,
        ProviderName: "Anthropic",
        ModelUsed: "claude-sonnet-4-6",
        GeneratedAt: DateTime.UtcNow);

    private static IConfiguration BuildConfig(
        string defaultProvider = "Anthropic",
        Dictionary<string, string>? taskMapping = null)
    {
        var dict = new Dictionary<string, string?>
        {
            ["Llm:DefaultProvider"] = defaultProvider
        };

        if (taskMapping is not null)
            foreach (var (task, provider) in taskMapping)
                dict[$"Llm:TaskProviderMapping:{task}"] = provider;

        // Pricing for cost calculator tests
        dict["Llm:Pricing:claude-sonnet-4-6:InputPerMToken"]  = "3.00";
        dict["Llm:Pricing:claude-sonnet-4-6:OutputPerMToken"] = "15.00";

        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    private static ILlmProvider MakeProvider(string name, LlmResponse? response = null)
    {
        var provider = Substitute.For<ILlmProvider>();
        provider.ProviderName.Returns(name);
        provider.GenerateAsync(Arg.Any<string>(), Arg.Any<LlmOptions?>(), Arg.Any<CancellationToken>())
                .Returns(response ?? FakeResponse);
        return provider;
    }

    private static LlmOrchestrator BuildOrchestrator(
        IConfiguration config,
        params ILlmProvider[] providers) =>
        new(providers, config, NullLogger<LlmOrchestrator>.Instance);

    // -------------------------------------------------------------------------

    [Fact]
    public async Task ExecuteAsync_MappedTaskType_SelectsCorrectProvider()
    {
        var anthropic = MakeProvider("Anthropic");
        var openai    = MakeProvider("OpenAI");
        var config = BuildConfig(taskMapping: new() { ["KeywordInsights"] = "Anthropic" });
        var orchestrator = BuildOrchestrator(config, anthropic, openai);

        await orchestrator.ExecuteAsync("KeywordInsights", "test prompt");

        await anthropic.Received(1).GenerateAsync(Arg.Any<string>(), Arg.Any<LlmOptions?>(), Arg.Any<CancellationToken>());
        await openai.DidNotReceive().GenerateAsync(Arg.Any<string>(), Arg.Any<LlmOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_UnmappedTaskType_UsesDefaultProvider()
    {
        var anthropic = MakeProvider("Anthropic");
        var config = BuildConfig(defaultProvider: "Anthropic");
        var orchestrator = BuildOrchestrator(config, anthropic);

        await orchestrator.ExecuteAsync("UnknownTask", "prompt");

        await anthropic.Received(1).GenerateAsync(Arg.Any<string>(), Arg.Any<LlmOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_FallsBackToDefault_WhenMappedProviderThrows()
    {
        var anthropic = MakeProvider("Anthropic");
        var openai    = MakeProvider("OpenAI");
        openai.GenerateAsync(Arg.Any<string>(), Arg.Any<LlmOptions?>(), Arg.Any<CancellationToken>())
              .ThrowsAsync(new HttpRequestException("OpenAI unavailable"));

        var config = BuildConfig(
            defaultProvider: "Anthropic",
            taskMapping: new() { ["QuickSummary"] = "OpenAI" });
        var orchestrator = BuildOrchestrator(config, anthropic, openai);

        var result = await orchestrator.ExecuteAsync("QuickSummary", "prompt");

        Assert.Equal("Anthropic", result.ProviderName);
        await anthropic.Received(1).GenerateAsync(Arg.Any<string>(), Arg.Any<LlmOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsLlmException_WhenAllProvidersFail()
    {
        var anthropic = MakeProvider("Anthropic");
        var openai    = MakeProvider("OpenAI");
        anthropic.GenerateAsync(Arg.Any<string>(), Arg.Any<LlmOptions?>(), Arg.Any<CancellationToken>())
                 .ThrowsAsync(new HttpRequestException("Anthropic down"));
        openai.GenerateAsync(Arg.Any<string>(), Arg.Any<LlmOptions?>(), Arg.Any<CancellationToken>())
              .ThrowsAsync(new HttpRequestException("OpenAI down"));

        var config = BuildConfig(
            defaultProvider: "Anthropic",
            taskMapping: new() { ["QuickSummary"] = "OpenAI" });
        var orchestrator = BuildOrchestrator(config, anthropic, openai);

        await Assert.ThrowsAsync<LlmException>(() =>
            orchestrator.ExecuteAsync("QuickSummary", "prompt"));
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsLlmException_WhenNoProvidersRegistered()
    {
        var config = BuildConfig();
        var orchestrator = BuildOrchestrator(config); // no providers

        await Assert.ThrowsAsync<LlmException>(() =>
            orchestrator.ExecuteAsync("KeywordInsights", "prompt"));
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsCorrectContent_FromProvider()
    {
        var expected = FakeResponse with { Content = "specific content" };
        var anthropic = MakeProvider("Anthropic", expected);
        var config = BuildConfig();
        var orchestrator = BuildOrchestrator(config, anthropic);

        var result = await orchestrator.ExecuteAsync("KeywordInsights", "prompt");

        Assert.Equal("specific content", result.Content);
    }

    [Fact]
    public async Task ExecuteAsync_UsesDefaultProvider_WhenMappedProviderNotRegistered()
    {
        // Config maps QuickSummary → "Groq", but Groq is not registered
        var anthropic = MakeProvider("Anthropic");
        var config = BuildConfig(
            defaultProvider: "Anthropic",
            taskMapping: new() { ["QuickSummary"] = "Groq" });
        var orchestrator = BuildOrchestrator(config, anthropic);

        var result = await orchestrator.ExecuteAsync("QuickSummary", "prompt");

        Assert.Equal("Anthropic", result.ProviderName);
        await anthropic.Received(1).GenerateAsync(Arg.Any<string>(), Arg.Any<LlmOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_PassesOptionsToProvider()
    {
        var anthropic = MakeProvider("Anthropic");
        var config = BuildConfig();
        var orchestrator = BuildOrchestrator(config, anthropic);
        var options = new LlmOptions(MaxTokens: 500, Temperature: 0.2);

        await orchestrator.ExecuteAsync("KeywordInsights", "prompt", options);

        await anthropic.Received(1).GenerateAsync(
            "prompt",
            Arg.Is<LlmOptions?>(o => o != null && o.MaxTokens == 500 && o.Temperature == 0.2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void CostCalculator_CalculatesCorrectly_ForKnownModel()
    {
        // claude-sonnet-4-6: input=$3/MTok, output=$15/MTok
        // 1000 input + 500 output = 0.003 + 0.0075 = 0.0105
        var config = BuildConfig();

        var cost = LlmCostCalculator.Calculate(config, "claude-sonnet-4-6", 1000, 500);

        Assert.Equal(0.0105m, cost);
    }

    [Fact]
    public void CostCalculator_ReturnsZero_ForUnknownModel()
    {
        var config = BuildConfig();

        var cost = LlmCostCalculator.Calculate(config, "unknown-model-xyz", 1000, 1000);

        Assert.Equal(0m, cost);
    }
}
