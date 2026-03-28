using Darwin.Application.Businesses.Queries;
using Darwin.Application.Settings.Queries;
using Darwin.WebAdmin.Controllers.Admin;
using Darwin.WebAdmin.ViewModels.Mobile;
using Microsoft.AspNetCore.Mvc;

namespace Darwin.WebAdmin.Controllers.Admin.Mobile
{
    public sealed class MobileOperationsController : AdminBaseController
    {
        private readonly GetSiteSettingHandler _getSiteSettings;
        private readonly GetBusinessSupportSummaryHandler _getBusinessSupportSummary;
        private readonly GetBusinessCommunicationOpsSummaryHandler _getCommunicationSummary;

        public MobileOperationsController(
            GetSiteSettingHandler getSiteSettings,
            GetBusinessSupportSummaryHandler getBusinessSupportSummary,
            GetBusinessCommunicationOpsSummaryHandler getCommunicationSummary)
        {
            _getSiteSettings = getSiteSettings;
            _getBusinessSupportSummary = getBusinessSupportSummary;
            _getCommunicationSummary = getCommunicationSummary;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken ct = default)
        {
            var settings = await _getSiteSettings.HandleAsync(ct).ConfigureAwait(false);
            if (settings is null)
            {
                TempData["Error"] = "Site settings are missing. Configure system settings before relying on mobile operations diagnostics.";
                return RedirectToAction("Edit", "SiteSettings");
            }

            var support = await _getBusinessSupportSummary.HandleAsync(ct: ct).ConfigureAwait(false);
            var comms = await _getCommunicationSummary.HandleAsync(ct: ct).ConfigureAwait(false);

            return View(new MobileOperationsVm
            {
                JwtEnabled = settings.JwtEnabled,
                JwtSingleDeviceOnly = settings.JwtSingleDeviceOnly,
                JwtRequireDeviceBinding = settings.JwtRequireDeviceBinding,
                JwtAccessTokenMinutes = settings.JwtAccessTokenMinutes,
                JwtRefreshTokenDays = settings.JwtRefreshTokenDays,
                MobileQrTokenRefreshSeconds = settings.MobileQrTokenRefreshSeconds,
                MobileMaxOutboxItems = settings.MobileMaxOutboxItems,
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
                                              !string.IsNullOrWhiteSpace(settings.AdminAlertSmsRecipientsCsv)
            });
        }
    }
}
