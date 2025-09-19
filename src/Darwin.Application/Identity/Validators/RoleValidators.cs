using Darwin.Application.Identity.DTOs;
using FluentValidation;

namespace Darwin.Application.Identity.Validators
{
    public sealed class RoleCreateValidator : AbstractValidator<RoleCreateDto>
    {
        public RoleCreateValidator()
        {
            RuleFor(x => x.Key).NotEmpty().MinimumLength(2);
            RuleFor(x => x.DisplayName).NotEmpty();
        }
    }

    public sealed class RoleEditValidator : AbstractValidator<RoleEditDto>
    {
        public RoleEditValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotNull();
            RuleFor(x => x.DisplayName).NotEmpty();
        }
    }
}
