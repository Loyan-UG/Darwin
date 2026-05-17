using Darwin.Infrastructure.Persistence.Db;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Darwin.Infrastructure.Tests.Persistence;

public sealed class RowVersionProviderConcurrencyModelTests
{
    private const string DummySqlServerConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=DarwinPersistenceMetadata;Trusted_Connection=True;TrustServerCertificate=True;";

    private const string DummyPostgreSqlConnectionString =
        "Host=127.0.0.1;Database=DarwinPersistenceMetadata;Username=postgres;Password=postgres;";

    [Fact]
    public void SqlServerModel_Should_UseRowVersionWithConcurrencyToken_ForRowVersionProperties()
    {
        using var context = new DarwinDbContext(
            new DbContextOptionsBuilder<DarwinDbContext>()
                .UseSqlServer(DummySqlServerConnectionString)
                .Options);

        var rowVersionProperties = GetRowVersionProperties(context);

        rowVersionProperties.Should().NotBeEmpty();

        rowVersionProperties.Should().OnlyContain(p =>
            p.IsConcurrencyToken &&
            p.ValueGenerated == ValueGenerated.OnAddOrUpdate &&
            string.Equals(p.GetColumnType(), "rowversion", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PostgreSqlModel_Should_UseByteaWithClientManagedConcurrency_ForRowVersionProperties()
    {
        using var context = new DarwinDbContext(
            new DbContextOptionsBuilder<DarwinDbContext>()
                .UseNpgsql(DummyPostgreSqlConnectionString)
                .Options);

        var rowVersionProperties = GetRowVersionProperties(context);

        rowVersionProperties.Should().NotBeEmpty();

        rowVersionProperties.Should().OnlyContain(p =>
            p.IsConcurrencyToken &&
            p.ValueGenerated == ValueGenerated.Never &&
            string.Equals(p.GetColumnType(), "bytea", StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<IProperty> GetRowVersionProperties(DarwinDbContext context)
    {
        return context.Model.GetEntityTypes()
            .SelectMany(entity => entity.GetProperties())
            .Where(property => property.Name == "RowVersion" && property.ClrType == typeof(byte[]))
            .ToList();
    }
}
