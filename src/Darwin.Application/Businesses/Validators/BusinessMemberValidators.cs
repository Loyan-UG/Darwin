using Darwin.Application.Businesses.DTOs;
using FluentValidation;


namespace Darwin.Application.Businesses.Validators
{
    /// <summary>
    /// Validator for creating a business member.
    /// </summary>
    public sealed class BusinessMemberCreateDtoValidator : AbstractValidator<BusinessMemberCreateDto>
    {
        public BusinessMemberCreateDtoValidator()
        {
            RuleFor(x => x.BusinessId).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
        }
    }

    /// <summary>
    /// Validator for editing a business member.
    /// </summary>
    public sealed class BusinessMemberEditDtoValidator : AbstractValidator<BusinessMemberEditDto>
    {
        public BusinessMemberEditDtoValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.BusinessId).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.RowVersion).NotEmpty();
            RuleFor(x => x.OverrideReason)
                .NotEmpty()
                .MaximumLength(1000)
                .When(x => x.AllowLastOwnerOverride);
        }
    }

    /// <summary>
    /// Validator for hard deleting a business member (still uses RowVersion for concurrency).
    /// </summary>
    public sealed class BusinessMemberDeleteDtoValidator : AbstractValidator<BusinessMemberDeleteDto>
    {
        public BusinessMemberDeleteDtoValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotEmpty();
            RuleFor(x => x.OverrideReason)
                .NotEmpty()
                .MaximumLength(1000)
                .When(x => x.AllowLastOwnerOverride);
        }
    }

}
