namespace Vara.Api.Plugins.OutlierDetection;

public static class PatternExtractor
{
    public static IReadOnlyList<string> Extract(IReadOnlyList<OutlierVideo> outliers)
    {
        var patterns = new List<string>();
        var titles = outliers.Select(o => o.Title.ToLowerInvariant()).ToList();

        var words = titles
            .SelectMany(t => t.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(w => w.Length > 4)
            .GroupBy(w => w)
            .Where(g => g.Count() > outliers.Count * 0.3)
            .OrderByDescending(g => g.Count())
            .Take(3)
            .Select(g => $"Common keyword: \"{g.Key}\" (in {g.Count()} titles)")
            .ToList();

        patterns.AddRange(words);

        var strong = outliers.Count(o => o.OutlierStrength == "Strong");
        if (strong > 0)
            patterns.Add($"{strong} strong outlier{(strong == 1 ? "" : "s")} (ratio ≥ 10×)");

        return patterns;
    }
}
