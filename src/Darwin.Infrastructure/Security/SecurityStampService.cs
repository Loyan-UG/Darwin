using System;
using System.Security.Cryptography;
using System.Text;
using Darwin.Application.Abstractions.Auth;

namespace Darwin.Infrastructure.Security
{
    /// <summary>
    /// Generates and compares security stamps used to invalidate existing sessions/cookies when
    /// sensitive attributes change (password, 2FA setup, etc.).
    /// - NewStamp() returns a high-entropy random token (hex).
    /// - Equals(a,b) performs constant-time comparison to prevent timing attacks.
    /// </summary>
    public sealed class SecurityStampService : ISecurityStampService
    {
        public string NewStamp()
        {
            Span<byte> bytes = stackalloc byte[32];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToHexString(bytes); // upper-case, 64 chars
        }

        public bool Equals(string? a, string? b)
        {
            // Treat null and empty differently; only true when both non-null and equal
            if (a is null || b is null) return false;
            var x = Encoding.UTF8.GetBytes(a);
            var y = Encoding.UTF8.GetBytes(b);
            var result = CryptographicOperations.FixedTimeEquals(x, y);
            Array.Clear(x, 0, x.Length);
            Array.Clear(y, 0, y.Length);
            return result;
        }
    }
}
