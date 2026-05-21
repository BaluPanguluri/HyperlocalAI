using System.Security.Cryptography;
using System.Text;

namespace HyperlocalAI.Infrastructure.Automation;

public static class WhatsAppSignatureValidator
{
    public static bool IsValid(string? appSecret, string rawBody, string? signatureHeader)
    {
        if (string.IsNullOrEmpty(appSecret))
            return true;
        if (string.IsNullOrEmpty(signatureHeader) || !signatureHeader.StartsWith("sha256=", StringComparison.Ordinal))
            return false;

        var hex = signatureHeader.AsSpan("sha256=".Length);
        byte[] expected;
        try
        {
            expected = Convert.FromHexString(hex);
        }
        catch
        {
            return false;
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
        var actual = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody));
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
