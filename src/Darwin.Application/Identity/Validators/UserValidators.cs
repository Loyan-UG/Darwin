using Darwin.Application.Identity.DTOs;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Identity.Validators
{
    public sealed class UserCreateValidator : AbstractValidator<UserCreateDto>
    {
        public UserCreateValidator(IStringLocalizer<ValidationResource> localizer)
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).ApplyPasswordPolicy(localizer);
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
        public UserAdminSetPasswordValidator(IStringLocalizer<ValidationResource> localizer)
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.NewPassword).ApplyPasswordPolicy(localizer);
        }
    }

    public sealed class UserChangePasswordValidator : AbstractValidator<UserChangePasswordDto>
    {
        public UserChangePasswordValidator(IStringLocalizer<ValidationResource> localizer)
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.CurrentPassword).NotEmpty();
            RuleFor(x => x.NewPassword).ApplyPasswordPolicy(localizer);
        }
    }

    /// <summary>
    /// Validation rules for admin user actions that only need a target identifier.
    /// </summary>
    public sealed class UserAdminActionValidator : AbstractValidator<UserAdminActionDto>
    {
        public UserAdminActionValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
