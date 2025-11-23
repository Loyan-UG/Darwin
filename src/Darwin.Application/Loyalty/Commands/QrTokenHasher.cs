using System;
using System.Security.Cryptography;
using System.Text;

namespace Darwin.Application.Loyalty.Common
{
    /// <summary>
    /// Provides a deterministic hashing mechanism for QR tokens.
    /// Tokens are stored hashed at-rest to prevent replay if the database leaks.
    /// </summary>
    internal static class QrTokenHasher
    {
        /// <summary>
        /// Computes a SHA-256 hex hash for the given token.
        /// </summary>
        public static string Hash(string token)
        {
            ArgumentNullException.ThrowIfNull(token);

            var bytes = Encoding.UTF8.GetBytes(token);
            var hashBytes = SHA256.HashData(bytes);

            return Convert.ToHexString(hashBytes);
        }
    }
}
