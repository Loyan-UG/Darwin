using Darwin.Infrastructure.Persistence.Db;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Darwin.Infrastructure.Tests.Persistence;

public sealed class MigrationScriptSchemaGuardTests
{
    private const string DummySqlServerConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=DarwinPersistenceMetadata;Trusted_Connection=True;TrustServerCertificate=True;";

    private const string DummyPostgreSqlConnectionString =
        "Host=127.0.0.1;Database=DarwinPersistenceMetadata;Username=postgres;Password=postgres;";

    [Fact]
    public void GenerateCreateScript_ForSqlServer_Should_UseConfiguredTableSchemas_AndAvoidDbo()
    {
        using var context = new DarwinDbContext(
            new DbContextOptionsBuilder<DarwinDbContext>()
                .UseSqlServer(DummySqlServerConnectionString)
                .Options);

        var script = context.Database.GenerateCreateScript();
        var mappings = GetTableMappings(context).ToList();

        mappings.Should().NotBeEmpty();
        script.Should().NotContain("CREATE TABLE [dbo].");

        foreach (var mapping in mappings)
        {
            var sqlServerRef = $"[{mapping.Schema}].[{mapping.Table}]";
            script.Should().Contain($"CREATE TABLE {sqlServerRef}");
        }
    }

    [Fact]
    public void GenerateCreateScript_ForPostgreSql_Should_UseConfiguredTableSchemas_AndAvoidPublic()
    {
        using var context = new DarwinDbContext(
            new DbContextOptionsBuilder<DarwinDbContext>()
                .UseNpgsql(DummyPostgreSqlConnectionString)
                .Options);

        var script = context.Database.GenerateCreateScript();
        var mappings = GetTableMappings(context).ToList();

        mappings.Should().NotBeEmpty();
        script.Should().NotContain("CREATE TABLE \"public\".");
        script.Should().NotContain("CREATE TABLE \"public\".\"");

        foreach (var mapping in mappings)
        {
            var pgRef = $"\"{mapping.Schema}\".\"{mapping.Table}\"";
            script.Should().Contain($"CREATE TABLE {pgRef}");
        }
    }

    private static IEnumerable<(string Schema, string Table)> GetTableMappings(DarwinDbContext context)
    {
        return context.Model.GetEntityTypes()
            .Where(entity => entity.GetTableName() is not null)
            .Where(entity => !string.Equals(entity.GetTableName(), "__EFMigrationsHistory", StringComparison.OrdinalIgnoreCase))
            .Select(entity => (
                Schema: entity.GetSchema() ?? string.Empty,
                Table: entity.GetTableName()!))
            .Distinct()
            .Where(mapping => !string.IsNullOrWhiteSpace(mapping.Schema));
    }
}
