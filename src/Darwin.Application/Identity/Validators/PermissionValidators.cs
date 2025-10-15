using Darwin.Application.Identity.DTOs;
using FluentValidation;

namespace Darwin.Application.Identity.Validators
{
    public sealed class PermissionCreateValidator : AbstractValidator<PermissionCreateDto>
    {
        public PermissionCreateValidator()
        {
            RuleFor(x => x.Key).NotEmpty().MinimumLength(3);
            RuleFor(x => x.DisplayName).NotEmpty();
        }
    }

    public sealed class PermissionEditValidator : AbstractValidator<PermissionEditDto>
    {
        public PermissionEditValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotNull();
            RuleFor(x => x.DisplayName).NotEmpty();
        }
    }

    
    /// <summary>
    /// Validation rules for deleting a permission.
    /// </summary>
    public sealed class PermissionDeleteValidator : AbstractValidator<PermissionDeleteDto>
    {
        public PermissionDeleteValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotNull();
        }
    }
}
