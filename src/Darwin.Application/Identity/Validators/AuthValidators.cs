using Darwin.Application.Identity.DTOs;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Identity.Validators
{
    public sealed class SignInValidator : AbstractValidator<SignInDto>
    {
        public SignInValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
            RuleFor(x => x.Password).NotEmpty().MaximumLength(256);
            RuleFor(x => x.AntiBotToken).MaximumLength(2048);
            RuleFor(x => x.AntiBotHoneypot).MaximumLength(256);
            RuleFor(x => x.ClientIpAddress).MaximumLength(64);
            RuleFor(x => x.UserAgent).MaximumLength(512);
        }
    }

    public sealed class PasswordResetRequestValidator : AbstractValidator<PasswordResetRequestDto>
    {
        public PasswordResetRequestValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        }
    }

    public sealed class PasswordResetConfirmValidator : AbstractValidator<PasswordResetConfirmDto>
    {
        public PasswordResetConfirmValidator(IStringLocalizer<ValidationResource> localizer)
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
            RuleFor(x => x.Token).NotEmpty().MaximumLength(256);
            RuleFor(x => x.NewPassword).ApplyPasswordPolicy(localizer);
        }
    }

    public sealed class PasswordLoginRequestValidator : AbstractValidator<PasswordLoginRequestDto>
    {
        public PasswordLoginRequestValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
            RuleFor(x => x.PasswordPlain).NotEmpty().MinimumLength(6).MaximumLength(256);
            RuleFor(x => x.DeviceId).MaximumLength(128);
        }
    }

    public sealed class RefreshRequestValidator : AbstractValidator<RefreshRequestDto>
    {
        public RefreshRequestValidator()
        {
            RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(512);
            RuleFor(x => x.DeviceId).MaximumLength(128);
        }
    }

    public sealed class RevokeRefreshRequestValidator : AbstractValidator<RevokeRefreshRequestDto>
    {
        public RevokeRefreshRequestValidator(IStringLocalizer<ValidationResource> localizer)
        {
            RuleFor(x => x).Must(x => x.UserId.HasValue || !string.IsNullOrWhiteSpace(x.RefreshToken))
                           .WithMessage(localizer["EitherUserIdOrRefreshTokenRequired"]);
            RuleFor(x => x.RefreshToken).MaximumLength(512);
            RuleFor(x => x.DeviceId).MaximumLength(128);
        }
    }
}
