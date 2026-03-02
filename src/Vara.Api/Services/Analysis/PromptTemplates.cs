namespace Vara.Api.Services.Analysis;

public static class PromptTemplates
{
    public static string KeywordInsights(KeywordAnalysisResult analysis) =>
        $"""
        You are a YouTube strategy expert. Analyze this keyword research data and provide strategic insights:

        Keyword: {analysis.Keyword}
        Niche: {analysis.Niche ?? "general"}
        Search Volume: {analysis.SearchVolumeRelative}/100
        Competition Score: {analysis.CompetitionScore}/100
        Trend: {analysis.TrendDirection}
        Intent: {analysis.KeywordIntent}

        Provide:
        1. Why creators should care about this keyword
        2. Specific positioning strategies to stand out
        3. Content gaps you could exploit
        4. 3 unique video angle ideas that haven't been overdone

        Be specific and actionable. Assume the creator is intermediate level (not beginner, not expert).
        """;

    public static string TranscriptAnalysis(string transcript) =>
        $"""
        You are a YouTube content strategist. Analyze the following video transcript and provide actionable insights for creators:

        TRANSCRIPT:
        {transcript}

        Provide a structured analysis covering:
        1. Main Topics (3-5 core subjects covered)
        2. Key Takeaways (top 3 actionable lessons for the viewer)
        3. Engagement Hooks (specific moments or techniques that keep viewers watching)
        4. Content Gaps (important aspects that were missed or underdeveloped)
        5. Unique Angle (what makes this content stand out — or what doesn't)
        6. Call-to-Action Effectiveness (how well the creator drives viewer action)

        Reference actual content from the transcript. Focus on what a competing creator can learn from this video.
        """;
}
