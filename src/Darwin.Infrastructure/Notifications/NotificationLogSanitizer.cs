using System;

namespace Darwin.Infrastructure.Notifications;

internal static class NotificationLogSanitizer
{
    public static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "<empty>";
        }

        var normalized = email.Trim();
        var at = normalized.IndexOf('@');
        if (at <= 0)
        {
            return "***";
        }

        var local = normalized[..at];
        var domain = normalized[(at + 1)..];
        var localPrefix = local.Length <= 1 ? local : local[..Math.Min(2, local.Length)];

        return $"{localPrefix}***@{domain}";
    }

    public static string MaskPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return "<empty>";
        }

        var normalized = phone.Trim();
        var visible = Math.Min(4, normalized.Length);
        return $"***{normalized[^visible..]}";
    }

    public static string ProviderFailure(string provider, int statusCode)
        => $"{NormalizeProvider(provider)} rejected the notification request with HTTP {statusCode}.";

    public static string TransportFailure(string provider)
        => $"{NormalizeProvider(provider)} notification dispatch failed before receiving a provider response.";

    private static string NormalizeProvider(string provider)
        => string.IsNullOrWhiteSpace(provider) ? "Provider" : provider.Trim();
}
