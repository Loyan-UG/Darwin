using System;

namespace Darwin.Infrastructure.Security.Secrets
{
    /// <summary>
    /// Minimal abstraction over a string protector for at-rest encryption.
    /// Implementations must be deterministic for the same input and reversible
    /// within the running environment (e.g., ASP.NET Core Data Protection).
    /// </summary>
    public interface ISecretProtector
    {
        /// <summary>
        /// Protects (encrypts) a plaintext string for at-rest storage.
        /// </summary>
        /// <param name="plain">Plaintext content to protect. Must not be null.</param>
        /// <returns>Protected (encrypted) string suitable for persistence.</returns>
        string Protect(string plain);

        /// <summary>
        /// Unprotects (decrypts) a previously protected string.
        /// </summary>
        /// <param name="protectedData">Protected string from storage. Must not be null.</param>
        /// <returns>Decrypted plaintext string.</returns>
        string Unprotect(string protectedData);
    }
}
