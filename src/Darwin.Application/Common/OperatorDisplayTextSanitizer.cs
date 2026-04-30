using System.Text.RegularExpressions;

namespace Darwin.Application.Common
{
    internal static class OperatorDisplayTextSanitizer
    {
        private static readonly Regex EmailPattern = new(
            @"[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));

        private static readonly Regex PhonePattern = new(
            @"(?<!\d)\+?\d[\d\s().-]{6,}\d(?!\d)",
            RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));

        private const string RedactedOperatorFailure = "Delivery failure details were captured but are redacted for operator display.";

        public static string? SanitizeFailureText(string? value, int maxLength = 220)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (ContainsSensitiveMarker(value))
            {
                return RedactedOperatorFailure;
            }

            var sanitized = EmailPattern.Replace(value.Trim(), MaskEmailMatch);
            sanitized = PhonePattern.Replace(sanitized, MaskPhoneMatch);
            return Summarize(sanitized, maxLength);
        }

        private static bool ContainsSensitiveMarker(string value)
        {
            return value.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
                   value.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                   value.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                   value.Contains("authorization", StringComparison.OrdinalIgnoreCase) ||
                   value.Contains("signature", StringComparison.OrdinalIgnoreCase) ||
                   value.Contains("api_key", StringComparison.OrdinalIgnoreCase) ||
                   value.Contains("apikey", StringComparison.OrdinalIgnoreCase);
        }

        private static string MaskEmailMatch(Match match)
        {
            var value = match.Value;
            var at = value.IndexOf('@');
            if (at <= 0)
            {
                return "***";
            }

            var local = value[..at];
            var domain = value[(at + 1)..];
            var prefix = local.Length <= 1 ? local : local[..Math.Min(2, local.Length)];
            return $"{prefix}***@{domain}";
        }

        private static string MaskPhoneMatch(Match match)
        {
            var digits = new string(match.Value.Where(char.IsDigit).ToArray());
            return digits.Length < 4 ? "***" : $"***{digits[^4..]}";
        }

        private static string Summarize(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var trimmed = value.Trim();
            return trimmed.Length <= maxLength ? trimmed : string.Concat(trimmed.AsSpan(0, maxLength - 3), "...");
        }
    }
}
