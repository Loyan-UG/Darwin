using Darwin.Application.Identity.DTOs;
using FluentValidation;

namespace Darwin.Application.Identity.Validators
{
    public sealed class RequestPasswordResetValidator : AbstractValidator<RequestPasswordResetDto>
    {
        public RequestPasswordResetValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
        }
    }

    public sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordDto>
    {
        public ResetPasswordValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Token).NotEmpty();
            RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
        }
    }
}
