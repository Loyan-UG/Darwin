using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Foundation;
using Darwin.Application.Inventory;
using Darwin.Application.Inventory.Commands;
using Darwin.Application.Inventory.DTOs;
using Darwin.Application.Inventory.Queries;
using Darwin.Application.Inventory.Validators;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Inventory;
using Darwin.Domain.Entities.Orders;
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

    [Fact]
    public async Task WarehouseLocationCreate_Should_NormalizeHierarchy_AndRejectDuplicateCode()
    {
        await using var db = InventoryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        db.Set<Warehouse>().Add(new Warehouse { Id = warehouseId, BusinessId = businessId, Name = "Main" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateWarehouseLocationHandler(db, new WarehouseLocationCreateValidator(), CreateLocalizer(), new FixedClock());

        var parentId = await handler.HandleAsync(new WarehouseLocationCreateDto
        {
            BusinessId = businessId,
            WarehouseId = warehouseId,
            Code = " zone-a ",
            DisplayName = " Zone A ",
            LocationType = WarehouseLocationType.Zone,
            Status = WarehouseLocationStatus.Active,
            Barcode = " ZA-01 "
        }, TestContext.Current.CancellationToken);

        var childId = await handler.HandleAsync(new WarehouseLocationCreateDto
        {
            BusinessId = businessId,
            WarehouseId = warehouseId,
            ParentLocationId = parentId,
            Code = " bin-01 ",
            DisplayName = " Bin 01 ",
            LocationType = WarehouseLocationType.Bin,
            Status = WarehouseLocationStatus.Active
        }, TestContext.Current.CancellationToken);

        var child = await db.Set<WarehouseLocation>().SingleAsync(x => x.Id == childId, TestContext.Current.CancellationToken);
        child.Code.Should().Be("BIN-01");
        child.ParentLocationId.Should().Be(parentId);

        var duplicate = async () => await handler.HandleAsync(new WarehouseLocationCreateDto
        {
            BusinessId = businessId,
            WarehouseId = warehouseId,
            Code = "BIN-01",
            DisplayName = "Duplicate",
            LocationType = WarehouseLocationType.Bin,
            Status = WarehouseLocationStatus.Active
        }, TestContext.Current.CancellationToken);

        await duplicate.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*WarehouseLocationCodeAlreadyExists*");
    }

    [Fact]
    public async Task WarehouseLocationUpdate_Should_RejectParentCycle_AndSensitiveMetadata()
    {
        await using var db = InventoryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        db.Set<Warehouse>().Add(new Warehouse { Id = warehouseId, BusinessId = businessId, Name = "Main" });
        db.Set<WarehouseLocation>().AddRange(
            new WarehouseLocation { Id = parentId, BusinessId = businessId, WarehouseId = warehouseId, Code = "ZONE", DisplayName = "Zone", LocationType = WarehouseLocationType.Zone, Status = WarehouseLocationStatus.Active, RowVersion = [1] },
            new WarehouseLocation { Id = childId, BusinessId = businessId, WarehouseId = warehouseId, ParentLocationId = parentId, Code = "BIN", DisplayName = "Bin", LocationType = WarehouseLocationType.Bin, Status = WarehouseLocationStatus.Active, RowVersion = [2] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateWarehouseLocationHandler(db, new WarehouseLocationEditValidator(), CreateLocalizer(), new FixedClock());

        var cycle = async () => await handler.HandleAsync(new WarehouseLocationEditDto
        {
            Id = parentId,
            RowVersion = [1],
            BusinessId = businessId,
            WarehouseId = warehouseId,
            ParentLocationId = childId,
            Code = "ZONE",
            DisplayName = "Zone",
            LocationType = WarehouseLocationType.Zone,
            Status = WarehouseLocationStatus.Active
        }, TestContext.Current.CancellationToken);

        await cycle.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*WarehouseLocationParentInvalid*");

        var sensitive = async () => await handler.HandleAsync(new WarehouseLocationEditDto
        {
            Id = childId,
            RowVersion = [2],
            BusinessId = businessId,
            WarehouseId = warehouseId,
            ParentLocationId = parentId,
            Code = "BIN",
            DisplayName = "Bin",
            LocationType = WarehouseLocationType.Bin,
            Status = WarehouseLocationStatus.Active,
            MetadataJson = "{\"apiToken\":\"secret\"}"
        }, TestContext.Current.CancellationToken);

        await sensitive.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*WarehouseLocationSensitiveMetadataRejected*");
    }

    [Fact]
    public async Task WarehouseLocationArchive_Should_Block_WhenChildLocationsExist()
    {
        await using var db = InventoryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        db.Set<Warehouse>().Add(new Warehouse { Id = warehouseId, BusinessId = businessId, Name = "Main" });
        db.Set<WarehouseLocation>().AddRange(
            new WarehouseLocation { Id = parentId, BusinessId = businessId, WarehouseId = warehouseId, Code = "ZONE", DisplayName = "Zone", LocationType = WarehouseLocationType.Zone, Status = WarehouseLocationStatus.Active, RowVersion = [1] },
            new WarehouseLocation { Id = Guid.NewGuid(), BusinessId = businessId, WarehouseId = warehouseId, ParentLocationId = parentId, Code = "BIN", DisplayName = "Bin", LocationType = WarehouseLocationType.Bin, Status = WarehouseLocationStatus.Active });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ArchiveWarehouseLocationHandler(db, CreateLocalizer(), new FixedClock());

        var result = await handler.HandleAsync(new WarehouseLocationArchiveDto
        {
            Id = parentId,
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("WarehouseLocationArchiveHasChildren");
        (await db.Set<WarehouseLocation>().SingleAsync(x => x.Id == parentId, TestContext.Current.CancellationToken)).IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task WarehouseLabelTemplateCreate_Should_NormalizeDefault_AndRejectDuplicateKey()
    {
        await using var db = InventoryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        db.Set<WarehouseLabelTemplate>().Add(new WarehouseLabelTemplate
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Name = "Old",
            TemplateKey = "OLD",
            Status = WarehouseLabelTemplateStatus.Active,
            Format = WarehouseLabelTemplateFormat.Html,
            IsDefault = true,
            ContentTemplate = "{Code}"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateWarehouseLabelTemplateHandler(db, new WarehouseLabelTemplateCreateValidator(), CreateLocalizer());
        var id = await handler.HandleAsync(new WarehouseLabelTemplateCreateDto
        {
            BusinessId = businessId,
            Name = " Bin Label ",
            TemplateKey = " bin-label ",
            Status = WarehouseLabelTemplateStatus.Active,
            Format = WarehouseLabelTemplateFormat.Html,
            IsDefault = true,
            WidthMm = 70,
            HeightMm = 35,
            ContentTemplate = "<strong>{Code}</strong>{Barcode}"
        }, TestContext.Current.CancellationToken);

        var saved = await db.Set<WarehouseLabelTemplate>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        saved.TemplateKey.Should().Be("BIN-LABEL");
        saved.IsDefault.Should().BeTrue();
        db.Set<WarehouseLabelTemplate>().Single(x => x.TemplateKey == "OLD").IsDefault.Should().BeFalse();

        var duplicate = async () => await handler.HandleAsync(new WarehouseLabelTemplateCreateDto
        {
            BusinessId = businessId,
            Name = "Duplicate",
            TemplateKey = "BIN-LABEL",
            Status = WarehouseLabelTemplateStatus.Active,
            Format = WarehouseLabelTemplateFormat.Html,
            WidthMm = 70,
            HeightMm = 35,
            ContentTemplate = "{Code}"
        }, TestContext.Current.CancellationToken);

        await duplicate.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*WarehouseLabelTemplateKeyAlreadyExists*");
    }

    [Fact]
    public async Task WarehouseLabelTemplateUpdate_Should_RequireRowVersion_AndRejectSensitiveContent()
    {
        await using var db = InventoryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var id = Guid.NewGuid();
        db.Set<WarehouseLabelTemplate>().Add(new WarehouseLabelTemplate
        {
            Id = id,
            BusinessId = businessId,
            Name = "Label",
            TemplateKey = "LABEL",
            Status = WarehouseLabelTemplateStatus.Active,
            Format = WarehouseLabelTemplateFormat.Html,
            WidthMm = 70,
            HeightMm = 35,
            ContentTemplate = "{Code}",
            RowVersion = [1]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateWarehouseLabelTemplateHandler(db, new WarehouseLabelTemplateEditValidator(), CreateLocalizer());
        var sensitive = async () => await handler.HandleAsync(new WarehouseLabelTemplateEditDto
        {
            Id = id,
            RowVersion = [1],
            BusinessId = businessId,
            Name = "Label",
            TemplateKey = "LABEL",
            Status = WarehouseLabelTemplateStatus.Active,
            Format = WarehouseLabelTemplateFormat.Html,
            WidthMm = 70,
            HeightMm = 35,
            ContentTemplate = "{Code} apiToken"
        }, TestContext.Current.CancellationToken);

        await sensitive.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*WarehouseLabelTemplateSensitiveMetadataRejected*");
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
    public async Task UpdateSupplier_Should_ThrowValidation_WhenRowVersionIsEmpty()
    {
        await using var db = InventoryTestDbContext.Create();
        var id = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        db.Set<Supplier>().Add(new Supplier { Id = id, BusinessId = businessId, Name = "Sup", Email = "s@s.com", Phone = "000", RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateSupplierHandler(db, new SupplierEditValidator(), CreateLocalizer());

        var act = async () => await handler.HandleAsync(new SupplierEditDto
        {
            Id = id,
            BusinessId = businessId,
            Name = "Sup Updated",
            Email = "s@s.com",
            Phone = "000",
            RowVersion = []
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>();
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

    [Fact]
    public async Task SupplierContact_Create_Should_NormalizeAndPersistContact()
    {
        await using var db = InventoryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        db.Set<Supplier>().Add(new Supplier { Id = supplierId, BusinessId = businessId, Name = "Supplier", Email = "s@example.test", Phone = "1" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var id = await new CreateSupplierContactHandler(db).HandleAsync(new SupplierContactEditDto
        {
            BusinessId = businessId,
            SupplierId = supplierId,
            Role = SupplierContactRole.Purchasing,
            Name = "  Jane Buyer  ",
            Email = "  jane@example.test  ",
            Phone = "  +49 30 123  ",
            LanguageCode = " de ",
            IsPrimary = true
        }, TestContext.Current.CancellationToken);

        var saved = await db.Set<SupplierContact>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        saved.Name.Should().Be("Jane Buyer");
        saved.Email.Should().Be("jane@example.test");
        saved.Phone.Should().Be("+49 30 123");
        saved.LanguageCode.Should().Be("DE");
        saved.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task SupplierContact_Create_Should_RejectDuplicateEmailAndSensitiveNotes()
    {
        await using var db = InventoryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        db.Set<Supplier>().Add(new Supplier { Id = supplierId, BusinessId = businessId, Name = "Supplier", Email = "s@example.test", Phone = "1" });
        db.Set<SupplierContact>().Add(new SupplierContact
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            SupplierId = supplierId,
            Role = SupplierContactRole.Purchasing,
            Name = "Existing",
            Email = "ops@example.test"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateSupplierContactHandler(db);

        var duplicate = async () => await handler.HandleAsync(new SupplierContactEditDto
        {
            BusinessId = businessId,
            SupplierId = supplierId,
            Role = SupplierContactRole.AccountsPayable,
            Name = "Duplicate",
            Email = "ops@example.test"
        }, TestContext.Current.CancellationToken);

        var sensitive = async () => await handler.HandleAsync(new SupplierContactEditDto
        {
            BusinessId = businessId,
            SupplierId = supplierId,
            Name = "Secret",
            Phone = "1",
            Notes = "token=secret"
        }, TestContext.Current.CancellationToken);

        await duplicate.Should().ThrowAsync<InvalidOperationException>().WithMessage("*DuplicateEmail*");
        await sensitive.Should().ThrowAsync<ArgumentException>().WithMessage("*SensitiveMetadataRejected*");
    }

    [Fact]
    public async Task SupplierDocument_Register_Should_CreateDocumentRecordAndRejectSensitiveMetadata()
    {
        await using var db = InventoryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        db.Set<Supplier>().Add(new Supplier { Id = supplierId, BusinessId = businessId, Name = "Supplier", Email = "s@example.test", Phone = "1" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new RegisterSupplierDocumentHandler(db, new DocumentRecordService(db));
        var id = await handler.HandleAsync(new SupplierDocumentRegisterDto
        {
            BusinessId = businessId,
            SupplierId = supplierId,
            DocumentKind = DocumentRecordKind.Attachment,
            Title = "Supplier certificate",
            FileName = "certificate.pdf",
            ContentType = "application/pdf",
            SizeBytes = 1234,
            StorageProvider = "External",
            StorageContainer = "supplier-documents",
            StorageKey = "suppliers/certificate.pdf",
            Visibility = FoundationVisibility.Internal,
            MetadataJson = "{\"source\":\"operator\"}"
        }, TestContext.Current.CancellationToken);

        var saved = await db.Set<DocumentRecord>().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        saved.EntityType.Should().Be("Supplier");
        saved.EntityId.Should().Be(supplierId);
        saved.FileName.Should().Be("certificate.pdf");

        var sensitive = async () => await handler.HandleAsync(new SupplierDocumentRegisterDto
        {
            BusinessId = businessId,
            SupplierId = supplierId,
            Title = "Supplier certificate",
            FileName = "certificate.pdf",
            ContentType = "application/pdf",
            StorageProvider = "External",
            StorageContainer = "supplier-documents",
            StorageKey = "token=secret"
        }, TestContext.Current.CancellationToken);
        await sensitive.Should().ThrowAsync<ArgumentException>().WithMessage("*SensitiveMetadataRejected*");
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
    public async Task UpdateStockLevel_Should_ThrowValidation_WhenRowVersionIsEmpty()
    {
        await using var db = InventoryTestDbContext.Create();
        var handler = new UpdateStockLevelHandler(db, new StockLevelEditValidator(), CreateLocalizer());

        var act = async () => await handler.HandleAsync(new StockLevelEditDto
        {
            Id = Guid.NewGuid(),
            WarehouseId = Guid.NewGuid(),
            ProductVariantId = Guid.NewGuid(),
            RowVersion = []
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>();
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

    [Fact]
    public async Task UpdateStockTransfer_Should_ThrowValidation_WhenRowVersionIsEmpty()
    {
        await using var db = InventoryTestDbContext.Create();
        var id = Guid.NewGuid();
        var fromId = Guid.NewGuid();
        var toId = Guid.NewGuid();
        db.Set<StockTransfer>().Add(new StockTransfer
        {
            Id = id, FromWarehouseId = fromId, ToWarehouseId = toId, Status = TransferStatus.Draft, RowVersion = [1]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateStockTransferHandler(db, new StockTransferEditValidator(), CreateLocalizer());

        var act = async () => await handler.HandleAsync(new StockTransferEditDto
        {
            Id = id,
            FromWarehouseId = fromId,
            ToWarehouseId = toId,
            Status = "Draft",
            Lines = [new StockTransferLineDto { ProductVariantId = Guid.NewGuid(), Quantity = 1 }],
            RowVersion = []
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>();
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
    public async Task CreatePurchaseOrder_Should_ReserveSequence_WhenOrderNumberIsEmpty()
    {
        await using var db = InventoryTestDbContext.Create();
        var supplierId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        db.Set<Supplier>().Add(new Supplier
        {
            Id = supplierId,
            BusinessId = businessId,
            Name = "Supplier",
            Email = "supplier@example.test",
            Phone = "123",
            Status = SupplierStatus.Active
        });
        db.Set<NumberSequence>().Add(new NumberSequence
        {
            BusinessId = businessId,
            DocumentType = NumberSequenceDocumentType.PurchaseOrder,
            ScopeKey = "GLOBAL",
            PrefixPattern = "PO-{seq}",
            NextValue = 7,
            PaddingLength = 4,
            ResetPolicy = NumberSequenceResetPolicy.Never,
            IsActive = true
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreatePurchaseOrderHandler(db, new PurchaseOrderCreateValidator(), CreateLocalizer());

        var newId = await handler.HandleAsync(new PurchaseOrderCreateDto
        {
            SupplierId = supplierId,
            BusinessId = businessId,
            OrderNumber = "",
            Status = "Draft",
            Lines = [new PurchaseOrderLineDto { ProductVariantId = Guid.NewGuid(), Quantity = 1 }]
        }, TestContext.Current.CancellationToken);

        var saved = await db.Set<PurchaseOrder>().SingleAsync(x => x.Id == newId, TestContext.Current.CancellationToken);
        saved.OrderNumber.Should().Be("PO-0007");
    }

    [Fact]
    public async Task CreatePurchaseOrder_Should_PersistOrder_WhenValid()
    {
        await using var db = InventoryTestDbContext.Create();
        var supplierId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        db.Set<Supplier>().Add(new Supplier
        {
            Id = supplierId,
            BusinessId = businessId,
            Name = "Supplier",
            Email = "supplier@example.test",
            Phone = "123",
            Status = SupplierStatus.Active
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

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
        db.Set<Supplier>().Add(new Supplier
        {
            Id = supplierId,
            BusinessId = businessId,
            Name = "Supplier",
            Email = "supplier@example.test",
            Phone = "123",
            Status = SupplierStatus.Active
        });
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

    [Fact]
    public async Task UpdatePurchaseOrder_Should_ThrowValidation_WhenRowVersionIsEmpty()
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
            RowVersion = []
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>();
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
        transaction.QuantityDelta.Should().Be(30);
        var receipt = await db.Set<GoodsReceipt>()
            .Include(x => x.Lines)
            .SingleAsync(x => x.PurchaseOrderId == id, TestContext.Current.CancellationToken);
        receipt.Status.Should().Be(GoodsReceiptStatus.Posted);
        receipt.WarehouseId.Should().Be(warehouseId);
        receipt.Lines.Should().ContainSingle(x => x.AcceptedQuantity == 30);
        transaction.Reason.Should().Be(UpdateGoodsReceiptLifecycleHandler.PostedReason);
        transaction.ReferenceId.Should().Be(receipt.Id);
    }

    [Fact]
    public async Task GoodsReceiptCreate_Should_UseRemainingPurchaseOrderQuantities()
    {
        await using var db = InventoryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var purchaseOrderId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        db.Set<Warehouse>().Add(new Warehouse { Id = warehouseId, BusinessId = businessId, Name = "Receiving WH" });
        db.Set<PurchaseOrder>().Add(new PurchaseOrder
        {
            Id = purchaseOrderId,
            BusinessId = businessId,
            SupplierId = Guid.NewGuid(),
            OrderNumber = "PO-GR-1",
            Status = PurchaseOrderStatus.Issued,
            RowVersion = [1],
            Lines =
            [
                new PurchaseOrderLine
                {
                    Id = lineId,
                    ProductVariantId = variantId,
                    SupplierSku = " SUP-1 ",
                    Description = "  Test item  ",
                    Quantity = 10,
                    ReceivedQuantity = 4,
                    CancelledQuantity = 1,
                    UnitCostMinor = 250,
                    TotalCostMinor = 2500
                }
            ]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateGoodsReceiptFromPurchaseOrderHandler(
            db,
            new GoodsReceiptCreateValidator(),
            CreateLocalizer());

        var result = await handler.HandleAsync(new GoodsReceiptCreateDto
        {
            PurchaseOrderId = purchaseOrderId,
            WarehouseId = warehouseId,
            InternalNotes = "  Dock A  "
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var receipt = await db.Set<GoodsReceipt>()
            .Include(x => x.Lines)
            .SingleAsync(x => x.Id == result.Value, TestContext.Current.CancellationToken);
        receipt.Status.Should().Be(GoodsReceiptStatus.Draft);
        receipt.BusinessId.Should().Be(businessId);
        receipt.InternalNotes.Should().Be("Dock A");
        receipt.Lines.Should().ContainSingle();
        var line = receipt.Lines.Single();
        line.PurchaseOrderLineId.Should().Be(lineId);
        line.ProductVariantId.Should().Be(variantId);
        line.SupplierSku.Should().Be("SUP-1");
        line.Description.Should().Be("Test item");
        line.OrderedQuantity.Should().Be(10);
        line.PreviouslyReceivedQuantity.Should().Be(4);
        line.ReceivedQuantity.Should().Be(5);
        line.UnitCostMinor.Should().Be(250);
        line.TotalCostMinor.Should().Be(2500);
    }

    [Fact]
    public async Task GoodsReceiptLifecycle_Should_PostAcceptedQuantityOnly_AndRemainIdempotent()
    {
        await using var db = InventoryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var purchaseOrderId = Guid.NewGuid();
        var purchaseOrderLineId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var receiptId = Guid.NewGuid();
        var receiptLineId = Guid.NewGuid();

        db.Set<Warehouse>().Add(new Warehouse { Id = warehouseId, BusinessId = businessId, Name = "Main WH" });
        db.Set<ProductVariant>().Add(new ProductVariant { Id = variantId, Sku = "GR-POST" });
        db.Set<PurchaseOrder>().Add(new PurchaseOrder
        {
            Id = purchaseOrderId,
            BusinessId = businessId,
            SupplierId = Guid.NewGuid(),
            OrderNumber = "PO-GR-POST",
            Status = PurchaseOrderStatus.Issued,
            RowVersion = [2],
            Lines =
            [
                new PurchaseOrderLine
                {
                    Id = purchaseOrderLineId,
                    ProductVariantId = variantId,
                    Quantity = 10,
                    UnitCostMinor = 100,
                    TotalCostMinor = 1000
                }
            ]
        });
        db.Set<GoodsReceipt>().Add(new GoodsReceipt
        {
            Id = receiptId,
            BusinessId = businessId,
            SupplierId = Guid.NewGuid(),
            PurchaseOrderId = purchaseOrderId,
            WarehouseId = warehouseId,
            Status = GoodsReceiptStatus.Draft,
            RowVersion = [7],
            Lines =
            [
                new GoodsReceiptLine
                {
                    Id = receiptLineId,
                    PurchaseOrderLineId = purchaseOrderLineId,
                    ProductVariantId = variantId,
                    OrderedQuantity = 10,
                    PreviouslyReceivedQuantity = 0,
                    ReceivedQuantity = 10,
                    UnitCostMinor = 100,
                    TotalCostMinor = 1000
                }
            ]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateGoodsReceiptLifecycleHandler(db, CreateLocalizer());

        var receive = await handler.HandleAsync(new GoodsReceiptLifecycleActionDto
        {
            Id = receiptId,
            RowVersion = [7],
            Action = UpdateGoodsReceiptLifecycleHandler.ReceiveAction,
            Lines = [new GoodsReceiptLineDto { Id = receiptLineId, PurchaseOrderLineId = purchaseOrderLineId, ProductVariantId = variantId, ReceivedQuantity = 10 }]
        }, TestContext.Current.CancellationToken);
        receive.Succeeded.Should().BeTrue();

        var inspect = await handler.HandleAsync(new GoodsReceiptLifecycleActionDto
        {
            Id = receiptId,
            RowVersion = [7],
            Action = UpdateGoodsReceiptLifecycleHandler.InspectAction,
            Lines =
            [
                new GoodsReceiptLineDto
                {
                    Id = receiptLineId,
                    PurchaseOrderLineId = purchaseOrderLineId,
                    ProductVariantId = variantId,
                    AcceptedQuantity = 7,
                    RejectedQuantity = 2,
                    DamagedQuantity = 1
                }
            ]
        }, TestContext.Current.CancellationToken);
        inspect.Succeeded.Should().BeTrue();

        var post = await handler.HandleAsync(new GoodsReceiptLifecycleActionDto
        {
            Id = receiptId,
            RowVersion = [7],
            Action = UpdateGoodsReceiptLifecycleHandler.PostAction
        }, TestContext.Current.CancellationToken);
        post.Succeeded.Should().BeTrue();

        var retry = await handler.HandleAsync(new GoodsReceiptLifecycleActionDto
        {
            Id = receiptId,
            RowVersion = [7],
            Action = UpdateGoodsReceiptLifecycleHandler.PostAction
        }, TestContext.Current.CancellationToken);
        retry.Succeeded.Should().BeTrue();

        var stock = await db.Set<StockLevel>().SingleAsync(x => x.WarehouseId == warehouseId && x.ProductVariantId == variantId, TestContext.Current.CancellationToken);
        stock.AvailableQuantity.Should().Be(7);
        db.Set<InventoryTransaction>()
            .Where(x => x.ReferenceId == receiptId && x.Reason == UpdateGoodsReceiptLifecycleHandler.PostedReason)
            .Should()
            .ContainSingle(x => x.QuantityDelta == 7);
        var poLine = await db.Set<PurchaseOrderLine>().SingleAsync(x => x.Id == purchaseOrderLineId, TestContext.Current.CancellationToken);
        poLine.ReceivedQuantity.Should().Be(7);
        var receipt = await db.Set<GoodsReceipt>().SingleAsync(x => x.Id == receiptId, TestContext.Current.CancellationToken);
        receipt.Status.Should().Be(GoodsReceiptStatus.Posted);
    }

    [Fact]
    public async Task GoodsReceiptLifecycle_Should_RejectInspectionQuantityMismatch()
    {
        await using var db = InventoryTestDbContext.Create();
        var receiptId = Guid.NewGuid();
        var receiptLineId = Guid.NewGuid();
        var purchaseOrderLineId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        db.Set<GoodsReceipt>().Add(new GoodsReceipt
        {
            Id = receiptId,
            BusinessId = Guid.NewGuid(),
            SupplierId = Guid.NewGuid(),
            PurchaseOrderId = Guid.NewGuid(),
            WarehouseId = Guid.NewGuid(),
            Status = GoodsReceiptStatus.Received,
            RowVersion = [5],
            Lines =
            [
                new GoodsReceiptLine
                {
                    Id = receiptLineId,
                    PurchaseOrderLineId = purchaseOrderLineId,
                    ProductVariantId = variantId,
                    OrderedQuantity = 5,
                    ReceivedQuantity = 5
                }
            ]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateGoodsReceiptLifecycleHandler(db, CreateLocalizer());
        var result = await handler.HandleAsync(new GoodsReceiptLifecycleActionDto
        {
            Id = receiptId,
            RowVersion = [5],
            Action = UpdateGoodsReceiptLifecycleHandler.InspectAction,
            Lines =
            [
                new GoodsReceiptLineDto
                {
                    Id = receiptLineId,
                    PurchaseOrderLineId = purchaseOrderLineId,
                    ProductVariantId = variantId,
                    AcceptedQuantity = 6
                }
            ]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("GoodsReceiptInvalidQuantity");
    }

    [Fact]
    public async Task UpdateWarehouse_Should_ThrowConcurrency_WhenSaveChangesThrowsConcurrencyException()
    {
        await using var db = InventoryTestDbContext.Create();
        var id = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        db.Set<Warehouse>().Add(new Warehouse { Id = id, BusinessId = businessId, Name = "WH", RowVersion = [1] });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.ThrowConcurrencyOnSave = true;

        var handler = new UpdateWarehouseHandler(db, new WarehouseEditValidator(), CreateLocalizer());

        var act = async () => await handler.HandleAsync(new WarehouseEditDto
        {
            Id = id,
            BusinessId = businessId,
            Name = "WH 2",
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task UpdateSupplier_Should_ThrowConcurrency_WhenSaveChangesThrowsConcurrencyException()
    {
        await using var db = InventoryTestDbContext.Create();
        var id = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        db.Set<Supplier>().Add(new Supplier
        {
            Id = id,
            BusinessId = businessId,
            Name = "Legacy",
            Email = "legacy@sup.com",
            Phone = "000",
            RowVersion = [1]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.ThrowConcurrencyOnSave = true;

        var handler = new UpdateSupplierHandler(db, new SupplierEditValidator(), CreateLocalizer());

        var act = async () => await handler.HandleAsync(new SupplierEditDto
        {
            Id = id,
            BusinessId = businessId,
            Name = "Updated",
            Email = "legacy@sup.com",
            Phone = "000",
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task UpdateStockLevel_Should_ThrowConcurrency_WhenSaveChangesThrowsConcurrencyException()
    {
        await using var db = InventoryTestDbContext.Create();
        var id = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        db.Set<Warehouse>().Add(new Warehouse { Id = warehouseId, Name = "WH" });
        db.Set<ProductVariant>().Add(new ProductVariant { Id = variantId, Sku = "SKU-1" });
        db.Set<StockLevel>().Add(new StockLevel
        {
            Id = id,
            WarehouseId = warehouseId,
            ProductVariantId = variantId,
            AvailableQuantity = 10,
            RowVersion = [1]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.ThrowConcurrencyOnSave = true;

        var handler = new UpdateStockLevelHandler(db, new StockLevelEditValidator(), CreateLocalizer());
        var act = async () => await handler.HandleAsync(new StockLevelEditDto
        {
            Id = id,
            WarehouseId = warehouseId,
            ProductVariantId = variantId,
            AvailableQuantity = 20,
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task UpdateStockTransfer_Should_ThrowConcurrency_WhenSaveChangesThrowsConcurrencyException()
    {
        await using var db = InventoryTestDbContext.Create();
        var id = Guid.NewGuid();
        var fromId = Guid.NewGuid();
        var toId = Guid.NewGuid();

        db.Set<Warehouse>().AddRange(
            new Warehouse { Id = fromId, Name = "From WH", IsDefault = true },
            new Warehouse { Id = toId, Name = "To WH" });
        db.Set<StockTransfer>().Add(new StockTransfer
        {
            Id = id,
            FromWarehouseId = fromId,
            ToWarehouseId = toId,
            Status = TransferStatus.Draft,
            RowVersion = [1],
            Lines = [new StockTransferLine { ProductVariantId = Guid.NewGuid(), Quantity = 1 }]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.ThrowConcurrencyOnSave = true;

        var handler = new UpdateStockTransferHandler(db, new StockTransferEditValidator(), CreateLocalizer());
        var act = async () => await handler.HandleAsync(new StockTransferEditDto
        {
            Id = id,
            FromWarehouseId = fromId,
            ToWarehouseId = toId,
            Status = "Draft",
            Lines = [new StockTransferLineDto { ProductVariantId = Guid.NewGuid(), Quantity = 1 }],
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task StockTransferLifecycle_Should_ReturnFail_AndConcurrencyConflict_WhenSaveChangesThrowsConcurrencyException()
    {
        await using var db = InventoryTestDbContext.Create();
        var id = Guid.NewGuid();
        var fromId = Guid.NewGuid();
        var toId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        db.Set<Warehouse>().AddRange(
            new Warehouse { Id = fromId, Name = "From WH", IsDefault = true },
            new Warehouse { Id = toId, Name = "To WH" });
        db.Set<ProductVariant>().Add(new ProductVariant { Id = variantId, Sku = "V3" });
        db.Set<StockLevel>().Add(new StockLevel
        {
            Id = Guid.NewGuid(),
            WarehouseId = fromId,
            ProductVariantId = variantId,
            AvailableQuantity = 10
        });
        db.Set<StockTransfer>().Add(new StockTransfer
        {
            Id = id,
            FromWarehouseId = fromId,
            ToWarehouseId = toId,
            Status = TransferStatus.Draft,
            RowVersion = [1],
            Lines = [new StockTransferLine { ProductVariantId = variantId, Quantity = 5 }]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.ThrowConcurrencyOnSave = true;

        var handler = new UpdateStockTransferLifecycleHandler(db, CreateLocalizer());
        var result = await handler.HandleAsync(new StockTransferLifecycleActionDto
        {
            Id = id,
            RowVersion = [1],
            Action = "MarkInTransit"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task UpdatePurchaseOrder_Should_ThrowConcurrency_WhenSaveChangesThrowsConcurrencyException()
    {
        await using var db = InventoryTestDbContext.Create();
        var id = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        db.Set<Supplier>().Add(new Supplier
        {
            Id = supplierId,
            BusinessId = businessId,
            Name = "Supplier",
            Email = "supplier@example.test",
            Phone = "123",
            Status = SupplierStatus.Active
        });
        db.Set<PurchaseOrder>().Add(new PurchaseOrder
        {
            Id = id,
            SupplierId = supplierId,
            BusinessId = businessId,
            OrderNumber = "PO-CONC",
            Status = PurchaseOrderStatus.Draft,
            RowVersion = [2],
            Lines = [new PurchaseOrderLine { ProductVariantId = Guid.NewGuid(), Quantity = 1, UnitCostMinor = 100, TotalCostMinor = 100 }]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.ThrowConcurrencyOnSave = true;

        var handler = new UpdatePurchaseOrderHandler(db, new PurchaseOrderEditValidator(), CreateLocalizer());
        var act = async () => await handler.HandleAsync(new PurchaseOrderEditDto
        {
            Id = id,
            SupplierId = supplierId,
            BusinessId = businessId,
            OrderNumber = "PO-CONC",
            Status = "Draft",
            Lines = [new PurchaseOrderLineDto { ProductVariantId = Guid.NewGuid(), Quantity = 1 }],
            RowVersion = [2]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task PurchaseOrderLifecycle_Should_ReturnFail_AndConcurrencyConflict_WhenSaveChangesThrowsConcurrencyException()
    {
        await using var db = InventoryTestDbContext.Create();
        var id = Guid.NewGuid();
        db.Set<PurchaseOrder>().Add(new PurchaseOrder
        {
            Id = id,
            SupplierId = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            OrderNumber = "PO-LC-CONC",
            Status = PurchaseOrderStatus.Draft,
            RowVersion = [4]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.ThrowConcurrencyOnSave = true;

        var handler = new UpdatePurchaseOrderLifecycleHandler(db, CreateLocalizer());
        var result = await handler.HandleAsync(new PurchaseOrderLifecycleActionDto
        {
            Id = id,
            RowVersion = [4],
            Action = "Issue"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Null database RowVersion guards
    // These tests verify that handlers compare entity.RowVersion null-safely
    // (via ?? Array.Empty<byte>()) so legacy rows with null RowVersion values
    // produce a safe concurrency failure rather than a NullReferenceException.
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateWarehouse_Should_ThrowConcurrency_WhenDatabaseRowVersionIsNull()
    {
        await using var db = InventoryTestDbContext.Create();
        var id = Guid.NewGuid();
        var entity = new Warehouse { Id = id, BusinessId = Guid.NewGuid(), Name = "Legacy WH" };
        entity.RowVersion = [0];
        db.Set<Warehouse>().Add(entity);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateWarehouseHandler(db, new WarehouseEditValidator(), CreateLocalizer());

        var act = async () => await handler.HandleAsync(new WarehouseEditDto
        {
            Id = id,
            BusinessId = Guid.NewGuid(),
            Name = "Legacy WH",
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>(
            "null DB RowVersion must produce a safe concurrency failure, not NullReferenceException");
    }

    [Fact]
    public async Task UpdateSupplier_Should_ThrowConcurrency_WhenDatabaseRowVersionIsNull()
    {
        await using var db = InventoryTestDbContext.Create();
        var id = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var entity = new Supplier { Id = id, BusinessId = businessId, Name = "Legacy Sup", Email = "s@s.com", Phone = "000" };
        entity.RowVersion = [0];
        db.Set<Supplier>().Add(entity);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateSupplierHandler(db, new SupplierEditValidator(), CreateLocalizer());

        var act = async () => await handler.HandleAsync(new SupplierEditDto
        {
            Id = id, BusinessId = businessId, Name = "Legacy Sup", Email = "s@s.com", Phone = "000",
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>(
            "null DB RowVersion must produce a safe concurrency failure");
    }

    [Fact]
    public async Task UpdateStockLevel_Should_ThrowConcurrency_WhenDatabaseRowVersionIsNull()
    {
        await using var db = InventoryTestDbContext.Create();
        var id = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var entity = new StockLevel { Id = id, WarehouseId = warehouseId, ProductVariantId = variantId, AvailableQuantity = 5 };
        entity.RowVersion = [0];
        db.Set<Warehouse>().Add(new Warehouse { Id = warehouseId, Name = "WH" });
        db.Set<ProductVariant>().Add(new ProductVariant { Id = variantId, Sku = "SKU-NULL" });
        db.Set<StockLevel>().Add(entity);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateStockLevelHandler(db, new StockLevelEditValidator(), CreateLocalizer());

        var act = async () => await handler.HandleAsync(new StockLevelEditDto
        {
            Id = id, WarehouseId = warehouseId, ProductVariantId = variantId,
            AvailableQuantity = 10, RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>(
            "null DB RowVersion must produce a safe concurrency failure");
    }

    [Fact]
    public async Task UpdateStockTransfer_Should_ThrowConcurrency_WhenDatabaseRowVersionIsNull()
    {
        await using var db = InventoryTestDbContext.Create();
        var id = Guid.NewGuid();
        var fromId = Guid.NewGuid();
        var toId = Guid.NewGuid();
        var entity = new StockTransfer { Id = id, FromWarehouseId = fromId, ToWarehouseId = toId, Status = TransferStatus.Draft };
        entity.RowVersion = [0];
        db.Set<StockTransfer>().Add(entity);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateStockTransferHandler(db, new StockTransferEditValidator(), CreateLocalizer());

        var act = async () => await handler.HandleAsync(new StockTransferEditDto
        {
            Id = id, FromWarehouseId = fromId, ToWarehouseId = toId, Status = "Draft",
            Lines = [new StockTransferLineDto { ProductVariantId = Guid.NewGuid(), Quantity = 1 }],
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>(
            "null DB RowVersion must produce a safe concurrency failure");
    }

    [Fact]
    public async Task StockTransferLifecycle_Should_ReturnFail_WhenDatabaseRowVersionIsNull()
    {
        await using var db = InventoryTestDbContext.Create();
        var id = Guid.NewGuid();
        var entity = new StockTransfer
        {
            Id = id, FromWarehouseId = Guid.NewGuid(), ToWarehouseId = Guid.NewGuid(),
            Status = TransferStatus.Draft
        };
        entity.RowVersion = [0];
        db.Set<StockTransfer>().Add(entity);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateStockTransferLifecycleHandler(db, CreateLocalizer());

        var result = await handler.HandleAsync(new StockTransferLifecycleActionDto
        {
            Id = id, RowVersion = [1], Action = "MarkInTransit"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse(
            "null DB RowVersion must produce a safe concurrency failure, not NullReferenceException");
    }

    [Fact]
    public async Task UpdatePurchaseOrder_Should_ThrowConcurrency_WhenDatabaseRowVersionIsNull()
    {
        await using var db = InventoryTestDbContext.Create();
        var id = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var entity = new PurchaseOrder
        {
            Id = id, SupplierId = supplierId, BusinessId = businessId,
            OrderNumber = "PO-NULL", Status = PurchaseOrderStatus.Draft
        };
        entity.RowVersion = [0];
        db.Set<PurchaseOrder>().Add(entity);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdatePurchaseOrderHandler(db, new PurchaseOrderEditValidator(), CreateLocalizer());

        var act = async () => await handler.HandleAsync(new PurchaseOrderEditDto
        {
            Id = id, SupplierId = supplierId, BusinessId = businessId, OrderNumber = "PO-NULL", Status = "Draft",
            Lines = [new PurchaseOrderLineDto { ProductVariantId = Guid.NewGuid(), Quantity = 1 }],
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>(
            "null DB RowVersion must produce a safe concurrency failure");
    }

    [Fact]
    public async Task PurchaseOrderLifecycle_Should_ReturnFail_WhenDatabaseRowVersionIsNull()
    {
        await using var db = InventoryTestDbContext.Create();
        var id = Guid.NewGuid();
        var entity = new PurchaseOrder
        {
            Id = id, SupplierId = Guid.NewGuid(), BusinessId = Guid.NewGuid(),
            OrderNumber = "PO-NULL-LC", Status = PurchaseOrderStatus.Draft
        };
        entity.RowVersion = [0];
        db.Set<PurchaseOrder>().Add(entity);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdatePurchaseOrderLifecycleHandler(db, CreateLocalizer());

        var result = await handler.HandleAsync(new PurchaseOrderLifecycleActionDto
        {
            Id = id, RowVersion = [1], Action = "Issue"
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse(
            "null DB RowVersion must produce a safe concurrency failure, not NullReferenceException");
    }

    [Fact]
    public async Task WarehouseTask_Should_Create_Draft_With_Normalized_Lines_And_Number()
    {
        await using var db = InventoryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        db.Set<NumberSequence>().Add(new NumberSequence
        {
            BusinessId = businessId,
            DocumentType = NumberSequenceDocumentType.WarehouseTask,
            ScopeKey = NumberSequenceService.GlobalScopeKey,
            PrefixPattern = "WT-{seq}",
            PaddingLength = 4,
            NextValue = 1,
            IsActive = true
        });
        db.Set<Warehouse>().Add(new Warehouse { Id = warehouseId, BusinessId = businessId, Name = "WH" });
        db.Set<WarehouseLocation>().Add(new WarehouseLocation { Id = locationId, BusinessId = businessId, WarehouseId = warehouseId, Code = "BIN-1", DisplayName = "Bin 1", Status = WarehouseLocationStatus.Active });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateWarehouseTaskHandler(db, new WarehouseTaskCreateValidator(), CreateLocalizer(), new FixedClock(), new NumberSequenceService(db, new FixedClock()));
        var id = await handler.HandleAsync(new WarehouseTaskCreateDto
        {
            BusinessId = businessId,
            WarehouseId = warehouseId,
            ToLocationId = locationId,
            Title = "  Putaway receipt  ",
            TaskType = WarehouseTaskType.Putaway,
            Lines = [new WarehouseTaskLineDto { Description = "  Case 1  ", RequestedQuantity = 3 }]
        }, TestContext.Current.CancellationToken);

        var task = await db.Set<WarehouseTask>().Include(x => x.Lines).SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        task.TaskNumber.Should().Be("WT-0001");
        task.Title.Should().Be("Putaway receipt");
        task.Status.Should().Be(WarehouseTaskStatus.Draft);
        task.Lines.Should().ContainSingle();
        task.Lines[0].Description.Should().Be("Case 1");
        task.Lines[0].RequestedQuantity.Should().Be(3);
        db.Set<InventoryTransaction>().Should().BeEmpty("task foundation must not post stock movements");
    }

    [Fact]
    public async Task WarehouseTaskLifecycle_Should_Complete_Without_Inventory_Mutation()
    {
        await using var db = InventoryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        db.Set<Warehouse>().Add(new Warehouse { Id = warehouseId, BusinessId = businessId, Name = "WH" });
        db.Set<WarehouseTask>().Add(new WarehouseTask
        {
            Id = taskId,
            BusinessId = businessId,
            WarehouseId = warehouseId,
            Title = "Pick",
            Status = WarehouseTaskStatus.InProgress,
            RowVersion = [1],
            Lines = [new WarehouseTaskLine { Description = "Line", RequestedQuantity = 2, CompletedQuantity = 0, SortOrder = 1 }]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateWarehouseTaskLifecycleHandler(db, CreateLocalizer(), new FixedClock(), new WarehouseTaskWorkflowPolicy());
        var result = await handler.HandleAsync(new WarehouseTaskLifecycleActionDto
        {
            Id = taskId,
            RowVersion = [1],
            TargetStatus = WarehouseTaskStatus.Completed
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue(result.Error);
        var task = await db.Set<WarehouseTask>().Include(x => x.Lines).SingleAsync(x => x.Id == taskId, TestContext.Current.CancellationToken);
        task.Status.Should().Be(WarehouseTaskStatus.Completed);
        task.CompletedAtUtc.Should().Be(new FixedClock().UtcNow);
        task.Lines.Single().CompletedQuantity.Should().Be(2);
        db.Set<InventoryTransaction>().Should().BeEmpty();
    }

    [Fact]
    public async Task WarehouseTask_Should_Reject_CrossWarehouse_Location()
    {
        await using var db = InventoryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var otherWarehouseId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        db.Set<Warehouse>().AddRange(
            new Warehouse { Id = warehouseId, BusinessId = businessId, Name = "WH1" },
            new Warehouse { Id = otherWarehouseId, BusinessId = businessId, Name = "WH2" });
        db.Set<WarehouseLocation>().Add(new WarehouseLocation { Id = locationId, BusinessId = businessId, WarehouseId = otherWarehouseId, Code = "BIN-2", DisplayName = "Bin 2", Status = WarehouseLocationStatus.Active });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateWarehouseTaskHandler(db, new WarehouseTaskCreateValidator(), CreateLocalizer(), new FixedClock(), new NumberSequenceService(db, new FixedClock()));
        var act = async () => await handler.HandleAsync(new WarehouseTaskCreateDto
        {
            BusinessId = businessId,
            WarehouseId = warehouseId,
            ToLocationId = locationId,
            Title = "Putaway",
            Lines = [new WarehouseTaskLineDto { Description = "Line", RequestedQuantity = 1 }]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*WarehouseLocationNotFound*");
    }

    [Fact]
    public async Task WarehouseTask_Should_Reject_Sensitive_Metadata()
    {
        await using var db = InventoryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        db.Set<Warehouse>().Add(new Warehouse { Id = warehouseId, BusinessId = businessId, Name = "WH" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateWarehouseTaskHandler(db, new WarehouseTaskCreateValidator(), CreateLocalizer(), new FixedClock(), new NumberSequenceService(db, new FixedClock()));
        var act = async () => await handler.HandleAsync(new WarehouseTaskCreateDto
        {
            BusinessId = businessId,
            WarehouseId = warehouseId,
            Title = "token sk-live-secret",
            Lines = [new WarehouseTaskLineDto { Description = "Line", RequestedQuantity = 1 }]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*WarehouseTaskSensitiveMetadataRejected*");
    }

    [Fact]
    public async Task GoodsReceiptPutawayTask_Should_Create_From_Posted_Accepted_Lines_Without_Stock_Mutation()
    {
        await using var db = InventoryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var purchaseLineId = Guid.NewGuid();
        var receiptId = Guid.NewGuid();
        var receiptLineId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        db.Set<ProductVariant>().Add(new ProductVariant { Id = variantId, Sku = "PUT-1" });
        db.Set<Warehouse>().Add(new Warehouse { Id = warehouseId, BusinessId = businessId, Name = "WH" });
        db.Set<WarehouseLocation>().Add(new WarehouseLocation { Id = locationId, BusinessId = businessId, WarehouseId = warehouseId, Code = "BIN-PUT", DisplayName = "Putaway Bin", Status = WarehouseLocationStatus.Active });
        db.Set<GoodsReceipt>().Add(new GoodsReceipt
        {
            Id = receiptId,
            BusinessId = businessId,
            SupplierId = supplierId,
            PurchaseOrderId = orderId,
            WarehouseId = warehouseId,
            Status = GoodsReceiptStatus.Posted,
            GoodsReceiptNumber = "GR-100",
            Lines =
            [
                new GoodsReceiptLine
                {
                    Id = receiptLineId,
                    PurchaseOrderLineId = purchaseLineId,
                    ProductVariantId = variantId,
                    SupplierSku = "SUP-1",
                    Description = "Case",
                    OrderedQuantity = 8,
                    ReceivedQuantity = 8,
                    AcceptedQuantity = 6,
                    SortOrder = 1
                }
            ]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var createTask = new CreateWarehouseTaskHandler(db, new WarehouseTaskCreateValidator(), CreateLocalizer(), new FixedClock(), new NumberSequenceService(db, new FixedClock()));
        var handler = new CreateWarehousePutawayTaskFromGoodsReceiptHandler(db, createTask, CreateLocalizer());
        var result = await handler.HandleAsync(new CreateWarehousePutawayTaskFromGoodsReceiptDto
        {
            GoodsReceiptId = receiptId,
            ToLocationId = locationId,
            Priority = WarehouseTaskPriority.High
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue(result.Error);
        var task = await db.Set<WarehouseTask>().Include(x => x.Lines).SingleAsync(x => x.Id == result.Value, TestContext.Current.CancellationToken);
        task.TaskType.Should().Be(WarehouseTaskType.Putaway);
        task.Status.Should().Be(WarehouseTaskStatus.Ready);
        task.SourceType.Should().Be(WarehouseTaskSourceType.GoodsReceipt);
        task.SourceEntityId.Should().Be(receiptId);
        task.ToLocationId.Should().Be(locationId);
        task.Lines.Should().ContainSingle();
        task.Lines[0].SourceLineId.Should().Be(receiptLineId);
        task.Lines[0].RequestedQuantity.Should().Be(6);
        task.Lines[0].ToLocationId.Should().Be(locationId);
        db.Set<InventoryTransaction>().Should().BeEmpty();
    }

    [Fact]
    public async Task GoodsReceiptPutawayTask_Should_Reject_Unposted_Receipt()
    {
        await using var db = InventoryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var receiptId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        db.Set<Warehouse>().Add(new Warehouse { Id = warehouseId, BusinessId = businessId, Name = "WH" });
        db.Set<WarehouseLocation>().Add(new WarehouseLocation { Id = locationId, BusinessId = businessId, WarehouseId = warehouseId, Code = "BIN-PUT", DisplayName = "Putaway Bin", Status = WarehouseLocationStatus.Active });
        db.Set<GoodsReceipt>().Add(new GoodsReceipt
        {
            Id = receiptId,
            BusinessId = businessId,
            SupplierId = Guid.NewGuid(),
            PurchaseOrderId = Guid.NewGuid(),
            WarehouseId = warehouseId,
            Status = GoodsReceiptStatus.Inspected,
            Lines =
            [
                new GoodsReceiptLine
                {
                    Id = Guid.NewGuid(),
                    PurchaseOrderLineId = Guid.NewGuid(),
                    ProductVariantId = Guid.NewGuid(),
                    Description = "Case",
                    AcceptedQuantity = 2,
                    SortOrder = 1
                }
            ]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var createTask = new CreateWarehouseTaskHandler(db, new WarehouseTaskCreateValidator(), CreateLocalizer(), new FixedClock(), new NumberSequenceService(db, new FixedClock()));
        var handler = new CreateWarehousePutawayTaskFromGoodsReceiptHandler(db, createTask, CreateLocalizer());
        var result = await handler.HandleAsync(new CreateWarehousePutawayTaskFromGoodsReceiptDto
        {
            GoodsReceiptId = receiptId,
            ToLocationId = locationId
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("GoodsReceiptLifecycleUnsupportedAction");
        db.Set<WarehouseTask>().Should().BeEmpty();
    }

    [Fact]
    public async Task WarehouseTaskLifecycle_Should_Reject_Putaway_Completion_When_GoodsReceipt_Not_Posted()
    {
        await using var db = InventoryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var receiptId = Guid.NewGuid();
        var receiptLineId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        db.Set<Warehouse>().Add(new Warehouse { Id = warehouseId, BusinessId = businessId, Name = "WH" });
        db.Set<WarehouseLocation>().Add(new WarehouseLocation { Id = locationId, BusinessId = businessId, WarehouseId = warehouseId, Code = "BIN-PUT", DisplayName = "Putaway Bin", Status = WarehouseLocationStatus.Active });
        db.Set<GoodsReceipt>().Add(new GoodsReceipt
        {
            Id = receiptId,
            BusinessId = businessId,
            SupplierId = Guid.NewGuid(),
            PurchaseOrderId = Guid.NewGuid(),
            WarehouseId = warehouseId,
            Status = GoodsReceiptStatus.Inspected,
            Lines =
            [
                new GoodsReceiptLine
                {
                    Id = receiptLineId,
                    PurchaseOrderLineId = Guid.NewGuid(),
                    ProductVariantId = Guid.NewGuid(),
                    Description = "Case",
                    AcceptedQuantity = 2,
                    SortOrder = 1
                }
            ]
        });
        db.Set<WarehouseTask>().Add(new WarehouseTask
        {
            Id = taskId,
            BusinessId = businessId,
            WarehouseId = warehouseId,
            ToLocationId = locationId,
            Title = "Putaway",
            TaskType = WarehouseTaskType.Putaway,
            SourceType = WarehouseTaskSourceType.GoodsReceipt,
            SourceEntityId = receiptId,
            Status = WarehouseTaskStatus.InProgress,
            RowVersion = [1],
            Lines =
            [
                new WarehouseTaskLine
                {
                    Description = "Case",
                    RequestedQuantity = 2,
                    ToLocationId = locationId,
                    SourceLineType = "GoodsReceiptLine",
                    SourceLineId = receiptLineId,
                    SortOrder = 1
                }
            ]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateWarehouseTaskLifecycleHandler(db, CreateLocalizer(), new FixedClock(), new WarehouseTaskWorkflowPolicy());
        var result = await handler.HandleAsync(new WarehouseTaskLifecycleActionDto
        {
            Id = taskId,
            RowVersion = [1],
            TargetStatus = WarehouseTaskStatus.Completed
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("GoodsReceiptLifecycleUnsupportedAction");
        var task = await db.Set<WarehouseTask>().SingleAsync(x => x.Id == taskId, TestContext.Current.CancellationToken);
        task.Status.Should().Be(WarehouseTaskStatus.InProgress);
    }

    [Fact]
    public async Task WarehousePickingTask_Should_Create_From_Allocated_Order_Without_Stock_Mutation()
    {
        await using var db = InventoryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var orderLineId = Guid.NewGuid();
        db.Set<ProductVariant>().Add(new ProductVariant { Id = variantId, Sku = "PICK-1" });
        db.Set<Warehouse>().Add(new Warehouse { Id = warehouseId, BusinessId = businessId, Name = "WH" });
        db.Set<WarehouseLocation>().Add(new WarehouseLocation { Id = locationId, BusinessId = businessId, WarehouseId = warehouseId, Code = "BIN-PICK", DisplayName = "Pick Bin", Status = WarehouseLocationStatus.Active });
        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            BusinessId = businessId,
            OrderNumber = "SO-100",
            Status = OrderStatus.Paid,
            Currency = "EUR",
            Lines =
            [
                new OrderLine
                {
                    Id = orderLineId,
                    VariantId = variantId,
                    WarehouseId = warehouseId,
                    Name = "Pickable item",
                    Sku = "PICK-1",
                    Quantity = 3
                }
            ]
        });
        db.Set<InventoryTransaction>().Add(new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            WarehouseId = warehouseId,
            ProductVariantId = variantId,
            QuantityDelta = -3,
            Reason = InventoryMovementReferencePolicy.ShipmentAllocation,
            ReferenceId = orderId
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var createTask = new CreateWarehouseTaskHandler(db, new WarehouseTaskCreateValidator(), CreateLocalizer(), new FixedClock(), new NumberSequenceService(db, new FixedClock()));
        var handler = new CreateWarehousePickingTaskFromOrderHandler(db, createTask, CreateLocalizer());
        var result = await handler.HandleAsync(new CreateWarehousePickingTaskFromOrderDto
        {
            BusinessId = businessId,
            WarehouseId = warehouseId,
            OrderId = orderId,
            FromLocationId = locationId,
            Priority = WarehouseTaskPriority.High
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue(result.Error);
        var task = await db.Set<WarehouseTask>().Include(x => x.Lines).SingleAsync(x => x.Id == result.Value, TestContext.Current.CancellationToken);
        task.TaskType.Should().Be(WarehouseTaskType.Picking);
        task.Status.Should().Be(WarehouseTaskStatus.Ready);
        task.SourceType.Should().Be(WarehouseTaskSourceType.Order);
        task.SourceEntityId.Should().Be(orderId);
        task.FromLocationId.Should().Be(locationId);
        task.Lines.Should().ContainSingle();
        task.Lines[0].SourceLineType.Should().Be("OrderLine");
        task.Lines[0].SourceLineId.Should().Be(orderLineId);
        task.Lines[0].RequestedQuantity.Should().Be(3);
        task.Lines[0].FromLocationId.Should().Be(locationId);
        db.Set<InventoryTransaction>().Should().ContainSingle("picking tasks consume allocation evidence but do not post stock movements");
    }

    [Fact]
    public async Task WarehousePickingTask_Should_Reject_Order_Without_Allocation()
    {
        await using var db = InventoryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        db.Set<ProductVariant>().Add(new ProductVariant { Id = variantId, Sku = "PICK-2" });
        db.Set<Warehouse>().Add(new Warehouse { Id = warehouseId, BusinessId = businessId, Name = "WH" });
        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            BusinessId = businessId,
            OrderNumber = "SO-101",
            Status = OrderStatus.Paid,
            Currency = "EUR",
            Lines = [new OrderLine { Id = Guid.NewGuid(), VariantId = variantId, WarehouseId = warehouseId, Name = "Item", Sku = "PICK-2", Quantity = 1 }]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var createTask = new CreateWarehouseTaskHandler(db, new WarehouseTaskCreateValidator(), CreateLocalizer(), new FixedClock(), new NumberSequenceService(db, new FixedClock()));
        var handler = new CreateWarehousePickingTaskFromOrderHandler(db, createTask, CreateLocalizer());
        var result = await handler.HandleAsync(new CreateWarehousePickingTaskFromOrderDto
        {
            BusinessId = businessId,
            WarehouseId = warehouseId,
            OrderId = orderId
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("WarehouseTaskPickingAllocationMissing");
        db.Set<WarehouseTask>().Should().BeEmpty();
    }

    [Fact]
    public async Task WarehouseTaskLifecycle_Should_Reject_Picking_Completion_When_RequestedQuantity_Exceeds_Allocation()
    {
        await using var db = InventoryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var orderLineId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        db.Set<ProductVariant>().Add(new ProductVariant { Id = variantId, Sku = "PICK-3" });
        db.Set<Warehouse>().Add(new Warehouse { Id = warehouseId, BusinessId = businessId, Name = "WH" });
        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            BusinessId = businessId,
            OrderNumber = "SO-102",
            Status = OrderStatus.Paid,
            Currency = "EUR",
            Lines = [new OrderLine { Id = orderLineId, VariantId = variantId, WarehouseId = warehouseId, Name = "Item", Sku = "PICK-3", Quantity = 4 }]
        });
        db.Set<InventoryTransaction>().Add(new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            WarehouseId = warehouseId,
            ProductVariantId = variantId,
            QuantityDelta = -2,
            Reason = InventoryMovementReferencePolicy.ShipmentAllocation,
            ReferenceId = orderId
        });
        db.Set<WarehouseTask>().Add(new WarehouseTask
        {
            Id = taskId,
            BusinessId = businessId,
            WarehouseId = warehouseId,
            Title = "Pick SO-102",
            TaskType = WarehouseTaskType.Picking,
            SourceType = WarehouseTaskSourceType.Order,
            SourceEntityId = orderId,
            Status = WarehouseTaskStatus.InProgress,
            RowVersion = [1],
            Lines =
            [
                new WarehouseTaskLine
                {
                    ProductVariantId = variantId,
                    Description = "Item",
                    RequestedQuantity = 3,
                    SourceLineType = "OrderLine",
                    SourceLineId = orderLineId,
                    SortOrder = 1
                }
            ]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateWarehouseTaskLifecycleHandler(db, CreateLocalizer(), new FixedClock(), new WarehouseTaskWorkflowPolicy());
        var result = await handler.HandleAsync(new WarehouseTaskLifecycleActionDto
        {
            Id = taskId,
            RowVersion = [1],
            TargetStatus = WarehouseTaskStatus.Completed
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("WarehouseTaskPickingAllocationMissing");
        var task = await db.Set<WarehouseTask>().SingleAsync(x => x.Id == taskId, TestContext.Current.CancellationToken);
        task.Status.Should().Be(WarehouseTaskStatus.InProgress);
    }

    [Fact]
    public async Task WarehouseTaskLifecycle_Should_Record_Picking_Shortage_Without_Mutating_Order_Or_Inventory()
    {
        await using var db = InventoryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var orderLineId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        db.Set<ProductVariant>().Add(new ProductVariant { Id = variantId, Sku = "PICK-SHORT" });
        db.Set<Warehouse>().Add(new Warehouse { Id = warehouseId, BusinessId = businessId, Name = "WH" });
        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            BusinessId = businessId,
            OrderNumber = "SO-103",
            Status = OrderStatus.Paid,
            Currency = "EUR",
            Lines = [new OrderLine { Id = orderLineId, VariantId = variantId, WarehouseId = warehouseId, Name = "Item", Sku = "PICK-SHORT", Quantity = 4 }]
        });
        db.Set<InventoryTransaction>().Add(new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            WarehouseId = warehouseId,
            ProductVariantId = variantId,
            QuantityDelta = -4,
            Reason = InventoryMovementReferencePolicy.ShipmentAllocation,
            ReferenceId = orderId
        });
        db.Set<WarehouseTask>().Add(new WarehouseTask
        {
            Id = taskId,
            BusinessId = businessId,
            WarehouseId = warehouseId,
            Title = "Pick SO-103",
            TaskType = WarehouseTaskType.Picking,
            SourceType = WarehouseTaskSourceType.Order,
            SourceEntityId = orderId,
            Status = WarehouseTaskStatus.InProgress,
            RowVersion = [1],
            Lines =
            [
                new WarehouseTaskLine
                {
                    ProductVariantId = variantId,
                    Description = "Item",
                    RequestedQuantity = 4,
                    CompletedQuantity = 2,
                    ShortQuantity = 2,
                    ShortReason = "Bin empty",
                    SourceLineType = "OrderLine",
                    SourceLineId = orderLineId,
                    SortOrder = 1
                }
            ]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateWarehouseTaskLifecycleHandler(db, CreateLocalizer(), new FixedClock(), new WarehouseTaskWorkflowPolicy());
        var result = await handler.HandleAsync(new WarehouseTaskLifecycleActionDto
        {
            Id = taskId,
            RowVersion = [1],
            TargetStatus = WarehouseTaskStatus.Completed
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue(result.Error);
        var task = await db.Set<WarehouseTask>().Include(x => x.Lines).SingleAsync(x => x.Id == taskId, TestContext.Current.CancellationToken);
        task.Status.Should().Be(WarehouseTaskStatus.Completed);
        task.Lines.Single().CompletedQuantity.Should().Be(2);
        task.Lines.Single().ShortQuantity.Should().Be(2);
        task.Lines.Single().ShortReason.Should().Be("Bin empty");
        (await db.Set<Order>().SingleAsync(x => x.Id == orderId, TestContext.Current.CancellationToken)).Status.Should().Be(OrderStatus.Paid);
        db.Set<InventoryTransaction>().Should().ContainSingle("short-pick attention must not create stock movement");
    }

    [Fact]
    public async Task WarehouseTaskCreate_Should_Reject_Shortage_For_NonPicking_Task()
    {
        await using var db = InventoryTestDbContext.Create();
        var handler = new CreateWarehouseTaskHandler(db, new WarehouseTaskCreateValidator(), CreateLocalizer(), new FixedClock(), new NumberSequenceService(db, new FixedClock()));
        var businessId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        db.Set<Warehouse>().Add(new Warehouse { Id = warehouseId, BusinessId = businessId, Name = "Main", IsDefault = true });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var dto = new WarehouseTaskCreateDto
        {
            BusinessId = businessId,
            WarehouseId = warehouseId,
            Title = "General task",
            TaskType = WarehouseTaskType.General,
            Lines =
            [
                new WarehouseTaskLineDto
                {
                    Description = "Line",
                    RequestedQuantity = 3,
                    ShortQuantity = 1,
                    ShortReason = "Not applicable"
                }
            ]
        };

        var act = async () => await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*WarehouseTaskShortageOnlyAllowedForPicking*");
    }

    [Fact]
    public async Task WarehouseTaskPage_Should_Filter_And_Summarize_Picking_Shortage()
    {
        await using var db = InventoryTestDbContext.Create();
        var businessId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        db.Set<Warehouse>().Add(new Warehouse { Id = warehouseId, BusinessId = businessId, Name = "WH" });
        db.Set<WarehouseTask>().AddRange(
            new WarehouseTask
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId,
                WarehouseId = warehouseId,
                Title = "Short pick",
                TaskType = WarehouseTaskType.Picking,
                SourceType = WarehouseTaskSourceType.Order,
                Status = WarehouseTaskStatus.Completed,
                Lines = [new WarehouseTaskLine { Description = "Item", RequestedQuantity = 5, CompletedQuantity = 3, ShortQuantity = 2, ShortReason = "Missing", SortOrder = 1 }]
            },
            new WarehouseTask
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId,
                WarehouseId = warehouseId,
                Title = "Clean pick",
                TaskType = WarehouseTaskType.Picking,
                SourceType = WarehouseTaskSourceType.Order,
                Status = WarehouseTaskStatus.Completed,
                Lines = [new WarehouseTaskLine { Description = "Item", RequestedQuantity = 2, CompletedQuantity = 2, SortOrder = 1 }]
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetWarehouseTasksPageHandler(db);
        var (items, total) = await handler.HandleAsync(businessId, warehouseId, 1, 20, filter: WarehouseTaskQueueFilter.Shortage, ct: TestContext.Current.CancellationToken);
        var summary = await handler.GetSummaryAsync(businessId, warehouseId, TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().ContainSingle();
        items[0].HasShortage.Should().BeTrue();
        items[0].ShortQuantity.Should().Be(2);
        summary.ShortageCount.Should().Be(1);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private test infrastructure
    // ─────────────────────────────────────────────────────────────────────────

    private sealed class InventoryTestDbContext : DbContext, IAppDbContext
    {
        private InventoryTestDbContext(DbContextOptions<InventoryTestDbContext> options)
            : base(options) { }

        public bool ThrowConcurrencyOnSave { get; set; }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static InventoryTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<InventoryTestDbContext>()
                .UseInMemoryDatabase($"darwin_inventory_handler_tests_{Guid.NewGuid()}")
                .Options;
            return new InventoryTestDbContext(options);
        }

        public override Task<int> SaveChangesAsync(global::System.Threading.CancellationToken cancellationToken = default)
        {
            if (ThrowConcurrencyOnSave)
            {
                ThrowConcurrencyOnSave = false;
                throw new DbUpdateConcurrencyException("Simulated concurrency conflict during save");
            }

            return base.SaveChangesAsync(cancellationToken);
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
                b.Ignore(x => x.RowVersion);
                b.Ignore(x => x.StockLevels);
                b.Ignore(x => x.Locations);
            });

            modelBuilder.Entity<WarehouseLocation>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.BusinessId).IsRequired();
                b.Property(x => x.WarehouseId).IsRequired();
                b.Property(x => x.ParentLocationId);
                b.Property(x => x.Code).HasMaxLength(64).IsRequired();
                b.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
                b.Property(x => x.LocationType);
                b.Property(x => x.Status);
                b.Property(x => x.Barcode).HasMaxLength(128);
                b.Property(x => x.SortOrder);
                b.Property(x => x.Description).HasMaxLength(1000);
                b.Property(x => x.MetadataJson);
                b.Property(x => x.IsDeleted);
                b.Property(x => x.RowVersion);
                b.Ignore(x => x.Children);
            });

            modelBuilder.Entity<User>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.FirstName).HasMaxLength(100);
                b.Property(x => x.LastName).HasMaxLength(100);
                b.Property(x => x.IsDeleted);
                b.Ignore(x => x.RowVersion);
                b.Ignore(x => x.UserRoles);
                b.Ignore(x => x.Logins);
                b.Ignore(x => x.Tokens);
                b.Ignore(x => x.TwoFactorSecrets);
                b.Ignore(x => x.Devices);
                b.Ignore(x => x.BusinessFavorites);
                b.Ignore(x => x.BusinessLikes);
                b.Ignore(x => x.BusinessReviews);
                b.Ignore(x => x.EngagementSnapshot);
            });

            modelBuilder.Entity<WarehouseLabelTemplate>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.BusinessId).IsRequired();
                b.Property(x => x.Name).HasMaxLength(200).IsRequired();
                b.Property(x => x.TemplateKey).HasMaxLength(100).IsRequired();
                b.Property(x => x.Status);
                b.Property(x => x.Format);
                b.Property(x => x.IsDefault);
                b.Property(x => x.WidthMm);
                b.Property(x => x.HeightMm);
                b.Property(x => x.ContentTemplate).HasMaxLength(8000).IsRequired();
                b.Property(x => x.Description).HasMaxLength(1000);
                b.Property(x => x.MetadataJson);
                b.Property(x => x.IsDeleted);
                b.Property(x => x.RowVersion);
            });

            modelBuilder.Entity<WarehouseTask>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.BusinessId).IsRequired();
                b.Property(x => x.WarehouseId).IsRequired();
                b.Property(x => x.FromLocationId);
                b.Property(x => x.ToLocationId);
                b.Property(x => x.AssignedToUserId);
                b.Property(x => x.TaskNumber).HasMaxLength(100);
                b.Property(x => x.Title).HasMaxLength(200).IsRequired();
                b.Property(x => x.TaskType);
                b.Property(x => x.Status);
                b.Property(x => x.Priority);
                b.Property(x => x.SourceType);
                b.Property(x => x.SourceEntityId);
                b.Property(x => x.DueAtUtc);
                b.Property(x => x.ReadyAtUtc);
                b.Property(x => x.AssignedAtUtc);
                b.Property(x => x.StartedAtUtc);
                b.Property(x => x.CompletedAtUtc);
                b.Property(x => x.CancelledAtUtc);
                b.Property(x => x.InternalNotes).HasMaxLength(4000);
                b.Property(x => x.MetadataJson);
                b.Property(x => x.IsDeleted);
                b.Property(x => x.RowVersion);
                b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.WarehouseTaskId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<WarehouseTaskLine>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.WarehouseTaskId).IsRequired();
                b.Property(x => x.ProductVariantId);
                b.Property(x => x.FromLocationId);
                b.Property(x => x.ToLocationId);
                b.Property(x => x.SkuSnapshot).HasMaxLength(100);
                b.Property(x => x.Description).HasMaxLength(1000).IsRequired();
                b.Property(x => x.RequestedQuantity);
                b.Property(x => x.CompletedQuantity);
                b.Property(x => x.ShortQuantity);
                b.Property(x => x.ShortReason).HasMaxLength(1000);
                b.Property(x => x.SortOrder);
                b.Property(x => x.SourceLineType).HasMaxLength(100);
                b.Property(x => x.SourceLineId);
                b.Property(x => x.MetadataJson);
                b.Property(x => x.IsDeleted);
                b.Ignore(x => x.RowVersion);
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
                b.Ignore(x => x.RowVersion);
            });

            modelBuilder.Entity<Supplier>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.BusinessId).IsRequired();
                b.Property(x => x.Name).HasMaxLength(200).IsRequired();
                b.Property(x => x.Code).HasMaxLength(64);
                b.Property(x => x.Status);
                b.Property(x => x.Email).HasMaxLength(256).IsRequired();
                b.Property(x => x.Phone).HasMaxLength(50).IsRequired();
                b.Property(x => x.Address).HasMaxLength(500);
                b.Property(x => x.Notes);
                b.Property(x => x.PreferredCurrency).HasMaxLength(3);
                b.Property(x => x.PaymentTermDays);
                b.Property(x => x.LeadTimeDays);
                b.Property(x => x.Website).HasMaxLength(500);
                b.Property(x => x.TaxRegistrationNumber).HasMaxLength(100);
                b.Property(x => x.ExternalNotes).HasMaxLength(2000);
                b.Property(x => x.IsDeleted);
                b.Ignore(x => x.RowVersion);
                b.HasMany(x => x.Contacts).WithOne().HasForeignKey(x => x.SupplierId);
            });

            modelBuilder.Entity<SupplierContact>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.BusinessId).IsRequired();
                b.Property(x => x.SupplierId).IsRequired();
                b.Property(x => x.Role);
                b.Property(x => x.Name).HasMaxLength(200).IsRequired();
                b.Property(x => x.JobTitle).HasMaxLength(200);
                b.Property(x => x.Email).HasMaxLength(320);
                b.Property(x => x.Phone).HasMaxLength(50);
                b.Property(x => x.LanguageCode).HasMaxLength(16);
                b.Property(x => x.IsPrimary);
                b.Property(x => x.Notes).HasMaxLength(1000);
                b.Property(x => x.IsDeleted);
                b.Ignore(x => x.RowVersion);
            });

            modelBuilder.Entity<StockTransfer>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.FromWarehouseId).IsRequired();
                b.Property(x => x.ToWarehouseId).IsRequired();
                b.Property(x => x.Status);
                b.Property(x => x.IsDeleted);
                b.Ignore(x => x.RowVersion);
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
                b.Property(x => x.Currency).HasMaxLength(3).IsRequired();
                b.Property(x => x.ExpectedDeliveryDateUtc);
                b.Property(x => x.IssuedAtUtc);
                b.Property(x => x.ReceivedAtUtc);
                b.Property(x => x.CancelledAtUtc);
                b.Property(x => x.InternalNotes).HasMaxLength(4000);
                b.Property(x => x.IsDeleted);
                b.Ignore(x => x.RowVersion);
                b.HasMany(x => x.Lines).WithOne().HasForeignKey(l => l.PurchaseOrderId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PurchaseOrderLine>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.PurchaseOrderId).IsRequired();
                b.Property(x => x.ProductVariantId).IsRequired();
                b.Property(x => x.SupplierSku).HasMaxLength(100);
                b.Property(x => x.Description).HasMaxLength(1000);
                b.Property(x => x.Quantity);
                b.Property(x => x.ReceivedQuantity);
                b.Property(x => x.CancelledQuantity);
                b.Property(x => x.UnitCostMinor);
                b.Property(x => x.TotalCostMinor);
                b.Property(x => x.IsDeleted);
            });

            modelBuilder.Entity<GoodsReceipt>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.BusinessId).IsRequired();
                b.Property(x => x.SupplierId).IsRequired();
                b.Property(x => x.PurchaseOrderId).IsRequired();
                b.Property(x => x.WarehouseId).IsRequired();
                b.Property(x => x.GoodsReceiptNumber).HasMaxLength(100);
                b.Property(x => x.Status);
                b.Property(x => x.ReceivedAtUtc);
                b.Property(x => x.InspectedAtUtc);
                b.Property(x => x.PostedAtUtc);
                b.Property(x => x.CancelledAtUtc);
                b.Property(x => x.InternalNotes).HasMaxLength(4000);
                b.Property(x => x.MetadataJson);
                b.Property(x => x.IsDeleted);
                b.Ignore(x => x.RowVersion);
                b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.GoodsReceiptId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<GoodsReceiptLine>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.GoodsReceiptId).IsRequired();
                b.Property(x => x.PurchaseOrderLineId).IsRequired();
                b.Property(x => x.ProductVariantId).IsRequired();
                b.Property(x => x.SupplierSku).HasMaxLength(100);
                b.Property(x => x.Description).HasMaxLength(1000);
                b.Property(x => x.OrderedQuantity);
                b.Property(x => x.PreviouslyReceivedQuantity);
                b.Property(x => x.ReceivedQuantity);
                b.Property(x => x.AcceptedQuantity);
                b.Property(x => x.RejectedQuantity);
                b.Property(x => x.DamagedQuantity);
                b.Property(x => x.UnitCostMinor);
                b.Property(x => x.TotalCostMinor);
                b.Property(x => x.SortOrder);
                b.Property(x => x.IsDeleted);
            });

            modelBuilder.Entity<Order>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.OrderNumber).HasMaxLength(64).IsRequired();
                b.Property(x => x.BusinessId);
                b.Property(x => x.CustomerId);
                b.Property(x => x.Currency).HasMaxLength(3).IsRequired();
                b.Property(x => x.Status);
                b.Property(x => x.IsDeleted);
                b.Ignore(x => x.RowVersion);
                b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
                b.Ignore(x => x.Payments);
                b.Ignore(x => x.Shipments);
            });

            modelBuilder.Entity<OrderLine>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.OrderId).IsRequired();
                b.Property(x => x.VariantId);
                b.Property(x => x.WarehouseId);
                b.Property(x => x.Name).HasMaxLength(400).IsRequired();
                b.Property(x => x.Sku).HasMaxLength(128).IsRequired();
                b.Property(x => x.Quantity);
                b.Property(x => x.IsDeleted);
                b.Ignore(x => x.RowVersion);
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

            modelBuilder.Entity<NumberSequence>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.BusinessId);
                b.Property(x => x.DocumentType);
                b.Property(x => x.ScopeKey).HasMaxLength(128).IsRequired();
                b.Property(x => x.PrefixPattern).HasMaxLength(128).IsRequired();
                b.Property(x => x.NextValue);
                b.Property(x => x.PaddingLength);
                b.Property(x => x.ResetPolicy);
                b.Property(x => x.CurrentPeriodKey).HasMaxLength(32);
                b.Property(x => x.IsActive);
                b.Property(x => x.Description);
                b.Property(x => x.MetadataJson);
                b.Property(x => x.IsDeleted);
                b.Ignore(x => x.RowVersion);
            });

            modelBuilder.Entity<DocumentRecord>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
                b.Property(x => x.EntityId).IsRequired();
                b.Property(x => x.DocumentKind);
                b.Property(x => x.Title).HasMaxLength(200).IsRequired();
                b.Property(x => x.FileName).HasMaxLength(260).IsRequired();
                b.Property(x => x.ContentType).HasMaxLength(128).IsRequired();
                b.Property(x => x.SizeBytes);
                b.Property(x => x.ContentHash).HasMaxLength(128);
                b.Property(x => x.StorageProvider).HasMaxLength(64).IsRequired();
                b.Property(x => x.StorageContainer).HasMaxLength(128).IsRequired();
                b.Property(x => x.StorageKey).HasMaxLength(512).IsRequired();
                b.Property(x => x.Visibility);
                b.Property(x => x.MetadataJson);
                b.Property(x => x.IsDeleted);
                b.Ignore(x => x.RowVersion);
            });
        }
    }

    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow { get; } = new(2026, 6, 16, 12, 0, 0, DateTimeKind.Utc);
    }
}
