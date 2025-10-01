using System;
using Darwin.Infrastructure.Security.Secrets;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Darwin.Infrastructure.Persistence.Converters
{
    /// <summary>
    /// Provides a ValueConverter for encrypting/decrypting secrets at-rest.
    /// The converter is initialized once with an <see cref="ISecretProtector"/>.
    /// </summary>
    public static class SecretProtectionConverterFactory
    {
        private static volatile ISecretProtector? _protector;

        /// <summary>
        /// Initializes the factory with a singleton protector. Call this once during composition.
        /// </summary>
        public static void Initialize(ISecretProtector protector)
        {
            _protector = protector ?? throw new ArgumentNullException(nameof(protector));
        }

        /// <summary>
        /// Returns a <see cref="ValueConverter{String,String}"/> that encrypts on save
        /// and decrypts on read. If not initialized, the converter acts as pass-through.
        /// </summary>
        public static ValueConverter<string, string> CreateStringProtector()
        {
            return new ValueConverter<string, string>(
                convertToProviderExpression: plain =>
                    _protector is null || plain == null ? plain : _protector.Protect(plain),
                convertFromProviderExpression: protectedData =>
                    _protector is null || protectedData == null ? protectedData : _protector.Unprotect(protectedData));
        }
    }
}
