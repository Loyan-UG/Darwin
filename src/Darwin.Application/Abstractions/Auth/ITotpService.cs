using System;

namespace Darwin.Application.Abstractions.Auth
{
    /// <summary>
    /// Abstraction for TOTP generation/verification (RFC 6238).
    /// Infrastructure will use a concrete library. Application consumes it for 2FA flows.
    /// </summary>
    public interface ITotpService
    {
        // Verify a code against a shared secret. window: allowed drift in steps.
        bool VerifyCode(string base32Secret, string code, int window = 1);
        // Generate a code mainly for tests/admin preview.
        string GenerateCode(string base32Secret);
    }
}
