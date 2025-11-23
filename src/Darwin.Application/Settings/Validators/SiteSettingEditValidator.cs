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
    }
}
