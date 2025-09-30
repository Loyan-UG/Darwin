using System;
using System.Security.Cryptography;
using System.Text;

namespace Darwin.Application.Identity.Services
{
    /// <summary>
    /// TOTP helper based on RFC 6238 using HMAC-SHA1, 30-second steps, 6 digits.
    /// Provides Base32 encoding/decoding for secrets and otpauth URI generation.
    /// </summary>
    public static class TotpUtility
    {
        /// <summary>Generates a new random secret (20 bytes) and returns Base32 (no padding).</summary>
        public static string GenerateSecretBase32(int byteLength = 20)
        {
            var bytes = new byte[byteLength];
            RandomNumberGenerator.Fill(bytes);
            return Base32Encode(bytes);
        }

        /// <summary>Builds an otpauth:// URI for QR provisioning.</summary>
        public static string BuildOtpAuthUri(string issuer, string accountLabel, string secretBase32, int digits = 6, int period = 30, string algorithm = "SHA1")
        {
            // otpauth://totp/{issuer}:{account}?secret=...&issuer=...&digits=6&period=30&algorithm=SHA1
            var label = Uri.EscapeDataString($"{issuer}:{accountLabel}");
            var iss = Uri.EscapeDataString(issuer);
            var sec = Uri.EscapeDataString(secretBase32);
            return $"otpauth://totp/{label}?secret={sec}&issuer={iss}&digits={digits}&period={period}&algorithm={algorithm}";
        }

        /// <summary>Computes a TOTP code for the given secret at the given UTC time.</summary>
        public static int ComputeTotpCode(string secretBase32, DateTime utc, int digits = 6, int periodSeconds = 30)
        {
            var key = Base32Decode(secretBase32);
            var counter = (long)Math.Floor((utc - DateTime.UnixEpoch).TotalSeconds / periodSeconds);
            return ComputeHotp(key, counter, digits);
        }

        /// <summary>Verifies a TOTP code with allowed time-step drift.</summary>
        public static bool VerifyTotpCode(string secretBase32, DateTime utc, int code, int allowedDriftSteps = 1, int digits = 6, int periodSeconds = 30)
        {
            for (var delta = -allowedDriftSteps; delta <= allowedDriftSteps; delta++)
            {
                var key = Base32Decode(secretBase32);
                var counter = (long)Math.Floor((utc - DateTime.UnixEpoch).TotalSeconds / periodSeconds) + delta;
                var candidate = ComputeHotp(key, counter, digits);
                if (candidate == code) return true;
            }
            return false;
        }

        private static int ComputeHotp(byte[] key, long counter, int digits)
        {
            var cntBytes = BitConverter.GetBytes(counter);
            if (BitConverter.IsLittleEndian) Array.Reverse(cntBytes);

            using var hmac = new HMACSHA1(key);
            var hash = hmac.ComputeHash(cntBytes);
            var offset = hash[^1] & 0x0F;
            var binary = ((hash[offset] & 0x7f) << 24)
                         | ((hash[offset + 1] & 0xff) << 16)
                         | ((hash[offset + 2] & 0xff) << 8)
                         | (hash[offset + 3] & 0xff);
            var code = binary % (int)Math.Pow(10, digits);
            return code;
        }

        // ---- Base32 (RFC 4648) without padding ----
        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        private static string Base32Encode(byte[] data)
        {
            var output = new StringBuilder((data.Length + 7) * 8 / 5);
            int bits = 0, value = 0;
            foreach (var b in data)
            {
                value = (value << 8) | b;
                bits += 8;
                while (bits >= 5)
                {
                    output.Append(Alphabet[(value >> (bits - 5)) & 31]);
                    bits -= 5;
                }
            }
            if (bits > 0) output.Append(Alphabet[(value << (5 - bits)) & 31]);
            return output.ToString();
        }

        private static byte[] Base32Decode(string base32)
        {
            int bits = 0, value = 0;
            var outBytes = new System.Collections.Generic.List<byte>(base32.Length * 5 / 8);
            foreach (var c in base32.ToUpperInvariant())
            {
                var idx = Alphabet.IndexOf(c);
                if (idx < 0) throw new FormatException("Invalid Base32 character.");
                value = (value << 5) | idx;
                bits += 5;
                if (bits >= 8)
                {
                    outBytes.Add((byte)((value >> (bits - 8)) & 0xFF));
                    bits -= 8;
                }
            }
            return outBytes.ToArray();
        }
    }
}
