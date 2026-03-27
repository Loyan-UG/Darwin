using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Businesses.Queries;
using Darwin.WebAdmin.Security;
using Darwin.WebAdmin.Services.Settings;
using Darwin.WebAdmin.ViewModels.Admin;
using Darwin.WebAdmin.ViewModels.Businesses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Darwin.WebAdmin.Controllers.Admin.Businesses
{
    /// <summary>
    /// Read-only communication operations workspace for onboarding and support operators.
    /// </summary>
    [PermissionAuthorize(PermissionKeys.ManageBusinessSupport)]
    public sealed class BusinessCommunicationsController : AdminBaseController
    {
        private readonly GetBusinessCommunicationOpsSummaryHandler _getSummary;
        private readonly GetBusinessCommunicationSetupPageHandler _getSetupPage;
        private readonly ISiteSettingCache _siteSettingCache;

        public BusinessCommunicationsController(
            GetBusinessCommunicationOpsSummaryHandler getSummary,
            GetBusinessCommunicationSetupPageHandler getSetupPage,
            ISiteSettingCache siteSettingCache)
        {
            _getSummary = getSummary;
            _getSetupPage = getSetupPage;
            _siteSettingCache = siteSettingCache;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            int page = 1,
            int pageSize = 20,
            string? query = null,
            bool setupOnly = true,
            CancellationToken ct = default)
        {
            var summaryTask = _getSummary.HandleAsync(ct);
            var settingsTask = _siteSettingCache.GetAsync(ct);
            var setupPageTask = _getSetupPage.HandleAsync(page, pageSize, query, setupOnly, ct);

            await Task.WhenAll(summaryTask, settingsTask, setupPageTask).ConfigureAwait(false);

            var summary = await summaryTask.ConfigureAwait(false);
            var settings = await settingsTask.ConfigureAwait(false);
            var (items, total) = await setupPageTask.ConfigureAwait(false);

            var vm = new BusinessCommunicationOpsVm
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = query ?? string.Empty,
                SetupOnly = setupOnly,
                PageSizeItems = BuildPageSizeItems(pageSize),
                Transport = new BusinessCommunicationOpsTransportVm
                {
                    EmailTransportConfigured = settings.SmtpEnabled &&
                                               !string.IsNullOrWhiteSpace(settings.SmtpHost) &&
                                               settings.SmtpPort.HasValue &&
                                               !string.IsNullOrWhiteSpace(settings.SmtpFromAddress),
                    SmsTransportConfigured = settings.SmsEnabled &&
                                             !string.IsNullOrWhiteSpace(settings.SmsProvider) &&
                                             !string.IsNullOrWhiteSpace(settings.SmsFromPhoneE164),
                    WhatsAppTransportConfigured = settings.WhatsAppEnabled &&
                                                  !string.IsNullOrWhiteSpace(settings.WhatsAppBusinessPhoneId) &&
                                                  !string.IsNullOrWhiteSpace(settings.WhatsAppAccessToken),
                    AdminAlertRoutingConfigured = !string.IsNullOrWhiteSpace(settings.AdminAlertEmailsCsv) ||
                                                  !string.IsNullOrWhiteSpace(settings.AdminAlertSmsRecipientsCsv)
                },
                Summary = new BusinessCommunicationOpsSummaryPanelVm
                {
                    TransactionalEmailBusinessesCount = summary.BusinessesWithCustomerEmailNotificationsEnabledCount,
                    MarketingEmailBusinessesCount = summary.BusinessesWithMarketingEmailsEnabledCount,
                    OperationalAlertBusinessesCount = summary.BusinessesWithOperationalAlertEmailsEnabledCount,
                    MissingSupportEmailCount = summary.BusinessesMissingSupportEmailCount,
                    MissingSenderIdentityCount = summary.BusinessesMissingSenderIdentityCount,
                    BusinessesRequiringEmailSetupCount = summary.BusinessesRequiringEmailSetupCount
                },
                Items = items.Select(x => new BusinessCommunicationSetupListItemVm
                {
                    Id = x.Id,
                    Name = x.Name,
                    LegalName = x.LegalName,
                    SupportEmail = x.SupportEmail,
                    CommunicationSenderName = x.CommunicationSenderName,
                    CommunicationReplyToEmail = x.CommunicationReplyToEmail,
                    CustomerEmailNotificationsEnabled = x.CustomerEmailNotificationsEnabled,
                    CustomerMarketingEmailsEnabled = x.CustomerMarketingEmailsEnabled,
                    OperationalAlertEmailsEnabled = x.OperationalAlertEmailsEnabled,
                    MissingSupportEmail = x.MissingSupportEmail,
                    MissingSenderIdentity = x.MissingSenderIdentity
                }).ToList()
            };

            return View(vm);
        }

        private static IEnumerable<SelectListItem> BuildPageSizeItems(int selectedPageSize)
        {
            var sizes = new[] { 10, 20, 50, 100 };
            return sizes.Select(x => new SelectListItem(x.ToString(), x.ToString(), x == selectedPageSize)).ToList();
        }
    }
}
