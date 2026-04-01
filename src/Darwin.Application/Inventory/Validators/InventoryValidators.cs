using Darwin.Application.Inventory.DTOs;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Inventory.Validators
{
    public sealed class InventoryAdjustValidator : AbstractValidator<InventoryAdjustDto>
    {
        public InventoryAdjustValidator()
        {
            RuleFor(x => x.VariantId).NotEmpty();
            RuleFor(x => x.QuantityDelta).NotEqual(0);
            RuleFor(x => x.Reason).NotEmpty().MaximumLength(64);
        }
    }

    public sealed class InventoryReserveValidator : AbstractValidator<InventoryReserveDto>
    {
        public InventoryReserveValidator()
        {
            RuleFor(x => x.VariantId).NotEmpty();
            RuleFor(x => x.Quantity).GreaterThan(0);
            RuleFor(x => x.Reason).NotEmpty().MaximumLength(64);
        }
    }

    public sealed class InventoryReleaseReservationValidator : AbstractValidator<InventoryReleaseReservationDto>
    {
        public InventoryReleaseReservationValidator()
        {
            RuleFor(x => x.VariantId).NotEmpty();
            RuleFor(x => x.Quantity).GreaterThan(0);
            RuleFor(x => x.Reason).NotEmpty().MaximumLength(64);
        }
    }

    /// <summary>
    /// Validation rules for <see cref="InventoryAllocateForOrderDto"/>.
    /// </summary>
    public sealed class InventoryAllocateForOrderValidator : AbstractValidator<InventoryAllocateForOrderDto>
    {
        public InventoryAllocateForOrderValidator(IStringLocalizer<ValidationResource> localizer)
        {
            RuleFor(x => x.OrderId).NotEmpty();
            RuleFor(x => x.Lines).NotNull().NotEmpty();
            RuleForEach(x => x.Lines).SetValidator(new InventoryAllocateForOrderLineValidator());

            // Optional: ensure no duplicate variant lines in a single request
            RuleFor(x => x.Lines)
                .Must(lines => lines.Select(l => l.VariantId).Distinct().Count() == lines.Count)
                .WithMessage(localizer["DuplicateVariantLinesNotAllowed"]);
        }
    }

    /// <summary>
    /// Validation rules for <see cref="InventoryAllocateForOrderLineDto"/>.
    /// </summary>
    public sealed class InventoryAllocateForOrderLineValidator : AbstractValidator<InventoryAllocateForOrderLineDto>
    {
        public InventoryAllocateForOrderLineValidator()
        {
            RuleFor(x => x.VariantId).NotEmpty();
            RuleFor(x => x.Quantity).GreaterThan(0);
        }
    }

    /// <summary>
    /// Validation rules for <see cref="InventoryReturnReceiptDto"/>.
    /// </summary>
    public sealed class InventoryReturnReceiptValidator : AbstractValidator<InventoryReturnReceiptDto>
    {
        public InventoryReturnReceiptValidator()
        {
            RuleFor(x => x.VariantId).NotEmpty();
            RuleFor(x => x.Quantity).GreaterThan(0);
            RuleFor(x => x.Reason).NotEmpty().MaximumLength(64);
        }
    }

    public sealed class WarehouseCreateValidator : AbstractValidator<WarehouseCreateDto>
    {
        public WarehouseCreateValidator()
        {
            RuleFor(x => x.BusinessId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(1000);
            RuleFor(x => x.Location).MaximumLength(500);
        }
    }

    public sealed class WarehouseEditValidator : AbstractValidator<WarehouseEditDto>
    {
        public WarehouseEditValidator()
        {
            Include(new WarehouseCreateValidator());
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotEmpty();
        }
    }

    public sealed class SupplierCreateValidator : AbstractValidator<SupplierCreateDto>
    {
        public SupplierCreateValidator()
        {
            RuleFor(x => x.BusinessId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
            RuleFor(x => x.Phone).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Address).MaximumLength(500);
            RuleFor(x => x.Notes).MaximumLength(2000);
        }
    }

    public sealed class SupplierEditValidator : AbstractValidator<SupplierEditDto>
    {
        public SupplierEditValidator()
        {
            Include(new SupplierCreateValidator());
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotEmpty();
        }
    }

    public sealed class StockLevelCreateValidator : AbstractValidator<StockLevelCreateDto>
    {
        public StockLevelCreateValidator()
        {
            RuleFor(x => x.WarehouseId).NotEmpty();
            RuleFor(x => x.ProductVariantId).NotEmpty();
            RuleFor(x => x.AvailableQuantity).GreaterThanOrEqualTo(0);
            RuleFor(x => x.ReservedQuantity).GreaterThanOrEqualTo(0);
            RuleFor(x => x.ReorderPoint).GreaterThanOrEqualTo(0);
            RuleFor(x => x.ReorderQuantity).GreaterThanOrEqualTo(0);
            RuleFor(x => x.InTransitQuantity).GreaterThanOrEqualTo(0);
        }
    }

    public sealed class StockLevelEditValidator : AbstractValidator<StockLevelEditDto>
    {
        public StockLevelEditValidator()
        {
            Include(new StockLevelCreateValidator());
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotEmpty();
        }
    }

    public sealed class StockTransferLineValidator : AbstractValidator<StockTransferLineDto>
    {
        public StockTransferLineValidator()
        {
            RuleFor(x => x.ProductVariantId).NotEmpty();
            RuleFor(x => x.Quantity).GreaterThan(0);
        }
    }

    public sealed class StockTransferCreateValidator : AbstractValidator<StockTransferCreateDto>
    {
        public StockTransferCreateValidator()
        {
            RuleFor(x => x.FromWarehouseId).NotEmpty();
            RuleFor(x => x.ToWarehouseId).NotEmpty().NotEqual(x => x.FromWarehouseId);
            RuleFor(x => x.Status).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Lines).NotEmpty();
            RuleForEach(x => x.Lines).SetValidator(new StockTransferLineValidator());
        }
    }

    public sealed class StockTransferEditValidator : AbstractValidator<StockTransferEditDto>
    {
        public StockTransferEditValidator()
        {
            Include(new StockTransferCreateValidator());
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotEmpty();
        }
    }

    public sealed class PurchaseOrderLineValidator : AbstractValidator<PurchaseOrderLineDto>
    {
        public PurchaseOrderLineValidator()
        {
            RuleFor(x => x.ProductVariantId).NotEmpty();
            RuleFor(x => x.Quantity).GreaterThan(0);
            RuleFor(x => x.UnitCostMinor).GreaterThanOrEqualTo(0);
            RuleFor(x => x.TotalCostMinor).GreaterThanOrEqualTo(0);
        }
    }

    public sealed class PurchaseOrderCreateValidator : AbstractValidator<PurchaseOrderCreateDto>
    {
        public PurchaseOrderCreateValidator()
        {
            RuleFor(x => x.SupplierId).NotEmpty();
            RuleFor(x => x.BusinessId).NotEmpty();
            RuleFor(x => x.OrderNumber).NotEmpty().MaximumLength(64);
            RuleFor(x => x.Status).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Lines).NotEmpty();
            RuleForEach(x => x.Lines).SetValidator(new PurchaseOrderLineValidator());
        }
    }

    public sealed class PurchaseOrderEditValidator : AbstractValidator<PurchaseOrderEditDto>
    {
        public PurchaseOrderEditValidator()
        {
            Include(new PurchaseOrderCreateValidator());
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotEmpty();
        }
    }
}
