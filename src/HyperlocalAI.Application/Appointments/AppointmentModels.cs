namespace HyperlocalAI.Application.Appointments;

public enum AppointmentStatus
{
    Scheduled = 0,
    Cancelled = 1,
    Completed = 2,
}

public sealed record AppointmentDto(
    Guid Id,
    string Channel,
    string CustomerName,
    string Phone,
    BusinessVertical Vertical,
    DateTime StartUtc,
    DateTime EndUtc,
    string? Notes,
    AppointmentStatus Status,
    DateTime CreatedUtc);

public sealed class CreateAppointmentRequest
{
    public string CustomerName { get; set; } = "";

    public string Phone { get; set; } = "";

    public BusinessVertical Vertical { get; set; }

    public DateTime StartUtc { get; set; }

    public int DurationMinutes { get; set; } = 30;

    public string? Notes { get; set; }

    /// <summary>e.g. WhatsApp, Web</summary>
    public string Channel { get; set; } = "Web";
}
