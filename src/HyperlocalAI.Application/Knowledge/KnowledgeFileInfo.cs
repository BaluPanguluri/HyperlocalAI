namespace HyperlocalAI.Application.Knowledge;

public sealed record KnowledgeFileInfo(string FileName, long SizeBytes, DateTimeOffset LastModifiedUtc);
