using Microsoft.EntityFrameworkCore;
using Vara.Api.Data;
using Vara.Api.Models.Entities;
using Vara.Api.Services;

namespace Vara.Api.Endpoints;

public static class CanonicalNicheEndpoints
{
    // -------------------------------------------------------------------------
    // Public niche routes: /api/niches
    // -------------------------------------------------------------------------

    public static RouteGroupBuilder MapNicheListEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", ListNiches)
            .WithTags("Niches")
            .WithSummary("List all active canonical niches");

        group.MapPost("/resolve", ResolveNiche)
            .WithTags("Niches")
            .WithSummary("Resolve a free-text niche to the best canonical match");

        return group;
    }

    // -------------------------------------------------------------------------
    // Admin niche routes: /api/admin/niches
    // -------------------------------------------------------------------------

    public static RouteGroupBuilder MapAdminNicheEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", AdminListNiches)
            .WithTags("Admin")
            .WithSummary("List all canonical niches including inactive (admin only)");

        group.MapPost("/", AdminCreateNiche)
            .WithTags("Admin")
            .WithSummary("Create a new canonical niche (admin only)");

        group.MapPut("/{id:int}", AdminUpdateNiche)
            .WithTags("Admin")
            .WithSummary("Update a canonical niche's name, slug or aliases (admin only)");

        group.MapDelete("/{id:int}", AdminDeleteNiche)
            .WithTags("Admin")
            .WithSummary("Soft-delete a canonical niche by setting it inactive (admin only)");

        return group;
    }

    // -------------------------------------------------------------------------
    // Handlers — public
    // -------------------------------------------------------------------------

    private static async Task<IResult> ListNiches(INicheNormalizationService nicheService) =>
        Results.Ok(await nicheService.GetAllActiveAsync());

    private static async Task<IResult> ResolveNiche(
        ResolveNicheRequest req,
        INicheNormalizationService nicheService)
    {
        if (string.IsNullOrWhiteSpace(req.Niche))
            return Results.BadRequest(new { error = "Niche text is required." });

        var resolved = await nicheService.ResolveAsync(req.Niche);
        if (resolved is not null)
            return Results.Ok(new
            {
                matched = true,
                niche = new { resolved.Value.Niche.Id, resolved.Value.Niche.Name, resolved.Value.Niche.Slug },
                confidence = Math.Round(resolved.Value.Confidence, 3)
            });

        var suggestions = await nicheService.GetSuggestionsAsync(req.Niche, 5);
        return Results.UnprocessableEntity(new
        {
            matched = false,
            error = $"Could not match '{req.Niche}' to a canonical niche (confidence below threshold).",
            suggestions = suggestions.Select(s => new
            {
                s.Niche.Id, s.Niche.Name, s.Niche.Slug,
                confidence = Math.Round(s.Confidence, 3)
            })
        });
    }

    // -------------------------------------------------------------------------
    // Handlers — admin
    // -------------------------------------------------------------------------

    private static async Task<IResult> AdminListNiches(VaraContext db)
    {
        var niches = await db.CanonicalNiches
            .AsNoTracking()
            .OrderBy(n => n.Name)
            .ToListAsync();
        return Results.Ok(niches);
    }

    private static async Task<IResult> AdminCreateNiche(
        AdminNicheRequest req,
        VaraContext db)
    {
        if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Slug))
            return Results.BadRequest(new { error = "Name and slug are required." });

        if (await db.CanonicalNiches.AnyAsync(n => n.Slug == req.Slug))
            return Results.Conflict(new { error = $"A niche with slug '{req.Slug}' already exists." });

        var niche = new CanonicalNiche
        {
            Name = req.Name.Trim(),
            Slug = req.Slug.Trim().ToLowerInvariant(),
            Aliases = req.Aliases ?? [],
            IsActive = true
        };

        db.CanonicalNiches.Add(niche);
        await db.SaveChangesAsync();
        return Results.Created($"/api/admin/niches/{niche.Id}", niche);
    }

    private static async Task<IResult> AdminUpdateNiche(
        int id,
        AdminNicheRequest req,
        VaraContext db)
    {
        var niche = await db.CanonicalNiches.FindAsync(id);
        if (niche is null) return Results.NotFound();

        if (!string.IsNullOrWhiteSpace(req.Name)) niche.Name = req.Name.Trim();
        if (!string.IsNullOrWhiteSpace(req.Slug)) niche.Slug = req.Slug.Trim().ToLowerInvariant();
        if (req.Aliases is not null) niche.Aliases = req.Aliases;

        await db.SaveChangesAsync();
        return Results.Ok(niche);
    }

    private static async Task<IResult> AdminDeleteNiche(int id, VaraContext db)
    {
        var niche = await db.CanonicalNiches.FindAsync(id);
        if (niche is null) return Results.NotFound();

        niche.IsActive = false;
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
}

// -------------------------------------------------------------------------
// Request records
// -------------------------------------------------------------------------

public record ResolveNicheRequest(string Niche);
public record AdminNicheRequest(string? Name, string? Slug, string[]? Aliases);
