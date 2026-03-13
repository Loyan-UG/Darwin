using Darwin.Infrastructure.Persistence.Db;
using FluentAssertions;

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

            // Assert
            context.Database.GetConnectionString().Should().Be(expected);
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
}
