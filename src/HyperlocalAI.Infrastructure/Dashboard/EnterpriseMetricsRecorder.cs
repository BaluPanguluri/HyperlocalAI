using HyperlocalAI.Application;
using HyperlocalAI.Application.Dashboard;
using HyperlocalAI.Application.Leads;
using HyperlocalAI.Infrastructure.Persistence;
using HyperlocalAI.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace HyperlocalAI.Infrastructure.Dashboard;

/// <summary>Persists leads and daily funnel metrics (SQLite / SQL-backed).</summary>
public sealed class EnterpriseMetricsRecorder : IBusinessMetricsRecorder
{
    private readonly IDbContextFactory<LocalMindDbContext> _factory;

    public EnterpriseMetricsRecorder(IDbContextFactory<LocalMindDbContext> factory)
    {
        _factory = factory;
    }

    public void RecordChatInteraction(BusinessVertical vertical, string channel, string contactKey)
    {
        using var db = _factory.CreateDbContext();
        var normalizedChannel = channel.Trim().ToLowerInvariant();
        var keyBody = normalizedChannel == "whatsapp"
            ? new string(contactKey.Where(char.IsDigit).ToArray())
            : NormalizeContact(contactKey);
        if (normalizedChannel == "whatsapp" && keyBody.Length == 0)
            keyBody = NormalizeContact(contactKey);
        var fullKey = $"{normalizedChannel}:{keyBody}";
        var day = DateOnly.FromDateTime(DateTime.UtcNow);
        var source = MapSource(normalizedChannel);

        var lead = db.Leads.FirstOrDefault(l => l.Vertical == vertical && l.ContactKey == fullKey);
        var isNew = lead is null;
        if (isNew)
        {
            lead = new LeadRecord
            {
                Id = Guid.NewGuid(),
                Vertical = vertical,
                Channel = normalizedChannel,
                ContactKey = fullKey,
                Stage = LeadStage.New,
                Source = source,
                LeadScore = 0,
                FirstSeenUtc = DateTime.UtcNow,
                LastActivityUtc = DateTime.UtcNow,
                PhoneE164 = TryPhoneFromKey(normalizedChannel, keyBody),
            };
            db.Leads.Add(lead);
        }
        else
        {
            lead!.LastActivityUtc = DateTime.UtcNow;
        }

        var daily = db.DailyMetrics.FirstOrDefault(d => d.Vertical == vertical && d.DayUtc == day);
        if (daily is null)
        {
            daily = new DailyMetricsRecord { Vertical = vertical, DayUtc = day };
            db.DailyMetrics.Add(daily);
        }

        daily.MessagesHandled++;
        if (isNew)
            daily.NewLeads++;

        db.SaveChanges();
    }

    private static string MapSource(string normalizedChannel) =>
        normalizedChannel switch
        {
            "web" => "WebChat",
            "whatsapp" => "WhatsApp",
            _ => normalizedChannel,
        };

    private static string? TryPhoneFromKey(string channel, string keyBody) =>
        channel == "whatsapp" && keyBody.Length > 0 ? keyBody : null;

    private static string NormalizeContact(string contactKey)
    {
        var t = contactKey.Trim();
        return t.Length <= 200 ? t : t[..200];
    }
}
