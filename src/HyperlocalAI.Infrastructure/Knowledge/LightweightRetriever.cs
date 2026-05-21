using System.Globalization;

using HyperlocalAI.Application.Knowledge;

namespace HyperlocalAI.Infrastructure.Knowledge;

/// <summary>
/// Simple term-overlap scoring for local demos without embeddings or a vector database.
/// Swap for <c>ISemanticTextMemory</c> + embeddings when you add Azure AI Search or similar.
/// </summary>
public static class LightweightRetriever
{
    public static IReadOnlyList<KnowledgeChunk> SelectTop(IEnumerable<KnowledgeChunk> chunks, string query, int topK)
    {
        var terms = Tokenize(query);
        if (terms.Count == 0)
            return chunks.Take(topK).ToList();

        return chunks
            .Select(c => (Chunk: c, Score: Score(c.Text, terms)))
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => x.Chunk)
            .ToList();
    }

    private static float Score(string text, HashSet<string> queryTerms)
    {
        var textTerms = Tokenize(text);
        if (textTerms.Count == 0 || queryTerms.Count == 0)
            return 0;

        var overlap = queryTerms.Intersect(textTerms).Count();
        return overlap / (float)Math.Sqrt(textTerms.Count * queryTerms.Count);
    }

    private static HashSet<string> Tokenize(string s)
    {
        var words = s
            .ToLowerInvariant()
            .Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return words.Where(w => w.Length > 1).ToHashSet();
    }

    private static readonly char[] Separators =
        [' ', '\t', '\n', '\r', ',', '.', '?', '!', ':', ';', '(', ')', '[', ']', '{', '}', '-', '–', '—', '/', '\\', '"', '\''];
}
