using System;
using System.Security.Cryptography;

namespace Darwin.Shared.Security
{
    /// <summary>
    /// Generates cryptographically-strong opaque tokens suitable for short-lived QR presentation.
    /// Uses 256-bit random and encodes with Base64Url without padding.
    /// </summary>
    public static class OpaqueTokenGenerator
    {
        /// <summary>
        /// Creates a cryptographically-strong random token, Base64Url-encoded, with no padding.
        /// </summary>
        public static string Create(int numBytes = 32)
        {
            if (numBytes < 16) numBytes = 16; // enforce minimum strength
            var bytes = new byte[numBytes];
            RandomNumberGenerator.Fill(bytes);
            return Base64UrlEncode(bytes);
        }

        /// <summary>
        /// Base64Url encoded string without padding.
        /// </summary>
        private static string Base64UrlEncode(byte[] input)
        {
            var s = Convert.ToBase64String(input);
            s = s.Replace('+', '-').Replace('/', '_').TrimEnd('=');
            return s;
        }
    }
}
