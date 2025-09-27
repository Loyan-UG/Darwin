using System;

namespace Darwin.Application.Abstractions.Auth
{
    /// <summary>
    /// Issues security stamps (random opaque strings) and offers constant-time comparison.
    /// </summary>
    public interface ISecurityStampService
    {
        /// <summary>Generates a new random stamp (e.g., upon password change, 2FA change).</summary>
        string NewStamp();

        /// <summary>Constant-time equals to prevent timing side-channels.</summary>
        bool AreEqual(string? a, string? b);
    }
}
