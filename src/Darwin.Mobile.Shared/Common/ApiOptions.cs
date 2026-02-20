namespace Darwin.Mobile.Shared.Common;

/// <summary>
/// Options for configuring the mobile API client and app-specific behavior.
/// </summary>
public sealed class ApiOptions
{
    /// <summary>
    /// Base URL of the Darwin WebApi (e.g., https://api.example.com/).
    /// </summary>
    public string BaseUrl { get; init; } = default!;

    /// <summary>
    /// Audience expected for JWT (provided by bootstrap endpoint).
    /// </summary>
    public string JwtAudience { get; set; } = "Darwin.PublicApi";

    /// <summary>
    /// Suggested QR token refresh interval (seconds) provided by server bootstrap.
    /// </summary>
    public int QrRefreshSeconds { get; set; } = 60;

    /// <summary>
    /// Maximum outbox size before forcing sync.
    /// </summary>
    public int MaxOutbox { get; set; } = 100;

    /// <summary>
    /// Declares the role/type of the mobile app using this shared library.
    /// Used to validate tokens returned by the auth endpoint so Consumer and Business
    /// apps cannot accidentally use tokens intended for the other persona.
    /// Default is Unknown (no validation beyond normal token storage).
    /// </summary>
    public MobileAppRole AppRole { get; set; } = MobileAppRole.Unknown;

    /// <summary>
    /// TEST-ONLY switch:
    /// When true, mobile client bypasses TLS certificate validation.
    /// This is only for temporary test tunnels (e.g. ngrok).
    /// </summary>
    public bool UnsafeTrustAnyServerCertificate { get; set; } = false;

    /// <summary>
    /// When true, login error message includes compact network diagnostics (exception type + hint).
    /// Useful in Release test builds to identify DNS/TLS/connectivity root cause quickly.
    /// </summary>
    public bool EnableVerboseNetworkDiagnostics { get; set; } = false;
}

/// <summary>
/// Mobile app role used by shared mobile components to apply app-specific checks.
/// </summary>
public enum MobileAppRole
{
    /// <summary>No specific app role declared (default).</summary>
    Unknown = 0,

    /// <summary>Consumer app — end user showing QR, using member APIs.</summary>
    Consumer = 1,

    /// <summary>Business app — operator, must present business_id claim in token.</summary>
    Business = 2
}
