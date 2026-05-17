using Darwin.Infrastructure.Persistence.Db;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Infrastructure.Tests.Persistence;

public sealed class ProviderNeutralDecimalPrecisionModelTests
{
    private const string DummySqlServerConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=DarwinPersistenceMetadata;Trusted_Connection=True;TrustServerCertificate=True;";

    private const string DummyPostgreSqlConnectionString =
        "Host=127.0.0.1;Database=DarwinPersistenceMetadata;Username=postgres;Password=postgres;";

    [Fact]
    public void SqlServerModel_Should_ConfigurePrecisionAndScale_ForAllDecimalProperties()
    {
        var violations = GetDecimalPrecisionViolations(
            "SqlServer",
            new DbContextOptionsBuilder<DarwinDbContext>()
                .UseSqlServer(DummySqlServerConnectionString)
                .Options);

        violations.Should().BeEmpty(
            "all decimal properties must keep explicit precision metadata via entity config or fallback convention");
    }

    [Fact]
    public void PostgreSqlModel_Should_ConfigurePrecisionAndScale_ForAllDecimalProperties()
    {
        var violations = GetDecimalPrecisionViolations(
            "PostgreSql",
            new DbContextOptionsBuilder<DarwinDbContext>()
                .UseNpgsql(DummyPostgreSqlConnectionString)
                .Options);

        violations.Should().BeEmpty(
            "all decimal properties must keep explicit precision metadata via entity config or fallback convention");
    }

    private static List<string> GetDecimalPrecisionViolations(string provider, DbContextOptions<DarwinDbContext> options)
    {
        using var context = new DarwinDbContext(options);

        return context.Model
            .GetEntityTypes()
            .SelectMany(entityType => entityType.GetProperties())
            .Where(property => property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
            .Select(property => new
            {
                Entity = property.DeclaringType?.Name ?? string.Empty,
                Property = property.Name,
                Precision = property.GetPrecision(),
                Scale = property.GetScale()
            })
            .Where(item => !item.Precision.HasValue || !item.Scale.HasValue || item.Precision <= 0 || item.Scale < 0)
            .Select(item => $"{provider}: {item.Entity}.{item.Property} precision={(item.Precision?.ToString() ?? "null")} scale={(item.Scale?.ToString() ?? "null")}")
            .ToList();
    }
}
