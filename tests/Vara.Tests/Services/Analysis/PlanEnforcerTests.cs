using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Vara.Api.Data;
using Vara.Api.Models.Entities;
using Vara.Api.Services.Analysis;

namespace Vara.Tests.Services.Analysis;

public class PlanEnforcerTests
{
    private static VaraContext BuildDb()
    {
        var options = new DbContextOptionsBuilder<VaraContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new VaraContext(options);
    }

    private static IConfiguration BuildConfig(int creatorMonthlyUnits = 500) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Plans:creator:MonthlyLlmUnits"] = creatorMonthlyUnits.ToString()
            })
            .Build();

    private static User MakeUser(string tier, DateTime? expiresAt = null) => new()
    {
        Id = Guid.NewGuid(),
        Email = "test@example.com",
        PasswordHash = "hash",
        SubscriptionTier = tier,
        SubscriptionExpiresAt = expiresAt
    };

    // -------------------------------------------------------------------------
    // Feature access — tier checks
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EnforceAsync_FreeTier_AllowedFeature_DoesNotThrow()
    {
        await using var db = BuildDb();
        var user = MakeUser("free");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var sut = new PlanEnforcer(db, BuildConfig());

        await sut.EnforceAsync(user.Id, "keyword_research");
    }

    [Fact]
    public async Task EnforceAsync_FreeTier_LlmFeature_ThrowsFeatureAccessDenied()
    {
        await using var db = BuildDb();
        var user = MakeUser("free");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var sut = new PlanEnforcer(db, BuildConfig());

        await Assert.ThrowsAsync<FeatureAccessDeniedException>(() =>
            sut.EnforceAsync(user.Id, "llm_insights"));
    }

    [Fact]
    public async Task EnforceAsync_CreatorTier_LlmFeature_WithinQuota_DoesNotThrow()
    {
        await using var db = BuildDb();
        var user = MakeUser("creator");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var sut = new PlanEnforcer(db, BuildConfig(creatorMonthlyUnits: 500));

        await sut.EnforceAsync(user.Id, "llm_insights");
    }

    // -------------------------------------------------------------------------
    // Subscription expiry
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EnforceAsync_ExpiredSubscription_TreatedAsFreeTier()
    {
        await using var db = BuildDb();
        var user = MakeUser("creator", expiresAt: DateTime.UtcNow.AddDays(-1));
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var sut = new PlanEnforcer(db, BuildConfig());

        // creator-only feature should be denied because subscription expired
        await Assert.ThrowsAsync<FeatureAccessDeniedException>(() =>
            sut.EnforceAsync(user.Id, "llm_insights"));
    }

    [Fact]
    public async Task EnforceAsync_ActiveSubscription_NotExpired_GrantsAccess()
    {
        await using var db = BuildDb();
        var user = MakeUser("creator", expiresAt: DateTime.UtcNow.AddDays(30));
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var sut = new PlanEnforcer(db, BuildConfig());

        // Should not throw
        await sut.EnforceAsync(user.Id, "llm_insights");
    }

    [Fact]
    public async Task EnforceAsync_NoExpiryDate_NeverExpires()
    {
        await using var db = BuildDb();
        var user = MakeUser("creator", expiresAt: null);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var sut = new PlanEnforcer(db, BuildConfig());

        await sut.EnforceAsync(user.Id, "llm_insights");
    }

    // -------------------------------------------------------------------------
    // Monthly quota
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EnforceAsync_CreatorTier_QuotaExceeded_ThrowsQuotaExceeded()
    {
        await using var db = BuildDb();
        var user = MakeUser("creator");
        db.Users.Add(user);

        // Add usage logs that fill up the 10-unit quota
        var thisMonth = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        db.UsageLogs.Add(new UsageLog
        {
            UserId = user.Id,
            Feature = "llm_call",
            UnitCount = 10,
            BillingPeriod = thisMonth
        });
        await db.SaveChangesAsync();

        var sut = new PlanEnforcer(db, BuildConfig(creatorMonthlyUnits: 10));

        await Assert.ThrowsAsync<QuotaExceededException>(() =>
            sut.EnforceAsync(user.Id, "llm_insights"));
    }

    [Fact]
    public async Task EnforceAsync_QuotaFromPreviousMonth_DoesNotCountAgainstCurrent()
    {
        await using var db = BuildDb();
        var user = MakeUser("creator");
        db.Users.Add(user);

        // Usage from last month should not count
        var lastMonth = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-1);
        db.UsageLogs.Add(new UsageLog
        {
            UserId = user.Id,
            Feature = "llm_call",
            UnitCount = 999,
            BillingPeriod = lastMonth
        });
        await db.SaveChangesAsync();

        var sut = new PlanEnforcer(db, BuildConfig(creatorMonthlyUnits: 10));

        // Should not throw — previous month's usage doesn't count
        await sut.EnforceAsync(user.Id, "llm_insights");
    }

    [Fact]
    public async Task EnforceAsync_NonLlmFeature_QuotaNotChecked_EvenIfExceeded()
    {
        await using var db = BuildDb();
        var user = MakeUser("creator");
        db.Users.Add(user);

        var thisMonth = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        db.UsageLogs.Add(new UsageLog
        {
            UserId = user.Id,
            Feature = "llm_call",
            UnitCount = 9999,
            BillingPeriod = thisMonth
        });
        await db.SaveChangesAsync();

        var sut = new PlanEnforcer(db, BuildConfig(creatorMonthlyUnits: 10));

        // keyword_research is not an LLM feature — quota check skipped
        await sut.EnforceAsync(user.Id, "keyword_research");
    }

    [Fact]
    public async Task EnforceAsync_UserNotFound_ThrowsInvalidOperation()
    {
        await using var db = BuildDb();
        var sut = new PlanEnforcer(db, BuildConfig());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.EnforceAsync(Guid.NewGuid(), "keyword_research"));
    }
}
