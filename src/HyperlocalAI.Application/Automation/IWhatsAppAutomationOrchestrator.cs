namespace HyperlocalAI.Application.Automation;

/// <summary>Handles inbound WhatsApp text: optional structured booking + auto-reply using the same AI/knowledge stack as the web chat.</summary>
public interface IWhatsAppAutomationOrchestrator
{
    /// <summary>Produces the outbound reply text (caller sends it via WhatsApp Cloud API).</summary>
    Task<string> ProcessInboundAsync(
        string fromPhoneE164,
        string? profileName,
        string messageBody,
        BusinessVertical vertical,
        string phoneNumberId,
        CancellationToken cancellationToken = default);
}
