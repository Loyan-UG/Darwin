using Darwin.Infrastructure.Persistence.Db;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Darwin.Infrastructure.Tests.Persistence;

public sealed class SharedEfMappingProviderAgnosticColumnTypesTests
{
    private const string DummyPostgreSqlConnectionString =
        "Host=127.0.0.1;Database=DarwinPersistenceMetadata;Username=postgres;Password=postgres;";

    [Fact]
    public void PostgreSqlModel_Should_Not_UseSqlServerOnlyColumnTypeLiterals()
    {
        using var context = new DarwinDbContext(
            new DbContextOptionsBuilder<DarwinDbContext>()
                .UseNpgsql(DummyPostgreSqlConnectionString)
                .Options);

        var forbiddenTypeLiterals = new[] { "uniqueidentifier", "nvarchar(max)" };
        var offendingProperties = context.Model.GetEntityTypes()
            .SelectMany(entityType => entityType.GetProperties())
            .Select(property => new
            {
                EntityName = property.DeclaringType?.Name ?? string.Empty,
                PropertyName = property.Name,
                ColumnType = property.GetColumnType()
            })
            .Where(item =>
                !string.IsNullOrWhiteSpace(item.ColumnType) &&
                forbiddenTypeLiterals.Any(literal =>
                    item.ColumnType!.Contains(literal, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        offendingProperties.Should().BeEmpty(
            "shared mappings must stay provider-agnostic and should not force SQL Server-specific types");
    }
}
