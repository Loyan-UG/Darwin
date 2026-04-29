using Darwin.Application.AdminTextOverrides;
using Darwin.Application.Businesses.DTOs;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Businesses.Validators
{
    /// <summary>
    /// Validator for creating businesses.
    /// </summary>
    public sealed class BusinessCreateDtoValidator : AbstractValidator<BusinessCreateDto>
    {
        public BusinessCreateDtoValidator(IStringLocalizer<ValidationResource> localizer)
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.DefaultCurrency).NotEmpty().MaximumLength(3);
            RuleFor(x => x.DefaultCulture).NotEmpty().MaximumLength(20);
            RuleFor(x => x.DefaultTimeZoneId).NotEmpty().MaximumLength(64);
            RuleFor(x => x.AdminTextOverridesJson).MaximumLength(16000);
            RuleFor(x => x.AdminTextOverridesJson)
                .Must(BusinessValidatorJsonHelpers.BeAdminTextOverridesJson)
                .When(x => !string.IsNullOrWhiteSpace(x.AdminTextOverridesJson))
                .WithMessage(localizer["BusinessAdminTextOverridesJsonInvalid"]);

            RuleFor(x => x.WebsiteUrl).MaximumLength(500);
            RuleFor(x => x.BrandDisplayName).MaximumLength(200);
            RuleFor(x => x.BrandLogoUrl).MaximumLength(500);
            RuleFor(x => x.BrandPrimaryColorHex).MaximumLength(16);
            RuleFor(x => x.BrandSecondaryColorHex).MaximumLength(16);
            RuleFor(x => x.SupportEmail).MaximumLength(200).EmailAddress().When(x => x.SupportEmail != null);
            RuleFor(x => x.CommunicationSenderName).MaximumLength(200);
            RuleFor(x => x.CommunicationReplyToEmail).MaximumLength(200).EmailAddress().When(x => x.CommunicationReplyToEmail != null);
            RuleFor(x => x.ContactEmail).MaximumLength(200).EmailAddress().When(x => x.ContactEmail != null);
            RuleFor(x => x.ContactPhoneE164).MaximumLength(30);
            RuleFor(x => x.LegalName).MaximumLength(300);
            RuleFor(x => x.TaxId).MaximumLength(100);
            RuleFor(x => x.ShortDescription).MaximumLength(1000);
        }
    }

    /// <summary>
    /// Validator for editing businesses (includes RowVersion rules).
    /// </summary>
    public sealed class BusinessEditDtoValidator : AbstractValidator<BusinessEditDto>
    {
        public BusinessEditDtoValidator(IStringLocalizer<ValidationResource> localizer)
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.OperationalStatus).IsInEnum();
            RuleFor(x => x.DefaultCurrency).NotEmpty().MaximumLength(3);
            RuleFor(x => x.DefaultCulture).NotEmpty().MaximumLength(20);
            RuleFor(x => x.DefaultTimeZoneId).NotEmpty().MaximumLength(64);
            RuleFor(x => x.AdminTextOverridesJson).MaximumLength(16000);
            RuleFor(x => x.AdminTextOverridesJson)
                .Must(BusinessValidatorJsonHelpers.BeAdminTextOverridesJson)
                .When(x => !string.IsNullOrWhiteSpace(x.AdminTextOverridesJson))
                .WithMessage(localizer["BusinessAdminTextOverridesJsonInvalid"]);

            RuleFor(x => x.WebsiteUrl).MaximumLength(500);
            RuleFor(x => x.BrandDisplayName).MaximumLength(200);
            RuleFor(x => x.BrandLogoUrl).MaximumLength(500);
            RuleFor(x => x.BrandPrimaryColorHex).MaximumLength(16);
            RuleFor(x => x.BrandSecondaryColorHex).MaximumLength(16);
            RuleFor(x => x.SupportEmail).MaximumLength(200).EmailAddress().When(x => x.SupportEmail != null);
            RuleFor(x => x.CommunicationSenderName).MaximumLength(200);
            RuleFor(x => x.CommunicationReplyToEmail).MaximumLength(200).EmailAddress().When(x => x.CommunicationReplyToEmail != null);
            RuleFor(x => x.ContactEmail).MaximumLength(200).EmailAddress().When(x => x.ContactEmail != null);
            RuleFor(x => x.ContactPhoneE164).MaximumLength(30);
            RuleFor(x => x.LegalName).MaximumLength(300);
            RuleFor(x => x.TaxId).MaximumLength(100);
            RuleFor(x => x.ShortDescription).MaximumLength(1000);
            RuleFor(x => x.RowVersion).NotEmpty();
        }
    }

    /// <summary>
    /// Validator for soft deleting businesses.
    /// </summary>
    public sealed class BusinessDeleteDtoValidator : AbstractValidator<BusinessDeleteDto>
    {
        public BusinessDeleteDtoValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotEmpty();
        }
    }

    /// <summary>
    /// Validator for business lifecycle actions such as approve, suspend, and reactivate.
    /// </summary>
    public sealed class BusinessLifecycleActionDtoValidator : AbstractValidator<BusinessLifecycleActionDto>
    {
        public BusinessLifecycleActionDtoValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotEmpty();
            RuleFor(x => x.Note).MaximumLength(500);
        }
    }

    internal static class BusinessValidatorJsonHelpers
    {
        public static bool BeAdminTextOverridesJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return true;
            }

            try
            {
                return AdminTextOverrideJsonCatalog.IsValid(json);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
