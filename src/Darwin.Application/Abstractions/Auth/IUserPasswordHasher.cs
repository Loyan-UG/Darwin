using System;

namespace Darwin.Application.Abstractions.Auth
{
    /// <summary>
    /// Abstraction for hashing and verifying user passwords.
    /// Keeps Application layer free from any specific hashing library.
    /// Infrastructure provides a concrete implementation (e.g., PBKDF2/ASP.NET Identity hasher).
    /// </summary>
    public interface IUserPasswordHasher
    {
        // Returns a salted hash string for the given plaintext password.
        string Hash(string password);

        // Returns true if plaintext password matches the hashed password.
        bool Verify(string hashedPassword, string password);
    }
}
