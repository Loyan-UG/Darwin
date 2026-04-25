using System;
using System.Collections.Generic;
using Darwin.Application.Inventory.DTOs;
using Darwin.Application.Inventory.Validators;
using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace Darwin.Tests.Unit.Inventory;

/// <summary>
/// Unit tests for the inventory operation validators:
/// <see cref="InventoryAdjustValidator"/>, <see cref="InventoryReserveValidator"/>,
/// <see cref="InventoryReleaseReservationValidator"/>, <see cref="InventoryAllocateForOrderValidator"/>,
/// <see cref="WarehouseCreateValidator"/>, <see cref="WarehouseEditValidator"/>,
/// <see cref="SupplierCreateValidator"/>, <see cref="SupplierEditValidator"/>,
/// <see cref="StockLevelCreateValidator"/>, <see cref="StockLevelEditValidator"/>,
/// <see cref="StockTransferCreateValidator"/>, <see cref="StockTransferEditValidator"/>,
/// <see cref="PurchaseOrderCreateValidator"/>, and <see cref="PurchaseOrderEditValidator"/>.
/// </summary>
public sealed class InventoryValidatorTests
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
    // InventoryAdjustValidator
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void InventoryAdjust_Should_Pass_For_Valid_Positive_Delta()
    {
        var dto = new InventoryAdjustDto
        {
            VariantId = Guid.NewGuid(),
            QuantityDelta = 10,
            Reason = "GoodsReceipt"
        };

        var result = new InventoryAdjustValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a positive delta with required fields should pass");
    }

    [Fact]
    public void InventoryAdjust_Should_Pass_For_Negative_Delta()
    {
        var dto = new InventoryAdjustDto
        {
            VariantId = Guid.NewGuid(),
            QuantityDelta = -5,
            Reason = "WriteOff"
        };

        var result = new InventoryAdjustValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a negative delta (write-off) with required fields should pass");
    }

    [Fact]
    public void InventoryAdjust_Should_Fail_When_VariantId_Empty()
    {
        var dto = new InventoryAdjustDto
        {
            VariantId = Guid.Empty,
            QuantityDelta = 5,
            Reason = "GoodsReceipt"
        };

        var result = new InventoryAdjustValidator().Validate(dto);

        result.IsValid.Should().BeFalse("VariantId is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.VariantId));
    }

    [Fact]
    public void InventoryAdjust_Should_Fail_When_QuantityDelta_Zero()
    {
        var dto = new InventoryAdjustDto
        {
            VariantId = Guid.NewGuid(),
            QuantityDelta = 0,
            Reason = "GoodsReceipt"
        };

        var result = new InventoryAdjustValidator().Validate(dto);

        result.IsValid.Should().BeFalse("a zero delta would be a no-op and is rejected");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.QuantityDelta));
    }

    [Fact]
    public void InventoryAdjust_Should_Fail_When_Reason_Empty()
    {
        var dto = new InventoryAdjustDto
        {
            VariantId = Guid.NewGuid(),
            QuantityDelta = 1,
            Reason = ""
        };

        var result = new InventoryAdjustValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Reason is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Reason));
    }

    [Fact]
    public void InventoryAdjust_Should_Fail_When_Reason_Too_Long()
    {
        var dto = new InventoryAdjustDto
        {
            VariantId = Guid.NewGuid(),
            QuantityDelta = 1,
            Reason = new string('R', 65)
        };

        var result = new InventoryAdjustValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Reason must not exceed 64 characters");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // InventoryReserveValidator
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void InventoryReserve_Should_Pass_For_Valid_Dto()
    {
        var dto = new InventoryReserveDto
        {
            VariantId = Guid.NewGuid(),
            Quantity = 2,
            Reason = "CartReservation"
        };

        var result = new InventoryReserveValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a valid reservation request should pass");
    }

    [Fact]
    public void InventoryReserve_Should_Fail_When_VariantId_Empty()
    {
        var dto = new InventoryReserveDto { VariantId = Guid.Empty, Quantity = 1, Reason = "Reservation" };

        var result = new InventoryReserveValidator().Validate(dto);

        result.IsValid.Should().BeFalse("VariantId is required");
    }

    [Fact]
    public void InventoryReserve_Should_Fail_When_Quantity_Zero()
    {
        var dto = new InventoryReserveDto { VariantId = Guid.NewGuid(), Quantity = 0, Reason = "Reservation" };

        var result = new InventoryReserveValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Quantity must be positive");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Quantity));
    }

    [Fact]
    public void InventoryReserve_Should_Fail_When_Quantity_Negative()
    {
        var dto = new InventoryReserveDto { VariantId = Guid.NewGuid(), Quantity = -1, Reason = "Reservation" };

        var result = new InventoryReserveValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Quantity must be positive");
    }

    [Fact]
    public void InventoryReserve_Should_Fail_When_Reason_Empty()
    {
        var dto = new InventoryReserveDto { VariantId = Guid.NewGuid(), Quantity = 1, Reason = "" };

        var result = new InventoryReserveValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Reason is required");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // InventoryReleaseReservationValidator
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void InventoryReleaseReservation_Should_Pass_For_Valid_Dto()
    {
        var dto = new InventoryReleaseReservationDto
        {
            VariantId = Guid.NewGuid(),
            Quantity = 3,
            Reason = "CartAbandonment"
        };

        var result = new InventoryReleaseReservationValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a valid release request should pass");
    }

    [Fact]
    public void InventoryReleaseReservation_Should_Fail_When_VariantId_Empty()
    {
        var dto = new InventoryReleaseReservationDto { VariantId = Guid.Empty, Quantity = 1, Reason = "Release" };

        var result = new InventoryReleaseReservationValidator().Validate(dto);

        result.IsValid.Should().BeFalse("VariantId is required");
    }

    [Fact]
    public void InventoryReleaseReservation_Should_Fail_When_Quantity_Zero()
    {
        var dto = new InventoryReleaseReservationDto { VariantId = Guid.NewGuid(), Quantity = 0, Reason = "Release" };

        var result = new InventoryReleaseReservationValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Quantity must be positive");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // InventoryAllocateForOrderValidator
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void InventoryAllocateForOrder_Should_Pass_For_Valid_Dto()
    {
        var dto = new InventoryAllocateForOrderDto
        {
            OrderId = Guid.NewGuid(),
            Lines = new List<InventoryAllocateForOrderLineDto>
            {
                new() { VariantId = Guid.NewGuid(), Quantity = 2 }
            }
        };

        var result = new InventoryAllocateForOrderValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("a valid allocation request with one line should pass");
    }

    [Fact]
    public void InventoryAllocateForOrder_Should_Fail_When_OrderId_Empty()
    {
        var dto = new InventoryAllocateForOrderDto
        {
            OrderId = Guid.Empty,
            Lines = new List<InventoryAllocateForOrderLineDto>
            {
                new() { VariantId = Guid.NewGuid(), Quantity = 1 }
            }
        };

        var result = new InventoryAllocateForOrderValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("OrderId is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.OrderId));
    }

    [Fact]
    public void InventoryAllocateForOrder_Should_Fail_When_Lines_Empty()
    {
        var dto = new InventoryAllocateForOrderDto
        {
            OrderId = Guid.NewGuid(),
            Lines = new List<InventoryAllocateForOrderLineDto>()
        };

        var result = new InventoryAllocateForOrderValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("at least one allocation line is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Lines));
    }

    [Fact]
    public void InventoryAllocateForOrder_Should_Fail_When_Duplicate_VariantIds()
    {
        var variantId = Guid.NewGuid();
        var dto = new InventoryAllocateForOrderDto
        {
            OrderId = Guid.NewGuid(),
            Lines = new List<InventoryAllocateForOrderLineDto>
            {
                new() { VariantId = variantId, Quantity = 1 },
                new() { VariantId = variantId, Quantity = 2 }
            }
        };

        var result = new InventoryAllocateForOrderValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("duplicate variant ids in a single allocation request are rejected");
    }

    [Fact]
    public void InventoryAllocateForOrder_Should_Fail_When_Line_Quantity_Zero()
    {
        var dto = new InventoryAllocateForOrderDto
        {
            OrderId = Guid.NewGuid(),
            Lines = new List<InventoryAllocateForOrderLineDto>
            {
                new() { VariantId = Guid.NewGuid(), Quantity = 0 }
            }
        };

        var result = new InventoryAllocateForOrderValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("line quantity must be positive");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // WarehouseCreateValidator / WarehouseEditValidator
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void WarehouseCreate_Should_Pass_For_Valid_Dto()
    {
        var dto = new WarehouseCreateDto
        {
            BusinessId = Guid.NewGuid(),
            Name = "Main Warehouse"
        };

        var result = new WarehouseCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a well-formed warehouse create request should pass");
    }

    [Fact]
    public void WarehouseCreate_Should_Fail_When_BusinessId_Empty()
    {
        var dto = new WarehouseCreateDto { BusinessId = Guid.Empty, Name = "WH" };

        var result = new WarehouseCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("BusinessId is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.BusinessId));
    }

    [Fact]
    public void WarehouseCreate_Should_Fail_When_Name_Empty()
    {
        var dto = new WarehouseCreateDto { BusinessId = Guid.NewGuid(), Name = "" };

        var result = new WarehouseCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Name is required");
    }

    [Fact]
    public void WarehouseCreate_Should_Fail_When_Name_Too_Long()
    {
        var dto = new WarehouseCreateDto { BusinessId = Guid.NewGuid(), Name = new string('W', 201) };

        var result = new WarehouseCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Name must not exceed 200 characters");
    }

    [Fact]
    public void WarehouseEdit_Should_Fail_When_Id_Empty()
    {
        var dto = new WarehouseEditDto
        {
            Id = Guid.Empty,
            RowVersion = new byte[] { 1 },
            BusinessId = Guid.NewGuid(),
            Name = "WH"
        };

        var result = new WarehouseEditValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Id is required for edit");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Id));
    }

    [Fact]
    public void WarehouseEdit_Should_Fail_When_RowVersion_Empty()
    {
        var dto = new WarehouseEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = Array.Empty<byte>(),
            BusinessId = Guid.NewGuid(),
            Name = "WH"
        };

        var result = new WarehouseEditValidator().Validate(dto);

        result.IsValid.Should().BeFalse("RowVersion must not be empty for edit");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.RowVersion));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SupplierCreateValidator / SupplierEditValidator
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SupplierCreate_Should_Pass_For_Valid_Dto()
    {
        var dto = new SupplierCreateDto
        {
            BusinessId = Guid.NewGuid(),
            Name = "Acme Supplies",
            Email = "contact@acme.example",
            Phone = "+49 30 12345"
        };

        var result = new SupplierCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a well-formed supplier create request should pass");
    }

    [Fact]
    public void SupplierCreate_Should_Fail_When_Email_Invalid()
    {
        var dto = new SupplierCreateDto
        {
            BusinessId = Guid.NewGuid(),
            Name = "Acme",
            Email = "not-an-email",
            Phone = "+49123"
        };

        var result = new SupplierCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("email must be a valid address");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Email));
    }

    [Fact]
    public void SupplierCreate_Should_Fail_When_Name_Empty()
    {
        var dto = new SupplierCreateDto
        {
            BusinessId = Guid.NewGuid(),
            Name = "",
            Email = "x@x.com",
            Phone = "123"
        };

        var result = new SupplierCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("supplier name is required");
    }

    [Fact]
    public void SupplierEdit_Should_Fail_When_Id_Empty()
    {
        var dto = new SupplierEditDto
        {
            Id = Guid.Empty,
            RowVersion = new byte[] { 1 },
            BusinessId = Guid.NewGuid(),
            Name = "Acme",
            Email = "x@x.com",
            Phone = "123"
        };

        var result = new SupplierEditValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Id is required for supplier edit");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Id));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // StockLevelCreateValidator / StockLevelEditValidator
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void StockLevelCreate_Should_Pass_For_Valid_Dto()
    {
        var dto = new StockLevelCreateDto
        {
            WarehouseId = Guid.NewGuid(),
            ProductVariantId = Guid.NewGuid(),
            AvailableQuantity = 100,
            ReservedQuantity = 10
        };

        var result = new StockLevelCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a well-formed stock level create request should pass");
    }

    [Fact]
    public void StockLevelCreate_Should_Fail_When_AvailableQuantity_Negative()
    {
        var dto = new StockLevelCreateDto
        {
            WarehouseId = Guid.NewGuid(),
            ProductVariantId = Guid.NewGuid(),
            AvailableQuantity = -1
        };

        var result = new StockLevelCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("AvailableQuantity must be >= 0");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.AvailableQuantity));
    }

    [Fact]
    public void StockLevelCreate_Should_Fail_When_WarehouseId_Empty()
    {
        var dto = new StockLevelCreateDto
        {
            WarehouseId = Guid.Empty,
            ProductVariantId = Guid.NewGuid()
        };

        var result = new StockLevelCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("WarehouseId is required");
    }

    [Fact]
    public void StockLevelEdit_Should_Fail_When_Id_Empty()
    {
        var dto = new StockLevelEditDto
        {
            Id = Guid.Empty,
            RowVersion = new byte[] { 1 },
            WarehouseId = Guid.NewGuid(),
            ProductVariantId = Guid.NewGuid()
        };

        var result = new StockLevelEditValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Id is required for stock level edit");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // StockTransferCreateValidator / StockTransferEditValidator
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void StockTransferCreate_Should_Pass_For_Valid_Dto()
    {
        var dto = new StockTransferCreateDto
        {
            FromWarehouseId = Guid.NewGuid(),
            ToWarehouseId = Guid.NewGuid(),
            Status = "Draft",
            Lines = new List<StockTransferLineDto>
            {
                new() { ProductVariantId = Guid.NewGuid(), Quantity = 5 }
            }
        };

        var result = new StockTransferCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a well-formed stock transfer should pass");
    }

    [Fact]
    public void StockTransferCreate_Should_Fail_When_FromAndTo_Same()
    {
        var warehouseId = Guid.NewGuid();
        var dto = new StockTransferCreateDto
        {
            FromWarehouseId = warehouseId,
            ToWarehouseId = warehouseId,
            Status = "Draft",
            Lines = new List<StockTransferLineDto>
            {
                new() { ProductVariantId = Guid.NewGuid(), Quantity = 1 }
            }
        };

        var result = new StockTransferCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("source and destination warehouse must differ");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.ToWarehouseId));
    }

    [Fact]
    public void StockTransferCreate_Should_Fail_When_Lines_Empty()
    {
        var dto = new StockTransferCreateDto
        {
            FromWarehouseId = Guid.NewGuid(),
            ToWarehouseId = Guid.NewGuid(),
            Status = "Draft",
            Lines = new List<StockTransferLineDto>()
        };

        var result = new StockTransferCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("at least one transfer line is required");
    }

    [Fact]
    public void StockTransferCreate_Should_Fail_When_Status_Empty()
    {
        var dto = new StockTransferCreateDto
        {
            FromWarehouseId = Guid.NewGuid(),
            ToWarehouseId = Guid.NewGuid(),
            Status = "",
            Lines = new List<StockTransferLineDto> { new() { ProductVariantId = Guid.NewGuid(), Quantity = 1 } }
        };

        var result = new StockTransferCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Status is required");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PurchaseOrderCreateValidator / PurchaseOrderEditValidator
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void PurchaseOrderCreate_Should_Pass_For_Valid_Dto()
    {
        var dto = new PurchaseOrderCreateDto
        {
            SupplierId = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            OrderNumber = "PO-2026-001",
            Status = "Draft",
            Lines = new List<PurchaseOrderLineDto>
            {
                new() { ProductVariantId = Guid.NewGuid(), Quantity = 10, UnitCostMinor = 500, TotalCostMinor = 5000 }
            }
        };

        var result = new PurchaseOrderCreateValidator().Validate(dto);

        result.IsValid.Should().BeTrue("a well-formed purchase order should pass");
    }

    [Fact]
    public void PurchaseOrderCreate_Should_Fail_When_OrderNumber_Empty()
    {
        var dto = new PurchaseOrderCreateDto
        {
            SupplierId = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            OrderNumber = "",
            Status = "Draft",
            Lines = new List<PurchaseOrderLineDto>
            {
                new() { ProductVariantId = Guid.NewGuid(), Quantity = 1 }
            }
        };

        var result = new PurchaseOrderCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("OrderNumber is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.OrderNumber));
    }

    [Fact]
    public void PurchaseOrderCreate_Should_Fail_When_Lines_Empty()
    {
        var dto = new PurchaseOrderCreateDto
        {
            SupplierId = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            OrderNumber = "PO-001",
            Status = "Draft",
            Lines = new List<PurchaseOrderLineDto>()
        };

        var result = new PurchaseOrderCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("at least one purchase order line is required");
    }

    [Fact]
    public void PurchaseOrderCreate_Should_Fail_When_Line_Quantity_Zero()
    {
        var dto = new PurchaseOrderCreateDto
        {
            SupplierId = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            OrderNumber = "PO-001",
            Status = "Draft",
            Lines = new List<PurchaseOrderLineDto>
            {
                new() { ProductVariantId = Guid.NewGuid(), Quantity = 0 }
            }
        };

        var result = new PurchaseOrderCreateValidator().Validate(dto);

        result.IsValid.Should().BeFalse("line quantity must be positive");
    }

    [Fact]
    public void PurchaseOrderEdit_Should_Fail_When_Id_Empty()
    {
        var dto = new PurchaseOrderEditDto
        {
            Id = Guid.Empty,
            RowVersion = new byte[] { 1 },
            SupplierId = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            OrderNumber = "PO-001",
            Status = "Draft",
            Lines = new List<PurchaseOrderLineDto>
            {
                new() { ProductVariantId = Guid.NewGuid(), Quantity = 1 }
            }
        };

        var result = new PurchaseOrderEditValidator().Validate(dto);

        result.IsValid.Should().BeFalse("Id is required for purchase order edit");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Id));
    }
}
