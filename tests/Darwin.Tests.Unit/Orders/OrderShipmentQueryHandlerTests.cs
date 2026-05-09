using System;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Orders.DTOs;
using Darwin.Application.Orders.Queries;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.Orders;

/// <summary>
/// Unit tests for <see cref="GetOrderShipmentsPageHandler"/> and
/// <see cref="GetShipmentProviderOperationsPageHandler"/>.
/// </summary>
public sealed class GetOrderShipmentsPageHandlerTests
{
    // ─── helpers ──────────────────────────────────────────────────────────────

    private static OrderShipmentQueryTestDbContext CreateDb() =>
        OrderShipmentQueryTestDbContext.Create();

    private static GetOrderShipmentsPageHandler CreateHandler(
        OrderShipmentQueryTestDbContext db,
        DateTime? fixedNow = null)
    {
        IClock clock = fixedNow.HasValue ? new FixedClock(fixedNow.Value) : new FixedClock(DateTime.UtcNow);
        return new GetOrderShipmentsPageHandler(db, clock);
    }

    // ─── empty state ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrderShipments_Should_ReturnEmpty_WhenNoShipmentsExist()
    {
        await using var db = CreateDb();
        var orderId = Guid.NewGuid();
        db.Set<Order>().Add(new Order { Id = orderId, OrderNumber = "ORD-001", Currency = "EUR" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var (items, total) = await handler.HandleAsync(orderId, 1, 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(0);
        items.Should().BeEmpty();
    }

    // ─── soft-delete exclusion ────────────────────────────────────────────────

    [Fact]
    public async Task GetOrderShipments_Should_ExcludeSoftDeletedShipments()
    {
        await using var db = CreateDb();
        var orderId = Guid.NewGuid();
        db.Set<Order>().Add(new Order { Id = orderId, OrderNumber = "ORD-002", Currency = "EUR" });
        db.Set<Shipment>().Add(new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            Status = ShipmentStatus.Pending,
            IsDeleted = true,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var (items, total) = await handler.HandleAsync(orderId, 1, 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(0);
        items.Should().BeEmpty();
    }

    // ─── order scoping ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrderShipments_Should_ExcludeShipmentsFromOtherOrders()
    {
        await using var db = CreateDb();
        var targetOrderId = Guid.NewGuid();
        var otherOrderId = Guid.NewGuid();

        db.Set<Order>().AddRange(
            new Order { Id = targetOrderId, OrderNumber = "ORD-TARGET", Currency = "EUR" },
            new Order { Id = otherOrderId, OrderNumber = "ORD-OTHER", Currency = "EUR" });
        db.Set<Shipment>().AddRange(
            new Shipment
            {
                Id = Guid.NewGuid(),
                OrderId = targetOrderId,
                Carrier = "DHL",
                Service = "Parcel",
                Status = ShipmentStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow
            },
            new Shipment
            {
                Id = Guid.NewGuid(),
                OrderId = otherOrderId,
                Carrier = "DHL",
                Service = "Parcel",
                Status = ShipmentStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var (items, total) = await handler.HandleAsync(targetOrderId, 1, 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().ContainSingle();
    }

    // ─── page normalization ───────────────────────────────────────────────────

    [Fact]
    public async Task GetOrderShipments_Should_NormalizePage_WhenBelowOne()
    {
        await using var db = CreateDb();
        var orderId = Guid.NewGuid();
        db.Set<Order>().Add(new Order { Id = orderId, OrderNumber = "ORD-003", Currency = "EUR" });
        db.Set<Shipment>().Add(new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            Status = ShipmentStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        // page=0 should behave the same as page=1
        var (items, total) = await handler.HandleAsync(orderId, 0, 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().ContainSingle();
    }

    [Fact]
    public async Task GetOrderShipments_Should_ClampPageSize_WhenAboveMax()
    {
        await using var db = CreateDb();
        var orderId = Guid.NewGuid();
        db.Set<Order>().Add(new Order { Id = orderId, OrderNumber = "ORD-004", Currency = "EUR" });
        for (var i = 0; i < 5; i++)
        {
            db.Set<Shipment>().Add(new Shipment
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                Carrier = "DHL",
                Service = "Parcel",
                Status = ShipmentStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        // oversized pageSize must be clamped to 200; with 5 items we still get all 5
        var (items, total) = await handler.HandleAsync(orderId, 1, 9999, ct: TestContext.Current.CancellationToken);

        total.Should().Be(5);
        items.Should().HaveCount(5);
    }

    // ─── OrderNumber enrichment ───────────────────────────────────────────────

    [Fact]
    public async Task GetOrderShipments_Should_EnrichOrderNumber_FromOrder()
    {
        await using var db = CreateDb();
        var orderId = Guid.NewGuid();
        db.Set<Order>().Add(new Order { Id = orderId, OrderNumber = "ORD-ENRICH", Currency = "EUR" });
        db.Set<Shipment>().Add(new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Carrier = "DHL",
            Service = "Parcel",
            Status = ShipmentStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var (items, _) = await handler.HandleAsync(orderId, 1, 20, ct: TestContext.Current.CancellationToken);

        items.Should().ContainSingle()
            .Which.OrderNumber.Should().Be("ORD-ENRICH");
    }

    // ─── filter: Pending ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrderShipments_Filter_Pending_Should_ReturnPendingAndPackedOnly()
    {
        await using var db = CreateDb();
        var orderId = Guid.NewGuid();
        db.Set<Order>().Add(new Order { Id = orderId, OrderNumber = "ORD-PEND", Currency = "EUR" });
        db.Set<Shipment>().AddRange(
            new Shipment { Id = Guid.NewGuid(), OrderId = orderId, Carrier = "DHL", Service = "Parcel", Status = ShipmentStatus.Pending, CreatedAtUtc = DateTime.UtcNow },
            new Shipment { Id = Guid.NewGuid(), OrderId = orderId, Carrier = "DHL", Service = "Parcel", Status = ShipmentStatus.Packed, CreatedAtUtc = DateTime.UtcNow },
            new Shipment { Id = Guid.NewGuid(), OrderId = orderId, Carrier = "DHL", Service = "Parcel", Status = ShipmentStatus.Shipped, CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var (items, total) = await handler.HandleAsync(orderId, 1, 20, ShipmentQueueFilter.Pending, ct: TestContext.Current.CancellationToken);

        total.Should().Be(2);
        items.Should().HaveCount(2);
        items.Should().AllSatisfy(x => x.Status.Should().BeOneOf(ShipmentStatus.Pending, ShipmentStatus.Packed));
    }

    // ─── filter: Shipped ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrderShipments_Filter_Shipped_Should_ReturnShippedAndDeliveredOnly()
    {
        await using var db = CreateDb();
        var orderId = Guid.NewGuid();
        db.Set<Order>().Add(new Order { Id = orderId, OrderNumber = "ORD-SHIP", Currency = "EUR" });
        db.Set<Shipment>().AddRange(
            new Shipment { Id = Guid.NewGuid(), OrderId = orderId, Carrier = "DHL", Service = "Parcel", Status = ShipmentStatus.Pending, CreatedAtUtc = DateTime.UtcNow },
            new Shipment { Id = Guid.NewGuid(), OrderId = orderId, Carrier = "DHL", Service = "Parcel", Status = ShipmentStatus.Shipped, CreatedAtUtc = DateTime.UtcNow },
            new Shipment { Id = Guid.NewGuid(), OrderId = orderId, Carrier = "DHL", Service = "Parcel", Status = ShipmentStatus.Delivered, CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var (items, total) = await handler.HandleAsync(orderId, 1, 20, ShipmentQueueFilter.Shipped, ct: TestContext.Current.CancellationToken);

        total.Should().Be(2);
        items.Should().AllSatisfy(x => x.Status.Should().BeOneOf(ShipmentStatus.Shipped, ShipmentStatus.Delivered));
    }

    // ─── filter: MissingTracking ──────────────────────────────────────────────

    [Fact]
    public async Task GetOrderShipments_Filter_MissingTracking_Should_ReturnShippedWithNoTrackingNumber()
    {
        await using var db = CreateDb();
        var orderId = Guid.NewGuid();
        db.Set<Order>().Add(new Order { Id = orderId, OrderNumber = "ORD-TRACK", Currency = "EUR" });
        db.Set<Shipment>().AddRange(
            new Shipment { Id = Guid.NewGuid(), OrderId = orderId, Carrier = "DHL", Service = "Parcel", Status = ShipmentStatus.Shipped, TrackingNumber = null, CreatedAtUtc = DateTime.UtcNow },
            new Shipment { Id = Guid.NewGuid(), OrderId = orderId, Carrier = "DHL", Service = "Parcel", Status = ShipmentStatus.Shipped, TrackingNumber = "1234567890", CreatedAtUtc = DateTime.UtcNow },
            new Shipment { Id = Guid.NewGuid(), OrderId = orderId, Carrier = "DHL", Service = "Parcel", Status = ShipmentStatus.Pending, TrackingNumber = null, CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var (items, total) = await handler.HandleAsync(orderId, 1, 20, ShipmentQueueFilter.MissingTracking, ct: TestContext.Current.CancellationToken);

        // Only the Shipped one without tracking number qualifies
        total.Should().Be(1);
        items.Should().ContainSingle().Which.TrackingNumber.Should().BeNullOrEmpty();
    }

    // ─── filter: Returned ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrderShipments_Filter_Returned_Should_ReturnReturnedOnly()
    {
        await using var db = CreateDb();
        var orderId = Guid.NewGuid();
        db.Set<Order>().Add(new Order { Id = orderId, OrderNumber = "ORD-RET", Currency = "EUR" });
        db.Set<Shipment>().AddRange(
            new Shipment { Id = Guid.NewGuid(), OrderId = orderId, Carrier = "DHL", Service = "Parcel", Status = ShipmentStatus.Returned, CreatedAtUtc = DateTime.UtcNow },
            new Shipment { Id = Guid.NewGuid(), OrderId = orderId, Carrier = "DHL", Service = "Parcel", Status = ShipmentStatus.Shipped, CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var (items, total) = await handler.HandleAsync(orderId, 1, 20, ShipmentQueueFilter.Returned, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().ContainSingle().Which.Status.Should().Be(ShipmentStatus.Returned);
    }

    // ─── filter: Dhl ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrderShipments_Filter_Dhl_Should_ReturnDhlCarrierOnly()
    {
        await using var db = CreateDb();
        var orderId = Guid.NewGuid();
        db.Set<Order>().Add(new Order { Id = orderId, OrderNumber = "ORD-DHL", Currency = "EUR" });
        db.Set<Shipment>().AddRange(
            new Shipment { Id = Guid.NewGuid(), OrderId = orderId, Carrier = "DHL", Service = "Parcel", Status = ShipmentStatus.Pending, CreatedAtUtc = DateTime.UtcNow },
            new Shipment { Id = Guid.NewGuid(), OrderId = orderId, Carrier = "FedEx", Service = "Express", Status = ShipmentStatus.Pending, CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var (items, total) = await handler.HandleAsync(orderId, 1, 20, ShipmentQueueFilter.Dhl, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().ContainSingle().Which.Carrier.Should().Be("DHL");
    }

    // ─── filter: MissingService ───────────────────────────────────────────────

    [Fact]
    public async Task GetOrderShipments_Filter_MissingService_Should_ReturnShipmentsWithEmptyService()
    {
        await using var db = CreateDb();
        var orderId = Guid.NewGuid();
        db.Set<Order>().Add(new Order { Id = orderId, OrderNumber = "ORD-SVC", Currency = "EUR" });
        db.Set<Shipment>().AddRange(
            new Shipment { Id = Guid.NewGuid(), OrderId = orderId, Carrier = "DHL", Service = string.Empty, Status = ShipmentStatus.Pending, CreatedAtUtc = DateTime.UtcNow },
            new Shipment { Id = Guid.NewGuid(), OrderId = orderId, Carrier = "DHL", Service = "Parcel", Status = ShipmentStatus.Pending, CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var (items, total) = await handler.HandleAsync(orderId, 1, 20, ShipmentQueueFilter.MissingService, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().ContainSingle().Which.Service.Should().BeNullOrEmpty();
    }

    // ─── filter: AwaitingHandoff ──────────────────────────────────────────────

    [Fact]
    public async Task GetOrderShipments_Filter_AwaitingHandoff_Should_FilterByConfiguredThreshold()
    {
        var fixedNow = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        await using var db = CreateDb();
        var orderId = Guid.NewGuid();
        db.Set<Order>().Add(new Order { Id = orderId, OrderNumber = "ORD-HO", Currency = "EUR" });
        db.Set<Shipment>().AddRange(
            // Old Pending — should be returned (created > 24 h ago)
            new Shipment
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                Carrier = "DHL",
                Service = "Parcel",
                Status = ShipmentStatus.Pending,
                CreatedAtUtc = fixedNow.AddHours(-30)
            },
            // Fresh Pending — not yet past threshold
            new Shipment
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                Carrier = "DHL",
                Service = "Parcel",
                Status = ShipmentStatus.Pending,
                CreatedAtUtc = fixedNow.AddHours(-4)
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db, fixedNow);
        var (items, total) = await handler.HandleAsync(
            orderId, 1, 20,
            ShipmentQueueFilter.AwaitingHandoff,
            attentionDelayHours: 24,
            ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().ContainSingle().Which.AwaitingHandoff.Should().BeTrue();
    }

    // ─── filter: TrackingOverdue ──────────────────────────────────────────────

    [Fact]
    public async Task GetOrderShipments_Filter_TrackingOverdue_Should_FilterByTrackingGrace()
    {
        var fixedNow = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        await using var db = CreateDb();
        var orderId = Guid.NewGuid();
        db.Set<Order>().Add(new Order { Id = orderId, OrderNumber = "ORD-TRK", Currency = "EUR" });
        db.Set<Shipment>().AddRange(
            // Old DHL Shipped, no tracking — past grace period
            new Shipment
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                Carrier = "DHL",
                Service = "Parcel",
                Status = ShipmentStatus.Shipped,
                TrackingNumber = null,
                ShippedAtUtc = fixedNow.AddHours(-15),
                CreatedAtUtc = fixedNow.AddHours(-16)
            },
            // Fresh DHL Shipped, no tracking — still within grace period
            new Shipment
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                Carrier = "DHL",
                Service = "Parcel",
                Status = ShipmentStatus.Shipped,
                TrackingNumber = null,
                ShippedAtUtc = fixedNow.AddHours(-5),
                CreatedAtUtc = fixedNow.AddHours(-6)
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db, fixedNow);
        var (items, total) = await handler.HandleAsync(
            orderId, 1, 20,
            ShipmentQueueFilter.TrackingOverdue,
            trackingGraceHours: 12,
            ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().ContainSingle().Which.TrackingOverdue.Should().BeTrue();
    }
}

public sealed class GetShipmentProviderOperationsPageHandlerTests
{
    // ─── helpers ──────────────────────────────────────────────────────────────

    private static OrderShipmentQueryTestDbContext CreateDb() =>
        OrderShipmentQueryTestDbContext.Create();

    private static GetShipmentProviderOperationsPageHandler CreateHandler(
        OrderShipmentQueryTestDbContext db,
        DateTime? fixedNow = null)
    {
        IClock clock = new FixedClock(fixedNow ?? new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc));
        return new GetShipmentProviderOperationsPageHandler(db, clock);
    }

    // ─── empty state ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetShipmentProviderOperationsPage_Should_ReturnEmpty_WhenNoOperations()
    {
        await using var db = CreateDb();
        var handler = CreateHandler(db);
        var (items, total, summary, providers, operationTypes) =
            await handler.HandleAsync(1, 20, new ShipmentProviderOperationFilterDto(), TestContext.Current.CancellationToken);

        total.Should().Be(0);
        items.Should().BeEmpty();
        summary.TotalCount.Should().Be(0);
        providers.Should().BeEmpty();
        operationTypes.Should().BeEmpty();
    }

    // ─── summary counts ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetShipmentProviderOperationsPage_Should_PopulateSummary_WithCorrectCounts()
    {
        var fixedNow = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        await using var db = CreateDb();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        db.Set<Order>().Add(new Order { Id = orderId, OrderNumber = "ORD-SUM", Currency = "EUR" });
        db.Set<Shipment>().Add(new Shipment { Id = shipmentId, OrderId = orderId, Carrier = "DHL", Service = "Parcel", Status = ShipmentStatus.Pending, CreatedAtUtc = fixedNow });

        db.Set<ShipmentProviderOperation>().AddRange(
            new ShipmentProviderOperation { Id = Guid.NewGuid(), ShipmentId = shipmentId, Provider = "DHL", OperationType = "CreateShipment", Status = "Pending", CreatedAtUtc = fixedNow },
            new ShipmentProviderOperation { Id = Guid.NewGuid(), ShipmentId = shipmentId, Provider = "DHL", OperationType = "CreateShipment", Status = "Failed", CreatedAtUtc = fixedNow },
            new ShipmentProviderOperation { Id = Guid.NewGuid(), ShipmentId = shipmentId, Provider = "DHL", OperationType = "CreateLabel", Status = "Processed", CreatedAtUtc = fixedNow },
            new ShipmentProviderOperation { Id = Guid.NewGuid(), ShipmentId = shipmentId, Provider = "DHL", OperationType = "CreateLabel", Status = "Succeeded", CreatedAtUtc = fixedNow },
            // Stale Pending: created more than 30 minutes ago
            new ShipmentProviderOperation { Id = Guid.NewGuid(), ShipmentId = shipmentId, Provider = "DHL", OperationType = "CreateShipment", Status = "Pending", CreatedAtUtc = fixedNow.AddMinutes(-45) },
            // Deleted / cancelled
            new ShipmentProviderOperation { Id = Guid.NewGuid(), ShipmentId = shipmentId, Provider = "DHL", OperationType = "CreateShipment", Status = "Pending", CreatedAtUtc = fixedNow, IsDeleted = true });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db, fixedNow);
        var (_, _, summary, _, _) =
            await handler.HandleAsync(1, 20, new ShipmentProviderOperationFilterDto(), TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(5);        // 5 non-deleted
        summary.PendingCount.Should().Be(2);      // Pending (fresh) + Pending (stale)
        summary.FailedCount.Should().Be(1);
        summary.ProcessedCount.Should().Be(2);    // Processed + Succeeded
        summary.StalePendingCount.Should().Be(1); // only the one older than 30 min
        summary.CancelledCount.Should().Be(1);    // IsDeleted=true
    }

    // ─── soft-delete exclusion from active queries ────────────────────────────

    [Fact]
    public async Task GetShipmentProviderOperationsPage_Should_ExcludeDeleted_FromActiveItems()
    {
        var fixedNow = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        await using var db = CreateDb();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        db.Set<Order>().Add(new Order { Id = orderId, OrderNumber = "ORD-DEL", Currency = "EUR" });
        db.Set<Shipment>().Add(new Shipment { Id = shipmentId, OrderId = orderId, Carrier = "DHL", Service = "Parcel", Status = ShipmentStatus.Pending, CreatedAtUtc = fixedNow });

        db.Set<ShipmentProviderOperation>().AddRange(
            new ShipmentProviderOperation { Id = Guid.NewGuid(), ShipmentId = shipmentId, Provider = "DHL", OperationType = "CreateShipment", Status = "Pending", CreatedAtUtc = fixedNow },
            new ShipmentProviderOperation { Id = Guid.NewGuid(), ShipmentId = shipmentId, Provider = "DHL", OperationType = "CreateShipment", Status = "Pending", CreatedAtUtc = fixedNow, IsDeleted = true });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db, fixedNow);
        var (items, total, _, _, _) =
            await handler.HandleAsync(1, 20, new ShipmentProviderOperationFilterDto(), TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().ContainSingle();
    }

    // ─── filter: Provider ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetShipmentProviderOperationsPage_Filter_ByProvider_Should_ReturnMatchingOnly()
    {
        var fixedNow = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        await using var db = CreateDb();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        db.Set<Order>().Add(new Order { Id = orderId, OrderNumber = "ORD-PROV", Currency = "EUR" });
        db.Set<Shipment>().Add(new Shipment { Id = shipmentId, OrderId = orderId, Carrier = "DHL", Service = "Parcel", Status = ShipmentStatus.Pending, CreatedAtUtc = fixedNow });

        db.Set<ShipmentProviderOperation>().AddRange(
            new ShipmentProviderOperation { Id = Guid.NewGuid(), ShipmentId = shipmentId, Provider = "DHL", OperationType = "CreateShipment", Status = "Pending", CreatedAtUtc = fixedNow },
            new ShipmentProviderOperation { Id = Guid.NewGuid(), ShipmentId = shipmentId, Provider = "FedEx", OperationType = "CreateShipment", Status = "Pending", CreatedAtUtc = fixedNow });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db, fixedNow);
        var (items, total, _, _, _) =
            await handler.HandleAsync(1, 20, new ShipmentProviderOperationFilterDto { Provider = "DHL" }, TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().ContainSingle().Which.Provider.Should().Be("DHL");
    }

    // ─── filter: OperationType ────────────────────────────────────────────────

    [Fact]
    public async Task GetShipmentProviderOperationsPage_Filter_ByOperationType_Should_ReturnMatchingOnly()
    {
        var fixedNow = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        await using var db = CreateDb();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        db.Set<Order>().Add(new Order { Id = orderId, OrderNumber = "ORD-OT", Currency = "EUR" });
        db.Set<Shipment>().Add(new Shipment { Id = shipmentId, OrderId = orderId, Carrier = "DHL", Service = "Parcel", Status = ShipmentStatus.Pending, CreatedAtUtc = fixedNow });

        db.Set<ShipmentProviderOperation>().AddRange(
            new ShipmentProviderOperation { Id = Guid.NewGuid(), ShipmentId = shipmentId, Provider = "DHL", OperationType = "CreateShipment", Status = "Pending", CreatedAtUtc = fixedNow },
            new ShipmentProviderOperation { Id = Guid.NewGuid(), ShipmentId = shipmentId, Provider = "DHL", OperationType = "CreateLabel", Status = "Pending", CreatedAtUtc = fixedNow });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db, fixedNow);
        var (items, total, _, _, _) =
            await handler.HandleAsync(1, 20, new ShipmentProviderOperationFilterDto { OperationType = "CreateLabel" }, TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().ContainSingle().Which.OperationType.Should().Be("CreateLabel");
    }

    // ─── filter: Status ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetShipmentProviderOperationsPage_Filter_ByStatus_Should_ReturnMatchingOnly()
    {
        var fixedNow = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        await using var db = CreateDb();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        db.Set<Order>().Add(new Order { Id = orderId, OrderNumber = "ORD-ST", Currency = "EUR" });
        db.Set<Shipment>().Add(new Shipment { Id = shipmentId, OrderId = orderId, Carrier = "DHL", Service = "Parcel", Status = ShipmentStatus.Pending, CreatedAtUtc = fixedNow });

        db.Set<ShipmentProviderOperation>().AddRange(
            new ShipmentProviderOperation { Id = Guid.NewGuid(), ShipmentId = shipmentId, Provider = "DHL", OperationType = "CreateShipment", Status = "Pending", CreatedAtUtc = fixedNow },
            new ShipmentProviderOperation { Id = Guid.NewGuid(), ShipmentId = shipmentId, Provider = "DHL", OperationType = "CreateShipment", Status = "Failed", CreatedAtUtc = fixedNow });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db, fixedNow);
        var (items, total, _, _, _) =
            await handler.HandleAsync(1, 20, new ShipmentProviderOperationFilterDto { Status = "Failed" }, TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().ContainSingle().Which.Status.Should().Be("Failed");
    }

    [Fact]
    public async Task GetShipmentProviderOperationsPage_Filter_Processed_Should_IncludeSucceeded()
    {
        var fixedNow = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        await using var db = CreateDb();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        db.Set<Order>().Add(new Order { Id = orderId, OrderNumber = "ORD-PROC", Currency = "EUR" });
        db.Set<Shipment>().Add(new Shipment { Id = shipmentId, OrderId = orderId, Carrier = "DHL", Service = "Parcel", Status = ShipmentStatus.Pending, CreatedAtUtc = fixedNow });

        db.Set<ShipmentProviderOperation>().AddRange(
            new ShipmentProviderOperation { Id = Guid.NewGuid(), ShipmentId = shipmentId, Provider = "DHL", OperationType = "CreateShipment", Status = "Processed", CreatedAtUtc = fixedNow },
            new ShipmentProviderOperation { Id = Guid.NewGuid(), ShipmentId = shipmentId, Provider = "DHL", OperationType = "CreateLabel", Status = "Succeeded", CreatedAtUtc = fixedNow },
            new ShipmentProviderOperation { Id = Guid.NewGuid(), ShipmentId = shipmentId, Provider = "DHL", OperationType = "CreateShipment", Status = "Pending", CreatedAtUtc = fixedNow });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db, fixedNow);
        var (items, total, _, _, _) =
            await handler.HandleAsync(1, 20, new ShipmentProviderOperationFilterDto { Status = "Processed" }, TestContext.Current.CancellationToken);

        // Both "Processed" and "Succeeded" should be returned when filtering by "Processed"
        total.Should().Be(2);
        items.Should().HaveCount(2);
        // After normalization both should be reported as "Processed"
        items.Should().AllSatisfy(x => x.Status.Should().Be("Processed"));
    }

    // ─── filter: FailedOnly ───────────────────────────────────────────────────

    [Fact]
    public async Task GetShipmentProviderOperationsPage_Filter_FailedOnly_Should_ReturnOnlyFailed()
    {
        var fixedNow = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        await using var db = CreateDb();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        db.Set<Order>().Add(new Order { Id = orderId, OrderNumber = "ORD-FAIL", Currency = "EUR" });
        db.Set<Shipment>().Add(new Shipment { Id = shipmentId, OrderId = orderId, Carrier = "DHL", Service = "Parcel", Status = ShipmentStatus.Pending, CreatedAtUtc = fixedNow });

        db.Set<ShipmentProviderOperation>().AddRange(
            new ShipmentProviderOperation { Id = Guid.NewGuid(), ShipmentId = shipmentId, Provider = "DHL", OperationType = "CreateShipment", Status = "Pending", CreatedAtUtc = fixedNow },
            new ShipmentProviderOperation { Id = Guid.NewGuid(), ShipmentId = shipmentId, Provider = "DHL", OperationType = "CreateShipment", Status = "Failed", CreatedAtUtc = fixedNow, FailureReason = "Timeout" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db, fixedNow);
        var (items, total, _, _, _) =
            await handler.HandleAsync(1, 20, new ShipmentProviderOperationFilterDto { FailedOnly = true }, TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().ContainSingle().Which.Status.Should().Be("Failed");
    }

    // ─── filter: StalePendingOnly ─────────────────────────────────────────────

    [Fact]
    public async Task GetShipmentProviderOperationsPage_Filter_StalePendingOnly_Should_FilterByAge()
    {
        var fixedNow = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        await using var db = CreateDb();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        db.Set<Order>().Add(new Order { Id = orderId, OrderNumber = "ORD-STALE", Currency = "EUR" });
        db.Set<Shipment>().Add(new Shipment { Id = shipmentId, OrderId = orderId, Carrier = "DHL", Service = "Parcel", Status = ShipmentStatus.Pending, CreatedAtUtc = fixedNow });

        db.Set<ShipmentProviderOperation>().AddRange(
            // Stale (> 30 minutes)
            new ShipmentProviderOperation { Id = Guid.NewGuid(), ShipmentId = shipmentId, Provider = "DHL", OperationType = "CreateShipment", Status = "Pending", CreatedAtUtc = fixedNow.AddMinutes(-45) },
            // Fresh (< 30 minutes)
            new ShipmentProviderOperation { Id = Guid.NewGuid(), ShipmentId = shipmentId, Provider = "DHL", OperationType = "CreateShipment", Status = "Pending", CreatedAtUtc = fixedNow.AddMinutes(-10) });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db, fixedNow);
        var (items, total, _, _, _) =
            await handler.HandleAsync(1, 20, new ShipmentProviderOperationFilterDto { StalePendingOnly = true }, TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().ContainSingle().Which.IsStalePending.Should().BeTrue();
    }

    // ─── providers / operation types lists ───────────────────────────────────

    [Fact]
    public async Task GetShipmentProviderOperationsPage_Should_PopulateProvidersList_Distinct_Ordered()
    {
        var fixedNow = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        await using var db = CreateDb();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        db.Set<Order>().Add(new Order { Id = orderId, OrderNumber = "ORD-PLIST", Currency = "EUR" });
        db.Set<Shipment>().Add(new Shipment { Id = shipmentId, OrderId = orderId, Carrier = "DHL", Service = "Parcel", Status = ShipmentStatus.Pending, CreatedAtUtc = fixedNow });

        db.Set<ShipmentProviderOperation>().AddRange(
            new ShipmentProviderOperation { Id = Guid.NewGuid(), ShipmentId = shipmentId, Provider = "DHL", OperationType = "CreateShipment", Status = "Pending", CreatedAtUtc = fixedNow },
            new ShipmentProviderOperation { Id = Guid.NewGuid(), ShipmentId = shipmentId, Provider = "DHL", OperationType = "CreateLabel", Status = "Pending", CreatedAtUtc = fixedNow },
            new ShipmentProviderOperation { Id = Guid.NewGuid(), ShipmentId = shipmentId, Provider = "FedEx", OperationType = "CreateShipment", Status = "Pending", CreatedAtUtc = fixedNow });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db, fixedNow);
        var (_, _, _, providers, _) =
            await handler.HandleAsync(1, 20, new ShipmentProviderOperationFilterDto(), TestContext.Current.CancellationToken);

        providers.Should().BeEquivalentTo(new[] { "DHL", "FedEx" }, options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task GetShipmentProviderOperationsPage_Should_PopulateOperationTypesList_Distinct_Ordered()
    {
        var fixedNow = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        await using var db = CreateDb();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        db.Set<Order>().Add(new Order { Id = orderId, OrderNumber = "ORD-OTLIST", Currency = "EUR" });
        db.Set<Shipment>().Add(new Shipment { Id = shipmentId, OrderId = orderId, Carrier = "DHL", Service = "Parcel", Status = ShipmentStatus.Pending, CreatedAtUtc = fixedNow });

        db.Set<ShipmentProviderOperation>().AddRange(
            new ShipmentProviderOperation { Id = Guid.NewGuid(), ShipmentId = shipmentId, Provider = "DHL", OperationType = "CreateShipment", Status = "Pending", CreatedAtUtc = fixedNow },
            new ShipmentProviderOperation { Id = Guid.NewGuid(), ShipmentId = shipmentId, Provider = "DHL", OperationType = "CreateLabel", Status = "Pending", CreatedAtUtc = fixedNow },
            new ShipmentProviderOperation { Id = Guid.NewGuid(), ShipmentId = shipmentId, Provider = "DHL", OperationType = "CreateShipment", Status = "Failed", CreatedAtUtc = fixedNow });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db, fixedNow);
        var (_, _, _, _, operationTypes) =
            await handler.HandleAsync(1, 20, new ShipmentProviderOperationFilterDto(), TestContext.Current.CancellationToken);

        operationTypes.Should().BeEquivalentTo(new[] { "CreateLabel", "CreateShipment" }, options => options.WithStrictOrdering());
    }

    // ─── OrderNumber enrichment via Shipment → Order ──────────────────────────

    [Fact]
    public async Task GetShipmentProviderOperationsPage_Should_EnrichOrderNumber_ViaShipment()
    {
        var fixedNow = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        await using var db = CreateDb();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        db.Set<Order>().Add(new Order { Id = orderId, OrderNumber = "ORD-ENRICH-OP", Currency = "EUR" });
        db.Set<Shipment>().Add(new Shipment { Id = shipmentId, OrderId = orderId, Carrier = "DHL", Service = "Parcel", Status = ShipmentStatus.Pending, CreatedAtUtc = fixedNow });
        db.Set<ShipmentProviderOperation>().Add(
            new ShipmentProviderOperation { Id = Guid.NewGuid(), ShipmentId = shipmentId, Provider = "DHL", OperationType = "CreateShipment", Status = "Pending", CreatedAtUtc = fixedNow });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db, fixedNow);
        var (items, _, _, _, _) =
            await handler.HandleAsync(1, 20, new ShipmentProviderOperationFilterDto(), TestContext.Current.CancellationToken);

        items.Should().ContainSingle().Which.OrderNumber.Should().Be("ORD-ENRICH-OP");
    }
}

// ─── shared test infrastructure ───────────────────────────────────────────────

internal sealed class FixedClock : IClock
{
    public FixedClock(DateTime utcNow) => UtcNow = utcNow;
    public DateTime UtcNow { get; }
}

internal sealed class OrderShipmentQueryTestDbContext : DbContext, IAppDbContext
{
    private OrderShipmentQueryTestDbContext(DbContextOptions<OrderShipmentQueryTestDbContext> options)
        : base(options) { }

    public new DbSet<T> Set<T>() where T : class => base.Set<T>();

    public static OrderShipmentQueryTestDbContext Create()
    {
        var options = new DbContextOptionsBuilder<OrderShipmentQueryTestDbContext>()
            .UseInMemoryDatabase($"darwin_order_shipment_query_{Guid.NewGuid()}")
            .Options;
        return new OrderShipmentQueryTestDbContext(options);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.OrderNumber).HasMaxLength(64).IsRequired();
            b.Property(x => x.Currency).IsRequired();
            b.Property(x => x.IsDeleted);
            b.Property(x => x.RowVersion).IsRequired();
        });

        modelBuilder.Entity<Shipment>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Carrier).IsRequired();
            b.Property(x => x.Service).IsRequired();
            b.Property(x => x.IsDeleted);
            b.Property(x => x.RowVersion).IsRequired();
        });

        modelBuilder.Entity<ShipmentCarrierEvent>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.ShipmentId).IsRequired();
            b.Property(x => x.Carrier).IsRequired();
            b.Property(x => x.ProviderShipmentReference).IsRequired();
            b.Property(x => x.CarrierEventKey).IsRequired();
        });

        modelBuilder.Entity<ShipmentProviderOperation>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Provider).IsRequired();
            b.Property(x => x.OperationType).IsRequired();
            b.Property(x => x.Status).IsRequired();
            b.Property(x => x.IsDeleted);
            b.Property(x => x.RowVersion).IsRequired();
        });

        modelBuilder.Entity<Payment>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Currency).IsRequired();
            b.Property(x => x.Provider).IsRequired();
            b.Property(x => x.IsDeleted);
            b.Property(x => x.RowVersion).IsRequired();
        });
    }
}
