using System;
using System.Security.Cryptography;
using Darwin.Application.Abstractions.Auth;

namespace Darwin.Infrastructure.Security
{
    /// <summary>
    /// Issues opaque stamps and compares them using constant-time equality.
    /// </summary>
    public sealed class SecurityStampService : ISecurityStampService
    {
        public string NewStamp()
        {
            Span<byte> bytes = stackalloc byte[16];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToHexString(bytes); // 32 hex chars
        }

        public bool AreEqual(string? a, string? b)
        {
            // constant-time compare
            if (a is null || b is null) return a == b;
            var ba = System.Text.Encoding.UTF8.GetBytes(a);
            var bb = System.Text.Encoding.UTF8.GetBytes(b);
            return CryptographicOperations.FixedTimeEquals(ba, bb);
        }
    }
}
