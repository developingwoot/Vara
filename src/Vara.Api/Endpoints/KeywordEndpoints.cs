using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Vara.Api.Data;
using Vara.Api.Filters;
using Vara.Api.Models.DTOs;
using Vara.Api.Models.Entities;
using Vara.Api.Services.Analysis;

namespace Vara.Api.Endpoints;

public static class KeywordEndpoints
{
    public static RouteGroupBuilder MapKeywordEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/", AnalyzeKeyword)
            .AddEndpointFilter<ValidationFilter<AnalyzeKeywordRequest>>()
            .WithTags("Keywords")
            .WithSummary("Analyze a keyword and save results to your library");

        group.MapGet("/", ListKeywords)
            .WithTags("Keywords")
            .WithSummary("List saved keywords");

        group.MapGet("/{id:guid}", GetKeyword)
            .WithTags("Keywords")
            .WithSummary("Get a saved keyword by ID");

        group.MapDelete("/{id:guid}", DeleteKeyword)
            .WithTags("Keywords")
            .WithSummary("Remove a keyword from your library");

        return group;
    }

    // -------------------------------------------------------------------------
    // POST /api/keywords
    // -------------------------------------------------------------------------

    private static async Task<IResult> AnalyzeKeyword(
        AnalyzeKeywordRequest req,
        ClaimsPrincipal principal,
        VaraContext db,
        IKeywordAnalyzer analyzer)
    {
        var userId = GetUserId(principal);

        var analysis = await analyzer.AnalyzeAsync(req.Keyword, req.Niche);

        var existing = await db.Keywords
            .FirstOrDefaultAsync(k => k.UserId == userId
                && k.Text == req.Keyword
                && k.Niche == req.Niche);

        if (existing is null)
        {
            existing = new Keyword { UserId = userId, Text = req.Keyword, Niche = req.Niche };
            db.Keywords.Add(existing);
        }

        existing.SearchVolumeRelative = analysis.SearchVolumeRelative;
        existing.CompetitionScore     = analysis.CompetitionScore;
        existing.TrendDirection       = analysis.TrendDirection;
        existing.KeywordIntent        = analysis.KeywordIntent;
        existing.LastAnalyzed         = analysis.AnalyzedAt;

        await db.SaveChangesAsync();

        return Results.Ok(ToAnalysisResponse(existing, analysis.AnalyzedAt));
    }

    // -------------------------------------------------------------------------
    // GET /api/keywords
    // -------------------------------------------------------------------------

    private static async Task<IResult> ListKeywords(
        ClaimsPrincipal principal,
        VaraContext db)
    {
        var userId = GetUserId(principal);
        var keywords = await db.Keywords
            .Where(k => k.UserId == userId)
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => ToResponse(k))
            .ToListAsync();

        return Results.Ok(keywords);
    }

    // -------------------------------------------------------------------------
    // GET /api/keywords/{id}
    // -------------------------------------------------------------------------

    private static async Task<IResult> GetKeyword(
        Guid id,
        ClaimsPrincipal principal,
        VaraContext db)
    {
        var userId = GetUserId(principal);
        var keyword = await db.Keywords
            .FirstOrDefaultAsync(k => k.Id == id && k.UserId == userId);

        return keyword is null ? Results.NotFound() : Results.Ok(ToResponse(keyword));
    }

    // -------------------------------------------------------------------------
    // DELETE /api/keywords/{id}
    // -------------------------------------------------------------------------

    private static async Task<IResult> DeleteKeyword(
        Guid id,
        ClaimsPrincipal principal,
        VaraContext db)
    {
        var userId = GetUserId(principal);
        var keyword = await db.Keywords
            .FirstOrDefaultAsync(k => k.Id == id && k.UserId == userId);

        if (keyword is null)
            return Results.NotFound();

        db.Keywords.Remove(keyword);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static Guid GetUserId(ClaimsPrincipal principal) =>
        Guid.Parse(principal.FindFirstValue("sub")!);

    private static KeywordResponse ToResponse(Keyword k) => new(
        k.Id, k.Text, k.Niche,
        k.SearchVolumeRelative, k.CompetitionScore,
        k.TrendDirection, k.KeywordIntent,
        k.LastAnalyzed, k.CreatedAt);

    private static KeywordAnalysisResponse ToAnalysisResponse(Keyword k, DateTime analyzedAt) => new(
        k.Id, k.Text, k.Niche,
        k.SearchVolumeRelative!.Value, k.CompetitionScore!.Value,
        k.TrendDirection!, k.KeywordIntent!,
        analyzedAt);
}
