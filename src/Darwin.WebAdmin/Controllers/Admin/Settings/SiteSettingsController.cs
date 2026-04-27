using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Settings.Commands;
using Darwin.Application.Settings.DTOs;
using Darwin.WebAdmin.Security;
using Darwin.WebAdmin.ViewModels.Settings;
using Darwin.WebAdmin.Services.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace Darwin.WebAdmin.Controllers.Admin.Settings
{
    /// <summary>
    /// Admin controller for viewing and editing site-wide settings. Reads via cache,
    /// saves via Application handlers, and invalidates cache on success.
    /// </summary>
    [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
    public sealed class SiteSettingsController : AdminBaseController
    {
        private readonly UpdateSiteSettingHandler _update;
        private readonly ISiteSettingCache _cache;
        private readonly IBusinessEffectiveSettingsCache _businessEffectiveSettingsCache;

        /// <summary>
        /// Initializes a new instance of <see cref="SiteSettingsController"/>.
        /// </summary>
        public SiteSettingsController(
            UpdateSiteSettingHandler update,
            ISiteSettingCache cache,
            IBusinessEffectiveSettingsCache businessEffectiveSettingsCache)
        {
            _update = update;
            _cache = cache;
            _businessEffectiveSettingsCache = businessEffectiveSettingsCache;
        }

        /// <summary>
        /// Shows the edit form with current settings (loaded from cache).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(string? fragment, CancellationToken ct)
        {
            var dto = await _cache.GetAsync(ct);
            var vm = MapToVm(dto);
            return RenderEditor(vm, fragment);
        }

        /// <summary>
        /// Processes posted changes; on success redirects back to GET with a success alert.
        /// Handles concurrency and validation errors and redisplays the form.
        /// </summary>
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Edit(SiteSettingVm vm, string? fragment, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return RenderEditor(vm, fragment);

            var dto = MapToUpdateDto(vm);

            try
            {
                await _update.HandleAsync(dto, ct);
                _cache.Invalidate();
                _businessEffectiveSettingsCache.InvalidateAll();
                SetSuccessMessage("SettingsUpdatedMessage");
                return RedirectOrHtmx(nameof(Edit), fragment);
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty, T("SettingsConcurrencyMessage"));
                return RenderEditor(vm, fragment);
            }
            catch (FluentValidation.ValidationException ex)
            {
                foreach (var e in ex.Errors)
                    ModelState.AddModelError(e.PropertyName, e.ErrorMessage);
                return RenderEditor(vm, fragment);
            }
        }

        private IActionResult RenderEditor(SiteSettingVm vm, string? fragment)
        {
            ViewData["ActiveFragment"] = string.IsNullOrWhiteSpace(fragment) ? null : fragment.Trim();

            if (IsHtmxRequest())
            {
                return PartialView("~/Views/SiteSettings/_SiteSettingsEditorShell.cshtml", vm);
            }

            return View("Edit", vm);
        }

        private IActionResult RedirectOrHtmx(string actionName, string? fragment)
        {
            var targetUrl = Url.Action(actionName, new { fragment }) ?? string.Empty;

            if (IsHtmxRequest())
            {
                Response.Headers["HX-Redirect"] = targetUrl;
                return new EmptyResult();
            }

            return RedirectToAction(actionName, new { fragment });
        }

        private bool IsHtmxRequest()
        {
            return string.Equals(Request.Headers["HX-Request"], "true", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Maps DTO ? VM for the form.
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
                AdminTextOverridesJson = dto.AdminTextOverridesJson,

            // Security / JWT
            JwtEnabled = dto.JwtEnabled,
            JwtIssuer = dto.JwtIssuer,
            JwtAudience = dto.JwtAudience,
            JwtAccessTokenMinutes = dto.JwtAccessTokenMinutes,
            JwtRefreshTokenDays = dto.JwtRefreshTokenDays,
            JwtSigningKey = dto.JwtSigningKey,
            JwtPreviousSigningKey = dto.JwtPreviousSigningKey,
            JwtEmitScopes = dto.JwtEmitScopes,
            JwtSingleDeviceOnly = dto.JwtSingleDeviceOnly,
            JwtRequireDeviceBinding = dto.JwtRequireDeviceBinding,
            JwtClockSkewSeconds = dto.JwtClockSkewSeconds,

            // Mobile bootstrap
            MobileQrTokenRefreshSeconds = dto.MobileQrTokenRefreshSeconds,
            MobileMaxOutboxItems = dto.MobileMaxOutboxItems,
            BusinessManagementWebsiteUrl = dto.BusinessManagementWebsiteUrl,
            ImpressumUrl = dto.ImpressumUrl,
            PrivacyPolicyUrl = dto.PrivacyPolicyUrl,
            BusinessTermsUrl = dto.BusinessTermsUrl,
            AccountDeletionUrl = dto.AccountDeletionUrl,
            StripeEnabled = dto.StripeEnabled,
            StripePublishableKey = dto.StripePublishableKey,
            StripeSecretKey = dto.StripeSecretKey,
            StripeWebhookSecret = dto.StripeWebhookSecret,
            StripeMerchantDisplayName = dto.StripeMerchantDisplayName,
            VatEnabled = dto.VatEnabled,
            DefaultVatRatePercent = dto.DefaultVatRatePercent,
            PricesIncludeVat = dto.PricesIncludeVat,
            AllowReverseCharge = dto.AllowReverseCharge,
            InvoiceIssuerLegalName = dto.InvoiceIssuerLegalName,
            InvoiceIssuerTaxId = dto.InvoiceIssuerTaxId,
            InvoiceIssuerAddressLine1 = dto.InvoiceIssuerAddressLine1,
            InvoiceIssuerPostalCode = dto.InvoiceIssuerPostalCode,
            InvoiceIssuerCity = dto.InvoiceIssuerCity,
            InvoiceIssuerCountry = dto.InvoiceIssuerCountry,
            DhlEnabled = dto.DhlEnabled,
            DhlEnvironment = dto.DhlEnvironment,
            DhlApiBaseUrl = dto.DhlApiBaseUrl,
            DhlApiKey = dto.DhlApiKey,
            DhlApiSecret = dto.DhlApiSecret,
            DhlAccountNumber = dto.DhlAccountNumber,
            DhlShipperName = dto.DhlShipperName,
            DhlShipperEmail = dto.DhlShipperEmail,
            DhlShipperPhoneE164 = dto.DhlShipperPhoneE164,
            DhlShipperStreet = dto.DhlShipperStreet,
            DhlShipperPostalCode = dto.DhlShipperPostalCode,
            DhlShipperCity = dto.DhlShipperCity,
            DhlShipperCountry = dto.DhlShipperCountry,
            ShipmentAttentionDelayHours = dto.ShipmentAttentionDelayHours,
            ShipmentTrackingGraceHours = dto.ShipmentTrackingGraceHours,

            // Retention
            SoftDeleteCleanupEnabled = dto.SoftDeleteCleanupEnabled,
            SoftDeleteRetentionDays = dto.SoftDeleteRetentionDays,
            SoftDeleteCleanupBatchSize = dto.SoftDeleteCleanupBatchSize,

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
            AdminAlertSmsRecipientsCsv = dto.AdminAlertSmsRecipientsCsv,
            TransactionalEmailSubjectPrefix = dto.TransactionalEmailSubjectPrefix,
            CommunicationTestInboxEmail = dto.CommunicationTestInboxEmail,
            CommunicationTestSmsRecipientE164 = dto.CommunicationTestSmsRecipientE164,
            CommunicationTestWhatsAppRecipientE164 = dto.CommunicationTestWhatsAppRecipientE164,
            CommunicationTestEmailSubjectTemplate = dto.CommunicationTestEmailSubjectTemplate,
            CommunicationTestEmailBodyTemplate = dto.CommunicationTestEmailBodyTemplate,
            CommunicationTestSmsTemplate = dto.CommunicationTestSmsTemplate,
            CommunicationTestWhatsAppTemplate = dto.CommunicationTestWhatsAppTemplate,
            BusinessInvitationEmailSubjectTemplate = dto.BusinessInvitationEmailSubjectTemplate,
            BusinessInvitationEmailBodyTemplate = dto.BusinessInvitationEmailBodyTemplate,
            AccountActivationEmailSubjectTemplate = dto.AccountActivationEmailSubjectTemplate,
            AccountActivationEmailBodyTemplate = dto.AccountActivationEmailBodyTemplate,
            PasswordResetEmailSubjectTemplate = dto.PasswordResetEmailSubjectTemplate,
            PasswordResetEmailBodyTemplate = dto.PasswordResetEmailBodyTemplate,
            PhoneVerificationSmsTemplate = dto.PhoneVerificationSmsTemplate,
            PhoneVerificationWhatsAppTemplate = dto.PhoneVerificationWhatsAppTemplate,
            PhoneVerificationPreferredChannel = dto.PhoneVerificationPreferredChannel,
            PhoneVerificationAllowFallback = dto.PhoneVerificationAllowFallback
        };

        /// <summary>
        /// Maps VM ? DTO for persistence.
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
                AdminTextOverridesJson = vm.AdminTextOverridesJson,

            // Security / JWT
            JwtEnabled = vm.JwtEnabled,
            JwtIssuer = vm.JwtIssuer,
            JwtAudience = vm.JwtAudience,
            JwtAccessTokenMinutes = vm.JwtAccessTokenMinutes,
            JwtRefreshTokenDays = vm.JwtRefreshTokenDays,
            JwtSigningKey = vm.JwtSigningKey,
            JwtPreviousSigningKey = vm.JwtPreviousSigningKey,
            JwtEmitScopes = vm.JwtEmitScopes,
            JwtSingleDeviceOnly = vm.JwtSingleDeviceOnly,
            JwtRequireDeviceBinding = vm.JwtRequireDeviceBinding,
            JwtClockSkewSeconds = vm.JwtClockSkewSeconds,

            // Mobile bootstrap
            MobileQrTokenRefreshSeconds = vm.MobileQrTokenRefreshSeconds,
            MobileMaxOutboxItems = vm.MobileMaxOutboxItems,
            BusinessManagementWebsiteUrl = vm.BusinessManagementWebsiteUrl,
            ImpressumUrl = vm.ImpressumUrl,
            PrivacyPolicyUrl = vm.PrivacyPolicyUrl,
            BusinessTermsUrl = vm.BusinessTermsUrl,
            AccountDeletionUrl = vm.AccountDeletionUrl,
            StripeEnabled = vm.StripeEnabled,
            StripePublishableKey = vm.StripePublishableKey,
            StripeSecretKey = vm.StripeSecretKey,
            StripeWebhookSecret = vm.StripeWebhookSecret,
            StripeMerchantDisplayName = vm.StripeMerchantDisplayName,
            VatEnabled = vm.VatEnabled,
            DefaultVatRatePercent = vm.DefaultVatRatePercent,
            PricesIncludeVat = vm.PricesIncludeVat,
            AllowReverseCharge = vm.AllowReverseCharge,
            InvoiceIssuerLegalName = vm.InvoiceIssuerLegalName,
            InvoiceIssuerTaxId = vm.InvoiceIssuerTaxId,
            InvoiceIssuerAddressLine1 = vm.InvoiceIssuerAddressLine1,
            InvoiceIssuerPostalCode = vm.InvoiceIssuerPostalCode,
            InvoiceIssuerCity = vm.InvoiceIssuerCity,
            InvoiceIssuerCountry = vm.InvoiceIssuerCountry,
            DhlEnabled = vm.DhlEnabled,
            DhlEnvironment = vm.DhlEnvironment,
            DhlApiBaseUrl = vm.DhlApiBaseUrl,
            DhlApiKey = vm.DhlApiKey,
            DhlApiSecret = vm.DhlApiSecret,
            DhlAccountNumber = vm.DhlAccountNumber,
            DhlShipperName = vm.DhlShipperName,
            DhlShipperEmail = vm.DhlShipperEmail,
            DhlShipperPhoneE164 = vm.DhlShipperPhoneE164,
            DhlShipperStreet = vm.DhlShipperStreet,
            DhlShipperPostalCode = vm.DhlShipperPostalCode,
            DhlShipperCity = vm.DhlShipperCity,
            DhlShipperCountry = vm.DhlShipperCountry,
            ShipmentAttentionDelayHours = vm.ShipmentAttentionDelayHours,
            ShipmentTrackingGraceHours = vm.ShipmentTrackingGraceHours,

            // Retention
            SoftDeleteCleanupEnabled = vm.SoftDeleteCleanupEnabled,
            SoftDeleteRetentionDays = vm.SoftDeleteRetentionDays,
            SoftDeleteCleanupBatchSize = vm.SoftDeleteCleanupBatchSize,

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
            AdminAlertSmsRecipientsCsv = vm.AdminAlertSmsRecipientsCsv,
            TransactionalEmailSubjectPrefix = vm.TransactionalEmailSubjectPrefix,
            CommunicationTestInboxEmail = vm.CommunicationTestInboxEmail,
            CommunicationTestSmsRecipientE164 = vm.CommunicationTestSmsRecipientE164,
            CommunicationTestWhatsAppRecipientE164 = vm.CommunicationTestWhatsAppRecipientE164,
            CommunicationTestEmailSubjectTemplate = vm.CommunicationTestEmailSubjectTemplate,
            CommunicationTestEmailBodyTemplate = vm.CommunicationTestEmailBodyTemplate,
            CommunicationTestSmsTemplate = vm.CommunicationTestSmsTemplate,
            CommunicationTestWhatsAppTemplate = vm.CommunicationTestWhatsAppTemplate,
            BusinessInvitationEmailSubjectTemplate = vm.BusinessInvitationEmailSubjectTemplate,
            BusinessInvitationEmailBodyTemplate = vm.BusinessInvitationEmailBodyTemplate,
            AccountActivationEmailSubjectTemplate = vm.AccountActivationEmailSubjectTemplate,
            AccountActivationEmailBodyTemplate = vm.AccountActivationEmailBodyTemplate,
            PasswordResetEmailSubjectTemplate = vm.PasswordResetEmailSubjectTemplate,
            PasswordResetEmailBodyTemplate = vm.PasswordResetEmailBodyTemplate,
            PhoneVerificationSmsTemplate = vm.PhoneVerificationSmsTemplate,
            PhoneVerificationWhatsAppTemplate = vm.PhoneVerificationWhatsAppTemplate,
            PhoneVerificationPreferredChannel = vm.PhoneVerificationPreferredChannel,
            PhoneVerificationAllowFallback = vm.PhoneVerificationAllowFallback
        };
    }
}
