using System;

namespace Darwin.Application.Identity.DTOs
{
    public sealed class RequestPasswordResetDto
    {
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// Requests a new email-confirmation token for the specified account.
    /// </summary>
    public sealed class RequestEmailConfirmationDto
    {
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// Completes email confirmation using an opaque one-time token.
    /// </summary>
    public sealed class ConfirmEmailDto
    {
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }

    public sealed class ResetPasswordDto
    {
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty; // one-time
        public string NewPassword { get; set; } = string.Empty;
    }
}
