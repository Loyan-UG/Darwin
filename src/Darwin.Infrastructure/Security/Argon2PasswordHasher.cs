using System;
using System.Security.Cryptography;
using Darwin.Application.Abstractions.Auth;
using Isopoh.Cryptography.Argon2;

namespace Darwin.Infrastructure.Security
{
    /// <summary>
    /// Password hasher based on Argon2id using PHC-encoded strings.
    /// Stores salt+params inside the hash; verification is parameter-agnostic.
    /// </summary>
    public sealed class Argon2PasswordHasher : IUserPasswordHasher
    {
        // Chosen defaults for server-side hashing (tune per hardware/load tests)
        private const int Iterations = 3;     // t
        private const int MemoryKiB = 64 * 1024; // 64 MB
        private const int DegreeOfParallelism = 2; // p
        private const int SaltBytes = 16;
        private const int HashBytes = 32;

        public string Hash(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentNullException(nameof(password));

            var salt = RandomNumberGenerator.GetBytes(SaltBytes);
            var cfg = new Argon2Config
            {
                Type = Argon2Type.Id,
                TimeCost = Iterations,
                MemoryCost = MemoryKiB,
                Lanes = DegreeOfParallelism,
                Threads = DegreeOfParallelism,
                Salt = salt,
                HashLength = HashBytes
            };

            var hasher = new Argon2(cfg);
            return hasher.Hash(password); // PHC string: $argon2id$...
        }

        public bool Verify(string password, string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash)) return false;
            return Argon2.Verify(passwordHash, password);
        }

        public bool NeedsRehash(string passwordHash)
        {
            // If policy changes later (iterations/memory/parallelism), check and rehash here.
            // For now, return false. 
            // TODO: Inspect PHC parameters and compare with current policy.
            return false;
        }
    }
}
