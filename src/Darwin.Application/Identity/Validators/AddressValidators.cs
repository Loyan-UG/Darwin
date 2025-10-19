using Darwin.Application.Identity.DTOs;
using FluentValidation;

namespace Darwin.Application.Identity.Validators
{
    /// <summary>
    /// Validation rules for creating an address.
    /// </summary>
    public sealed class AddressCreateValidator : AbstractValidator<AddressCreateDto>
    {
        public AddressCreateValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();

            RuleFor(x => x.Street1).NotEmpty().MaximumLength(300);
            RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(32);
            RuleFor(x => x.City).NotEmpty().MaximumLength(150);
            RuleFor(x => x.CountryCode).NotEmpty().Length(2);

            RuleFor(x => x.FullName).MaximumLength(200);
            RuleFor(x => x.Company).MaximumLength(200);
            RuleFor(x => x.Street2).MaximumLength(300);
            RuleFor(x => x.State).MaximumLength(150);
            RuleFor(x => x.PhoneE164).MaximumLength(20);
        }
    }

    /// <summary>
    /// Validation rules for editing an address.
    /// </summary>
    public sealed class AddressEditValidator : AbstractValidator<AddressEditDto>
    {
        public AddressEditValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotNull();

            RuleFor(x => x.Street1).NotEmpty().MaximumLength(300);
            RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(32);
            RuleFor(x => x.City).NotEmpty().MaximumLength(150);
            RuleFor(x => x.CountryCode).NotEmpty().Length(2);

            RuleFor(x => x.FullName).MaximumLength(200);
            RuleFor(x => x.Company).MaximumLength(200);
            RuleFor(x => x.Street2).MaximumLength(300);
            RuleFor(x => x.State).MaximumLength(150);
            RuleFor(x => x.PhoneE164).MaximumLength(20);
        }
    }

    /// <summary>
    /// Validation rules for deleting an address.
    /// </summary>
    public sealed class AddressDeleteValidator : AbstractValidator<AddressDeleteDto>
    {
        public AddressDeleteValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotNull();
        }
    }
}
