using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.Infrastructure.Extensions
{
    /// <summary>
    /// Registers ASP.NET Core Data Protection with a persisted key ring suitable for shared hosting.
    /// Keys are stored on disk at a configurable path to ensure Unprotect() works across app restarts
    /// and multiple processes on the same deployment.
    /// </summary>
    public static class ServiceCollectionExtensionsSecurity
    {
        /// <summary>
        /// Adds a persisted Data Protection key ring. Reads the path from configuration:
        /// <c>DataProtection:KeysPath</c>. If the path does not exist, it will be created.
        /// </summary>
        /// <param name="services">The DI container.</param>
        /// <param name="configuration">Application configuration (appsettings).</param>
        /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
        public static IServiceCollection AddSharedHostingDataProtection(this IServiceCollection services, IConfiguration configuration)
        {
            var keysPath = configuration["DataProtection:KeysPath"]?.Trim();
            if (string.IsNullOrWhiteSpace(keysPath))
            {
                keysPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Darwin",
                    "DataProtectionKeys");
            }

            Directory.CreateDirectory(keysPath);

            var builder = services
                .AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
                .SetApplicationName(ResolveApplicationName(configuration));

            var certificate = FindProtectionCertificate(configuration);
            if (certificate is not null)
            {
                builder.ProtectKeysWithCertificate(certificate);
            }
            else if (configuration.GetValue<bool>("DataProtection:RequireKeyEncryption"))
            {
                throw new InvalidOperationException(
                    "DataProtection:RequireKeyEncryption is true, but no valid DataProtection certificate was found. " +
                    "Configure DataProtection:CertificateThumbprint and optional StoreName/StoreLocation.");
            }

            return services;
        }

        private static string ResolveApplicationName(IConfiguration configuration)
        {
            var applicationName = configuration["DataProtection:ApplicationName"]?.Trim();
            return string.IsNullOrWhiteSpace(applicationName) ? "Darwin" : applicationName;
        }

        private static X509Certificate2? FindProtectionCertificate(IConfiguration configuration)
        {
            var thumbprint = NormalizeThumbprint(configuration["DataProtection:CertificateThumbprint"]);
            if (string.IsNullOrWhiteSpace(thumbprint))
            {
                return null;
            }

            var storeName = Enum.TryParse<StoreName>(
                configuration["DataProtection:CertificateStoreName"],
                ignoreCase: true,
                out var configuredStoreName)
                ? configuredStoreName
                : StoreName.My;

            var storeLocation = Enum.TryParse<StoreLocation>(
                configuration["DataProtection:CertificateStoreLocation"],
                ignoreCase: true,
                out var configuredStoreLocation)
                ? configuredStoreLocation
                : StoreLocation.CurrentUser;

            using var store = new X509Store(storeName, storeLocation);
            store.Open(OpenFlags.ReadOnly);

            var now = DateTimeOffset.UtcNow;
            var certificate = store.Certificates
                .Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false)
                .OfType<X509Certificate2>()
                .FirstOrDefault(candidate =>
                    candidate.HasPrivateKey &&
                    candidate.NotBefore.ToUniversalTime() <= now.UtcDateTime &&
                    candidate.NotAfter.ToUniversalTime() > now.UtcDateTime);

            if (certificate is null)
            {
                throw new InvalidOperationException(
                    "DataProtection:CertificateThumbprint is configured, but no matching valid certificate with a private key was found. " +
                    "Verify the thumbprint, store name, store location, private-key permissions, and certificate validity window.");
            }

            return certificate;
        }

        private static string? NormalizeThumbprint(string? thumbprint)
        {
            if (string.IsNullOrWhiteSpace(thumbprint))
            {
                return null;
            }

            return new string(thumbprint
                .Where(c => !char.IsWhiteSpace(c) && c != '\u200e' && c != '\u200f')
                .Select(char.ToUpperInvariant)
                .ToArray());
        }
    }
}
