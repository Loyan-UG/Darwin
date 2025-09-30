using System;
using System.Security.Cryptography;
using System.Text;
using Darwin.Application.Abstractions.Auth;

namespace Darwin.Infrastructure.Security
{
    /// <summary>
    ///     RFC 6238 TOTP service with HMAC-SHA1, 30-second steps, 6 digits.
    ///     Matches common authenticator apps. This implementation is self-contained.
    /// </summary>
    public sealed class TotpService : ITotpService
    {
        private const int StepSeconds = 30;
        private const int Digits = 6;

        /// <summary>
        ///     Verifies a user-supplied TOTP code against a Base32-encoded secret.
        ///     Allows a small step drift (default: ±1 step).
        /// </summary>
        /// <param name="base32Secret">Shared secret in Base32 (no padding).</param>
        /// <param name="code">Code from the authenticator application (string of digits).</param>
        /// <param name="window">Allowed drift in time-steps (e.g., 1 = ±30s).</param>
        /// <returns>True if code is valid for current time within the given window.</returns>
        public bool VerifyCode(string base32Secret, string code, int window = 1)
        {
            if (string.IsNullOrWhiteSpace(base32Secret) || string.IsNullOrWhiteSpace(code))
                return false;
            if (!int.TryParse(code, out var codeInt)) return false;

            var utc = DateTime.UtcNow;
            for (var w = -window; w <= window; w++)
            {
                var computed = ComputeTotp(base32Secret, utc.AddSeconds(w * StepSeconds));
                if (computed == codeInt) return true;
            }
            return false;
        }

        /// <summary>
        ///     Generates the current TOTP code for testing/admin previews.
        /// </summary>
        /// <param name="base32Secret">Shared secret in Base32 (no padding).</param>
        /// <returns>Numeric code rendered as string (zero-padded).</returns>
        public string GenerateCode(string base32Secret)
        {
            var code = ComputeTotp(base32Secret, DateTime.UtcNow);
            return code.ToString(new string('0', Digits));
        }

        private static int ComputeTotp(string base32Secret, DateTime utc)
        {
            var key = Base32Decode(base32Secret);
            var timestep = (long)Math.Floor((utc - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds / StepSeconds);
            var counter = BitConverter.GetBytes(timestep);
            if (BitConverter.IsLittleEndian) Array.Reverse(counter);

            using var hmac = new HMACSHA1(key);
            var hash = hmac.ComputeHash(counter);
            var offset = hash[^1] & 0x0F;
            var binary =
                ((hash[offset] & 0x7f) << 24) |
                ((hash[offset + 1] & 0xff) << 16) |
                ((hash[offset + 2] & 0xff) << 8) |
                (hash[offset + 3] & 0xff);

            var code = binary % (int)Math.Pow(10, Digits);
            return code;
        }

        // Minimal Base32 (RFC4648) decode for secrets; expects uppercase alphabet without padding.
        private static byte[] Base32Decode(string input)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            var normalized = input.Replace("=", string.Empty).ToUpperInvariant();
            var bits = 0;
            var value = 0;
            var output = new System.Collections.Generic.List<byte>(normalized.Length * 5 / 8);

            foreach (var c in normalized)
            {
                var idx = alphabet.IndexOf(c);
                if (idx < 0) throw new FormatException("Invalid Base32 character.");
                value = (value << 5) | idx;
                bits += 5;
                if (bits >= 8)
                {
                    output.Add((byte)((value >> (bits - 8)) & 0xFF));
                    bits -= 8;
                }
            }
            return output.ToArray();
        }
    }
}
