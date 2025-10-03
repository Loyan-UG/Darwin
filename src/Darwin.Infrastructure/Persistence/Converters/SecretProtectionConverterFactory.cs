using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.AspNetCore.DataProtection;

namespace Darwin.Infrastructure.Persistence.Converters
{
    /// <summary>
    /// Produces ValueConverters that protect/unprotect sensitive strings at-rest using ASP.NET Core Data Protection.
    /// Converters are expression-tree friendly (no C# pattern matching), so EF Core can translate them.
    /// </summary>
    public static class SecretProtectionConverterFactory
    {
        /// <summary>
        /// Creates a converter for non-nullable strings where empty/null are stored as null and other values are protected.
        /// </summary>
        public static ValueConverter<string, string?> CreateStringProtector(IDataProtector protector = null)
        {
            // Lazy resolve protector via factory to keep method testable and DI-friendly
            var _protector = protector;

            return new ValueConverter<string, string?>(
                // To provider: plaintext -> protected (or null)
                v => string.IsNullOrEmpty(v)
                        ? null
                        : GetProtector(ref _protector).Protect(v),
                // From provider: protected -> plaintext (or empty)
                v => string.IsNullOrEmpty(v)
                        ? string.Empty
                        : GetProtector(ref _protector).Unprotect(v)
            );
        }

        /// <summary>
        /// Creates a converter for nullable strings (keeps nulls as null), protecting non-empty values.
        /// </summary>
        public static ValueConverter<string?, string?> CreateNullableStringProtector(IDataProtector protector = null)
        {
            var _protector = protector;

            return new ValueConverter<string?, string?>(
                v => string.IsNullOrEmpty(v)
                        ? v
                        : GetProtector(ref _protector).Protect(v),
                v => string.IsNullOrEmpty(v)
                        ? v
                        : GetProtector(ref _protector).Unprotect(v)
            );
        }

        // Helper to create/get a protector without capturing non-translatable state inside expression trees.
        private static IDataProtector GetProtector(ref IDataProtector protector)
        {
            if (protector != null) return protector;

            // Fallback: create a default protector from the ambient DataProtection system.
            // In practice, pass a scoped protector via DI when registering the converter in DbContext.
            var provider = DataProtectionProvider.Create("Darwin.Persistence.Secrets");
            protector = provider.CreateProtector("EFCore.ValueConverter.Secret");
            return protector;
        }
    }
}
