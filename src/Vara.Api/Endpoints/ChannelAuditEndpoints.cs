using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Vara.Api.Data;
using Vara.Api.Models.DTOs;
using Vara.Api.Services.Analysis;

namespace Vara.Api.Endpoints;

public static class ChannelAuditEndpoints
{
    public static RouteGroupBuilder MapChannelAuditEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}/quick-scan", QuickScan)
            .WithTags("Channels")
            .WithSummary("Get a Quick Scan audit comparing recent vs top-performing videos");

        group.MapPost("/{id:guid}/deep-audit", DeepAudit)
            .WithTags("Channels")
            .WithSummary("Run an AI-powered deep audit comparing recent vs outlier video transcripts (Creator tier)");

        group.MapGet("/{id:guid}/videos", ListChannelVideos)
            .WithTags("Channels")
            .WithSummary("List synced videos for a tracked channel");

        group.MapPost("/videos/compare", CompareVideos)
            .WithTags("Channels")
            .WithSummary("Compare transcripts of two videos side-by-side using AI (Creator tier)");

        return group;
    }

    private static async Task<IResult> QuickScan(
        Guid id,
        ClaimsPrincipal principal,
        IChannelAuditService service,
        CancellationToken ct)
    {
        var userId = GetUserId(principal);
        try
        {
            var result = await service.QuickScanAsync(userId, id, ct);
            return Results.Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
    }

    private static async Task<IResult> DeepAudit(
        Guid id,
        ClaimsPrincipal principal,
        IChannelAuditService service,
        CancellationToken ct)
    {
        var userId = GetUserId(principal);
        try
        {
            var result = await service.DeepAuditAsync(userId, id, ct);
            return Results.Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
    }

    private static async Task<IResult> ListChannelVideos(
        Guid id,
        ClaimsPrincipal principal,
        VaraContext db,
        CancellationToken ct)
    {
        var userId = GetUserId(principal);

        var channel = await db.TrackedChannels
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct);

        if (channel is null)
            return Results.NotFound();

        var videos = await db.Videos
            .AsNoTracking()
            .Where(v => v.UserId == userId && v.ChannelId == channel.YoutubeChannelId)
            .OrderByDescending(v => v.UploadDate)
            .Select(v => new
            {
                youtubeId = v.YoutubeId,
                title = v.Title,
                viewCount = v.ViewCount,
                uploadDate = v.UploadDate,
                thumbnailUrl = v.ThumbnailUrl,
                durationSeconds = v.DurationSeconds
            })
            .ToListAsync(ct);

        return Results.Ok(videos);
    }

    private static async Task<IResult> CompareVideos(
        CompareVideosRequest req,
        ClaimsPrincipal principal,
        IChannelAuditService service,
        CancellationToken ct)
    {
        var userId = GetUserId(principal);
        try
        {
            var result = await service.CompareVideosAsync(userId, req.Video1Id, req.Video2Id, ct);
            return Results.Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
    }

    private static Guid GetUserId(ClaimsPrincipal principal) =>
        Guid.Parse(
            principal.FindFirstValue("sub")
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found."));
}
