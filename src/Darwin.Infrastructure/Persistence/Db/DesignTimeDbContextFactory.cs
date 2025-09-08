using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Darwin.Infrastructure.Persistence.Db
{
    /// <summary>
    /// Design-time factory for creating <see cref="DarwinDbContext"/> instances used by EF Core CLI
    /// (e.g., "dotnet ef migrations add", "dotnet ef database update").
    ///
    /// Why this exists:
    /// - At design time, your application's DI container (and Program.cs) are not available.
    /// - EF tooling needs a deterministic way to construct the DbContext with proper options (provider, migrations assembly).
    ///
    /// Connection string resolution order (first match wins):
    ///  1) Environment variable "ConnectionStrings__DefaultConnection"
    ///  2) appsettings files discovered by probing typical locations:
    ///       - Current directory (where the command runs)
    ///       - Sibling/parent paths like ../Darwin.Web, ../../Darwin.Web (common when startup-project is Web)
    ///       - appsettings.{Environment}.json then appsettings.json
    ///  3) Fallback LocalDB for developer machines
    ///
    /// Notes:
    /// - We intentionally use the DbContext ctor that accepts only DbContextOptions so that we don't need to compose
    ///   runtime services (like ICurrentUserService) at design time. In runtime, DI will use the other partial ctor
    ///   that includes ICurrentUserService for auditing.
    /// - Ensure this class is in the SAME namespace as your runtime DbContext (Darwin.Infrastructure.Persistence.Db)
    ///   so EF sees exactly one DbContext type and you avoid "More than one DbContext was found" errors.
    /// </summary>
    public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DarwinDbContext>
    {
        public DarwinDbContext CreateDbContext(string[] args)
        {
            // 1) Try environment variable (works great for CI/Dev: ConnectionStrings__DefaultConnection)
            var connFromEnv = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

            // 2) If not supplied, try to load from appsettings in a few likely locations
            var configuration = BuildConfiguration();
            var connFromConfig = configuration.GetConnectionString("DefaultConnection");

            // 3) Decide final connection string
            var connectionString = FirstNonEmpty(connFromEnv, connFromConfig)
                                   ?? "Server=(localdb)\\MSSQLLocalDB;Database=Darwin;Trusted_Connection=True;MultipleActiveResultSets=true";

            // 4) Build options
            var optionsBuilder = new DbContextOptionsBuilder<DarwinDbContext>()
                .UseSqlServer(connectionString, sql =>
                {
                    // Make migrations assembly explicit to avoid EF guessing wrong assembly
                    sql.MigrationsAssembly(typeof(DarwinDbContext).Assembly.FullName);
                    // Helpful in dev if SQL is flaky
                    sql.EnableRetryOnFailure();
                });

            // Return DbContext using the options-only ctor (design-time safe)
            return new DarwinDbContext(optionsBuilder.Options);
        }

        /// <summary>
        /// Builds a configuration root by probing for appsettings in common locations.
        /// This allows "dotnet ef" to run from the Infrastructure project while the
        /// JSON config files (appsettings.json, appsettings.Development.json) may live in Web.
        /// </summary>
        private static IConfigurationRoot BuildConfiguration()
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            // Typical probe paths (ordered). Adjust if your solution layout differs.
            // - Current directory
            // - One level up (monorepo style)
            // - ../Darwin.Web and ../../Darwin.Web (common when startup project is Web)
            var probeRoots = new[]
            {
                Directory.GetCurrentDirectory(),
                Path.Combine(Directory.GetCurrentDirectory(), ".."),
                Path.Combine(Directory.GetCurrentDirectory(), "../Darwin.Web"),
                Path.Combine(Directory.GetCurrentDirectory(), "../../Darwin.Web"),
            }
            .Select(p => Path.GetFullPath(p))
            .Distinct()
            .ToArray();

            var builder = new ConfigurationBuilder();

            // Always allow environment variables to override
            builder.AddEnvironmentVariables();

            // Add the first path that contains any appsettings file
            foreach (var root in probeRoots)
            {
                var appsettings = Path.Combine(root, "appsettings.json");
                var appsettingsEnv = Path.Combine(root, $"appsettings.{env}.json");

                if (File.Exists(appsettings) || File.Exists(appsettingsEnv))
                {
                    builder.SetBasePath(root);
                    // Include environment-specific first to allow overriding
                    builder.AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: false);
                    builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
                    break;
                }
            }

            return builder.Build();
        }

        /// <summary>
        /// Returns the first non-null/non-whitespace string from the inputs, or null if all are empty.
        /// </summary>
        private static string? FirstNonEmpty(params string?[] values)
            => values.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
    }
}
