using System;

namespace Darwin.Application.Identity.DTOs
{
    public sealed class BeginTotpProvisioningDto
    {
        public Guid UserId { get; set; }
        public string Label { get; set; } = string.Empty; // often email
        public string Issuer { get; set; } = "Darwin";
    }

    public sealed class TotpProvisioningResultDto
    {
        public string SecretBase32 { get; set; } = string.Empty;
        public string OtpAuthUri { get; set; } = string.Empty; // for QR
    }

    public sealed class VerifyTotpDto
    {
        public Guid UserId { get; set; }
        public string Code { get; set; } = string.Empty;
    }

    public sealed class DisableTotpDto
    {
        public Guid UserId { get; set; }
    }
}
