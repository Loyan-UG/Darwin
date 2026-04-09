using System.IO;
using Darwin.Infrastructure.Persistence.Db;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Infrastructure.Tests.Persistence;

/// <summary>
///     Verifies design-time DbContext factory behavior used by EF tooling.
/// </summary>
public sealed class DesignTimeDbContextFactoryTests
{
    /// <summary>
    ///     Ensures explicit environment connection string has highest precedence
    ///     and is used when creating the design-time DbContext.
    /// </summary>
    [Fact]
    public void CreateDbContext_Should_UseConnectionStringFromEnvironment_WhenProvided()
    {
        // Arrange
        const string envName = "ConnectionStrings__DefaultConnection";
        const string expected = "Server=127.0.0.1;Database=DarwinDesignTime;User Id=sa;Password=Passw0rd!;TrustServerCertificate=True;";
        var previous = Environment.GetEnvironmentVariable(envName);

        try
        {
            Environment.SetEnvironmentVariable(envName, expected);
            var factory = new DesignTimeDbContextFactory();

            // Act
            using var context = factory.CreateDbContext([]);
            var connectionString = context.Database.GetConnectionString();

            // Assert
            connectionString.Should().NotBeNullOrWhiteSpace();
            connectionString!.Should().ContainEquivalentOf("127.0.0.1");
            connectionString.Should().ContainEquivalentOf("DarwinDesignTime");
            connectionString.Should().ContainEquivalentOf("User ID=sa");
            connectionString.Should().ContainEquivalentOf("Trust Server Certificate=True");
        }
        finally
        {
            Environment.SetEnvironmentVariable(envName, previous);
        }
    }

    /// <summary>
    ///     Ensures the factory always returns a usable DbContext instance even
    ///     when no explicit environment override is present.
    /// </summary>
    [Fact]
    public void CreateDbContext_Should_ReturnConfiguredContext_WhenEnvironmentConnectionMissing()
    {
        // Arrange
        const string envName = "ConnectionStrings__DefaultConnection";
        var previous = Environment.GetEnvironmentVariable(envName);

        try
        {
            Environment.SetEnvironmentVariable(envName, null);
            var factory = new DesignTimeDbContextFactory();

            // Act
            using var context = factory.CreateDbContext([]);

            // Assert
            context.Should().NotBeNull();
            context.Database.GetConnectionString().Should().NotBeNullOrWhiteSpace();
        }
        finally
        {
            Environment.SetEnvironmentVariable(envName, previous);
        }
    }

    /// <summary>
    ///     Ensures design-time context is configured for SQL Server provider so
    ///     EF tooling and runtime migrations target the intended database engine.
    /// </summary>
    [Fact]
    public void CreateDbContext_Should_UseSqlServerProvider()
    {
        // Arrange
        var factory = new DesignTimeDbContextFactory();

        // Act
        using var context = factory.CreateDbContext([]);
        var providerName = context.Database.ProviderName;

        // Assert
        providerName.Should().NotBeNullOrWhiteSpace();
        providerName.Should().Contain("SqlServer");
    }

    /// <summary>
    ///     Ensures design-time factory falls back to deterministic LocalDB connection string
    ///     when neither environment override nor discoverable appsettings are available.
    /// </summary>
    [Fact]
    public void CreateDbContext_Should_FallbackToLocalDb_WhenNoEnvironmentAndNoConfigFilesExist()
    {
        // Arrange
        const string envName = "ConnectionStrings__DefaultConnection";
        var previousEnv = Environment.GetEnvironmentVariable(envName);
        var previousCwd = Directory.GetCurrentDirectory();
        var isolatedDir = Directory.CreateTempSubdirectory("darwin-design-time-no-config-");

        try
        {
            Environment.SetEnvironmentVariable(envName, null);
            Directory.SetCurrentDirectory(isolatedDir.FullName);

            var factory = new DesignTimeDbContextFactory();

            // Act
            using var context = factory.CreateDbContext([]);
            var connectionString = context.Database.GetConnectionString();

            // Assert
            connectionString.Should().NotBeNullOrWhiteSpace();
            connectionString!.Should().ContainEquivalentOf("(localdb)\\MSSQLLocalDB");
            connectionString.Should().ContainEquivalentOf("Darwin");
        }
        finally
        {
            Directory.SetCurrentDirectory(previousCwd);
            Environment.SetEnvironmentVariable(envName, previousEnv);
            isolatedDir.Delete(recursive: true);
        }
    }

}
