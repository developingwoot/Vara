using System.Text.Json;

namespace Vara.Api.Plugins.OutlierDetection;

public class OutlierDetectionPlugin : IPlugin
{
    public string PluginId => "outlier-detection";

    public async Task<object> ExecuteAsync(
        IAnalysisContext context, object input, CancellationToken ct = default)
    {
        var req = input is JsonElement je
            ? je.Deserialize<OutlierRequest>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!
            : (OutlierRequest)input;

        // 1. Search YouTube for keyword
        var videos = await context.SearchVideosAsync(req.Keyword, req.MaxResults, ct);

        // 2. Filter by age
        var cutoff = DateTime.UtcNow.AddDays(-req.MaxAgeDays);
        var recent = videos.Where(v => v.UploadDate == null || v.UploadDate >= cutoff).ToList();

        // 3. Fetch subscriber counts (deduplicated by ChannelId)
        var channelIds = recent
            .Select(v => v.ChannelId)
            .Where(id => id != null)
            .Distinct()
            .ToList();

        var channelMap = new Dictionary<string, long>();
        foreach (var cid in channelIds)
        {
            var channel = await context.GetChannelAsync(cid!, ct);
            if (channel?.SubscriberCount is long sc)
                channelMap[cid!] = sc;
        }

        // 4. Build candidates for scorer
        var candidates = recent
            .Where(v => v.ChannelId != null && channelMap.ContainsKey(v.ChannelId!))
            .Select(v => new OutlierCandidate(
                v.YoutubeId, v.Title, v.ChannelName,
                channelMap[v.ChannelId!], v.ViewCount, v.UploadDate))
            .ToList();

        // 5. Score (pure function)
        var outliers = OutlierScorer.Score(candidates, req.MinOutlierRatio, req.MaxChannelSize);

        // 6. Optional LLM insights for top 5 (Creator tier)
        if (req.IncludeLlmInsights)
        {
            for (var i = 0; i < Math.Min(5, outliers.Count); i++)
            {
                var outlier = outliers[i];
                var prompt = OutlierInsightPrompt.Build(outlier);
                var execCtx = new LlmExecutionContext
                    { UserId = context.UserId, TaskType = "OutlierInsights" };
                var llmResponse = await context.CallLlmAsync(prompt, execCtx, ct);
                outliers[i] = outlier with { LlmInsight = llmResponse.Content };
            }
        }

        // 7. Extract patterns and build summary
        var patterns = PatternExtractor.Extract(outliers);
        var summary = new OutlierSummary(
            TotalAnalyzed:       recent.Count,
            OutliersFound:       outliers.Count,
            StrongOutliers:      outliers.Count(o => o.OutlierStrength == "Strong"),
            AvgOutlierRatio:     outliers.Count > 0
                ? Math.Round(outliers.Average(o => o.OutlierRatio), 2) : 0,
            TopOpportunityTitle: outliers.FirstOrDefault()?.Title,
            CommonPatterns:      patterns);

        return new OutlierResult(outliers, summary, QuotaUsed: 102);
    }
}
