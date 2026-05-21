namespace HyperlocalAI.Application.Leads;

public sealed record LeadListItemDto(
    Guid Id,
    BusinessVertical Vertical,
    string Channel,
    string ContactKey,
    string? DisplayName,
    string? Email,
    string? PhoneE164,
    LeadStage Stage,
    string Source,
    int LeadScore,
    DateTime FirstSeenUtc,
    DateTime LastActivityUtc);

public sealed record LeadDetailDto(
    Guid Id,
    BusinessVertical Vertical,
    string Channel,
    string ContactKey,
    string? DisplayName,
    string? Email,
    string? PhoneE164,
    LeadStage Stage,
    string Source,
    int LeadScore,
    string? Notes,
    DateTime FirstSeenUtc,
    DateTime LastActivityUtc,
    DateTime? WonAtUtc,
    DateTime? LostAtUtc,
    string? LostReason);

public sealed class UpdateLeadRequest
{
    public LeadStage? Stage { get; set; }
    public string? Notes { get; set; }
    public int? LeadScore { get; set; }
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string? LostReason { get; set; }
}

public sealed record PagedLeadsResult(
    IReadOnlyList<LeadListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
