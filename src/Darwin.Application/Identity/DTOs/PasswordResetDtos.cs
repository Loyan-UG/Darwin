using System;

namespace Darwin.Application.Identity.DTOs
{
    public sealed class RequestPasswordResetDto
    {
        public string Email { get; set; } = string.Empty;
    }

    public sealed class ResetPasswordDto
    {
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty; // one-time
        public string NewPassword { get; set; } = string.Empty;
    }
}
