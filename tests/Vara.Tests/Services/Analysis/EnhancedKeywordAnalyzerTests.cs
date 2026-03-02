using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Vara.Api.Services.Analysis;
using Vara.Api.Services.Llm;

namespace Vara.Tests.Services.Analysis;

public class EnhancedKeywordAnalyzerTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static readonly KeywordAnalysisResult BaseResult = new(
        Keyword: "react tutorial",
        Niche: "Web Dev",
        SearchVolumeRelative: 75,
        CompetitionScore: 60,
        TrendDirection: "rising",
        KeywordIntent: "how-to",
        AnalyzedAt: DateTime.UtcNow);

    private static readonly LlmResponse FakeLlmResponse = new(
        Content: "Here are strategic insights for this keyword...",
        PromptTokens: 200,
        CompletionTokens: 400,
        CostUsd: 0.005m,
        ProviderName: "Anthropic",
        ModelUsed: "claude-sonnet-4-6",
        GeneratedAt: DateTime.UtcNow);

    private static (
        IKeywordAnalyzer baseAnalyzer,
        ILlmOrchestrator llm,
        IPlanEnforcer planEnforcer,
        IUsageMeter usageMeter,
        EnhancedKeywordAnalyzerService sut)
    BuildSut(
        KeywordAnalysisResult? baseReturn = null,
        LlmResponse? llmReturn = null)
    {
        var baseAnalyzer  = Substitute.For<IKeywordAnalyzer>();
        var llm           = Substitute.For<ILlmOrchestrator>();
        var planEnforcer  = Substitute.For<IPlanEnforcer>();
        var usageMeter    = Substitute.For<IUsageMeter>();

        baseAnalyzer
            .AnalyzeAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(baseReturn ?? BaseResult);

        llm.ExecuteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LlmOptions?>(), Arg.Any<CancellationToken>())
           .Returns(llmReturn ?? FakeLlmResponse);

        var sut = new EnhancedKeywordAnalyzerService(
            baseAnalyzer, llm, planEnforcer, usageMeter,
            NullLogger<EnhancedKeywordAnalyzerService>.Instance);

        return (baseAnalyzer, llm, planEnforcer, usageMeter, sut);
    }

    // -------------------------------------------------------------------------

    [Fact]
    public async Task AnalyzeAsync_WithoutInsights_ReturnsBaseResult_NoLlmCall()
    {
        var (baseAnalyzer, llm, planEnforcer, _, sut) = BuildSut();

        var result = await sut.AnalyzeAsync(UserId, "react tutorial", "Web Dev", includeInsights: false);

        Assert.Equal("react tutorial", result.Keyword);
        Assert.False(result.LlmEnhanced);
        Assert.Null(result.LlmInsights);

        await llm.DidNotReceive()
            .ExecuteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LlmOptions?>(), Arg.Any<CancellationToken>());
        await planEnforcer.DidNotReceive()
            .EnforceAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnalyzeAsync_WithInsights_CallsPlanEnforcer_ThenLlm()
    {
        var (_, llm, planEnforcer, _, sut) = BuildSut();

        await sut.AnalyzeAsync(UserId, "react tutorial", "Web Dev", includeInsights: true);

        await planEnforcer.Received(1).EnforceAsync(UserId, "llm_insights", Arg.Any<CancellationToken>());
        await llm.Received(1)
            .ExecuteAsync("KeywordInsights", Arg.Any<string>(), Arg.Any<LlmOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnalyzeAsync_WithInsights_ReturnsEnrichedResult()
    {
        var (_, _, _, _, sut) = BuildSut();

        var result = await sut.AnalyzeAsync(UserId, "react tutorial", "Web Dev", includeInsights: true);

        Assert.True(result.LlmEnhanced);
        Assert.Equal(FakeLlmResponse.Content, result.LlmInsights);
        Assert.Equal("react tutorial", result.Keyword);
        Assert.Equal("rising", result.TrendDirection);
    }

    [Fact]
    public async Task AnalyzeAsync_WithInsights_RecordsUsage()
    {
        var (_, _, _, usageMeter, sut) = BuildSut();

        await sut.AnalyzeAsync(UserId, "react tutorial", "Web Dev", includeInsights: true);

        await usageMeter.Received(1).RecordLlmCallAsync(UserId, "KeywordInsights", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnalyzeAsync_WithInsights_FreeTier_ThrowsFeatureAccessDeniedException()
    {
        var (_, _, planEnforcer, _, sut) = BuildSut();

        planEnforcer
            .EnforceAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new FeatureAccessDeniedException("Feature 'llm_insights' requires the Creator tier."));

        await Assert.ThrowsAsync<FeatureAccessDeniedException>(() =>
            sut.AnalyzeAsync(UserId, "react tutorial", "Web Dev", includeInsights: true));
    }

    [Fact]
    public async Task AnalyzeAsync_WithInsights_AccessDenied_DoesNotRecordUsage()
    {
        var (_, _, planEnforcer, usageMeter, sut) = BuildSut();

        planEnforcer
            .EnforceAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new FeatureAccessDeniedException("Feature 'llm_insights' requires the Creator tier."));

        await Assert.ThrowsAsync<FeatureAccessDeniedException>(() =>
            sut.AnalyzeAsync(UserId, "react tutorial", "Web Dev", includeInsights: true));

        await usageMeter.DidNotReceive()
            .RecordLlmCallAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnalyzeAsync_PassesKeywordAndNiche_ToBaseAnalyzer()
    {
        var (baseAnalyzer, _, _, _, sut) = BuildSut();

        await sut.AnalyzeAsync(UserId, "tailwind css", "Web Dev");

        await baseAnalyzer.Received(1)
            .AnalyzeAsync("tailwind css", "Web Dev", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnalyzeAsync_BaseResultFields_PreservedInEnrichedResult()
    {
        var (_, _, _, _, sut) = BuildSut();

        var result = await sut.AnalyzeAsync(UserId, "react tutorial", "Web Dev", includeInsights: true);

        Assert.Equal(BaseResult.SearchVolumeRelative, result.SearchVolumeRelative);
        Assert.Equal(BaseResult.CompetitionScore, result.CompetitionScore);
        Assert.Equal(BaseResult.KeywordIntent, result.KeywordIntent);
    }
}
