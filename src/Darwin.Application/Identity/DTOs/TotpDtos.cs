using System;

namespace Darwin.Application.Identity.DTOs
{
    /// <summary>Starts provisioning: returns SecretBase32 and otpauth URI.</summary>
    public sealed class TotpProvisionDto
    {
        public Guid UserId { get; set; }
        public string Issuer { get; set; } = "Darwin";
        public string? AccountLabelOverride { get; set; } // default: user email
    }

    public sealed class TotpProvisionResult
    {
        public string SecretBase32 { get; set; } = string.Empty;
        public string OtpAuthUri { get; set; } = string.Empty;
    }

    /// <summary>Enables TOTP after verifying the first code.</summary>
    public sealed class TotpEnableDto
    {
        public Guid UserId { get; set; }
        public int Code { get; set; }
    }

    /// <summary>Disables TOTP for the user.</summary>
    public sealed class TotpDisableDto
    {
        public Guid UserId { get; set; }
    }

    /// <summary>Verifies a TOTP code during login (server-side challenge).</summary>
    public sealed class TotpVerifyDto
    {
        public Guid UserId { get; set; }
        public int Code { get; set; }
    }
}
