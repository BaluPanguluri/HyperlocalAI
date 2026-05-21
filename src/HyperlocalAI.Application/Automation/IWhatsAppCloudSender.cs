namespace HyperlocalAI.Application.Automation;

/// <summary>Sends WhatsApp Cloud API messages (Meta Graph). No-op when credentials are missing.</summary>
public interface IWhatsAppCloudSender
{
    bool IsConfigured { get; }

    Task SendTextAsync(string toE164Digits, string body, CancellationToken cancellationToken = default);
}
