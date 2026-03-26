using System;
using Darwin.Mobile.Consumer.Resources;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// Centralized mapper that translates low-level exception details into safe, user-facing messages.
/// This prevents leaking transport/server internals in UI-bound view models.
/// </summary>
internal static class ViewModelErrorMapper
{
    /// <summary>
    /// Maps an exception to a generic connectivity/operation message for end users.
    /// </summary>
    /// <param name="ex">The source exception thrown by service or transport layers.</param>
    /// <param name="fallback">Fallback message used when no specific mapping applies.</param>
    /// <returns>A safe, user-facing localized message.</returns>
    public static string ToUserMessage(Exception ex, string fallback)
    {
        var raw = ex.Message ?? string.Empty;

        if (raw.Contains("Network error", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("timed out", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("No such host", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("Name or service not known", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("SSL", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("certificate", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("connection", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("invalid_requesturi", StringComparison.OrdinalIgnoreCase))
        {
            return AppResources.ServerUnreachableMessage;
        }

        if (raw.Contains("401", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase))
        {
            return AppResources.SessionExpiredReLogin;
        }

        if (raw.Contains("Email address is not confirmed", StringComparison.OrdinalIgnoreCase))
        {
            return AppResources.LoginEmailConfirmationRequired;
        }

        if (raw.Contains("Account is locked", StringComparison.OrdinalIgnoreCase))
        {
            return AppResources.LoginAccountLocked;
        }

        if (raw.Contains("409", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("concurrency", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("rowversion", StringComparison.OrdinalIgnoreCase))
        {
            return AppResources.ProfileConcurrencyConflict;
        }

        return fallback;
    }
}
