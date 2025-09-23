using System;
using System.Security.Cryptography;

namespace Darwin.Shared.Security
{
    /// <summary>
    /// Cryptographically-strong random token generator for one-time tokens and secrets.
    /// </summary>
    public static class RandomTokenGenerator
    {
        /// <summary>Generates URL-safe base64 token of given bytes length (default 32 bytes ~ 43 chars).</summary>
        public static string UrlSafeToken(int bytes = 32)
        {
            Span<byte> buffer = stackalloc byte[bytes];
            RandomNumberGenerator.Fill(buffer);
            return Convert.ToBase64String(buffer).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }
}
