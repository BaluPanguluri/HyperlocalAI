using HyperlocalAI.Application;
using HyperlocalAI.Application.Leads;
using HyperlocalAI.Infrastructure.Persistence;
using HyperlocalAI.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace HyperlocalAI.Infrastructure.Leads;

public sealed class LeadPipelineService : ILeadPipelineService
{
    private readonly IDbContextFactory<LocalMindDbContext> _factory;

    public LeadPipelineService(IDbContextFactory<LocalMindDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<PagedLeadsResult> ListAsync(
        BusinessVertical vertical,
        LeadStage? stageFilter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var q = db.Leads.AsNoTracking().Where(l => l.Vertical == vertical);
        if (stageFilter.HasValue)
            q = q.Where(l => l.Stage == stageFilter.Value);
        var total = await q.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await q
            .OrderByDescending(l => l.LastActivityUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LeadListItemDto(
                l.Id,
                l.Vertical,
                l.Channel,
                l.ContactKey,
                l.DisplayName,
                l.Email,
                l.PhoneE164,
                l.Stage,
                l.Source,
                l.LeadScore,
                l.FirstSeenUtc,
                l.LastActivityUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return new PagedLeadsResult(items, total, page, pageSize);
    }

    public async Task<LeadDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var l = await db.Leads.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken).ConfigureAwait(false);
        return l is null ? null : MapDetail(l);
    }

    public async Task<LeadDetailDto?> UpdateAsync(Guid id, UpdateLeadRequest request, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var l = await db.Leads.FirstOrDefaultAsync(x => x.Id == id, cancellationToken).ConfigureAwait(false);
        if (l is null)
            return null;

        var utc = DateTime.UtcNow;
        if (request.Stage.HasValue && request.Stage.Value != l.Stage)
        {
            l.Stage = request.Stage.Value;
            if (request.Stage.Value == LeadStage.Won)
            {
                l.WonAtUtc = utc;
                l.LostAtUtc = null;
                l.LostReason = null;
            }
            else if (request.Stage.Value == LeadStage.Lost)
            {
                l.LostAtUtc = utc;
                l.WonAtUtc = null;
            }
            else
            {
                if (l.Stage == LeadStage.Won || l.Stage == LeadStage.Lost)
                {
                    l.WonAtUtc = null;
                    l.LostAtUtc = null;
                    l.LostReason = null;
                }
            }
        }

        if (request.Notes != null)
            l.Notes = request.Notes.Trim().Length == 0 ? null : request.Notes.Trim()[..Math.Min(request.Notes.Trim().Length, 4000)];
        if (request.LeadScore.HasValue)
            l.LeadScore = Math.Clamp(request.LeadScore.Value, 0, 100);
        if (request.DisplayName != null)
            l.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? null : request.DisplayName.Trim()[..Math.Min(request.DisplayName.Trim().Length, 256)];
        if (request.Email != null)
            l.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim()[..Math.Min(request.Email.Trim().Length, 256)];
        if (request.LostReason != null)
            l.LostReason = string.IsNullOrWhiteSpace(request.LostReason) ? null : request.LostReason.Trim()[..Math.Min(request.LostReason.Trim().Length, 500)];

        l.LastActivityUtc = utc;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return MapDetail(l);
    }

    private static LeadDetailDto MapDetail(LeadRecord l) =>
        new(
            l.Id,
            l.Vertical,
            l.Channel,
            l.ContactKey,
            l.DisplayName,
            l.Email,
            l.PhoneE164,
            l.Stage,
            l.Source,
            l.LeadScore,
            l.Notes,
            l.FirstSeenUtc,
            l.LastActivityUtc,
            l.WonAtUtc,
            l.LostAtUtc,
            l.LostReason);
}
