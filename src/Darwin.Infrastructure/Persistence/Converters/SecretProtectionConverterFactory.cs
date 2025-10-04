using Darwin.Infrastructure.Security.Secrets;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;

namespace Darwin.Infrastructure.Persistence.Converters
{
    /// <summary>
    /// Produces ValueConverters that protect/unprotect sensitive strings at-rest using ASP.NET Core Data Protection.
    /// Converters are expression-tree friendly so EF Core can translate them.
    /// </summary>
    public static class SecretProtectionConverterFactory
    {
        // Static default protector configured via Initialize(). Volatile for safe reads across threads.
        private static volatile IDataProtector? _defaultProtector;

        /// <summary>
        /// Sets a default <see cref="IDataProtector"/> to be used by converters when no explicit protector is provided.
        /// Call this once during application startup (DI composition).
        /// </summary>
        public static void Initialize(IDataProtector protector)
        {
            _defaultProtector = protector ?? throw new ArgumentNullException(nameof(protector));
        }

        /// <summary>
        /// Creates a converter for non-nullable strings where empty/null are stored as null and other values are protected.
        /// </summary>
        public static ValueConverter<string, string?> CreateStringProtector(IDataProtector? protector = null)
        {
            var local = protector; // avoid capturing outer variable inside expression trees

            return new ValueConverter<string, string?>(
                // To provider: plaintext -> protected (or null)
                v => string.IsNullOrEmpty(v)
                        ? null
                        : GetProtector(ref local).Protect(v),
                // From provider: protected -> plaintext (or empty)
                v => string.IsNullOrEmpty(v)
                        ? string.Empty
                        : GetProtector(ref local).Unprotect(v)
            );
        }

        /// <summary>
        /// Creates a converter for nullable strings (keeps null as null), protecting non-empty values.
        /// </summary>
        public static ValueConverter<string?, string?> CreateNullableStringProtector(IDataProtector? protector = null)
        {
            var local = protector;

            return new ValueConverter<string?, string?>(
                v => string.IsNullOrEmpty(v)
                        ? v
                        : GetProtector(ref local).Protect(v!),
                v => string.IsNullOrEmpty(v)
                        ? v
                        : GetProtector(ref local).Unprotect(v!)
            );
        }

        /// <summary>
        /// Resolves a protector without capturing non-translatable state in expression trees.
        /// Priority: explicit parameter → globally-initialized → fallback ad-hoc provider.
        /// </summary>
        private static IDataProtector GetProtector(ref IDataProtector? protector)
        {
            if (protector != null) return protector;
            if (_defaultProtector != null) return _defaultProtector;

            // Fallback (development-only safety): creates a local provider if Initialize was not called.
            // In production, always call Initialize(...) to bind to your central key ring.
            var provider = DataProtectionProvider.Create("Darwin.Persistence.Secrets");
            protector = provider.CreateProtector("EFCore.ValueConverter.Secret");
            return protector;
        }
    }
}
