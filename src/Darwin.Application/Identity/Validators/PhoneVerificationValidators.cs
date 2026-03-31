using Darwin.Application.Identity.DTOs;
using FluentValidation;

namespace Darwin.Application.Identity.Validators;

public sealed class RequestPhoneVerificationValidator : AbstractValidator<RequestPhoneVerificationDto>
{
    public RequestPhoneVerificationValidator()
    {
        RuleFor(x => x.Channel)
            .IsInEnum()
            .When(x => x.Channel.HasValue);
    }
}

public sealed class ConfirmPhoneVerificationValidator : AbstractValidator<ConfirmPhoneVerificationDto>
{
    public ConfirmPhoneVerificationValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .Length(4, 8)
            .Matches("^[0-9]+$");
    }
}
