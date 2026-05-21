using HyperlocalAI.Application;

namespace HyperlocalAI.Infrastructure.Persistence.Entities;

/// <summary>Pre-aggregated daily funnel metrics per vertical (UTC calendar day).</summary>
public sealed class DailyMetricsRecord
{
    public BusinessVertical Vertical { get; set; }

    public DateOnly DayUtc { get; set; }

    public int NewLeads { get; set; }

    public int MessagesHandled { get; set; }
}
