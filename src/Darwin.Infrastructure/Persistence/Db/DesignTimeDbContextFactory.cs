using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Darwin.Infrastructure.Persistence.Db
{
    /// <summary>
    /// Used by 'dotnet ef' tooling to instantiate DbContext at design-time for migrations.
    /// Priority: EnvVar (ConnectionStrings__DefaultConnection) -> appsettings.Development.json -> fallback.
    /// </summary>
    public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DarwinDbContext>
    {
        public DarwinDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<DarwinDbContext>();

            // 1) Environment variable (best for dev/CI)
            var conn = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

            // 2) appsettings.Development.json (if present)
            if (string.IsNullOrWhiteSpace(conn))
            {
                var basePath = Directory.GetCurrentDirectory();
                var config = new ConfigurationBuilder()
                    .SetBasePath(basePath)
                    .AddJsonFile("appsettings.Development.json", optional: true)
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddEnvironmentVariables()
                    .Build();

                conn = config.GetConnectionString("DefaultConnection");
            }

            // 3) Fallback (LocalDB)
            if (string.IsNullOrWhiteSpace(conn))
            {
                conn = "Server=(localdb)\\MSSQLLocalDB;Database=Darwin;Trusted_Connection=True;MultipleActiveResultSets=true";
            }

            builder.UseSqlServer(conn, sql => sql.MigrationsAssembly(typeof(DarwinDbContext).Assembly.FullName));
            return new DarwinDbContext(builder.Options);
        }
    }
}
