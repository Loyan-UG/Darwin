using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Identity.Validators;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Businesses.Validators
{
    /// <summary>
    /// Validator for issuing business invitations.
    /// </summary>
    public sealed class BusinessInvitationCreateDtoValidator : AbstractValidator<BusinessInvitationCreateDto>
    {
        public BusinessInvitationCreateDtoValidator()
        {
            RuleFor(x => x.BusinessId).NotEmpty();
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
            RuleFor(x => x.ExpiresInDays).InclusiveBetween(1, 30);
            RuleFor(x => x.Note).MaximumLength(2000);
        }
    }

    /// <summary>
    /// Validator for resending business invitations.
    /// </summary>
    public sealed class BusinessInvitationResendDtoValidator : AbstractValidator<BusinessInvitationResendDto>
    {
        public BusinessInvitationResendDtoValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.ExpiresInDays).InclusiveBetween(1, 30);
            RuleFor(x => x.RowVersion).NotEmpty();
        }
    }

    /// <summary>
    /// Validator for revoking business invitations.
    /// </summary>
    public sealed class BusinessInvitationRevokeDtoValidator : AbstractValidator<BusinessInvitationRevokeDto>
    {
        public BusinessInvitationRevokeDtoValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Note).MaximumLength(2000);
            RuleFor(x => x.RowVersion).NotEmpty();
        }
    }

    /// <summary>
    /// Validator for accepting business invitations.
    /// </summary>
    public sealed class BusinessInvitationAcceptDtoValidator : AbstractValidator<BusinessInvitationAcceptDto>
    {
        public BusinessInvitationAcceptDtoValidator(IStringLocalizer<ValidationResource>? localizer = null)
        {
            var effectiveLocalizer = localizer ?? DefaultHandlerDependencies.DefaultLocalizer;

            RuleFor(x => x.Token).NotEmpty().MinimumLength(12).MaximumLength(256);
            RuleFor(x => x.DeviceId).MaximumLength(128);
            RuleFor(x => x.FirstName).MaximumLength(128);
            RuleFor(x => x.LastName).MaximumLength(128);

            When(x => !string.IsNullOrWhiteSpace(x.Password), () =>
            {
                RuleFor(x => x.Password!).MaximumLength(256).ApplyPasswordPolicy(effectiveLocalizer);
            });
        }
    }
}
