using Darwin.Application.Identity.DTOs;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Identity.Validators
{
    public sealed class RequestPasswordResetValidator : AbstractValidator<RequestPasswordResetDto>
    {
        public RequestPasswordResetValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
        }
    }

    /// <summary>
    /// Validation rules for requesting a new email confirmation token.
    /// </summary>
    public sealed class RequestEmailConfirmationValidator : AbstractValidator<RequestEmailConfirmationDto>
    {
        public RequestEmailConfirmationValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
        }
    }

    /// <summary>
    /// Validation rules for completing email confirmation.
    /// </summary>
    public sealed class ConfirmEmailValidator : AbstractValidator<ConfirmEmailDto>
    {
        public ConfirmEmailValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Token).NotEmpty();
        }
    }

    public sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordDto>
    {
        public ResetPasswordValidator(IStringLocalizer<ValidationResource> localizer)
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Token).NotEmpty();
            RuleFor(x => x.NewPassword).ApplyPasswordPolicy(localizer);
        }
    }
}
