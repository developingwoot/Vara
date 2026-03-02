namespace Vara.Api.Plugins.OutlierDetection;

public static class OutlierInsightPrompt
{
    public static string Build(OutlierVideo video) =>
        $"""
        A YouTube video significantly outperformed its channel size. Explain in 2-3 sentences why this likely succeeded:

        Title: {video.Title}
        Channel subscribers: {video.SubscriberCount:N0}
        Views: {video.ViewCount:N0}
        Outlier ratio: {video.OutlierRatio:F1}× (views ÷ subscribers)
        Upload date: {video.UploadDate?.ToString("yyyy-MM-dd") ?? "unknown"}

        Focus on content angle, timing, or audience fit. Be specific and actionable.
        """;
}
