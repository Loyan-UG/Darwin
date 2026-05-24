using Darwin.Domain.Entities.Shipping;
using Darwin.Infrastructure.Persistence.Db;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Darwin.Infrastructure.Tests.Persistence;

public sealed class ShippingMethodCarrierServiceIndexProviderSpecificTests
{
    private const string DummySqlServerConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=DarwinPersistenceMetadata;Trusted_Connection=True;TrustServerCertificate=True;";

    private const string DummyPostgreSqlConnectionString =
        "Host=127.0.0.1;Database=DarwinPersistenceMetadata;Username=postgres;Password=postgres;";

    [Fact]
    public void SqlServerContext_Should_KeepShippingMethodCarrierServiceIndexAsFilteredIndex_ForActiveRows()
    {
        // Arrange
        using var context = BuildSqlServerContext();

        // Act
        var index = FindCarrierServiceCarrierIndex(context);

        // Assert
        index.IsUnique.Should().BeTrue();
        index.Properties.Select(p => p.Name)
            .Should().Equal("Carrier", "Service");
        index.GetDatabaseName().Should().Be("UX_ShippingMethods_ActiveCarrierService");
        index.GetFilter().Should().Be("[IsDeleted] = 0");
    }

    [Fact]
    public void NpgsqlContext_Should_NormalizeCarrierServiceFilterToPostgreSqlPredicate()
    {
        // Arrange
        using var context = BuildPostgreSqlContext();

        // Act
        var index = FindCarrierServiceCarrierIndex(context);

        // Assert
        index.IsUnique.Should().BeTrue();
        index.Properties.Select(p => p.Name)
            .Should().Equal("Carrier", "Service");
        index.GetDatabaseName().Should().Be("UX_ShippingMethods_ActiveCarrierService");
        index.GetFilter().Should().Be("\"IsDeleted\" = FALSE");
    }

    private static DarwinDbContext BuildSqlServerContext()
    {
        var options = new DbContextOptionsBuilder<DarwinDbContext>()
            .UseSqlServer(DummySqlServerConnectionString)
            .Options;

        return new DarwinDbContext(options);
    }

    private static DarwinDbContext BuildPostgreSqlContext()
    {
        var options = new DbContextOptionsBuilder<DarwinDbContext>()
            .UseNpgsql(DummyPostgreSqlConnectionString)
            .Options;

        return new DarwinDbContext(options);
    }

    private static IIndex FindCarrierServiceCarrierIndex(DarwinDbContext context)
    {
        var entityType = context.Model.FindEntityType(typeof(ShippingMethod));
        entityType.Should().NotBeNull();

        return entityType!.GetIndexes()
            .Single(index => index.GetDatabaseName() == "UX_ShippingMethods_ActiveCarrierService");
    }
}
