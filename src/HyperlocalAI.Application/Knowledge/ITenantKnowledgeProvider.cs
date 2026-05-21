namespace HyperlocalAI.Application.Knowledge;

/// <summary>Provides knowledge chunks for retrieval (seed + uploaded documents per vertical).</summary>
public interface ITenantKnowledgeProvider
{
    Task<IReadOnlyList<KnowledgeChunk>> GetChunksForVerticalAsync(BusinessVertical vertical, CancellationToken cancellationToken = default);
}
