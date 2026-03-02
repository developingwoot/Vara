using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Vara.Api.Data;
using Vara.Api.Models.Entities;
using Vara.Api.Services.Analysis;
using Vara.Api.Services.Llm;
using Vara.Api.Services.YouTube;

namespace Vara.Tests.Services.Analysis;

public class TranscriptAnalysisServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static readonly VideoMetadata FakeMeta = new(
        YoutubeId: "dQw4w9WgXcQ",
        Title: "Test Video Title",
        Description: null,
        ChannelName: "Test Channel",
        ChannelId: null,
        DurationSeconds: 600,
        UploadDate: null,
        ViewCount: 100_000,
        LikeCount: 5_000,
        CommentCount: 300,
        ThumbnailUrl: null);

    private static readonly LlmResponse FakeLlmResponse = new(
        Content: "Structured transcript analysis...",
        PromptTokens: 500,
        CompletionTokens: 600,
        CostUsd: 0.015m,
        ProviderName: "Anthropic",
        ModelUsed: "claude-sonnet-4-6",
        GeneratedAt: DateTime.UtcNow);

    private static VaraContext BuildDb()
    {
        var options = new DbContextOptionsBuilder<VaraContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new VaraContext(options);
    }

    private static (
        IYouTubeClient youtube,
        ILlmOrchestrator llm,
        IPlanEnforcer planEnforcer,
        IUsageMeter usageMeter,
        VaraContext db,
        TranscriptAnalysisService sut)
    BuildSut(
        VaraContext? db = null,
        string? transcriptReturn = "sample transcript text",
        LlmResponse? llmReturn = null)
    {
        var youtube      = Substitute.For<IYouTubeClient>();
        var llm          = Substitute.For<ILlmOrchestrator>();
        var planEnforcer = Substitute.For<IPlanEnforcer>();
        var usageMeter   = Substitute.For<IUsageMeter>();

        youtube.GetVideoAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(FakeMeta);
        youtube.GetTranscriptAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(transcriptReturn);

        llm.ExecuteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LlmOptions?>(), Arg.Any<CancellationToken>())
           .Returns(llmReturn ?? FakeLlmResponse);

        var context = db ?? BuildDb();

        var sut = new TranscriptAnalysisService(
            youtube, llm, planEnforcer, usageMeter, context,
            NullLogger<TranscriptAnalysisService>.Instance);

        return (youtube, llm, planEnforcer, usageMeter, context, sut);
    }

    // -------------------------------------------------------------------------
    // Caching behaviour
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AnalyzeAsync_NoSavedVideo_NoTranscript_ReturnsBaseResult()
    {
        var (_, _, _, _, _, sut) = BuildSut(transcriptReturn: null);

        var result = await sut.AnalyzeAsync(UserId, "dQw4w9WgXcQ");

        Assert.False(result.TranscriptAvailable);
        Assert.Equal(0, result.WordCount);
        Assert.Equal(0, result.SentenceCount);
        Assert.False(result.LlmEnhanced);
    }

    [Fact]
    public async Task AnalyzeAsync_CachedTranscript_SkipsYouTubeFetch()
    {
        var db = BuildDb();
        db.Videos.Add(new Video
        {
            UserId         = UserId,
            YoutubeId      = "dQw4w9WgXcQ",
            TranscriptText = "cached transcript here"
        });
        await db.SaveChangesAsync();

        var (youtube, _, _, _, _, sut) = BuildSut(db: db);

        var result = await sut.AnalyzeAsync(UserId, "dQw4w9WgXcQ");

        Assert.True(result.TranscriptAvailable);
        await youtube.DidNotReceive()
            .GetTranscriptAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnalyzeAsync_FetchedTranscript_SavedToExistingVideoRecord()
    {
        var db = BuildDb();
        db.Videos.Add(new Video
        {
            UserId    = UserId,
            YoutubeId = "dQw4w9WgXcQ"
            // TranscriptText intentionally null
        });
        await db.SaveChangesAsync();

        var (_, _, _, _, context, sut) = BuildSut(db: db, transcriptReturn: "fresh transcript");

        await sut.AnalyzeAsync(UserId, "dQw4w9WgXcQ");

        var video = await context.Videos.FirstAsync(v => v.UserId == UserId && v.YoutubeId == "dQw4w9WgXcQ");
        Assert.Equal("fresh transcript", video.TranscriptText);
    }

    [Fact]
    public async Task AnalyzeAsync_NoSavedVideo_FetchedTranscript_NotPersistedToDb()
    {
        var (_, _, _, _, context, sut) = BuildSut(transcriptReturn: "fetched text");

        var result = await sut.AnalyzeAsync(UserId, "dQw4w9WgXcQ");

        Assert.True(result.TranscriptAvailable);
        Assert.Equal(0, await context.Videos.CountAsync());
    }

    // -------------------------------------------------------------------------
    // Base metric calculations
    // -------------------------------------------------------------------------

    [Fact]
    public void AnalyzeAsync_WordCount_CorrectForSampleText()
    {
        var result = TranscriptAnalysisService.CountWords("hello world foo bar");

        Assert.Equal(4, result);
    }

    [Fact]
    public void AnalyzeAsync_SentenceCount_CorrectForSampleText()
    {
        var result = TranscriptAnalysisService.CountSentences("Hello world. How are you? Great!");

        Assert.Equal(3, result);
    }

    // -------------------------------------------------------------------------
    // LLM integration
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AnalyzeAsync_WithoutInsights_LlmNotCalled()
    {
        var (_, llm, _, usageMeter, _, sut) = BuildSut();

        var result = await sut.AnalyzeAsync(UserId, "dQw4w9WgXcQ", includeInsights: false);

        Assert.False(result.LlmEnhanced);
        await llm.DidNotReceive()
            .ExecuteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LlmOptions?>(), Arg.Any<CancellationToken>());
        await usageMeter.DidNotReceive()
            .RecordLlmCallAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnalyzeAsync_WithInsights_CallsLlmAndRecordsUsage()
    {
        var (_, llm, _, usageMeter, _, sut) = BuildSut();

        var result = await sut.AnalyzeAsync(UserId, "dQw4w9WgXcQ", includeInsights: true);

        Assert.True(result.LlmEnhanced);
        Assert.Equal(FakeLlmResponse.Content, result.LlmInsights);
        await llm.Received(1)
            .ExecuteAsync("TranscriptAnalysis", Arg.Any<string>(), Arg.Any<LlmOptions?>(), Arg.Any<CancellationToken>());
        await usageMeter.Received(1)
            .RecordLlmCallAsync(UserId, "TranscriptAnalysis", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnalyzeAsync_LongTranscript_TruncatedToMaxChars()
    {
        var longTranscript = new string('a', 40_000); // exceeds 32 000 char limit
        var (_, llm, _, _, _, sut) = BuildSut(transcriptReturn: longTranscript);

        await sut.AnalyzeAsync(UserId, "dQw4w9WgXcQ", includeInsights: true);

        await llm.Received(1).ExecuteAsync(
            "TranscriptAnalysis",
            Arg.Is<string>(p => p.Contains(new string('a', 32_000)) && !p.Contains(new string('a', 32_001))),
            Arg.Any<LlmOptions?>(),
            Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // Plan enforcement
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AnalyzeAsync_AlwaysEnforcesPlan()
    {
        var (_, _, planEnforcer, _, _, sut) = BuildSut();

        await sut.AnalyzeAsync(UserId, "dQw4w9WgXcQ", includeInsights: false);

        await planEnforcer.Received(1)
            .EnforceAsync(UserId, "transcripts", Arg.Any<CancellationToken>());
    }
}
