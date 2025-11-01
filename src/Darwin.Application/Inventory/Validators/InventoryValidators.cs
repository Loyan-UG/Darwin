using Darwin.Application.Inventory.DTOs;
using FluentValidation;

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
        public InventoryAllocateForOrderValidator()
        {
            RuleFor(x => x.OrderId).NotEmpty();
            RuleFor(x => x.Lines).NotNull().NotEmpty();
            RuleForEach(x => x.Lines).SetValidator(new InventoryAllocateForOrderLineValidator());

            // Optional: ensure no duplicate variant lines in a single request
            RuleFor(x => x.Lines)
                .Must(lines => lines.Select(l => l.VariantId).Distinct().Count() == lines.Count)
                .WithMessage("Duplicate variant lines are not allowed.");
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
}
