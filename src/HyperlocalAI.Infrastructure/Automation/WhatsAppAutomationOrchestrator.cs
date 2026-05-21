using System.Text;
using HyperlocalAI.Application;
using HyperlocalAI.Application.Appointments;
using HyperlocalAI.Application.Automation;
using HyperlocalAI.Application.Dashboard;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HyperlocalAI.Infrastructure.Automation;

public sealed class WhatsAppAutomationOrchestrator : IWhatsAppAutomationOrchestrator
{
    private readonly IHyperlocalChatService _chat;
    private readonly IAppointmentBookingService _appointments;
    private readonly IOptions<WhatsAppOptions> _whatsappOptions;
    private readonly IBusinessMetricsRecorder _metrics;
    private readonly ILogger<WhatsAppAutomationOrchestrator> _logger;

    public WhatsAppAutomationOrchestrator(
        IHyperlocalChatService chat,
        IAppointmentBookingService appointments,
        IOptions<WhatsAppOptions> whatsappOptions,
        IBusinessMetricsRecorder metrics,
        ILogger<WhatsAppAutomationOrchestrator> logger)
    {
        _chat = chat;
        _appointments = appointments;
        _whatsappOptions = whatsappOptions;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<string> ProcessInboundAsync(
        string fromPhoneE164,
        string? profileName,
        string messageBody,
        BusinessVertical vertical,
        string phoneNumberId,
        CancellationToken cancellationToken = default)
    {
        var features = _whatsappOptions.Value.GetFeaturesFor(vertical);

        if (features.AppointmentBooking)
        {
            var booking = WhatsAppBookingCommandParser.TryParse(messageBody, vertical, fromPhoneE164, profileName);
            if (booking is not null)
            {
                try
                {
                    var appt = await _appointments.CreateAsync(booking, cancellationToken).ConfigureAwait(false);
                    var text = FormatBookingConfirmation(appt, vertical);
                    _metrics.RecordChatInteraction(vertical, "whatsapp", fromPhoneE164);
                    return text;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Appointment create failed for WhatsApp user {From}", fromPhoneE164);
                    var err = "Sorry — we could not save that booking. Please try again or call the clinic.";
                    _metrics.RecordChatInteraction(vertical, "whatsapp", fromPhoneE164);
                    return err;
                }
            }
        }

        if (!features.AutoReply)
        {
            _metrics.RecordChatInteraction(vertical, "whatsapp", fromPhoneE164);
            return _whatsappOptions.Value.AutoReplyDisabledMessage;
        }

        var business = WhatsAppVerticalResolver.BusinessName(_whatsappOptions, vertical);
        var tenant = new TenantContext(
            TenantId: $"wa-{SanitizeTenantId(fromPhoneE164)}",
            BusinessName: business,
            Vertical: vertical);

        var prefixed = PrefixForVertical(vertical) + messageBody;
        var chatReply = await _chat.ChatAsync(
                new ChatRequest(prefixed, tenant, SkipEngagementRecording: true),
                cancellationToken)
            .ConfigureAwait(false);
        _metrics.RecordChatInteraction(vertical, "whatsapp", fromPhoneE164);
        return chatReply.Answer;
    }

    private static string FormatBookingConfirmation(AppointmentDto appt, BusinessVertical vertical)
    {
        var sb = new StringBuilder();
        sb.Append("✅ *Booking confirmed*\n");
        sb.Append("• Reference: `").Append(appt.Id.ToString("N")[..8]).Append("…`\n");
        sb.Append("• When (UTC): ").Append(appt.StartUtc.ToString("yyyy-MM-dd HH:mm")).Append(" – ").Append(appt.EndUtc.ToString("HH:mm")).Append('\n');
        sb.Append("• Type: ").Append(vertical).Append('\n');
        sb.Append("• Name: ").Append(appt.CustomerName);
        return sb.ToString();
    }

    private static string PrefixForVertical(BusinessVertical vertical) =>
        vertical switch
        {
            BusinessVertical.Shop =>
                "[WhatsApp · retail] ",
            BusinessVertical.Clinic =>
                "[WhatsApp · clinic / health — not for emergencies] ",
            BusinessVertical.RealEstate =>
                "[WhatsApp · real estate] ",
            _ => "[WhatsApp] ",
        };

    private static string SanitizeTenantId(string phone) =>
        new string(phone.Where(char.IsDigit).ToArray());
}
