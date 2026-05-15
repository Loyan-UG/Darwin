using System;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Orders.Commands;
using Darwin.Application.Orders.DTOs;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.Orders;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Moq;

namespace Darwin.Tests.Unit.Orders;

/// <summary>
/// Unit tests for <see cref="ResolveShipmentCarrierExceptionHandler"/> and
/// <see cref="UpdateShipmentProviderOperationHandler"/>.
/// </summary>
public sealed class ShipmentCommandHandlerTests
{
    // ─── shared helpers ────────────────────────────────────────────────────────

    private static IStringLocalizer<ValidationResource> CreateLocalizer()
    {
        var mock = new Mock<IStringLocalizer<ValidationResource>>();
        mock.Setup(l => l[It.IsAny<string>()])
            .Returns<string>(name => new LocalizedString(name, name));
        mock.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns<string, object[]>((name, _) => new LocalizedString(name, name));
        return mock.Object;
    }

    private static ShipmentCommandTestDbContext CreateDb() => ShipmentCommandTestDbContext.Create();

    private static readonly DateTime FixedNow = new(2026, 5, 15, 10, 0, 0, DateTimeKind.Utc);

    private static IClock CreateClock() => new FixedClock(FixedNow);

    private sealed class FixedClock : IClock
    {
        private readonly DateTime _utcNow;
        public FixedClock(DateTime utcNow) { _utcNow = utcNow; }
        public DateTime UtcNow => _utcNow;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ResolveShipmentCarrierExceptionHandler
    // ─────────────────────────────────────────────────────────────────────────

    private static ResolveShipmentCarrierExceptionHandler CreateResolveHandler(ShipmentCommandTestDbContext db)
        => new(db, CreateClock(), CreateLocalizer());

    [Fact]
    public async Task ResolveShipmentCarrierException_Should_Fail_WhenShipmentIdIsEmpty()
    {
        await using var db = CreateDb();
        var handler = CreateResolveHandler(db);

        var result = await handler.HandleAsync(new ResolveShipmentCarrierExceptionDto
        {
            ShipmentId = Guid.Empty,
            RowVersion = new byte[] { 1, 2, 3 },
            ResolutionNote = "Resolved"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("empty ShipmentId is invalid");
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ResolveShipmentCarrierException_Should_Fail_WhenRowVersionIsEmpty()
    {
        await using var db = CreateDb();
        var handler = CreateResolveHandler(db);

        var result = await handler.HandleAsync(new ResolveShipmentCarrierExceptionDto
        {
            ShipmentId = Guid.NewGuid(),
            RowVersion = Array.Empty<byte>(),
            ResolutionNote = "Resolved"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("empty RowVersion must be rejected");
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ResolveShipmentCarrierException_Should_Fail_WhenResolutionNoteIsEmpty()
    {
        await using var db = CreateDb();
        var handler = CreateResolveHandler(db);

        var result = await handler.HandleAsync(new ResolveShipmentCarrierExceptionDto
        {
            ShipmentId = Guid.NewGuid(),
            RowVersion = new byte[] { 1, 2, 3 },
            ResolutionNote = ""
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("empty ResolutionNote must be rejected");
    }

    [Fact]
    public async Task ResolveShipmentCarrierException_Should_Fail_WhenResolutionNoteIsWhitespace()
    {
        await using var db = CreateDb();
        var handler = CreateResolveHandler(db);

        var result = await handler.HandleAsync(new ResolveShipmentCarrierExceptionDto
        {
            ShipmentId = Guid.NewGuid(),
            RowVersion = new byte[] { 1, 2, 3 },
            ResolutionNote = "   "
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("whitespace-only ResolutionNote must be rejected");
    }

    [Fact]
    public async Task ResolveShipmentCarrierException_Should_Fail_WhenShipmentNotFound()
    {
        await using var db = CreateDb();
        var handler = CreateResolveHandler(db);

        var result = await handler.HandleAsync(new ResolveShipmentCarrierExceptionDto
        {
            ShipmentId = Guid.NewGuid(),
            RowVersion = new byte[] { 1, 2, 3 },
            ResolutionNote = "Carrier confirmed delivery"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("a non-existent shipment should fail");
    }

    [Fact]
    public async Task ResolveShipmentCarrierException_Should_Fail_WhenRowVersionIsStale()
    {
        await using var db = CreateDb();
        var shipmentId = Guid.NewGuid();
        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = Guid.NewGuid(),
            Carrier = "DHL",
            Service = "Parcel",
            RowVersion = new byte[] { 1, 2, 3 }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateResolveHandler(db);
        var result = await handler.HandleAsync(new ResolveShipmentCarrierExceptionDto
        {
            ShipmentId = shipmentId,
            RowVersion = new byte[] { 9, 9, 9 }, // stale
            ResolutionNote = "Carrier confirmed delivery"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("stale RowVersion must trigger a concurrency conflict");
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ResolveShipmentCarrierException_Should_Succeed_WhenValid()
    {
        await using var db = CreateDb();
        var shipmentId = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3 };
        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = Guid.NewGuid(),
            Carrier = "DHL",
            Service = "Parcel",
            RowVersion = rowVersion
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateResolveHandler(db);
        var result = await handler.HandleAsync(new ResolveShipmentCarrierExceptionDto
        {
            ShipmentId = shipmentId,
            RowVersion = rowVersion,
            ResolutionNote = "Carrier confirmed delivery"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue("a matching RowVersion and valid note should succeed");
        var shipment = await db.Set<Shipment>().FindAsync([shipmentId], TestContext.Current.CancellationToken);
        shipment!.LastCarrierEventKey.Should().Be("shipment.exception_resolved");
    }

    [Fact]
    public async Task ResolveShipmentCarrierException_Should_Fail_WhenShipmentIsSoftDeleted()
    {
        await using var db = CreateDb();
        var shipmentId = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3 };
        db.Set<Shipment>().Add(new Shipment
        {
            Id = shipmentId,
            OrderId = Guid.NewGuid(),
            Carrier = "DHL",
            Service = "Parcel",
            IsDeleted = true,
            RowVersion = rowVersion
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateResolveHandler(db);
        var result = await handler.HandleAsync(new ResolveShipmentCarrierExceptionDto
        {
            ShipmentId = shipmentId,
            RowVersion = rowVersion,
            ResolutionNote = "Carrier confirmed delivery"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("soft-deleted shipments must not be resolved");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UpdateShipmentProviderOperationHandler
    // ─────────────────────────────────────────────────────────────────────────

    private static UpdateShipmentProviderOperationHandler CreateUpdateOpHandler(ShipmentCommandTestDbContext db)
        => new(db, CreateClock(), CreateLocalizer());

    private static ShipmentProviderOperation BuildOperation(
        string status = "Pending",
        byte[]? rowVersion = null,
        bool isDeleted = false,
        string provider = "DHL",
        string operationType = "CreateShipment") =>
        new()
        {
            Id = Guid.NewGuid(),
            ShipmentId = Guid.NewGuid(),
            Provider = provider,
            OperationType = operationType,
            Status = status,
            IsDeleted = isDeleted,
            RowVersion = rowVersion ?? new byte[] { 1, 2, 3 }
        };

    [Fact]
    public async Task UpdateShipmentProviderOperation_Should_Fail_WhenIdIsEmpty()
    {
        await using var db = CreateDb();
        var handler = CreateUpdateOpHandler(db);

        var result = await handler.HandleAsync(new UpdateShipmentProviderOperationDto
        {
            Id = Guid.Empty,
            RowVersion = new byte[] { 1, 2, 3 },
            Action = "MarkProcessed"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("empty Id is invalid");
    }

    [Fact]
    public async Task UpdateShipmentProviderOperation_Should_Fail_WhenRowVersionIsEmpty()
    {
        await using var db = CreateDb();
        var handler = CreateUpdateOpHandler(db);

        var result = await handler.HandleAsync(new UpdateShipmentProviderOperationDto
        {
            Id = Guid.NewGuid(),
            RowVersion = Array.Empty<byte>(),
            Action = "MarkProcessed"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("empty RowVersion must be rejected");
    }

    [Fact]
    public async Task UpdateShipmentProviderOperation_Should_Fail_WhenOperationNotFound()
    {
        await using var db = CreateDb();
        var handler = CreateUpdateOpHandler(db);

        var result = await handler.HandleAsync(new UpdateShipmentProviderOperationDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            Action = "MarkProcessed"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("non-existent operation should fail");
    }

    [Fact]
    public async Task UpdateShipmentProviderOperation_Should_Fail_WhenRowVersionIsStale()
    {
        await using var db = CreateDb();
        var operation = BuildOperation(rowVersion: new byte[] { 1, 2, 3 });
        db.Set<ShipmentProviderOperation>().Add(operation);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateUpdateOpHandler(db);
        var result = await handler.HandleAsync(new UpdateShipmentProviderOperationDto
        {
            Id = operation.Id,
            RowVersion = new byte[] { 9, 9, 9 }, // stale
            Action = "MarkProcessed"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("stale RowVersion must trigger a concurrency conflict");
    }

    [Fact]
    public async Task UpdateShipmentProviderOperation_Should_ApplyMarkProcessed()
    {
        await using var db = CreateDb();
        var rowVersion = new byte[] { 1, 2, 3 };
        var operation = BuildOperation(status: "Pending", rowVersion: rowVersion);
        db.Set<ShipmentProviderOperation>().Add(operation);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateUpdateOpHandler(db);
        var result = await handler.HandleAsync(new UpdateShipmentProviderOperationDto
        {
            Id = operation.Id,
            RowVersion = rowVersion,
            Action = "MarkProcessed"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var updated = await db.Set<ShipmentProviderOperation>().FindAsync([operation.Id], TestContext.Current.CancellationToken);
        updated!.Status.Should().Be("Processed");
        updated.ProcessedAtUtc.Should().Be(FixedNow);
        updated.FailureReason.Should().BeNull();
    }

    [Fact]
    public async Task UpdateShipmentProviderOperation_Should_ApplyMarkFailed()
    {
        await using var db = CreateDb();
        var rowVersion = new byte[] { 5, 6, 7 };
        var operation = BuildOperation(status: "Pending", rowVersion: rowVersion);
        db.Set<ShipmentProviderOperation>().Add(operation);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateUpdateOpHandler(db);
        var result = await handler.HandleAsync(new UpdateShipmentProviderOperationDto
        {
            Id = operation.Id,
            RowVersion = rowVersion,
            Action = "MarkFailed",
            FailureReason = "Provider timeout"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var updated = await db.Set<ShipmentProviderOperation>().FindAsync([operation.Id], TestContext.Current.CancellationToken);
        updated!.Status.Should().Be("Failed");
        updated.FailureReason.Should().Be("Provider timeout");
        updated.LastAttemptAtUtc.Should().Be(FixedNow);
        updated.ProcessedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task UpdateShipmentProviderOperation_Should_UseDefaultFailureReason_WhenReasonIsEmpty()
    {
        await using var db = CreateDb();
        var rowVersion = new byte[] { 5, 6, 7 };
        var operation = BuildOperation(status: "Pending", rowVersion: rowVersion);
        db.Set<ShipmentProviderOperation>().Add(operation);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateUpdateOpHandler(db);
        var result = await handler.HandleAsync(new UpdateShipmentProviderOperationDto
        {
            Id = operation.Id,
            RowVersion = rowVersion,
            Action = "MarkFailed",
            FailureReason = ""
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var updated = await db.Set<ShipmentProviderOperation>().FindAsync([operation.Id], TestContext.Current.CancellationToken);
        updated!.FailureReason.Should().Be("Marked failed by WebAdmin operator.");
    }

    [Fact]
    public async Task UpdateShipmentProviderOperation_Should_ApplyCancel()
    {
        await using var db = CreateDb();
        var rowVersion = new byte[] { 3, 3, 3 };
        var operation = BuildOperation(status: "Pending", rowVersion: rowVersion);
        db.Set<ShipmentProviderOperation>().Add(operation);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateUpdateOpHandler(db);
        var result = await handler.HandleAsync(new UpdateShipmentProviderOperationDto
        {
            Id = operation.Id,
            RowVersion = rowVersion,
            Action = "Cancel"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var updated = await db.Set<ShipmentProviderOperation>().FindAsync([operation.Id], TestContext.Current.CancellationToken);
        updated!.Status.Should().Be("Cancelled");
        updated.IsDeleted.Should().BeTrue();
        updated.FailureReason.Should().Be("Cancelled by WebAdmin operator.");
    }

    [Fact]
    public async Task UpdateShipmentProviderOperation_Should_Fail_WhenActionIsUnsupported()
    {
        await using var db = CreateDb();
        var rowVersion = new byte[] { 1, 2, 3 };
        var operation = BuildOperation(status: "Pending", rowVersion: rowVersion);
        db.Set<ShipmentProviderOperation>().Add(operation);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateUpdateOpHandler(db);
        var result = await handler.HandleAsync(new UpdateShipmentProviderOperationDto
        {
            Id = operation.Id,
            RowVersion = rowVersion,
            Action = "DoSomethingUnsupported"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("unsupported actions must be rejected");
    }

    [Fact]
    public async Task UpdateShipmentProviderOperation_Should_BeCaseInsensitive_ForAction()
    {
        await using var db = CreateDb();
        var rowVersion = new byte[] { 1, 2, 3 };
        var operation = BuildOperation(status: "Pending", rowVersion: rowVersion);
        db.Set<ShipmentProviderOperation>().Add(operation);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateUpdateOpHandler(db);
        var result = await handler.HandleAsync(new UpdateShipmentProviderOperationDto
        {
            Id = operation.Id,
            RowVersion = rowVersion,
            Action = "markprocessed" // lowercase
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue("action matching should be case-insensitive");
        var updated = await db.Set<ShipmentProviderOperation>().FindAsync([operation.Id], TestContext.Current.CancellationToken);
        updated!.Status.Should().Be("Processed");
    }

    // ─── Test DbContext ────────────────────────────────────────────────────────

    private sealed class ShipmentCommandTestDbContext : DbContext, IAppDbContext
    {
        private ShipmentCommandTestDbContext(DbContextOptions<ShipmentCommandTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static ShipmentCommandTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<ShipmentCommandTestDbContext>()
                .UseInMemoryDatabase($"darwin_shipment_cmd_tests_{Guid.NewGuid()}")
                .Options;
            return new ShipmentCommandTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Shipment>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Carrier).IsRequired();
                builder.Property(x => x.Service).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
                builder.Ignore(x => x.Lines);
                builder.Ignore(x => x.CarrierEvents);
            });

            modelBuilder.Entity<ShipmentCarrierEvent>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.ShipmentId).IsRequired();
                builder.Property(x => x.Carrier).IsRequired();
                builder.Property(x => x.ProviderShipmentReference).IsRequired();
                builder.Property(x => x.CarrierEventKey).IsRequired();
            });

            modelBuilder.Entity<ShipmentProviderOperation>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Provider).IsRequired();
                builder.Property(x => x.OperationType).IsRequired();
                builder.Property(x => x.Status).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });
        }
    }
}
