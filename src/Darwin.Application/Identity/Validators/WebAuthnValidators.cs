using Darwin.Application.Identity.DTOs;
using FluentValidation;

namespace Darwin.Application.Identity.Validators;

public sealed class WebAuthnBeginRegisterValidator : AbstractValidator<WebAuthnBeginRegisterDto>
{
    public WebAuthnBeginRegisterValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.UserName).MaximumLength(256);
        RuleFor(x => x.DisplayName).MaximumLength(256);
    }
}

public sealed class WebAuthnFinishRegisterValidator : AbstractValidator<WebAuthnFinishRegisterDto>
{
    public WebAuthnFinishRegisterValidator()
    {
        RuleFor(x => x.ChallengeTokenId).NotEmpty();
        RuleFor(x => x.ClientResponseJson).NotEmpty().MaximumLength(32 * 1024);
    }
}

public sealed class WebAuthnBeginLoginValidator : AbstractValidator<WebAuthnBeginLoginDto>
{
    public WebAuthnBeginLoginValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public sealed class WebAuthnFinishLoginValidator : AbstractValidator<WebAuthnFinishLoginDto>
{
    public WebAuthnFinishLoginValidator()
    {
        RuleFor(x => x.ChallengeTokenId).NotEmpty();
        RuleFor(x => x.ClientResponseJson).NotEmpty().MaximumLength(32 * 1024);
    }
}
