using System;
using System.Globalization;
using System.Linq;
using Darwin.Application.Settings.DTOs;
using FluentValidation;

namespace Darwin.Application.Settings.Validators
{
    /// <summary>
    /// FluentValidation rules for editing the singleton <see cref="SiteSettingDto"/>.
    /// This validator is used both in Admin and in API-level configuration updates.
    /// </summary>
    public sealed class SiteSettingEditValidator : AbstractValidator<SiteSettingDto>
    {
        public SiteSettingEditValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RowVersion).NotNull();

            // -------- Basic site information --------
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(200);

            RuleFor(x => x.LogoUrl)
                .MaximumLength(500)
                .When(x => !string.IsNullOrWhiteSpace(x.LogoUrl));

            RuleFor(x => x.ContactEmail)
                .EmailAddress()
                .When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));

            // -------- JWT --------
            RuleFor(x => x.JwtIssuer)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(x => x.JwtAudience)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(x => x.JwtAccessTokenMinutes)
                .GreaterThanOrEqualTo(1)
                .LessThanOrEqualTo(1440)
                .WithMessage("JwtAccessTokenMinutes must be between 1 and 1440 minutes.");

            RuleFor(x => x.JwtRefreshTokenDays)
                .GreaterThanOrEqualTo(1)
                .LessThanOrEqualTo(3650)
                .WithMessage("JwtRefreshTokenDays must be between 1 and 3650 days.");

            RuleFor(x => x.JwtSigningKey)
                .NotEmpty()
                .MinimumLength(32)
                .MaximumLength(2048)
                .WithMessage("JwtSigningKey must be a non-empty high-entropy secret.");

            RuleFor(x => x.JwtPreviousSigningKey)
                .MinimumLength(32)
                .MaximumLength(2048)
                .When(x => !string.IsNullOrWhiteSpace(x.JwtPreviousSigningKey));

            RuleFor(x => x.JwtClockSkewSeconds)
                .GreaterThanOrEqualTo(0)
                .LessThanOrEqualTo(3600)
                .WithMessage("JwtClockSkewSeconds must be between 0 and 3600 seconds.");


            // -------- Mobile bootstrap --------
            RuleFor(x => x.MobileQrTokenRefreshSeconds)
                .GreaterThan(0)
                .LessThanOrEqualTo(3600)
                .WithMessage("MobileQrTokenRefreshSeconds must be between 1 and 3600 seconds.");

            RuleFor(x => x.MobileMaxOutboxItems)
                .GreaterThan(0)
                .LessThanOrEqualTo(10000)
                .WithMessage("MobileMaxOutboxItems must be between 1 and 10000.");

            RuleFor(x => x.BusinessManagementWebsiteUrl)
                .MaximumLength(500)
                .Must(BeHttpsAbsoluteUrl)
                .When(x => !string.IsNullOrWhiteSpace(x.BusinessManagementWebsiteUrl))
                .WithMessage("BusinessManagementWebsiteUrl must be an absolute HTTPS URL.");

            RuleFor(x => x.ImpressumUrl)
                .MaximumLength(500)
                .Must(BeHttpsAbsoluteUrl)
                .When(x => !string.IsNullOrWhiteSpace(x.ImpressumUrl))
                .WithMessage("ImpressumUrl must be an absolute HTTPS URL.");

            RuleFor(x => x.PrivacyPolicyUrl)
                .MaximumLength(500)
                .Must(BeHttpsAbsoluteUrl)
                .When(x => !string.IsNullOrWhiteSpace(x.PrivacyPolicyUrl))
                .WithMessage("PrivacyPolicyUrl must be an absolute HTTPS URL.");

            RuleFor(x => x.BusinessTermsUrl)
                .MaximumLength(500)
                .Must(BeHttpsAbsoluteUrl)
                .When(x => !string.IsNullOrWhiteSpace(x.BusinessTermsUrl))
                .WithMessage("BusinessTermsUrl must be an absolute HTTPS URL.");

            RuleFor(x => x.AccountDeletionUrl)
                .MaximumLength(500)
                .Must(BeHttpsAbsoluteUrl)
                .When(x => !string.IsNullOrWhiteSpace(x.AccountDeletionUrl))
                .WithMessage("AccountDeletionUrl must be an absolute HTTPS URL.");

            // -------- Phase-1 payment and shipping providers --------
            RuleFor(x => x.StripePublishableKey)
                .MaximumLength(256)
                .When(x => !string.IsNullOrWhiteSpace(x.StripePublishableKey));

            RuleFor(x => x.StripeSecretKey)
                .MaximumLength(256)
                .When(x => !string.IsNullOrWhiteSpace(x.StripeSecretKey));

            RuleFor(x => x.StripeWebhookSecret)
                .MaximumLength(256)
                .When(x => !string.IsNullOrWhiteSpace(x.StripeWebhookSecret));

            RuleFor(x => x.StripeMerchantDisplayName)
                .MaximumLength(200)
                .When(x => !string.IsNullOrWhiteSpace(x.StripeMerchantDisplayName));

            RuleFor(x => x.DefaultVatRatePercent)
                .GreaterThanOrEqualTo(0)
                .LessThanOrEqualTo(100)
                .WithMessage("DefaultVatRatePercent must be between 0 and 100.");

            RuleFor(x => x.InvoiceIssuerLegalName)
                .MaximumLength(200)
                .When(x => !string.IsNullOrWhiteSpace(x.InvoiceIssuerLegalName));

            RuleFor(x => x.InvoiceIssuerTaxId)
                .MaximumLength(128)
                .When(x => !string.IsNullOrWhiteSpace(x.InvoiceIssuerTaxId));

            RuleFor(x => x.InvoiceIssuerAddressLine1)
                .MaximumLength(300)
                .When(x => !string.IsNullOrWhiteSpace(x.InvoiceIssuerAddressLine1));

            RuleFor(x => x.InvoiceIssuerPostalCode)
                .MaximumLength(32)
                .When(x => !string.IsNullOrWhiteSpace(x.InvoiceIssuerPostalCode));

            RuleFor(x => x.InvoiceIssuerCity)
                .MaximumLength(120)
                .When(x => !string.IsNullOrWhiteSpace(x.InvoiceIssuerCity));

            RuleFor(x => x.InvoiceIssuerCountry)
                .Matches("^[A-Z]{2}$")
                .When(x => !string.IsNullOrWhiteSpace(x.InvoiceIssuerCountry))
                .WithMessage("InvoiceIssuerCountry must be a 2-letter uppercase country code.");

            RuleFor(x => x.DhlEnvironment)
                .MaximumLength(50)
                .When(x => !string.IsNullOrWhiteSpace(x.DhlEnvironment));

            RuleFor(x => x.DhlApiBaseUrl)
                .MaximumLength(500)
                .Must(BeHttpsAbsoluteUrl)
                .When(x => !string.IsNullOrWhiteSpace(x.DhlApiBaseUrl))
                .WithMessage("DhlApiBaseUrl must be an absolute HTTPS URL.");

            RuleFor(x => x.DhlApiKey)
                .MaximumLength(256)
                .When(x => !string.IsNullOrWhiteSpace(x.DhlApiKey));

            RuleFor(x => x.DhlApiSecret)
                .MaximumLength(256)
                .When(x => !string.IsNullOrWhiteSpace(x.DhlApiSecret));

            RuleFor(x => x.DhlAccountNumber)
                .MaximumLength(128)
                .When(x => !string.IsNullOrWhiteSpace(x.DhlAccountNumber));

            RuleFor(x => x.DhlShipperName)
                .MaximumLength(200)
                .When(x => !string.IsNullOrWhiteSpace(x.DhlShipperName));

            RuleFor(x => x.DhlShipperEmail)
                .EmailAddress()
                .MaximumLength(256)
                .When(x => !string.IsNullOrWhiteSpace(x.DhlShipperEmail));

            RuleFor(x => x.DhlShipperPhoneE164)
                .MaximumLength(32)
                .When(x => !string.IsNullOrWhiteSpace(x.DhlShipperPhoneE164));

            RuleFor(x => x.DhlShipperStreet)
                .MaximumLength(300)
                .When(x => !string.IsNullOrWhiteSpace(x.DhlShipperStreet));

            RuleFor(x => x.DhlShipperPostalCode)
                .MaximumLength(32)
                .When(x => !string.IsNullOrWhiteSpace(x.DhlShipperPostalCode));

            RuleFor(x => x.DhlShipperCity)
                .MaximumLength(120)
                .When(x => !string.IsNullOrWhiteSpace(x.DhlShipperCity));

            RuleFor(x => x.DhlShipperCountry)
                .Matches("^[A-Z]{2}$")
                .When(x => !string.IsNullOrWhiteSpace(x.DhlShipperCountry))
                .WithMessage("DhlShipperCountry must be a 2-letter uppercase country code.");

            RuleFor(x => x.ShipmentAttentionDelayHours)
                .GreaterThanOrEqualTo(1)
                .LessThanOrEqualTo(720)
                .WithMessage("ShipmentAttentionDelayHours must be between 1 and 720.");

            RuleFor(x => x.ShipmentTrackingGraceHours)
                .GreaterThanOrEqualTo(1)
                .LessThanOrEqualTo(720)
                .WithMessage("ShipmentTrackingGraceHours must be between 1 and 720.");



            // -------- Soft delete / data retention --------
            RuleFor(x => x.SoftDeleteRetentionDays)
                .GreaterThanOrEqualTo(1)
                .LessThanOrEqualTo(3650)
                .WithMessage("SoftDeleteRetentionDays must be between 1 and 3650 days.");

            RuleFor(x => x.SoftDeleteCleanupBatchSize)
                .GreaterThanOrEqualTo(1)
                .LessThanOrEqualTo(100_000)
                .WithMessage("SoftDeleteCleanupBatchSize must be between 1 and 100000.");

            // -------- Localization --------
            RuleFor(x => x.DefaultCulture)
                .NotEmpty().MaximumLength(10)
                .Must(IsCulture)
                .WithMessage("DefaultCulture must be like 'de-DE'.");

            RuleFor(x => x.SupportedCulturesCsv)
                .NotEmpty()
                .Must(AllCulturesValid)
                .WithMessage("SupportedCulturesCsv must be comma-separated cultures like 'de-DE,en-US'.");

            RuleFor(x => x.DefaultCountry)
                .Matches("^[A-Z]{2}$")
                .When(x => !string.IsNullOrWhiteSpace(x.DefaultCountry));

            RuleFor(x => x.DefaultCurrency)
                .NotEmpty().Length(3)
                .Matches("^[A-Z]{3}$");

            RuleFor(x => x.TimeZone)
                .MaximumLength(100)
                .When(x => !string.IsNullOrWhiteSpace(x.TimeZone));

            RuleFor(x => x.DateFormat)
                .MaximumLength(50)
                .When(x => !string.IsNullOrWhiteSpace(x.DateFormat));

            RuleFor(x => x.TimeFormat)
                .MaximumLength(50)
                .When(x => !string.IsNullOrWhiteSpace(x.TimeFormat));

            // -------- Measurement --------
            RuleFor(x => x.MeasurementSystem)
                .Must(v => v == "Metric" || v == "Imperial")
                .WithMessage("MeasurementSystem must be 'Metric' or 'Imperial'.");

            RuleFor(x => x.DisplayWeightUnit)
                .MaximumLength(10)
                .When(x => !string.IsNullOrWhiteSpace(x.DisplayWeightUnit));

            RuleFor(x => x.DisplayLengthUnit)
                .MaximumLength(10)
                .When(x => !string.IsNullOrWhiteSpace(x.DisplayLengthUnit));

            // -------- SEO --------
            RuleFor(x => x.SeoTitleTemplate)
                .MaximumLength(150)
                .When(x => !string.IsNullOrWhiteSpace(x.SeoTitleTemplate));

            RuleFor(x => x.SeoMetaDescriptionTemplate)
                .MaximumLength(200)
                .When(x => !string.IsNullOrWhiteSpace(x.SeoMetaDescriptionTemplate));

            RuleFor(x => x.OpenGraphDefaultsJson)
                .MaximumLength(2000)
                .When(x => !string.IsNullOrWhiteSpace(x.OpenGraphDefaultsJson));

            // -------- Analytics --------
            RuleFor(x => x.GoogleAnalyticsId)
                .MaximumLength(50)
                .When(x => !string.IsNullOrWhiteSpace(x.GoogleAnalyticsId));

            RuleFor(x => x.GoogleTagManagerId)
                .MaximumLength(50)
                .When(x => !string.IsNullOrWhiteSpace(x.GoogleTagManagerId));

            RuleFor(x => x.GoogleSearchConsoleVerification)
                .MaximumLength(200)
                .When(x => !string.IsNullOrWhiteSpace(x.GoogleSearchConsoleVerification));

            // -------- WhatsApp --------
            RuleFor(x => x.WhatsAppBusinessPhoneId)
                .MaximumLength(50)
                .When(x => !string.IsNullOrWhiteSpace(x.WhatsAppBusinessPhoneId));

            RuleFor(x => x.WhatsAppAccessToken)
                .MaximumLength(200)
                .When(x => !string.IsNullOrWhiteSpace(x.WhatsAppAccessToken));

            RuleFor(x => x.WhatsAppFromPhoneE164)
                .MaximumLength(32)
                .When(x => !string.IsNullOrWhiteSpace(x.WhatsAppFromPhoneE164));

            RuleFor(x => x.WhatsAppAdminRecipientsCsv)
                .MaximumLength(1000)
                .When(x => !string.IsNullOrWhiteSpace(x.WhatsAppAdminRecipientsCsv));

            // -------- WebAuthn --------
            RuleFor(x => x.WebAuthnRelyingPartyId)
                .NotEmpty()
                .MaximumLength(255);

            RuleFor(x => x.WebAuthnRelyingPartyName)
                .NotEmpty()
                .MaximumLength(255);

            RuleFor(x => x.WebAuthnAllowedOriginsCsv)
                .NotEmpty()
                .MaximumLength(2000);

            // -------- SMTP --------
            RuleFor(x => x.SmtpHost)
                .MaximumLength(255)
                .When(x => !string.IsNullOrWhiteSpace(x.SmtpHost));

            RuleFor(x => x.SmtpPort)
                .GreaterThan(0)
                .LessThanOrEqualTo(65535)
                .When(x => x.SmtpPort.HasValue);

            RuleFor(x => x.SmtpUsername)
                .MaximumLength(256)
                .When(x => !string.IsNullOrWhiteSpace(x.SmtpUsername));

            RuleFor(x => x.SmtpPassword)
                .MaximumLength(512)
                .When(x => !string.IsNullOrWhiteSpace(x.SmtpPassword));

            RuleFor(x => x.SmtpFromAddress)
                .EmailAddress()
                .MaximumLength(256)
                .When(x => !string.IsNullOrWhiteSpace(x.SmtpFromAddress));

            RuleFor(x => x.SmtpFromDisplayName)
                .MaximumLength(200)
                .When(x => !string.IsNullOrWhiteSpace(x.SmtpFromDisplayName));

            // -------- SMS --------
            RuleFor(x => x.SmsProvider)
                .MaximumLength(100)
                .When(x => !string.IsNullOrWhiteSpace(x.SmsProvider));

            RuleFor(x => x.SmsFromPhoneE164)
                .MaximumLength(32)
                .When(x => !string.IsNullOrWhiteSpace(x.SmsFromPhoneE164));

            RuleFor(x => x.SmsApiKey)
                .MaximumLength(256)
                .When(x => !string.IsNullOrWhiteSpace(x.SmsApiKey));

            RuleFor(x => x.SmsApiSecret)
                .MaximumLength(512)
                .When(x => !string.IsNullOrWhiteSpace(x.SmsApiSecret));

            RuleFor(x => x.SmsExtraSettingsJson)
                .MaximumLength(2000)
                .When(x => !string.IsNullOrWhiteSpace(x.SmsExtraSettingsJson));

            // -------- Admin routing --------
            RuleFor(x => x.AdminAlertEmailsCsv)
                .MaximumLength(2000)
                .When(x => !string.IsNullOrWhiteSpace(x.AdminAlertEmailsCsv));

            RuleFor(x => x.AdminAlertSmsRecipientsCsv)
                .MaximumLength(1000)
                .When(x => !string.IsNullOrWhiteSpace(x.AdminAlertSmsRecipientsCsv));

            RuleFor(x => x.TransactionalEmailSubjectPrefix)
                .MaximumLength(120)
                .When(x => !string.IsNullOrWhiteSpace(x.TransactionalEmailSubjectPrefix));

            RuleFor(x => x.CommunicationTestInboxEmail)
                .EmailAddress()
                .MaximumLength(256)
                .When(x => !string.IsNullOrWhiteSpace(x.CommunicationTestInboxEmail));

            RuleFor(x => x.CommunicationTestSmsRecipientE164)
                .MaximumLength(32)
                .When(x => !string.IsNullOrWhiteSpace(x.CommunicationTestSmsRecipientE164));

            RuleFor(x => x.CommunicationTestWhatsAppRecipientE164)
                .MaximumLength(32)
                .When(x => !string.IsNullOrWhiteSpace(x.CommunicationTestWhatsAppRecipientE164));

            RuleFor(x => x.BusinessInvitationEmailSubjectTemplate)
                .MaximumLength(300)
                .When(x => !string.IsNullOrWhiteSpace(x.BusinessInvitationEmailSubjectTemplate));

            RuleFor(x => x.BusinessInvitationEmailBodyTemplate)
                .MaximumLength(8000)
                .When(x => !string.IsNullOrWhiteSpace(x.BusinessInvitationEmailBodyTemplate));

            RuleFor(x => x.AccountActivationEmailSubjectTemplate)
                .MaximumLength(300)
                .When(x => !string.IsNullOrWhiteSpace(x.AccountActivationEmailSubjectTemplate));

            RuleFor(x => x.AccountActivationEmailBodyTemplate)
                .MaximumLength(8000)
                .When(x => !string.IsNullOrWhiteSpace(x.AccountActivationEmailBodyTemplate));

            RuleFor(x => x.PasswordResetEmailSubjectTemplate)
                .MaximumLength(300)
                .When(x => !string.IsNullOrWhiteSpace(x.PasswordResetEmailSubjectTemplate));

            RuleFor(x => x.PasswordResetEmailBodyTemplate)
                .MaximumLength(8000)
                .When(x => !string.IsNullOrWhiteSpace(x.PasswordResetEmailBodyTemplate));
        }

        private static bool IsCulture(string culture)
        {
            return CultureInfo
                .GetCultures(CultureTypes.AllCultures)
                .Any(c => string.Equals(c.Name, culture, StringComparison.OrdinalIgnoreCase));
        }

        private static bool AllCulturesValid(string csv)
        {
            var parts = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0) return false;
            return parts.All(IsCulture);
        }

        private static bool BeHttpsAbsoluteUrl(string? value)
        {
            return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
                   string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        }
    }
}
