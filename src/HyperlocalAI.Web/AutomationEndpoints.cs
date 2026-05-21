using System.Text.Json;
using HyperlocalAI.Application;
using HyperlocalAI.Application.Appointments;
using HyperlocalAI.Application.Automation;
using HyperlocalAI.Infrastructure;
using HyperlocalAI.Infrastructure.Automation;
using Microsoft.Extensions.Options;

namespace HyperlocalAI.Web;

public static class AutomationEndpoints
{
    public static void MapLocalMindAutomation(this WebApplication app)
    {
        app.MapGet("/webhooks/whatsapp", (
            HttpRequest req,
            IOptions<WhatsAppOptions> wa) =>
        {
            var mode = req.Query["hub.mode"].ToString();
            var token = req.Query["hub.verify_token"].ToString();
            var challenge = req.Query["hub.challenge"].ToString();
            if (mode == "subscribe" && !string.IsNullOrEmpty(challenge) && token == wa.Value.VerifyToken)
                return Results.Content(challenge, "text/plain");
            return Results.Unauthorized();
        });

        app.MapPost("/webhooks/whatsapp", async (
            HttpRequest httpRequest,
            IWhatsAppAutomationOrchestrator orchestrator,
            IWhatsAppCloudSender sender,
            IOptions<WhatsAppOptions> wa,
            ILoggerFactory logs,
            CancellationToken cancellationToken) =>
        {
            var log = logs.CreateLogger("WhatsAppWebhook");
            using var reader = new StreamReader(httpRequest.Body);
            var raw = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

            var sig = httpRequest.Headers["X-Hub-Signature-256"].ToString();
            if (!WhatsAppSignatureValidator.IsValid(wa.Value.AppSecret, raw, sig))
            {
                log.LogWarning("Invalid WhatsApp webhook signature");
                return Results.Unauthorized();
            }

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(raw);
            }
            catch (JsonException ex)
            {
                log.LogWarning(ex, "WhatsApp webhook JSON parse failed");
                return Results.BadRequest();
            }

            using (doc)
            {
                if (!doc.RootElement.TryGetProperty("entry", out var entries))
                    return Results.Ok();

                foreach (var entry in entries.EnumerateArray())
                {
                    if (!entry.TryGetProperty("changes", out var changes))
                        continue;
                    foreach (var change in changes.EnumerateArray())
                    {
                        if (!change.TryGetProperty("value", out var value))
                            continue;
                        if (!value.TryGetProperty("metadata", out var metadata))
                            continue;
                        var phoneNumberId = metadata.TryGetProperty("phone_number_id", out var pn)
                            ? pn.GetString() ?? ""
                            : "";

                        string? profileName = null;
                        if (value.TryGetProperty("contacts", out var contacts))
                        {
                            foreach (var c in contacts.EnumerateArray())
                            {
                                if (c.TryGetProperty("profile", out var prof)
                                    && prof.TryGetProperty("name", out var n))
                                {
                                    profileName = n.GetString();
                                    break;
                                }
                            }
                        }

                        if (!value.TryGetProperty("messages", out var messages))
                            continue;

                        foreach (var msg in messages.EnumerateArray())
                        {
                            if (!msg.TryGetProperty("type", out var typeEl) || typeEl.GetString() != "text")
                                continue;
                            var from = msg.TryGetProperty("from", out var f) ? f.GetString() : null;
                            if (string.IsNullOrEmpty(from))
                                continue;
                            if (!msg.TryGetProperty("text", out var textEl)
                                || !textEl.TryGetProperty("body", out var bodyEl))
                                continue;
                            var body = bodyEl.GetString() ?? "";
                            var vertical = WhatsAppVerticalResolver.ResolveVertical(
                                wa,
                                phoneNumberId);

                            string reply;
                            try
                            {
                                reply = await orchestrator
                                    .ProcessInboundAsync(from, profileName, body, vertical, phoneNumberId, cancellationToken)
                                    .ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                log.LogError(ex, "Orchestrator failed for WhatsApp message");
                                reply = "Sorry — something went wrong. Please try again shortly.";
                            }

                            await sender.SendTextAsync(from, reply, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
            }

            return Results.Ok();
        });

        app.MapGet("/api/appointments", async (
            BusinessVertical? vertical,
            IAppointmentBookingService svc,
            CancellationToken ct) =>
        {
            var list = await svc.ListAsync(vertical, ct).ConfigureAwait(false);
            return Results.Json(list);
        });

        app.MapPost("/api/appointments", async (
            CreateAppointmentRequest body,
            IAppointmentBookingService svc,
            IOptions<WhatsAppOptions> wa,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body.CustomerName) || string.IsNullOrWhiteSpace(body.Phone))
                return Results.BadRequest("CustomerName and Phone are required.");
            if (!wa.Value.GetFeaturesFor(body.Vertical).AppointmentBooking)
                return Results.BadRequest("Appointment booking is disabled for this vertical in WhatsApp:VerticalFeatures (AppointmentBooking).");
            var created = await svc.CreateAsync(body, ct).ConfigureAwait(false);
            return Results.Json(created);
        });
    }
}
