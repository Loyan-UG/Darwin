using System;

namespace Darwin.Contracts.Auth;

/// <summary>Login request for password-based authentication.</summary>
public sealed class LoginRequest
{
    /// <summary>User email (normalized server-side).</summary>
    public string Email { get; init; } = default!;

    /// <summary>User password in plain text (TLS-protected).</summary>
    public string Password { get; init; } = default!;

    /// <summary>Optional device identifier for binding/rate-limiting.</summary>
    public string? DeviceId { get; init; }
}

/// <summary>Successful login/refresh response with JWT access and refresh tokens.</summary>
public sealed class TokenResponse
{
    /// <summary>JWT access token (bearer).</summary>
    public string AccessToken { get; init; } = default!;

    /// <summary>UTC expiry for access token.</summary>
    public DateTime AccessTokenExpiresAtUtc { get; init; }

    /// <summary>Opaque refresh token (http-only in web, stored securely in mobile).</summary>
    public string RefreshToken { get; init; } = default!;

    /// <summary>UTC expiry for refresh token.</summary>
    public DateTime RefreshTokenExpiresAtUtc { get; init; }
}

/// <summary>Refresh request to obtain a new access token.</summary>
public sealed class RefreshRequest
{
    /// <summary>Opaque refresh token issued previously.</summary>
    public string RefreshToken { get; init; } = default!;

    /// <summary>Optional device id to enforce device binding policies.</summary>
    public string? DeviceId { get; init; }
}

/// <summary>Logout request to revoke refresh token.</summary>
public sealed class LogoutRequest
{
    /// <summary>Refresh token to revoke.</summary>
    public string RefreshToken { get; init; } = default!;
}
