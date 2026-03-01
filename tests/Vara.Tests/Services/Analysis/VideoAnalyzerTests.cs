using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Vara.Api.Services.Analysis;
using Vara.Api.Services.YouTube;

namespace Vara.Tests.Services.Analysis;

public class VideoAnalyzerTests
{
    private readonly IYouTubeClient _youtube = Substitute.For<IYouTubeClient>();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    private VideoAnalyzer BuildSut() =>
        new(_youtube, _cache, NullLogger<VideoAnalyzer>.Instance);

    // -------------------------------------------------------------------------
    // Helper
    // -------------------------------------------------------------------------

    private static VideoMetadata MakeVideo(
        string title,
        int? durationSeconds,
        long views,
        int likes,
        int comments,
        int? daysOld) =>
        new(
            YoutubeId: "vid1",
            Title: title,
            Description: null,
            ChannelName: "TestChannel",
            ChannelId: null,
            DurationSeconds: durationSeconds,
            UploadDate: daysOld.HasValue ? DateTime.UtcNow.AddDays(-daysOld.Value) : null,
            ViewCount: views,
            LikeCount: likes,
            CommentCount: comments,
            ThumbnailUrl: null);

    // -------------------------------------------------------------------------
    // Empty results
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AnalyzeAsync_NoVideos_ReturnsEmptyResult()
    {
        _youtube.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        var sut = BuildSut();

        var result = await sut.AnalyzeAsync("unknown");

        Assert.Equal(0, result.SampleSize);
        Assert.Equal(0, result.AvgTitleLength);
        Assert.Null(result.AvgDurationSeconds);
        Assert.Equal(0, result.AvgEngagementRate);
        Assert.Single(result.Patterns);
        Assert.Contains("No strong patterns", result.Patterns[0]);
    }

    // -------------------------------------------------------------------------
    // Title stats
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AnalyzeAsync_TitleStats_CalculatedCorrectly()
    {
        // Titles of length 10, 20, 30 → avg=20, min=10, max=30
        var videos = new List<VideoMetadata>
        {
            MakeVideo(new string('a', 10), null, 0, 0, 0, null),
            MakeVideo(new string('a', 20), null, 0, 0, 0, null),
            MakeVideo(new string('a', 30), null, 0, 0, 0, null),
        };
        _youtube.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(videos);
        var sut = BuildSut();

        var result = await sut.AnalyzeAsync("keyword");

        Assert.Equal(20.0, result.AvgTitleLength);
        Assert.Equal(10, result.MinTitleLength);
        Assert.Equal(30, result.MaxTitleLength);
        Assert.True(result.TitleLengthStdDev > 0);
    }

    // -------------------------------------------------------------------------
    // Duration stats
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AnalyzeAsync_DurationStats_NullWhenNoDurations()
    {
        var videos = Enumerable.Range(0, 3)
            .Select(_ => MakeVideo("Title", null, 1000, 10, 5, 30))
            .ToList();
        _youtube.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(videos);
        var sut = BuildSut();

        var result = await sut.AnalyzeAsync("noduration");

        Assert.Null(result.AvgDurationSeconds);
        Assert.Null(result.MinDurationSeconds);
        Assert.Null(result.MaxDurationSeconds);
    }

    [Fact]
    public async Task AnalyzeAsync_DurationStats_CalculatedWhenPresent()
    {
        // Durations: 100, 200, 300 → avg=200, min=100, max=300
        var videos = new List<VideoMetadata>
        {
            MakeVideo("T1", 100, 0, 0, 0, null),
            MakeVideo("T2", 200, 0, 0, 0, null),
            MakeVideo("T3", 300, 0, 0, 0, null),
        };
        _youtube.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(videos);
        var sut = BuildSut();

        var result = await sut.AnalyzeAsync("durations");

        Assert.Equal(200.0, result.AvgDurationSeconds);
        Assert.Equal(100, result.MinDurationSeconds);
        Assert.Equal(300, result.MaxDurationSeconds);
    }

    // -------------------------------------------------------------------------
    // Engagement rate
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AnalyzeAsync_EngagementRate_CalculatedCorrectly()
    {
        // 100 likes + 0 comments on 1000 views → 10%
        var videos = Enumerable.Range(0, 3)
            .Select(_ => MakeVideo("Title", null, 1000, 100, 0, null))
            .ToList();
        _youtube.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(videos);
        var sut = BuildSut();

        var result = await sut.AnalyzeAsync("engagement");

        Assert.Equal(10.0, result.AvgEngagementRate);
    }

    // -------------------------------------------------------------------------
    // Upload day distribution
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AnalyzeAsync_UploadsByDay_GroupsCorrectly()
    {
        // Find the next Monday and Friday relative to UTC now.
        // We create uploads on specific days by calculating offsets.
        static int DaysUntilLast(DayOfWeek target)
        {
            var today = DateTime.UtcNow.DayOfWeek;
            var diff  = ((int)today - (int)target + 7) % 7;
            return diff == 0 ? 7 : diff; // go back at least 1 day
        }

        var mondayOffset = DaysUntilLast(DayOfWeek.Monday);
        var fridayOffset = DaysUntilLast(DayOfWeek.Friday);

        var videos = new List<VideoMetadata>
        {
            MakeVideo("M1", null, 0, 0, 0, mondayOffset),
            MakeVideo("M2", null, 0, 0, 0, mondayOffset),
            MakeVideo("M3", null, 0, 0, 0, mondayOffset),
            MakeVideo("F1", null, 0, 0, 0, fridayOffset),
        };
        _youtube.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(videos);
        var sut = BuildSut();

        var result = await sut.AnalyzeAsync("days");

        Assert.Equal(3, result.UploadsByDayOfWeek["Monday"]);
        Assert.Equal(1, result.UploadsByDayOfWeek["Friday"]);
    }

    // -------------------------------------------------------------------------
    // Pattern detection
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AnalyzeAsync_LongTitles_PatternDetected()
    {
        // Title of 60 chars → avg > 50 → triggers long-title pattern
        var videos = Enumerable.Range(0, 3)
            .Select(_ => MakeVideo(new string('a', 60), null, 0, 0, 0, null))
            .ToList();
        _youtube.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(videos);
        var sut = BuildSut();

        var result = await sut.AnalyzeAsync("longtitles");

        Assert.Contains(result.Patterns, p => p.Contains("chars"));
    }

    [Fact]
    public async Task AnalyzeAsync_ShortVideos_PatternDetected()
    {
        // Duration 300s (<600) → "under 10 minutes" pattern
        var videos = Enumerable.Range(0, 3)
            .Select(_ => MakeVideo("Title", 300, 0, 0, 0, null))
            .ToList();
        _youtube.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(videos);
        var sut = BuildSut();

        var result = await sut.AnalyzeAsync("shortvideos");

        Assert.Contains(result.Patterns, p => p.Contains("under 10 minutes"));
    }

    [Fact]
    public async Task AnalyzeAsync_HighEngagement_PatternDetected()
    {
        // 600 likes / 1000 views = 60% engagement → > 5% threshold
        var videos = Enumerable.Range(0, 3)
            .Select(_ => MakeVideo("Title", null, 1000, 600, 0, null))
            .ToList();
        _youtube.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(videos);
        var sut = BuildSut();

        var result = await sut.AnalyzeAsync("highengagement");

        Assert.Contains(result.Patterns, p => p.Contains("engagement rate"));
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

        await sut.AnalyzeAsync("cached");
        await sut.AnalyzeAsync("cached"); // second call — should hit cache

        await _youtube.Received(1)
            .SearchAsync("cached", Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnalyzeAsync_DifferentSampleSizes_SeparateCacheEntries()
    {
        _youtube.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        var sut = BuildSut();

        await sut.AnalyzeAsync("keyword", sampleSize: 10);
        await sut.AnalyzeAsync("keyword", sampleSize: 20);

        await _youtube.Received(1)
            .SearchAsync("keyword", 10, Arg.Any<CancellationToken>());
        await _youtube.Received(1)
            .SearchAsync("keyword", 20, Arg.Any<CancellationToken>());
    }
}
