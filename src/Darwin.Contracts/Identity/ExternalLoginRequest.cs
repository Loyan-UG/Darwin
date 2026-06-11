using System;

namespace Darwin.Contracts.Identity;

/// <summary>
/// Request payload for exchanging a verified external identity token for Darwin tokens.
/// The raw provider token must never be logged.
/// </summary>
public sealed class ExternalLoginRequest
{
    /// <summary>
    /// External provider key, for example Google.
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Provider-issued ID token. Never log this value.
    /// </summary>
    public string IdToken { get; set; } = string.Empty;

    /// <summary>
    /// Optional device identifier used for refresh-token binding.
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>
    /// Optional preferred business context for business-facing clients.
    /// </summary>
    public Guid? BusinessId { get; set; }

    /// <summary>
    /// Allows the provider-verified identity to create a new local member account
    /// when no matching Darwin account exists. Login screens should keep this
    /// disabled; registration screens may enable it explicitly.
    /// </summary>
    public bool AllowAccountCreation { get; set; }
}
