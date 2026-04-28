using Darwin.Infrastructure.Persistence.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.Infrastructure.Extensions;

public static class ServiceCollectionExtensionsSqlServerPersistence
{
    public const string ProviderName = "SqlServer";

    public static IServiceCollection AddSqlServerPersistence(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("SqlServer")
                               ?? config.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException("ConnectionString 'SqlServer' or 'DefaultConnection' is missing.");

        services.AddDbContext<DarwinDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.EnableRetryOnFailure();
                sql.MigrationsAssembly(typeof(ServiceCollectionExtensionsSqlServerPersistence).Assembly.FullName);
            });
        });

        services.AddDarwinPersistenceCoreServices();

        return services;
    }
}
