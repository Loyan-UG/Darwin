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

    /// <summary>
    ///     Ensures environment-specific appsettings override base appsettings values
    ///     so design-time migrations follow the active ASPNETCORE_ENVIRONMENT.
    /// </summary>
    [Fact]
    public void CreateDbContext_Should_PreferEnvironmentSpecificAppsettings_WhenBothFilesExist()
    {
        // Arrange
        const string connectionEnvName = "ConnectionStrings__DefaultConnection";
        const string aspnetEnvName = "ASPNETCORE_ENVIRONMENT";
        var previousConnection = Environment.GetEnvironmentVariable(connectionEnvName);
        var previousAspNetEnvironment = Environment.GetEnvironmentVariable(aspnetEnvName);
        var previousCwd = Directory.GetCurrentDirectory();
        var isolatedDir = Directory.CreateTempSubdirectory("darwin-design-time-env-specific-");
        var baseConnection = "Server=127.0.0.1;Database=DarwinBaseConfig;User Id=sa;Password=Passw0rd!;TrustServerCertificate=True;";
        var envConnection = "Server=127.0.0.1;Database=DarwinTestingConfig;User Id=sa;Password=Passw0rd!;TrustServerCertificate=True;";

        try
        {
            File.WriteAllText(
                Path.Combine(isolatedDir.FullName, "appsettings.json"),
                $$"""
                  {
                    "ConnectionStrings": {
                      "DefaultConnection": "{{baseConnection}}"
                    }
                  }
                  """);

            File.WriteAllText(
                Path.Combine(isolatedDir.FullName, "appsettings.Testing.json"),
                $$"""
                  {
                    "ConnectionStrings": {
                      "DefaultConnection": "{{envConnection}}"
                    }
                  }
                  """);

            Environment.SetEnvironmentVariable(connectionEnvName, null);
            Environment.SetEnvironmentVariable(aspnetEnvName, "Testing");
            Directory.SetCurrentDirectory(isolatedDir.FullName);
            var factory = new DesignTimeDbContextFactory();

            // Act
            using var context = factory.CreateDbContext([]);
            var connectionString = context.Database.GetConnectionString();

            // Assert
            connectionString.Should().NotBeNullOrWhiteSpace();
            connectionString.Should().ContainEquivalentOf("DarwinTestingConfig");
            connectionString.Should().NotContainEquivalentOf("DarwinBaseConfig");
        }
        finally
        {
            Directory.SetCurrentDirectory(previousCwd);
            Environment.SetEnvironmentVariable(connectionEnvName, previousConnection);
            Environment.SetEnvironmentVariable(aspnetEnvName, previousAspNetEnvironment);
            isolatedDir.Delete(recursive: true);
        }
    }

    /// <summary>
    ///     Ensures whitespace environment connection strings do not shadow appsettings values.
    /// </summary>
    [Fact]
    public void CreateDbContext_Should_IgnoreWhitespaceEnvironmentConnectionString_AndUseConfig()
    {
        // Arrange
        const string connectionEnvName = "ConnectionStrings__DefaultConnection";
        const string aspnetEnvName = "ASPNETCORE_ENVIRONMENT";
        var previousConnection = Environment.GetEnvironmentVariable(connectionEnvName);
        var previousAspNetEnvironment = Environment.GetEnvironmentVariable(aspnetEnvName);
        var previousCwd = Directory.GetCurrentDirectory();
        var isolatedDir = Directory.CreateTempSubdirectory("darwin-design-time-whitespace-env-");
        var expected = "Server=127.0.0.1;Database=DarwinConfigFallback;User Id=sa;Password=Passw0rd!;TrustServerCertificate=True;";

        try
        {
            File.WriteAllText(
                Path.Combine(isolatedDir.FullName, "appsettings.json"),
                $$"""
                  {
                    "ConnectionStrings": {
                      "DefaultConnection": "{{expected}}"
                    }
                  }
                  """);

            Environment.SetEnvironmentVariable(connectionEnvName, "   ");
            Environment.SetEnvironmentVariable(aspnetEnvName, "Development");
            Directory.SetCurrentDirectory(isolatedDir.FullName);
            var factory = new DesignTimeDbContextFactory();

            // Act
            using var context = factory.CreateDbContext([]);
            var connectionString = context.Database.GetConnectionString();

            // Assert
            connectionString.Should().NotBeNullOrWhiteSpace();
            connectionString.Should().ContainEquivalentOf("DarwinConfigFallback");
        }
        finally
        {
            Directory.SetCurrentDirectory(previousCwd);
            Environment.SetEnvironmentVariable(connectionEnvName, previousConnection);
            Environment.SetEnvironmentVariable(aspnetEnvName, previousAspNetEnvironment);
            isolatedDir.Delete(recursive: true);
        }
    }

    /// <summary>
    ///     Ensures appsettings discovery probes sibling <c>Darwin.WebAdmin</c> path
    ///     so EF design-time commands still resolve configuration when executed from a nested folder.
    /// </summary>
    [Fact]
    public void CreateDbContext_Should_ReadConnectionStringFromSiblingWebAdminProbePath()
    {
        // Arrange
        const string connectionEnvName = "ConnectionStrings__DefaultConnection";
        const string aspnetEnvName = "ASPNETCORE_ENVIRONMENT";
        var previousConnection = Environment.GetEnvironmentVariable(connectionEnvName);
        var previousAspNetEnvironment = Environment.GetEnvironmentVariable(aspnetEnvName);
        var previousCwd = Directory.GetCurrentDirectory();
        var rootDir = Directory.CreateTempSubdirectory("darwin-design-time-probe-root-");
        var runDir = Directory.CreateDirectory(Path.Combine(rootDir.FullName, "runner"));
        var webAdminDir = Directory.CreateDirectory(Path.Combine(rootDir.FullName, "Darwin.WebAdmin"));
        var expected = "Server=127.0.0.1;Database=DarwinProbeFromWebAdmin;User Id=sa;Password=Passw0rd!;TrustServerCertificate=True;";

        try
        {
            File.WriteAllText(
                Path.Combine(webAdminDir.FullName, "appsettings.json"),
                $$"""
                  {
                    "ConnectionStrings": {
                      "DefaultConnection": "{{expected}}"
                    }
                  }
                  """);

            Environment.SetEnvironmentVariable(connectionEnvName, null);
            Environment.SetEnvironmentVariable(aspnetEnvName, "Development");
            Directory.SetCurrentDirectory(runDir.FullName);
            var factory = new DesignTimeDbContextFactory();

            // Act
            using var context = factory.CreateDbContext([]);
            var connectionString = context.Database.GetConnectionString();

            // Assert
            connectionString.Should().NotBeNullOrWhiteSpace();
            connectionString.Should().ContainEquivalentOf("DarwinProbeFromWebAdmin");
        }
        finally
        {
            Directory.SetCurrentDirectory(previousCwd);
            Environment.SetEnvironmentVariable(connectionEnvName, previousConnection);
            Environment.SetEnvironmentVariable(aspnetEnvName, previousAspNetEnvironment);
            rootDir.Delete(recursive: true);
        }
    }

    /// <summary>
    ///     Ensures explicit environment variable keeps top precedence even when
    ///     appsettings files are discoverable in probe paths.
    /// </summary>
    [Fact]
    public void CreateDbContext_Should_PreferEnvironmentVariableOverDiscoveredConfigFiles()
    {
        // Arrange
        const string connectionEnvName = "ConnectionStrings__DefaultConnection";
        const string aspnetEnvName = "ASPNETCORE_ENVIRONMENT";
        var previousConnection = Environment.GetEnvironmentVariable(connectionEnvName);
        var previousAspNetEnvironment = Environment.GetEnvironmentVariable(aspnetEnvName);
        var previousCwd = Directory.GetCurrentDirectory();
        var rootDir = Directory.CreateTempSubdirectory("darwin-design-time-env-precedence-");
        var runDir = Directory.CreateDirectory(Path.Combine(rootDir.FullName, "runner"));
        var webAdminDir = Directory.CreateDirectory(Path.Combine(rootDir.FullName, "Darwin.WebAdmin"));
        var fromEnvironment = "Server=127.0.0.1;Database=DarwinEnvWins;User Id=sa;Password=Passw0rd!;TrustServerCertificate=True;";
        var fromConfig = "Server=127.0.0.1;Database=DarwinConfigLoses;User Id=sa;Password=Passw0rd!;TrustServerCertificate=True;";

        try
        {
            File.WriteAllText(
                Path.Combine(webAdminDir.FullName, "appsettings.json"),
                $$"""
                  {
                    "ConnectionStrings": {
                      "DefaultConnection": "{{fromConfig}}"
                    }
                  }
                  """);

            Environment.SetEnvironmentVariable(connectionEnvName, fromEnvironment);
            Environment.SetEnvironmentVariable(aspnetEnvName, "Development");
            Directory.SetCurrentDirectory(runDir.FullName);
            var factory = new DesignTimeDbContextFactory();

            // Act
            using var context = factory.CreateDbContext([]);
            var connectionString = context.Database.GetConnectionString();

            // Assert
            connectionString.Should().NotBeNullOrWhiteSpace();
            connectionString.Should().ContainEquivalentOf("DarwinEnvWins");
            connectionString.Should().NotContainEquivalentOf("DarwinConfigLoses");
        }
        finally
        {
            Directory.SetCurrentDirectory(previousCwd);
            Environment.SetEnvironmentVariable(connectionEnvName, previousConnection);
            Environment.SetEnvironmentVariable(aspnetEnvName, previousAspNetEnvironment);
            rootDir.Delete(recursive: true);
        }
    }

}
