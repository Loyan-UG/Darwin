using Darwin.Application.Shipping.DTOs;
using FluentValidation;
using System.Linq;
using System.Text.RegularExpressions;

namespace Darwin.Application.Shipping.Validators
{
    /// <summary>
    /// Validates the individual shipping rate tiers.
    /// </summary>
    public sealed class ShippingRateDtoValidator : AbstractValidator<ShippingRateDto>
    {
        public ShippingRateDtoValidator()
        {
            RuleFor(r => r.PriceMinor)
                .GreaterThanOrEqualTo(0).WithMessage("Rate price must be non-negative");

            RuleFor(r => r.MaxWeight)
                .GreaterThan(0).When(r => r.MaxWeight.HasValue)
                .WithMessage("MaxWeight must be positive");

            RuleFor(r => r.MaxSubtotalNetMinor)
                .GreaterThan(0).When(r => r.MaxSubtotalNetMinor.HasValue)
                .WithMessage("MaxSubtotalNetMinor must be positive");

            RuleFor(r => r.SortOrder)
                .GreaterThanOrEqualTo(0)
                .WithMessage("SortOrder must be zero or greater");
        }
    }

    /// <summary>
    /// Validates properties of <see cref="ShippingMethodCreateDto"/>.
    /// </summary>
    public sealed class ShippingMethodCreateDtoValidator : AbstractValidator<ShippingMethodCreateDto>
    {
        public ShippingMethodCreateDtoValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Carrier).MaximumLength(100);
            RuleFor(x => x.Service).MaximumLength(100);

            RuleFor(x => x.CountriesCsv)
                .NotEmpty().WithMessage("Countries list must not be empty")
                .Must(BeValidCountries).WithMessage("CountriesCsv must contain comma-separated ISO codes (e.g., DE,FR)");

            RuleFor(x => x.Rates).NotEmpty().WithMessage("At least one rate must be provided");

            RuleForEach(x => x.Rates).SetValidator(new ShippingRateDtoValidator());

            RuleFor(x => x.Rates)
                .Must(rates => rates.Select(r => r.SortOrder).Distinct().Count() == rates.Count)
                .WithMessage("SortOrder values must be unique within the rate list");
        }

        private static bool BeValidCountries(string csv)
        {
            var codes = csv.Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries);
            return codes.All(c => Regex.IsMatch(c, "^[A-Z]{2}$"));
        }
    }

    /// <summary>
    /// Validates properties of <see cref="ShippingMethodEditDto"/>.
    /// Includes all create rules plus concurrency and rate replacement checks.
    /// </summary>
    public sealed class ShippingMethodEditDtoValidator : AbstractValidator<ShippingMethodEditDto>
    {
        public ShippingMethodEditDtoValidator()
        {
            // concurrency token required
            RuleFor(x => x.RowVersion).NotNull().Must(r => r.Length > 0);

            // reuse base rules
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Carrier).MaximumLength(100);
            RuleFor(x => x.Service).MaximumLength(100);
            RuleFor(x => x.CountriesCsv)
                .NotEmpty().Must(ShippingMethodCreateDtoValidator.BeValidCountries)
                .WithMessage("CountriesCsv must contain comma-separated ISO codes (e.g., DE,FR)");

            RuleFor(x => x.Rates).NotEmpty();
            RuleForEach(x => x.Rates).SetValidator(new ShippingRateDtoValidator());
            RuleFor(x => x.Rates)
                .Must(rates => rates.Select(r => r.SortOrder).Distinct().Count() == rates.Count)
                .WithMessage("SortOrder values must be unique within the rate list");
        }
    }
}
