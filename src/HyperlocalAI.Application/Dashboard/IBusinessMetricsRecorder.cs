namespace HyperlocalAI.Application.Dashboard;

/// <summary>Records engagement for the simple dashboard (first contact = lead, each reply = message handled). Bookings come from <see cref="Appointments.IAppointmentBookingService"/>.</summary>
public interface IBusinessMetricsRecorder
{
    /// <summary>One inbound conversation turn that received an automated reply (web or WhatsApp).</summary>
    void RecordChatInteraction(BusinessVertical vertical, string channel, string contactKey);
}
