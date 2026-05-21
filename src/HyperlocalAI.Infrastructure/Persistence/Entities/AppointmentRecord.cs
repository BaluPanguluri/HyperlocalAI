using HyperlocalAI.Application;
using HyperlocalAI.Application.Appointments;

namespace HyperlocalAI.Infrastructure.Persistence.Entities;

public sealed class AppointmentRecord
{
    public Guid Id { get; set; }

    public Guid? LeadId { get; set; }
    public LeadRecord? Lead { get; set; }

    public string Channel { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string Phone { get; set; } = "";
    public BusinessVertical Vertical { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string? Notes { get; set; }
    public AppointmentStatus Status { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? CancelledUtc { get; set; }
}
