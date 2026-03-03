using Microsoft.EntityFrameworkCore;
using Vara.Api.Data;
using Vara.Api.Models.Entities;

namespace Vara.Api.Services;

public interface INicheNormalizationService
{
    /// <summary>
    /// Resolves a free-text niche input to the best-matching canonical niche.
    /// Returns null if no match exceeds the confidence threshold (0.85).
    /// </summary>
    Task<(CanonicalNiche Niche, double Confidence)?> ResolveAsync(string rawNiche, CancellationToken ct = default);

    /// <summary>Returns the top N closest matches regardless of threshold, for suggestions.</summary>
    Task<IReadOnlyList<(CanonicalNiche Niche, double Confidence)>> GetSuggestionsAsync(
        string rawNiche, int count = 5, CancellationToken ct = default);

    /// <summary>Returns all active canonical niches.</summary>
    Task<IReadOnlyList<CanonicalNiche>> GetAllActiveAsync(CancellationToken ct = default);
}

public class NicheNormalizationService(VaraContext db) : INicheNormalizationService
{
    private const double MatchThreshold = 0.85;

    public async Task<(CanonicalNiche Niche, double Confidence)?> ResolveAsync(
        string rawNiche, CancellationToken ct = default)
    {
        var niches = await GetAllActiveAsync(ct);
        var best = Score(rawNiche, niches).FirstOrDefault();
        return best.Confidence >= MatchThreshold ? best : null;
    }

    public async Task<IReadOnlyList<(CanonicalNiche Niche, double Confidence)>> GetSuggestionsAsync(
        string rawNiche, int count = 5, CancellationToken ct = default)
    {
        var niches = await GetAllActiveAsync(ct);
        return Score(rawNiche, niches).Take(count).ToList();
    }

    public async Task<IReadOnlyList<CanonicalNiche>> GetAllActiveAsync(CancellationToken ct = default) =>
        await db.CanonicalNiches
            .AsNoTracking()
            .Where(n => n.IsActive)
            .OrderBy(n => n.Name)
            .ToListAsync(ct);

    // -------------------------------------------------------------------------
    // Scoring
    // -------------------------------------------------------------------------

    private static IOrderedEnumerable<(CanonicalNiche Niche, double Confidence)> Score(
        string raw, IReadOnlyList<CanonicalNiche> niches)
    {
        var input = raw.Trim().ToLowerInvariant();
        return niches
            .Select(n => (Niche: n, Confidence: BestScore(input, n)))
            .OrderByDescending(x => x.Confidence);
    }

    private static double BestScore(string input, CanonicalNiche niche)
    {
        var candidates = new List<string> { niche.Name.ToLowerInvariant(), niche.Slug.ToLowerInvariant() };
        candidates.AddRange(niche.Aliases.Select(a => a.ToLowerInvariant()));
        return candidates.Max(c => JaroWinkler(input, c));
    }

    // -------------------------------------------------------------------------
    // Jaro-Winkler implementation
    // -------------------------------------------------------------------------

    private static double JaroWinkler(string s1, string s2)
    {
        if (s1 == s2) return 1.0;
        if (s1.Length == 0 || s2.Length == 0) return 0.0;

        int matchWindow = Math.Max(s1.Length, s2.Length) / 2 - 1;
        if (matchWindow < 0) matchWindow = 0;

        var s1Matches = new bool[s1.Length];
        var s2Matches = new bool[s2.Length];
        int matches = 0;

        for (int i = 0; i < s1.Length; i++)
        {
            int start = Math.Max(0, i - matchWindow);
            int end = Math.Min(i + matchWindow + 1, s2.Length);
            for (int j = start; j < end; j++)
            {
                if (s2Matches[j] || s1[i] != s2[j]) continue;
                s1Matches[i] = true;
                s2Matches[j] = true;
                matches++;
                break;
            }
        }

        if (matches == 0) return 0.0;

        int transpositions = 0;
        int k = 0;
        for (int i = 0; i < s1.Length; i++)
        {
            if (!s1Matches[i]) continue;
            while (!s2Matches[k]) k++;
            if (s1[i] != s2[k]) transpositions++;
            k++;
        }

        double jaro = (
            (double)matches / s1.Length +
            (double)matches / s2.Length +
            (matches - transpositions / 2.0) / matches
        ) / 3.0;

        // Winkler prefix bonus (up to 4 chars, p = 0.1)
        int prefix = 0;
        for (int i = 0; i < Math.Min(4, Math.Min(s1.Length, s2.Length)); i++)
        {
            if (s1[i] == s2[i]) prefix++;
            else break;
        }

        return jaro + prefix * 0.1 * (1.0 - jaro);
    }
}
