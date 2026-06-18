using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Integration;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.Integration;

public sealed class SyncStateServiceTests
{
    [Fact]
    public async Task UpsertStateAsync_Should_CreateThenUpdateExistingState()
    {
        await using var db = SyncStateTestDbContext.Create();
        var service = new SyncStateService(db);
        var externalSystemId = await CreateSystemAsync(db);
        var entityId = Guid.NewGuid();

        var created = await service.UpsertStateAsync(new UpsertSyncStateCommand(
            externalSystemId,
            " Customer ",
            entityId,
            SyncDirection.Bidirectional,
            SyncStateStatus.PendingInbound,
            SyncScope: "master-data",
            AttemptCount: 1,
            MetadataJson: "{\"source\":\"import\"}"));
        var updated = await service.UpsertStateAsync(new UpsertSyncStateCommand(
            externalSystemId,
            "Customer",
            entityId,
            SyncDirection.Bidirectional,
            SyncStateStatus.Synced,
            SyncScope: "master-data",
            LastSuccessfulSyncAtUtc: new DateTime(2026, 6, 17, 12, 0, 0, DateTimeKind.Utc)));

        created.Succeeded.Should().BeTrue();
        updated.Succeeded.Should().BeTrue();
        updated.Value.Should().Be(created.Value);

        var state = await db.Set<SyncState>().SingleAsync();
        state.EntityType.Should().Be("Customer");
        state.SyncScope.Should().Be("master-data");
        state.Status.Should().Be(SyncStateStatus.Synced);
        state.AttemptCount.Should().Be(0);
        state.MetadataJson.Should().Be("{}");
    }

    [Fact]
    public async Task RecordConflictAsync_Should_CreateIdempotentOpenConflict_And_SetStateConflict()
    {
        await using var db = SyncStateTestDbContext.Create();
        var service = new SyncStateService(db);
        var externalSystemId = await CreateSystemAsync(db);
        var stateId = (await service.UpsertStateAsync(new UpsertSyncStateCommand(
            externalSystemId,
            "SupplierInvoice",
            Guid.NewGuid(),
            SyncDirection.Inbound,
            SyncStateStatus.PendingInbound))).Value;

        var first = await service.RecordConflictAsync(new RecordSyncConflictCommand(
            stateId,
            "invoice-total",
            FieldPath: "TotalGrossMinor",
            DarwinValueSummary: "10000",
            ExternalValueSummary: "12000",
            DetectedAtUtc: new DateTime(2026, 6, 17, 13, 0, 0, DateTimeKind.Utc)));
        var second = await service.RecordConflictAsync(new RecordSyncConflictCommand(
            stateId,
            " invoice-total ",
            FieldPath: "TotalGrossMinor",
            DarwinValueSummary: "10000",
            ExternalValueSummary: "13000"));

        first.Succeeded.Should().BeTrue();
        second.Succeeded.Should().BeTrue();
        second.Value.Should().Be(first.Value);

        var conflict = await db.Set<SyncConflict>().SingleAsync();
        conflict.ExternalValueSummary.Should().Be("13000");
        conflict.Status.Should().Be(SyncConflictStatus.Open);

        var state = await db.Set<SyncState>().SingleAsync();
        state.Status.Should().Be(SyncStateStatus.Conflict);
    }

    [Fact]
    public async Task ResolveConflictAsync_Should_RecordResolution_And_ClearStateWhenNoOtherOpenConflicts()
    {
        await using var db = SyncStateTestDbContext.Create();
        var service = new SyncStateService(db);
        var externalSystemId = await CreateSystemAsync(db);
        var stateId = (await service.UpsertStateAsync(new UpsertSyncStateCommand(
            externalSystemId,
            "Customer",
            Guid.NewGuid(),
            SyncDirection.Inbound,
            SyncStateStatus.PendingInbound))).Value;
        var conflictId = (await service.RecordConflictAsync(new RecordSyncConflictCommand(
            stateId,
            "email"))).Value;

        var resolved = await service.ResolveConflictAsync(new ResolveSyncConflictCommand(
            conflictId,
            SyncConflictResolution.UseDarwin,
            ResolutionSummary: "Darwin record kept.",
            ResolvedAtUtc: new DateTime(2026, 6, 17, 14, 0, 0, DateTimeKind.Utc)));

        resolved.Succeeded.Should().BeTrue();

        var conflict = await db.Set<SyncConflict>().SingleAsync();
        conflict.Status.Should().Be(SyncConflictStatus.Resolved);
        conflict.Resolution.Should().Be(SyncConflictResolution.UseDarwin);
        conflict.ResolvedAtUtc.Should().NotBeNull();

        var state = await db.Set<SyncState>().SingleAsync();
        state.Status.Should().Be(SyncStateStatus.PendingInbound);
    }

    [Theory]
    [InlineData("{\"accessToken\":\"secret\"}")]
    [InlineData("{\"raw_provider_payload\":{}}")]
    [InlineData("{\"connectionString\":\"server\"}")]
    public async Task UpsertStateAsync_Should_RejectSensitiveMetadata(string metadata)
    {
        await using var db = SyncStateTestDbContext.Create();
        var service = new SyncStateService(db);
        var externalSystemId = await CreateSystemAsync(db);

        var result = await service.UpsertStateAsync(new UpsertSyncStateCommand(
            externalSystemId,
            "Customer",
            Guid.NewGuid(),
            SyncDirection.Outbound,
            SyncStateStatus.Failed,
            MetadataJson: metadata));

        result.Succeeded.Should().BeFalse();
        (await db.Set<SyncState>().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task GetOpenConflictsAsync_Should_FilterByEntity()
    {
        await using var db = SyncStateTestDbContext.Create();
        var service = new SyncStateService(db);
        var externalSystemId = await CreateSystemAsync(db);
        var customerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var customerStateId = (await service.UpsertStateAsync(new UpsertSyncStateCommand(
            externalSystemId,
            "Customer",
            customerId,
            SyncDirection.Inbound,
            SyncStateStatus.PendingInbound))).Value;
        var orderStateId = (await service.UpsertStateAsync(new UpsertSyncStateCommand(
            externalSystemId,
            "Order",
            orderId,
            SyncDirection.Inbound,
            SyncStateStatus.PendingInbound))).Value;

        await service.RecordConflictAsync(new RecordSyncConflictCommand(customerStateId, "customer-name"));
        await service.RecordConflictAsync(new RecordSyncConflictCommand(orderStateId, "order-total"));

        var conflicts = await service.GetOpenConflictsAsync("Customer", customerId);

        conflicts.Should().ContainSingle();
        conflicts[0].EntityType.Should().Be("Customer");
        conflicts[0].EntityId.Should().Be(customerId);
    }

    private static async Task<Guid> CreateSystemAsync(SyncStateTestDbContext db)
    {
        var system = new ExternalSystem
        {
            Code = $"CRM-{Guid.NewGuid():N}",
            Name = "CRM",
            Kind = ExternalSystemKind.Crm,
            IsActive = true,
            MetadataJson = "{}"
        };
        db.Set<ExternalSystem>().Add(system);
        await db.SaveChangesAsync();
        return system.Id;
    }

    private sealed class SyncStateTestDbContext : DbContext, IAppDbContext
    {
        private SyncStateTestDbContext(DbContextOptions<SyncStateTestDbContext> options)
            : base(options)
        {
        }

        public static SyncStateTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<SyncStateTestDbContext>()
                .UseInMemoryDatabase($"darwin_sync_state_{Guid.NewGuid()}")
                .Options;
            return new SyncStateTestDbContext(options);
        }

        public DbSet<ExternalSystem> ExternalSystems => Set<ExternalSystem>();
        public DbSet<SyncState> SyncStates => Set<SyncState>();
        public DbSet<SyncConflict> SyncConflicts => Set<SyncConflict>();
    }
}
