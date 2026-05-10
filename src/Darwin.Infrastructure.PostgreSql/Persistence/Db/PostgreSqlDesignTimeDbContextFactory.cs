using Darwin.Infrastructure.PostgreSql.Configuration;
using Darwin.Infrastructure.Persistence.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;

namespace Darwin.Infrastructure.PostgreSql.Persistence.Db;

public sealed class PostgreSqlDesignTimeDbContextFactory : IDesignTimeDbContextFactory<DarwinDbContext>
{
    public DarwinDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();
        var connectionString =
            FirstNonWhiteSpace(
                Environment.GetEnvironmentVariable("ConnectionStrings__PostgreSql"),
                configuration.GetConnectionString("PostgreSql"),
                configuration.GetConnectionString("DefaultConnection")) ??
            "Host=localhost;Port=5432;Database=darwin_dev;Username=darwin;Include Error Detail=true";

        var normalizedConnectionString = PostgreSqlConnectionString.Normalize(connectionString);

        var options = new DbContextOptionsBuilder<DarwinDbContext>()
            .UseNpgsql(normalizedConnectionString, npgsql =>
            {
                npgsql.CommandTimeout(60);
                npgsql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null);
                npgsql.MigrationsAssembly(typeof(PostgreSqlDesignTimeDbContextFactory).Assembly.FullName);
            })
            .Options;

        return new DarwinDbContext(options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var currentDirectory = Directory.GetCurrentDirectory();

        var probeRoots = new[]
        {
            currentDirectory,
            Path.Combine(currentDirectory, ".."),
            Path.Combine(currentDirectory, "../Darwin.WebAdmin"),
            Path.Combine(currentDirectory, "../../Darwin.WebAdmin"),
            Path.Combine(currentDirectory, "../Darwin.WebApi"),
            Path.Combine(currentDirectory, "../../Darwin.WebApi")
        }
        .Select(Path.GetFullPath)
        .Distinct()
        .ToArray();

        var builder = new ConfigurationBuilder()
            .AddEnvironmentVariables();

        foreach (var root in probeRoots)
        {
            var appsettings = Path.Combine(root, "appsettings.json");
            var appsettingsEnv = Path.Combine(root, $"appsettings.{env}.json");
            if (!File.Exists(appsettings) && !File.Exists(appsettingsEnv))
            {
                continue;
            }

            builder.SetBasePath(root);
            builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
            builder.AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: false);
            break;
        }

        return builder.Build();
    }

    private static string? FirstNonWhiteSpace(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
