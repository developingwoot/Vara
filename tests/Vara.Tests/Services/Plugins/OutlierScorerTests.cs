using Vara.Api.Plugins.OutlierDetection;

namespace Vara.Tests.Services.Plugins;

public class OutlierScorerTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static OutlierCandidate MakeCandidate(
        string videoId = "vid1",
        string title = "Test Video",
        long subscribers = 10_000,
        long views = 100_000,
        DateTime? uploadDate = null) =>
        new(videoId, title, "Test Channel", subscribers, views, uploadDate);

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [Fact]
    public void Score_EmptyInput_ReturnsEmpty()
    {
        var result = OutlierScorer.Score([], minOutlierRatio: 5, maxChannelSize: 500_000);

        Assert.Empty(result);
    }

    [Fact]
    public void Score_ZeroSubscribers_Excluded()
    {
        var candidates = new[]
        {
            MakeCandidate(subscribers: 0, views: 100_000)
        };

        var result = OutlierScorer.Score(candidates, minOutlierRatio: 5, maxChannelSize: 500_000);

        Assert.Empty(result);
    }

    [Fact]
    public void Score_AboveMaxChannelSize_Excluded()
    {
        var candidates = new[]
        {
            MakeCandidate(subscribers: 600_000, views: 6_000_000) // ratio 10 but channel too large
        };

        var result = OutlierScorer.Score(candidates, minOutlierRatio: 5, maxChannelSize: 500_000);

        Assert.Empty(result);
    }

    [Fact]
    public void Score_BelowMinRatio_Excluded()
    {
        var candidates = new[]
        {
            MakeCandidate(subscribers: 10_000, views: 30_000) // ratio = 3, below min of 5
        };

        var result = OutlierScorer.Score(candidates, minOutlierRatio: 5, maxChannelSize: 500_000);

        Assert.Empty(result);
    }

    [Fact]
    public void Score_BasicOutlier_CorrectRatio()
    {
        var candidates = new[]
        {
            MakeCandidate(subscribers: 5_000, views: 100_000) // ratio = 20.0
        };

        var result = OutlierScorer.Score(candidates, minOutlierRatio: 5, maxChannelSize: 500_000);

        Assert.Single(result);
        Assert.Equal(20.0, result[0].OutlierRatio);
    }

    [Fact]
    public void Score_NormalizesScores_MaxIs100()
    {
        var candidates = new[]
        {
            MakeCandidate("v1", subscribers: 5_000,  views: 100_000), // ratio 20
            MakeCandidate("v2", subscribers: 10_000, views: 100_000)  // ratio 10
        };

        var result = OutlierScorer.Score(candidates, minOutlierRatio: 5, maxChannelSize: 500_000);

        var maxScore = result.Max(r => r.OutlierScore);
        Assert.Equal(100, maxScore);
    }

    [Fact]
    public void Score_StrengthStrong_RatioAtOrAbove10()
    {
        var candidates = new[]
        {
            MakeCandidate(subscribers: 10_000, views: 100_000) // ratio = 10.0
        };

        var result = OutlierScorer.Score(candidates, minOutlierRatio: 5, maxChannelSize: 500_000);

        Assert.Equal("Strong", result[0].OutlierStrength);
    }

    [Fact]
    public void Score_StrengthModerate_RatioBetween5And10()
    {
        var candidates = new[]
        {
            MakeCandidate(subscribers: 10_000, views: 75_000) // ratio = 7.5
        };

        var result = OutlierScorer.Score(candidates, minOutlierRatio: 5, maxChannelSize: 500_000);

        Assert.Equal("Moderate", result[0].OutlierStrength);
    }

    [Fact]
    public void Score_StrengthMild_RatioMeetsMinButBelow5()
    {
        var candidates = new[]
        {
            MakeCandidate(subscribers: 10_000, views: 30_000) // ratio = 3.0, below threshold
        };

        // Use minOutlierRatio: 3 so this candidate qualifies
        var result = OutlierScorer.Score(candidates, minOutlierRatio: 3, maxChannelSize: 500_000);

        Assert.Equal("Mild", result[0].OutlierStrength);
    }

    [Fact]
    public void Score_MultipleVideos_SortedByScoreDescending()
    {
        var candidates = new[]
        {
            MakeCandidate("v1", subscribers: 10_000, views: 50_000),  // ratio 5
            MakeCandidate("v2", subscribers: 5_000,  views: 100_000), // ratio 20
            MakeCandidate("v3", subscribers: 10_000, views: 80_000)   // ratio 8
        };

        var result = OutlierScorer.Score(candidates, minOutlierRatio: 5, maxChannelSize: 500_000);

        Assert.Equal(3, result.Count);
        Assert.Equal("v2", result[0].VideoId); // highest ratio first
        Assert.Equal("v3", result[1].VideoId);
        Assert.Equal("v1", result[2].VideoId);
    }
}
