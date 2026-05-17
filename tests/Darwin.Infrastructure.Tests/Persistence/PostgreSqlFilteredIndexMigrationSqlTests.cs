using Darwin.Infrastructure.Persistence.Db;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Infrastructure.Tests.Persistence;

public sealed class PostgreSqlFilteredIndexMigrationSqlTests
{
    private const string DummyPostgreSqlConnectionString =
        "Host=127.0.0.1;Database=DarwinPersistenceMetadata;Username=postgres;Password=postgres;";

    [Fact]
    public void GenerateCreateScript_ForPostgreSql_Should_EmitNormalizedCarrierServiceFilteredIndexPredicate()
    {
        using var context = new DarwinDbContext(
            new DbContextOptionsBuilder<DarwinDbContext>()
                .UseNpgsql(DummyPostgreSqlConnectionString)
                .Options);

        var script = context.Database.GenerateCreateScript();

        script.Should().Contain("\"UX_ShippingMethods_ActiveCarrierService\"");
        script.Should().Contain("WHERE \"IsDeleted\" = FALSE");
    }
}
