using Darwin.Application.Identity.DTOs;
using FluentValidation;

namespace Darwin.Application.Identity.Validators
{
    public sealed class RoleCreateValidator : AbstractValidator<RoleCreateDto>
    {
        public RoleCreateValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
            RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(256);
            RuleFor(x => x.Description).MaximumLength(2000);
        }
    }

    public sealed class RoleEditValidator : AbstractValidator<RoleEditDto>
    {
        public RoleEditValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(256);
            RuleFor(x => x.Description).MaximumLength(2000);
        }
    }
}
