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
}
