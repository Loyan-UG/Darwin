using System;

namespace Darwin.Application.Identity.DTOs
{
    /// <summary>Credentials posted from login form.</summary>
    public sealed class SignInDto
    {
        public string Email { get; set; } = string.Empty;     // Case-insensitive; Infra should normalize
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; } = false;
        // TODO: Add captcha/anti-bot signal if needed.
    }

    /// <summary>Result of sign-in flow. Cookie issuance is done in Web, not here.</summary>
    public sealed class SignInResultDto
    {
        public bool Succeeded { get; set; }
        public bool RequiresTwoFactor { get; set; }
        public string? TwoFactorDelivery { get; set; } // e.g., "TOTP"
        public string? FailureReason { get; set; }
        public Guid? UserId { get; set; }
        public string? SecurityStamp { get; set; }     // Web can include this in the auth ticket
    }

    /// <summary>Password-reset request input.</summary>
    public sealed class PasswordResetRequestDto
    {
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>Confirm password reset with token.</summary>
    public sealed class PasswordResetConfirmDto
    {
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;  // Opaque token previously emailed
        public string NewPassword { get; set; } = string.Empty;
    }



    /// <summary>
    /// Request for username/password login. Username is email in current Domain model.
    /// </summary>
    public sealed class PasswordLoginRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string PasswordPlain { get; set; } = string.Empty;

        /// <summary>
        /// Optional device identifier (e.g., mobile installation id) for device-bound refresh tokens.
        /// </summary>
        public string? DeviceId { get; set; }
    }

    /// <summary>
    /// Response carrying short-lived JWT access token and opaque refresh token.
    /// </summary>
    public sealed class AuthResultDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpiresAtUtc { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenExpiresAtUtc { get; set; }
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to exchange a refresh token for a new access token.
    /// </summary>
    public sealed class RefreshRequestDto
    {
        public string RefreshToken { get; set; } = string.Empty;
        public string? DeviceId { get; set; }
    }

    /// <summary>
    /// Request to revoke refresh tokens.
    /// </summary>
    public sealed class RevokeRefreshRequestDto
    {
        public string? RefreshToken { get; set; }
        public Guid? UserId { get; set; }
        public string? DeviceId { get; set; }
    }
}
