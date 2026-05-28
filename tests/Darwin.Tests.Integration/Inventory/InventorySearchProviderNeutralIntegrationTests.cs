using Darwin.Application.Inventory.DTOs;
using Darwin.Application.Inventory.Queries;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.Inventory;
using Darwin.Domain.Entities.Pricing;
using Darwin.Domain.Enums;
using Darwin.Infrastructure.Persistence.Db;
using Darwin.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.Tests.Integration.Inventory;

public sealed class InventorySearchProviderNeutralIntegrationTests
    : DeterministicIntegrationTestBase,
      IClassFixture<WebApplicationFactory<Program>>
{
    public InventorySearchProviderNeutralIntegrationTests(WebApplicationFactory<Program> factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetWarehousesPage_Should_HandleEscapedSubstringAndCaseVariants_OnNameSearch()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DarwinDbContext>();

        var marker = Guid.NewGuid().ToString("N");
        var exactMatchName = $"warehouse_%_probe[{marker}]";
        var unrelatedName = $"warehouseXprobe[{marker.Substring(0, 6)}]";
        var businessId = Guid.NewGuid();

        var exactMatchWarehouse = new Warehouse { BusinessId = businessId, Name = exactMatchName };
        var unrelatedWarehouse = new Warehouse { BusinessId = businessId, Name = unrelatedName };

        db.Set<Warehouse>().AddRange(exactMatchWarehouse, unrelatedWarehouse);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = scope.ServiceProvider.GetRequiredService<GetWarehousesPageHandler>();
        var lowerCaseResult = await handler.HandleAsync(
            businessId,
            page: 1,
            pageSize: 20,
            query: $"warehouse_%_probe[{marker}]",
            filter: WarehouseQueueFilter.All,
            ct: TestContext.Current.CancellationToken);

        var upperCaseResult = await handler.HandleAsync(
            businessId,
            page: 1,
            pageSize: 20,
            query: $"WAREHOUSE_%_PROBE[{marker}]",
            filter: WarehouseQueueFilter.All,
            ct: TestContext.Current.CancellationToken);

        lowerCaseResult.Total.Should().Be(1);
        lowerCaseResult.Items.Should().ContainSingle(x => x.Id == exactMatchWarehouse.Id);
        lowerCaseResult.Items.Should().NotContain(x => x.Id == unrelatedWarehouse.Id);

        upperCaseResult.Total.Should().Be(1);
        upperCaseResult.Items.Should().ContainSingle(x => x.Id == exactMatchWarehouse.Id);
        upperCaseResult.Items.Should().NotContain(x => x.Id == unrelatedWarehouse.Id);
    }

    [Fact]
    public async Task GetSuppliersPage_Should_HandleEscapedSubstringAndCaseVariants_OnEmailSearch()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DarwinDbContext>();

        var marker = Guid.NewGuid().ToString("N");
        var businessId = Guid.NewGuid();
        var exactMatchEmail = $"supplier_%_probe[{marker}]@example.test";
        var unrelatedEmail = $"supplierprobe[{marker.Substring(0, 6)}]@example.test";

        var exactMatchSupplier = new Supplier { BusinessId = businessId, Name = "Matching supplier", Email = exactMatchEmail, Phone = "123" };
        var unrelatedSupplier = new Supplier { BusinessId = businessId, Name = "Other supplier", Email = unrelatedEmail, Phone = "456" };

        db.Set<Supplier>().AddRange(exactMatchSupplier, unrelatedSupplier);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = scope.ServiceProvider.GetRequiredService<GetSuppliersPageHandler>();
        var lowerCaseResult = await handler.HandleAsync(
            businessId,
            page: 1,
            pageSize: 20,
            query: $"supplier_%_probe[{marker}]",
            filter: SupplierQueueFilter.All,
            ct: TestContext.Current.CancellationToken);

        var upperCaseResult = await handler.HandleAsync(
            businessId,
            page: 1,
            pageSize: 20,
            query: $"SUPPLIER_%_PROBE[{marker}]",
            filter: SupplierQueueFilter.All,
            ct: TestContext.Current.CancellationToken);

        lowerCaseResult.Total.Should().Be(1);
        lowerCaseResult.Items.Should().ContainSingle(x => x.Id == exactMatchSupplier.Id);
        lowerCaseResult.Items.Should().NotContain(x => x.Id == unrelatedSupplier.Id);

        upperCaseResult.Total.Should().Be(1);
        upperCaseResult.Items.Should().ContainSingle(x => x.Id == exactMatchSupplier.Id);
        upperCaseResult.Items.Should().NotContain(x => x.Id == unrelatedSupplier.Id);
    }

    [Fact]
    public async Task GetStockLevelsPage_Should_HandleEscapedSubstringAndCaseVariants_OnSkuSearch()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DarwinDbContext>();

        var marker = Guid.NewGuid().ToString("N");
        var businessId = Guid.NewGuid();
        var exactMatchSku = $"stock_%_probe[{marker}]";
        var unrelatedSku = $"stockprobe[{marker.Substring(0, 6)}]";

        var taxCategory = new TaxCategory
        {
            Name = $"Tax {marker}",
            VatRate = 0.19m
        };
        var warehouse = new Warehouse { BusinessId = businessId, Name = "Warehouse A" };
        var product = new Product();
        var matchingVariant = new ProductVariant
        {
            Sku = exactMatchSku,
            BasePriceNetMinor = 1500,
            Currency = "EUR",
            TaxCategoryId = taxCategory.Id
        };
        var unrelatedVariant = new ProductVariant
        {
            Sku = unrelatedSku,
            BasePriceNetMinor = 1800,
            Currency = "EUR",
            TaxCategoryId = taxCategory.Id
        };

        product.Variants.AddRange([matchingVariant, unrelatedVariant]);
        matchingVariant.ProductId = product.Id;
        unrelatedVariant.ProductId = product.Id;

        db.Set<TaxCategory>().Add(taxCategory);
        db.Set<Product>().Add(product);
        db.Set<Warehouse>().Add(warehouse);
        db.Set<StockLevel>().AddRange(
            new StockLevel
            {
                WarehouseId = warehouse.Id,
                ProductVariantId = matchingVariant.Id,
                AvailableQuantity = 12,
                ReservedQuantity = 0,
                ReorderPoint = 1,
                ReorderQuantity = 5
            },
            new StockLevel
            {
                WarehouseId = warehouse.Id,
                ProductVariantId = unrelatedVariant.Id,
                AvailableQuantity = 8,
                ReservedQuantity = 0,
                ReorderPoint = 1,
                ReorderQuantity = 5
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = scope.ServiceProvider.GetRequiredService<GetStockLevelsPageHandler>();
        var lowerCaseResult = await handler.HandleAsync(
            warehouseId: warehouse.Id,
            page: 1,
            pageSize: 20,
            query: $"stock_%_probe[{marker}]",
            filter: StockLevelQueueFilter.All,
            ct: TestContext.Current.CancellationToken);

        var upperCaseResult = await handler.HandleAsync(
            warehouseId: warehouse.Id,
            page: 1,
            pageSize: 20,
            query: $"STOCK_%_PROBE[{marker}]",
            filter: StockLevelQueueFilter.All,
            ct: TestContext.Current.CancellationToken);

        lowerCaseResult.Total.Should().Be(1);
        lowerCaseResult.Items.Should().NotContain(x => x.VariantSku == unrelatedSku);

        upperCaseResult.Total.Should().Be(1);
        upperCaseResult.Items.Should().NotContain(x => x.VariantSku == unrelatedSku);
    }

    [Fact]
    public async Task GetStockTransfersPage_Should_HandleEscapedSubstringAndCaseVariants_OnWarehouseNameSearch()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DarwinDbContext>();

        var marker = Guid.NewGuid().ToString("N");
        var exactMatchName = $"transferWarehouse_%_probe[{marker}]";
        var unrelatedName = $"transferWarehouseXprobe[{marker.Substring(0, 6)}]";
        var businessId = Guid.NewGuid();

        var fromWarehouseMatch = new Warehouse { BusinessId = businessId, Name = exactMatchName };
        var toWarehouseMatch = new Warehouse { BusinessId = businessId, Name = $"to-{exactMatchName}" };
        var fromWarehouseUnrelated = new Warehouse { BusinessId = businessId, Name = unrelatedName };
        var toWarehouseUnrelated = new Warehouse { BusinessId = businessId, Name = "to-unrelated" };

        db.Set<Warehouse>().AddRange(fromWarehouseMatch, toWarehouseMatch, fromWarehouseUnrelated, toWarehouseUnrelated);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<StockTransfer>().AddRange(
            new StockTransfer
            {
                FromWarehouseId = fromWarehouseMatch.Id,
                ToWarehouseId = toWarehouseMatch.Id,
                Status = TransferStatus.InTransit
            },
            new StockTransfer
            {
                FromWarehouseId = fromWarehouseUnrelated.Id,
                ToWarehouseId = toWarehouseUnrelated.Id,
                Status = TransferStatus.Draft
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = scope.ServiceProvider.GetRequiredService<GetStockTransfersPageHandler>();
        var lowerCaseResult = await handler.HandleAsync(
            fromWarehouseMatch.Id,
            page: 1,
            pageSize: 20,
            query: $"transferWarehouse_%_probe[{marker}]",
            filter: StockTransferQueueFilter.All,
            ct: TestContext.Current.CancellationToken);

        var upperCaseResult = await handler.HandleAsync(
            fromWarehouseMatch.Id,
            page: 1,
            pageSize: 20,
            query: $"TRANSFERWAREHOUSE_%_PROBE[{marker}]",
            filter: StockTransferQueueFilter.All,
            ct: TestContext.Current.CancellationToken);

        lowerCaseResult.Total.Should().Be(1);
        lowerCaseResult.Items.Should().ContainSingle(x => x.FromWarehouseId == fromWarehouseMatch.Id);

        upperCaseResult.Total.Should().Be(1);
        upperCaseResult.Items.Should().ContainSingle(x => x.FromWarehouseId == fromWarehouseMatch.Id);
    }

    [Fact]
    public async Task GetPurchaseOrdersPage_Should_HandleEscapedSubstringAndCaseVariants_OnOrderNumberSearch()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DarwinDbContext>();

        var marker = Guid.NewGuid().ToString("N");
        var exactMatchOrderNumber = $"po_%_probe[{marker}]";
        var unrelatedOrderNumber = $"poXprobe[{marker.Substring(0, 6)}]";
        var businessId = Guid.NewGuid();

        var supplier = new Supplier
        {
            BusinessId = businessId,
            Name = "Supplier Primary",
            Email = "supplier@example.test",
            Phone = "123"
        };
        db.Set<Supplier>().Add(supplier);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<PurchaseOrder>().AddRange(
            new PurchaseOrder
            {
                BusinessId = businessId,
                SupplierId = supplier.Id,
                OrderNumber = exactMatchOrderNumber,
                OrderedAtUtc = DateTime.UtcNow
            },
            new PurchaseOrder
            {
                BusinessId = businessId,
                SupplierId = supplier.Id,
                OrderNumber = unrelatedOrderNumber,
                OrderedAtUtc = DateTime.UtcNow
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = scope.ServiceProvider.GetRequiredService<GetPurchaseOrdersPageHandler>();
        var lowerCaseResult = await handler.HandleAsync(
            businessId,
            page: 1,
            pageSize: 20,
            query: $"po_%_probe[{marker}]",
            filter: PurchaseOrderQueueFilter.All,
            ct: TestContext.Current.CancellationToken);

        var upperCaseResult = await handler.HandleAsync(
            businessId,
            page: 1,
            pageSize: 20,
            query: $"PO_%_PROBE[{marker}]",
            filter: PurchaseOrderQueueFilter.All,
            ct: TestContext.Current.CancellationToken);

        lowerCaseResult.Total.Should().Be(1);
        lowerCaseResult.Items.Should().ContainSingle(x => x.OrderNumber == exactMatchOrderNumber);

        upperCaseResult.Total.Should().Be(1);
        upperCaseResult.Items.Should().ContainSingle(x => x.OrderNumber == exactMatchOrderNumber);
    }
}
