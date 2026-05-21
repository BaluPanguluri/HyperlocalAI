namespace HyperlocalAI.Application.Dashboard;

/// <summary>Read-only dashboard for a business vertical (in-memory demo metrics).</summary>
public interface IBusinessDashboardService
{
    /// <param name="dailyDays">How many recent UTC calendar days to include in daily stats (including today).</param>
    Task<BusinessDashboardSnapshot> GetSnapshotAsync(BusinessVertical vertical, int dailyDays = 14, CancellationToken cancellationToken = default);
}
