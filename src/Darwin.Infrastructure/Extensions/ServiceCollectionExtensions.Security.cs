using System;
using System.IO;
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
                // Fallback: use a local subfolder under the current content root.
                keysPath = Path.Combine(AppContext.BaseDirectory, "dpkeys");
            }

            Directory.CreateDirectory(keysPath);

            services.AddDataProtection()
                    .PersistKeysToFileSystem(new DirectoryInfo(keysPath));
            // .SetApplicationName("Darwin") // Optional: set if multiple apps share the same keys.

            return services;
        }


        // TODO(Backlog): AddAzureBlobDataProtection(...) that reads: /or Redis
        // DataProtection:Azure:ConnectionString
        // DataProtection:Azure:ContainerName
        // DataProtection:Azure:BlobName
        // and calls .PersistKeysToAzureBlobStorage(...)

    }
}
