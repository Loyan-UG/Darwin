using System;
using System.Linq;
using System.Threading.Tasks;
using Darwin.Application.Inventory.Commands;
using Darwin.Application.Inventory.DTOs;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.Inventory;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace Darwin.Tests.Unit.Inventory;

/// <summary>
/// Unit tests for <see cref="AdjustInventoryHandler"/>,
/// <see cref="ReserveInventoryHandler"/>, and
/// <see cref="ReleaseInventoryReservationHandler"/>.
///
/// These tests use the in-memory <see cref="Darwin.Tests.Unit.TestDbFactory"/> to
/// exercise the full handler logic without a real database.
/// </summary>
public sealed class InventoryHandlerTests
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

    // ─────────────────────────────────────────────────────────────────────────
    // AdjustInventoryHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AdjustInventory_Should_Throw_ValidationException_When_Dto_Invalid()
    {
        var db = TestDbFactory.Create();
        var handler = new AdjustInventoryHandler(db, CreateLocalizer());
        var dto = new InventoryAdjustDto { VariantId = Guid.NewGuid(), QuantityDelta = 0, Reason = "" };

        var act = async () => await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("a zero delta with empty reason violates the validator");
    }

    [Fact]
    public async Task AdjustInventory_Should_Throw_When_Variant_Not_Found()
    {
        var db = TestDbFactory.Create();
        var handler = new AdjustInventoryHandler(db, CreateLocalizer());

        // Seed a warehouse so the helper can resolve one
        var warehouse = new Warehouse { Id = Guid.NewGuid(), Name = "Main", IsDefault = true };
        await db.Set<Warehouse>().AddAsync(warehouse, TestContext.Current.CancellationToken);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var dto = new InventoryAdjustDto
        {
            VariantId = Guid.NewGuid(), // does not exist in DB
            QuantityDelta = 5,
            Reason = "GoodsReceipt"
        };

        var act = async () => await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>("the variant does not exist");
    }

    [Fact]
    public async Task AdjustInventory_Should_Increase_AvailableQuantity_On_Positive_Delta()
    {
        var db = TestDbFactory.Create();
        var handler = new AdjustInventoryHandler(db, CreateLocalizer());

        var variantId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();

        await db.Set<Warehouse>().AddAsync(new Warehouse { Id = warehouseId, Name = "WH", IsDefault = true }, TestContext.Current.CancellationToken);
        await db.Set<ProductVariant>().AddAsync(new ProductVariant { Id = variantId, Sku = "SKU1", StockOnHand = 0 }, TestContext.Current.CancellationToken);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var dto = new InventoryAdjustDto
        {
            VariantId = variantId,
            WarehouseId = warehouseId,
            QuantityDelta = 20,
            Reason = "GoodsReceipt"
        };

        await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        var stockLevel = db.Set<StockLevel>().Single(s => s.ProductVariantId == variantId);
        stockLevel.AvailableQuantity.Should().Be(20, "20 units were received");
    }

    [Fact]
    public async Task AdjustInventory_Should_Decrease_AvailableQuantity_On_Negative_Delta()
    {
        var db = TestDbFactory.Create();
        var handler = new AdjustInventoryHandler(db, CreateLocalizer());

        var variantId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();

        await db.Set<Warehouse>().AddAsync(new Warehouse { Id = warehouseId, Name = "WH", IsDefault = true }, TestContext.Current.CancellationToken);
        await db.Set<ProductVariant>().AddAsync(new ProductVariant { Id = variantId, Sku = "SKU2", StockOnHand = 0 }, TestContext.Current.CancellationToken);
        // Pre-seed a stock level with 30 units
        await db.Set<StockLevel>().AddAsync(new StockLevel { Id = Guid.NewGuid(), WarehouseId = warehouseId, ProductVariantId = variantId, AvailableQuantity = 30 }, TestContext.Current.CancellationToken);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var dto = new InventoryAdjustDto
        {
            VariantId = variantId,
            WarehouseId = warehouseId,
            QuantityDelta = -10,
            Reason = "WriteOff"
        };

        await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        var stockLevel = db.Set<StockLevel>().Single(s => s.ProductVariantId == variantId);
        stockLevel.AvailableQuantity.Should().Be(20, "10 units were written off from 30");
    }

    [Fact]
    public async Task AdjustInventory_Should_Throw_When_WriteOff_Exceeds_Available()
    {
        var db = TestDbFactory.Create();
        var handler = new AdjustInventoryHandler(db, CreateLocalizer());

        var variantId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();

        await db.Set<Warehouse>().AddAsync(new Warehouse { Id = warehouseId, Name = "WH", IsDefault = true }, TestContext.Current.CancellationToken);
        await db.Set<ProductVariant>().AddAsync(new ProductVariant { Id = variantId, Sku = "SKU3" }, TestContext.Current.CancellationToken);
        await db.Set<StockLevel>().AddAsync(new StockLevel { Id = Guid.NewGuid(), WarehouseId = warehouseId, ProductVariantId = variantId, AvailableQuantity = 5 }, TestContext.Current.CancellationToken);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var dto = new InventoryAdjustDto
        {
            VariantId = variantId,
            WarehouseId = warehouseId,
            QuantityDelta = -10,
            Reason = "WriteOff"
        };

        var act = async () => await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>("cannot write off more than available stock");
    }

    [Fact]
    public async Task AdjustInventory_Should_Append_Ledger_Transaction()
    {
        var db = TestDbFactory.Create();
        var handler = new AdjustInventoryHandler(db, CreateLocalizer());

        var variantId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var referenceId = Guid.NewGuid();

        await db.Set<Warehouse>().AddAsync(new Warehouse { Id = warehouseId, Name = "WH", IsDefault = true }, TestContext.Current.CancellationToken);
        await db.Set<ProductVariant>().AddAsync(new ProductVariant { Id = variantId, Sku = "SKU4" }, TestContext.Current.CancellationToken);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var dto = new InventoryAdjustDto
        {
            VariantId = variantId,
            WarehouseId = warehouseId,
            QuantityDelta = 15,
            Reason = "ManualReceipt",
            ReferenceId = referenceId
        };

        await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        var transaction = db.Set<InventoryTransaction>()
            .Single(t => t.ProductVariantId == variantId);
        transaction.QuantityDelta.Should().Be(15);
        transaction.Reason.Should().Be("ManualReceipt");
        transaction.ReferenceId.Should().Be(referenceId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ReserveInventoryHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReserveInventory_Should_Throw_ValidationException_When_Dto_Invalid()
    {
        var db = TestDbFactory.Create();
        var handler = new ReserveInventoryHandler(db, CreateLocalizer());
        var dto = new InventoryReserveDto { VariantId = Guid.Empty, Quantity = 0, Reason = "" };

        var act = async () => await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task ReserveInventory_Should_Throw_When_StockLevel_Not_Found()
    {
        var db = TestDbFactory.Create();
        var handler = new ReserveInventoryHandler(db, CreateLocalizer());

        var warehouseId = Guid.NewGuid();
        await db.Set<Warehouse>().AddAsync(new Warehouse { Id = warehouseId, Name = "WH", IsDefault = true }, TestContext.Current.CancellationToken);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var dto = new InventoryReserveDto
        {
            VariantId = Guid.NewGuid(),
            WarehouseId = warehouseId,
            Quantity = 1,
            Reason = "CartReservation"
        };

        var act = async () => await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>("there is no stock level row for this variant");
    }

    [Fact]
    public async Task ReserveInventory_Should_Throw_When_Insufficient_Available_Stock()
    {
        var db = TestDbFactory.Create();
        var handler = new ReserveInventoryHandler(db, CreateLocalizer());

        var variantId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();

        await db.Set<Warehouse>().AddAsync(new Warehouse { Id = warehouseId, Name = "WH", IsDefault = true }, TestContext.Current.CancellationToken);
        await db.Set<ProductVariant>().AddAsync(new ProductVariant { Id = variantId, Sku = "SKU5" }, TestContext.Current.CancellationToken);
        await db.Set<StockLevel>().AddAsync(new StockLevel { Id = Guid.NewGuid(), WarehouseId = warehouseId, ProductVariantId = variantId, AvailableQuantity = 2 }, TestContext.Current.CancellationToken);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var dto = new InventoryReserveDto
        {
            VariantId = variantId,
            WarehouseId = warehouseId,
            Quantity = 10,
            Reason = "CartReservation"
        };

        var act = async () => await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("cannot reserve more than available");
    }

    [Fact]
    public async Task ReserveInventory_Should_Transfer_Stock_From_Available_To_Reserved()
    {
        var db = TestDbFactory.Create();
        var handler = new ReserveInventoryHandler(db, CreateLocalizer());

        var variantId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();

        await db.Set<Warehouse>().AddAsync(new Warehouse { Id = warehouseId, Name = "WH", IsDefault = true }, TestContext.Current.CancellationToken);
        await db.Set<ProductVariant>().AddAsync(new ProductVariant { Id = variantId, Sku = "SKU6", StockOnHand = 0 }, TestContext.Current.CancellationToken);
        await db.Set<StockLevel>().AddAsync(new StockLevel { Id = Guid.NewGuid(), WarehouseId = warehouseId, ProductVariantId = variantId, AvailableQuantity = 10, ReservedQuantity = 0 }, TestContext.Current.CancellationToken);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var dto = new InventoryReserveDto
        {
            VariantId = variantId,
            WarehouseId = warehouseId,
            Quantity = 3,
            Reason = "CartReservation"
        };

        await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        var stockLevel = db.Set<StockLevel>().Single(s => s.ProductVariantId == variantId);
        stockLevel.AvailableQuantity.Should().Be(7, "3 units moved from available to reserved");
        stockLevel.ReservedQuantity.Should().Be(3);
    }

    [Fact]
    public async Task ReserveInventory_Should_BeIdempotent_When_ReferenceAlreadyLedgered()
    {
        var db = TestDbFactory.Create();
        var handler = new ReserveInventoryHandler(db, CreateLocalizer());

        var variantId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var referenceId = Guid.NewGuid();

        await db.Set<Warehouse>().AddAsync(new Warehouse { Id = warehouseId, Name = "WH", IsDefault = true }, TestContext.Current.CancellationToken);
        await db.Set<ProductVariant>().AddAsync(new ProductVariant { Id = variantId, Sku = "SKU6-IDEMP", StockOnHand = 0 }, TestContext.Current.CancellationToken);
        await db.Set<StockLevel>().AddAsync(new StockLevel { Id = Guid.NewGuid(), WarehouseId = warehouseId, ProductVariantId = variantId, AvailableQuantity = 10, ReservedQuantity = 0 }, TestContext.Current.CancellationToken);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var dto = new InventoryReserveDto
        {
            VariantId = variantId,
            WarehouseId = warehouseId,
            Quantity = 3,
            Reason = "CartReservation",
            ReferenceId = referenceId
        };

        await handler.HandleAsync(dto, TestContext.Current.CancellationToken);
        await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        var stockLevel = db.Set<StockLevel>().Single(s => s.ProductVariantId == variantId);
        stockLevel.AvailableQuantity.Should().Be(7);
        stockLevel.ReservedQuantity.Should().Be(3);
        db.Set<InventoryTransaction>().Count(t => t.ReferenceId == referenceId && t.Reason == "CartReservation").Should().Be(1);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ReleaseInventoryReservationHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReleaseInventoryReservation_Should_Throw_ValidationException_When_Dto_Invalid()
    {
        var db = TestDbFactory.Create();
        var handler = new ReleaseInventoryReservationHandler(db, CreateLocalizer());
        var dto = new InventoryReleaseReservationDto { VariantId = Guid.Empty, Quantity = 0, Reason = "" };

        var act = async () => await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task ReleaseInventoryReservation_Should_Throw_When_Insufficient_Reserved()
    {
        var db = TestDbFactory.Create();
        var handler = new ReleaseInventoryReservationHandler(db, CreateLocalizer());

        var variantId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();

        await db.Set<Warehouse>().AddAsync(new Warehouse { Id = warehouseId, Name = "WH", IsDefault = true }, TestContext.Current.CancellationToken);
        await db.Set<ProductVariant>().AddAsync(new ProductVariant { Id = variantId, Sku = "SKU7" }, TestContext.Current.CancellationToken);
        await db.Set<StockLevel>().AddAsync(new StockLevel { Id = Guid.NewGuid(), WarehouseId = warehouseId, ProductVariantId = variantId, ReservedQuantity = 1 }, TestContext.Current.CancellationToken);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var dto = new InventoryReleaseReservationDto
        {
            VariantId = variantId,
            WarehouseId = warehouseId,
            Quantity = 5,
            Reason = "CartAbandonment"
        };

        var act = async () => await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<Exception>("cannot release more than reserved");
    }

    [Fact]
    public async Task ReleaseInventoryReservation_Should_Return_Stock_From_Reserved_To_Available()
    {
        var db = TestDbFactory.Create();
        var handler = new ReleaseInventoryReservationHandler(db, CreateLocalizer());

        var variantId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();

        await db.Set<Warehouse>().AddAsync(new Warehouse { Id = warehouseId, Name = "WH", IsDefault = true }, TestContext.Current.CancellationToken);
        await db.Set<ProductVariant>().AddAsync(new ProductVariant { Id = variantId, Sku = "SKU8", StockOnHand = 0, StockReserved = 4 }, TestContext.Current.CancellationToken);
        await db.Set<StockLevel>().AddAsync(new StockLevel { Id = Guid.NewGuid(), WarehouseId = warehouseId, ProductVariantId = variantId, AvailableQuantity = 6, ReservedQuantity = 4 }, TestContext.Current.CancellationToken);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var dto = new InventoryReleaseReservationDto
        {
            VariantId = variantId,
            WarehouseId = warehouseId,
            Quantity = 4,
            Reason = "CartAbandonment"
        };

        await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        var stockLevel = db.Set<StockLevel>().Single(s => s.ProductVariantId == variantId);
        stockLevel.ReservedQuantity.Should().Be(0, "all reserved stock was released");
        stockLevel.AvailableQuantity.Should().Be(10, "4 units returned to available pool");
    }

    [Fact]
    public async Task ReleaseInventoryReservation_Should_BeIdempotent_When_ReferenceAlreadyLedgered()
    {
        var db = TestDbFactory.Create();
        var handler = new ReleaseInventoryReservationHandler(db, CreateLocalizer());

        var variantId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var referenceId = Guid.NewGuid();

        await db.Set<Warehouse>().AddAsync(new Warehouse { Id = warehouseId, Name = "WH", IsDefault = true }, TestContext.Current.CancellationToken);
        await db.Set<ProductVariant>().AddAsync(new ProductVariant { Id = variantId, Sku = "SKU8-IDEMP", StockOnHand = 0, StockReserved = 4 }, TestContext.Current.CancellationToken);
        await db.Set<StockLevel>().AddAsync(new StockLevel { Id = Guid.NewGuid(), WarehouseId = warehouseId, ProductVariantId = variantId, AvailableQuantity = 6, ReservedQuantity = 4 }, TestContext.Current.CancellationToken);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var dto = new InventoryReleaseReservationDto
        {
            VariantId = variantId,
            WarehouseId = warehouseId,
            Quantity = 4,
            Reason = "CartAbandonment",
            ReferenceId = referenceId
        };

        await handler.HandleAsync(dto, TestContext.Current.CancellationToken);
        await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        var stockLevel = db.Set<StockLevel>().Single(s => s.ProductVariantId == variantId);
        stockLevel.ReservedQuantity.Should().Be(0);
        stockLevel.AvailableQuantity.Should().Be(10);
        db.Set<InventoryTransaction>().Count(t => t.ReferenceId == referenceId && t.Reason == "CartAbandonment").Should().Be(1);
    }
}
