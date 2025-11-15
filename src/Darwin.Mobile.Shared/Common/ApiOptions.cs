namespace Darwin.Mobile.Shared.Common;

/// <summary>
/// Options for configuring the mobile API client.
/// </summary>
public sealed class ApiOptions
{
    /// <summary>Base URL of the Darwin WebApi (e.g., https://api.example.com/).</summary>
    public string BaseUrl { get; init; } = default!;

    /// <summary>Audience expected for JWT (provided by bootstrap endpoint).</summary>
    public string JwtAudience { get; set; } = "Darwin.PublicApi";

    /// <summary>Suggested QR token refresh interval (seconds) provided by server bootstrap.</summary>
    public int QrRefreshSeconds { get; set; } = 60;

    /// <summary>Maximum outbox size before forcing sync.</summary>
    public int MaxOutbox { get; set; } = 100;
}
