using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Vara.Api.Services.Analysis;
using Vara.Api.Services.YouTube;

namespace Vara.Tests.Services.Analysis;

public class KeywordAnalyzerTests
{
    private readonly IYouTubeClient _youtube = Substitute.For<IYouTubeClient>();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    private KeywordAnalyzer BuildSut() =>
        new(_youtube, _cache, NullLogger<KeywordAnalyzer>.Instance);

    // -------------------------------------------------------------------------
    // Helper
    // -------------------------------------------------------------------------

    private static VideoMetadata MakeVideo(
        long views,
        int likes,
        int comments,
        int? daysOld) =>
        new(
            YoutubeId: "vid1",
            Title: "Test",
            Description: null,
            ChannelName: null,
            ChannelId: null,
            DurationSeconds: null,
            UploadDate: daysOld.HasValue ? DateTime.UtcNow.AddDays(-daysOld.Value) : null,
            ViewCount: views,
            LikeCount: likes,
            CommentCount: comments,
            ThumbnailUrl: null);

    // -------------------------------------------------------------------------
    // Score bounds
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AnalyzeAsync_ScoresAreInBounds()
    {
        var videos = Enumerable.Range(0, 5)
            .Select(_ => MakeVideo(500_000, 1000, 100, 60))
            .ToList();
        _youtube.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(videos);
        var sut = BuildSut();

        var result = await sut.AnalyzeAsync("some keyword");

        Assert.InRange(result.SearchVolumeRelative, (short)0, (short)100);
        Assert.InRange(result.CompetitionScore, (short)0, (short)100);
    }

    // -------------------------------------------------------------------------
    // Search volume
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AnalyzeAsync_HighViews_ReturnsMaxVolume()
    {
        // 10 videos × 10M views = 100M total → 100M / 1M = 100 (capped)
        var videos = Enumerable.Range(0, 10)
            .Select(_ => MakeVideo(10_000_000, 0, 0, 90))
            .ToList();
        _youtube.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(videos);
        var sut = BuildSut();

        var result = await sut.AnalyzeAsync("huge keyword");

        Assert.Equal((short)100, result.SearchVolumeRelative);
    }

    [Fact]
    public async Task AnalyzeAsync_LowViews_ReturnsLowVolume()
    {
        // 10 videos × 100 views = 1,000 total → 1,000 / 1M = 0
        var videos = Enumerable.Range(0, 10)
            .Select(_ => MakeVideo(100, 0, 0, 30))
            .ToList();
        _youtube.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(videos);
        var sut = BuildSut();

        var result = await sut.AnalyzeAsync("tiny keyword");

        Assert.Equal((short)0, result.SearchVolumeRelative);
    }

    // -------------------------------------------------------------------------
    // Competition
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AnalyzeAsync_OldHighEngagement_ReturnsHighCompetition()
    {
        // Very old (500 days → ageScore = 50 max) + high engagement
        var videos = Enumerable.Range(0, 5)
            .Select(_ => MakeVideo(1000, 500, 500, 500))
            .ToList();
        _youtube.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(videos);
        var sut = BuildSut();

        var result = await sut.AnalyzeAsync("saturated keyword");

        Assert.True(result.CompetitionScore > 50);
    }

    // -------------------------------------------------------------------------
    // Trend direction
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AnalyzeAsync_RecentOutperformsOlder_ReturnsRising()
    {
        // 5 recent videos with 2M views, 5 older with 1M views → growth = 100% > 20%
        var recent = Enumerable.Range(0, 5).Select(_ => MakeVideo(2_000_000, 0, 0, 15));
        var older  = Enumerable.Range(0, 5).Select(_ => MakeVideo(1_000_000, 0, 0, 60));
        _youtube.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(recent.Concat(older).ToList());
        var sut = BuildSut();

        var result = await sut.AnalyzeAsync("rising keyword");

        Assert.Equal("rising", result.TrendDirection);
    }

    [Fact]
    public async Task AnalyzeAsync_OlderOutperformsRecent_ReturnsDeclining()
    {
        // 5 recent videos with 500K views, 5 older with 2M views → growth = -75% < -20%
        var recent = Enumerable.Range(0, 5).Select(_ => MakeVideo(500_000, 0, 0, 10));
        var older  = Enumerable.Range(0, 5).Select(_ => MakeVideo(2_000_000, 0, 0, 90));
        _youtube.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(recent.Concat(older).ToList());
        var sut = BuildSut();

        var result = await sut.AnalyzeAsync("declining keyword");

        Assert.Equal("declining", result.TrendDirection);
    }

    // -------------------------------------------------------------------------
    // Empty results
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AnalyzeAsync_NoVideos_ReturnsZeroScoresAndNewTrend()
    {
        _youtube.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        var sut = BuildSut();

        var result = await sut.AnalyzeAsync("unknown keyword");

        Assert.Equal((short)0, result.SearchVolumeRelative);
        Assert.Equal((short)0, result.CompetitionScore);
        Assert.Equal("new", result.TrendDirection);
    }

    // -------------------------------------------------------------------------
    // Caching
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AnalyzeAsync_CachesResult_YouTubeCalledOnce()
    {
        _youtube.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        var sut = BuildSut();

        await sut.AnalyzeAsync("cached keyword");
        await sut.AnalyzeAsync("cached keyword"); // second call — should hit cache

        await _youtube.Received(1)
            .SearchAsync("cached keyword", Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // Intent classification
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AnalyzeAsync_HowToKeyword_ClassifiesAsHowTo()
    {
        _youtube.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        var sut = BuildSut();

        var result = await sut.AnalyzeAsync("how to cook pasta");

        Assert.Equal("how-to", result.KeywordIntent);
    }

    [Fact]
    public async Task AnalyzeAsync_ReviewKeyword_ClassifiesAsOpinion()
    {
        _youtube.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        var sut = BuildSut();

        var result = await sut.AnalyzeAsync("iphone 16 review");

        Assert.Equal("opinion", result.KeywordIntent);
    }
}
