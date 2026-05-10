using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Storage;
using Darwin.Application.Settings.Commands;
using Darwin.Application.Settings.DTOs;
using Darwin.Infrastructure.Storage;
using Darwin.WebAdmin.Security;
using Darwin.WebAdmin.ViewModels.Settings;
using Darwin.WebAdmin.Services.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Darwin.WebAdmin.Controllers.Admin.Settings
{
    /// <summary>
    /// Admin controller for viewing and editing site-wide settings. Reads via cache,
    /// saves via Application handlers, and invalidates cache on success.
    /// </summary>
    [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
    public sealed class SiteSettingsController : AdminBaseController
    {
        private const string SecretPlaceholder = "********";
        private const string ActiveObjectStorageProfile = "__active";
        private readonly UpdateSiteSettingHandler _update;
        private readonly ISiteSettingCache _cache;
        private readonly IBusinessEffectiveSettingsCache _businessEffectiveSettingsCache;
        private readonly IObjectStorageService _objectStorage;
        private readonly ObjectStorageOptions _objectStorageOptions;

        /// <summary>
        /// Initializes a new instance of <see cref="SiteSettingsController"/>.
        /// </summary>
        public SiteSettingsController(
            UpdateSiteSettingHandler update,
            ISiteSettingCache cache,
            IBusinessEffectiveSettingsCache businessEffectiveSettingsCache,
            IObjectStorageService objectStorage,
            IOptions<ObjectStorageOptions> objectStorageOptions)
        {
            _update = update ?? throw new ArgumentNullException(nameof(update));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _businessEffectiveSettingsCache = businessEffectiveSettingsCache ?? throw new ArgumentNullException(nameof(businessEffectiveSettingsCache));
            _objectStorage = objectStorage ?? throw new ArgumentNullException(nameof(objectStorage));
            _objectStorageOptions = objectStorageOptions?.Value ?? throw new ArgumentNullException(nameof(objectStorageOptions));
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
            {
                MaskSecretsForRedisplay(vm);
                return RenderEditor(vm, fragment);
            }

            var current = await _cache.GetAsync(ct).ConfigureAwait(false);
            var dto = MapToUpdateDto(vm, current);

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
                MaskSecretsForRedisplay(vm);
                return RenderEditor(vm, fragment);
            }
            catch (FluentValidation.ValidationException ex)
            {
                foreach (var e in ex.Errors)
                    ModelState.AddModelError(e.PropertyName, e.ErrorMessage);
                MaskSecretsForRedisplay(vm);
                return RenderEditor(vm, fragment);
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> SmokeObjectStorage(string? profileName, string? fragment, CancellationToken ct)
        {
            string? normalizedProfile = null;
            var profileLabel = T("ObjectStorageActiveProfile");

            try
            {
                normalizedProfile = NormalizeSmokeProfile(profileName);
                profileLabel = string.IsNullOrWhiteSpace(normalizedProfile)
                    ? T("ObjectStorageActiveProfile")
                    : normalizedProfile;

                var nowUtc = DateTime.UtcNow;
                var payload = Encoding.UTF8.GetBytes($"Darwin object storage smoke {nowUtc:O} {Guid.NewGuid():N}");
                var expectedHash = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
                var objectKey = ObjectStorageKeyBuilder.Build(
                    "smoke",
                    nowUtc.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture),
                    Guid.NewGuid().ToString("N") + ".txt");

                await using var content = new MemoryStream(payload, writable: false);
                var write = await _objectStorage.SaveAsync(
                    new ObjectStorageWriteRequest(
                        ContainerName: ResolveSmokeContainerName(normalizedProfile),
                        ObjectKey: objectKey,
                        ContentType: "text/plain",
                        FileName: "darwin-object-storage-smoke.txt",
                        Content: content,
                        ContentLength: payload.LongLength,
                        ExpectedSha256Hash: expectedHash,
                        Metadata: new Dictionary<string, string>
                        {
                            ["purpose"] = "storage-smoke",
                            ["created-by"] = "webadmin"
                        },
                        OverwritePolicy: ObjectOverwritePolicy.Disallow,
                        ProfileName: normalizedProfile),
                    ct).ConfigureAwait(false);

                var reference = new ObjectStorageObjectReference(
                    write.ContainerName,
                    write.ObjectKey,
                    write.VersionId,
                    ProfileName: normalizedProfile);

                var read = await _objectStorage.ReadAsync(reference, ct).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("Object storage smoke read returned no object.");

                await using (read.Content.ConfigureAwait(false))
                await using (var buffer = new MemoryStream())
                {
                    await read.Content.CopyToAsync(buffer, ct).ConfigureAwait(false);
                    var actualHash = Convert.ToHexString(SHA256.HashData(buffer.ToArray())).ToLowerInvariant();
                    if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Object storage smoke read hash did not match the written payload.");
                    }
                }

                var metadata = await _objectStorage.GetMetadataAsync(reference, ct).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("Object storage smoke metadata lookup returned no object.");

                if (!string.IsNullOrWhiteSpace(metadata.Sha256Hash) &&
                    !string.Equals(metadata.Sha256Hash, expectedHash, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Object storage smoke metadata hash did not match the written payload.");
                }

                try
                {
                    await _objectStorage.DeleteAsync(
                        new ObjectStorageDeleteRequest(reference, "WebAdmin object storage smoke cleanup"),
                        ct).ConfigureAwait(false);
                    TempData["Success"] = string.Format(
                        System.Globalization.CultureInfo.CurrentCulture,
                        T("ObjectStorageSmokeSucceeded"),
                        profileLabel);
                }
                catch (InvalidOperationException)
                {
                    TempData["Warning"] = string.Format(
                        System.Globalization.CultureInfo.CurrentCulture,
                        T("ObjectStorageSmokeSucceededCleanupBlocked"),
                        profileLabel);
                }
            }
            catch (Exception)
            {
                TempData["Error"] = string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    T("ObjectStorageSmokeFailed"),
                    profileLabel);
            }

            return RedirectOrHtmx(nameof(Edit), string.IsNullOrWhiteSpace(fragment) ? "site-settings-tax" : fragment.Trim());
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
            MultilingualEnabled = CountCultures(dto.SupportedCulturesCsv) > 1,
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
            JwtSigningKey = ToRequiredSecretPlaceholder(dto.JwtSigningKey),
            JwtPreviousSigningKey = ToSecretPlaceholder(dto.JwtPreviousSigningKey),
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
            StripeSecretKey = ToSecretPlaceholder(dto.StripeSecretKey),
            StripeWebhookSecret = ToSecretPlaceholder(dto.StripeWebhookSecret),
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
            InvoiceArchiveRetentionYears = dto.InvoiceArchiveRetentionYears,
            DhlEnabled = dto.DhlEnabled,
            DhlEnvironment = dto.DhlEnvironment,
            DhlApiBaseUrl = dto.DhlApiBaseUrl,
            DhlApiKey = ToSecretPlaceholder(dto.DhlApiKey),
            DhlApiSecret = ToSecretPlaceholder(dto.DhlApiSecret),
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
            WhatsAppAccessToken = ToSecretPlaceholder(dto.WhatsAppAccessToken),
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
            SmtpPassword = ToSecretPlaceholder(dto.SmtpPassword),
            SmtpFromAddress = dto.SmtpFromAddress,
            SmtpFromDisplayName = dto.SmtpFromDisplayName,

            // SMS
            SmsEnabled = dto.SmsEnabled,
            SmsProvider = dto.SmsProvider,
            SmsFromPhoneE164 = dto.SmsFromPhoneE164,
            SmsApiKey = ToSecretPlaceholder(dto.SmsApiKey),
            SmsApiSecret = ToSecretPlaceholder(dto.SmsApiSecret),
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
        private static SiteSettingDto MapToUpdateDto(SiteSettingVm vm, SiteSettingDto current) => new()
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
            SupportedCulturesCsv = ResolveSupportedCultures(vm),
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
            JwtSigningKey = ResolveRequiredSecret(vm.JwtSigningKey, current.JwtSigningKey),
            JwtPreviousSigningKey = ResolveSecret(vm.JwtPreviousSigningKey, current.JwtPreviousSigningKey),
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
            StripeSecretKey = ResolveSecret(vm.StripeSecretKey, current.StripeSecretKey),
            StripeWebhookSecret = ResolveSecret(vm.StripeWebhookSecret, current.StripeWebhookSecret),
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
            InvoiceArchiveRetentionYears = vm.InvoiceArchiveRetentionYears,
            DhlEnabled = vm.DhlEnabled,
            DhlEnvironment = vm.DhlEnvironment,
            DhlApiBaseUrl = vm.DhlApiBaseUrl,
            DhlApiKey = ResolveSecret(vm.DhlApiKey, current.DhlApiKey),
            DhlApiSecret = ResolveSecret(vm.DhlApiSecret, current.DhlApiSecret),
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
            WhatsAppAccessToken = ResolveSecret(vm.WhatsAppAccessToken, current.WhatsAppAccessToken),
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
            SmtpPassword = ResolveSecret(vm.SmtpPassword, current.SmtpPassword),
            SmtpFromAddress = vm.SmtpFromAddress,
            SmtpFromDisplayName = vm.SmtpFromDisplayName,

            // SMS
            SmsEnabled = vm.SmsEnabled,
            SmsProvider = vm.SmsProvider,
            SmsFromPhoneE164 = vm.SmsFromPhoneE164,
            SmsApiKey = ResolveSecret(vm.SmsApiKey, current.SmsApiKey),
            SmsApiSecret = ResolveSecret(vm.SmsApiSecret, current.SmsApiSecret),
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

        private static string? ToSecretPlaceholder(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : SecretPlaceholder;

        private static string ToRequiredSecretPlaceholder(string value)
            => string.IsNullOrWhiteSpace(value) ? string.Empty : SecretPlaceholder;

        private static string ResolveRequiredSecret(string postedValue, string currentValue)
            => string.IsNullOrWhiteSpace(postedValue) || IsSecretPlaceholder(postedValue)
                ? currentValue
                : postedValue.Trim();

        private static string? ResolveSecret(string? postedValue, string? currentValue)
            => string.IsNullOrWhiteSpace(postedValue) || IsSecretPlaceholder(postedValue)
                ? currentValue
                : postedValue.Trim();

        private static bool IsSecretPlaceholder(string value)
            => string.Equals(value.Trim(), SecretPlaceholder, StringComparison.Ordinal);

        private static string? NormalizeSmokeProfile(string? profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName) ||
                string.Equals(profileName.Trim(), ActiveObjectStorageProfile, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return profileName.Trim() switch
            {
                "InvoiceArchive" => "InvoiceArchive",
                "MediaAssets" => "MediaAssets",
                "ShipmentLabels" => "ShipmentLabels",
                _ => throw new InvalidOperationException("Unsupported object storage smoke profile.")
            };
        }

        private string ResolveSmokeContainerName(string? normalizedProfile)
        {
            if (!string.IsNullOrWhiteSpace(normalizedProfile))
            {
                return string.Empty;
            }

            var effectiveProvider = !string.IsNullOrWhiteSpace(_objectStorageOptions.ActiveProfile) &&
                _objectStorageOptions.Profiles.TryGetValue(_objectStorageOptions.ActiveProfile.Trim(), out var activeProfile)
                    ? activeProfile.Provider
                    : _objectStorageOptions.Provider;

            return effectiveProvider == ObjectStorageProviderKind.FileSystem ? "darwin-smoke" : string.Empty;
        }

        private void MaskSecretsForRedisplay(SiteSettingVm vm)
        {
            MaskRequiredSecret(nameof(SiteSettingVm.JwtSigningKey), value => vm.JwtSigningKey = value, vm.JwtSigningKey);
            MaskSecret(nameof(SiteSettingVm.JwtPreviousSigningKey), value => vm.JwtPreviousSigningKey = value, vm.JwtPreviousSigningKey);
            MaskSecret(nameof(SiteSettingVm.StripeSecretKey), value => vm.StripeSecretKey = value, vm.StripeSecretKey);
            MaskSecret(nameof(SiteSettingVm.StripeWebhookSecret), value => vm.StripeWebhookSecret = value, vm.StripeWebhookSecret);
            MaskSecret(nameof(SiteSettingVm.DhlApiKey), value => vm.DhlApiKey = value, vm.DhlApiKey);
            MaskSecret(nameof(SiteSettingVm.DhlApiSecret), value => vm.DhlApiSecret = value, vm.DhlApiSecret);
            MaskSecret(nameof(SiteSettingVm.WhatsAppAccessToken), value => vm.WhatsAppAccessToken = value, vm.WhatsAppAccessToken);
            MaskSecret(nameof(SiteSettingVm.SmtpPassword), value => vm.SmtpPassword = value, vm.SmtpPassword);
            MaskSecret(nameof(SiteSettingVm.SmsApiKey), value => vm.SmsApiKey = value, vm.SmsApiKey);
            MaskSecret(nameof(SiteSettingVm.SmsApiSecret), value => vm.SmsApiSecret = value, vm.SmsApiSecret);
        }

        private void MaskRequiredSecret(string key, Action<string> setValue, string postedValue)
        {
            ModelState.Remove(key);
            setValue(string.IsNullOrWhiteSpace(postedValue) ? string.Empty : SecretPlaceholder);
        }

        private void MaskSecret(string key, Action<string?> setValue, string? postedValue)
        {
            ModelState.Remove(key);
            setValue(string.IsNullOrWhiteSpace(postedValue) ? null : SecretPlaceholder);
        }

        private static int CountCultures(string? supportedCulturesCsv)
            => (supportedCulturesCsv ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

        private static string ResolveSupportedCultures(SiteSettingVm vm)
        {
            var defaultCulture = string.IsNullOrWhiteSpace(vm.DefaultCulture)
                ? Darwin.WebAdmin.Localization.AdminCultureCatalog.DefaultCulture
                : vm.DefaultCulture.Trim();

            if (!vm.MultilingualEnabled)
            {
                return defaultCulture;
            }

            var cultures = (vm.SupportedCulturesCsv ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Prepend(defaultCulture)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return cultures.Length == 0 ? defaultCulture : string.Join(",", cultures);
        }
    }
}
