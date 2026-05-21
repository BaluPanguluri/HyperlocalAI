using HyperlocalAI.Application;
using HyperlocalAI.Application.Appointments;
using HyperlocalAI.Application.Dashboard;
using HyperlocalAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HyperlocalAI.Infrastructure.Dashboard;

public sealed class EfBusinessDashboardService : IBusinessDashboardService
{
    private readonly IDbContextFactory<LocalMindDbContext> _factory;
    private readonly IAppointmentBookingService _appointments;

    public EfBusinessDashboardService(
        IDbContextFactory<LocalMindDbContext> factory,
        IAppointmentBookingService appointments)
    {
        _factory = factory;
        _appointments = appointments;
    }

    public async Task<BusinessDashboardSnapshot> GetSnapshotAsync(
        BusinessVertical vertical,
        int dailyDays = 14,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var totalLeads = await db.Leads.AsNoTracking()
            .CountAsync(l => l.Vertical == vertical, cancellationToken)
            .ConfigureAwait(false);

        var messagesTotal = await db.DailyMetrics.AsNoTracking()
            .Where(d => d.Vertical == vertical)
            .SumAsync(d => (int?)d.MessagesHandled, cancellationToken)
            .ConfigureAwait(false) ?? 0;

        var apptList = await _appointments.ListAsync(vertical, cancellationToken).ConfigureAwait(false);
        var totalBookings = apptList.Count;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startDay = today.AddDays(-(dailyDays - 1));
        var dailyRows = await db.DailyMetrics.AsNoTracking()
            .Where(x => x.Vertical == vertical && x.DayUtc >= startDay && x.DayUtc <= today)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var dailyByDay = dailyRows.ToDictionary(x => x.DayUtc);

        var rows = new List<DailyStatRow>(dailyDays);
        for (var i = dailyDays - 1; i >= 0; i--)
        {
            var d = today.AddDays(-i);
            _ = dailyByDay.TryGetValue(d, out var daily);
            var newLeads = daily?.NewLeads ?? 0;
            var messages = daily?.MessagesHandled ?? 0;
            var bookings = apptList.Count(a => DateOnly.FromDateTime(a.CreatedUtc) == d);
            rows.Add(new DailyStatRow(d, newLeads, messages, bookings));
        }

        return new BusinessDashboardSnapshot(vertical, totalLeads, messagesTotal, totalBookings, rows);
    }
}
