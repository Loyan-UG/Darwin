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
}
