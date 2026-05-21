namespace HyperlocalAI.Application.Dashboard;

public sealed record DailyStatRow(
    DateOnly Date,
    int NewLeads,
    int MessagesHandled,
    int Bookings);

public sealed record BusinessDashboardSnapshot(
    BusinessVertical Vertical,
    int TotalLeads,
    int MessagesHandled,
    int TotalBookings,
    IReadOnlyList<DailyStatRow> DailyStats);
