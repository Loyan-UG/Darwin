using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.Infrastructure.Extensions;

public static class ServiceCollectionExtensionsConfiguredPersistence
{
    public const string PostgreSqlProviderName = "PostgreSql";
    public const string SqlServerProviderName = "SqlServer";

    public static IServiceCollection AddConfiguredPersistence(this IServiceCollection services, IConfiguration config)
    {
        var provider = NormalizeProviderName(config["Persistence:Provider"]);

        return provider switch
        {
            PostgreSqlProviderName => services.AddPostgreSqlPersistence(config),
            SqlServerProviderName => services.AddSqlServerPersistence(config),
            _ => throw new InvalidOperationException(
                $"Unsupported persistence provider '{provider}'. Supported values are '{PostgreSqlProviderName}' and '{SqlServerProviderName}'.")
        };
    }

    private static string NormalizeProviderName(string? configuredProvider)
    {
        if (string.IsNullOrWhiteSpace(configuredProvider))
        {
            return PostgreSqlProviderName;
        }

        var provider = configuredProvider.Trim();
        if (string.Equals(provider, PostgreSqlProviderName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(provider, "Postgres", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(provider, "Npgsql", StringComparison.OrdinalIgnoreCase))
        {
            return PostgreSqlProviderName;
        }

        if (string.Equals(provider, SqlServerProviderName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(provider, "MSSQL", StringComparison.OrdinalIgnoreCase))
        {
            return SqlServerProviderName;
        }

        return provider;
    }
}
