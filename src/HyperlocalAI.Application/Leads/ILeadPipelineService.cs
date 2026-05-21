namespace HyperlocalAI.Application.Leads;

/// <summary>Enterprise lead pipeline: list, read, update stages and scores.</summary>
public interface ILeadPipelineService
{
    Task<PagedLeadsResult> ListAsync(
        BusinessVertical vertical,
        LeadStage? stageFilter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<LeadDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<LeadDetailDto?> UpdateAsync(Guid id, UpdateLeadRequest request, CancellationToken cancellationToken = default);
}
