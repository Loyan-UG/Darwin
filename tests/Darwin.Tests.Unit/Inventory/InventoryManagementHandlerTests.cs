using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Inventory.Commands;
using Darwin.Application.Inventory.DTOs;
using Darwin.Application.Inventory.Validators;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.Inventory;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Xunit;

namespace Darwin.Tests.Unit.Inventory;

/// <summary>
/// Unit tests for the Inventory management command handlers:
/// <see cref="CreateWarehouseHandler"/>, <see cref="UpdateWarehouseHandler"/>,
/// <see cref="CreateSupplierHandler"/>, <see cref="UpdateSupplierHandler"/>,
/// <see cref="CreateStockLevelHandler"/>, <see cref="UpdateStockLevelHandler"/>,
/// <see cref="CreateStockTransferHandler"/>, <see cref="UpdateStockTransferHandler"/>,
/// <see cref="UpdateStockTransferLifecycleHandler"/>,
/// <see cref="CreatePurchaseOrderHandler"/>, <see cref="UpdatePurchaseOrderHandler"/>,
/// <see cref="UpdatePurchaseOrderLifecycleHandler"/>.
/// </summary>
public sealed class InventoryManagementHandlerTests
{
    private static IStringLocalizer<Darwin.Application.ValidationResource> CreateLocalizer()
    {
        var mock = new Moq.Mock<IStringLocalizer<Darwin.Application.ValidationResource>>();
        mock.Setup(l => l[Moq.It.IsAny<string>()])
            .Returns<string>(name => new LocalizedString(name, name));
        mock.Setup(l => l[Moq.It.IsAny<string>(), Moq.It.IsAny<object[]>()])
            .Returns<string, object[]>((name, _) => new LocalizedString(name, name));
        return mock.Object;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CreateWarehouseHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateWarehouse_Should_ThrowValidation_WhenNameIsEmpty()
    {
        await using var db = InventoryTestDbContext.Create();
        var handler = new CreateWarehouseHandler(db, new WarehouseCreateValidator());

        var dto = new WarehouseCreateDto { BusinessId = Guid.NewGuid(), Name = "" };

        var act = async () => await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("an empty name violates the validator");
    }

    [Fact]
    public async Task CreateWarehouse_Should_PersistWarehouse_WhenValid()
    {
        await using var db = InventoryTestDbContext.Create();
        var handler = new CreateWarehouseHandler(db, new WarehouseCreateValidator());
        var businessId = Guid.NewGuid();

        var newId = await handler.HandleAsync(new WarehouseCreateDto
        {
            BusinessId = businessId,
            Name = "  Main Warehouse  ",
            Description = "Primary stock location",
            Location = "Building A",
            IsDefault = false
        }, TestContext.Current.CancellationToken);

        newId.Should().NotBeEmpty();
        var saved = await db.Set<Warehouse>().SingleAsync(x => x.Id == newId, TestContext.Current.CancellationToken);
        saved.Name.Should().Be("Main Warehouse", "name should be trimmed");
        saved.BusinessId.Should().Be(businessId);
        saved.IsDefault.Should().BeFalse();
    }

    [Fact]
    public async Task CreateWarehouse_Should_ClearExistingDefaults_WhenIsDefaultTrue()
    {
        await using var db = InventoryTestDbContext.Create();
        var businessId = Guid.NewGuid();

        // Seed an existing default warehouse for the same business
        db.Set<Warehouse>().Add(new Warehouse { Id = Guid.NewGuid(), BusinessId = businessId, Name = "Old Default", IsDefault = true });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateWarehouseHandler(db, new WarehouseCreateValidator());
        await handler.HandleAsync(new WarehouseCreateDto
        {
            BusinessId = businessId,
            Name = "New Default",
            IsDefault = true
        }, TestContext.Current.CancellationToken);

        var all = db.Set<Warehouse>().ToList();
        all.Where(w => w.IsDefault).Should().ContainSingle(w => w.Name == "New Default");
        all.First(w => w.Name == "Old Default").IsDefault.Should().BeFalse("previous default must be cleared");
    }

    [Fact]
    public async Task CreateWarehouse_Should_SetNullDescription_WhenDescriptionIsWhitespace()
    {
        await using var db = InventoryTestDbContext.Create();
        var handler = new CreateWarehouseHandler(db, new WarehouseCreateValidator());

        var newId = await handler.HandleAsync(new WarehouseCreateDto
        {
            BusinessId = Guid.NewGuid(),
            Name = "WH",
            Description = "   "
        }, TestContext.Current.CancellationToken);

        var saved = await db.Set<Warehouse>().SingleAsync(x => x.Id == newId, TestContext.Current.CancellationToken);
        saved.Description.Should().BeNull("whitespace-only description should be normalized to null");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UpdateWarehouseHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateWarehouse_Should_ThrowInvalidOperation_WhenWarehouseNotFound()
    {
        await using var db = InventoryTestDbContext.Create();
        var handler = new UpdateWarehouseHandler(db, new WarehouseEditValidator(), CreateLocalizer());

        var act = async () => await handler.HandleAsync(new WarehouseEditDto
        {
            Id = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            Name = "Ghost",
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*WarehouseNotFound*");
    }

    [Fact]
    public async Task UpdateWarehouse_Should_ThrowConcurrency_WhenRowVersionMismatches()
    {
        await using var db = InventoryTestDbContext.Create();
        var id = Guid.NewGuid();
        db.Set<Warehouse>().Add(new Warehouse { Id = id, BusinessId = Guid.NewGuid(), Name = "WH", RowVersion = [1, 2, 3] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateWarehouseHandler(db, new WarehouseEditValidator(), CreateLocalizer());

        var act = async () => await handler.HandleAsync(new WarehouseEditDto
        {
            Id = id,
            BusinessId = Guid.NewGuid(),
            Name = "WH",
            RowVersion = [9, 9, 9]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>().WithMessage("*ConcurrencyConflictDetected*");
    }

    [Fact]
    public async Task UpdateWarehouse_Should_ThrowValidation_WhenRowVersionIsEmpty()
    {
        await using var db = InventoryTestDbContext.Create();
        var id = Guid.NewGuid();
        db.Set<Warehouse>().Add(new Warehouse { Id = id, BusinessId = Guid.NewGuid(), Name = "WH", RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateWarehouseHandler(db, new WarehouseEditValidator(), CreateLocalizer());

        var act = async () => await handler.HandleAsync(new WarehouseEditDto
        {
            Id = id,
            BusinessId = Guid.NewGuid(),
            Name = "WH",
            RowVersion = []
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UpdateWarehouse_Should_UpdateFields_WhenValid()
    {
        await using var db = InventoryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var id = Guid.NewGuid();
        db.Set<Warehouse>().Add(new Warehouse { Id = id, BusinessId = businessId, Name = "Old", Location = "Loc A", RowVersion = [5] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateWarehouseHandler(db, new WarehouseEditValidator(), CreateLocalizer());

        await handler.HandleAsync(new WarehouseEditDto
        {
            Id = id,
            BusinessId = businessId,
            Name = "  Updated WH  ",
            Location = "  Loc B  ",
            RowVersion = [5]
        }, TestContext.Current.CancellationToken);

        var saved = await db.Set<Warehouse>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        saved.Name.Should().Be("Updated WH");
        saved.Location.Should().Be("Loc B");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CreateSupplierHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateSupplier_Should_ThrowValidation_WhenEmailIsInvalid()
    {
        await using var db = InventoryTestDbContext.Create();
        var handler = new CreateSupplierHandler(db, new SupplierCreateValidator());

        var dto = new SupplierCreateDto
        {
            BusinessId = Guid.NewGuid(),
            Name = "Acme Corp",
            Email = "not-an-email",
            Phone = "+1234567890"
        };

        var act = async () => await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateSupplier_Should_PersistSupplier_WhenValid()
    {
        await using var db = InventoryTestDbContext.Create();
        var handler = new CreateSupplierHandler(db, new SupplierCreateValidator());
        var businessId = Guid.NewGuid();

        var newId = await handler.HandleAsync(new SupplierCreateDto
        {
            BusinessId = businessId,
            Name = "  Acme Corp  ",
            Email = "  supplier@acme.com  ",
            Phone = "  +1234567890  ",
            Address = "  123 Main St  ",
            Notes = "Preferred supplier"
        }, TestContext.Current.CancellationToken);

        newId.Should().NotBeEmpty();
        var saved = await db.Set<Supplier>().SingleAsync(x => x.Id == newId, TestContext.Current.CancellationToken);
        saved.Name.Should().Be("Acme Corp");
        saved.Email.Should().Be("supplier@acme.com", "email should be trimmed");
        saved.Phone.Should().Be("+1234567890");
        saved.Address.Should().Be("123 Main St");
        saved.Notes.Should().Be("Preferred supplier");
        saved.BusinessId.Should().Be(businessId);
    }

    [Fact]
    public async Task CreateSupplier_Should_SetNullAddress_WhenAddressIsWhitespace()
    {
        await using var db = InventoryTestDbContext.Create();
        var handler = new CreateSupplierHandler(db, new SupplierCreateValidator());

        var newId = await handler.HandleAsync(new SupplierCreateDto
        {
            BusinessId = Guid.NewGuid(),
            Name = "Supplier X",
            Email = "x@supplier.com",
            Phone = "0000000000",
            Address = "   "
        }, TestContext.Current.CancellationToken);

        var saved = await db.Set<Supplier>().SingleAsync(x => x.Id == newId, TestContext.Current.CancellationToken);
        saved.Address.Should().BeNull("whitespace-only address should normalize to null");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UpdateSupplierHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateSupplier_Should_ThrowInvalidOperation_WhenNotFound()
    {
        await using var db = InventoryTestDbContext.Create();
        var handler = new UpdateSupplierHandler(db, new SupplierEditValidator(), CreateLocalizer());

        var act = async () => await handler.HandleAsync(new SupplierEditDto
        {
            Id = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            Name = "Ghost",
            Email = "ghost@supplier.com",
            Phone = "0000",
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*SupplierNotFound*");
    }

    [Fact]
    public async Task UpdateSupplier_Should_ThrowConcurrency_WhenRowVersionMismatches()
    {
        await using var db = InventoryTestDbContext.Create();
        var id = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        db.Set<Supplier>().Add(new Supplier
        {
            Id = id, BusinessId = businessId, Name = "Sup", Email = "s@s.com", Phone = "000", RowVersion = [1, 2]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateSupplierHandler(db, new SupplierEditValidator(), CreateLocalizer());

        var act = async () => await handler.HandleAsync(new SupplierEditDto
        {
            Id = id, BusinessId = businessId, Name = "Sup Updated", Email = "s@s.com", Phone = "000",
            RowVersion = [9, 9]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>().WithMessage("*ConcurrencyConflictDetected*");
    }

    [Fact]
    public async Task UpdateSupplier_Should_UpdateFields_WhenValid()
    {
        await using var db = InventoryTestDbContext.Create();
        var id = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        db.Set<Supplier>().Add(new Supplier
        {
            Id = id, BusinessId = businessId, Name = "Old", Email = "old@s.com", Phone = "111", RowVersion = [3]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateSupplierHandler(db, new SupplierEditValidator(), CreateLocalizer());

        await handler.HandleAsync(new SupplierEditDto
        {
            Id = id, BusinessId = businessId, Name = "  New Supplier  ", Email = "new@s.com", Phone = "999",
            Address = "New Address", RowVersion = [3]
        }, TestContext.Current.CancellationToken);

        var saved = await db.Set<Supplier>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        saved.Name.Should().Be("New Supplier");
        saved.Email.Should().Be("new@s.com");
        saved.Phone.Should().Be("999");
        saved.Address.Should().Be("New Address");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CreateStockLevelHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateStockLevel_Should_ThrowInvalidOperation_WhenDuplicate()
    {
        await using var db = InventoryTestDbContext.Create();
        var warehouseId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        db.Set<Warehouse>().Add(new Warehouse { Id = warehouseId, Name = "WH", IsDefault = true });
        db.Set<ProductVariant>().Add(new ProductVariant { Id = variantId, Sku = "SKU-A" });
        db.Set<StockLevel>().Add(new StockLevel { Id = Guid.NewGuid(), WarehouseId = warehouseId, ProductVariantId = variantId });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateStockLevelHandler(db, new StockLevelCreateValidator(), CreateLocalizer());

        var act = async () => await handler.HandleAsync(new StockLevelCreateDto
        {
            WarehouseId = warehouseId,
            ProductVariantId = variantId,
            AvailableQuantity = 10
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*StockLevelAlreadyExistsForWarehouseAndVariant*");
    }

    [Fact]
    public async Task CreateStockLevel_Should_PersistStockLevel_WhenValid()
    {
        await using var db = InventoryTestDbContext.Create();
        var warehouseId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        db.Set<Warehouse>().Add(new Warehouse { Id = warehouseId, Name = "WH", IsDefault = true });
        db.Set<ProductVariant>().Add(new ProductVariant { Id = variantId, Sku = "SKU-B" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateStockLevelHandler(db, new StockLevelCreateValidator(), CreateLocalizer());

        var newId = await handler.HandleAsync(new StockLevelCreateDto
        {
            WarehouseId = warehouseId,
            ProductVariantId = variantId,
            AvailableQuantity = 25,
            ReservedQuantity = 5,
            ReorderPoint = 10,
            ReorderQuantity = 50
        }, TestContext.Current.CancellationToken);

        newId.Should().NotBeEmpty();
        var saved = await db.Set<StockLevel>().SingleAsync(x => x.Id == newId, TestContext.Current.CancellationToken);
        saved.AvailableQuantity.Should().Be(25);
        saved.ReservedQuantity.Should().Be(5);
        saved.ReorderPoint.Should().Be(10);
        saved.ReorderQuantity.Should().Be(50);
        saved.WarehouseId.Should().Be(warehouseId);
        saved.ProductVariantId.Should().Be(variantId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UpdateStockLevelHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStockLevel_Should_ThrowInvalidOperation_WhenNotFound()
    {
        await using var db = InventoryTestDbContext.Create();
        var handler = new UpdateStockLevelHandler(db, new StockLevelEditValidator(), CreateLocalizer());

        var act = async () => await handler.HandleAsync(new StockLevelEditDto
        {
            Id = Guid.NewGuid(),
            WarehouseId = Guid.NewGuid(),
            ProductVariantId = Guid.NewGuid(),
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*StockLevelNotFound*");
    }

    [Fact]
    public async Task UpdateStockLevel_Should_ThrowConcurrency_WhenRowVersionMismatches()
    {
        await using var db = InventoryTestDbContext.Create();
        var id = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        db.Set<StockLevel>().Add(new StockLevel
        {
            Id = id, WarehouseId = warehouseId, ProductVariantId = variantId,
            AvailableQuantity = 10, RowVersion = [1, 2]
        });
        db.Set<Warehouse>().Add(new Warehouse { Id = warehouseId, Name = "WH" });
        db.Set<ProductVariant>().Add(new ProductVariant { Id = variantId, Sku = "SKU-C" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateStockLevelHandler(db, new StockLevelEditValidator(), CreateLocalizer());

        var act = async () => await handler.HandleAsync(new StockLevelEditDto
        {
            Id = id, WarehouseId = warehouseId, ProductVariantId = variantId,
            AvailableQuantity = 20, RowVersion = [9, 9]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task UpdateStockLevel_Should_UpdateQuantities_WhenValid()
    {
        await using var db = InventoryTestDbContext.Create();
        var id = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        db.Set<StockLevel>().Add(new StockLevel
        {
            Id = id, WarehouseId = warehouseId, ProductVariantId = variantId,
            AvailableQuantity = 10, ReorderPoint = 5, RowVersion = [7]
        });
        db.Set<Warehouse>().Add(new Warehouse { Id = warehouseId, Name = "WH", IsDefault = true });
        db.Set<ProductVariant>().Add(new ProductVariant { Id = variantId, Sku = "SKU-D" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateStockLevelHandler(db, new StockLevelEditValidator(), CreateLocalizer());

        await handler.HandleAsync(new StockLevelEditDto
        {
            Id = id, WarehouseId = warehouseId, ProductVariantId = variantId,
            AvailableQuantity = 50, ReservedQuantity = 3, ReorderPoint = 15, ReorderQuantity = 100,
            RowVersion = [7]
        }, TestContext.Current.CancellationToken);

        var saved = await db.Set<StockLevel>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        saved.AvailableQuantity.Should().Be(50);
        saved.ReservedQuantity.Should().Be(3);
        saved.ReorderPoint.Should().Be(15);
        saved.ReorderQuantity.Should().Be(100);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CreateStockTransferHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateStockTransfer_Should_ThrowValidation_WhenSameFromAndToWarehouse()
    {
        await using var db = InventoryTestDbContext.Create();
        var warehouseId = Guid.NewGuid();
        var handler = new CreateStockTransferHandler(db, new StockTransferCreateValidator(), CreateLocalizer());

        var act = async () => await handler.HandleAsync(new StockTransferCreateDto
        {
            FromWarehouseId = warehouseId,
            ToWarehouseId = warehouseId,
            Status = "Draft",
            Lines = [new StockTransferLineDto { ProductVariantId = Guid.NewGuid(), Quantity = 1 }]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("from and to warehouse must differ");
    }

    [Fact]
    public async Task CreateStockTransfer_Should_PersistTransfer_WhenValid()
    {
        await using var db = InventoryTestDbContext.Create();
        var fromId = Guid.NewGuid();
        var toId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var handler = new CreateStockTransferHandler(db, new StockTransferCreateValidator(), CreateLocalizer());

        var newId = await handler.HandleAsync(new StockTransferCreateDto
        {
            FromWarehouseId = fromId,
            ToWarehouseId = toId,
            Status = "Draft",
            Lines = [new StockTransferLineDto { ProductVariantId = variantId, Quantity = 10 }]
        }, TestContext.Current.CancellationToken);

        newId.Should().NotBeEmpty();
        var saved = await db.Set<StockTransfer>()
            .Include(t => t.Lines)
            .SingleAsync(x => x.Id == newId, TestContext.Current.CancellationToken);
        saved.FromWarehouseId.Should().Be(fromId);
        saved.ToWarehouseId.Should().Be(toId);
        saved.Status.Should().Be(TransferStatus.Draft);
        saved.Lines.Should().ContainSingle(l => l.ProductVariantId == variantId && l.Quantity == 10);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UpdateStockTransferHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStockTransfer_Should_ThrowInvalidOperation_WhenNotFound()
    {
        await using var db = InventoryTestDbContext.Create();
        var handler = new UpdateStockTransferHandler(db, new StockTransferEditValidator(), CreateLocalizer());

        var act = async () => await handler.HandleAsync(new StockTransferEditDto
        {
            Id = Guid.NewGuid(),
            FromWarehouseId = Guid.NewGuid(),
            ToWarehouseId = Guid.NewGuid(),
            Status = "Draft",
            Lines = [new StockTransferLineDto { ProductVariantId = Guid.NewGuid(), Quantity = 1 }],
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*StockTransferNotFound*");
    }

    [Fact]
    public async Task UpdateStockTransfer_Should_ThrowConcurrency_WhenRowVersionMismatches()
    {
        await using var db = InventoryTestDbContext.Create();
        var id = Guid.NewGuid();
        var fromId = Guid.NewGuid();
        var toId = Guid.NewGuid();
        db.Set<StockTransfer>().Add(new StockTransfer
        {
            Id = id, FromWarehouseId = fromId, ToWarehouseId = toId,
            Status = TransferStatus.Draft, RowVersion = [1, 2]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateStockTransferHandler(db, new StockTransferEditValidator(), CreateLocalizer());

        var act = async () => await handler.HandleAsync(new StockTransferEditDto
        {
            Id = id, FromWarehouseId = fromId, ToWarehouseId = toId, Status = "Draft",
            Lines = [new StockTransferLineDto { ProductVariantId = Guid.NewGuid(), Quantity = 1 }],
            RowVersion = [9, 9]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UpdateStockTransferLifecycleHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task StockTransferLifecycle_Should_ReturnFail_WhenIdIsEmpty()
    {
        await using var db = InventoryTestDbContext.Create();
        var handler = new UpdateStockTransferLifecycleHandler(db, CreateLocalizer());

        var result = await handler.HandleAsync(new StockTransferLifecycleActionDto
        {
            Id = Guid.Empty,
            RowVersion = [1],
            Action = "MarkInTransit"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task StockTransferLifecycle_Should_ReturnFail_WhenRowVersionIsEmpty()
    {
        await using var db = InventoryTestDbContext.Create();
        var handler = new UpdateStockTransferLifecycleHandler(db, CreateLocalizer());

        var result = await handler.HandleAsync(new StockTransferLifecycleActionDto
        {
            Id = Guid.NewGuid(),
            RowVersion = [],
            Action = "MarkInTransit"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task StockTransferLifecycle_Should_ReturnFail_WhenTransferNotFound()
    {
        await using var db = InventoryTestDbContext.Create();
        var handler = new UpdateStockTransferLifecycleHandler(db, CreateLocalizer());

        var result = await handler.HandleAsync(new StockTransferLifecycleActionDto
        {
            Id = Guid.NewGuid(),
            RowVersion = [1],
            Action = "MarkInTransit"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task StockTransferLifecycle_Should_ReturnFail_WhenRowVersionMismatches()
    {
        await using var db = InventoryTestDbContext.Create();
        var id = Guid.NewGuid();
        db.Set<StockTransfer>().Add(new StockTransfer
        {
            Id = id, FromWarehouseId = Guid.NewGuid(), ToWarehouseId = Guid.NewGuid(),
            Status = TransferStatus.Draft, RowVersion = [1, 2]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateStockTransferLifecycleHandler(db, CreateLocalizer());

        var result = await handler.HandleAsync(new StockTransferLifecycleActionDto
        {
            Id = id, RowVersion = [9, 9], Action = "MarkInTransit"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task StockTransferLifecycle_Should_ReturnFail_WhenActionIsUnknown()
    {
        await using var db = InventoryTestDbContext.Create();
        var id = Guid.NewGuid();
        db.Set<StockTransfer>().Add(new StockTransfer
        {
            Id = id, FromWarehouseId = Guid.NewGuid(), ToWarehouseId = Guid.NewGuid(),
            Status = TransferStatus.Draft, RowVersion = [4]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateStockTransferLifecycleHandler(db, CreateLocalizer());

        var result = await handler.HandleAsync(new StockTransferLifecycleActionDto
        {
            Id = id, RowVersion = [4], Action = "DeleteNow"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task StockTransferLifecycle_MarkInTransit_Should_Succeed_WhenDraftAndSufficientStock()
    {
        await using var db = InventoryTestDbContext.Create();
        var fromId = Guid.NewGuid();
        var toId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var transferId = Guid.NewGuid();

        db.Set<Warehouse>().AddRange(
            new Warehouse { Id = fromId, Name = "From WH", IsDefault = true },
            new Warehouse { Id = toId, Name = "To WH" });
        db.Set<ProductVariant>().Add(new ProductVariant { Id = variantId, Sku = "V1" });
        db.Set<StockLevel>().Add(new StockLevel
        {
            Id = Guid.NewGuid(), WarehouseId = fromId, ProductVariantId = variantId, AvailableQuantity = 20
        });
        db.Set<StockTransfer>().Add(new StockTransfer
        {
            Id = transferId, FromWarehouseId = fromId, ToWarehouseId = toId,
            Status = TransferStatus.Draft,
            Lines = [new StockTransferLine { ProductVariantId = variantId, Quantity = 5 }],
            RowVersion = [1]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateStockTransferLifecycleHandler(db, CreateLocalizer());

        var result = await handler.HandleAsync(new StockTransferLifecycleActionDto
        {
            Id = transferId, RowVersion = [1], Action = "MarkInTransit"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var transfer = await db.Set<StockTransfer>().SingleAsync(x => x.Id == transferId, TestContext.Current.CancellationToken);
        transfer.Status.Should().Be(TransferStatus.InTransit);
        var stockLevel = db.Set<StockLevel>().Single(s => s.WarehouseId == fromId && s.ProductVariantId == variantId);
        stockLevel.AvailableQuantity.Should().Be(15, "5 units dispatched from source");
        stockLevel.InTransitQuantity.Should().Be(5);
    }

    [Fact]
    public async Task StockTransferLifecycle_MarkInTransit_Should_ReturnFail_WhenInsufficientStock()
    {
        await using var db = InventoryTestDbContext.Create();
        var fromId = Guid.NewGuid();
        var toId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var transferId = Guid.NewGuid();

        db.Set<Warehouse>().AddRange(
            new Warehouse { Id = fromId, Name = "From WH", IsDefault = true },
            new Warehouse { Id = toId, Name = "To WH" });
        db.Set<ProductVariant>().Add(new ProductVariant { Id = variantId, Sku = "V2" });
        db.Set<StockLevel>().Add(new StockLevel
        {
            Id = Guid.NewGuid(), WarehouseId = fromId, ProductVariantId = variantId, AvailableQuantity = 3
        });
        db.Set<StockTransfer>().Add(new StockTransfer
        {
            Id = transferId, FromWarehouseId = fromId, ToWarehouseId = toId,
            Status = TransferStatus.Draft,
            Lines = [new StockTransferLine { ProductVariantId = variantId, Quantity = 10 }],
            RowVersion = [2]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateStockTransferLifecycleHandler(db, CreateLocalizer());

        var result = await handler.HandleAsync(new StockTransferLifecycleActionDto
        {
            Id = transferId, RowVersion = [2], Action = "MarkInTransit"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("not enough available stock");
    }

    [Fact]
    public async Task StockTransferLifecycle_MarkInTransit_Should_ReturnFail_WhenNotDraft()
    {
        await using var db = InventoryTestDbContext.Create();
        var id = Guid.NewGuid();
        db.Set<StockTransfer>().Add(new StockTransfer
        {
            Id = id, FromWarehouseId = Guid.NewGuid(), ToWarehouseId = Guid.NewGuid(),
            Status = TransferStatus.Cancelled, RowVersion = [3]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateStockTransferLifecycleHandler(db, CreateLocalizer());

        var result = await handler.HandleAsync(new StockTransferLifecycleActionDto
        {
            Id = id, RowVersion = [3], Action = "MarkInTransit"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("can only mark in-transit a Draft transfer");
    }

    [Fact]
    public async Task StockTransferLifecycle_Cancel_Should_Succeed_WhenDraft()
    {
        await using var db = InventoryTestDbContext.Create();
        var id = Guid.NewGuid();
        db.Set<StockTransfer>().Add(new StockTransfer
        {
            Id = id, FromWarehouseId = Guid.NewGuid(), ToWarehouseId = Guid.NewGuid(),
            Status = TransferStatus.Draft, RowVersion = [5]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateStockTransferLifecycleHandler(db, CreateLocalizer());

        var result = await handler.HandleAsync(new StockTransferLifecycleActionDto
        {
            Id = id, RowVersion = [5], Action = "cancel"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var transfer = await db.Set<StockTransfer>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        transfer.Status.Should().Be(TransferStatus.Cancelled);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CreatePurchaseOrderHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreatePurchaseOrder_Should_ThrowValidation_WhenOrderNumberIsEmpty()
    {
        await using var db = InventoryTestDbContext.Create();
        var handler = new CreatePurchaseOrderHandler(db, new PurchaseOrderCreateValidator(), CreateLocalizer());

        var act = async () => await handler.HandleAsync(new PurchaseOrderCreateDto
        {
            SupplierId = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            OrderNumber = "",
            Status = "Draft",
            Lines = [new PurchaseOrderLineDto { ProductVariantId = Guid.NewGuid(), Quantity = 1 }]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreatePurchaseOrder_Should_PersistOrder_WhenValid()
    {
        await using var db = InventoryTestDbContext.Create();
        var supplierId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var handler = new CreatePurchaseOrderHandler(db, new PurchaseOrderCreateValidator(), CreateLocalizer());

        var newId = await handler.HandleAsync(new PurchaseOrderCreateDto
        {
            SupplierId = supplierId,
            BusinessId = businessId,
            OrderNumber = "  PO-001  ",
            OrderedAtUtc = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            Status = "Draft",
            Lines = [new PurchaseOrderLineDto { ProductVariantId = variantId, Quantity = 20, UnitCostMinor = 500, TotalCostMinor = 10000 }]
        }, TestContext.Current.CancellationToken);

        newId.Should().NotBeEmpty();
        var saved = await db.Set<PurchaseOrder>()
            .Include(o => o.Lines)
            .SingleAsync(x => x.Id == newId, TestContext.Current.CancellationToken);
        saved.OrderNumber.Should().Be("PO-001", "order number should be trimmed");
        saved.SupplierId.Should().Be(supplierId);
        saved.BusinessId.Should().Be(businessId);
        saved.Status.Should().Be(PurchaseOrderStatus.Draft);
        saved.Lines.Should().ContainSingle(l => l.Quantity == 20 && l.UnitCostMinor == 500);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UpdatePurchaseOrderHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdatePurchaseOrder_Should_ThrowInvalidOperation_WhenNotFound()
    {
        await using var db = InventoryTestDbContext.Create();
        var handler = new UpdatePurchaseOrderHandler(db, new PurchaseOrderEditValidator(), CreateLocalizer());

        var act = async () => await handler.HandleAsync(new PurchaseOrderEditDto
        {
            Id = Guid.NewGuid(),
            SupplierId = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            OrderNumber = "PO-X",
            Status = "Draft",
            Lines = [new PurchaseOrderLineDto { ProductVariantId = Guid.NewGuid(), Quantity = 1 }],
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*PurchaseOrderNotFound*");
    }

    [Fact]
    public async Task UpdatePurchaseOrder_Should_ThrowConcurrency_WhenRowVersionMismatches()
    {
        await using var db = InventoryTestDbContext.Create();
        var id = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        db.Set<PurchaseOrder>().Add(new PurchaseOrder
        {
            Id = id, SupplierId = supplierId, BusinessId = businessId,
            OrderNumber = "PO-1", Status = PurchaseOrderStatus.Draft, RowVersion = [1, 2]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdatePurchaseOrderHandler(db, new PurchaseOrderEditValidator(), CreateLocalizer());

        var act = async () => await handler.HandleAsync(new PurchaseOrderEditDto
        {
            Id = id, SupplierId = supplierId, BusinessId = businessId, OrderNumber = "PO-1", Status = "Draft",
            Lines = [new PurchaseOrderLineDto { ProductVariantId = Guid.NewGuid(), Quantity = 1 }],
            RowVersion = [9, 9]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UpdatePurchaseOrderLifecycleHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PurchaseOrderLifecycle_Should_ReturnFail_WhenIdIsEmpty()
    {
        await using var db = InventoryTestDbContext.Create();
        var handler = new UpdatePurchaseOrderLifecycleHandler(db, CreateLocalizer());

        var result = await handler.HandleAsync(new PurchaseOrderLifecycleActionDto
        {
            Id = Guid.Empty, RowVersion = [1], Action = "Issue"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task PurchaseOrderLifecycle_Should_ReturnFail_WhenRowVersionIsEmpty()
    {
        await using var db = InventoryTestDbContext.Create();
        var handler = new UpdatePurchaseOrderLifecycleHandler(db, CreateLocalizer());

        var result = await handler.HandleAsync(new PurchaseOrderLifecycleActionDto
        {
            Id = Guid.NewGuid(), RowVersion = [], Action = "Issue"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task PurchaseOrderLifecycle_Should_ReturnFail_WhenOrderNotFound()
    {
        await using var db = InventoryTestDbContext.Create();
        var handler = new UpdatePurchaseOrderLifecycleHandler(db, CreateLocalizer());

        var result = await handler.HandleAsync(new PurchaseOrderLifecycleActionDto
        {
            Id = Guid.NewGuid(), RowVersion = [1], Action = "Issue"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task PurchaseOrderLifecycle_Issue_Should_Succeed_WhenDraft()
    {
        await using var db = InventoryTestDbContext.Create();
        var id = Guid.NewGuid();
        db.Set<PurchaseOrder>().Add(new PurchaseOrder
        {
            Id = id, SupplierId = Guid.NewGuid(), BusinessId = Guid.NewGuid(),
            OrderNumber = "PO-2", Status = PurchaseOrderStatus.Draft, RowVersion = [8]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdatePurchaseOrderLifecycleHandler(db, CreateLocalizer());

        var result = await handler.HandleAsync(new PurchaseOrderLifecycleActionDto
        {
            Id = id, RowVersion = [8], Action = "issue"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var saved = await db.Set<PurchaseOrder>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        saved.Status.Should().Be(PurchaseOrderStatus.Issued);
    }

    [Fact]
    public async Task PurchaseOrderLifecycle_Issue_Should_ReturnFail_WhenNotDraft()
    {
        await using var db = InventoryTestDbContext.Create();
        var id = Guid.NewGuid();
        db.Set<PurchaseOrder>().Add(new PurchaseOrder
        {
            Id = id, SupplierId = Guid.NewGuid(), BusinessId = Guid.NewGuid(),
            OrderNumber = "PO-3", Status = PurchaseOrderStatus.Received, RowVersion = [2]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdatePurchaseOrderLifecycleHandler(db, CreateLocalizer());

        var result = await handler.HandleAsync(new PurchaseOrderLifecycleActionDto
        {
            Id = id, RowVersion = [2], Action = "Issue"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("cannot issue a non-Draft order");
    }

    [Fact]
    public async Task PurchaseOrderLifecycle_Cancel_Should_Succeed_WhenIssued()
    {
        await using var db = InventoryTestDbContext.Create();
        var id = Guid.NewGuid();
        db.Set<PurchaseOrder>().Add(new PurchaseOrder
        {
            Id = id, SupplierId = Guid.NewGuid(), BusinessId = Guid.NewGuid(),
            OrderNumber = "PO-4", Status = PurchaseOrderStatus.Issued, RowVersion = [6]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdatePurchaseOrderLifecycleHandler(db, CreateLocalizer());

        var result = await handler.HandleAsync(new PurchaseOrderLifecycleActionDto
        {
            Id = id, RowVersion = [6], Action = "CANCEL"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var saved = await db.Set<PurchaseOrder>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        saved.Status.Should().Be(PurchaseOrderStatus.Cancelled);
    }

    [Fact]
    public async Task PurchaseOrderLifecycle_Cancel_Should_ReturnFail_WhenAlreadyReceived()
    {
        await using var db = InventoryTestDbContext.Create();
        var id = Guid.NewGuid();
        db.Set<PurchaseOrder>().Add(new PurchaseOrder
        {
            Id = id, SupplierId = Guid.NewGuid(), BusinessId = Guid.NewGuid(),
            OrderNumber = "PO-5", Status = PurchaseOrderStatus.Received, RowVersion = [9]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdatePurchaseOrderLifecycleHandler(db, CreateLocalizer());

        var result = await handler.HandleAsync(new PurchaseOrderLifecycleActionDto
        {
            Id = id, RowVersion = [9], Action = "Cancel"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("cannot cancel a Received order");
    }

    [Fact]
    public async Task PurchaseOrderLifecycle_Should_ReturnFail_WhenActionIsUnknown()
    {
        await using var db = InventoryTestDbContext.Create();
        var id = Guid.NewGuid();
        db.Set<PurchaseOrder>().Add(new PurchaseOrder
        {
            Id = id, SupplierId = Guid.NewGuid(), BusinessId = Guid.NewGuid(),
            OrderNumber = "PO-6", Status = PurchaseOrderStatus.Draft, RowVersion = [1]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdatePurchaseOrderLifecycleHandler(db, CreateLocalizer());

        var result = await handler.HandleAsync(new PurchaseOrderLifecycleActionDto
        {
            Id = id, RowVersion = [1], Action = "Approve"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("unknown actions must be rejected");
    }

    [Fact]
    public async Task PurchaseOrderLifecycle_Receive_Should_Succeed_WhenIssuedAndWarehouseExists()
    {
        await using var db = InventoryTestDbContext.Create();
        var id = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        db.Set<Warehouse>().Add(new Warehouse { Id = warehouseId, BusinessId = businessId, Name = "Main WH", IsDefault = true });
        db.Set<ProductVariant>().Add(new ProductVariant { Id = variantId, Sku = "SKU-PO" });
        db.Set<PurchaseOrder>().Add(new PurchaseOrder
        {
            Id = id, SupplierId = Guid.NewGuid(), BusinessId = businessId,
            OrderNumber = "PO-7", Status = PurchaseOrderStatus.Issued,
            Lines = [new PurchaseOrderLine { ProductVariantId = variantId, Quantity = 30, UnitCostMinor = 100, TotalCostMinor = 3000 }],
            RowVersion = [4]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdatePurchaseOrderLifecycleHandler(db, CreateLocalizer());

        var result = await handler.HandleAsync(new PurchaseOrderLifecycleActionDto
        {
            Id = id, RowVersion = [4], Action = "Receive"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var saved = await db.Set<PurchaseOrder>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        saved.Status.Should().Be(PurchaseOrderStatus.Received);
        var stockLevel = db.Set<StockLevel>().Single(s => s.ProductVariantId == variantId);
        stockLevel.AvailableQuantity.Should().Be(30, "30 units received from purchase order");
        var transaction = db.Set<InventoryTransaction>().Single(t => t.ProductVariantId == variantId);
        transaction.Reason.Should().Be("PurchaseOrderReceived");
        transaction.QuantityDelta.Should().Be(30);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private test infrastructure
    // ─────────────────────────────────────────────────────────────────────────

    private sealed class InventoryTestDbContext : DbContext, IAppDbContext
    {
        private InventoryTestDbContext(DbContextOptions<InventoryTestDbContext> options)
            : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static InventoryTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<InventoryTestDbContext>()
                .UseInMemoryDatabase($"darwin_inventory_handler_tests_{Guid.NewGuid()}")
                .Options;
            return new InventoryTestDbContext(options);
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
                b.Ignore(x => x.PurchaseOrders);
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
        }
    }
}
