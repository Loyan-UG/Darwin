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
    /// and multiple processes on the same machine. In production, consider a network share or
    /// cloud-backed key ring (Azure Blob/Redis/etc.)—see TODOs at the end of this file.
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
            var keysPath = configuration["DataProtection:KeysPath"];
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
                .SetApplicationName(configuration["DataProtection:ApplicationName"] ?? "Darwin");

            var certificate = TryFindProtectionCertificate(configuration);
            if (certificate is not null)
            {
                builder.ProtectKeysWithCertificate(certificate);
            }
            else if (configuration.GetValue<bool>("DataProtection:RequireKeyEncryption"))
            {
                throw new InvalidOperationException(
                    "DataProtection:RequireKeyEncryption is true, but no usable DataProtection certificate was found. " +
                    "Configure DataProtection:CertificateThumbprint and optional StoreName/StoreLocation.");
            }

            return services;
        }

        private static X509Certificate2? TryFindProtectionCertificate(IConfiguration configuration)
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

            return store.Certificates
                .Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false)
                .OfType<X509Certificate2>()
                .FirstOrDefault(certificate => certificate.HasPrivateKey);
        }

        private static string? NormalizeThumbprint(string? thumbprint)
        {
            if (string.IsNullOrWhiteSpace(thumbprint))
            {
                return null;
            }

            return thumbprint.Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
        }


        // TODO(Backlog): AddAzureBlobDataProtection(...) that reads: /or Redis
        // DataProtection:Azure:ConnectionString
        // DataProtection:Azure:ContainerName
        // DataProtection:Azure:BlobName
        // and calls .PersistKeysToAzureBlobStorage(...)

    }
}
