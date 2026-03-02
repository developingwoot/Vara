using Vara.Api.Services.Background;
using Vara.Api.Services.Monitoring;
using Vara.Api.Services.YouTube;

namespace Vara.Tests.Services.Background;

public class TrendAnalysisBackgroundServiceTests
{
    // -------------------------------------------------------------------------
    // CalculateVolume — pure function, no mocking needed
    // -------------------------------------------------------------------------

    [Fact]
    public void CalculateVolume_EmptyList_ReturnsZero()
    {
        var result = TrendAnalysisBackgroundService.CalculateVolume([]);

        Assert.Equal(0, result);
    }

    [Fact]
    public void CalculateVolume_TotalViewsUnderOneMillion_ReturnsLowVolume()
    {
        var videos = new List<VideoMetadata>
        {
            MakeVideo(viewCount: 500_000),
        };

        var result = TrendAnalysisBackgroundService.CalculateVolume(videos);

        Assert.Equal(0, result); // 500_000 / 1_000_000 = 0 (integer division)
    }

    [Fact]
    public void CalculateVolume_TotalViewsAtOneMillion_ReturnsOne()
    {
        var videos = new List<VideoMetadata>
        {
            MakeVideo(viewCount: 1_000_000),
        };

        var result = TrendAnalysisBackgroundService.CalculateVolume(videos);

        Assert.Equal(1, result);
    }

    [Fact]
    public void CalculateVolume_MultipleVideos_SumsViews()
    {
        var videos = new List<VideoMetadata>
        {
            MakeVideo(viewCount: 30_000_000),
            MakeVideo(viewCount: 20_000_000),
        };

        var result = TrendAnalysisBackgroundService.CalculateVolume(videos);

        Assert.Equal(50, result);
    }

    [Fact]
    public void CalculateVolume_ExcessiveViews_ClampsAt100()
    {
        var videos = new List<VideoMetadata>
        {
            MakeVideo(viewCount: 999_000_000),
        };

        var result = TrendAnalysisBackgroundService.CalculateVolume(videos);

        Assert.Equal(100, result);
    }

    // -------------------------------------------------------------------------
    // GetDelayUntilNext2Am — time calculation
    // -------------------------------------------------------------------------

    [Fact]
    public void GetDelayUntilNext2Am_Before2Am_ReturnsPositiveDelay()
    {
        var delay = TrendAnalysisBackgroundService.GetDelayUntilNext2Am();

        Assert.True(delay > TimeSpan.Zero);
        Assert.True(delay <= TimeSpan.FromHours(24));
    }

    [Fact]
    public void GetDelayUntilNext2Am_DelayIsUnder24Hours()
    {
        var delay = TrendAnalysisBackgroundService.GetDelayUntilNext2Am();

        Assert.True(delay < TimeSpan.FromHours(24));
    }

    // -------------------------------------------------------------------------
    // ExecuteAsync — cancellation handling
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ExecuteAsync_AlreadyCancelled_ExitsCleanly()
    {
        var serviceProvider = NSubstitute.Substitute.For<IServiceProvider>();
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<TrendAnalysisBackgroundService>.Instance;

        var sut = new TrendAnalysisBackgroundService(serviceProvider, new BackgroundJobHealthMonitor(), logger);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Should not throw — cancelled token before first run exits gracefully
        await sut.StartAsync(cts.Token);
        await sut.StopAsync(CancellationToken.None);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static VideoMetadata MakeVideo(long viewCount) => new(
        YoutubeId: Guid.NewGuid().ToString(),
        Title: "Test Video",
        Description: null,
        ChannelName: null,
        ChannelId: null,
        DurationSeconds: null,
        UploadDate: null,
        ViewCount: viewCount,
        LikeCount: 0,
        CommentCount: 0,
        ThumbnailUrl: null);
}
