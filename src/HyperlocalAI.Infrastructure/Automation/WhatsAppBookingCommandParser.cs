using System.Globalization;
using System.Text.RegularExpressions;
using HyperlocalAI.Application;
using HyperlocalAI.Application.Appointments;

namespace HyperlocalAI.Infrastructure.Automation;

/// <summary>Parses structured booking lines from WhatsApp, e.g. <c>BOOK 2026-04-15 14:30 Jane Doe</c></summary>
public static partial class WhatsAppBookingCommandParser
{
    [GeneratedRegex(@"^BOOK\s+(?<d>\d{4}-\d{2}-\d{2})\s+(?<t>\d{1,2}:\d{2})\s+(?<name>.+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BookLineRegex();

    public static CreateAppointmentRequest? TryParse(string body, BusinessVertical vertical, string phone, string? defaultCustomerName)
    {
        var m = BookLineRegex().Match(body.Trim());
        if (!m.Success)
            return null;

        if (!DateOnly.TryParse(m.Groups["d"].Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return null;
        if (!TimeOnly.TryParse(m.Groups["t"].Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
            return null;

        var local = date.ToDateTime(time);
        var startUtc = DateTime.SpecifyKind(local, DateTimeKind.Utc);

        var name = m.Groups["name"].Value.Trim();
        if (string.IsNullOrEmpty(name))
            name = defaultCustomerName?.Trim() ?? "Customer";

        return new CreateAppointmentRequest
        {
            CustomerName = name,
            Phone = phone,
            Vertical = vertical,
            StartUtc = startUtc,
            DurationMinutes = vertical == BusinessVertical.Clinic ? 30 : 45,
            Channel = "WhatsApp",
            Notes = "Created from WhatsApp BOOK command.",
        };
    }
}
