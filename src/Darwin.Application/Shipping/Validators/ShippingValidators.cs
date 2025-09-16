using Darwin.Application.Shipping.DTOs;
using FluentValidation;

namespace Darwin.Application.Shipping.Validators
{
    public sealed class ShippingMethodCreateValidator : AbstractValidator<ShippingMethodCreateDto>
    {
        public ShippingMethodCreateValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
            RuleFor(x => x.Carrier).NotEmpty().MaximumLength(64);
            RuleFor(x => x.Service).NotEmpty().MaximumLength(64);
            RuleForEach(x => x.Rates).SetValidator(new ShippingRateValidator());
            RuleFor(x => x.Currency).Length(3).When(x => !string.IsNullOrWhiteSpace(x.Currency));
        }
    }

    public sealed class ShippingMethodEditValidator : AbstractValidator<ShippingMethodEditDto>
    {
        public ShippingMethodEditValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotNull();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
            RuleFor(x => x.Carrier).NotEmpty().MaximumLength(64);
            RuleFor(x => x.Service).NotEmpty().MaximumLength(64);
            RuleForEach(x => x.Rates).SetValidator(new ShippingRateValidator());
            RuleFor(x => x.Currency).Length(3).When(x => !string.IsNullOrWhiteSpace(x.Currency));
        }
    }

    public sealed class ShippingRateValidator : AbstractValidator<ShippingRateDto>
    {
        public ShippingRateValidator()
        {
            RuleFor(r => r.SortOrder).GreaterThanOrEqualTo(0);
            RuleFor(r => r.PriceMinor).GreaterThanOrEqualTo(0);
            RuleFor(r => r.MaxShipmentMass).GreaterThan(0).When(r => r.MaxShipmentMass.HasValue);
            RuleFor(r => r.MaxSubtotalNetMinor).GreaterThan(0).When(r => r.MaxSubtotalNetMinor.HasValue);
        }
    }

    public sealed class RateShipmentInputValidator : AbstractValidator<RateShipmentInputDto>
    {
        public RateShipmentInputValidator()
        {
            RuleFor(x => x.Country).NotEmpty().Length(2, 2);
            RuleFor(x => x.SubtotalNetMinor).GreaterThanOrEqualTo(0);
            RuleFor(x => x.ShipmentMass).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Currency).Length(3).When(x => !string.IsNullOrWhiteSpace(x.Currency));
        }
    }
}
