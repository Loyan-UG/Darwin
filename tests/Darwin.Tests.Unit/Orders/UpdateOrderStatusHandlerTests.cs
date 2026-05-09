using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Inventory.Commands;
using Darwin.Application.Inventory.Validators;
using Darwin.Application.Orders.Commands;
using Darwin.Application.Orders.DTOs;
using Darwin.Application.Orders.Validators;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.Inventory;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Moq;

namespace Darwin.Tests.Unit.Orders;

/// <summary>
/// Unit tests for <see cref="UpdateOrderStatusHandler"/>:
/// guards (validation, concurrency, state-machine),
/// evidence checks (payment/shipment/refund requirements),
/// and inventory side-effects (reserve on Paid, release on Cancelled).
/// </summary>
public sealed class UpdateOrderStatusHandlerTests
{
    private static IStringLocalizer<Darwin.Application.ValidationResource> CreateLocalizer()
    {
        var mock = new Mock<IStringLocalizer<Darwin.Application.ValidationResource>>();
        mock.Setup(l => l[It.IsAny<string>()])
            .Returns<string>(name => new LocalizedString(name, name));
        mock.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns<string, object[]>((name, _) => new LocalizedString(name, name));
        return mock.Object;
    }

    private static UpdateOrderStatusHandler CreateHandler(UpdateOrderStatusTestDbContext db)
    {
        var localizer = CreateLocalizer();
        var validator = new UpdateOrderStatusValidator(localizer);
        var reserve = new ReserveInventoryHandler(db, localizer);
        var release = new ReleaseInventoryReservationHandler(db, localizer);
        var allocateValidator = new InventoryAllocateForOrderValidator(localizer);
        var allocate = new AllocateInventoryForOrderHandler(db, allocateValidator, localizer);
        return new UpdateOrderStatusHandler(db, validator, localizer, reserve, release, allocate);
    }

    // ─── Guard: validator catches empty OrderId ───────────────────────────────

    [Fact]
    public async Task Should_Throw_WhenOrderIdIsEmpty()
    {
        await using var db = UpdateOrderStatusTestDbContext.Create();
        var handler = CreateHandler(db);

        var act = async () => await handler.HandleAsync(new UpdateOrderStatusDto
        {
            OrderId = Guid.Empty,
            RowVersion = new byte[] { 1 },
            NewStatus = OrderStatus.Confirmed
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("empty OrderId is rejected by the validator");
    }

    // ─── Guard: empty RowVersion ──────────────────────────────────────────────

    [Fact]
    public async Task Should_Throw_WhenRowVersionIsEmpty()
    {
        await using var db = UpdateOrderStatusTestDbContext.Create();
        var handler = CreateHandler(db);

        var act = async () => await handler.HandleAsync(new UpdateOrderStatusDto
        {
            OrderId = Guid.NewGuid(),
            RowVersion = Array.Empty<byte>(),
            NewStatus = OrderStatus.Confirmed
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("empty RowVersion is rejected");
    }

    // ─── Guard: order not found ───────────────────────────────────────────────

    [Fact]
    public async Task Should_Throw_WhenOrderNotFound()
    {
        await using var db = UpdateOrderStatusTestDbContext.Create();
        var handler = CreateHandler(db);

        var act = async () => await handler.HandleAsync(new UpdateOrderStatusDto
        {
            OrderId = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            NewStatus = OrderStatus.Confirmed
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("order must exist");
    }

    // ─── Guard: stale RowVersion ──────────────────────────────────────────────

    [Fact]
    public async Task Should_Throw_WhenRowVersionIsStale()
    {
        await using var db = UpdateOrderStatusTestDbContext.Create();
        var orderId = Guid.NewGuid();
        db.Set<Order>().Add(BuildOrder(orderId, OrderStatus.Created, rowVersion: new byte[] { 1, 2, 3 }));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);

        var act = async () => await handler.HandleAsync(new UpdateOrderStatusDto
        {
            OrderId = orderId,
            RowVersion = new byte[] { 9, 9, 9 },
            NewStatus = OrderStatus.Confirmed
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("stale RowVersion indicates concurrent modification");
    }

    // ─── Guard: invalid state machine transition ──────────────────────────────

    [Fact]
    public async Task Should_Throw_WhenTransitionIsNotAllowed()
    {
        await using var db = UpdateOrderStatusTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var rowVersion = new byte[] { 5 };
        db.Set<Order>().Add(BuildOrder(orderId, OrderStatus.Created, rowVersion));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);

        // Created → Shipped is not an allowed transition.
        var act = async () => await handler.HandleAsync(new UpdateOrderStatusDto
        {
            OrderId = orderId,
            RowVersion = rowVersion,
            NewStatus = OrderStatus.Shipped
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("Created → Shipped violates the state policy");
    }

    // ─── Evidence: Paid requires sufficient captured payment ──────────────────

    [Fact]
    public async Task Should_Throw_WhenMovingToPaid_WithoutSufficientCapturedPayment()
    {
        await using var db = UpdateOrderStatusTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var rowVersion = new byte[] { 2 };
        // Grand total = 5000 but no captured payments.
        db.Set<Order>().Add(BuildOrder(orderId, OrderStatus.Confirmed, rowVersion, grandTotal: 5000));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);

        var act = async () => await handler.HandleAsync(new UpdateOrderStatusDto
        {
            OrderId = orderId,
            RowVersion = rowVersion,
            NewStatus = OrderStatus.Paid
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("Paid requires a captured payment >= grand total");
    }

    // ─── Evidence: PartiallyShipped requires some but not all lines shipped ───

    [Fact]
    public async Task Should_Throw_WhenMovingToPartiallyShipped_WithNoShipmentEvidence()
    {
        await using var db = UpdateOrderStatusTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var rowVersion = new byte[] { 3 };
        var lineId = Guid.NewGuid();
        db.Set<Order>().Add(BuildOrder(orderId, OrderStatus.Paid, rowVersion));
        db.Set<OrderLine>().Add(BuildOrderLine(orderId, lineId, qty: 2));
        // No shipments → no shipped quantity → evidence check fails.
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);

        var act = async () => await handler.HandleAsync(new UpdateOrderStatusDto
        {
            OrderId = orderId,
            RowVersion = rowVersion,
            NewStatus = OrderStatus.PartiallyShipped
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("PartiallyShipped requires at least some shipped quantity");
    }

    // ─── Evidence: Shipped requires ALL lines fully shipped ───────────────────

    [Fact]
    public async Task Should_Throw_WhenMovingToShipped_WithIncompleteShipment()
    {
        await using var db = UpdateOrderStatusTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var rowVersion = new byte[] { 4 };
        var lineId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();

        db.Set<Order>().Add(BuildOrder(orderId, OrderStatus.Paid, rowVersion));
        db.Set<OrderLine>().Add(BuildOrderLine(orderId, lineId, qty: 5));
        // Shipment covers only 3 of 5 units.
        db.Set<Shipment>().Add(BuildShipment(shipmentId, orderId, ShipmentStatus.Shipped));
        db.Set<ShipmentLine>().Add(new ShipmentLine { Id = Guid.NewGuid(), ShipmentId = shipmentId, OrderLineId = lineId, Quantity = 3, RowVersion = new byte[] { 1 } });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);

        var act = async () => await handler.HandleAsync(new UpdateOrderStatusDto
        {
            OrderId = orderId,
            RowVersion = rowVersion,
            NewStatus = OrderStatus.Shipped
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("Shipped requires all lines to be covered by shipments");
    }

    // ─── Evidence: Delivered requires ALL shipments delivered ────────────────

    [Fact]
    public async Task Should_Throw_WhenMovingToDelivered_WithNoDeliveredShipments()
    {
        await using var db = UpdateOrderStatusTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var rowVersion = new byte[] { 6 };
        var lineId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();

        db.Set<Order>().Add(BuildOrder(orderId, OrderStatus.Shipped, rowVersion));
        db.Set<OrderLine>().Add(BuildOrderLine(orderId, lineId, qty: 1));
        // Shipment is still in Shipped (not Delivered) status.
        db.Set<Shipment>().Add(BuildShipment(shipmentId, orderId, ShipmentStatus.Shipped));
        db.Set<ShipmentLine>().Add(new ShipmentLine { Id = Guid.NewGuid(), ShipmentId = shipmentId, OrderLineId = lineId, Quantity = 1, RowVersion = new byte[] { 1 } });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);

        var act = async () => await handler.HandleAsync(new UpdateOrderStatusDto
        {
            OrderId = orderId,
            RowVersion = rowVersion,
            NewStatus = OrderStatus.Delivered
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("Delivered requires all lines to be in Delivered shipments");
    }

    // ─── Evidence: PartiallyRefunded needs a partial refund ──────────────────

    [Fact]
    public async Task Should_Throw_WhenMovingToPartiallyRefunded_WithNoCompletedRefunds()
    {
        await using var db = UpdateOrderStatusTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var rowVersion = new byte[] { 7 };

        db.Set<Order>().Add(BuildOrder(orderId, OrderStatus.Paid, rowVersion, grandTotal: 1000));
        db.Set<Payment>().Add(BuildPayment(orderId, paymentId, PaymentStatus.Captured, amount: 1000));
        // No refunds at all.
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);

        var act = async () => await handler.HandleAsync(new UpdateOrderStatusDto
        {
            OrderId = orderId,
            RowVersion = rowVersion,
            NewStatus = OrderStatus.PartiallyRefunded
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("PartiallyRefunded requires a partial refund");
    }

    // ─── Evidence: Refunded needs a full refund ───────────────────────────────

    [Fact]
    public async Task Should_Throw_WhenMovingToRefunded_WithInsufficientRefundTotal()
    {
        await using var db = UpdateOrderStatusTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var rowVersion = new byte[] { 8 };

        db.Set<Order>().Add(BuildOrder(orderId, OrderStatus.Paid, rowVersion, grandTotal: 2000));
        db.Set<Payment>().Add(BuildPayment(orderId, paymentId, PaymentStatus.Captured, amount: 2000));
        // Refund covers only 500 of 2000.
        db.Set<Refund>().Add(BuildRefund(orderId, paymentId, 500, RefundStatus.Completed));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);

        var act = async () => await handler.HandleAsync(new UpdateOrderStatusDto
        {
            OrderId = orderId,
            RowVersion = rowVersion,
            NewStatus = OrderStatus.Refunded
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("Refunded requires refund total >= grand total");
    }

    // ─── Evidence: Completed needs full delivery + no open refunds ───────────

    [Fact]
    public async Task Should_Throw_WhenMovingToCompleted_WithOpenRefund()
    {
        await using var db = UpdateOrderStatusTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var rowVersion = new byte[] { 9 };
        var lineId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();

        db.Set<Order>().Add(BuildOrder(orderId, OrderStatus.Delivered, rowVersion, grandTotal: 500));
        db.Set<OrderLine>().Add(BuildOrderLine(orderId, lineId, qty: 1));
        db.Set<Payment>().Add(BuildPayment(orderId, paymentId, PaymentStatus.Captured, amount: 500));
        db.Set<Shipment>().Add(BuildShipment(shipmentId, orderId, ShipmentStatus.Delivered));
        db.Set<ShipmentLine>().Add(new ShipmentLine { Id = Guid.NewGuid(), ShipmentId = shipmentId, OrderLineId = lineId, Quantity = 1, RowVersion = new byte[] { 1 } });
        // Pending refund prevents Completed.
        db.Set<Refund>().Add(BuildRefund(orderId, paymentId, 100, RefundStatus.Pending));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);

        var act = async () => await handler.HandleAsync(new UpdateOrderStatusDto
        {
            OrderId = orderId,
            RowVersion = rowVersion,
            NewStatus = OrderStatus.Completed
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("open refunds prevent Completed");
    }

    // ─── Success: Created → Confirmed ────────────────────────────────────────

    [Fact]
    public async Task Should_Succeed_Created_To_Confirmed()
    {
        await using var db = UpdateOrderStatusTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var rowVersion = new byte[] { 10 };
        db.Set<Order>().Add(BuildOrder(orderId, OrderStatus.Created, rowVersion));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        await handler.HandleAsync(new UpdateOrderStatusDto
        {
            OrderId = orderId,
            RowVersion = rowVersion,
            NewStatus = OrderStatus.Confirmed
        }, TestContext.Current.CancellationToken);

        var saved = await db.Set<Order>().SingleAsync(x => x.Id == orderId, TestContext.Current.CancellationToken);
        saved.Status.Should().Be(OrderStatus.Confirmed);
    }

    // ─── Success: Created → Cancelled (no inventory release, no lines) ────────

    [Fact]
    public async Task Should_Succeed_Created_To_Cancelled_WithNoLines()
    {
        await using var db = UpdateOrderStatusTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var rowVersion = new byte[] { 11 };
        db.Set<Order>().Add(BuildOrder(orderId, OrderStatus.Created, rowVersion));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        await handler.HandleAsync(new UpdateOrderStatusDto
        {
            OrderId = orderId,
            RowVersion = rowVersion,
            NewStatus = OrderStatus.Cancelled
        }, TestContext.Current.CancellationToken);

        var saved = await db.Set<Order>().SingleAsync(x => x.Id == orderId, TestContext.Current.CancellationToken);
        saved.Status.Should().Be(OrderStatus.Cancelled);
    }

    // ─── Success: Confirmed → Paid with captured payment (no lines) ──────────

    [Fact]
    public async Task Should_Succeed_Confirmed_To_Paid_WithCapturedPayment_NoLines()
    {
        await using var db = UpdateOrderStatusTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var rowVersion = new byte[] { 12 };
        db.Set<Order>().Add(BuildOrder(orderId, OrderStatus.Confirmed, rowVersion, grandTotal: 1500));
        db.Set<Payment>().Add(BuildPayment(orderId, paymentId, PaymentStatus.Captured, amount: 1500));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        await handler.HandleAsync(new UpdateOrderStatusDto
        {
            OrderId = orderId,
            RowVersion = rowVersion,
            NewStatus = OrderStatus.Paid
        }, TestContext.Current.CancellationToken);

        var saved = await db.Set<Order>().SingleAsync(x => x.Id == orderId, TestContext.Current.CancellationToken);
        saved.Status.Should().Be(OrderStatus.Paid, "transition should succeed with sufficient captured payment");
    }

    // ─── Success: Confirmed → Cancelled (already released, idempotent) ──────────

    [Fact]
    public async Task Should_Succeed_Confirmed_To_Cancelled_WithAlreadyReleasedInventory()
    {
        await using var db = UpdateOrderStatusTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var rowVersion = new byte[] { 13 };
        var lineId = Guid.NewGuid();

        db.Set<Order>().Add(BuildOrder(orderId, OrderStatus.Confirmed, rowVersion));
        db.Set<OrderLine>().Add(BuildOrderLine(orderId, lineId, qty: 1, variantId: variantId));
        // Pre-seed "already released" transaction so the release loop is skipped.
        db.Set<InventoryTransaction>().Add(new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            WarehouseId = Guid.NewGuid(),
            ProductVariantId = variantId,
            QuantityDelta = 0,
            Reason = "OrderCancelled-Release",
            ReferenceId = orderId,
            RowVersion = new byte[] { 1 }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        await handler.HandleAsync(new UpdateOrderStatusDto
        {
            OrderId = orderId,
            RowVersion = rowVersion,
            NewStatus = OrderStatus.Cancelled
        }, TestContext.Current.CancellationToken);

        var saved = await db.Set<Order>().SingleAsync(x => x.Id == orderId, TestContext.Current.CancellationToken);
        saved.Status.Should().Be(OrderStatus.Cancelled);
    }

    // ─── Success: Paid → Shipped with full shipment and idempotent allocation ─

    [Fact]
    public async Task Should_Succeed_Paid_To_Shipped_WithFullShipmentAndAlreadyAllocated()
    {
        await using var db = UpdateOrderStatusTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var rowVersion = new byte[] { 14 };
        var lineId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();

        db.Set<Order>().Add(BuildOrder(orderId, OrderStatus.Paid, rowVersion));
        db.Set<OrderLine>().Add(BuildOrderLine(orderId, lineId, qty: 2, variantId: variantId, warehouseId: warehouseId));
        db.Set<Shipment>().Add(BuildShipment(shipmentId, orderId, ShipmentStatus.Shipped));
        db.Set<ShipmentLine>().Add(new ShipmentLine { Id = Guid.NewGuid(), ShipmentId = shipmentId, OrderLineId = lineId, Quantity = 2, RowVersion = new byte[] { 1 } });

        // Pre-seed Warehouse and ProductVariant so ResolveWarehouseIdAsync succeeds.
        db.Set<Warehouse>().Add(new Warehouse { Id = warehouseId, Name = "Main", IsDefault = true, RowVersion = new byte[] { 1 } });
        db.Set<ProductVariant>().Add(new ProductVariant { Id = variantId, Sku = "V1", RowVersion = new byte[] { 1 } });

        // Pre-seed "already allocated" so the per-line allocation is skipped.
        db.Set<InventoryTransaction>().Add(new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            WarehouseId = warehouseId,
            ProductVariantId = variantId,
            QuantityDelta = -2,
            Reason = "ShipmentAllocation",
            ReferenceId = orderId,
            RowVersion = new byte[] { 1 }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        await handler.HandleAsync(new UpdateOrderStatusDto
        {
            OrderId = orderId,
            RowVersion = rowVersion,
            NewStatus = OrderStatus.Shipped
        }, TestContext.Current.CancellationToken);

        var saved = await db.Set<Order>().SingleAsync(x => x.Id == orderId, TestContext.Current.CancellationToken);
        saved.Status.Should().Be(OrderStatus.Shipped);
    }

    // ─── Success: Paid → reserve stock when moving to Paid ───────────────────

    [Fact]
    public async Task Should_ReserveInventory_WhenMovingToPaid()
    {
        await using var db = UpdateOrderStatusTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var rowVersion = new byte[] { 15 };
        var lineId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        db.Set<Order>().Add(BuildOrder(orderId, OrderStatus.Confirmed, rowVersion, grandTotal: 500));
        db.Set<OrderLine>().Add(BuildOrderLine(orderId, lineId, qty: 3, variantId: variantId, warehouseId: warehouseId));
        db.Set<Payment>().Add(BuildPayment(orderId, paymentId, PaymentStatus.Captured, amount: 500));
        db.Set<Warehouse>().Add(new Warehouse { Id = warehouseId, Name = "WH1", IsDefault = true, RowVersion = new byte[] { 1 } });
        db.Set<ProductVariant>().Add(new ProductVariant { Id = variantId, Sku = "V-RESERVE", RowVersion = new byte[] { 1 } });
        db.Set<StockLevel>().Add(new StockLevel { Id = Guid.NewGuid(), WarehouseId = warehouseId, ProductVariantId = variantId, AvailableQuantity = 10, ReservedQuantity = 0, RowVersion = new byte[] { 1 } });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        await handler.HandleAsync(new UpdateOrderStatusDto
        {
            OrderId = orderId,
            RowVersion = rowVersion,
            NewStatus = OrderStatus.Paid
        }, TestContext.Current.CancellationToken);

        var saved = await db.Set<Order>().SingleAsync(x => x.Id == orderId, TestContext.Current.CancellationToken);
        saved.Status.Should().Be(OrderStatus.Paid);

        var stock = await db.Set<StockLevel>().SingleAsync(x => x.ProductVariantId == variantId, TestContext.Current.CancellationToken);
        stock.AvailableQuantity.Should().Be(7, "3 units should be reserved");
        stock.ReservedQuantity.Should().Be(3);

        var transaction = await db.Set<InventoryTransaction>()
            .FirstOrDefaultAsync(t => t.ReferenceId == orderId && t.Reason == "OrderPaid-Reserve", TestContext.Current.CancellationToken);
        transaction.Should().NotBeNull("a reservation ledger entry should be created");
    }

    // ─── Success: WarehouseId on lines updated when WarehouseId passed ────────

    [Fact]
    public async Task Should_AssignWarehouseId_ToLines_WhenWarehouseIdProvided()
    {
        await using var db = UpdateOrderStatusTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var rowVersion = new byte[] { 16 };
        var warehouseId = Guid.NewGuid();
        var lineId = Guid.NewGuid();

        db.Set<Order>().Add(BuildOrder(orderId, OrderStatus.Created, rowVersion));
        db.Set<OrderLine>().Add(BuildOrderLine(orderId, lineId, qty: 1)); // no warehouseId initially
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        await handler.HandleAsync(new UpdateOrderStatusDto
        {
            OrderId = orderId,
            RowVersion = rowVersion,
            NewStatus = OrderStatus.Confirmed,
            WarehouseId = warehouseId
        }, TestContext.Current.CancellationToken);

        var line = await db.Set<OrderLine>().SingleAsync(x => x.Id == lineId, TestContext.Current.CancellationToken);
        line.WarehouseId.Should().Be(warehouseId, "handler should assign WarehouseId to unset lines");
    }

    // ─── Builders ─────────────────────────────────────────────────────────────

    private static Order BuildOrder(
        Guid id,
        OrderStatus status,
        byte[] rowVersion,
        long grandTotal = 0) => new()
    {
        Id = id,
        OrderNumber = $"ORD-{id:N}",
        Currency = "EUR",
        Status = status,
        GrandTotalGrossMinor = grandTotal,
        BillingAddressJson = "{}",
        ShippingAddressJson = "{}",
        RowVersion = rowVersion
    };

    private static OrderLine BuildOrderLine(
        Guid orderId,
        Guid lineId,
        int qty,
        Guid? variantId = null,
        Guid? warehouseId = null) => new()
    {
        Id = lineId,
        OrderId = orderId,
        VariantId = variantId ?? Guid.NewGuid(),
        WarehouseId = warehouseId,
        Name = "Widget",
        Sku = "WGT-1",
        Quantity = qty,
        UnitPriceNetMinor = 100,
        VatRate = 0.19m,
        UnitPriceGrossMinor = 119,
        LineTaxMinor = 19,
        LineGrossMinor = qty * 119,
        AddOnValueIdsJson = "[]",
        RowVersion = new byte[] { 1 }
    };

    private static Payment BuildPayment(
        Guid orderId,
        Guid paymentId,
        PaymentStatus status,
        long amount = 0) => new()
    {
        Id = paymentId,
        OrderId = orderId,
        Status = status,
        Provider = "Stripe",
        Currency = "EUR",
        AmountMinor = amount,
        RowVersion = new byte[] { 1 }
    };

    private static Shipment BuildShipment(Guid shipmentId, Guid orderId, ShipmentStatus status) => new()
    {
        Id = shipmentId,
        OrderId = orderId,
        Carrier = "DHL",
        Service = "Parcel",
        Status = status,
        RowVersion = new byte[] { 1 }
    };

    private static Refund BuildRefund(Guid orderId, Guid paymentId, long amount, RefundStatus status) => new()
    {
        Id = Guid.NewGuid(),
        OrderId = orderId,
        PaymentId = paymentId,
        AmountMinor = amount,
        Currency = "EUR",
        Status = status,
        Reason = "Return",
        RowVersion = new byte[] { 1 }
    };

    // ─── Test DbContext ───────────────────────────────────────────────────────

    internal sealed class UpdateOrderStatusTestDbContext : DbContext, IAppDbContext
    {
        private UpdateOrderStatusTestDbContext(DbContextOptions<UpdateOrderStatusTestDbContext> options)
            : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static UpdateOrderStatusTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<UpdateOrderStatusTestDbContext>()
                .UseInMemoryDatabase($"darwin_update_order_status_tests_{Guid.NewGuid()}")
                .Options;
            return new UpdateOrderStatusTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Order>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.OrderNumber).IsRequired();
                b.Property(x => x.Currency).IsRequired();
                b.Property(x => x.BillingAddressJson).IsRequired();
                b.Property(x => x.ShippingAddressJson).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
                b.HasMany(x => x.Lines).WithOne().HasForeignKey(l => l.OrderId);
                b.HasMany(x => x.Payments).WithOne().HasForeignKey(p => p.OrderId);
                b.HasMany(x => x.Shipments).WithOne().HasForeignKey(s => s.OrderId);
            });

            modelBuilder.Entity<OrderLine>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).IsRequired();
                b.Property(x => x.Sku).IsRequired();
                b.Property(x => x.AddOnValueIdsJson).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Payment>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Provider).IsRequired();
                b.Property(x => x.Currency).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Shipment>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Carrier).IsRequired();
                b.Property(x => x.Service).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
                b.HasMany(x => x.Lines).WithOne().HasForeignKey(l => l.ShipmentId);
                b.Ignore(x => x.CarrierEvents);
            });

            modelBuilder.Entity<ShipmentLine>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Refund>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Currency).IsRequired();
                b.Property(x => x.Reason).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Warehouse>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
                b.Ignore(x => x.StockLevels);
            });

            modelBuilder.Entity<StockLevel>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.WarehouseId).IsRequired();
                b.Property(x => x.ProductVariantId).IsRequired();
                b.Property(x => x.AvailableQuantity);
                b.Property(x => x.ReservedQuantity);
                b.Property(x => x.InTransitQuantity);
                b.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<ProductVariant>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Sku).IsRequired();
                b.Property(x => x.StockOnHand);
                b.Property(x => x.StockReserved);
                b.Property(x => x.RowVersion).IsRequired();
                b.Ignore(x => x.OptionValues);
            });

            modelBuilder.Entity<InventoryTransaction>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.WarehouseId).IsRequired();
                b.Property(x => x.ProductVariantId).IsRequired();
                b.Property(x => x.QuantityDelta);
                b.Property(x => x.Reason).HasMaxLength(64).IsRequired();
                b.Property(x => x.ReferenceId);
                b.Property(x => x.RowVersion).IsRequired();
            });
        }
    }
}
