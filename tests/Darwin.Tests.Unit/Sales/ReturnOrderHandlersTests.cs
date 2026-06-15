using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Foundation;
using Darwin.Application.Inventory.Commands;
using Darwin.Application.Sales.Commands;
using Darwin.Application.Sales.DTOs;
using Darwin.Application.Sales.Queries;
using Darwin.Application.Sales.Services;
using Darwin.Application.Sales.Validators;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Entities.Inventory;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Sales;
using Darwin.Domain.Enums;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Sales;

public sealed class ReturnOrderHandlersTests
{
    private static readonly DateTime FixedNow = new(2026, 6, 12, 12, 30, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateReturnOrder_Should_Snapshot_Catalog_And_NonCatalog_Lines()
    {
        await using var db = ReturnOrderTestDbContext.Create();
        var seed = SeedOrderShipmentInventoryAndRefund(db, includeNonCatalogLine: true);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var id = await CreateHandler(db).HandleAsync(new ReturnOrderCreateDto
        {
            OrderId = seed.OrderId,
            ShipmentId = seed.ShipmentId,
            InternalNotes = "  Inspect on arrival. ",
            Lines =
            {
                new() { OrderLineId = seed.CatalogLineId, ShipmentLineId = seed.CatalogShipmentLineId, RequestedQuantity = 2 },
                new() { OrderLineId = seed.NonCatalogLineId, ShipmentLineId = seed.NonCatalogShipmentLineId, RequestedQuantity = 1 }
            }
        }, TestContext.Current.CancellationToken);

        var order = await db.Set<ReturnOrder>().Include(x => x.Lines).SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);

        order.Status.Should().Be(ReturnOrderStatus.Requested);
        order.OrderId.Should().Be(seed.OrderId);
        order.ShipmentId.Should().Be(seed.ShipmentId);
        order.InternalNotes.Should().Be("Inspect on arrival.");
        order.RequestedQuantity.Should().Be(3);
        order.RequestedGrossMinor.Should().Be(4165);
        order.Lines.Should().Contain(x => x.ProductVariantId == seed.CatalogVariantId && x.RequestedQuantity == 2 && x.Name == "Coffee beans");
        order.Lines.Should().Contain(x => x.ProductVariantId == null && x.RequestedQuantity == 1 && x.Name == "Installation service");
        db.Set<BusinessEvent>().Should().ContainSingle(x => x.EventType == "sales.return_order.created");
    }

    [Fact]
    public async Task Lifecycle_Should_Approve_Receive_Inspect_Restock_RefundReady_And_Link_Refund()
    {
        await using var db = ReturnOrderTestDbContext.Create();
        var seed = SeedOrderShipmentInventoryAndRefund(db, includeNonCatalogLine: false);
        SeedReturnOrderNumberSequence(db);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var id = await CreateHandler(db).HandleAsync(new ReturnOrderCreateDto
        {
            OrderId = seed.OrderId,
            ShipmentId = seed.ShipmentId,
            Lines = { new() { OrderLineId = seed.CatalogLineId, ShipmentLineId = seed.CatalogShipmentLineId, RequestedQuantity = 2 } }
        }, TestContext.Current.CancellationToken);
        await SetRowVersionAsync(db, id);
        var handler = LifecycleHandler(db);

        var rowVersion = await RowVersionAsync(db, id);
        await handler.ApproveAsync(new ReturnOrderApproveDto { Id = id, RowVersion = rowVersion }, TestContext.Current.CancellationToken);
        var approved = await db.Set<ReturnOrder>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        approved.ReturnOrderNumber.Should().Be("RO-20260612-001");
        approved.ApprovedQuantity.Should().Be(2);

        rowVersion = await RowVersionAsync(db, id);
        await handler.ReceiveAsync(new ReturnOrderReceiveDto { Id = id, RowVersion = rowVersion }, TestContext.Current.CancellationToken);

        rowVersion = await RowVersionAsync(db, id);
        var line = await db.Set<ReturnOrderLine>().SingleAsync(x => x.ReturnOrderId == id, TestContext.Current.CancellationToken);
        await handler.InspectAsync(new ReturnOrderInspectDto
        {
            Id = id,
            RowVersion = rowVersion,
            Lines = { new() { LineId = line.Id, AcceptedQuantity = 2, RestockQuantity = 2, RestockWarehouseId = seed.WarehouseId } }
        }, TestContext.Current.CancellationToken);

        db.Set<InventoryTransaction>().Should().ContainSingle(x => x.ReferenceId == id && x.Reason == "ReturnOrderRestock" && x.QuantityDelta == 2);
        (await db.Set<StockLevel>().SingleAsync(x => x.WarehouseId == seed.WarehouseId && x.ProductVariantId == seed.CatalogVariantId, TestContext.Current.CancellationToken))
            .AvailableQuantity.Should().Be(2);

        rowVersion = await RowVersionAsync(db, id);
        await handler.MarkRefundReadyAsync(new ReturnOrderLifecycleDto { Id = id, RowVersion = rowVersion }, TestContext.Current.CancellationToken);
        rowVersion = await RowVersionAsync(db, id);
        await handler.LinkRefundAsync(new ReturnOrderLinkRefundDto { Id = id, RowVersion = rowVersion, RefundId = seed.RefundId }, TestContext.Current.CancellationToken);

        var refunded = await db.Set<ReturnOrder>().Include(x => x.RefundLinks).SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        refunded.Status.Should().Be(ReturnOrderStatus.Refunded);
        refunded.RefundLinks.Should().ContainSingle(x => x.RefundId == seed.RefundId);
    }

    [Fact]
    public async Task LinkRefund_Should_Keep_Partial_Links_RefundReady_And_Complete_When_Eligibility_Is_Covered()
    {
        await using var db = ReturnOrderTestDbContext.Create();
        var seed = SeedOrderShipmentInventoryAndRefund(db, includeNonCatalogLine: false);
        var firstRefundId = AddRefund(db, seed.OrderId, 1000);
        var secondRefundId = AddRefund(db, seed.OrderId, 1380);
        SeedReturnOrderNumberSequence(db);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var id = await CreateRefundReadyReturnAsync(db, seed, acceptedQuantity: 2);
        var handler = LifecycleHandler(db);

        var rowVersion = await RowVersionAsync(db, id);
        await handler.LinkRefundAsync(new ReturnOrderLinkRefundDto { Id = id, RowVersion = rowVersion, RefundId = firstRefundId }, TestContext.Current.CancellationToken);
        var partial = await db.Set<ReturnOrder>().Include(x => x.RefundLinks).SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        partial.Status.Should().Be(ReturnOrderStatus.RefundReady);
        partial.RefundLinks.Should().ContainSingle(x => x.RefundId == firstRefundId);

        rowVersion = await RowVersionAsync(db, id);
        await handler.LinkRefundAsync(new ReturnOrderLinkRefundDto { Id = id, RowVersion = rowVersion, RefundId = firstRefundId }, TestContext.Current.CancellationToken);
        var duplicate = await db.Set<ReturnOrder>().Include(x => x.RefundLinks).SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        duplicate.Status.Should().Be(ReturnOrderStatus.RefundReady);
        duplicate.RefundLinks.Where(x => x.RefundId == firstRefundId).Should().ContainSingle();

        rowVersion = await RowVersionAsync(db, id);
        await handler.LinkRefundAsync(new ReturnOrderLinkRefundDto { Id = id, RowVersion = rowVersion, RefundId = secondRefundId }, TestContext.Current.CancellationToken);
        var refunded = await db.Set<ReturnOrder>().Include(x => x.RefundLinks).SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        refunded.Status.Should().Be(ReturnOrderStatus.Refunded);
        refunded.RefundLinks.Where(x => !x.IsDeleted).Sum(x => x.AmountMinor).Should().Be(refunded.RefundEligibleGrossMinor);

        var detail = await new GetReturnOrderDetailHandler(db).HandleAsync(id, TestContext.Current.CancellationToken);
        detail!.LinkedRefundGrossMinor.Should().Be(2380);
        detail.RemainingRefundGrossMinor.Should().Be(0);
    }

    [Fact]
    public async Task LinkRefund_Should_Reject_Invalid_Currency_Status_Order_Deleted_And_OverEligibility()
    {
        await using var db = ReturnOrderTestDbContext.Create();
        var seed = SeedOrderShipmentInventoryAndRefund(db, includeNonCatalogLine: false);
        var overRefundId = AddRefund(db, seed.OrderId, 2381);
        var otherOrderRefundId = AddRefund(db, Guid.NewGuid(), 100);
        var otherCurrencyRefundId = AddRefund(db, seed.OrderId, 100, currency: "USD");
        var pendingRefundId = AddRefund(db, seed.OrderId, 100, status: RefundStatus.Pending);
        var failedRefundId = AddRefund(db, seed.OrderId, 100, status: RefundStatus.Failed);
        var deletedRefundId = AddRefund(db, seed.OrderId, 100, isDeleted: true);
        SeedReturnOrderNumberSequence(db);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var id = await CreateRefundReadyReturnAsync(db, seed, acceptedQuantity: 2);
        var handler = LifecycleHandler(db);

        foreach (var refundId in new[] { overRefundId, otherOrderRefundId, otherCurrencyRefundId, pendingRefundId, failedRefundId, deletedRefundId })
        {
            var rowVersion = await RowVersionAsync(db, id);
            await handler.Invoking(x => x.LinkRefundAsync(new ReturnOrderLinkRefundDto { Id = id, RowVersion = rowVersion, RefundId = refundId }, TestContext.Current.CancellationToken))
                .Should()
                .ThrowAsync<ValidationException>();
        }

        var order = await db.Set<ReturnOrder>().Include(x => x.RefundLinks).SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        order.Status.Should().Be(ReturnOrderStatus.RefundReady);
        order.RefundLinks.Should().BeEmpty();
    }

    [Fact]
    public async Task Inspect_Should_Not_Create_Second_Restock_On_Retry()
    {
        await using var db = ReturnOrderTestDbContext.Create();
        var seed = SeedOrderShipmentInventoryAndRefund(db, includeNonCatalogLine: false);
        SeedReturnOrderNumberSequence(db);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var id = await CreateReceivedReturnAsync(db, seed, requestedQuantity: 2);
        var handler = LifecycleHandler(db);
        var line = await db.Set<ReturnOrderLine>().SingleAsync(x => x.ReturnOrderId == id, TestContext.Current.CancellationToken);

        var inspect = new ReturnOrderInspectDto
        {
            Id = id,
            RowVersion = await RowVersionAsync(db, id),
            Lines = { new() { LineId = line.Id, AcceptedQuantity = 2, RestockQuantity = 2, RestockWarehouseId = seed.WarehouseId } }
        };
        await handler.InspectAsync(inspect, TestContext.Current.CancellationToken);

        inspect.RowVersion = await RowVersionAsync(db, id);
        await handler.Invoking(x => x.InspectAsync(inspect, TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<ValidationException>();

        db.Set<InventoryTransaction>().Where(x => x.ReferenceId == id && x.Reason == "ReturnOrderRestock").Should().ContainSingle();
        (await db.Set<StockLevel>().SingleAsync(x => x.WarehouseId == seed.WarehouseId && x.ProductVariantId == seed.CatalogVariantId, TestContext.Current.CancellationToken))
            .AvailableQuantity.Should().Be(2);
    }

    [Fact]
    public async Task NonCatalog_Return_Line_Should_Allow_Acceptance_But_Reject_Restock()
    {
        await using var db = ReturnOrderTestDbContext.Create();
        var seed = SeedOrderShipmentInventoryAndRefund(db, includeNonCatalogLine: true);
        SeedReturnOrderNumberSequence(db);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var acceptedId = await CreateReceivedReturnAsync(db, seed, requestedQuantity: 1, useNonCatalogLine: true);
        var handler = LifecycleHandler(db);
        var acceptedLine = await db.Set<ReturnOrderLine>().SingleAsync(x => x.ReturnOrderId == acceptedId, TestContext.Current.CancellationToken);

        await handler.InspectAsync(new ReturnOrderInspectDto
        {
            Id = acceptedId,
            RowVersion = await RowVersionAsync(db, acceptedId),
            Lines = { new() { LineId = acceptedLine.Id, AcceptedQuantity = 1 } }
        }, TestContext.Current.CancellationToken);
        var accepted = await db.Set<ReturnOrder>().SingleAsync(x => x.Id == acceptedId, TestContext.Current.CancellationToken);
        accepted.AcceptedQuantity.Should().Be(1);
        accepted.RefundEligibleGrossMinor.Should().Be(1785);
        db.Set<InventoryTransaction>().Should().BeEmpty();

        var restockId = await CreateReceivedReturnAsync(db, seed, requestedQuantity: 1, useNonCatalogLine: true);
        var restockLine = await db.Set<ReturnOrderLine>().SingleAsync(x => x.ReturnOrderId == restockId, TestContext.Current.CancellationToken);
        await handler.Invoking(x => x.InspectAsync(new ReturnOrderInspectDto
            {
                Id = restockId,
                RowVersion = RowVersionAsync(db, restockId).GetAwaiter().GetResult(),
                Lines = { new() { LineId = restockLine.Id, AcceptedQuantity = 1, RestockQuantity = 1, RestockWarehouseId = seed.WarehouseId } }
            }, TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<ValidationException>();
        db.Set<InventoryTransaction>().Should().BeEmpty();
    }

    [Fact]
    public async Task Lifecycle_Should_Reject_RefundReady_And_Restock_Before_Inspection()
    {
        await using var db = ReturnOrderTestDbContext.Create();
        var seed = SeedOrderShipmentInventoryAndRefund(db, includeNonCatalogLine: false);
        SeedReturnOrderNumberSequence(db);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var id = await CreateHandler(db).HandleAsync(new ReturnOrderCreateDto
        {
            OrderId = seed.OrderId,
            ShipmentId = seed.ShipmentId,
            Lines = { new() { OrderLineId = seed.CatalogLineId, ShipmentLineId = seed.CatalogShipmentLineId, RequestedQuantity = 1 } }
        }, TestContext.Current.CancellationToken);
        await SetRowVersionAsync(db, id);
        var handler = LifecycleHandler(db);

        await handler.Invoking(x => x.MarkRefundReadyAsync(new ReturnOrderLifecycleDto
            {
                Id = id,
                RowVersion = RowVersionAsync(db, id).GetAwaiter().GetResult()
            }, TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<ValidationException>();

        db.Set<InventoryTransaction>().Should().BeEmpty();
    }

    [Fact]
    public async Task GetReturnOrdersPage_And_Detail_Should_Filter_And_Return_NonNull_Collections()
    {
        await using var db = ReturnOrderTestDbContext.Create();
        var seed = SeedOrderShipmentInventoryAndRefund(db, includeNonCatalogLine: false);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var id = await CreateHandler(db).HandleAsync(new ReturnOrderCreateDto
        {
            OrderId = seed.OrderId,
            ShipmentId = seed.ShipmentId,
            Lines = { new() { OrderLineId = seed.CatalogLineId, ShipmentLineId = seed.CatalogShipmentLineId, RequestedQuantity = 1 } }
        }, TestContext.Current.CancellationToken);
        var order = await db.Set<ReturnOrder>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        order.ReturnOrderNumber = "RO-MATCH";
        order.Status = ReturnOrderStatus.Approved;
        order.ApprovedAtUtc = FixedNow;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var (items, total) = await new GetReturnOrdersPageHandler(db).HandleAsync(
            1,
            20,
            "MATCH",
            ReturnOrderDocumentFilter.Approved,
            seed.BusinessId,
            seed.CustomerId,
            null,
            null,
            TestContext.Current.CancellationToken);
        var detail = await new GetReturnOrderDetailHandler(db).HandleAsync(id, TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().ContainSingle().Which.ReturnOrderNumber.Should().Be("RO-MATCH");
        detail.Should().NotBeNull();
        detail!.Lines.Should().ContainSingle();
        detail.RefundLinks.Should().BeEmpty();
    }

    private static CreateReturnOrderHandler CreateHandler(ReturnOrderTestDbContext db)
        => new(db, new ReturnOrderCreateValidator(), new TestStringLocalizer(), new FixedClock(FixedNow), EventService(db));

    private static UpdateReturnOrderLifecycleHandler LifecycleHandler(ReturnOrderTestDbContext db)
        => new(
            db,
            new TestStringLocalizer(),
            new FixedClock(FixedNow),
            new NumberSequenceService(db, new FixedClock(FixedNow)),
            new ReturnOrderWorkflowPolicy(),
            EventService(db),
            new ProcessReturnReceiptHandler(db, new TestStringLocalizer()),
            new ReturnOrderLifecycleValidator(),
            new ReturnOrderApproveValidator(),
            new ReturnOrderQueueShipmentValidator(),
            new ReturnOrderReceiveValidator(),
            new ReturnOrderInspectValidator(),
            new ReturnOrderLinkRefundValidator());

    private static ReturnOrderLifecycleEventService EventService(ReturnOrderTestDbContext db)
        => new(new BusinessEventService(db, new FixedClock(FixedNow)), db);

    private static async Task<Guid> CreateRefundReadyReturnAsync(ReturnOrderTestDbContext db, SeedIds seed, int acceptedQuantity)
    {
        var id = await CreateReceivedReturnAsync(db, seed, acceptedQuantity);
        var handler = LifecycleHandler(db);
        var line = await db.Set<ReturnOrderLine>().SingleAsync(x => x.ReturnOrderId == id, TestContext.Current.CancellationToken);
        await handler.InspectAsync(new ReturnOrderInspectDto
        {
            Id = id,
            RowVersion = await RowVersionAsync(db, id),
            Lines = { new() { LineId = line.Id, AcceptedQuantity = acceptedQuantity } }
        }, TestContext.Current.CancellationToken);
        await handler.MarkRefundReadyAsync(new ReturnOrderLifecycleDto
        {
            Id = id,
            RowVersion = await RowVersionAsync(db, id)
        }, TestContext.Current.CancellationToken);
        return id;
    }

    private static async Task<Guid> CreateReceivedReturnAsync(ReturnOrderTestDbContext db, SeedIds seed, int requestedQuantity, bool useNonCatalogLine = false)
    {
        var id = await CreateHandler(db).HandleAsync(new ReturnOrderCreateDto
        {
            OrderId = seed.OrderId,
            ShipmentId = seed.ShipmentId,
            Lines =
            {
                new()
                {
                    OrderLineId = useNonCatalogLine ? seed.NonCatalogLineId : seed.CatalogLineId,
                    ShipmentLineId = useNonCatalogLine ? seed.NonCatalogShipmentLineId : seed.CatalogShipmentLineId,
                    RequestedQuantity = requestedQuantity
                }
            }
        }, TestContext.Current.CancellationToken);
        await SetRowVersionAsync(db, id);
        var handler = LifecycleHandler(db);
        await handler.ApproveAsync(new ReturnOrderApproveDto { Id = id, RowVersion = await RowVersionAsync(db, id) }, TestContext.Current.CancellationToken);
        await handler.ReceiveAsync(new ReturnOrderReceiveDto { Id = id, RowVersion = await RowVersionAsync(db, id) }, TestContext.Current.CancellationToken);
        return id;
    }

    private static async Task<byte[]> RowVersionAsync(ReturnOrderTestDbContext db, Guid id)
        => await db.Set<ReturnOrder>().Where(x => x.Id == id).Select(x => x.RowVersion).SingleAsync(TestContext.Current.CancellationToken);

    private static async Task SetRowVersionAsync(ReturnOrderTestDbContext db, Guid id)
    {
        var order = await db.Set<ReturnOrder>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        order.RowVersion = new byte[] { 9, 9, 9 };
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static Guid AddRefund(ReturnOrderTestDbContext db, Guid orderId, long amountMinor, string currency = "EUR", RefundStatus status = RefundStatus.Completed, bool isDeleted = false)
    {
        var refundId = Guid.NewGuid();
        db.Set<Refund>().Add(new Refund
        {
            Id = refundId,
            PaymentId = Guid.NewGuid(),
            OrderId = orderId,
            AmountMinor = amountMinor,
            Currency = currency,
            Reason = "Return",
            Status = status,
            IsDeleted = isDeleted,
            RowVersion = new byte[] { 8 }
        });
        return refundId;
    }

    private static SeedIds SeedOrderShipmentInventoryAndRefund(ReturnOrderTestDbContext db, bool includeNonCatalogLine)
    {
        var businessId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var catalogLineId = Guid.NewGuid();
        var nonCatalogLineId = Guid.NewGuid();
        var catalogShipmentLineId = Guid.NewGuid();
        var nonCatalogShipmentLineId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var refundId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        var order = new Order { Id = orderId, BusinessId = businessId, CustomerId = customerId, OrderNumber = "ORD-RO-1", Currency = "EUR", ShippingAddressJson = "{\"line1\":\"Customer\"}", RowVersion = new byte[] { 1 } };
        order.Lines.Add(new OrderLine { Id = catalogLineId, OrderId = orderId, VariantId = variantId, Name = "Coffee beans", Sku = "COF-1", Quantity = 3, UnitPriceNetMinor = 1000, UnitPriceGrossMinor = 1190, VatRate = 0.19m, RowVersion = new byte[] { 2 } });
        if (includeNonCatalogLine)
        {
            order.Lines.Add(new OrderLine { Id = nonCatalogLineId, OrderId = orderId, VariantId = null, Name = "Installation service", Sku = string.Empty, Quantity = 1, UnitPriceNetMinor = 1500, UnitPriceGrossMinor = 1785, VatRate = 0.19m, RowVersion = new byte[] { 3 } });
        }

        var shipment = new Shipment { Id = shipmentId, OrderId = orderId, RowVersion = new byte[] { 4 } };
        shipment.Lines.Add(new ShipmentLine { Id = catalogShipmentLineId, ShipmentId = shipmentId, OrderLineId = catalogLineId, Quantity = 2, RowVersion = new byte[] { 5 } });
        if (includeNonCatalogLine)
        {
            shipment.Lines.Add(new ShipmentLine { Id = nonCatalogShipmentLineId, ShipmentId = shipmentId, OrderLineId = nonCatalogLineId, Quantity = 1, RowVersion = new byte[] { 6 } });
        }

        db.Set<Order>().Add(order);
        db.Set<Shipment>().Add(shipment);
        db.Set<ProductVariant>().Add(new ProductVariant { Id = variantId, ProductId = Guid.NewGuid(), Sku = "COF-1", Currency = "EUR" });
        db.Set<Warehouse>().Add(new Warehouse { Id = warehouseId, BusinessId = businessId, Name = "Returns warehouse", IsDefault = true, RowVersion = new byte[] { 9 } });
        db.Set<StockLevel>().Add(new StockLevel { Id = Guid.NewGuid(), WarehouseId = warehouseId, ProductVariantId = variantId, AvailableQuantity = 0, ReservedQuantity = 0, RowVersion = new byte[] { 7 } });
        db.Set<Refund>().Add(new Refund { Id = refundId, PaymentId = paymentId, OrderId = orderId, AmountMinor = 2380, Currency = "EUR", Reason = "Return", Status = RefundStatus.Completed, RowVersion = new byte[] { 8 } });
        return new SeedIds(businessId, customerId, orderId, shipmentId, catalogLineId, nonCatalogLineId, catalogShipmentLineId, nonCatalogShipmentLineId, variantId, warehouseId, refundId);
    }

    private static void SeedReturnOrderNumberSequence(ReturnOrderTestDbContext db)
    {
        db.Set<NumberSequence>().Add(new NumberSequence
        {
            Id = Guid.NewGuid(),
            DocumentType = NumberSequenceDocumentType.ReturnOrder,
            ScopeKey = NumberSequenceService.GlobalScopeKey,
            PrefixPattern = "RO-{yyyy}{MM}{dd}-{seq}",
            NextValue = 1,
            PaddingLength = 3,
            ResetPolicy = NumberSequenceResetPolicy.Never,
            IsActive = true,
            MetadataJson = "{}",
            RowVersion = new byte[] { 10 }
        });
    }

    private sealed record SeedIds(
        Guid BusinessId,
        Guid CustomerId,
        Guid OrderId,
        Guid ShipmentId,
        Guid CatalogLineId,
        Guid NonCatalogLineId,
        Guid CatalogShipmentLineId,
        Guid NonCatalogShipmentLineId,
        Guid CatalogVariantId,
        Guid WarehouseId,
        Guid RefundId);

    private sealed class ReturnOrderTestDbContext : DbContext, IAppDbContext
    {
        private ReturnOrderTestDbContext(DbContextOptions<ReturnOrderTestDbContext> options) : base(options) { }

        public static ReturnOrderTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<ReturnOrderTestDbContext>()
                .UseInMemoryDatabase($"darwin_return_order_tests_{Guid.NewGuid():N}")
                .Options;
            return new ReturnOrderTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>().HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.OrderId);
            modelBuilder.Entity<OrderLine>();
            modelBuilder.Entity<Shipment>().HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.ShipmentId);
            modelBuilder.Entity<ShipmentLine>();
            modelBuilder.Entity<Refund>();
            modelBuilder.Entity<ReturnOrder>().HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.ReturnOrderId);
            modelBuilder.Entity<ReturnOrder>().HasMany(x => x.RefundLinks).WithOne().HasForeignKey(x => x.ReturnOrderId);
            modelBuilder.Entity<ReturnOrderLine>();
            modelBuilder.Entity<ReturnOrderRefundLink>();
            modelBuilder.Entity<NumberSequence>();
            modelBuilder.Entity<BusinessEvent>();
            modelBuilder.Entity<AuditTrail>();
            modelBuilder.Entity<ProductVariant>();
            modelBuilder.Entity<StockLevel>();
            modelBuilder.Entity<Warehouse>();
            modelBuilder.Entity<InventoryTransaction>();
        }
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
    }

    private sealed class TestStringLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name);
        public LocalizedString this[string name, params object[] arguments] => new(name, string.Format(name, arguments));
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();
    }
}
