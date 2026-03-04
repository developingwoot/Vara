using Microsoft.EntityFrameworkCore;
using Vara.Api.Data;
using Vara.Api.Models.DTOs;
using Vara.Api.Services.Llm;
using Vara.Api.Services.YouTube;

namespace Vara.Api.Services.Analysis;

public interface IChannelAuditService
{
    Task<ChannelQuickScanResult> QuickScanAsync(Guid userId, Guid channelId, CancellationToken ct = default);
    Task<ChannelDeepAuditResult> DeepAuditAsync(Guid userId, Guid channelId, CancellationToken ct = default);
    Task<VideoComparisonResult> CompareVideosAsync(Guid userId, string video1Id, string video2Id, CancellationToken ct = default);
}

public class ChannelAuditService(
    VaraContext db,
    IYouTubeClient youtube,
    ILlmOrchestrator llm,
    IPlanEnforcer planEnforcer,
    IUsageMeter usageMeter,
    ILogger<ChannelAuditService> logger) : IChannelAuditService
{
    private const int MaxTranscriptChars = 24_000;

    // ─── Quick Scan ──────────────────────────────────────────────────────────

    public async Task<ChannelQuickScanResult> QuickScanAsync(
        Guid userId, Guid channelId, CancellationToken ct = default)
    {
        var channel = await db.TrackedChannels
            .FirstOrDefaultAsync(c => c.Id == channelId && c.UserId == userId, ct)
            ?? throw new KeyNotFoundException($"Channel {channelId} not found.");

        var videos = await db.Videos
            .AsNoTracking()
            .Where(v => v.UserId == userId && v.ChannelId == channel.YoutubeChannelId)
            .OrderByDescending(v => v.UploadDate)
            .ToListAsync(ct);

        if (videos.Count == 0)
        {
            return new ChannelQuickScanResult(
                channelId, channel.DisplayName,
                HasVideos: false,
                IsSynced: channel.LastSyncedAt.HasValue,
                TotalVideos: 0,
                OverallScore: 0,
                VaraAssessment: "Sync this channel to unlock your performance audit.",
                ViewsComparison: new(0, 0, 0, "below"),
                EngagementComparison: new(0, 0, 0, "below"),
                PostingStats: new(0, null, 0, "inactive"),
                Priorities: [new(1, "critical", "Sync your channel", "Tap Sync to pull in your video library so VARA can start analysing your performance.")],
                Badges: [],
                RecentVideos: [],
                TopVideos: [],
                GeneratedAt: DateTime.UtcNow);
        }

        // Split into recent 5 vs top 5 by views
        var recentVideos = videos.Take(5).ToList();
        var topVideos = videos.OrderByDescending(v => v.ViewCount).Take(5).ToList();

        // ── Views comparison
        var recentAvgViews = recentVideos.Average(v => (double)v.ViewCount);
        var topAvgViews = topVideos.Average(v => (double)v.ViewCount);
        var viewsGap = topAvgViews > 0 ? (recentAvgViews - topAvgViews) / topAvgViews * 100 : 0;
        var viewsTrend = viewsGap >= -10 ? "above" : viewsGap >= -30 ? "on-par" : "below";
        var viewsComparison = new ChannelMetricComparison(recentAvgViews, topAvgViews, viewsGap, viewsTrend);

        // ── Engagement comparison (likes + comments / views)
        static double EngagementRate(Models.Entities.Video v) =>
            v.ViewCount > 0 ? (v.LikeCount + v.CommentCount) / (double)v.ViewCount * 100 : 0;

        var recentAvgEngagement = recentVideos.Average(EngagementRate);
        var topAvgEngagement = topVideos.Average(EngagementRate);
        var engGap = topAvgEngagement > 0 ? (recentAvgEngagement - topAvgEngagement) / topAvgEngagement * 100 : 0;
        var engTrend = engGap >= -10 ? "above" : engGap >= -30 ? "on-par" : "below";
        var engagementComparison = new ChannelMetricComparison(recentAvgEngagement, topAvgEngagement, engGap, engTrend);

        // ── Posting frequency
        var datedVideos = videos.Where(v => v.UploadDate.HasValue).ToList();
        var postsPerMonth = 0.0;
        if (datedVideos.Count > 1)
        {
            var earliest = datedVideos.Min(v => v.UploadDate!.Value);
            var latest = datedVideos.Max(v => v.UploadDate!.Value);
            var months = (latest - earliest).TotalDays / 30.0;
            postsPerMonth = months > 0 ? datedVideos.Count / months : datedVideos.Count;
        }

        var lastUpload = datedVideos.Count > 0 ? datedVideos.Max(v => v.UploadDate) : null;
        var daysSinceLast = lastUpload.HasValue ? (int)(DateTime.UtcNow - lastUpload.Value).TotalDays : 9999;
        var consistency = daysSinceLast > 90 ? "inactive"
            : postsPerMonth >= 2 ? "regular"
            : "irregular";

        var postingStats = new PostingFrequencyStats(postsPerMonth, lastUpload, daysSinceLast, consistency);

        // ── Health score (weighted)
        // Views score: how close recent avg is to top avg (100 = matching, 0 = <30% of top)
        var viewsScore = topAvgViews > 0
            ? Math.Clamp(recentAvgViews / topAvgViews * 100, 0, 100)
            : 50.0;

        // Engagement score
        var engScore = topAvgEngagement > 0
            ? Math.Clamp(recentAvgEngagement / topAvgEngagement * 100, 0, 100)
            : 50.0;

        // Frequency score
        var freqScore = postsPerMonth >= 4 ? 100
            : postsPerMonth >= 2 ? 75
            : postsPerMonth >= 1 ? 50
            : 25.0;
        if (daysSinceLast > 90) freqScore = Math.Min(freqScore, 25);

        var overallScore = (int)Math.Round(viewsScore * 0.5 + engScore * 0.3 + freqScore * 0.2);

        // ── VARA assessment text (template-based, no LLM cost)
        var assessment = BuildAssessmentText(overallScore, viewsGap, engGap, daysSinceLast, postsPerMonth);

        // ── Priorities
        var priorities = BuildPriorities(viewsGap, engGap, daysSinceLast, postsPerMonth, videos.Count);

        // ── Badges
        var badges = BuildBadges(videos.Count, postsPerMonth, channel.SubscriberCount ?? 0, recentAvgViews, topAvgViews, recentAvgEngagement, topAvgEngagement);

        // ── Video snapshots
        VideoSnapshotDto ToSnapshot(Models.Entities.Video v) => new(
            v.YoutubeId, v.Title, v.ViewCount, v.LikeCount, v.CommentCount,
            v.DurationSeconds, v.UploadDate, v.ThumbnailUrl, EngagementRate(v));

        return new ChannelQuickScanResult(
            channelId, channel.DisplayName,
            HasVideos: true,
            IsSynced: channel.LastSyncedAt.HasValue,
            TotalVideos: videos.Count,
            OverallScore: overallScore,
            VaraAssessment: assessment,
            ViewsComparison: viewsComparison,
            EngagementComparison: engagementComparison,
            PostingStats: postingStats,
            Priorities: priorities,
            Badges: badges,
            RecentVideos: recentVideos.Select(ToSnapshot).ToList(),
            TopVideos: topVideos.Select(ToSnapshot).ToList(),
            GeneratedAt: DateTime.UtcNow);
    }

    // ─── Deep Audit ──────────────────────────────────────────────────────────

    public async Task<ChannelDeepAuditResult> DeepAuditAsync(
        Guid userId, Guid channelId, CancellationToken ct = default)
    {
        await planEnforcer.EnforceAsync(userId, "transcripts", ct);

        var channel = await db.TrackedChannels
            .FirstOrDefaultAsync(c => c.Id == channelId && c.UserId == userId, ct)
            ?? throw new KeyNotFoundException($"Channel {channelId} not found.");

        var allVideos = await db.Videos
            .Where(v => v.UserId == userId && v.ChannelId == channel.YoutubeChannelId)
            .ToListAsync(ct);

        if (allVideos.Count == 0)
            return new ChannelDeepAuditResult(channelId, null, null, false, null, DateTime.UtcNow);

        // Most recent video + top video by views
        var recentVideo = allVideos.OrderByDescending(v => v.UploadDate).First();
        var topVideo = allVideos.OrderByDescending(v => v.ViewCount).First();

        // Fetch transcripts (DB cache first, then YouTube)
        var recentTranscript = await GetOrFetchTranscriptAsync(recentVideo, userId, ct);
        var topTranscript = await GetOrFetchTranscriptAsync(topVideo, userId, ct);

        if (recentTranscript is null && topTranscript is null)
            return new ChannelDeepAuditResult(channelId, recentVideo.Title, topVideo.Title, false, null, DateTime.UtcNow);

        var t1 = Truncate(recentTranscript ?? "(transcript unavailable)");
        var t2 = Truncate(topTranscript ?? "(transcript unavailable)");

        var prompt = PromptTemplates.ChannelDeepAudit(
            recentVideo.Title, t1,
            topVideo.Title, t2);

        var response = await llm.ExecuteAsync(
            "ChannelDeepAudit", prompt, new LlmOptions(MaxTokens: 800), ct);

        await usageMeter.RecordLlmCallAsync(userId, "ChannelDeepAudit", ct);

        logger.LogInformation("Deep audit for channel {ChannelId} (user {UserId}), cost ${Cost:F4}",
            channelId, userId, response.CostUsd);

        return new ChannelDeepAuditResult(
            channelId, recentVideo.Title, topVideo.Title,
            TranscriptsAvailable: recentTranscript is not null || topTranscript is not null,
            LlmAnalysis: response.Content,
            GeneratedAt: DateTime.UtcNow);
    }

    // ─── Video Comparison ────────────────────────────────────────────────────

    public async Task<VideoComparisonResult> CompareVideosAsync(
        Guid userId, string video1Id, string video2Id, CancellationToken ct = default)
    {
        await planEnforcer.EnforceAsync(userId, "transcripts", ct);

        var video1 = await db.Videos
            .FirstOrDefaultAsync(v => v.UserId == userId && v.YoutubeId == video1Id, ct)
            ?? throw new KeyNotFoundException($"Video {video1Id} not found in your library. Ensure the channel is synced.");

        var video2 = await db.Videos
            .FirstOrDefaultAsync(v => v.UserId == userId && v.YoutubeId == video2Id, ct)
            ?? throw new KeyNotFoundException($"Video {video2Id} not found in your library. Ensure the channel is synced.");

        var transcript1 = await GetOrFetchTranscriptAsync(video1, userId, ct);
        var transcript2 = await GetOrFetchTranscriptAsync(video2, userId, ct);

        if (transcript1 is null && transcript2 is null)
            return new VideoComparisonResult(video1Id, video1.Title, video2Id, video2.Title, false, null, DateTime.UtcNow);

        var t1 = Truncate(transcript1 ?? "(transcript unavailable)");
        var t2 = Truncate(transcript2 ?? "(transcript unavailable)");

        var prompt = PromptTemplates.VideoComparison(video1.Title, t1, video2.Title, t2);

        var response = await llm.ExecuteAsync(
            "VideoComparison", prompt, new LlmOptions(MaxTokens: 800), ct);

        await usageMeter.RecordLlmCallAsync(userId, "VideoComparison", ct);

        logger.LogInformation("Video comparison {V1} vs {V2} (user {UserId}), cost ${Cost:F4}",
            video1Id, video2Id, userId, response.CostUsd);

        return new VideoComparisonResult(
            video1Id, video1.Title, video2Id, video2.Title,
            TranscriptsAvailable: true,
            LlmAnalysis: response.Content,
            GeneratedAt: DateTime.UtcNow);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private async Task<string?> GetOrFetchTranscriptAsync(
        Models.Entities.Video video, Guid userId, CancellationToken ct)
    {
        if (video.TranscriptText is not null)
            return video.TranscriptText;

        var transcript = await youtube.GetTranscriptAsync(video.YoutubeId, ct);
        if (transcript is not null)
        {
            video.TranscriptText = transcript;
            await db.SaveChangesAsync(ct);
        }

        return transcript;
    }

    private static string Truncate(string text) =>
        text.Length > MaxTranscriptChars ? text[..MaxTranscriptChars] : text;

    private static string BuildAssessmentText(
        int score, double viewsGap, double engGap, int daysSinceLast, double postsPerMonth)
    {
        var opening = score switch
        {
            >= 80 => "Your recent videos are performing strongly, close to your all-time best.",
            >= 60 => "Your channel has a solid track record, but your recent uploads are dipping below your peak performance.",
            >= 40 => "There is a noticeable gap between your recent videos and your top performers — this is your biggest opportunity.",
            _ => "Your recent videos are significantly underperforming compared to your channel's best content."
        };

        var focus = daysSinceLast > 60 ? " Getting back to a consistent upload schedule should be your first move."
            : viewsGap < -30 ? " Focus on understanding what made your top videos resonate and replicate that energy."
            : engGap < -20 ? " Your engagement rate is slipping — lean into CTAs and community interaction."
            : " Keep the momentum going and double down on what's working.";

        return opening + focus;
    }

    private static List<ActionablePriority> BuildPriorities(
        double viewsGap, double engGap, int daysSinceLast, double postsPerMonth, int totalVideos)
    {
        var priorities = new List<(int weight, ActionablePriority p)>();

        if (daysSinceLast > 60)
            priorities.Add((100, new(0, "critical", "Resume uploading",
                $"You haven't posted in {daysSinceLast} days. Consistent posting is the single biggest lever for channel growth.")));

        if (viewsGap < -40)
            priorities.Add((90, new(0, "critical", "Improve view count on new videos",
                $"Your recent videos average {Math.Abs(viewsGap):F0}% fewer views than your top performers. Study what hooks and thumbnails drove your best results.")));
        else if (viewsGap < -20)
            priorities.Add((70, new(0, "improve", "Close the views gap",
                $"Your recent videos are {Math.Abs(viewsGap):F0}% below your channel peak. Try A/B testing titles and thumbnails.")));

        if (engGap < -30)
            priorities.Add((80, new(0, "critical", "Re-engage your audience",
                "Engagement on recent videos is well below your historical best. End each video with a clear, specific call-to-action.")));
        else if (engGap < -15)
            priorities.Add((60, new(0, "improve", "Boost engagement",
                "Your recent engagement rate is slightly down. Try pinning a comment, responding to viewers, or adding a community poll.")));

        if (postsPerMonth < 1 && daysSinceLast <= 60)
            priorities.Add((50, new(0, "improve", "Post more consistently",
                $"You're averaging {postsPerMonth:F1} video/month. Even one extra upload per month compounds significantly over a year.")));

        if (totalVideos < 10)
            priorities.Add((40, new(0, "improve", "Build your content library",
                "With fewer than 10 videos, the algorithm has limited data to recommend your channel. More content creates more discovery opportunities.")));

        // Always include a "maintain" if things are going well
        if (viewsGap >= -10 && engGap >= -10 && daysSinceLast <= 30)
            priorities.Add((30, new(0, "maintain", "Keep up the great work",
                "Your recent videos are performing at or near your channel's best. Stay consistent and keep iterating on what's working.")));

        var ranked = priorities
            .OrderByDescending(x => x.weight)
            .Take(3)
            .Select((x, i) => x.p with { Rank = i + 1 })
            .ToList();

        return ranked;
    }

    private static List<ChannelBadge> BuildBadges(
        int totalVideos, double postsPerMonth, long subscriberCount,
        double recentAvgViews, double topAvgViews,
        double recentAvgEngagement, double topAvgEngagement)
    {
        var badges = new List<ChannelBadge>();

        // Achievement: Century Club
        if (totalVideos >= 100)
            badges.Add(new("century-club", "Century Club", "100+ videos published",
                "achievement", "silver", "🏛️", true, null));

        // Achievement: Consistent Publisher
        var publisherTier = postsPerMonth >= 4 ? "gold"
            : postsPerMonth >= 2 ? "silver"
            : postsPerMonth >= 1 ? "bronze"
            : null;
        if (publisherTier is not null)
            badges.Add(new("consistent-publisher", "Consistent Publisher",
                $"Averaging {postsPerMonth:F1} video{(postsPerMonth != 1 ? "s" : "")} per month",
                "achievement", publisherTier, "📅", true, null));

        // Achievement: Rising Star
        if (subscriberCount >= 10_000)
            badges.Add(new("rising-star", "Rising Star", "10K+ subscribers",
                "achievement", "bronze", "⭐", true, null));

        if (subscriberCount >= 100_000)
            badges.Add(new("silver-play", "Silver Play", "100K+ subscribers",
                "achievement", "silver", "🥈", true, null));

        // Performance: On a Streak (recent videos near channel peak)
        if (recentAvgViews >= topAvgViews * 0.8 && totalVideos >= 5)
            badges.Add(new("on-a-streak", "On a Streak",
                "Recent videos performing at or near your all-time best",
                "performance", "gold", "🔥", true, null));

        // Performance: Top Performer (a recent video beat 2x channel median)
        if (recentAvgViews >= topAvgViews * 0.9 && totalVideos >= 3)
            badges.Add(new("top-performer", "Top Performer",
                "Recent content matches your channel's peak performance",
                "performance", "bronze", "🏆", true, null));

        // Performance: Engagement Ace
        if (recentAvgEngagement >= topAvgEngagement * 0.9 && totalVideos >= 3)
            badges.Add(new("engagement-ace", "Engagement Ace",
                "Recent videos maintain strong engagement compared to your best",
                "performance", "bronze", "💬", true, null));

        return badges;
    }
}
