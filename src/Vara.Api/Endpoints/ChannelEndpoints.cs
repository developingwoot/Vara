using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Vara.Api.Data;
using Vara.Api.Filters;
using Vara.Api.Models.DTOs;
using Vara.Api.Models.Entities;
using Vara.Api.Services.YouTube;

namespace Vara.Api.Endpoints;

public static class ChannelEndpoints
{
    public static RouteGroupBuilder MapChannelEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/", AddChannel)
            .AddEndpointFilter<ValidationFilter<AddChannelRequest>>()
            .WithTags("Channels")
            .WithSummary("Track a YouTube channel");

        group.MapGet("/", ListChannels)
            .WithTags("Channels")
            .WithSummary("List tracked channels");

        group.MapGet("/{id:guid}/stats", GetStats)
            .WithTags("Channels")
            .WithSummary("Get aggregated stats for a tracked channel");

        group.MapPost("/{id:guid}/sync", SyncChannel)
            .WithTags("Channels")
            .WithSummary("Crawl all channel videos into the library");

        group.MapDelete("/{id:guid}", DeleteChannel)
            .WithTags("Channels")
            .WithSummary("Stop tracking a channel");

        return group;
    }

    // -------------------------------------------------------------------------
    // POST /api/channels
    // -------------------------------------------------------------------------

    private static async Task<IResult> AddChannel(
        AddChannelRequest req,
        ClaimsPrincipal principal,
        VaraContext db,
        IYouTubeClient youtube)
    {
        var userId = GetUserId(principal);

        var channel = await youtube.GetChannelAsync(req.HandleOrUrl);
        if (channel is null)
            return Results.NotFound(new { error = "Channel not found on YouTube." });

        if (await db.TrackedChannels.AnyAsync(c => c.UserId == userId && c.YoutubeChannelId == channel.YoutubeChannelId))
            return Results.Conflict(new { error = "You are already tracking this channel." });

        var entity = new TrackedChannel
        {
            UserId = userId,
            YoutubeChannelId = channel.YoutubeChannelId,
            Handle = channel.Handle,
            DisplayName = channel.DisplayName,
            ThumbnailUrl = channel.ThumbnailUrl,
            SubscriberCount = channel.SubscriberCount,
            VideoCount = channel.VideoCount,
            TotalViewCount = channel.TotalViewCount,
            IsOwner = req.IsOwner
        };

        db.TrackedChannels.Add(entity);
        await db.SaveChangesAsync();

        return Results.Created($"/api/channels/{entity.Id}", ToResponse(entity));
    }

    // -------------------------------------------------------------------------
    // GET /api/channels
    // -------------------------------------------------------------------------

    private static async Task<IResult> ListChannels(
        ClaimsPrincipal principal,
        VaraContext db)
    {
        var userId = GetUserId(principal);
        var channels = await db.TrackedChannels
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.AddedAt)
            .Select(c => ToResponse(c))
            .ToListAsync();

        return Results.Ok(channels);
    }

    // -------------------------------------------------------------------------
    // GET /api/channels/{id}/stats
    // -------------------------------------------------------------------------

    private static async Task<IResult> GetStats(
        Guid id,
        ClaimsPrincipal principal,
        VaraContext db)
    {
        var userId = GetUserId(principal);
        var channel = await db.TrackedChannels
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (channel is null)
            return Results.NotFound();

        var videos = await db.Videos
            .Where(v => v.UserId == userId && v.ChannelId == channel.YoutubeChannelId)
            .Select(v => new { v.YoutubeId, v.Title, v.ViewCount, v.UploadDate })
            .ToListAsync();

        if (videos.Count == 0)
            return Results.Ok(new ChannelStatsResponse(0, 0, [], [], 0));

        var viewCounts = videos.Select(v => (double)v.ViewCount).OrderBy(x => x).ToList();
        var avgViews = viewCounts.Average();
        var medianViews = viewCounts.Count % 2 == 0
            ? (viewCounts[viewCounts.Count / 2 - 1] + viewCounts[viewCounts.Count / 2]) / 2.0
            : viewCounts[viewCounts.Count / 2];

        var topVideos = videos
            .OrderByDescending(v => v.ViewCount)
            .Take(5)
            .Select(v => new VideoSummary(v.YoutubeId, v.Title, v.ViewCount))
            .ToList();

        var bottomVideos = videos
            .OrderBy(v => v.ViewCount)
            .Take(5)
            .Select(v => new VideoSummary(v.YoutubeId, v.Title, v.ViewCount))
            .ToList();

        var postsPerMonth = 0.0;
        var datedVideos = videos.Where(v => v.UploadDate.HasValue).ToList();
        if (datedVideos.Count > 1)
        {
            var earliest = datedVideos.Min(v => v.UploadDate!.Value);
            var latest = datedVideos.Max(v => v.UploadDate!.Value);
            var months = (latest - earliest).TotalDays / 30.0;
            postsPerMonth = months > 0 ? datedVideos.Count / months : datedVideos.Count;
        }

        return Results.Ok(new ChannelStatsResponse(avgViews, medianViews, topVideos, bottomVideos, postsPerMonth));
    }

    // -------------------------------------------------------------------------
    // POST /api/channels/{id}/sync
    // -------------------------------------------------------------------------

    private static async Task<IResult> SyncChannel(
        Guid id,
        ClaimsPrincipal principal,
        VaraContext db,
        IYouTubeClient youtube)
    {
        var userId = GetUserId(principal);
        var channel = await db.TrackedChannels
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (channel is null)
            return Results.NotFound();

        var savedCount = 0;
        await foreach (var videoId in youtube.GetChannelVideoIdsAsync(channel.YoutubeChannelId))
        {
            var metadata = await youtube.GetVideoAsync(videoId);
            if (metadata is null) continue;

            var existing = await db.Videos.FirstOrDefaultAsync(
                v => v.UserId == userId && v.YoutubeId == videoId);

            if (existing is null)
            {
                db.Videos.Add(new Vara.Api.Models.Entities.Video
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
                });
                savedCount++;
            }
            else
            {
                // Refresh metadata on re-sync
                existing.ViewCount = metadata.ViewCount;
                existing.LikeCount = metadata.LikeCount;
                existing.CommentCount = metadata.CommentCount;
                existing.MetadataFetchedAt = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync();

        channel.LastSyncedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(new { synced = savedCount });
    }

    // -------------------------------------------------------------------------
    // DELETE /api/channels/{id}
    // -------------------------------------------------------------------------

    private static async Task<IResult> DeleteChannel(
        Guid id,
        ClaimsPrincipal principal,
        VaraContext db)
    {
        var userId = GetUserId(principal);
        var channel = await db.TrackedChannels
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (channel is null)
            return Results.NotFound();

        db.TrackedChannels.Remove(channel);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static Guid GetUserId(ClaimsPrincipal principal) =>
        Guid.Parse(principal.FindFirstValue("sub")!);

    private static ChannelResponse ToResponse(TrackedChannel c) => new(
        c.Id, c.YoutubeChannelId, c.Handle, c.DisplayName, c.ThumbnailUrl,
        c.SubscriberCount, c.VideoCount, c.TotalViewCount,
        c.IsOwner, c.IsVerified, c.LastSyncedAt, c.AddedAt);
}
