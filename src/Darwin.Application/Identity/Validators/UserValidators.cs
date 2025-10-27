using Darwin.Application.Identity.DTOs;
using FluentValidation;

namespace Darwin.Application.Identity.Validators
{
    public sealed class UserCreateValidator : AbstractValidator<UserCreateDto>
    {
        public UserCreateValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
            RuleFor(x => x.Locale).NotEmpty();
            RuleFor(x => x.Timezone).NotEmpty();
            RuleFor(x => x.Currency).NotEmpty().Length(3);
        }
    }

    public sealed class UserEditValidator : AbstractValidator<UserEditDto>
    {
        public UserEditValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotNull();
            RuleFor(x => x.Locale).NotEmpty();
            RuleFor(x => x.Timezone).NotEmpty();
            RuleFor(x => x.Currency).NotEmpty().Length(3);
        }
    }

    /// <summary>
    /// Validation rules for <see cref="UserAdminSetPasswordDto"/>.
    /// </summary>
    public sealed class UserAdminSetPasswordValidator : AbstractValidator<UserAdminSetPasswordDto>
    {
        public UserAdminSetPasswordValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .MinimumLength(8);
            // TODO: Consider adding stronger password policy (mixed case, digits, symbols) if required.
        }
    }

    public sealed class UserChangePasswordValidator : AbstractValidator<UserChangePasswordDto>
    {
        public UserChangePasswordValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.CurrentPassword).NotEmpty();
            RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
        }
    }
}
