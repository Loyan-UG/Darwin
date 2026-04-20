using Darwin.Application.Identity.DTOs;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Identity.Validators
{
    public sealed class SignInValidator : AbstractValidator<SignInDto>
    {
        public SignInValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty();
        }
    }

    public sealed class PasswordResetRequestValidator : AbstractValidator<PasswordResetRequestDto>
    {
        public PasswordResetRequestValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
        }
    }

    public sealed class PasswordResetConfirmValidator : AbstractValidator<PasswordResetConfirmDto>
    {
        public PasswordResetConfirmValidator(IStringLocalizer<ValidationResource> localizer)
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Token).NotEmpty();
            RuleFor(x => x.NewPassword).ApplyPasswordPolicy(localizer);
        }
    }

    public sealed class PasswordLoginRequestValidator : AbstractValidator<PasswordLoginRequestDto>
    {
        public PasswordLoginRequestValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.PasswordPlain).NotEmpty().MinimumLength(6);
        }
    }

    public sealed class RefreshRequestValidator : AbstractValidator<RefreshRequestDto>
    {
        public RefreshRequestValidator()
        {
            RuleFor(x => x.RefreshToken).NotEmpty();
        }
    }

    public sealed class RevokeRefreshRequestValidator : AbstractValidator<RevokeRefreshRequestDto>
    {
        public RevokeRefreshRequestValidator(IStringLocalizer<ValidationResource> localizer)
        {
            RuleFor(x => x).Must(x => x.UserId.HasValue || !string.IsNullOrWhiteSpace(x.RefreshToken))
                           .WithMessage(localizer["EitherUserIdOrRefreshTokenRequired"]);
        }
    }
}
