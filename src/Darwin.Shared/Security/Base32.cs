using System;
using System.Text;

namespace Darwin.Shared.Security
{
    /// <summary>
    /// Minimal Base32 (RFC4648) encoder used for TOTP secrets. Encoding only (decoding done in Totp via own routine).
    /// </summary>
    public static class Base32
    {
        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        public static string Encode(ReadOnlySpan<byte> data)
        {
            if (data.Length == 0) return string.Empty;

            StringBuilder sb = new StringBuilder((data.Length + 4) / 5 * 8);
            int bitBuffer = 0;
            int bitCount = 0;

            foreach (var b in data)
            {
                bitBuffer = (bitBuffer << 8) | b;
                bitCount += 8;
                while (bitCount >= 5)
                {
                    int index = (bitBuffer >> (bitCount - 5)) & 0x1F;
                    bitCount -= 5;
                    sb.Append(Alphabet[index]);
                }
            }

            if (bitCount > 0)
            {
                int index = (bitBuffer << (5 - bitCount)) & 0x1F;
                sb.Append(Alphabet[index]);
            }

            // No padding for secrets; authenticator apps accept no-padding Base32.
            return sb.ToString();
        }
    }
}
