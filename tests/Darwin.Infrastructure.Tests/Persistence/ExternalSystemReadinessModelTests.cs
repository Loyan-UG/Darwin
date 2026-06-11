using Darwin.Domain.Entities.Integration;
using Darwin.Infrastructure.Persistence.Db;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Darwin.Infrastructure.Tests.Persistence;

public sealed class ExternalSystemReadinessModelTests
{
    private const string DummySqlServerConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=DarwinPersistenceMetadata;Trusted_Connection=True;TrustServerCertificate=True;";

    private const string DummyPostgreSqlConnectionString =
        "Host=127.0.0.1;Database=DarwinPersistenceMetadata;Username=postgres;Password=postgres;";

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void ExternalSystemAndReference_Should_MapToIntegrationSchema(string provider)
    {
        using var context = CreateContext(provider);

        context.Model.FindEntityType(typeof(ExternalSystem))!.GetSchema().Should().Be("Integration");
        context.Model.FindEntityType(typeof(ExternalSystem))!.GetTableName().Should().Be("ExternalSystems");
        context.Model.FindEntityType(typeof(ExternalReference))!.GetSchema().Should().Be("Integration");
        context.Model.FindEntityType(typeof(ExternalReference))!.GetTableName().Should().Be("ExternalReferences");
    }

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void ExternalSystem_Should_HaveStableRequiredColumnsAndUniqueCode(string provider)
    {
        using var context = CreateContext(provider);
        var entity = context.Model.FindEntityType(typeof(ExternalSystem))!;

        entity.FindProperty(nameof(ExternalSystem.Code))!.GetMaxLength().Should().Be(64);
        entity.FindProperty(nameof(ExternalSystem.Name))!.GetMaxLength().Should().Be(200);
        entity.FindProperty(nameof(ExternalSystem.Kind))!.GetMaxLength().Should().Be(32);
        entity.FindProperty(nameof(ExternalSystem.MetadataJson))!.GetMaxLength().Should().Be(4000);

        var codeIndex = entity.GetIndexes().Single(x => x.GetDatabaseName() == "UX_ExternalSystems_Code");
        codeIndex.IsUnique.Should().BeTrue();
        codeIndex.Properties.Select(x => x.Name).Should().Equal(nameof(ExternalSystem.Code));
        codeIndex.GetFilter().Should().Contain("IsDeleted");
    }

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void ExternalReference_Should_HaveLookupAndExternalIdentityIndexes(string provider)
    {
        using var context = CreateContext(provider);
        var entity = context.Model.FindEntityType(typeof(ExternalReference))!;

        entity.FindProperty(nameof(ExternalReference.EntityType))!.GetMaxLength().Should().Be(128);
        entity.FindProperty(nameof(ExternalReference.ReferenceKind))!.GetMaxLength().Should().Be(32);
        entity.FindProperty(nameof(ExternalReference.ExternalId))!.GetMaxLength().Should().Be(256);
        entity.FindProperty(nameof(ExternalReference.ExternalDisplayId))!.GetMaxLength().Should().Be(256);
        entity.FindProperty(nameof(ExternalReference.SourceOfTruth))!.GetMaxLength().Should().Be(32);
        entity.FindProperty(nameof(ExternalReference.MetadataJson))!.GetMaxLength().Should().Be(4000);

        var lookupIndex = entity.GetIndexes().Single(x => x.GetDatabaseName() == "IX_ExternalReferences_EntityType_EntityId");
        lookupIndex.Properties.Select(x => x.Name).Should().Equal(
            nameof(ExternalReference.EntityType),
            nameof(ExternalReference.EntityId));

        var identityIndex = entity.GetIndexes().Single(x => x.GetDatabaseName() == "UX_ExternalReferences_System_EntityType_Kind_ExternalId");
        identityIndex.IsUnique.Should().BeTrue();
        identityIndex.Properties.Select(x => x.Name).Should().Equal(
            nameof(ExternalReference.ExternalSystemId),
            nameof(ExternalReference.EntityType),
            nameof(ExternalReference.ReferenceKind),
            nameof(ExternalReference.ExternalId));
        identityIndex.GetFilter().Should().Contain("IsDeleted");
    }

    [Fact]
    public void PostgreSqlModel_Should_MapExternalMetadataJsonToJsonb()
    {
        using var context = CreateContext("PostgreSql");

        context.Model.FindEntityType(typeof(ExternalSystem))!
            .FindProperty(nameof(ExternalSystem.MetadataJson))!
            .GetColumnType()
            .Should().Be("jsonb");
        context.Model.FindEntityType(typeof(ExternalReference))!
            .FindProperty(nameof(ExternalReference.MetadataJson))!
            .GetColumnType()
            .Should().Be("jsonb");
    }

    private static DarwinDbContext CreateContext(string provider)
    {
        var builder = new DbContextOptionsBuilder<DarwinDbContext>();
        if (provider == "PostgreSql")
        {
            builder.UseNpgsql(DummyPostgreSqlConnectionString);
        }
        else
        {
            builder.UseSqlServer(DummySqlServerConnectionString);
        }

        return new DarwinDbContext(builder.Options);
    }
}
