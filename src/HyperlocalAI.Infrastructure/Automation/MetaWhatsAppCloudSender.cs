using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HyperlocalAI.Application.Automation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HyperlocalAI.Infrastructure.Automation;

public sealed class MetaWhatsAppCloudSender : IWhatsAppCloudSender
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<WhatsAppOptions> _options;
    private readonly ILogger<MetaWhatsAppCloudSender> _logger;

    public MetaWhatsAppCloudSender(
        IHttpClientFactory httpClientFactory,
        IOptions<WhatsAppOptions> options,
        ILogger<MetaWhatsAppCloudSender> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.Value.AccessToken)
        && !string.IsNullOrWhiteSpace(_options.Value.PhoneNumberId);

    public async Task SendTextAsync(string toE164Digits, string body, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("WhatsApp Cloud API not configured — set WhatsApp:PhoneNumberId and WhatsApp:AccessToken to send replies.");
            return;
        }

        var o = _options.Value;
        var to = new string(toE164Digits.Where(char.IsDigit).ToArray());
        var url =
            $"https://graph.facebook.com/{o.GraphApiVersion.Trim('/')}/{o.PhoneNumberId.Trim()}/messages";

        var payload = new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to,
            type = "text",
            text = new { preview_url = false, body },
        };

        var json = JsonSerializer.Serialize(payload);
        var client = _httpClientFactory.CreateClient("MetaWhatsApp");
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", o.AccessToken);
        req.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var res = await client.SendAsync(req, cancellationToken).ConfigureAwait(false);
        var responseBody = await res.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!res.IsSuccessStatusCode)
            _logger.LogError("WhatsApp send failed {Status}: {Body}", res.StatusCode, responseBody);
    }
}
