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
}
