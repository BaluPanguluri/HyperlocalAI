using System;

namespace HyperlocalAI.Infrastructure;

/// <summary>Resolved feature flags for a <see cref="Application.BusinessVertical"/> after merging defaults and config overrides.</summary>
public sealed record WhatsAppVerticalFeatures(bool AutoReply, bool AppointmentBooking);

/// <summary>Optional overrides per vertical (keys: Shop, Clinic, RealEstate). Null properties fall back to built-in defaults.</summary>
public sealed class WhatsAppVerticalFeaturesOverride
{
    public bool? AutoReply { get; set; }

    public bool? AppointmentBooking { get; set; }
}

public sealed class WhatsAppOptions
{
    public const string SectionName = "WhatsApp";

    /// <summary>Webhook verification token (must match Meta dashboard).</summary>
    public string VerifyToken { get; set; } = "";

    /// <summary>WhatsApp phone number id (Graph API).</summary>
    public string PhoneNumberId { get; set; } = "";

    /// <summary>Permanent or long-lived system user token with whatsapp_business_messaging.</summary>
    public string AccessToken { get; set; } = "";

    /// <summary>Optional: validate X-Hub-Signature-256 when set.</summary>
    public string AppSecret { get; set; } = "";

    public string GraphApiVersion { get; set; } = "v21.0";

    /// <summary>When a phone_number_id is not in the map, use this vertical.</summary>
    public string DefaultVertical { get; set; } = nameof(Application.BusinessVertical.Shop);

    /// <summary>Display names used as <see cref="Application.TenantContext.BusinessName"/> for auto-replies.</summary>
    public string ShopBusinessName { get; set; } = "LocalMind Shop";

    public string ClinicBusinessName { get; set; } = "LocalMind Clinic";

    public string RealEstateBusinessName { get; set; } = "LocalMind Real Estate";

    /// <summary>Map Graph <c>phone_number_id</c> string → vertical name (Shop, Clinic, RealEstate).</summary>
    public Dictionary<string, string> PhoneNumberIdToVertical { get; set; } = new();

    /// <summary>
    /// Per-vertical overrides for WhatsApp automation. Built-in defaults: Shop/RealEstate = auto-replies only;
    /// Clinic = auto-replies + appointment booking. Set only the properties you want to change.
    /// </summary>
    public Dictionary<string, WhatsAppVerticalFeaturesOverride> VerticalFeatures { get; set; } = new();

    /// <summary>Sent when <see cref="WhatsAppVerticalFeatures.AutoReply"/> is false for the inbound vertical.</summary>
    public string AutoReplyDisabledMessage { get; set; } =
        "Thanks for your message. Automated replies are turned off for this line — please call or visit us during business hours.";

    /// <summary>
    /// Default click-to-chat URL for the web UI (e.g. <c>https://wa.me/15551234567</c>).
    /// Used when <see cref="ClickToChatUrls"/> has no entry for the selected vertical, or that entry is empty.
    /// </summary>
    public string DefaultClickToChatUrl { get; set; } = "";

    /// <summary>
    /// Optional per-vertical links (keys: Shop, Clinic, RealEstate). Use different WhatsApp numbers per line, or leave empty to inherit <see cref="DefaultClickToChatUrl"/>.
    /// </summary>
    public Dictionary<string, string> ClickToChatUrls { get; set; } = new();

    /// <summary>Resolved https link for the Chat page button, or null when not configured.</summary>
    public string? GetClickToChatUrl(Application.BusinessVertical vertical)
    {
        var key = vertical.ToString();
        if (ClickToChatUrls.TryGetValue(key, out var specific) && TryNormalizeHttpUrl(specific, out var u))
            return u;
        if (TryNormalizeHttpUrl(DefaultClickToChatUrl, out var d))
            return d;
        return null;
    }

    private static bool TryNormalizeHttpUrl(string? raw, out string normalized)
    {
        normalized = "";
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        var t = raw.Trim();
        if (!Uri.TryCreate(t, UriKind.Absolute, out var uri))
            return false;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return false;
        normalized = t;
        return true;
    }

    public WhatsAppVerticalFeatures GetFeaturesFor(Application.BusinessVertical vertical)
    {
        var defaults = DefaultFeatures(vertical);
        if (!VerticalFeatures.TryGetValue(vertical.ToString(), out var o) || o is null)
            return defaults;

        return new WhatsAppVerticalFeatures(
            AutoReply: o.AutoReply ?? defaults.AutoReply,
            AppointmentBooking: o.AppointmentBooking ?? defaults.AppointmentBooking);
    }

    private static WhatsAppVerticalFeatures DefaultFeatures(Application.BusinessVertical vertical) =>
        vertical switch
        {
            Application.BusinessVertical.Clinic =>
                new WhatsAppVerticalFeatures(AutoReply: true, AppointmentBooking: true),
            Application.BusinessVertical.RealEstate =>
                new WhatsAppVerticalFeatures(AutoReply: true, AppointmentBooking: false),
            _ =>
                new WhatsAppVerticalFeatures(AutoReply: true, AppointmentBooking: false),
        };
}
