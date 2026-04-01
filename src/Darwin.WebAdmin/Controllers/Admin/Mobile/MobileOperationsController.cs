using Darwin.Application.Businesses.Queries;
using Darwin.Application.Identity.Commands;
using Darwin.Application.Identity.Queries;
using Darwin.Application.Settings.Queries;
using Darwin.Domain.Enums;
using Darwin.WebAdmin.Controllers.Admin;
using Darwin.WebAdmin.ViewModels.Mobile;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;

namespace Darwin.WebAdmin.Controllers.Admin.Mobile
{
    public sealed class MobileOperationsController : AdminBaseController
    {
        private readonly GetSiteSettingHandler _getSiteSettings;
        private readonly GetBusinessSupportSummaryHandler _getBusinessSupportSummary;
        private readonly GetBusinessCommunicationOpsSummaryHandler _getCommunicationSummary;
        private readonly GetMobileDeviceOpsSummaryHandler _getDeviceSummary;
        private readonly GetMobileDevicesPageHandler _getDevicesPage;
        private readonly ClearUserDevicePushTokenHandler _clearDevicePushToken;
        private readonly DeactivateUserDeviceHandler _deactivateDevice;

        public MobileOperationsController(
            GetSiteSettingHandler getSiteSettings,
            GetBusinessSupportSummaryHandler getBusinessSupportSummary,
            GetBusinessCommunicationOpsSummaryHandler getCommunicationSummary,
            GetMobileDeviceOpsSummaryHandler getDeviceSummary,
            GetMobileDevicesPageHandler getDevicesPage,
            ClearUserDevicePushTokenHandler clearDevicePushToken,
            DeactivateUserDeviceHandler deactivateDevice)
        {
            _getSiteSettings = getSiteSettings;
            _getBusinessSupportSummary = getBusinessSupportSummary;
            _getCommunicationSummary = getCommunicationSummary;
            _getDeviceSummary = getDeviceSummary;
            _getDevicesPage = getDevicesPage;
            _clearDevicePushToken = clearDevicePushToken;
            _deactivateDevice = deactivateDevice;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? q = null,
            MobilePlatform? platform = null,
            string? state = null,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default)
        {
            var settings = await _getSiteSettings.HandleAsync(ct).ConfigureAwait(false);
            if (settings is null)
            {
                SetErrorMessage("MobileOperationsSiteSettingsMissing");
                return RedirectOrHtmx("Edit", "SiteSettings", new { fragment = "site-settings-mobile" });
            }

            var support = await _getBusinessSupportSummary.HandleAsync(ct: ct).ConfigureAwait(false);
            var comms = await _getCommunicationSummary.HandleAsync(ct: ct).ConfigureAwait(false);
            var deviceSummary = await _getDeviceSummary.HandleAsync(ct).ConfigureAwait(false);
            var devicesPage = await _getDevicesPage.HandleAsync(page, pageSize, q, platform, state, ct).ConfigureAwait(false);

            var vm = new MobileOperationsVm
            {
                JwtEnabled = settings.JwtEnabled,
                JwtSingleDeviceOnly = settings.JwtSingleDeviceOnly,
                JwtRequireDeviceBinding = settings.JwtRequireDeviceBinding,
                JwtAccessTokenMinutes = settings.JwtAccessTokenMinutes,
                JwtRefreshTokenDays = settings.JwtRefreshTokenDays,
                MobileQrTokenRefreshSeconds = settings.MobileQrTokenRefreshSeconds,
                MobileMaxOutboxItems = settings.MobileMaxOutboxItems,
                BusinessManagementWebsiteConfigured = !string.IsNullOrWhiteSpace(settings.BusinessManagementWebsiteUrl),
                ImpressumConfigured = !string.IsNullOrWhiteSpace(settings.ImpressumUrl),
                PrivacyPolicyConfigured = !string.IsNullOrWhiteSpace(settings.PrivacyPolicyUrl),
                BusinessTermsConfigured = !string.IsNullOrWhiteSpace(settings.BusinessTermsUrl),
                AccountDeletionConfigured = !string.IsNullOrWhiteSpace(settings.AccountDeletionUrl),
                DefaultCulture = settings.DefaultCulture,
                TimeZone = settings.TimeZone,
                AttentionBusinessCount = support.AttentionBusinessCount,
                PendingApprovalBusinessCount = support.PendingApprovalBusinessCount,
                OpenInvitationCount = support.OpenInvitationCount,
                PendingActivationMemberCount = support.PendingActivationMemberCount,
                LockedMemberCount = support.LockedMemberCount,
                BusinessesRequiringEmailSetupCount = comms.BusinessesRequiringEmailSetupCount,
                EmailTransportConfigured = settings.SmtpEnabled && !string.IsNullOrWhiteSpace(settings.SmtpHost),
                SmsTransportConfigured = settings.SmsEnabled && !string.IsNullOrWhiteSpace(settings.SmsProvider),
                WhatsAppTransportConfigured = settings.WhatsAppEnabled && !string.IsNullOrWhiteSpace(settings.WhatsAppBusinessPhoneId),
                AdminAlertRoutingConfigured = !string.IsNullOrWhiteSpace(settings.AdminAlertEmailsCsv) ||
                                              !string.IsNullOrWhiteSpace(settings.AdminAlertSmsRecipientsCsv),
                TotalActiveDevices = deviceSummary.TotalActiveDevices,
                BusinessMemberDevicesCount = deviceSummary.BusinessMemberDevicesCount,
                StaleDevicesCount = deviceSummary.StaleDevicesCount,
                DevicesMissingPushTokenCount = deviceSummary.DevicesMissingPushTokenCount,
                NotificationsDisabledCount = deviceSummary.NotificationsDisabledCount,
                AndroidDevicesCount = deviceSummary.AndroidDevicesCount,
                IosDevicesCount = deviceSummary.IosDevicesCount,
                RecentVersions = deviceSummary.RecentVersions.Select(x => new MobileAppVersionSnapshotVm
                {
                    Platform = x.Platform,
                    AppVersion = x.AppVersion,
                    DeviceCount = x.DeviceCount,
                    LastSeenAtUtc = x.LastSeenAtUtc
                }).ToList(),
                Query = q ?? string.Empty,
                PlatformFilter = platform,
                StateFilter = state ?? string.Empty,
                PlatformItems = BuildPlatformItems(platform),
                StateItems = BuildStateItems(state),
                Devices = devicesPage.Items.Select(x => new MobileDeviceOpsListItemVm
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    UserEmail = x.UserEmail,
                    UserDisplayName = x.UserDisplayName,
                    DeviceId = x.DeviceId,
                    Platform = x.Platform,
                    AppVersion = x.AppVersion,
                    DeviceModel = x.DeviceModel,
                    NotificationsEnabled = x.NotificationsEnabled,
                    HasPushToken = x.HasPushToken,
                    IsActive = x.IsActive,
                    LastSeenAtUtc = x.LastSeenAtUtc,
                    BusinessMembershipCount = x.BusinessMembershipCount,
                    RowVersion = x.RowVersion
                }).ToList(),
                Page = page,
                PageSize = pageSize,
                Total = devicesPage.Total
            };

            return RenderIndex(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearPushToken(Guid id, byte[]? rowVersion, string? q = null, MobilePlatform? platform = null, string? state = null, int page = 1, CancellationToken ct = default)
        {
            var result = await _clearDevicePushToken.HandleAsync(id, rowVersion, ct).ConfigureAwait(false);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Push token cleared." : result.Error;
            return RedirectOrHtmx(nameof(Index), null, new { q, platform, state, page });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateDevice(Guid id, byte[]? rowVersion, string? q = null, MobilePlatform? platform = null, string? state = null, int page = 1, CancellationToken ct = default)
        {
            var result = await _deactivateDevice.HandleAsync(id, rowVersion, ct).ConfigureAwait(false);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Device deactivated." : result.Error;
            return RedirectOrHtmx(nameof(Index), null, new { q, platform, state, page });
        }

        private IActionResult RenderIndex(MobileOperationsVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/MobileOperations/Index.cshtml", vm);
            }

            return View(vm);
        }

        private IActionResult RedirectOrHtmx(string actionName, string? controllerName = null, object? routeValues = null)
        {
            if (IsHtmxRequest())
            {
                Response.Headers["HX-Redirect"] = Url.Action(actionName, controllerName, routeValues) ?? string.Empty;
                return new EmptyResult();
            }

            return RedirectToAction(actionName, controllerName, routeValues);
        }

        private bool IsHtmxRequest()
        {
            return string.Equals(Request.Headers["HX-Request"], "true", StringComparison.OrdinalIgnoreCase);
        }

        private static List<SelectListItem> BuildPlatformItems(MobilePlatform? selected)
        {
            var items = new List<SelectListItem> { new("All platforms", string.Empty, !selected.HasValue) };
            items.AddRange(Enum.GetValues<MobilePlatform>()
                .Where(x => x != MobilePlatform.Unknown)
                .Select(x => new SelectListItem(x.ToString(), x.ToString(), selected == x)));
            return items;
        }

        private static List<SelectListItem> BuildStateItems(string? selected)
        {
            return
            [
                new("All states", string.Empty, string.IsNullOrWhiteSpace(selected)),
                new("Stale devices", "stale", string.Equals(selected, "stale", StringComparison.OrdinalIgnoreCase)),
                new("Missing push token", "missing-push", string.Equals(selected, "missing-push", StringComparison.OrdinalIgnoreCase)),
                new("Notifications disabled", "notifications-disabled", string.Equals(selected, "notifications-disabled", StringComparison.OrdinalIgnoreCase)),
                new("Business members only", "business-members", string.Equals(selected, "business-members", StringComparison.OrdinalIgnoreCase))
            ];
        }
    }
}
