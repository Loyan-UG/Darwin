using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.Infrastructure.Extensions;

public static class ServiceCollectionExtensionsConfiguredPersistence
{
    public const string PostgreSqlProviderName = "PostgreSql";
    public const string SqlServerProviderName = "SqlServer";

    public static IServiceCollection AddConfiguredPersistence(this IServiceCollection services, IConfiguration config)
    {
        var provider = config["Persistence:Provider"];
        if (string.IsNullOrWhiteSpace(provider))
        {
            provider = PostgreSqlProviderName;
        }

        if (string.Equals(provider, PostgreSqlProviderName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(provider, "Postgres", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(provider, "Npgsql", StringComparison.OrdinalIgnoreCase))
        {
            return services.AddPostgreSqlPersistence(config);
        }

        if (string.Equals(provider, SqlServerProviderName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(provider, "MSSQL", StringComparison.OrdinalIgnoreCase))
        {
            return services.AddSqlServerPersistence(config);
        }

        throw new InvalidOperationException(
            $"Unsupported persistence provider '{provider}'. Supported values are '{PostgreSqlProviderName}' and '{SqlServerProviderName}'.");
    }
}
