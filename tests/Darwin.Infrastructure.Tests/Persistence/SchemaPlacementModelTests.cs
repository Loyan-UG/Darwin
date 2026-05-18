using Darwin.Infrastructure.Persistence.Db;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Darwin.Infrastructure.Tests.Persistence;

public sealed class SchemaPlacementModelTests
{
    private const string DummySqlServerConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=DarwinPersistenceMetadata;Trusted_Connection=True;TrustServerCertificate=True;";

    private const string DummyPostgreSqlConnectionString =
        "Host=127.0.0.1;Database=DarwinPersistenceMetadata;Username=postgres;Password=postgres;";

    [Fact]
    public void SqlServerModel_Should_Not_MapTablesToSchemaDboOrNull()
    {
        using var context = new DarwinDbContext(
            new DbContextOptionsBuilder<DarwinDbContext>()
                .UseSqlServer(DummySqlServerConnectionString)
                .Options);

        var tableMappings = GetMappedTableEntries(context);

        tableMappings.Should().NotBeEmpty();
        tableMappings.Should().OnlyContain(entry =>
            !string.IsNullOrWhiteSpace(entry.Schema) &&
            !string.Equals(entry.Schema, "dbo", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PostgreSqlModel_Should_Not_MapTablesToSchemaPublicOrNull()
    {
        using var context = new DarwinDbContext(
            new DbContextOptionsBuilder<DarwinDbContext>()
                .UseNpgsql(DummyPostgreSqlConnectionString)
                .Options);

        var tableMappings = GetMappedTableEntries(context);

        tableMappings.Should().NotBeEmpty();
        tableMappings.Should().OnlyContain(entry =>
            !string.IsNullOrWhiteSpace(entry.Schema) &&
            !string.Equals(entry.Schema, "public", StringComparison.OrdinalIgnoreCase));
    }

    private static List<(string Table, string Schema)> GetMappedTableEntries(DarwinDbContext context)
    {
        return context.Model.GetEntityTypes()
            .Where(entityType => entityType.GetTableName() is not null)
            .Select(entityType => (Table: entityType.GetTableName()!, Schema: entityType.GetSchema() ?? string.Empty))
            .Distinct()
            .ToList();
}
}
