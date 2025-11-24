using Darwin.Application.Businesses.DTOs;
using FluentValidation;

namespace Darwin.Application.Businesses.Validators
{
    /// <summary>
    /// Validator for creating businesses.
    /// </summary>
    public sealed class BusinessCreateDtoValidator : AbstractValidator<BusinessCreateDto>
    {
        public BusinessCreateDtoValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.DefaultCurrency).NotEmpty().MaximumLength(3);
            RuleFor(x => x.DefaultCulture).NotEmpty().MaximumLength(20);

            RuleFor(x => x.WebsiteUrl).MaximumLength(500);
            RuleFor(x => x.ContactEmail).MaximumLength(200).EmailAddress().When(x => x.ContactEmail != null);
            RuleFor(x => x.ContactPhoneE164).MaximumLength(30);
            RuleFor(x => x.LegalName).MaximumLength(300);
            RuleFor(x => x.TaxId).MaximumLength(100);
            RuleFor(x => x.ShortDescription).MaximumLength(1000);
        }
    }

    /// <summary>
    /// Validator for editing businesses (includes RowVersion rules).
    /// </summary>
    public sealed class BusinessEditDtoValidator : AbstractValidator<BusinessEditDto>
    {
        public BusinessEditDtoValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.DefaultCurrency).NotEmpty().MaximumLength(3);
            RuleFor(x => x.DefaultCulture).NotEmpty().MaximumLength(20);

            RuleFor(x => x.WebsiteUrl).MaximumLength(500);
            RuleFor(x => x.ContactEmail).MaximumLength(200).EmailAddress().When(x => x.ContactEmail != null);
            RuleFor(x => x.ContactPhoneE164).MaximumLength(30);
            RuleFor(x => x.LegalName).MaximumLength(300);
            RuleFor(x => x.TaxId).MaximumLength(100);
            RuleFor(x => x.ShortDescription).MaximumLength(1000);
            RuleFor(x => x.RowVersion).NotNull().NotEmpty();
        }
    }

    /// <summary>
    /// Validator for soft deleting businesses.
    /// </summary>
    public sealed class BusinessDeleteDtoValidator : AbstractValidator<BusinessDeleteDto>
    {
        public BusinessDeleteDtoValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotNull().NotEmpty();
        }
    }

}
