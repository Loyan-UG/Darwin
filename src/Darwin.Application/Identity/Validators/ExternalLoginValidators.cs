using Darwin.Application.Identity.DTOs;
using FluentValidation;

namespace Darwin.Application.Identity.Validators
{
    public sealed class LinkExternalLoginValidator : AbstractValidator<LinkExternalLoginDto>
    {
        public LinkExternalLoginValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Provider).NotEmpty();
            RuleFor(x => x.ProviderKey).NotEmpty();
        }
    }

    public sealed class UnlinkExternalLoginValidator : AbstractValidator<UnlinkExternalLoginDto>
    {
        public UnlinkExternalLoginValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Provider).NotEmpty();
            RuleFor(x => x.ProviderKey).NotEmpty();
        }
    }
}
