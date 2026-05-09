using System;
using System.Linq;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Inventory.DTOs;
using Darwin.Application.Inventory.Queries;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.Inventory;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Darwin.Tests.Unit.Inventory;

/// <summary>
/// Unit tests for the Inventory management query handlers:
/// <see cref="GetWarehouseLookupHandler"/>, <see cref="GetWarehousesPageHandler"/>,
/// <see cref="GetWarehouseForEditHandler"/>, <see cref="GetSuppliersPageHandler"/>,
/// <see cref="GetSupplierForEditHandler"/>, <see cref="GetStockLevelsPageHandler"/>,
/// <see cref="GetStockLevelForEditHandler"/>, <see cref="GetVariantStockHandler"/>,
/// <see cref="GetInventoryLedgerHandler"/>.
/// </summary>
public sealed class InventoryManagementQueryHandlerTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // GetWarehouseLookupHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetWarehouseLookup_Should_ReturnNonDeleted_OrderedByDefaultThenName()
    {
        await using var db = InventoryQueryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        db.Set<Warehouse>().AddRange(
            new Warehouse { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Zeta WH", IsDefault = false },
            new Warehouse { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Alpha WH", IsDefault = true },
            new Warehouse { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Beta WH", IsDefault = false, IsDeleted = true });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetWarehouseLookupHandler(db);
        var items = await handler.HandleAsync(TestContext.Current.CancellationToken);

        items.Should().HaveCount(2, "soft-deleted warehouses must be excluded");
        items[0].Name.Should().Be("Alpha WH", "default warehouse comes first");
        items[1].Name.Should().Be("Zeta WH");
    }

    [Fact]
    public async Task GetWarehouseLookup_Should_ReturnEmptyList_WhenNoWarehouses()
    {
        await using var db = InventoryQueryTestDbContext.Create();
        var handler = new GetWarehouseLookupHandler(db);

        var items = await handler.HandleAsync(TestContext.Current.CancellationToken);

        items.Should().BeEmpty();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetWarehousesPageHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetWarehousesPage_Should_ReturnAllNonDeleted_ForBusiness()
    {
        await using var db = InventoryQueryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        db.Set<Warehouse>().AddRange(
            new Warehouse { Id = Guid.NewGuid(), BusinessId = businessId, Name = "WH-A" },
            new Warehouse { Id = Guid.NewGuid(), BusinessId = businessId, Name = "WH-B" },
            new Warehouse { Id = Guid.NewGuid(), BusinessId = businessId, Name = "WH-Deleted", IsDeleted = true },
            new Warehouse { Id = Guid.NewGuid(), BusinessId = Guid.NewGuid(), Name = "WH-Other" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetWarehousesPageHandler(db);
        var (items, total) = await handler.HandleAsync(businessId, 1, 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(2);
        items.Should().HaveCount(2);
        items.Should().NotContain(x => x.Name == "WH-Deleted");
        items.Should().NotContain(x => x.Name == "WH-Other");
    }

    [Fact]
    public async Task GetWarehousesPage_Should_FilterDefaultOnly()
    {
        await using var db = InventoryQueryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        db.Set<Warehouse>().AddRange(
            new Warehouse { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Default WH", IsDefault = true },
            new Warehouse { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Secondary WH", IsDefault = false });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetWarehousesPageHandler(db);
        var (items, total) = await handler.HandleAsync(businessId, 1, 20, filter: WarehouseQueueFilter.Default, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().ContainSingle(x => x.Name == "Default WH");
    }

    [Fact]
    public async Task GetWarehousesPage_Should_NormalizeInvalidPageParams()
    {
        await using var db = InventoryQueryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        db.Set<Warehouse>().Add(new Warehouse { Id = Guid.NewGuid(), BusinessId = businessId, Name = "WH1" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetWarehousesPageHandler(db);
        var (items, total) = await handler.HandleAsync(businessId, page: -1, pageSize: 0, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetWarehousesSummary_Should_ReturnCorrectCounts()
    {
        await using var db = InventoryQueryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var whId = Guid.NewGuid();
        db.Set<Warehouse>().AddRange(
            new Warehouse { Id = whId, BusinessId = businessId, Name = "Default", IsDefault = true },
            new Warehouse { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Secondary", IsDefault = false });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetWarehousesPageHandler(db);
        var summary = await handler.GetSummaryAsync(businessId, TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(2);
        summary.DefaultCount.Should().Be(1);
    }

    [Fact]
    public async Task GetWarehousesSummary_Should_ReturnEmptySummary_WhenNoWarehouses()
    {
        await using var db = InventoryQueryTestDbContext.Create();
        var handler = new GetWarehousesPageHandler(db);
        var summary = await handler.GetSummaryAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(0);
        summary.DefaultCount.Should().Be(0);
        summary.NoStockLevelsCount.Should().Be(0);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetWarehouseForEditHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetWarehouseForEdit_Should_ReturnDto_WhenFound()
    {
        await using var db = InventoryQueryTestDbContext.Create();
        var id = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        db.Set<Warehouse>().Add(new Warehouse
        {
            Id = id, BusinessId = businessId, Name = "Edit WH", Location = "Floor 1",
            IsDefault = true, RowVersion = [1, 2, 3]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetWarehouseForEditHandler(db);
        var result = await handler.HandleAsync(id, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
        result.Name.Should().Be("Edit WH");
        result.Location.Should().Be("Floor 1");
        result.IsDefault.Should().BeTrue();
        result.BusinessId.Should().Be(businessId);
    }

    [Fact]
    public async Task GetWarehouseForEdit_Should_ReturnNull_WhenNotFound()
    {
        await using var db = InventoryQueryTestDbContext.Create();
        var handler = new GetWarehouseForEditHandler(db);

        var result = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetWarehouseForEdit_Should_ReturnNull_WhenSoftDeleted()
    {
        await using var db = InventoryQueryTestDbContext.Create();
        var id = Guid.NewGuid();
        db.Set<Warehouse>().Add(new Warehouse { Id = id, BusinessId = Guid.NewGuid(), Name = "Deleted WH", IsDeleted = true });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetWarehouseForEditHandler(db);
        var result = await handler.HandleAsync(id, TestContext.Current.CancellationToken);

        result.Should().BeNull("soft-deleted warehouse should not be returned for edit");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetSuppliersPageHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSuppliersPage_Should_ReturnAllNonDeleted_ForBusiness()
    {
        await using var db = InventoryQueryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        db.Set<Supplier>().AddRange(
            new Supplier { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Sup-A", Email = "a@a.com", Phone = "111" },
            new Supplier { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Sup-B", Email = "b@b.com", Phone = "222" },
            new Supplier { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Sup-Deleted", Email = "d@d.com", Phone = "333", IsDeleted = true },
            new Supplier { Id = Guid.NewGuid(), BusinessId = Guid.NewGuid(), Name = "Other Sup", Email = "o@o.com", Phone = "444" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetSuppliersPageHandler(db);
        var (items, total) = await handler.HandleAsync(businessId, 1, 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(2);
        items.Should().HaveCount(2);
        items.Should().NotContain(x => x.Name == "Sup-Deleted");
        items.Should().NotContain(x => x.Name == "Other Sup");
    }

    [Fact]
    public async Task GetSuppliersPage_Should_FilterMissingAddress()
    {
        await using var db = InventoryQueryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        db.Set<Supplier>().AddRange(
            new Supplier { Id = Guid.NewGuid(), BusinessId = businessId, Name = "No Addr", Email = "n@n.com", Phone = "000", Address = null },
            new Supplier { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Has Addr", Email = "h@h.com", Phone = "111", Address = "123 St" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetSuppliersPageHandler(db);
        var (items, total) = await handler.HandleAsync(businessId, 1, 20, filter: SupplierQueueFilter.MissingAddress, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().ContainSingle(x => x.Name == "No Addr");
    }

    [Fact]
    public async Task GetSuppliersSummary_Should_ReturnCorrectCounts()
    {
        await using var db = InventoryQueryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        db.Set<Supplier>().AddRange(
            new Supplier { Id = Guid.NewGuid(), BusinessId = businessId, Name = "S1", Email = "s1@s.com", Phone = "0", Address = null },
            new Supplier { Id = Guid.NewGuid(), BusinessId = businessId, Name = "S2", Email = "s2@s.com", Phone = "1", Address = "Addr" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetSuppliersPageHandler(db);
        var summary = await handler.GetSummaryAsync(businessId, TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(2);
        summary.MissingAddressCount.Should().Be(1);
        summary.HasPurchaseOrdersCount.Should().Be(0);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetSupplierForEditHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSupplierForEdit_Should_ReturnDto_WhenFound()
    {
        await using var db = InventoryQueryTestDbContext.Create();
        var id = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        db.Set<Supplier>().Add(new Supplier
        {
            Id = id, BusinessId = businessId, Name = "Acme", Email = "acme@acme.com",
            Phone = "+1234567890", Address = "Main St", Notes = "Top tier"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetSupplierForEditHandler(db);
        var result = await handler.HandleAsync(id, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
        result.Name.Should().Be("Acme");
        result.Email.Should().Be("acme@acme.com");
        result.Phone.Should().Be("+1234567890");
        result.Address.Should().Be("Main St");
        result.Notes.Should().Be("Top tier");
    }

    [Fact]
    public async Task GetSupplierForEdit_Should_ReturnNull_WhenNotFound()
    {
        await using var db = InventoryQueryTestDbContext.Create();
        var handler = new GetSupplierForEditHandler(db);

        var result = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSupplierForEdit_Should_ReturnNull_WhenSoftDeleted()
    {
        await using var db = InventoryQueryTestDbContext.Create();
        var id = Guid.NewGuid();
        db.Set<Supplier>().Add(new Supplier
        {
            Id = id, BusinessId = Guid.NewGuid(), Name = "Deleted", Email = "d@d.com", Phone = "0", IsDeleted = true
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetSupplierForEditHandler(db);
        var result = await handler.HandleAsync(id, TestContext.Current.CancellationToken);

        result.Should().BeNull("soft-deleted supplier should not be returned for edit");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetStockLevelsPageHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStockLevelsPage_Should_ReturnNonDeleted_ForWarehouse()
    {
        await using var db = InventoryQueryTestDbContext.Create();
        var warehouseId = Guid.NewGuid();
        var v1 = Guid.NewGuid();
        var v2 = Guid.NewGuid();
        var v3 = Guid.NewGuid();
        var otherWh = Guid.NewGuid();

        db.Set<Warehouse>().AddRange(
            new Warehouse { Id = warehouseId, Name = "Main WH" },
            new Warehouse { Id = otherWh, Name = "Other WH" });
        db.Set<ProductVariant>().AddRange(
            new ProductVariant { Id = v1, Sku = "SKU-1" },
            new ProductVariant { Id = v2, Sku = "SKU-2" },
            new ProductVariant { Id = v3, Sku = "SKU-3" });
        db.Set<StockLevel>().AddRange(
            new StockLevel { Id = Guid.NewGuid(), WarehouseId = warehouseId, ProductVariantId = v1, AvailableQuantity = 10 },
            new StockLevel { Id = Guid.NewGuid(), WarehouseId = warehouseId, ProductVariantId = v2, AvailableQuantity = 5, IsDeleted = true },
            new StockLevel { Id = Guid.NewGuid(), WarehouseId = otherWh, ProductVariantId = v3, AvailableQuantity = 20 });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetStockLevelsPageHandler(db);
        var (items, total) = await handler.HandleAsync(warehouseId, 1, 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().ContainSingle(x => x.VariantSku == "SKU-1");
    }

    [Fact]
    public async Task GetStockLevelsPage_Should_FilterLowStock()
    {
        await using var db = InventoryQueryTestDbContext.Create();
        var warehouseId = Guid.NewGuid();
        var v1 = Guid.NewGuid();
        var v2 = Guid.NewGuid();

        db.Set<Warehouse>().Add(new Warehouse { Id = warehouseId, Name = "WH" });
        db.Set<ProductVariant>().AddRange(
            new ProductVariant { Id = v1, Sku = "LOW" },
            new ProductVariant { Id = v2, Sku = "OK" });
        db.Set<StockLevel>().AddRange(
            new StockLevel { Id = Guid.NewGuid(), WarehouseId = warehouseId, ProductVariantId = v1, AvailableQuantity = 2, ReorderPoint = 10 },
            new StockLevel { Id = Guid.NewGuid(), WarehouseId = warehouseId, ProductVariantId = v2, AvailableQuantity = 50, ReorderPoint = 10 });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetStockLevelsPageHandler(db);
        var (items, total) = await handler.HandleAsync(warehouseId, 1, 20, filter: StockLevelQueueFilter.LowStock, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().ContainSingle(x => x.VariantSku == "LOW");
    }

    [Fact]
    public async Task GetStockLevelsPage_Should_FilterReserved()
    {
        await using var db = InventoryQueryTestDbContext.Create();
        var warehouseId = Guid.NewGuid();
        var v1 = Guid.NewGuid();
        var v2 = Guid.NewGuid();

        db.Set<Warehouse>().Add(new Warehouse { Id = warehouseId, Name = "WH" });
        db.Set<ProductVariant>().AddRange(
            new ProductVariant { Id = v1, Sku = "RESERVED" },
            new ProductVariant { Id = v2, Sku = "FREE" });
        db.Set<StockLevel>().AddRange(
            new StockLevel { Id = Guid.NewGuid(), WarehouseId = warehouseId, ProductVariantId = v1, ReservedQuantity = 5 },
            new StockLevel { Id = Guid.NewGuid(), WarehouseId = warehouseId, ProductVariantId = v2, ReservedQuantity = 0 });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetStockLevelsPageHandler(db);
        var (items, total) = await handler.HandleAsync(warehouseId, 1, 20, filter: StockLevelQueueFilter.Reserved, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().ContainSingle(x => x.VariantSku == "RESERVED");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetStockLevelForEditHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStockLevelForEdit_Should_ReturnDto_WhenFound()
    {
        await using var db = InventoryQueryTestDbContext.Create();
        var id = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        db.Set<StockLevel>().Add(new StockLevel
        {
            Id = id, WarehouseId = warehouseId, ProductVariantId = variantId,
            AvailableQuantity = 30, ReservedQuantity = 5, ReorderPoint = 10,
            ReorderQuantity = 50, InTransitQuantity = 2
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetStockLevelForEditHandler(db);
        var result = await handler.HandleAsync(id, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
        result.WarehouseId.Should().Be(warehouseId);
        result.ProductVariantId.Should().Be(variantId);
        result.AvailableQuantity.Should().Be(30);
        result.ReservedQuantity.Should().Be(5);
        result.ReorderPoint.Should().Be(10);
        result.ReorderQuantity.Should().Be(50);
        result.InTransitQuantity.Should().Be(2);
    }

    [Fact]
    public async Task GetStockLevelForEdit_Should_ReturnNull_WhenNotFound()
    {
        await using var db = InventoryQueryTestDbContext.Create();
        var handler = new GetStockLevelForEditHandler(db);

        var result = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetStockLevelForEdit_Should_ReturnNull_WhenSoftDeleted()
    {
        await using var db = InventoryQueryTestDbContext.Create();
        var id = Guid.NewGuid();
        db.Set<StockLevel>().Add(new StockLevel
        {
            Id = id, WarehouseId = Guid.NewGuid(), ProductVariantId = Guid.NewGuid(), IsDeleted = true
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetStockLevelForEditHandler(db);
        var result = await handler.HandleAsync(id, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetVariantStockHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetVariantStock_Should_ReturnNull_WhenNoStockLevels()
    {
        await using var db = InventoryQueryTestDbContext.Create();
        var handler = new GetVariantStockHandler(db);

        var result = await handler.HandleAsync(Guid.NewGuid(), ct: TestContext.Current.CancellationToken);

        result.Should().BeNull("no stock level rows exist for this variant");
    }

    [Fact]
    public async Task GetVariantStock_Should_AggregateTotals_AcrossWarehouses()
    {
        await using var db = InventoryQueryTestDbContext.Create();
        var variantId = Guid.NewGuid();
        var wh1 = Guid.NewGuid();
        var wh2 = Guid.NewGuid();
        db.Set<StockLevel>().AddRange(
            new StockLevel { Id = Guid.NewGuid(), WarehouseId = wh1, ProductVariantId = variantId, AvailableQuantity = 10, ReservedQuantity = 2 },
            new StockLevel { Id = Guid.NewGuid(), WarehouseId = wh2, ProductVariantId = variantId, AvailableQuantity = 15, ReservedQuantity = 3 });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetVariantStockHandler(db);
        var result = await handler.HandleAsync(variantId, ct: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Value.Available.Should().Be(25, "10 + 15");
        result.Value.Reserved.Should().Be(5, "2 + 3");
        result.Value.OnHand.Should().Be(30, "available + reserved");
    }

    [Fact]
    public async Task GetVariantStock_Should_FilterByWarehouse_WhenWarehouseIdProvided()
    {
        await using var db = InventoryQueryTestDbContext.Create();
        var variantId = Guid.NewGuid();
        var wh1 = Guid.NewGuid();
        var wh2 = Guid.NewGuid();
        db.Set<StockLevel>().AddRange(
            new StockLevel { Id = Guid.NewGuid(), WarehouseId = wh1, ProductVariantId = variantId, AvailableQuantity = 10, ReservedQuantity = 2 },
            new StockLevel { Id = Guid.NewGuid(), WarehouseId = wh2, ProductVariantId = variantId, AvailableQuantity = 15, ReservedQuantity = 3 });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetVariantStockHandler(db);
        var result = await handler.HandleAsync(variantId, warehouseId: wh1, ct: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Value.Available.Should().Be(10, "only wh1 stock");
        result.Value.Reserved.Should().Be(2);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetInventoryLedgerHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetInventoryLedger_Should_ReturnAllTransactions_WhenNoFilter()
    {
        await using var db = InventoryQueryTestDbContext.Create();
        var warehouseId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        db.Set<Warehouse>().Add(new Warehouse { Id = warehouseId, Name = "WH" });
        db.Set<InventoryTransaction>().AddRange(
            new InventoryTransaction { Id = Guid.NewGuid(), WarehouseId = warehouseId, ProductVariantId = variantId, QuantityDelta = 10, Reason = "GoodsReceipt" },
            new InventoryTransaction { Id = Guid.NewGuid(), WarehouseId = warehouseId, ProductVariantId = variantId, QuantityDelta = -5, Reason = "WriteOff" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetInventoryLedgerHandler(db);
        var (items, total) = await handler.HandleAsync(null, 1, 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(2);
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetInventoryLedger_Should_FilterByVariant()
    {
        await using var db = InventoryQueryTestDbContext.Create();
        var warehouseId = Guid.NewGuid();
        var v1 = Guid.NewGuid();
        var v2 = Guid.NewGuid();
        db.Set<Warehouse>().Add(new Warehouse { Id = warehouseId, Name = "WH" });
        db.Set<InventoryTransaction>().AddRange(
            new InventoryTransaction { Id = Guid.NewGuid(), WarehouseId = warehouseId, ProductVariantId = v1, QuantityDelta = 10, Reason = "GoodsReceipt" },
            new InventoryTransaction { Id = Guid.NewGuid(), WarehouseId = warehouseId, ProductVariantId = v2, QuantityDelta = 5, Reason = "GoodsReceipt" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetInventoryLedgerHandler(db);
        var (items, total) = await handler.HandleAsync(v1, 1, 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().ContainSingle(x => x.VariantId == v1);
    }

    [Fact]
    public async Task GetInventoryLedger_Should_FilterInbound()
    {
        await using var db = InventoryQueryTestDbContext.Create();
        var warehouseId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        db.Set<Warehouse>().Add(new Warehouse { Id = warehouseId, Name = "WH" });
        db.Set<InventoryTransaction>().AddRange(
            new InventoryTransaction { Id = Guid.NewGuid(), WarehouseId = warehouseId, ProductVariantId = variantId, QuantityDelta = 20, Reason = "GoodsReceipt" },
            new InventoryTransaction { Id = Guid.NewGuid(), WarehouseId = warehouseId, ProductVariantId = variantId, QuantityDelta = -5, Reason = "WriteOff" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetInventoryLedgerHandler(db);
        var (items, total) = await handler.HandleAsync(null, 1, 20, filter: InventoryLedgerQueueFilter.Inbound, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().QuantityDelta.Should().BePositive();
    }

    [Fact]
    public async Task GetInventoryLedger_Should_FilterOutbound()
    {
        await using var db = InventoryQueryTestDbContext.Create();
        var warehouseId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        db.Set<Warehouse>().Add(new Warehouse { Id = warehouseId, Name = "WH" });
        db.Set<InventoryTransaction>().AddRange(
            new InventoryTransaction { Id = Guid.NewGuid(), WarehouseId = warehouseId, ProductVariantId = variantId, QuantityDelta = 20, Reason = "GoodsReceipt" },
            new InventoryTransaction { Id = Guid.NewGuid(), WarehouseId = warehouseId, ProductVariantId = variantId, QuantityDelta = -3, Reason = "WriteOff" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetInventoryLedgerHandler(db);
        var (items, total) = await handler.HandleAsync(null, 1, 20, filter: InventoryLedgerQueueFilter.Outbound, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().QuantityDelta.Should().BeNegative();
    }

    [Fact]
    public async Task GetInventoryLedger_Should_FilterReservations()
    {
        await using var db = InventoryQueryTestDbContext.Create();
        var warehouseId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        db.Set<Warehouse>().Add(new Warehouse { Id = warehouseId, Name = "WH" });
        db.Set<InventoryTransaction>().AddRange(
            new InventoryTransaction { Id = Guid.NewGuid(), WarehouseId = warehouseId, ProductVariantId = variantId, QuantityDelta = -5, Reason = "CartReservation" },
            new InventoryTransaction { Id = Guid.NewGuid(), WarehouseId = warehouseId, ProductVariantId = variantId, QuantityDelta = 5, Reason = "Reserve" },
            new InventoryTransaction { Id = Guid.NewGuid(), WarehouseId = warehouseId, ProductVariantId = variantId, QuantityDelta = 10, Reason = "GoodsReceipt" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetInventoryLedgerHandler(db);
        var (items, total) = await handler.HandleAsync(null, 1, 20, filter: InventoryLedgerQueueFilter.Reservations, ct: TestContext.Current.CancellationToken);

        total.Should().Be(2, "both Reserve and CartReservation match the reservations filter");
    }

    [Fact]
    public async Task GetInventoryLedger_Should_ReturnEmptyState_WhenNoTransactions()
    {
        await using var db = InventoryQueryTestDbContext.Create();
        var handler = new GetInventoryLedgerHandler(db);

        var (items, total) = await handler.HandleAsync(null, 1, 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(0);
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetInventoryLedgerSummary_Should_ReturnCorrectCounts()
    {
        await using var db = InventoryQueryTestDbContext.Create();
        var warehouseId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        db.Set<Warehouse>().Add(new Warehouse { Id = warehouseId, Name = "WH" });
        db.Set<InventoryTransaction>().AddRange(
            new InventoryTransaction { Id = Guid.NewGuid(), WarehouseId = warehouseId, ProductVariantId = variantId, QuantityDelta = 20, Reason = "GoodsReceipt" },
            new InventoryTransaction { Id = Guid.NewGuid(), WarehouseId = warehouseId, ProductVariantId = variantId, QuantityDelta = -10, Reason = "WriteOff" },
            new InventoryTransaction { Id = Guid.NewGuid(), WarehouseId = warehouseId, ProductVariantId = variantId, QuantityDelta = -2, Reason = "CartReservation" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetInventoryLedgerHandler(db);
        var summary = await handler.GetSummaryAsync(null, ct: TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(3);
        summary.InboundCount.Should().Be(1);
        summary.OutboundCount.Should().Be(2);
        summary.ReservationCount.Should().Be(1);
    }

    [Fact]
    public async Task GetInventoryLedgerSummary_Should_ReturnEmptySummary_WhenNoTransactions()
    {
        await using var db = InventoryQueryTestDbContext.Create();
        var handler = new GetInventoryLedgerHandler(db);

        var summary = await handler.GetSummaryAsync(null, ct: TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(0);
        summary.InboundCount.Should().Be(0);
        summary.OutboundCount.Should().Be(0);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private test infrastructure
    // ─────────────────────────────────────────────────────────────────────────

    private sealed class InventoryQueryTestDbContext : DbContext, IAppDbContext
    {
        private InventoryQueryTestDbContext(DbContextOptions<InventoryQueryTestDbContext> options)
            : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static InventoryQueryTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<InventoryQueryTestDbContext>()
                .UseInMemoryDatabase($"darwin_inventory_query_tests_{Guid.NewGuid()}")
                .Options;
            return new InventoryQueryTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Warehouse>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).HasMaxLength(200).IsRequired();
                b.Property(x => x.IsDefault);
                b.Property(x => x.IsDeleted);
                b.Property(x => x.RowVersion).IsRowVersion();
                // Keep StockLevels navigation so that queries using StockLevels.Count/Any translate correctly.
                b.HasMany(x => x.StockLevels).WithOne().HasForeignKey(s => s.WarehouseId);
            });

            modelBuilder.Entity<StockLevel>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.WarehouseId).IsRequired();
                b.Property(x => x.ProductVariantId).IsRequired();
                b.Property(x => x.AvailableQuantity);
                b.Property(x => x.ReservedQuantity);
                b.Property(x => x.InTransitQuantity);
                b.Property(x => x.ReorderPoint);
                b.Property(x => x.ReorderQuantity);
                b.Property(x => x.IsDeleted);
                b.Property(x => x.RowVersion).IsRowVersion();
            });

            modelBuilder.Entity<Supplier>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.BusinessId).IsRequired();
                b.Property(x => x.Name).HasMaxLength(200).IsRequired();
                b.Property(x => x.Email).HasMaxLength(256).IsRequired();
                b.Property(x => x.Phone).HasMaxLength(50).IsRequired();
                b.Property(x => x.Address).HasMaxLength(500);
                b.Property(x => x.Notes);
                b.Property(x => x.IsDeleted);
                b.Property(x => x.RowVersion).IsRowVersion();
                // Keep PurchaseOrders navigation so that queries using PurchaseOrders.Any translate correctly.
                b.HasMany(x => x.PurchaseOrders).WithOne().HasForeignKey(o => o.SupplierId);
            });

            modelBuilder.Entity<ProductVariant>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Sku).HasMaxLength(128).IsRequired();
                b.Property(x => x.StockOnHand);
                b.Property(x => x.StockReserved);
                b.Property(x => x.IsDeleted);
            });

            modelBuilder.Entity<InventoryTransaction>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.WarehouseId).IsRequired();
                b.Property(x => x.ProductVariantId).IsRequired();
                b.Property(x => x.QuantityDelta);
                b.Property(x => x.Reason).HasMaxLength(64).IsRequired();
                b.Property(x => x.ReferenceId);
            });

            modelBuilder.Entity<PurchaseOrder>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.SupplierId).IsRequired();
                b.Property(x => x.BusinessId).IsRequired();
                b.Property(x => x.OrderNumber).HasMaxLength(64).IsRequired();
                b.Property(x => x.Status);
                b.Property(x => x.OrderedAtUtc).IsRequired();
                b.Property(x => x.IsDeleted);
                b.Property(x => x.RowVersion).IsRowVersion();
                b.HasMany(x => x.Lines).WithOne().HasForeignKey(l => l.PurchaseOrderId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PurchaseOrderLine>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.PurchaseOrderId).IsRequired();
                b.Property(x => x.ProductVariantId).IsRequired();
                b.Property(x => x.Quantity);
                b.Property(x => x.UnitCostMinor);
                b.Property(x => x.TotalCostMinor);
                b.Property(x => x.IsDeleted);
            });

            modelBuilder.Entity<StockTransfer>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.FromWarehouseId).IsRequired();
                b.Property(x => x.ToWarehouseId).IsRequired();
                b.Property(x => x.Status);
                b.Property(x => x.IsDeleted);
                b.Property(x => x.RowVersion).IsRowVersion();
                b.HasMany(x => x.Lines).WithOne().HasForeignKey(l => l.StockTransferId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<StockTransferLine>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.StockTransferId).IsRequired();
                b.Property(x => x.ProductVariantId).IsRequired();
                b.Property(x => x.Quantity);
                b.Property(x => x.IsDeleted);
            });
        }
    }
}
