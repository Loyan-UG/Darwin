using System;

namespace Darwin.Mobile.Shared.Common;

/// <summary>
/// Produces short user-safe error messages for mobile service Result values.
/// </summary>
public static class MobileErrorMessages
{
    public static string NetworkFailure(string operation)
        => $"Network error while {NormalizeOperation(operation)}. Please try again.";

    public static string InvalidSession()
        => "Your session could not be read. Please sign in again.";

    private static string NormalizeOperation(string operation)
        => string.IsNullOrWhiteSpace(operation) ? "processing the request" : operation.Trim();
}
