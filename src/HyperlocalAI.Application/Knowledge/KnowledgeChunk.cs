namespace HyperlocalAI.Application.Knowledge;

/// <summary>One retrievable unit of tenant knowledge (seed or uploaded document segment).</summary>
public sealed record KnowledgeChunk(string Id, string Text);
