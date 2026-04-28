using Darwin.Infrastructure.PostgreSql.Configuration;
using Darwin.Infrastructure.Persistence.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Darwin.Infrastructure.Extensions;

public static class ServiceCollectionExtensionsPostgreSqlPersistence
{
    public const string PostgreSqlProviderName = "PostgreSql";

    public static IServiceCollection AddPostgreSqlPersistence(this IServiceCollection services, IConfiguration config)
    {
        var conn = PostgreSqlConnectionString.Normalize(
            config.GetConnectionString(PostgreSqlProviderName)
            ?? config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionString 'PostgreSql' or 'DefaultConnection' is missing."));

        services.AddDbContext<DarwinDbContext>(opt =>
        {
            opt.UseNpgsql(conn, npgsql =>
            {
                npgsql.CommandTimeout(60);
                npgsql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null);
                npgsql.MigrationsAssembly(typeof(ServiceCollectionExtensionsPostgreSqlPersistence).Assembly.FullName);
            });
        });

        services.AddDarwinPersistenceCoreServices();

        return services;
    }
}
