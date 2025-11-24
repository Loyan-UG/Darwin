using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Common.DTOs;
using FluentValidation;

namespace Darwin.Application.Businesses.Validators
{

    /// <summary>
    /// Validator for creating business locations.
    /// </summary>
    public sealed class BusinessLocationCreateDtoValidator : AbstractValidator<BusinessLocationCreateDto>
    {
        public BusinessLocationCreateDtoValidator()
        {
            RuleFor(x => x.BusinessId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);

            RuleFor(x => x.AddressLine1).MaximumLength(300);
            RuleFor(x => x.AddressLine2).MaximumLength(300);
            RuleFor(x => x.City).MaximumLength(200);
            RuleFor(x => x.Region).MaximumLength(200);
            RuleFor(x => x.CountryCode).MaximumLength(2);
            RuleFor(x => x.PostalCode).MaximumLength(20);

            RuleFor(x => x.OpeningHoursJson).MaximumLength(8000);
            RuleFor(x => x.InternalNote).MaximumLength(2000);

            When(x => x.Coordinate != null, () =>
            {
                RuleFor(x => x.Coordinate!.Latitude).InclusiveBetween(-90, 90);
                RuleFor(x => x.Coordinate!.Longitude).InclusiveBetween(-180, 180);
            });
        }
    }

    /// <summary>
    /// Validator for editing business locations.
    /// </summary>
    public sealed class BusinessLocationEditDtoValidator : AbstractValidator<BusinessLocationEditDto>
    {
        public BusinessLocationEditDtoValidator()
        {
            RuleFor(x => x.Id).NotEmpty();

            RuleFor(x => x.BusinessId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);

            RuleFor(x => x.AddressLine1).MaximumLength(300);
            RuleFor(x => x.AddressLine2).MaximumLength(300);
            RuleFor(x => x.City).MaximumLength(200);
            RuleFor(x => x.Region).MaximumLength(200);
            RuleFor(x => x.CountryCode).MaximumLength(2);
            RuleFor(x => x.PostalCode).MaximumLength(20);

            RuleFor(x => x.OpeningHoursJson).MaximumLength(8000);
            RuleFor(x => x.InternalNote).MaximumLength(2000);

            When(x => x.Coordinate != null, () =>
            {
                RuleFor(x => x.Coordinate!.Latitude).InclusiveBetween(-90, 90);
                RuleFor(x => x.Coordinate!.Longitude).InclusiveBetween(-180, 180);
            });

            RuleFor(x => x.RowVersion).NotNull().NotEmpty();
        }
    }

    /// <summary>
    /// Validator for soft deleting business locations.
    /// </summary>
    public sealed class BusinessLocationDeleteDtoValidator : AbstractValidator<BusinessLocationDeleteDto>
    {
        public BusinessLocationDeleteDtoValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotNull().NotEmpty();
        }
    }

    
}
