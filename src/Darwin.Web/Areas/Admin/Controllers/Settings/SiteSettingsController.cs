using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Settings.Commands;
using Darwin.Application.Settings.DTOs;
using Darwin.Application.Settings.Queries;
using Darwin.Web.Areas.Admin.ViewModels.Settings;
using Darwin.Web.Services.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Web.Areas.Admin.Controllers.Settings
{
    /// <summary>
    /// Admin controller for viewing and editing site-wide settings. Includes fields for
    /// culture/currency, measurement units, SEO flags, analytics IDs, feature toggles and more.
    /// Utilizes handlers from the Application layer to load and persist settings and a cache
    /// service to refresh the settings across the application immediately after changes.
    /// </summary>
    [Area("Admin")]
    public sealed class SiteSettingsController : Controller
    {
        private readonly GetSiteSettingHandler _get;
        private readonly UpdateSiteSettingHandler _update;
        private readonly ISiteSettingCache _cache;

        /// <summary>
        /// Constructs a new instance of <see cref="SiteSettingsController"/> with the necessary
        /// handlers and caching service.
        /// </summary>
        public SiteSettingsController(GetSiteSettingHandler get, UpdateSiteSettingHandler update, ISiteSettingCache cache)
        {
            _get = get;
            _update = update;
            _cache = cache;
        }

        /// <summary>
        /// Displays the edit form with current site settings.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        [HttpGet]
        public async Task<IActionResult> Edit(CancellationToken ct)
        {
            var dto = await _get.HandleAsync(ct);
            if (dto == null)
                return NotFound();

            var vm = MapToVm(dto);
            return View(vm);
        }

        /// <summary>
        /// Processes posted changes to site settings. Performs ModelState validation, attempts
        /// to update via the handler, invalidates the settings cache on success, and handles
        /// concurrency or validation errors gracefully.
        /// </summary>
        /// <param name="vm">The view model carrying user input.</param>
        /// <param name="ct">Cancellation token.</param>
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

        // Mapping functions between DTO and ViewModel. Keeping mappings here local to the controller
        // avoids scattering mapping logic and ensures the form and handler remain in sync.
        private static SiteSettingVm MapToVm(SiteSettingDto dto)
        {
            return new SiteSettingVm
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion,

                // Basic
                Title = dto.Title,
                LogoUrl = dto.LogoUrl,
                ContactEmail = dto.ContactEmail,

                // Localization
                DefaultCulture = dto.DefaultCulture,
                SupportedCulturesCsv = dto.SupportedCulturesCsv,
                DefaultCountry = dto.DefaultCountry,
                DefaultCurrency = dto.DefaultCurrency,
                TimeZone = dto.TimeZone,
                DateFormat = dto.DateFormat,
                TimeFormat = dto.TimeFormat,

                // Measurement & units
                MeasurementSystem = dto.MeasurementSystem,
                DisplayWeightUnit = dto.DisplayWeightUnit,
                DisplayLengthUnit = dto.DisplayLengthUnit,

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

                // WhatsApp integration
                WhatsAppEnabled = dto.WhatsAppEnabled,
                WhatsAppBusinessPhoneId = dto.WhatsAppBusinessPhoneId,
                WhatsAppAccessToken = dto.WhatsAppAccessToken,
                WhatsAppFromPhoneE164 = dto.WhatsAppFromPhoneE164,
                WhatsAppAdminRecipientsCsv = dto.WhatsAppAdminRecipientsCsv,

                // Additional measurement & formatting overrides
                MeasurementSettingsJson = dto.MeasurementSettingsJson,
                NumberFormattingOverridesJson = dto.NumberFormattingOverridesJson,

                // Routing
                HomeSlug = dto.HomeSlug
            };
        }

        private static SiteSettingDto MapToUpdateDto(SiteSettingVm vm)
        {
            return new SiteSettingDto
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion,

                // Basic
                Title = vm.Title,
                LogoUrl = vm.LogoUrl,
                ContactEmail = vm.ContactEmail,

                // Localization
                DefaultCulture = vm.DefaultCulture,
                SupportedCulturesCsv = vm.SupportedCulturesCsv,
                DefaultCountry = vm.DefaultCountry,
                DefaultCurrency = vm.DefaultCurrency,
                TimeZone = vm.TimeZone,
                DateFormat = vm.DateFormat,
                TimeFormat = vm.TimeFormat,

                // Measurement & units
                MeasurementSystem = vm.MeasurementSystem,
                DisplayWeightUnit = vm.DisplayWeightUnit,
                DisplayLengthUnit = vm.DisplayLengthUnit,

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

                // WhatsApp integration
                WhatsAppEnabled = vm.WhatsAppEnabled,
                WhatsAppBusinessPhoneId = vm.WhatsAppBusinessPhoneId,
                WhatsAppAccessToken = vm.WhatsAppAccessToken,
                WhatsAppFromPhoneE164 = vm.WhatsAppFromPhoneE164,
                WhatsAppAdminRecipientsCsv = vm.WhatsAppAdminRecipientsCsv,

                // Additional measurement & formatting overrides
                MeasurementSettingsJson = vm.MeasurementSettingsJson,
                NumberFormattingOverridesJson = vm.NumberFormattingOverridesJson,

                // Routing
                HomeSlug = vm.HomeSlug
            };
        }
    }
}