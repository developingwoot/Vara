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

    public static string ChannelDeepAudit(
        string? recentTitle, string recentTranscript,
        string? topTitle, string topTranscript) =>
        $"""
        You are a YouTube strategy coach. A creator wants to understand why their best video outperformed their most recent upload and how to close the gap.

        MOST RECENT VIDEO: "{recentTitle ?? "Untitled"}"
        TRANSCRIPT:
        {recentTranscript}

        ---

        TOP PERFORMING VIDEO: "{topTitle ?? "Untitled"}"
        TRANSCRIPT:
        {topTranscript}

        ---

        Compare these two videos and provide a structured coaching analysis:

        1. Pacing Difference — How does the information density and pacing differ in the first 2 minutes? Which video hooks faster?
        2. Storytelling Structure — How does each video open, develop, and close? Which structure is more engaging and why?
        3. Hook Quality — What specific technique does each video use to grab attention? Which is stronger?
        4. CTA Effectiveness — How does each video drive viewer action (subscribe, comment, watch next)?
        5. What the Top Video Does Better — 3 concrete, specific things the creator did in their top video that are absent or weaker in the recent one.
        6. Immediate Action — The single most impactful change the creator can make in their next video, based on this comparison.

        Be direct, specific, and encouraging. Reference actual content from the transcripts.
        """;

    public static string VideoComparison(
        string? video1Title, string transcript1,
        string? video2Title, string transcript2) =>
        $"""
        You are a YouTube content strategist. Compare these two videos and help the creator understand the key differences in approach.

        YOUR VIDEO: "{video1Title ?? "Untitled"}"
        TRANSCRIPT:
        {transcript1}

        ---

        COMPETITOR / REFERENCE VIDEO: "{video2Title ?? "Untitled"}"
        TRANSCRIPT:
        {transcript2}

        ---

        Provide a side-by-side analysis covering:

        1. Pacing — Which video moves faster? Where do the pacing differences appear (intro, middle, outro)?
        2. Hook Approach — What technique does each use in the first 60 seconds? Which is more compelling?
        3. Content Depth — Which covers the topic more thoroughly or uniquely?
        4. Where Your Video Excels — 2-3 specific things your video does better or differently that add value.
        5. Where to Improve — 2-3 specific areas where the reference video outperforms yours, with concrete suggestions to close the gap.
        6. Biggest Takeaway — The one change that would have the highest impact on your next video.

        Be honest, specific, and constructive. Your goal is to help the creator improve, not to just compare.
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
