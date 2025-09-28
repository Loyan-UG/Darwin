using System;
using System.Security.Cryptography;
using System.Text;
using Darwin.Application.Abstractions.Auth;
using Isopoh.Cryptography.Argon2;

namespace Darwin.Infrastructure.Security
{
    /// <summary>
    /// Argon2id password hasher (Isopoh v2.0.0) that returns/accepts PHC-encoded strings.
    /// - Uses Argon2Type.HybridAddressing (Argon2id) with version 0x13 (19).
    /// - Encodes salt/params in the returned hash string for self-contained verification.
    /// - All parameters are tuned for server-side hashing; adjust MemoryCostKiB/TimeCost for your hardware.
    /// </summary>
    public sealed class Argon2PasswordHasher : IUserPasswordHasher
    {
        // Sensible defaults for server-side hashing (tune for your hardware).
        private const int DefaultTimeCost = 3;         // iterations
        private const int DefaultMemoryCostKiB = 65536; // 64 MiB
        private const int DefaultParallelism = 4;      // lanes/threads
        private const int DefaultHashLength = 32;      // 256-bit
        private const int DefaultSaltLength = 16;      // 128-bit

        /// <summary>
        /// Hashes a clear-text password using Argon2id and returns a PHC-encoded string.
        /// </summary>
        /// <param name="password">Clear-text password.</param>
        /// <returns>PHC string ($argon2id$v=19$m=...,t=...,p=...$salt$hash)</returns>
        public string Hash(string password)
        {
            if (password is null) throw new ArgumentNullException(nameof(password));

            var config = new Argon2Config
            {
                Type = Argon2Type.HybridAddressing,         // Argon2id
                Version = Argon2Version.Nineteen,           // v=19
                TimeCost = DefaultTimeCost,
                MemoryCost = DefaultMemoryCostKiB,          // KiB
                Lanes = DefaultParallelism,
                Threads = DefaultParallelism,
                HashLength = DefaultHashLength,
                Salt = RandomNumberGenerator.GetBytes(DefaultSaltLength),
                Password = Encoding.UTF8.GetBytes(password)
            };

            using var argon2 = new Argon2(config);
            var hash = argon2.Hash();
            // EncodeString() returns a PHC string that embeds all parameters and the salt.
            var phc = config.EncodeString(hash.Buffer);

            // Wipe password bytes from managed memory asap
            Array.Clear(config.Password, 0, config.Password.Length);
            return phc;
        }

        /// <summary>
        /// Verifies a clear-text password against a PHC-encoded Argon2 hash string.
        /// </summary>
        /// <param name="hashedPassword">PHC string as stored in DB.</param>
        /// <param name="providedPassword">Clear-text password to verify.</param>
        /// <returns>True if the password matches; otherwise false.</returns>
        public bool Verify(string hashedPassword, string providedPassword)
        {
            if (hashedPassword is null) throw new ArgumentNullException(nameof(hashedPassword));
            if (providedPassword is null) throw new ArgumentNullException(nameof(providedPassword));
            return Argon2.Verify(hashedPassword, providedPassword);
        }
    }
}
