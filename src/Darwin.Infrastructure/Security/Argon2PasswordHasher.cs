using System;
using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Darwin.Application.Abstractions.Auth;
using Konscious.Security.Cryptography;

namespace Darwin.Infrastructure.Security
{
    /// <summary>
    /// Argon2id-based password hasher.
    /// - Uses Argon2id (memory-hard, GPU-resistant) with project-wide parameters.
    /// - Stores a self-contained encoded string including: version, variant, parameters, salt, and hash.
    /// - Verify() re-computes the hash with stored parameters and constant-time compares the derived key.
    ///
    /// Format (custom, URL-safe Base64 without padding):
    ///   $argon2id$v=19$m=65536,t=3,p=2$<salt_b64>$<hash_b64>
    ///
    /// NOTE:
    /// - Keep parameters tuned for *server capacity*. Typical secure baseline:
    ///   memorySizeKB = 64_000..262_144, iterations = 2..5, degreeOfParallelism = CPU cores (~2..8).
    /// - For long-run: consider configuration-driven values to upgrade over time and enable rehash.
    /// - All methods are pure CPU; no async I/O, so methods are synchronous by design.
    /// </summary>
    public sealed class Argon2PasswordHasher : IUserPasswordHasher
    {
        // Sensible defaults for a typical web server. Tune per environment.
        private const int DefaultMemorySizeKb = 64_000;   // 64 MB
        private const int DefaultIterations = 3;
        private const int DefaultParallelism = 2;
        private const int SaltSizeBytes = 16;
        private const int HashSizeBytes = 32;       // 256-bit key

        public string Hash(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password must not be empty.", nameof(password));

            var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
            var hash = DeriveKey(password, salt, DefaultMemorySizeKb, DefaultIterations, DefaultParallelism, HashSizeBytes);

            var saltB64 = ToBase64Url(salt);
            var hashB64 = ToBase64Url(hash);

            // Encoded PHC-style string
            return $"$argon2id$v=19$m={DefaultMemorySizeKb},t={DefaultIterations},p={DefaultParallelism}${saltB64}${hashB64}";
        }

        public bool Verify(string password, string encodedHash)
        {
            if (string.IsNullOrWhiteSpace(encodedHash) || string.IsNullOrWhiteSpace(password))
                return false;

            try
            {
                // Parse format
                // $argon2id$v=19$m=...,t=...,p=...$<salt>$<hash>
                var parts = encodedHash.Split('$', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 4 || !parts[0].Equals("argon2id", StringComparison.OrdinalIgnoreCase))
                    return false;

                // parts[1] = v=19 (ignored for now, assume 19)
                var paramPart = parts[2]; // m=..,t=..,p=..
                var paramKv = paramPart.Split(',', StringSplitOptions.RemoveEmptyEntries);
                int memory = 0, iterations = 0, parallelism = 0;
                foreach (var kv in paramKv)
                {
                    var pair = kv.Split('=', StringSplitOptions.RemoveEmptyEntries);
                    if (pair.Length != 2) return false;
                    switch (pair[0])
                    {
                        case "m": memory = int.Parse(pair[1]); break;
                        case "t": iterations = int.Parse(pair[1]); break;
                        case "p": parallelism = int.Parse(pair[1]); break;
                        default: return false;
                    }
                }

                var salt = FromBase64Url(parts[3]);
                var expected = FromBase64Url(parts[4 - 1]); // last

                var actual = DeriveKey(password, salt, memory, iterations, parallelism, expected.Length);
                var equal = ConstantTimeEquals(expected, actual);
                CryptographicOperations.ZeroMemory(actual);
                return equal;
            }
            catch
            {
                return false;
            }
        }

        private static byte[] DeriveKey(string password, byte[] salt, int memoryKb, int iterations, int parallelism, int outLen)
        {
            var argon = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = salt,
                MemorySize = memoryKb,
                Iterations = iterations,
                DegreeOfParallelism = parallelism
            };
            return argon.GetBytes(outLen);
        }

        private static string ToBase64Url(ReadOnlySpan<byte> data)
        {
            // URL-safe base64 without padding
            var b64 = Convert.ToBase64String(data);
            return b64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        private static byte[] FromBase64Url(string s)
        {
            var b64 = s.Replace('-', '+').Replace('_', '/');
            switch (b64.Length % 4)
            {
                case 2: b64 += "=="; break;
                case 3: b64 += "="; break;
            }
            return Convert.FromBase64String(b64);
        }

        private static bool ConstantTimeEquals(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
        {
            if (a.Length != b.Length) return false;
            return CryptographicOperations.FixedTimeEquals(a, b);
        }
    }
}
