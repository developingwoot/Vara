using Microsoft.EntityFrameworkCore;
using Vara.Api.Data;
using Vara.Api.Services.Llm;
using Vara.Api.Services.YouTube;

namespace Vara.Api.Services.Analysis;

public record TranscriptAnalysisResult(
    string VideoId,
    string? Title,
    string? ChannelName,
    int WordCount,
    int SentenceCount,
    int EstimatedTokens,
    double ReadingTimeMinutes,
    bool TranscriptAvailable,
    string? LlmInsights,
    bool LlmEnhanced,
    DateTime AnalyzedAt);

public interface ITranscriptAnalysisService
{
    Task<TranscriptAnalysisResult> AnalyzeAsync(
        Guid userId,
        string videoId,
        bool includeInsights = false,
        CancellationToken ct = default);
}

public class TranscriptAnalysisService(
    IYouTubeClient youtube,
    ILlmOrchestrator llm,
    IPlanEnforcer planEnforcer,
    IUsageMeter usageMeter,
    VaraContext db,
    ILogger<TranscriptAnalysisService> logger) : ITranscriptAnalysisService
{
    private const int MaxTranscriptChars = 32_000; // ~8 000 tokens

    public async Task<TranscriptAnalysisResult> AnalyzeAsync(
        Guid userId, string videoId,
        bool includeInsights = false, CancellationToken ct = default)
    {
        // 1. Creator tier gate — "transcripts" feature requires creator
        await planEnforcer.EnforceAsync(userId, "transcripts", ct);

        // 2. Video metadata (title / channel name)
        var meta = await youtube.GetVideoAsync(videoId, ct);

        // 3. Transcript — DB cache first, then YouTube
        var transcript = await GetOrFetchTranscriptAsync(userId, videoId, ct);

        // 4. Base metrics
        var wordCount       = CountWords(transcript);
        var sentenceCount   = CountSentences(transcript);
        var estimatedTokens = (transcript?.Length ?? 0) / 4;
        var readingTime     = Math.Round(wordCount / 200.0, 1);

        // 5. Optional LLM deep-dive
        string? llmInsights = null;
        var llmEnhanced = false;

        if (includeInsights && transcript is not null)
        {
            var truncated = transcript.Length > MaxTranscriptChars
                ? transcript[..MaxTranscriptChars]
                : transcript;

            var prompt = PromptTemplates.TranscriptAnalysis(truncated);
            var llmResponse = await llm.ExecuteAsync(
                "TranscriptAnalysis", prompt, new LlmOptions(MaxTokens: 600), ct);

            await usageMeter.RecordLlmCallAsync(userId, "TranscriptAnalysis", ct);

            llmInsights = llmResponse.Content;
            llmEnhanced = true;

            logger.LogInformation(
                "Transcript LLM analysis for video {VideoId} (user {UserId}), cost ${Cost:F4}",
                videoId, userId, llmResponse.CostUsd);
        }

        return new TranscriptAnalysisResult(
            VideoId: videoId,
            Title: meta?.Title,
            ChannelName: meta?.ChannelName,
            WordCount: wordCount,
            SentenceCount: sentenceCount,
            EstimatedTokens: estimatedTokens,
            ReadingTimeMinutes: readingTime,
            TranscriptAvailable: transcript is not null,
            LlmInsights: llmInsights,
            LlmEnhanced: llmEnhanced,
            AnalyzedAt: DateTime.UtcNow);
    }

    private async Task<string?> GetOrFetchTranscriptAsync(
        Guid userId, string videoId, CancellationToken ct)
    {
        var video = await db.Videos
            .FirstOrDefaultAsync(v => v.UserId == userId && v.YoutubeId == videoId, ct);

        if (video?.TranscriptText is not null)
            return video.TranscriptText;   // cache hit — no YouTube call needed

        var transcript = await youtube.GetTranscriptAsync(videoId, ct);

        if (transcript is not null && video is not null)
        {
            video.TranscriptText = transcript;
            await db.SaveChangesAsync(ct);
        }

        return transcript;
    }

    internal static int CountWords(string? text) =>
        text is null ? 0
        : text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

    internal static int CountSentences(string? text) =>
        text is null ? 0 : text.Count(c => c is '.' or '!' or '?');
}
