namespace HyperlocalAI.Application;

public sealed record TenantContext(
    string TenantId,
    string BusinessName,
    BusinessVertical Vertical);
