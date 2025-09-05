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
    /// Registers DbContext and exposes helper for migration & seeding during startup.
    /// </summary>
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
        }
    }
}
