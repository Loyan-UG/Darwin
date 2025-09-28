using System;
using System.Security.Cryptography;
using System.Text;
using Darwin.Application.Abstractions.Auth;

namespace Darwin.Infrastructure.Security
{
    /// <summary>
    /// Creates opaque security stamps and compares them using constant-time comparison
    /// to reduce timing side-channel risks.
    /// </summary>
    public sealed class SecurityStampService : ISecurityStampService
    {
        /// <summary>
        /// Generates a 256-bit random value encoded as hex (lowercase).
        /// </summary>
        public string NewStamp()
        {
            Span<byte> bytes = stackalloc byte[32];
            RandomNumberGenerator.Fill(bytes);

            var sb = new StringBuilder(64);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        /// <summary>
        /// Compares two strings in (roughly) constant time. Nulls are treated as empty.
        /// </summary>
        public bool AreEqual(string? a, string? b)
        {
            var x = a ?? string.Empty;
            var y = b ?? string.Empty;
            if (x.Length != y.Length) return false;

            var diff = 0;
            for (int i = 0; i < x.Length; i++)
                diff |= x[i] ^ y[i];

            return diff == 0;
        }

        // Backward-compat overload if older code accidentally called Equals(a,b)
        /// <summary>Legacy overload – prefer <see cref="AreEqual"/> instead.</summary>
        public bool Equals(string? a, string? b) => AreEqual(a, b);
    }
}
