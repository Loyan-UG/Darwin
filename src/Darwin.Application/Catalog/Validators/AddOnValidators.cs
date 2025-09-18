using Darwin.Application.Catalog.DTOs;
using Darwin.Domain.Entities.Catalog;
using FluentValidation;

namespace Darwin.Application.Catalog.Validators
{
    /// <summary>
    /// Validation for creating an add-on group with options and values.
    /// </summary>
    public sealed class AddOnGroupCreateValidator : AbstractValidator<AddOnGroupCreateDto>
    {
        public AddOnGroupCreateValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
            RuleFor(x => x.Currency).NotEmpty().Length(3);
            RuleFor(x => x.MinSelections).GreaterThanOrEqualTo(0);
            RuleFor(x => x.MaxSelections).GreaterThanOrEqualTo(0).When(x => x.MaxSelections.HasValue);
            RuleForEach(x => x.Options).SetValidator(new AddOnOptionValidator());
        }
    }

    /// <summary>
    /// Validation for editing an add-on group.
    /// </summary>
    public sealed class AddOnGroupEditValidator : AbstractValidator<AddOnGroupEditDto>
    {
        public AddOnGroupEditValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotNull();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
            RuleFor(x => x.Currency).NotEmpty().Length(3);
            RuleFor(x => x.MinSelections).GreaterThanOrEqualTo(0);
            RuleFor(x => x.MaxSelections).GreaterThanOrEqualTo(0).When(x => x.MaxSelections.HasValue);
            RuleForEach(x => x.Options).SetValidator(new AddOnOptionValidator());
        }
    }

    internal sealed class AddOnOptionValidator : AbstractValidator<AddOnOptionDto>
    {
        public AddOnOptionValidator()
        {
            RuleFor(x => x.Label).NotEmpty().MaximumLength(256);
            RuleForEach(x => x.Values).SetValidator(new AddOnOptionValueValidator());
        }
    }

    internal sealed class AddOnOptionValueValidator : AbstractValidator<AddOnOptionValueDto>
    {
        public AddOnOptionValueValidator()
        {
            RuleFor(x => x.Label).NotEmpty().MaximumLength(256);
            RuleFor(x => x.PriceDeltaMinor);
            RuleFor(x => x.Hint).MaximumLength(256).When(x => x.Hint != null);
        }
    }
}
