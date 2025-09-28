using System;

namespace Darwin.Application.Abstractions.Auth
{
    /// <summary>
    /// Generates and compares security stamps. Security stamps must change when
    /// credentials or sensitive factors change to invalidate existing sessions.
    /// </summary>
    public interface ISecurityStampService
    {
        /// <summary>Returns a freshly generated random security stamp.</summary>
        string NewStamp();

        /// <summary>Constant-time comparison helper for security stamp strings.</summary>
        bool AreEqual(string? a, string? b);
    }
}
