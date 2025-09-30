using Darwin.Application.Settings.DTOs;
using FluentValidation;
using System;
using System.Text.RegularExpressions;

namespace Darwin.Application.Settings.Validators
{
    /// <summary>
    /// FluentValidation validator for <see cref="SiteSettingDto"/>. Enforces
    /// business rules and ensures data integrity before persisting or displaying
    /// site-wide settings. This validator combines the rules for reading and
    /// updating settings; when saving, a concurrency token (<see cref="SiteSettingDto.RowVersion"/>)
    /// must be provided.
    /// </summary>
    public sealed class SiteSettingEditValidator : AbstractValidator<SiteSettingDto>
    {
        /// <summary>
        /// Initializes validation rules. See inline comments for rule descriptions.
        /// </summary>
        public SiteSettingEditValidator()
        {
            // Concurrency token must be present for updates. We require RowVersion
            // always, since the same DTO is used for both display and update.
            RuleFor(x => x.RowVersion)
                .NotNull()
                .Must(x => x.Length > 0)
                .WithMessage("RowVersion is required for concurrency control.");

            // Basic site information
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(200);

            RuleFor(x => x.LogoUrl)
                .MaximumLength(500)
                .When(x => !string.IsNullOrWhiteSpace(x.LogoUrl));

            RuleFor(x => x.ContactEmail)
                .EmailAddress()
                .When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));

            // Localization
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

            // Measurement
            RuleFor(x => x.MeasurementSystem)
                .Must(v => v == "Metric" || v == "Imperial")
                .WithMessage("MeasurementSystem must be 'Metric' or 'Imperial'.");

            RuleFor(x => x.DisplayWeightUnit)
                .MaximumLength(10)
                .When(x => !string.IsNullOrWhiteSpace(x.DisplayWeightUnit));

            RuleFor(x => x.DisplayLengthUnit)
                .MaximumLength(10)
                .When(x => !string.IsNullOrWhiteSpace(x.DisplayLengthUnit));

            // SEO
            RuleFor(x => x.SeoTitleTemplate)
                .MaximumLength(150)
                .When(x => !string.IsNullOrWhiteSpace(x.SeoTitleTemplate));

            RuleFor(x => x.SeoMetaDescriptionTemplate)
                .MaximumLength(200)
                .When(x => !string.IsNullOrWhiteSpace(x.SeoMetaDescriptionTemplate));

            RuleFor(x => x.OpenGraphDefaultsJson)
                .MaximumLength(2000)
                .When(x => !string.IsNullOrWhiteSpace(x.OpenGraphDefaultsJson));

            // Analytics
            RuleFor(x => x.GoogleAnalyticsId)
                .MaximumLength(50)
                .When(x => !string.IsNullOrWhiteSpace(x.GoogleAnalyticsId));

            RuleFor(x => x.GoogleTagManagerId)
                .MaximumLength(50)
                .When(x => !string.IsNullOrWhiteSpace(x.GoogleTagManagerId));

            RuleFor(x => x.GoogleSearchConsoleVerification)
                .MaximumLength(200)
                .When(x => !string.IsNullOrWhiteSpace(x.GoogleSearchConsoleVerification));

            // Feature flags JSON
            RuleFor(x => x.FeatureFlagsJson)
                .MaximumLength(2000)
                .When(x => !string.IsNullOrWhiteSpace(x.FeatureFlagsJson));

            // WhatsApp integration
            RuleFor(x => x.WhatsAppBusinessPhoneId)
                .MaximumLength(50)
                .When(x => !string.IsNullOrWhiteSpace(x.WhatsAppBusinessPhoneId));

            RuleFor(x => x.WhatsAppAccessToken)
                .MaximumLength(200)
                .When(x => !string.IsNullOrWhiteSpace(x.WhatsAppAccessToken));

            RuleFor(x => x.WhatsAppFromPhoneE164)
                .Matches("^\\+\\d{4,15}$")
                .When(x => !string.IsNullOrWhiteSpace(x.WhatsAppFromPhoneE164))
                .WithMessage("WhatsAppFromPhoneE164 must be in E.164 format (e.g., +49123456789)");

            RuleFor(x => x.WhatsAppAdminRecipientsCsv)
                .Must(AllPhonesValid)
                .When(x => !string.IsNullOrWhiteSpace(x.WhatsAppAdminRecipientsCsv))
                .WithMessage("WhatsAppAdminRecipientsCsv must be comma-separated E.164 phone numbers.");

            // Additional formatting overrides
            RuleFor(x => x.MeasurementSettingsJson)
                .MaximumLength(2000)
                .When(x => !string.IsNullOrWhiteSpace(x.MeasurementSettingsJson));

            RuleFor(x => x.NumberFormattingOverridesJson)
                .MaximumLength(2000)
                .When(x => !string.IsNullOrWhiteSpace(x.NumberFormattingOverridesJson));

            // Routing
            RuleFor(x => x.HomeSlug)
                .MaximumLength(200)
                .When(x => !string.IsNullOrWhiteSpace(x.HomeSlug));

            // ------------------------------
            // WebAuthn (new) validations
            // ------------------------------
            RuleFor(x => x.WebAuthnRelyingPartyId)
                .NotEmpty()
                .MaximumLength(255)
                .Must(IsRpId)
                .WithMessage("WebAuthnRelyingPartyId must be a registrable domain (e.g., example.com) or 'localhost' for development.");

            RuleFor(x => x.WebAuthnRelyingPartyName)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.WebAuthnAllowedOriginsCsv)
                .NotEmpty()
                .Must(AllOriginsValid)
                .WithMessage("WebAuthnAllowedOriginsCsv must be a comma-separated list of valid origins (e.g., https://example.com,https://localhost:5001).");

            // WebAuthnRequireUserVerification is boolean; no rule needed beyond presence in DTO.

            // SMTP
            RuleFor(x => x.SmtpHost)
                .MaximumLength(200)
                .When(x => x.SmtpEnabled && !string.IsNullOrWhiteSpace(x.SmtpHost));
            RuleFor(x => x.SmtpPort)
                .InclusiveBetween(1, 65535)
                .When(x => x.SmtpEnabled && x.SmtpPort.HasValue);
            RuleFor(x => x.SmtpFromAddress)
                .EmailAddress()
                .When(x => x.SmtpEnabled && !string.IsNullOrWhiteSpace(x.SmtpFromAddress));
            RuleFor(x => x.SmtpFromDisplayName)
                .MaximumLength(200)
                .When(x => x.SmtpEnabled && !string.IsNullOrWhiteSpace(x.SmtpFromDisplayName));

            // SMS
            RuleFor(x => x.SmsProvider)
                .MaximumLength(50)
                .When(x => x.SmsEnabled && !string.IsNullOrWhiteSpace(x.SmsProvider));
            RuleFor(x => x.SmsFromPhoneE164)
                .Matches("^\\+\\d{4,15}$")
                .When(x => x.SmsEnabled && !string.IsNullOrWhiteSpace(x.SmsFromPhoneE164))
                .WithMessage("SmsFromPhoneE164 must be in E.164 format (e.g., +49123456789).");
            RuleFor(x => x.SmsExtraSettingsJson)
                .MaximumLength(4000)
                .When(x => x.SmsEnabled && !string.IsNullOrWhiteSpace(x.SmsExtraSettingsJson));

            // Admin routing
            RuleFor(x => x.AdminAlertEmailsCsv)
                .MaximumLength(1000)
                .When(x => !string.IsNullOrWhiteSpace(x.AdminAlertEmailsCsv));
            RuleFor(x => x.AdminAlertSmsRecipientsCsv)
                .Must(csv => {
                    if (string.IsNullOrWhiteSpace(csv)) return true;
                    var items = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    return items.Length == 0 || items.All(p => System.Text.RegularExpressions.Regex.IsMatch(p, "^\\+\\d{4,15}$"));
                })
                .WithMessage("AdminAlertSmsRecipientsCsv must be comma-separated E.164 phone numbers.")
                .When(x => !string.IsNullOrWhiteSpace(x.AdminAlertSmsRecipientsCsv));
        }

        private static bool IsCulture(string c)
        {
            return Regex.IsMatch(c, "^[a-z]{2}-[A-Z]{2}$");
        }

        private static bool AllCulturesValid(string csv)
        {
            var items = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (items.Length == 0) return false;
            foreach (var c in items)
            {
                if (!IsCulture(c)) return false;
            }
            return true;
        }

        /// <summary>
        /// Validates that a comma-separated list of phone numbers are all in E.164 format.
        /// Allows optional whitespace around the commas. Each number must start with '+' and
        /// contain 4-15 digits.
        /// </summary>
        private static bool AllPhonesValid(string csv)
        {
            var items = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (items.Length == 0) return false;
            foreach (var phone in items)
            {
                if (!Regex.IsMatch(phone, "^\\+\\d{4,15}$"))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Validates RP ID: either "localhost" or something like "example.com" (no scheme, no path).
        /// </summary>
        private static bool IsRpId(string rpId)
        {
            if (string.Equals(rpId, "localhost", StringComparison.OrdinalIgnoreCase))
                return true;
            // rudimentary domain label check; allows subdomains too
            return Regex.IsMatch(rpId, @"^[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
        }

        /// <summary>
        /// Validates CSV of origins. Each must be an absolute URI with http/https scheme.
        /// </summary>
        private static bool AllOriginsValid(string csv)
        {
            var items = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (items.Length == 0) return false;

            foreach (var origin in items)
            {
                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                    return false;
                if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
                    return false; // allow http for local dev only
            }
            return true;
        }
    }
}