using HyperlocalAI.Application.Knowledge;

namespace HyperlocalAI.Infrastructure.Knowledge;

public static class KnowledgeTextChunker
{
    /// <summary>Split long text into overlapping chunks for retrieval.</summary>
    public static IReadOnlyList<KnowledgeChunk> ChunkText(string text, string idPrefix, int maxChars = 1100, int overlap = 120)
    {
        text = text.Trim();
        if (text.Length == 0)
            return [];

        var chunks = new List<KnowledgeChunk>();
        var normalizedPrefix = string.IsNullOrWhiteSpace(idPrefix) ? "doc" : idPrefix.Trim();

        if (text.Length <= maxChars)
        {
            chunks.Add(new KnowledgeChunk($"{normalizedPrefix}:0", text));
            return chunks;
        }

        var i = 0;
        var part = 0;
        while (i < text.Length)
        {
            var len = Math.Min(maxChars, text.Length - i);
            var slice = text.Substring(i, len);
            chunks.Add(new KnowledgeChunk($"{normalizedPrefix}:{part}", slice));
            part++;
            if (i + len >= text.Length)
                break;
            i += maxChars - overlap;
        }

        return chunks;
    }
}
