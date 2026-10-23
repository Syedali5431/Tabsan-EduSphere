using System.Text.RegularExpressions;

namespace Tabsan.EduSphere.Application.Similarity;

/// <summary>A word of a text with where it sits in the original text.</summary>
public readonly record struct TextWord(string Word, int Start, int Length);

/// <summary>A fingerprint: the hash of <see cref="Fingerprinter.ShingleSize"/> words starting at word <c>Position</c>.</summary>
public readonly record struct Fingerprint(int Position, long Hash);

/// <summary>
/// Plan M9 built-in comparison (pure, so it can be unit tested): texts are split into words, every run of
/// <see cref="ShingleSize"/> words is hashed (FNV-1a, 64 bit), and winnowing keeps the smallest hash of every window of
/// <see cref="Window"/> runs — so any shared passage of at least ShingleSize + Window − 1 words is always detected,
/// whatever its position, while only a fraction of the hashes is stored.
/// </summary>
public static partial class Fingerprinter
{
    public const int ShingleSize = 5;
    public const int Window = 4;

    [GeneratedRegex(@"[\p{L}\p{N}]+(?:['’][\p{L}]+)?", RegexOptions.CultureInvariant)]
    private static partial Regex WordPattern();

    public static IReadOnlyList<TextWord> Words(string text)
        => WordPattern().Matches(text ?? string.Empty).Select(m => new TextWord(m.Value.ToLowerInvariant().Replace('’', '\''), m.Index, m.Length)).ToList();

    public static IReadOnlyList<Fingerprint> Fingerprints(IReadOnlyList<TextWord> words)
    {
        if (words.Count < ShingleSize) return [];
        var hashes = new long[words.Count - ShingleSize + 1];
        for (var i = 0; i < hashes.Length; i++) hashes[i] = Hash(words, i);
        if (hashes.Length <= Window) return [new Fingerprint(MinIndex(hashes, 0, hashes.Length), hashes[MinIndex(hashes, 0, hashes.Length)])];

        var result = new List<Fingerprint>();
        var last = -1;
        for (var start = 0; start + Window <= hashes.Length; start++)
        {
            var min = MinIndex(hashes, start, Window);
            if (min == last) continue;
            result.Add(new Fingerprint(min, hashes[min]));
            last = min;
        }
        return result;
    }

    /// <summary>Rightmost smallest hash of hashes[start..start+count) (the winnowing choice).</summary>
    private static int MinIndex(long[] hashes, int start, int count)
    {
        var min = start;
        for (var i = start + 1; i < start + count; i++)
            if (hashes[i] <= hashes[min]) min = i;
        return min;
    }

    private static long Hash(IReadOnlyList<TextWord> words, int start)
    {
        unchecked
        {
            ulong h = 14695981039346656037UL;
            for (var w = start; w < start + ShingleSize; w++)
            {
                foreach (var c in words[w].Word)
                {
                    h ^= c;
                    h *= 1099511628211UL;
                }
                h ^= ' ';
                h *= 1099511628211UL;
            }
            return (long)h;
        }
    }
}

/// <summary>A passage of the checked work matched in a source: character ranges in both texts.</summary>
public sealed record MatchedPassage(int Start, int Length, int SourceStart, int SourceLength);

/// <summary>One source document's matches: which fingerprints of the checked work it shares, at which of its own word positions.</summary>
public sealed record SourceMatches(Guid DocumentId, IReadOnlyList<(int Position, int SourcePosition)> Pairs);

/// <summary>Plan M9 scoring and passages for the built-in comparison.</summary>
public static class SimilarityScorer
{
    /// <summary>
    /// Share (0–100) of the checked work's words covered by passages found in a source. <paramref name="positions"/> are all
    /// of the work's fingerprint positions in order; a matched fingerprint covers its words and, when the next fingerprint
    /// matched too, the words up to it (winnowing never leaves more than a window between fingerprints); the first and
    /// last fingerprints also cover the edges of the text, so an exact copy scores 100%.
    /// </summary>
    public static decimal Score(int wordCount, IReadOnlyList<int> positions, IReadOnlySet<int> matched)
    {
        if (wordCount <= 0 || matched.Count == 0) return 0;
        var covered = new bool[wordCount];
        for (var i = 0; i < positions.Count; i++)
        {
            var p = positions[i];
            if (!matched.Contains(p)) continue;
            var from = i == 0 ? 0 : p;
            var to = i == positions.Count - 1 ? wordCount - 1 : p + Fingerprinter.ShingleSize - 1;
            if (i + 1 < positions.Count && matched.Contains(positions[i + 1])) to = Math.Max(to, positions[i + 1] - 1);
            for (var w = from; w <= Math.Min(wordCount - 1, to); w++) covered[w] = true;
        }
        return Math.Round(100m * covered.Count(c => c) / wordCount, 1, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Merges matched word runs into passages (runs closer than a few words join) and turns them into character ranges
    /// of the checked text and of the source text.
    /// </summary>
    public static IReadOnlyList<MatchedPassage> Passages(IReadOnlyList<TextWord> words, IReadOnlyList<TextWord> sourceWords,
        IReadOnlyList<(int Position, int SourcePosition)> pairs, int max = 5)
    {
        var runs = new List<(int From, int To, int SFrom, int STo)>();
        foreach (var (p, sp) in pairs.OrderBy(x => x.Position))
        {
            var to = Math.Min(words.Count - 1, p + Fingerprinter.ShingleSize - 1);
            var sto = Math.Min(sourceWords.Count - 1, sp + Fingerprinter.ShingleSize - 1);
            if (p >= words.Count || sp >= sourceWords.Count) continue;
            if (runs.Count > 0 && p <= runs[^1].To + Fingerprinter.Window)
            {
                var r = runs[^1];
                runs[^1] = (r.From, Math.Max(r.To, to), Math.Min(r.SFrom, sp), Math.Max(r.STo, sto));
            }
            else runs.Add((p, to, sp, sto));
        }
        // Fingerprints only mark where a copied run is; widen each passage while the words around it are the same in both texts.
        for (var i = 0; i < runs.Count; i++)
        {
            var (from, to, sFrom, sTo) = runs[i];
            while (from > 0 && sFrom > 0 && words[from - 1].Word == sourceWords[sFrom - 1].Word) { from--; sFrom--; }
            while (to + 1 < words.Count && sTo + 1 < sourceWords.Count && words[to + 1].Word == sourceWords[sTo + 1].Word) { to++; sTo++; }
            runs[i] = (from, to, sFrom, sTo);
        }
        return runs.OrderByDescending(r => r.To - r.From).Take(max).OrderBy(r => r.From)
            .Select(r => new MatchedPassage(words[r.From].Start, words[r.To].Start + words[r.To].Length - words[r.From].Start,
                sourceWords[r.SFrom].Start, sourceWords[r.STo].Start + sourceWords[r.STo].Length - sourceWords[r.SFrom].Start))
            .ToList();
    }
}
