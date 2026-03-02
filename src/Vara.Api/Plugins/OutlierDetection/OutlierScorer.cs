namespace Vara.Api.Plugins.OutlierDetection;

public static class OutlierScorer
{
    public static List<OutlierVideo> Score(
        IReadOnlyList<OutlierCandidate> candidates,
        double minOutlierRatio,
        long maxChannelSize)
    {
        var eligible = candidates
            .Where(c => c.SubscriberCount > 0
                     && c.SubscriberCount <= maxChannelSize)
            .Select(c => (c, ratio: (double)c.ViewCount / c.SubscriberCount))
            .Where(x => x.ratio >= minOutlierRatio)
            .ToList();

        if (eligible.Count == 0) return [];

        var maxRatio = eligible.Max(x => x.ratio);

        return eligible
            .Select(x => new OutlierVideo(
                VideoId:         x.c.VideoId,
                Title:           x.c.Title,
                ChannelName:     x.c.ChannelName,
                SubscriberCount: x.c.SubscriberCount,
                ViewCount:       x.c.ViewCount,
                OutlierRatio:    Math.Round(x.ratio, 2),
                OutlierScore:    (int)Math.Round(x.ratio / maxRatio * 100),
                OutlierStrength: x.ratio >= 10 ? "Strong" : x.ratio >= 5 ? "Moderate" : "Mild",
                UploadDate:      x.c.UploadDate,
                LlmInsight:      null))
            .OrderByDescending(v => v.OutlierScore)
            .ToList();
    }
}
