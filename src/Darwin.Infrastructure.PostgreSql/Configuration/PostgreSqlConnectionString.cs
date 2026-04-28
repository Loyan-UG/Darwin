using Npgsql;

namespace Darwin.Infrastructure.PostgreSql.Configuration;

internal static class PostgreSqlConnectionString
{
    private const string DefaultApplicationName = "Darwin";
    private const int DefaultAutoPrepareMinUsages = 2;
    private const int DefaultCommandTimeoutSeconds = 60;
    private const int DefaultKeepAliveSeconds = 30;
    private const int DefaultMaxAutoPrepare = 100;
    private const int DefaultTimeoutSeconds = 15;

    public static string Normalize(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        if (string.IsNullOrWhiteSpace(builder.ApplicationName))
        {
            builder.ApplicationName = DefaultApplicationName;
        }

        if (builder.MaxAutoPrepare <= 0)
        {
            builder.MaxAutoPrepare = DefaultMaxAutoPrepare;
        }

        if (builder.AutoPrepareMinUsages <= 0)
        {
            builder.AutoPrepareMinUsages = DefaultAutoPrepareMinUsages;
        }

        if (builder.KeepAlive <= 0)
        {
            builder.KeepAlive = DefaultKeepAliveSeconds;
        }

        if (builder.Timeout <= 0)
        {
            builder.Timeout = DefaultTimeoutSeconds;
        }

        if (builder.CommandTimeout <= 0)
        {
            builder.CommandTimeout = DefaultCommandTimeoutSeconds;
        }

        return builder.ConnectionString;
    }
}
