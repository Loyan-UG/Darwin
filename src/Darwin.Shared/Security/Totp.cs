using System;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Darwin.Shared.Security
{
    /// <summary>
    /// RFC 6238 TOTP (time-based OTP) helper. Default: 30s step, 6 digits, SHA1.
    /// </summary>
    public static class Totp
    {
        public static string Generate(ReadOnlySpan<byte> secret, DateTime utcNow, int stepSeconds = 30, int digits = 6)
        {
            long timestep = (long)(utcNow - DateTime.UnixEpoch).TotalSeconds / stepSeconds;
            Span<byte> counter = stackalloc byte[8];
            BinaryPrimitives.WriteInt64BigEndian(counter, timestep);

            var hmac = new HMACSHA1(secret.ToArray());
            var hash = hmac.ComputeHash(counter.ToArray());

            int offset = hash[^1] & 0x0F;
            int binary =
                ((hash[offset] & 0x7F) << 24) |
                ((hash[offset + 1] & 0xFF) << 16) |
                ((hash[offset + 2] & 0xFF) << 8) |
                (hash[offset + 3] & 0xFF);

            int hotp = binary % (int)Math.Pow(10, digits);
            return hotp.ToString(new string('0', digits));
        }

        public static bool Verify(ReadOnlySpan<byte> secret, string code, DateTime utcNow,
                                  int stepSeconds = 30, int digits = 6, int window = 1)
        {
            // Accept codes within [-window, +window] steps for clock skew.
            for (int i = -window; i <= window; i++)
            {
                var ts = utcNow.AddSeconds(i * stepSeconds);
                if (Generate(secret, ts, stepSeconds, digits).Equals(code, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        /// <summary>Builds otpauth:// URI for QR provisioning (Google Authenticator et al.).</summary>
        public static string BuildOtpAuthUri(string issuer, string accountLabel, string secretBase32, int digits = 6, int period = 30)
        {
            // otpauth://totp/Issuer:Account?secret=BASE32&issuer=Issuer&digits=6&period=30
            var encIssuer = Uri.EscapeDataString(issuer);
            var encLabel = Uri.EscapeDataString(accountLabel);
            return $"otpauth://totp/{encIssuer}:{encLabel}?secret={secretBase32}&issuer={encIssuer}&digits={digits}&period={period}";
        }
    }
}
