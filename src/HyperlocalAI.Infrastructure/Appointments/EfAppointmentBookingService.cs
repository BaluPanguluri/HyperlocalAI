using HyperlocalAI.Application;
using HyperlocalAI.Application.Appointments;
using HyperlocalAI.Infrastructure.Persistence;
using HyperlocalAI.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace HyperlocalAI.Infrastructure.Appointments;

public sealed class EfAppointmentBookingService : IAppointmentBookingService
{
    private readonly IDbContextFactory<LocalMindDbContext> _factory;

    public EfAppointmentBookingService(IDbContextFactory<LocalMindDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<AppointmentDto> CreateAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var start = request.StartUtc.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(request.StartUtc, DateTimeKind.Utc)
            : request.StartUtc.ToUniversalTime();
        var end = start.AddMinutes(Math.Clamp(request.DurationMinutes, 15, 480));
        var phone = request.Phone.Trim();
        var leadId = await TryFindLeadIdAsync(db, request.Vertical, request.Channel, phone, cancellationToken).ConfigureAwait(false);

        var id = Guid.NewGuid();
        var row = new AppointmentRecord
        {
            Id = id,
            LeadId = leadId,
            Channel = request.Channel.Trim(),
            CustomerName = request.CustomerName.Trim(),
            Phone = phone,
            Vertical = request.Vertical,
            StartUtc = start,
            EndUtc = end,
            Notes = request.Notes?.Trim(),
            Status = AppointmentStatus.Scheduled,
            CreatedUtc = DateTime.UtcNow,
        };
        db.Appointments.Add(row);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return ToDto(row);
    }

    public async Task<IReadOnlyList<AppointmentDto>> ListAsync(BusinessVertical? vertical, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var q = db.Appointments.AsNoTracking().AsQueryable();
        if (vertical.HasValue)
            q = q.Where(a => a.Vertical == vertical.Value);
        var list = await q.OrderByDescending(a => a.StartUtc).ToListAsync(cancellationToken).ConfigureAwait(false);
        return list.Select(ToDto).ToList();
    }

    private static AppointmentDto ToDto(AppointmentRecord a) =>
        new(
            a.Id,
            a.Channel,
            a.CustomerName,
            a.Phone,
            a.Vertical,
            a.StartUtc,
            a.EndUtc,
            a.Notes,
            a.Status,
            a.CreatedUtc);

    private static async Task<Guid?> TryFindLeadIdAsync(
        LocalMindDbContext db,
        BusinessVertical vertical,
        string channel,
        string phone,
        CancellationToken ct)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
            return null;

        var ch = channel.Trim().ToLowerInvariant();
        var contactKey = $"{ch}:{digits}";

        var id = await db.Leads.AsNoTracking()
            .Where(l => l.Vertical == vertical && l.ContactKey == contactKey)
            .Select(l => (Guid?)l.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        if (id.HasValue)
            return id;

        return await db.Leads.AsNoTracking()
            .Where(l => l.Vertical == vertical && l.PhoneE164 != null && l.PhoneE164.Replace("+", "").Replace(" ", "") == digits)
            .Select(l => (Guid?)l.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }
}
