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
            _getSiteSettings = getSiteSettings ?? throw new ArgumentNullException(nameof(getSiteSettings));
            _getBusinessSupportSummary = getBusinessSupportSummary ?? throw new ArgumentNullException(nameof(getBusinessSupportSummary));
            _getCommunicationSummary = getCommunicationSummary ?? throw new ArgumentNullException(nameof(getCommunicationSummary));
            _getDeviceSummary = getDeviceSummary ?? throw new ArgumentNullException(nameof(getDeviceSummary));
            _getDevicesPage = getDevicesPage ?? throw new ArgumentNullException(nameof(getDevicesPage));
            _clearDevicePushToken = clearDevicePushToken ?? throw new ArgumentNullException(nameof(clearDevicePushToken));
            _deactivateDevice = deactivateDevice ?? throw new ArgumentNullException(nameof(deactivateDevice));
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? q = null,
            Guid? businessId = null,
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
            var devicesPage = await _getDevicesPage.HandleAsync(page, pageSize, q, platform, state, businessId, ct).ConfigureAwait(false);

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
                PendingInvitationCount = support.PendingInvitationCount,
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
                BusinessId = businessId,
                PlatformFilter = platform,
                StateFilter = state ?? string.Empty,
                PlatformItems = BuildPlatformItems(platform),
                StateItems = BuildStateItems(state),
                Playbooks = BuildPlaybooks(businessId),
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
        public async Task<IActionResult> ClearPushToken(Guid id, string? rowVersion, string? q = null, Guid? businessId = null, MobilePlatform? platform = null, string? state = null, int page = 1, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("MobilePushTokenClearFailed");
                return RedirectOrHtmx(nameof(Index), null, new { q, businessId, platform, state, page });
            }

            var result = await _clearDevicePushToken.HandleAsync(id, DecodeBase64RowVersion(rowVersion), ct).ConfigureAwait(false);
            if (result.Succeeded)
            {
                SetSuccessMessage("MobilePushTokenCleared");
            }
            else
            {
                SetErrorMessage("MobilePushTokenClearFailed");
            }

            return RedirectOrHtmx(nameof(Index), null, new { q, businessId, platform, state, page });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateDevice(Guid id, string? rowVersion, string? q = null, Guid? businessId = null, MobilePlatform? platform = null, string? state = null, int page = 1, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("MobileDeviceDeactivateFailed");
                return RedirectOrHtmx(nameof(Index), null, new { q, businessId, platform, state, page });
            }

            var result = await _deactivateDevice.HandleAsync(id, DecodeBase64RowVersion(rowVersion), ct).ConfigureAwait(false);
            if (result.Succeeded)
            {
                SetSuccessMessage("MobileDeviceDeactivated");
            }
            else
            {
                SetErrorMessage("MobileDeviceDeactivateFailed");
            }

            return RedirectOrHtmx(nameof(Index), null, new { q, businessId, platform, state, page });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateStaleDevices(string? q = null, Guid? businessId = null, MobilePlatform? platform = null, CancellationToken ct = default)
        {
            var result = await _deactivateDevice.HandleStaleBatchAsync(businessId, platform, ct: ct).ConfigureAwait(false);
            if (result.Succeeded && result.Value is not null)
            {
                TempData["Success"] = string.Format(T("MobileStaleDevicesDeactivated"), result.Value.AffectedCount);
            }
            else
            {
                SetErrorMessage("MobileStaleDevicesDeactivateFailed");
            }

            return RedirectOrHtmx(nameof(Index), null, new { q, businessId, platform, state = "stale" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearNotificationsDisabledPushTokens(string? q = null, Guid? businessId = null, MobilePlatform? platform = null, CancellationToken ct = default)
        {
            var result = await _clearDevicePushToken.HandleNotificationsDisabledBatchAsync(businessId, platform, ct: ct).ConfigureAwait(false);
            if (result.Succeeded && result.Value is not null)
            {
                TempData["Success"] = string.Format(T("MobileNotificationsDisabledPushTokensCleared"), result.Value.AffectedCount);
            }
            else
            {
                SetErrorMessage("MobileNotificationsDisabledPushTokensClearFailed");
            }

            return RedirectOrHtmx(nameof(Index), null, new { q, businessId, platform, state = "notifications-disabled" });
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

        private List<MobileOpsPlaybookVm> BuildPlaybooks(Guid? businessId)
        {
            return new List<MobileOpsPlaybookVm>
            {
                new()
                {
                    Title = T("MobilePlaybookPushDebtTitle"),
                    ScopeNote = T("MobilePlaybookPushDebtScope"),
                    OperatorAction = T("MobilePlaybookPushDebtAction"),
                    QueueActionLabel = T("MobileMissingPushToken"),
                    QueueActionUrl = Url.Action(nameof(Index), "MobileOperations", new { businessId, state = "missing-push" }) ?? string.Empty,
                    FollowUpLabel = T("ReviewProviderLane"),
                    FollowUpUrl = Url.Action("ChannelAudits", "BusinessCommunications", new { providerReviewOnly = true }) ?? string.Empty
                },
                new()
                {
                    Title = T("MobilePlaybookLoyaltyScanTitle"),
                    ScopeNote = T("MobilePlaybookLoyaltyScanScope"),
                    OperatorAction = T("MobilePlaybookLoyaltyScanAction"),
                    QueueActionLabel = T("BusinessMemberDevices"),
                    QueueActionUrl = Url.Action(nameof(Index), "MobileOperations", new { businessId, state = "business-members" }) ?? string.Empty,
                    FollowUpLabel = T("LoyaltyScanSessionsTitle"),
                    FollowUpUrl = Url.Action("ScanSessions", "Loyalty") ?? string.Empty
                },
                new()
                {
                    Title = T("MobilePlaybookTransportTitle"),
                    ScopeNote = T("MobilePlaybookTransportScope"),
                    OperatorAction = T("MobilePlaybookTransportAction"),
                    QueueActionLabel = T("OpenCommunicationsWorkspace"),
                    QueueActionUrl = Url.Action("Index", "BusinessCommunications") ?? string.Empty,
                    FollowUpLabel = T("OpenMobileBootstrap"),
                    FollowUpUrl = Url.Action("Edit", "SiteSettings", new { fragment = "site-settings-mobile" }) ?? string.Empty
                },
                new()
                {
                    Title = T("MobilePlaybookProviderLaneTitle"),
                    ScopeNote = T("MobilePlaybookProviderLaneScope"),
                    OperatorAction = T("MobilePlaybookProviderLaneAction"),
                    QueueActionLabel = T("ReviewProviderLane"),
                    QueueActionUrl = Url.Action("ChannelAudits", "BusinessCommunications", new { providerReviewOnly = true }) ?? string.Empty,
                    FollowUpLabel = T("OpenCommunicationsWorkspace"),
                    FollowUpUrl = Url.Action("Index", "BusinessCommunications") ?? string.Empty
                },
                new()
                {
                    Title = T("MobilePlaybookMemberLifecycleTitle"),
                    ScopeNote = T("MobilePlaybookMemberLifecycleScope"),
                    OperatorAction = T("MobilePlaybookMemberLifecycleAction"),
                    QueueActionLabel = T("SupportQueue"),
                    QueueActionUrl = Url.Action("SupportQueue", "Businesses", new { businessId }) ?? string.Empty,
                    FollowUpLabel = T("UsersFilterMobileLinked"),
                    FollowUpUrl = Url.Action("Index", "Users", new { filter = Darwin.Application.Identity.DTOs.UserQueueFilter.MobileLinked }) ?? string.Empty
                }
            };
        }

        private List<SelectListItem> BuildPlatformItems(MobilePlatform? selected)
        {
            var items = new List<SelectListItem> { new(T("MobileAllPlatforms"), string.Empty, !selected.HasValue) };
            items.AddRange(Enum.GetValues<MobilePlatform>()
                .Where(x => x != MobilePlatform.Unknown)
                .Select(x => new SelectListItem(T(x.ToString()), x.ToString(), selected == x)));
            return items;
        }

        private List<SelectListItem> BuildStateItems(string? selected)
        {
            return
            [
                new(T("MobileAllStates"), string.Empty, string.IsNullOrWhiteSpace(selected)),
                new(T("MobileStaleDevices"), "stale", string.Equals(selected, "stale", StringComparison.OrdinalIgnoreCase)),
                new(T("MobileMissingPushToken"), "missing-push", string.Equals(selected, "missing-push", StringComparison.OrdinalIgnoreCase)),
                new(T("MobileNotificationsDisabled"), "notifications-disabled", string.Equals(selected, "notifications-disabled", StringComparison.OrdinalIgnoreCase)),
                new(T("MobileBusinessMembersOnly"), "business-members", string.Equals(selected, "business-members", StringComparison.OrdinalIgnoreCase))
            ];
        }
    }
}



