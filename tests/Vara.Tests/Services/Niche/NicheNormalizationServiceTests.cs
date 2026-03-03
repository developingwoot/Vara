using Microsoft.EntityFrameworkCore;
using Vara.Api.Data;
using Vara.Api.Models.Entities;
using Vara.Api.Services;

namespace Vara.Tests.Services.Niche;

public class NicheNormalizationServiceTests : IAsyncLifetime
{
    private VaraContext _db = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<VaraContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new VaraContext(options);

        // Seed a small set of canonical niches matching production seed data patterns
        _db.CanonicalNiches.AddRange(
            new CanonicalNiche { Id = 1, Name = "Personal Finance",     Slug = "personal-finance",     Aliases = ["finance", "money", "budgeting", "investing"], IsActive = true },
            new CanonicalNiche { Id = 2, Name = "Web Development",      Slug = "web-development",      Aliases = ["web dev", "coding", "programming"],           IsActive = true },
            new CanonicalNiche { Id = 3, Name = "Fitness & Health",     Slug = "fitness-health",       Aliases = ["fitness", "health", "workout", "gym"],        IsActive = true },
            new CanonicalNiche { Id = 4, Name = "Gaming",               Slug = "gaming",               Aliases = ["games", "video games", "game dev"],           IsActive = true },
            new CanonicalNiche { Id = 5, Name = "Cooking & Food",       Slug = "cooking-food",         Aliases = ["cooking", "food", "recipes", "baking"],       IsActive = true },
            new CanonicalNiche { Id = 6, Name = "Inactive Niche",       Slug = "inactive",             Aliases = [],                                             IsActive = false }
        );
        await _db.SaveChangesAsync();
    }

    public async Task DisposeAsync() => await _db.DisposeAsync();

    private NicheNormalizationService BuildSut() => new(_db);

    // -------------------------------------------------------------------------
    // Resolve — exact and high-confidence matches
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Resolve_ExactNameMatch_ReturnsNiche()
    {
        var sut = BuildSut();
        var result = await sut.ResolveAsync("Personal Finance");
        Assert.NotNull(result);
        Assert.Equal(1, result!.Value.Niche.Id);
        Assert.True(result.Value.Confidence >= 0.85);
    }

    [Fact]
    public async Task Resolve_AliasMatch_ReturnsNiche()
    {
        var sut = BuildSut();
        // "budgeting" is an alias for Personal Finance
        var result = await sut.ResolveAsync("budgeting");
        Assert.NotNull(result);
        Assert.Equal(1, result!.Value.Niche.Id);
    }

    [Fact]
    public async Task Resolve_FuzzyTypo_ReturnsCorrectNiche()
    {
        var sut = BuildSut();
        // "persnal finance" — one char deletion should still match
        var result = await sut.ResolveAsync("persnal finance");
        Assert.NotNull(result);
        Assert.Equal(1, result!.Value.Niche.Id);
    }

    [Fact]
    public async Task Resolve_AliasPhrase_WebDev_ReturnsWebDevelopment()
    {
        var sut = BuildSut();
        var result = await sut.ResolveAsync("web dev");
        Assert.NotNull(result);
        Assert.Equal(2, result!.Value.Niche.Id);
    }

    [Fact]
    public async Task Resolve_CaseInsensitive_ReturnsNiche()
    {
        var sut = BuildSut();
        var result = await sut.ResolveAsync("WEB DEVELOPMENT");
        Assert.NotNull(result);
        Assert.Equal(2, result!.Value.Niche.Id);
    }

    // -------------------------------------------------------------------------
    // Resolve — no match
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Resolve_CompletelyUnrelated_ReturnsNull()
    {
        var sut = BuildSut();
        // "zzzzqqqq" cannot match anything above threshold
        var result = await sut.ResolveAsync("zzzzqqqq");
        Assert.Null(result);
    }

    [Fact]
    public async Task Resolve_EmptyString_ReturnsNull()
    {
        var sut = BuildSut();
        var result = await sut.ResolveAsync("   ");
        Assert.Null(result);
    }

    // -------------------------------------------------------------------------
    // Inactive niches are excluded
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Resolve_InactiveNiche_IsNotReturned()
    {
        var sut = BuildSut();
        var result = await sut.ResolveAsync("Inactive Niche");
        // The inactive niche should not be returned even on perfect name match
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllActive_ExcludesInactiveNiches()
    {
        var sut = BuildSut();
        var niches = await sut.GetAllActiveAsync();
        Assert.DoesNotContain(niches, n => n.Slug == "inactive");
        Assert.Equal(5, niches.Count);
    }

    // -------------------------------------------------------------------------
    // GetSuggestions
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetSuggestions_ReturnsOrderedByConfidence()
    {
        var sut = BuildSut();
        var suggestions = await sut.GetSuggestionsAsync("coding", 3);
        Assert.NotEmpty(suggestions);
        // Web Development has "coding" as an alias — should appear first
        Assert.Equal(2, suggestions[0].Niche.Id);
        // Confidence descending
        for (int i = 1; i < suggestions.Count; i++)
            Assert.True(suggestions[i - 1].Confidence >= suggestions[i].Confidence);
    }

    [Fact]
    public async Task GetSuggestions_RespectsCountLimit()
    {
        var sut = BuildSut();
        var suggestions = await sut.GetSuggestionsAsync("finance", count: 2);
        Assert.True(suggestions.Count <= 2);
    }

    // -------------------------------------------------------------------------
    // Confidence is in valid range
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Resolve_ConfidenceIsInValidRange()
    {
        var sut = BuildSut();
        var suggestions = await sut.GetSuggestionsAsync("fitness");
        foreach (var (_, confidence) in suggestions)
        {
            Assert.InRange(confidence, 0.0, 1.0);
        }
    }
}
