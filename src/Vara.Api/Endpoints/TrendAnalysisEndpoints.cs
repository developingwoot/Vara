using System.Security.Claims;
using Vara.Api.Models.DTOs;
using Vara.Api.Services.Analysis;

namespace Vara.Api.Endpoints;

public static class TrendAnalysisEndpoints
{
    public static RouteGroupBuilder MapTrendAnalysisEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetTrends)
            .WithTags("Trends")
            .WithSummary("Detect trending, declining, and new keywords based on your analysis history");

        return group;
    }

    // -------------------------------------------------------------------------
    // GET /api/analysis/trends?niche=&minSnapshots=2
    // -------------------------------------------------------------------------

    private static async Task<IResult> GetTrends(
        ClaimsPrincipal principal,
        ITrendDetector detector,
        string? niche = null,
        int minSnapshots = 2)
    {
        var userId = GetUserId(principal);
        minSnapshots = Math.Clamp(minSnapshots, 1, 50);

        var result = await detector.FindTrendingAsync(userId, niche, minSnapshots);

        return Results.Ok(new FindTrendingResponse(
            Rising: result.Rising.Select(ToDto).ToList(),
            Declining: result.Declining.Select(ToDto).ToList(),
            New: result.New.Select(ToDto).ToList(),
            GeneratedAt: result.GeneratedAt));
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static Guid GetUserId(ClaimsPrincipal principal) =>
        Guid.Parse(
            principal.FindFirstValue("sub")
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found."));

    private static TrendingKeywordDto ToDto(TrendingKeyword t) => new(
        t.Keyword, t.Niche, t.CurrentVolume, t.PreviousVolume,
        t.GrowthRate, t.MomentumScore, t.Lifecycle, t.LastCaptured);
}
