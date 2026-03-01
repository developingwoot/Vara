using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Vara.Api.Data;
using Vara.Api.Models.Entities;
using Vara.Api.Services.Analysis;

namespace Vara.Tests.Services.Analysis;

public class TrendDetectionServiceTests
{
    private static VaraContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<VaraContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new VaraContext(options);
    }

    private static TrendDetectionService CreateService(VaraContext db) =>
        new(db, new MemoryCache(new MemoryCacheOptions()), NullLogger<TrendDetectionService>.Instance);

    private static void SeedSnapshot(
        VaraContext db,
        Guid userId,
        string keyword,
        short volume,
        int daysAgo,
        string? niche = null)
    {
        db.KeywordSnapshots.Add(new KeywordSnapshot
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Keyword = keyword,
            Niche = niche,
            SearchVolumeRelative = volume,
            CompetitionScore = 50,
            CapturedAt = DateTime.UtcNow.AddDays(-daysAgo)
        });
        db.SaveChanges();
    }

    // -------------------------------------------------------------------------

    [Fact]
    public async Task FindTrendingAsync_NoSnapshots_ReturnsEmptyLists()
    {
        using var db = CreateContext();
        var service = CreateService(db);

        var result = await service.FindTrendingAsync(Guid.NewGuid());

        Assert.Empty(result.Rising);
        Assert.Empty(result.Declining);
        Assert.Empty(result.New);
    }

    [Fact]
    public async Task FindTrendingAsync_NewKeyword_ClassifiedAsNew()
    {
        using var db = CreateContext();
        var userId = Guid.NewGuid();
        SeedSnapshot(db, userId, "react hooks", 80, daysAgo: 1);
        SeedSnapshot(db, userId, "react hooks", 85, daysAgo: 2);
        var service = CreateService(db);

        var result = await service.FindTrendingAsync(userId, minSnapshots: 2);

        var kw = Assert.Single(result.New);
        Assert.Equal("react hooks", kw.Keyword);
        Assert.Equal("New", kw.Lifecycle);
        Assert.Empty(result.Rising);
        Assert.Empty(result.Declining);
    }

    [Fact]
    public async Task FindTrendingAsync_GrowthOver10Pct_ClassifiedAsRising()
    {
        using var db = CreateContext();
        var userId = Guid.NewGuid();
        // Previous window (8-9 days ago): avg = 100
        SeedSnapshot(db, userId, "python basics", 100, daysAgo: 8);
        SeedSnapshot(db, userId, "python basics", 100, daysAgo: 9);
        // Current window (1-2 days ago): avg = 120
        SeedSnapshot(db, userId, "python basics", 120, daysAgo: 1);
        SeedSnapshot(db, userId, "python basics", 120, daysAgo: 2);
        var service = CreateService(db);

        var result = await service.FindTrendingAsync(userId, minSnapshots: 2);

        var kw = Assert.Single(result.Rising);
        Assert.Equal("python basics", kw.Keyword);
        Assert.Equal("Rising", kw.Lifecycle);
    }

    [Fact]
    public async Task FindTrendingAsync_DeclineOver10Pct_ClassifiedAsDeclining()
    {
        using var db = CreateContext();
        var userId = Guid.NewGuid();
        // Previous window: avg = 100
        SeedSnapshot(db, userId, "old framework", 100, daysAgo: 8);
        SeedSnapshot(db, userId, "old framework", 100, daysAgo: 9);
        // Current window: avg = 80
        SeedSnapshot(db, userId, "old framework", 80, daysAgo: 1);
        SeedSnapshot(db, userId, "old framework", 80, daysAgo: 2);
        var service = CreateService(db);

        var result = await service.FindTrendingAsync(userId, minSnapshots: 2);

        var kw = Assert.Single(result.Declining);
        Assert.Equal("old framework", kw.Keyword);
        Assert.Equal("Declining", kw.Lifecycle);
    }

    [Fact]
    public async Task FindTrendingAsync_SmallChange_ClassifiedAsStable()
    {
        using var db = CreateContext();
        var userId = Guid.NewGuid();
        // Previous window: avg = 100
        SeedSnapshot(db, userId, "stable topic", 100, daysAgo: 8);
        SeedSnapshot(db, userId, "stable topic", 100, daysAgo: 9);
        // Current window: avg = 105 (5% growth — not Rising)
        SeedSnapshot(db, userId, "stable topic", 105, daysAgo: 1);
        SeedSnapshot(db, userId, "stable topic", 105, daysAgo: 2);
        var service = CreateService(db);

        var result = await service.FindTrendingAsync(userId, minSnapshots: 2);

        // Stable keywords are not returned in any list
        Assert.Empty(result.Rising);
        Assert.Empty(result.Declining);
        Assert.Empty(result.New);
    }

    [Fact]
    public async Task FindTrendingAsync_GrowthRate_CalculatedCorrectly()
    {
        using var db = CreateContext();
        var userId = Guid.NewGuid();
        // Previous window: avg = 200
        SeedSnapshot(db, userId, "fast growth", 200, daysAgo: 8);
        SeedSnapshot(db, userId, "fast growth", 200, daysAgo: 9);
        // Current window: avg = 300 → growthRate = (300-200)/200 * 100 = 50%
        SeedSnapshot(db, userId, "fast growth", 300, daysAgo: 1);
        SeedSnapshot(db, userId, "fast growth", 300, daysAgo: 2);
        var service = CreateService(db);

        var result = await service.FindTrendingAsync(userId, minSnapshots: 2);

        var kw = Assert.Single(result.Rising);
        Assert.Equal(50.0, kw.GrowthRate, precision: 1);
    }

    [Fact]
    public async Task FindTrendingAsync_MomentumScore_CalculatedCorrectly()
    {
        using var db = CreateContext();
        var userId = Guid.NewGuid();
        // Previous: 200, Current: 300 → growthRate = 50, currentVolume = 300
        // momentum = 50 * Math.Log(301) = 50 * 5.707... ≈ 285.3
        SeedSnapshot(db, userId, "high momentum", 200, daysAgo: 8);
        SeedSnapshot(db, userId, "high momentum", 200, daysAgo: 9);
        SeedSnapshot(db, userId, "high momentum", 300, daysAgo: 1);
        SeedSnapshot(db, userId, "high momentum", 300, daysAgo: 2);
        var service = CreateService(db);

        var result = await service.FindTrendingAsync(userId, minSnapshots: 2);

        var kw = Assert.Single(result.Rising);
        var expectedMomentum = Math.Round(50.0 * Math.Log(301), 2);
        Assert.Equal(expectedMomentum, kw.MomentumScore, precision: 1);
    }

    [Fact]
    public async Task FindTrendingAsync_NicheFilter_OnlyReturnsMatchingNiche()
    {
        using var db = CreateContext();
        var userId = Guid.NewGuid();
        // Previous + Current for "programming" niche (Rising)
        SeedSnapshot(db, userId, "typescript", 100, daysAgo: 8, niche: "programming");
        SeedSnapshot(db, userId, "typescript", 100, daysAgo: 9, niche: "programming");
        SeedSnapshot(db, userId, "typescript", 150, daysAgo: 1, niche: "programming");
        SeedSnapshot(db, userId, "typescript", 150, daysAgo: 2, niche: "programming");
        // Another niche — should be excluded by filter
        SeedSnapshot(db, userId, "yoga", 100, daysAgo: 8, niche: "health");
        SeedSnapshot(db, userId, "yoga", 100, daysAgo: 9, niche: "health");
        SeedSnapshot(db, userId, "yoga", 150, daysAgo: 1, niche: "health");
        SeedSnapshot(db, userId, "yoga", 150, daysAgo: 2, niche: "health");
        var service = CreateService(db);

        var result = await service.FindTrendingAsync(userId, niche: "programming", minSnapshots: 2);

        var kw = Assert.Single(result.Rising);
        Assert.Equal("typescript", kw.Keyword);
        Assert.Equal("programming", kw.Niche);
    }

    [Fact]
    public async Task FindTrendingAsync_MinSnapshots_FiltersKeywordsWithInsufficientData()
    {
        using var db = CreateContext();
        var userId = Guid.NewGuid();
        // Only 1 snapshot in current window — does not meet minSnapshots=2
        SeedSnapshot(db, userId, "sparse keyword", 100, daysAgo: 1);
        var service = CreateService(db);

        var result = await service.FindTrendingAsync(userId, minSnapshots: 2);

        Assert.Empty(result.Rising);
        Assert.Empty(result.Declining);
        Assert.Empty(result.New);
    }

    [Fact]
    public async Task FindTrendingAsync_RisingKeywords_SortedByMomentumDescending()
    {
        using var db = CreateContext();
        var userId = Guid.NewGuid();
        // Keyword A: small volume, high growth rate
        SeedSnapshot(db, userId, "keyword-a", 10, daysAgo: 8);
        SeedSnapshot(db, userId, "keyword-a", 10, daysAgo: 9);
        SeedSnapshot(db, userId, "keyword-a", 50, daysAgo: 1); // 400% growth, low volume
        SeedSnapshot(db, userId, "keyword-a", 50, daysAgo: 2);
        // Keyword B: high volume, moderate growth rate
        SeedSnapshot(db, userId, "keyword-b", 500, daysAgo: 8);
        SeedSnapshot(db, userId, "keyword-b", 500, daysAgo: 9);
        SeedSnapshot(db, userId, "keyword-b", 600, daysAgo: 1); // 20% growth, high volume
        SeedSnapshot(db, userId, "keyword-b", 600, daysAgo: 2);
        var service = CreateService(db);

        var result = await service.FindTrendingAsync(userId, minSnapshots: 2);

        Assert.Equal(2, result.Rising.Count);
        // keyword-b has higher momentum: 20 * log(601) > 400 * log(51)
        // 20 * 6.4 = 128 vs 400 * 3.93 = 1572... actually keyword-a wins by momentum
        // Let's just verify they're in descending momentum order
        Assert.True(result.Rising[0].MomentumScore >= result.Rising[1].MomentumScore);
    }

    [Fact]
    public async Task FindTrendingAsync_CachesResult_SameGeneratedAt()
    {
        using var db = CreateContext();
        var userId = Guid.NewGuid();
        SeedSnapshot(db, userId, "cached keyword", 100, daysAgo: 8);
        SeedSnapshot(db, userId, "cached keyword", 100, daysAgo: 9);
        SeedSnapshot(db, userId, "cached keyword", 120, daysAgo: 1);
        SeedSnapshot(db, userId, "cached keyword", 120, daysAgo: 2);
        var service = CreateService(db);

        var result1 = await service.FindTrendingAsync(userId);
        var result2 = await service.FindTrendingAsync(userId);

        // Both calls return the same cached result
        Assert.Equal(result1.GeneratedAt, result2.GeneratedAt);
        Assert.Same(result1, result2);
    }
}
