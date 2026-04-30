using Darwin.Application.Identity.DTOs;
using FluentValidation;

namespace Darwin.Application.Identity.Validators
{
    public sealed class TotpProvisionValidator : AbstractValidator<TotpProvisionDto>
    {
        public TotpProvisionValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Issuer)
                .NotEmpty()
                .MaximumLength(64);
            RuleFor(x => x.AccountLabelOverride)
                .MaximumLength(256);
        }
    }

    public sealed class TotpEnableValidator : AbstractValidator<TotpEnableDto>
    {
        public TotpEnableValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Code).InclusiveBetween(0, 999999);
        }
    }

    public sealed class TotpDisableValidator : AbstractValidator<TotpDisableDto>
    {
        public TotpDisableValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
        }
    }

    public sealed class TotpVerifyValidator : AbstractValidator<TotpVerifyDto>
    {
        public TotpVerifyValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Code).InclusiveBetween(0, 999999);
        }
    }
}
