using Darwin.Infrastructure.Extensions;
using Darwin.Infrastructure.Persistence.Db;
using FluentAssertions;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.Infrastructure.Tests.Persistence;

public sealed class ServiceCollectionExtensionsConfiguredPersistenceTests
{
    private const string PostgresConnectionString = "Host=127.0.0.1;Database=DarwinPersistenceConfigured;Username=postgres;Password=postgres";
    private const string SqlServerConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=DarwinPersistenceConfigured;Trusted_Connection=True;TrustServerCertificate=True;";

    [Fact]
    public void AddConfiguredPersistence_Should_ResolvePostgreSqlProvider_WhenConfiguredAsPostgreSql()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(
            ("Persistence:Provider", ServiceCollectionExtensionsConfiguredPersistence.PostgreSqlProviderName),
            ("ConnectionStrings:PostgreSql", PostgresConnectionString));

        services.AddConfiguredPersistence(configuration);
        using var provider = services.BuildServiceProvider();

        using var context = provider.GetRequiredService<DarwinDbContext>();
        context.Database.ProviderName.Should().NotBeNullOrWhiteSpace();
        context.Database.ProviderName.Should().Be("Npgsql.EntityFrameworkCore.PostgreSQL");
    }

    [Fact]
    public void AddConfiguredPersistence_Should_ResolveSqlServerProvider_WhenConfiguredAsSqlServer()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(
            ("Persistence:Provider", ServiceCollectionExtensionsConfiguredPersistence.SqlServerProviderName),
            ("ConnectionStrings:SqlServer", SqlServerConnectionString));

        services.AddConfiguredPersistence(configuration);
        using var provider = services.BuildServiceProvider();

        using var context = provider.GetRequiredService<DarwinDbContext>();
        context.Database.ProviderName.Should().NotBeNullOrWhiteSpace();
        context.Database.ProviderName.Should().Be("Microsoft.EntityFrameworkCore.SqlServer");
    }

    [Theory]
    [InlineData("Postgres")]
    [InlineData("Npgsql")]
    public void AddConfiguredPersistence_Should_ResolvePostgreSqlProvider_WhenAliasProvided(string providerValue)
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(
            ("Persistence:Provider", providerValue),
            ("ConnectionStrings:PostgreSql", PostgresConnectionString));

        services.AddConfiguredPersistence(configuration);
        using var serviceProvider = services.BuildServiceProvider();

        using var context = serviceProvider.GetRequiredService<DarwinDbContext>();
        context.Database.ProviderName.Should().Be("Npgsql.EntityFrameworkCore.PostgreSQL");
    }

    [Fact]
    public void AddConfiguredPersistence_Should_NormalizePostgreSqlConnectionDefaults()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(
            ("Persistence:Provider", ServiceCollectionExtensionsConfiguredPersistence.PostgreSqlProviderName),
            ("ConnectionStrings:PostgreSql", PostgresConnectionString));

        services.AddConfiguredPersistence(configuration);
        using var serviceProvider = services.BuildServiceProvider();

        using var context = serviceProvider.GetRequiredService<DarwinDbContext>();
        var builder = new NpgsqlConnectionStringBuilder(context.Database.GetConnectionString()!);

        builder.ApplicationName.Should().Be("Darwin");
        builder.MaxAutoPrepare.Should().Be(100);
        builder.AutoPrepareMinUsages.Should().Be(2);
        builder.KeepAlive.Should().Be(30);
        builder.Timeout.Should().Be(15);
        builder.CommandTimeout.Should().Be(60);
    }

    [Theory]
    [InlineData("MSSQL")]
    public void AddConfiguredPersistence_Should_ResolveSqlServerProvider_WhenAliasProvided(string providerValue)
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(
            ("Persistence:Provider", providerValue),
            ("ConnectionStrings:SqlServer", SqlServerConnectionString));

        services.AddConfiguredPersistence(configuration);
        using var serviceProvider = services.BuildServiceProvider();

        using var context = serviceProvider.GetRequiredService<DarwinDbContext>();
        context.Database.ProviderName.Should().Be("Microsoft.EntityFrameworkCore.SqlServer");
    }

    [Theory]
    [InlineData("MySql")]
    [InlineData("Oracle")]
    public void AddConfiguredPersistence_Should_Throw_OnUnsupportedProvider(string providerValue)
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(
            ("Persistence:Provider", providerValue),
            ("ConnectionStrings:PostgreSql", PostgresConnectionString),
            ("ConnectionStrings:SqlServer", SqlServerConnectionString));

        var action = () => services.AddConfiguredPersistence(configuration);

        action.Should()
            .Throw<InvalidOperationException>()
            .WithMessage(
                "Unsupported persistence provider *." +
                " Supported values are 'PostgreSql' and 'SqlServer'.");
    }

    private static IConfiguration BuildConfiguration(params (string key, string value)[] values)
    {
        var dictionary = values.Select(item => new KeyValuePair<string, string?>(item.key, item.value)).ToDictionary();
        return new ConfigurationBuilder().AddInMemoryCollection(dictionary).Build();
    }
}
