using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Darwin.WebApi.Services;

/// <summary>
/// Verifies Stripe webhook signatures against the configured endpoint secret.
/// </summary>
public sealed class StripeWebhookSignatureVerifier
{
    private static readonly TimeSpan DefaultTolerance = TimeSpan.FromMinutes(10);

    public bool TryVerify(
        string payload,
        string? signatureHeader,
        string secret,
        out string errorKey)
    {
        errorKey = "StripeWebhookSignatureInvalid";

        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            errorKey = "StripeWebhookSignatureHeaderRequired";
            return false;
        }

        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(payload))
        {
            return false;
        }

        if (!TryParseSignatureHeader(signatureHeader, out var timestamp, out var signatures) ||
            signatures.Count == 0)
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (Math.Abs(now - timestamp) > (long)DefaultTolerance.TotalSeconds)
        {
            return false;
        }

        var signedPayload = $"{timestamp.ToString(CultureInfo.InvariantCulture)}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));

        foreach (var signature in signatures)
        {
            if (!TryDecodeHex(signature, out var providedHash))
            {
                continue;
            }

            if (CryptographicOperations.FixedTimeEquals(computedHash, providedHash))
            {
                errorKey = string.Empty;
                return true;
            }
        }

        return false;
    }

    private static bool TryParseSignatureHeader(string header, out long timestamp, out List<string> signatures)
    {
        timestamp = 0;
        signatures = new List<string>();

        var parts = header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            var keyValue = part.Split('=', 2, StringSplitOptions.TrimEntries);
            if (keyValue.Length != 2)
            {
                continue;
            }

            if (string.Equals(keyValue[0], "t", StringComparison.OrdinalIgnoreCase) &&
                long.TryParse(keyValue[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedTimestamp))
            {
                timestamp = parsedTimestamp;
                continue;
            }

            if (string.Equals(keyValue[0], "v1", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(keyValue[1]))
            {
                signatures.Add(keyValue[1]);
            }
        }

        return timestamp > 0;
    }

    private static bool TryDecodeHex(string value, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        if (string.IsNullOrWhiteSpace(value) || value.Length % 2 != 0)
        {
            return false;
        }

        try
        {
            bytes = Convert.FromHexString(value);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
