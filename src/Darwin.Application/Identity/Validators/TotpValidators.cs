using Darwin.Application.Identity.DTOs;
using FluentValidation;

namespace Darwin.Application.Identity.Validators
{
    public sealed class BeginTotpProvisioningValidator : AbstractValidator<BeginTotpProvisioningDto>
    {
        public BeginTotpProvisioningValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Label).NotEmpty();
            RuleFor(x => x.Issuer).NotEmpty();
        }
    }

    public sealed class VerifyTotpValidator : AbstractValidator<VerifyTotpDto>
    {
        public VerifyTotpValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Code).NotEmpty().Length(6);
        }
    }

    public sealed class DisableTotpValidator : AbstractValidator<DisableTotpDto>
    {
        public DisableTotpValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
        }
    }
}
