using Darwin.Application.Identity.DTOs;
using FluentValidation;

namespace Darwin.Application.Identity.Validators
{
    /// <summary>
    /// Validation rules for <see cref="UserProfileEditDto"/>.
    /// </summary>
    public sealed class UserProfileEditValidator : AbstractValidator<UserProfileEditDto>
    {
        public UserProfileEditValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotNull();
            RuleFor(x => x.Locale).NotEmpty();
            RuleFor(x => x.Timezone).NotEmpty();
            RuleFor(x => x.Currency).NotEmpty().Length(3);
        }
    }

    /// <summary>
    /// Validation rules for <see cref="UserChangeEmailDto"/>.
    /// </summary>
    public sealed class UserChangeEmailValidator : AbstractValidator<UserChangeEmailDto>
    {
        public UserChangeEmailValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.NewEmail).NotEmpty().EmailAddress();
        }
    }

    /// <summary>
    /// Validation rules for <see cref="UserDeleteDto"/>.
    /// </summary>
    public sealed class UserDeleteValidator : AbstractValidator<UserDeleteDto>
    {
        public UserDeleteValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotNull();
        }
    }
}
