using System.Security.Claims;
using System.Text;
using Vara.Api.Filters;
using Vara.Api.Models.DTOs;
using Vara.Api.Services.Analysis;
using Vara.Api.Services.YouTube;

namespace Vara.Api.Endpoints;

public static class VideoAnalysisEndpoints
{
    public static RouteGroupBuilder MapVideoAnalysisEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/", AnalyzeVideos)
            .AddEndpointFilter<ValidationFilter<AnalyzeVideosRequest>>()
            .WithTags("Video Analysis")
            .WithSummary("Analyze top YouTube videos for a keyword and return statistical patterns");

        group.MapPost("/export", ExportVideos)
            .AddEndpointFilter<ValidationFilter<AnalyzeVideosRequest>>()
            .WithTags("Video Analysis")
            .WithSummary("Export raw video data for a keyword as CSV");

        group.MapPost("/{videoId}/transcript", AnalyzeTranscript)
            .WithTags("Video Analysis")
            .WithSummary("Fetch and analyze a video transcript, optionally with LLM insights (Creator tier)");

        return group;
    }

    // -------------------------------------------------------------------------
    // POST /api/analysis/videos
    // -------------------------------------------------------------------------

    private static async Task<IResult> AnalyzeVideos(
        AnalyzeVideosRequest req,
        IVideoAnalyzer analyzer)
    {
        var result = await analyzer.AnalyzeAsync(req.Keyword, req.SampleSize);
        return Results.Ok(ToResponse(result));
    }

    // -------------------------------------------------------------------------
    // POST /api/analysis/videos/export
    // -------------------------------------------------------------------------

    private static async Task<IResult> ExportVideos(
        AnalyzeVideosRequest req,
        IVideoAnalyzer analyzer,
        HttpContext context)
    {
        // Re-use the cached analysis to get the video list — re-fetch if needed.
        // We call SearchAsync directly via the analyzer's underlying client, but
        // since the analyzer is cached the YouTube call will hit cache too.
        // Simpler: call AnalyzeAsync (for side-effect of populating cache), then
        // re-fetch the raw videos for the CSV rows.
        await analyzer.AnalyzeAsync(req.Keyword, req.SampleSize);

        // Fetch raw video metadata (cache hit — no additional quota cost).
        // We inject IYouTubeClient separately to get the list.
        var youtube = context.RequestServices.GetRequiredService<IYouTubeClient>();
        var videos  = await youtube.SearchAsync(req.Keyword, req.SampleSize);

        var csv = BuildCsv(videos);

        context.Response.Headers["Content-Disposition"] = "attachment; filename=\"analysis.csv\"";
        return Results.Text(csv, "text/csv");
    }

    // -------------------------------------------------------------------------
    // POST /api/analysis/videos/{videoId}/transcript
    // -------------------------------------------------------------------------

    private static async Task<IResult> AnalyzeTranscript(
        string videoId,
        AnalyzeTranscriptRequest req,
        ITranscriptAnalysisService service,
        ClaimsPrincipal user)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await service.AnalyzeAsync(userId, videoId, req.IncludeInsights);
        return Results.Ok(new TranscriptAnalysisResponse(
            result.VideoId, result.Title, result.ChannelName,
            result.WordCount, result.SentenceCount, result.EstimatedTokens,
            result.ReadingTimeMinutes, result.TranscriptAvailable,
            result.LlmInsights, result.LlmEnhanced, result.AnalyzedAt));
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static VideoAnalysisResponse ToResponse(VideoAnalysisResult r) => new(
        r.Keyword, r.SampleSize,
        r.AvgTitleLength, r.MinTitleLength, r.MaxTitleLength, r.TitleLengthStdDev,
        r.AvgDurationSeconds, r.MinDurationSeconds, r.MaxDurationSeconds,
        r.AvgViewCount,
        r.AvgEngagementRate,
        r.UploadsByDayOfWeek,
        r.Patterns,
        r.AnalyzedAt);

    private static string BuildCsv(List<VideoMetadata> videos)
    {
        var sb = new StringBuilder();
        sb.AppendLine("YoutubeId,Title,Channel,DurationSeconds,ViewCount,LikeCount,CommentCount,EngagementRate,UploadDate");

        foreach (var v in videos)
        {
            var engagement = (v.LikeCount + v.CommentCount) / (double)Math.Max(v.ViewCount, 1) * 100;
            sb.AppendLine(string.Join(',',
                v.YoutubeId,
                CsvQuote(v.Title),
                CsvQuote(v.ChannelName),
                v.DurationSeconds?.ToString() ?? "",
                v.ViewCount,
                v.LikeCount,
                v.CommentCount,
                $"{engagement:F2}",
                v.UploadDate.HasValue ? v.UploadDate.Value.ToString("yyyy-MM-dd") : ""));
        }

        return sb.ToString();
    }

    private static string CsvQuote(string? value)
    {
        if (value is null) return "";
        value = value.Replace("\"", "\"\"");
        return $"\"{value}\"";
    }
}
