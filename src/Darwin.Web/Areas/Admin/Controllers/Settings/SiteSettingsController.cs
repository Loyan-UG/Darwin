using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Settings.Commands;
using Darwin.Application.Settings.DTOs;
using Darwin.Web.Areas.Admin.ViewModels.Settings;
using Darwin.Web.Services.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Web.Areas.Admin.Controllers.Settings
{
    /// <summary>
    /// Admin controller for viewing and editing site-wide settings. Reads via cache,
    /// saves via Application handlers, and invalidates cache on success.
    /// </summary>
    [Area("Admin")]
    public sealed class SiteSettingsController : Controller
    {
        private readonly UpdateSiteSettingHandler _update;
        private readonly ISiteSettingCache _cache;

        /// <summary>
        /// Initializes a new instance of <see cref="SiteSettingsController"/>.
        /// </summary>
        public SiteSettingsController(UpdateSiteSettingHandler update, ISiteSettingCache cache)
        {
            _update = update;
            _cache = cache;
        }

        /// <summary>
        /// Shows the edit form with current settings (loaded from cache).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(CancellationToken ct)
        {
            var dto = await _cache.GetAsync(ct);
            var vm = MapToVm(dto);
            return View(vm);
        }

        /// <summary>
        /// Processes posted changes; on success redirects back to GET with a success alert.
        /// Handles concurrency and validation errors and redisplays the form.
        /// </summary>
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Edit(SiteSettingVm vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var dto = MapToUpdateDto(vm);

            try
            {
                await _update.HandleAsync(dto, ct);
                _cache.Invalidate();
                TempData["Success"] = "Settings have been updated.";
                return RedirectToAction(nameof(Edit));
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty, "Concurrency conflict: the settings were modified by another user.");
                return View(vm);
            }
            catch (FluentValidation.ValidationException ex)
            {
                foreach (var e in ex.Errors)
                    ModelState.AddModelError(e.PropertyName, e.ErrorMessage);
                return View(vm);
            }
        }

        /// <summary>
        /// Maps DTO → VM for the form.
        /// </summary>
        private static SiteSettingVm MapToVm(SiteSettingDto dto) => new()
        {
            Id = dto.Id,
            RowVersion = dto.RowVersion,

            // Basics
            Title = dto.Title,
            LogoUrl = dto.LogoUrl,
            ContactEmail = dto.ContactEmail,

            // Routing
            HomeSlug = dto.HomeSlug,

            // Localization
            DefaultCulture = dto.DefaultCulture,
            SupportedCulturesCsv = dto.SupportedCulturesCsv,
            DefaultCountry = dto.DefaultCountry,
            DefaultCurrency = dto.DefaultCurrency,
            TimeZone = dto.TimeZone,
            DateFormat = dto.DateFormat,
            TimeFormat = dto.TimeFormat,

            // Units & formatting
            MeasurementSystem = dto.MeasurementSystem,
            DisplayWeightUnit = dto.DisplayWeightUnit,
            DisplayLengthUnit = dto.DisplayLengthUnit,
            MeasurementSettingsJson = dto.MeasurementSettingsJson,
            NumberFormattingOverridesJson = dto.NumberFormattingOverridesJson,

            // SEO
            EnableCanonical = dto.EnableCanonical,
            HreflangEnabled = dto.HreflangEnabled,
            SeoTitleTemplate = dto.SeoTitleTemplate,
            SeoMetaDescriptionTemplate = dto.SeoMetaDescriptionTemplate,
            OpenGraphDefaultsJson = dto.OpenGraphDefaultsJson,

            // Analytics
            GoogleAnalyticsId = dto.GoogleAnalyticsId,
            GoogleTagManagerId = dto.GoogleTagManagerId,
            GoogleSearchConsoleVerification = dto.GoogleSearchConsoleVerification,

            // Feature flags
            FeatureFlagsJson = dto.FeatureFlagsJson,

            // WhatsApp
            WhatsAppEnabled = dto.WhatsAppEnabled,
            WhatsAppBusinessPhoneId = dto.WhatsAppBusinessPhoneId,
            WhatsAppAccessToken = dto.WhatsAppAccessToken,
            WhatsAppFromPhoneE164 = dto.WhatsAppFromPhoneE164,
            WhatsAppAdminRecipientsCsv = dto.WhatsAppAdminRecipientsCsv,

            // WebAuthn
            WebAuthnRelyingPartyId = dto.WebAuthnRelyingPartyId,
            WebAuthnRelyingPartyName = dto.WebAuthnRelyingPartyName,
            WebAuthnAllowedOriginsCsv = dto.WebAuthnAllowedOriginsCsv,
            WebAuthnRequireUserVerification = dto.WebAuthnRequireUserVerification,

            // SMTP
            SmtpEnabled = dto.SmtpEnabled,
            SmtpHost = dto.SmtpHost,
            SmtpPort = dto.SmtpPort,
            SmtpEnableSsl = dto.SmtpEnableSsl,
            SmtpUsername = dto.SmtpUsername,
            SmtpPassword = dto.SmtpPassword,
            SmtpFromAddress = dto.SmtpFromAddress,
            SmtpFromDisplayName = dto.SmtpFromDisplayName,

            // SMS
            SmsEnabled = dto.SmsEnabled,
            SmsProvider = dto.SmsProvider,
            SmsFromPhoneE164 = dto.SmsFromPhoneE164,
            SmsApiKey = dto.SmsApiKey,
            SmsApiSecret = dto.SmsApiSecret,
            SmsExtraSettingsJson = dto.SmsExtraSettingsJson,

            // Admin routing
            AdminAlertEmailsCsv = dto.AdminAlertEmailsCsv,
            AdminAlertSmsRecipientsCsv = dto.AdminAlertSmsRecipientsCsv
        };

        /// <summary>
        /// Maps VM → DTO for persistence.
        /// </summary>
        private static SiteSettingDto MapToUpdateDto(SiteSettingVm vm) => new()
        {
            Id = vm.Id,
            RowVersion = vm.RowVersion,

            // Basics
            Title = vm.Title,
            LogoUrl = vm.LogoUrl,
            ContactEmail = vm.ContactEmail,

            // Routing
            HomeSlug = vm.HomeSlug,

            // Localization
            DefaultCulture = vm.DefaultCulture,
            SupportedCulturesCsv = vm.SupportedCulturesCsv,
            DefaultCountry = vm.DefaultCountry,
            DefaultCurrency = vm.DefaultCurrency,
            TimeZone = vm.TimeZone,
            DateFormat = vm.DateFormat,
            TimeFormat = vm.TimeFormat,

            // Units & formatting
            MeasurementSystem = vm.MeasurementSystem,
            DisplayWeightUnit = vm.DisplayWeightUnit,
            DisplayLengthUnit = vm.DisplayLengthUnit,
            MeasurementSettingsJson = vm.MeasurementSettingsJson,
            NumberFormattingOverridesJson = vm.NumberFormattingOverridesJson,

            // SEO
            EnableCanonical = vm.EnableCanonical,
            HreflangEnabled = vm.HreflangEnabled,
            SeoTitleTemplate = vm.SeoTitleTemplate,
            SeoMetaDescriptionTemplate = vm.SeoMetaDescriptionTemplate,
            OpenGraphDefaultsJson = vm.OpenGraphDefaultsJson,

            // Analytics
            GoogleAnalyticsId = vm.GoogleAnalyticsId,
            GoogleTagManagerId = vm.GoogleTagManagerId,
            GoogleSearchConsoleVerification = vm.GoogleSearchConsoleVerification,

            // Feature flags
            FeatureFlagsJson = vm.FeatureFlagsJson,

            // WhatsApp
            WhatsAppEnabled = vm.WhatsAppEnabled,
            WhatsAppBusinessPhoneId = vm.WhatsAppBusinessPhoneId,
            WhatsAppAccessToken = vm.WhatsAppAccessToken,
            WhatsAppFromPhoneE164 = vm.WhatsAppFromPhoneE164,
            WhatsAppAdminRecipientsCsv = vm.WhatsAppAdminRecipientsCsv,

            // WebAuthn
            WebAuthnRelyingPartyId = vm.WebAuthnRelyingPartyId,
            WebAuthnRelyingPartyName = vm.WebAuthnRelyingPartyName,
            WebAuthnAllowedOriginsCsv = vm.WebAuthnAllowedOriginsCsv,
            WebAuthnRequireUserVerification = vm.WebAuthnRequireUserVerification,

            // SMTP
            SmtpEnabled = vm.SmtpEnabled,
            SmtpHost = vm.SmtpHost,
            SmtpPort = vm.SmtpPort,
            SmtpEnableSsl = vm.SmtpEnableSsl,
            SmtpUsername = vm.SmtpUsername,
            SmtpPassword = vm.SmtpPassword,
            SmtpFromAddress = vm.SmtpFromAddress,
            SmtpFromDisplayName = vm.SmtpFromDisplayName,

            // SMS
            SmsEnabled = vm.SmsEnabled,
            SmsProvider = vm.SmsProvider,
            SmsFromPhoneE164 = vm.SmsFromPhoneE164,
            SmsApiKey = vm.SmsApiKey,
            SmsApiSecret = vm.SmsApiSecret,
            SmsExtraSettingsJson = vm.SmsExtraSettingsJson,

            // Admin routing
            AdminAlertEmailsCsv = vm.AdminAlertEmailsCsv,
            AdminAlertSmsRecipientsCsv = vm.AdminAlertSmsRecipientsCsv
        };
    }
}
