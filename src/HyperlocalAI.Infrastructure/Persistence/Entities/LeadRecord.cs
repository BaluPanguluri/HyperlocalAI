using HyperlocalAI.Application;
using HyperlocalAI.Application.Leads;

namespace HyperlocalAI.Infrastructure.Persistence.Entities;

public sealed class LeadRecord
{
    public Guid Id { get; set; }

    public BusinessVertical Vertical { get; set; }

    /// <summary>Short channel id, e.g. web, whatsapp.</summary>
    public string Channel { get; set; } = "";

    /// <summary>Stable key within the vertical (e.g. web:tenant-1, whatsapp:15551234567).</summary>
    public string ContactKey { get; set; } = "";

    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string? PhoneE164 { get; set; }

    public LeadStage Stage { get; set; }

    /// <summary>Attribution: WebChat, WhatsApp, Campaign, Import, etc.</summary>
    public string Source { get; set; } = "Unknown";

    public int LeadScore { get; set; }

    public string? Notes { get; set; }

    public DateTime FirstSeenUtc { get; set; }
    public DateTime LastActivityUtc { get; set; }
    public DateTime? WonAtUtc { get; set; }
    public DateTime? LostAtUtc { get; set; }
    public string? LostReason { get; set; }

    public ICollection<AppointmentRecord> Appointments { get; set; } = new List<AppointmentRecord>();
}
