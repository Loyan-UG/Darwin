using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Infrastructure.Persistence.Db;
using Darwin.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Darwin.Infrastructure.Extensions
{
    /// <summary>
    ///     DI extension methods for registering persistence services:
    ///     EF Core <see cref="DbContext"/>, the application-facing <c>IAppDbContext</c> abstraction,
    ///     and helpers for applying migrations and seeding at application startup.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Responsibilities:
    ///         <list type="bullet">
    ///             <item>Bind <c>DarwinDbContext</c> to SQL Server using the configured connection string.</item>
    ///             <item>Expose <c>IAppDbContext</c> as a scoped mapping to the same DbContext for the Application layer.</item>
    ///             <item>Provide <c>MigrateAndSeedAsync</c> to apply pending migrations and run idempotent data seeding.</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Notes:
    ///         <list type="bullet">
    ///             <item>Enable retry-on-failure for transient SQL issues during development.</item>
    ///             <item>Keep this extension free of web/host-specific concerns; it is reusable across entry points.</item>
    ///         </list>
    ///     </para>
    /// </remarks>
    public static class ServiceCollectionExtensionsPersistence
    {
        public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration config)
        {
            var conn = config.GetConnectionString("DefaultConnection")
                      ?? throw new InvalidOperationException("ConnectionString 'DefaultConnection' is missing.");

            services.AddDbContext<DarwinDbContext>(opt =>
            {
                opt.UseSqlServer(conn, sql =>
                {
                    sql.EnableRetryOnFailure();
                    sql.MigrationsAssembly(typeof(DarwinDbContext).Assembly.FullName);
                });
            });

            // Expose DbContext via IAppDbContext for Application layer
            services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<DarwinDbContext>());

            // Seeder
            services.AddScoped<DataSeeder>();

            // after AddDbContext & IAppDbContext mapping:
            services.AddScoped<IdentitySeed>();


            return services;
        }

        /// <summary>
        /// Applies pending migrations and runs idempotent seeding. Call once on application startup.
        /// </summary>
        public static async Task MigrateAndSeedAsync(this IServiceProvider sp, CancellationToken ct = default)
        {
            using var scope = sp.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbStartup");
            var db = scope.ServiceProvider.GetRequiredService<DarwinDbContext>();

            logger.LogInformation("Applying database migrations…");
            await db.Database.MigrateAsync(ct);

            logger.LogInformation("Seeding baseline data…");
            var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
            await seeder.SeedAsync(ct);

            // Identity seed
            var identitySeed = scope.ServiceProvider.GetRequiredService<Darwin.Infrastructure.Persistence.Seed.IdentitySeed>();
            await identitySeed.SeedAsync(ct);
        }
    }
}
