using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Integration;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.Integration;

public sealed class ExternalSystemReferenceServiceTests
{
    [Fact]
    public async Task CreateExternalSystemAsync_Should_NormalizeCode_And_RejectDuplicates()
    {
        await using var db = ExternalSystemReferenceTestDbContext.Create();
        var service = new ExternalSystemReferenceService(db);

        var created = await service.CreateExternalSystemAsync(new CreateExternalSystemCommand(
            " erp-main ",
            " Main ERP ",
            ExternalSystemKind.Erp));
        var duplicate = await service.CreateExternalSystemAsync(new CreateExternalSystemCommand(
            "ERP-MAIN",
            "Duplicate",
            ExternalSystemKind.Erp));

        created.Succeeded.Should().BeTrue();
        duplicate.Succeeded.Should().BeFalse();

        var entity = await db.Set<ExternalSystem>().SingleAsync();
        entity.Code.Should().Be("ERP-MAIN");
        entity.Name.Should().Be("Main ERP");
        entity.MetadataJson.Should().Be("{}");
    }

    [Theory]
    [InlineData(null, "Name")]
    [InlineData(" ", "Name")]
    [InlineData("CODE", null)]
    [InlineData("CODE", " ")]
    public async Task CreateExternalSystemAsync_Should_RejectMissingRequiredFields(string? code, string? name)
    {
        await using var db = ExternalSystemReferenceTestDbContext.Create();
        var service = new ExternalSystemReferenceService(db);

        var result = await service.CreateExternalSystemAsync(new CreateExternalSystemCommand(
            code,
            name,
            ExternalSystemKind.Custom));

        result.Succeeded.Should().BeFalse();
        (await db.Set<ExternalSystem>().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task UpsertReferenceAsync_Should_CreateThenUpdateExistingReference()
    {
        await using var db = ExternalSystemReferenceTestDbContext.Create();
        var service = new ExternalSystemReferenceService(db);
        var systemId = (await service.CreateExternalSystemAsync(new CreateExternalSystemCommand(
            "CRM",
            "CRM",
            ExternalSystemKind.Crm))).Value;
        var firstEntityId = Guid.NewGuid();
        var secondEntityId = Guid.NewGuid();

        var created = await service.UpsertReferenceAsync(new UpsertExternalReferenceCommand(
            systemId,
            " Customer ",
            firstEntityId,
            ExternalReferenceKind.Primary,
            " ext-123 ",
            " EXT-123 ",
            SourceOfTruth.External,
            IsPrimary: true,
            MetadataJson: "{\"quality\":\"seed\"}"));
        var updated = await service.UpsertReferenceAsync(new UpsertExternalReferenceCommand(
            systemId,
            "Customer",
            secondEntityId,
            ExternalReferenceKind.Primary,
            "ext-123",
            null,
            SourceOfTruth.Shared,
            IsPrimary: false));

        created.Succeeded.Should().BeTrue();
        updated.Succeeded.Should().BeTrue();
        updated.Value.Should().Be(created.Value);

        var reference = await db.Set<ExternalReference>().SingleAsync();
        reference.EntityType.Should().Be("Customer");
        reference.EntityId.Should().Be(secondEntityId);
        reference.ExternalId.Should().Be("ext-123");
        reference.ExternalDisplayId.Should().BeNull();
        reference.SourceOfTruth.Should().Be(SourceOfTruth.Shared);
        reference.IsPrimary.Should().BeFalse();
        reference.MetadataJson.Should().Be("{}");
    }

    [Fact]
    public async Task UpsertReferenceAsync_Should_RejectMissingIdentifiers()
    {
        await using var db = ExternalSystemReferenceTestDbContext.Create();
        var service = new ExternalSystemReferenceService(db);

        var missingSystem = await service.UpsertReferenceAsync(new UpsertExternalReferenceCommand(
            Guid.Empty,
            "Customer",
            Guid.NewGuid(),
            ExternalReferenceKind.Primary,
            "external-id",
            null,
            SourceOfTruth.External));
        var missingEntity = await service.UpsertReferenceAsync(new UpsertExternalReferenceCommand(
            Guid.NewGuid(),
            "Customer",
            Guid.Empty,
            ExternalReferenceKind.Primary,
            "external-id",
            null,
            SourceOfTruth.External));
        var missingType = await service.UpsertReferenceAsync(new UpsertExternalReferenceCommand(
            Guid.NewGuid(),
            " ",
            Guid.NewGuid(),
            ExternalReferenceKind.Primary,
            "external-id",
            null,
            SourceOfTruth.External));
        var missingExternalId = await service.UpsertReferenceAsync(new UpsertExternalReferenceCommand(
            Guid.NewGuid(),
            "Customer",
            Guid.NewGuid(),
            ExternalReferenceKind.Primary,
            " ",
            null,
            SourceOfTruth.External));

        missingSystem.Succeeded.Should().BeFalse();
        missingEntity.Succeeded.Should().BeFalse();
        missingType.Succeeded.Should().BeFalse();
        missingExternalId.Succeeded.Should().BeFalse();
        (await db.Set<ExternalReference>().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task GetReferencesForEntityAsync_Should_ReturnOnlyActiveReferencesForEntity()
    {
        await using var db = ExternalSystemReferenceTestDbContext.Create();
        var service = new ExternalSystemReferenceService(db);
        var systemId = (await service.CreateExternalSystemAsync(new CreateExternalSystemCommand(
            "ACCOUNTING",
            "Accounting",
            ExternalSystemKind.Accounting))).Value;
        var entityId = Guid.NewGuid();

        await service.UpsertReferenceAsync(new UpsertExternalReferenceCommand(
            systemId,
            "Invoice",
            entityId,
            ExternalReferenceKind.Primary,
            "INV-1",
            null,
            SourceOfTruth.Darwin,
            IsPrimary: true));
        await service.UpsertReferenceAsync(new UpsertExternalReferenceCommand(
            systemId,
            "Invoice",
            entityId,
            ExternalReferenceKind.Alternate,
            "INV-2",
            null,
            SourceOfTruth.Shared,
            IsActive: false));
        await service.UpsertReferenceAsync(new UpsertExternalReferenceCommand(
            systemId,
            "Invoice",
            Guid.NewGuid(),
            ExternalReferenceKind.Primary,
            "INV-3",
            null,
            SourceOfTruth.Shared));

        var references = await service.GetReferencesForEntityAsync(" Invoice ", entityId);

        references.Should().ContainSingle();
        references[0].ExternalId.Should().Be("INV-1");
        references[0].IsPrimary.Should().BeTrue();
    }

    private sealed class ExternalSystemReferenceTestDbContext : DbContext, IAppDbContext
    {
        private ExternalSystemReferenceTestDbContext(DbContextOptions<ExternalSystemReferenceTestDbContext> options)
            : base(options)
        {
        }

        public static ExternalSystemReferenceTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<ExternalSystemReferenceTestDbContext>()
                .UseInMemoryDatabase($"darwin_external_references_{Guid.NewGuid()}")
                .Options;
            return new ExternalSystemReferenceTestDbContext(options);
        }

        public DbSet<ExternalSystem> ExternalSystems => Set<ExternalSystem>();
        public DbSet<ExternalReference> ExternalReferences => Set<ExternalReference>();
    }
}
