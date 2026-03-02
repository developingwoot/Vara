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
}
