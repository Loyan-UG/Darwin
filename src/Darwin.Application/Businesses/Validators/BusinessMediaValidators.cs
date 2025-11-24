using Darwin.Application.Businesses.DTOs;
using FluentValidation;

namespace Darwin.Application.Businesses.Validators
{
    /// <summary>
    /// Validator for creating media items.
    /// </summary>
    public sealed class BusinessMediaCreateDtoValidator : AbstractValidator<BusinessMediaCreateDto>
    {
        public BusinessMediaCreateDtoValidator()
        {
            RuleFor(x => x.BusinessId).NotEmpty();
            RuleFor(x => x.Url).NotEmpty().MaximumLength(1000);
            RuleFor(x => x.Caption).MaximumLength(500);
        }
    }

    /// <summary>
    /// Validator for editing media items.
    /// </summary>
    public sealed class BusinessMediaEditDtoValidator : AbstractValidator<BusinessMediaEditDto>
    {
        public BusinessMediaEditDtoValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.BusinessId).NotEmpty();
            RuleFor(x => x.Url).NotEmpty().MaximumLength(1000);
            RuleFor(x => x.Caption).MaximumLength(500);
            RuleFor(x => x.RowVersion).NotNull().NotEmpty();
        }
    }

    /// <summary>
    /// Validator for hard deleting media items.
    /// </summary>
    public sealed class BusinessMediaDeleteDtoValidator : AbstractValidator<BusinessMediaDeleteDto>
    {
        public BusinessMediaDeleteDtoValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotNull().NotEmpty();
        }
    }
}
