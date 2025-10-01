using System;
using Microsoft.AspNetCore.DataProtection;

namespace Darwin.Infrastructure.Security.Secrets
{
    /// <summary>
    /// <see cref="ISecretProtector"/> based on ASP.NET Core Data Protection.
    /// Uses a stable purpose string to isolate keys and enable key rotation.
    /// </summary>
    public sealed class DataProtectionSecretProtector : ISecretProtector
    {
        private static readonly string Purpose = "Darwin/TOTP/SecretBase32/v1";
        private readonly IDataProtector _protector;

        /// <summary>
        /// Creates a protector using the shared <see cref="IDataProtectionProvider"/>.
        /// </summary>
        public DataProtectionSecretProtector(IDataProtectionProvider provider)
        {
            if (provider is null) throw new ArgumentNullException(nameof(provider));
            _protector = provider.CreateProtector(Purpose);
        }

        /// <inheritdoc />
        public string Protect(string plain)
        {
            if (plain is null) throw new ArgumentNullException(nameof(plain));
            return _protector.Protect(plain);
        }

        /// <inheritdoc />
        public string Unprotect(string protectedData)
        {
            if (protectedData is null) throw new ArgumentNullException(nameof(protectedData));
            return _protector.Unprotect(protectedData);
        }
    }
}
