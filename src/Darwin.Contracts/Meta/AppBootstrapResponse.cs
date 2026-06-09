namespace Darwin.Contracts.Meta;

/// <summary>Minimal configuration payload for mobile apps after login.</summary>
public sealed class AppBootstrapResponse
{
    /// <summary>JWT audience the app should use; must match server config.</summary>
    public string JwtAudience { get; init; } = "Darwin.PublicApi";

    /// <summary>QR token refresh interval (seconds) suggested by server.</summary>
    public int QrTokenRefreshSeconds { get; init; } = 60;

    /// <summary>Max offline queue size before forcing sync.</summary>
    public int MaxOutboxItems { get; init; } = 100;

    /// <summary>Whether Google external login is enabled for member-facing clients.</summary>
    public bool GoogleExternalLoginEnabled { get; init; }

    /// <summary>Google OAuth client id for Android clients. This value is not a secret.</summary>
    public string? GoogleExternalLoginAndroidClientId { get; init; }

    /// <summary>Google OAuth client id for iOS/MacCatalyst clients. This value is not a secret.</summary>
    public string? GoogleExternalLoginIosClientId { get; init; }

    /// <summary>Google OAuth client id for Web/front-office clients. This value is not a secret.</summary>
    public string? GoogleExternalLoginWebClientId { get; init; }
}
