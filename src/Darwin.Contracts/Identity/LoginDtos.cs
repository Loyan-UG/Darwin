namespace Darwin.Contracts.Identity;

/// <summary>
/// Request payload for password-based authentication in the public Web API.
/// This maps closely to PasswordLoginRequestDto in the Application layer.
/// </summary>
public sealed class PasswordLoginRequest
{
    /// <summary>
    /// Gets or sets the email address used for login.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the plain-text password provided by the user.
    /// Never log this field.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional device identifier.
    /// When provided and JwtRequireDeviceBinding is enabled, the refresh token
    /// will be bound to this device.
    /// </summary>
    public string? DeviceId { get; set; }
}

/// <summary>
/// Request payload used to exchange a refresh token for a new access token.
/// </summary>
public sealed class RefreshTokenRequest
{
    /// <summary>
    /// Gets or sets the opaque refresh token issued by the server.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional device identifier used when the token
    /// was originally issued. If JwtRequireDeviceBinding is enabled,
    /// the value must match to successfully refresh.
    /// </summary>
    public string? DeviceId { get; set; }
}

/// <summary>
/// Standard authentication result for mobile/Web API clients.
/// Wraps access and refresh tokens along with user identity metadata.
/// </summary>
public sealed class TokenResponse
{
    /// <summary>
    /// Gets or sets the signed JWT access token (short-lived).
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp when the access token expires.
    /// </summary>
    public DateTime AccessTokenExpiresAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the opaque refresh token (long-lived).
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp when the refresh token expires.
    /// </summary>
    public DateTime RefreshTokenExpiresAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the authenticated user's identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the authenticated user's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of scopes granted in this token.
    /// This is optional and controlled by JwtEmitScopes site setting.
    /// </summary>
    public IReadOnlyList<string>? Scopes { get; set; }
}


/// <summary>Logout request to revoke refresh token.</summary>
public sealed class LogoutRequest
{
    /// <summary>Refresh token to revoke.</summary>
    public string RefreshToken { get; init; } = default!;
}
