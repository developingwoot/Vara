using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Vara.Api.Data;
using Vara.Api.Filters;
using Vara.Api.Models.DTOs;
using Vara.Api.Models.Entities;
using Vara.Api.Services.YouTube;

namespace Vara.Api.Endpoints;

public static class VideoEndpoints
{
    public static RouteGroupBuilder MapVideoEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/search", SearchVideos)
            .WithTags("Videos")
            .WithSummary("Search YouTube for videos");

        group.MapPost("/", SaveVideo)
            .AddEndpointFilter<ValidationFilter<SaveVideoRequest>>()
            .WithTags("Videos")
            .WithSummary("Save a video to your library");

        group.MapGet("/", ListVideos)
            .WithTags("Videos")
            .WithSummary("List saved videos");

        group.MapGet("/{id:guid}", GetVideo)
            .WithTags("Videos")
            .WithSummary("Get a saved video by ID");

        group.MapDelete("/{id:guid}", DeleteVideo)
            .WithTags("Videos")
            .WithSummary("Remove a video from your library");

        return group;
    }

    // -------------------------------------------------------------------------
    // GET /api/videos/search?q=keyword&maxResults=20
    // -------------------------------------------------------------------------

    private static async Task<IResult> SearchVideos(
        string q,
        int maxResults,
        IYouTubeClient youtube)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Results.BadRequest(new { error = "Query parameter 'q' is required." });

        maxResults = Math.Clamp(maxResults, 1, 50);

        var results = await youtube.SearchAsync(q, maxResults);
        var dtos = results.Select(ToSearchResult).ToList();

        return Results.Ok(dtos);
    }

    // -------------------------------------------------------------------------
    // POST /api/videos
    // -------------------------------------------------------------------------

    private static async Task<IResult> SaveVideo(
        SaveVideoRequest req,
        ClaimsPrincipal principal,
        VaraContext db,
        IYouTubeClient youtube)
    {
        var userId = GetUserId(principal);

        if (await db.Videos.AnyAsync(v => v.UserId == userId && v.YoutubeId == req.YoutubeId))
            return Results.Conflict(new { error = "This video is already in your library." });

        var metadata = await youtube.GetVideoAsync(req.YoutubeId);
        if (metadata is null)
            return Results.NotFound(new { error = "Video not found on YouTube." });

        var video = new Video
        {
            UserId = userId,
            YoutubeId = metadata.YoutubeId,
            Title = metadata.Title,
            Description = metadata.Description,
            ChannelName = metadata.ChannelName,
            ChannelId = metadata.ChannelId,
            DurationSeconds = metadata.DurationSeconds,
            UploadDate = metadata.UploadDate,
            ViewCount = metadata.ViewCount,
            LikeCount = metadata.LikeCount,
            CommentCount = metadata.CommentCount,
            ThumbnailUrl = metadata.ThumbnailUrl,
            MetadataFetchedAt = DateTime.UtcNow
        };

        db.Videos.Add(video);
        await db.SaveChangesAsync();

        return Results.Created($"/api/videos/{video.Id}", ToResponse(video));
    }

    // -------------------------------------------------------------------------
    // GET /api/videos
    // -------------------------------------------------------------------------

    private static async Task<IResult> ListVideos(
        ClaimsPrincipal principal,
        VaraContext db)
    {
        var userId = GetUserId(principal);
        var videos = await db.Videos
            .Where(v => v.UserId == userId)
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => ToResponse(v))
            .ToListAsync();

        return Results.Ok(videos);
    }

    // -------------------------------------------------------------------------
    // GET /api/videos/{id}
    // -------------------------------------------------------------------------

    private static async Task<IResult> GetVideo(
        Guid id,
        ClaimsPrincipal principal,
        VaraContext db)
    {
        var userId = GetUserId(principal);
        var video = await db.Videos
            .FirstOrDefaultAsync(v => v.Id == id && v.UserId == userId);

        return video is null ? Results.NotFound() : Results.Ok(ToResponse(video));
    }

    // -------------------------------------------------------------------------
    // DELETE /api/videos/{id}
    // -------------------------------------------------------------------------

    private static async Task<IResult> DeleteVideo(
        Guid id,
        ClaimsPrincipal principal,
        VaraContext db)
    {
        var userId = GetUserId(principal);
        var video = await db.Videos
            .FirstOrDefaultAsync(v => v.Id == id && v.UserId == userId);

        if (video is null)
            return Results.NotFound();

        db.Videos.Remove(video);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static Guid GetUserId(ClaimsPrincipal principal) =>
        Guid.Parse(principal.FindFirstValue("sub")!);

    private static VideoResponse ToResponse(Video v) => new(
        v.Id, v.YoutubeId, v.Title, v.Description, v.ChannelName, v.ChannelId,
        v.DurationSeconds, v.UploadDate, v.ViewCount, v.LikeCount,
        v.CommentCount, v.ThumbnailUrl, v.CreatedAt);

    private static VideoSearchResult ToSearchResult(VideoMetadata m) => new(
        m.YoutubeId, m.Title, m.ChannelName, m.ChannelId,
        m.DurationSeconds, m.ViewCount, m.LikeCount,
        m.CommentCount, m.ThumbnailUrl, m.UploadDate);
}
