using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Businesses.DTOs;
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
        private readonly GetBusinessCommunicationProfileHandler _getProfile;
        private readonly GetEmailDispatchAuditsPageHandler _getEmailDispatchAuditsPage;
        private readonly ISiteSettingCache _siteSettingCache;

        public BusinessCommunicationsController(
            GetBusinessCommunicationOpsSummaryHandler getSummary,
            GetBusinessCommunicationSetupPageHandler getSetupPage,
            GetBusinessCommunicationProfileHandler getProfile,
            GetEmailDispatchAuditsPageHandler getEmailDispatchAuditsPage,
            ISiteSettingCache siteSettingCache)
        {
            _getSummary = getSummary;
            _getSetupPage = getSetupPage;
            _getProfile = getProfile;
            _getEmailDispatchAuditsPage = getEmailDispatchAuditsPage;
            _siteSettingCache = siteSettingCache;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            int page = 1,
            int pageSize = 20,
            string? query = null,
            bool setupOnly = true,
            BusinessCommunicationSetupFilter filter = BusinessCommunicationSetupFilter.NeedsSetup,
            CancellationToken ct = default)
        {
            var summaryTask = _getSummary.HandleAsync(ct);
            var settingsTask = _siteSettingCache.GetAsync(ct);
            var setupPageTask = _getSetupPage.HandleAsync(page, pageSize, query, setupOnly, filter, ct);
            var emailAuditTask = _getEmailDispatchAuditsPage.HandleAsync(1, 10, null, null, null, null, ct);

            await Task.WhenAll(summaryTask, settingsTask, setupPageTask, emailAuditTask).ConfigureAwait(false);

            var summary = await summaryTask.ConfigureAwait(false);
            var settings = await settingsTask.ConfigureAwait(false);
            var (items, total) = await setupPageTask.ConfigureAwait(false);
            var (emailAudits, _) = await emailAuditTask.ConfigureAwait(false);

            var vm = new BusinessCommunicationOpsVm
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = query ?? string.Empty,
                SetupOnly = setupOnly,
                Filter = filter,
                PageSizeItems = BuildPageSizeItems(pageSize),
                FilterItems = BuildFilterItems(filter),
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
                BuiltInFlows = BuildBuiltInFlows(),
                CapabilityCoverage = BuildCapabilityCoverage(),
                RecentEmailAudits = emailAudits.Select(x => new EmailDispatchAuditListItemVm
                {
                    Id = x.Id,
                    Provider = x.Provider,
                    FlowKey = x.FlowKey,
                    BusinessId = x.BusinessId,
                    RecipientEmail = x.RecipientEmail,
                    Subject = x.Subject,
                    Status = x.Status,
                    AttemptedAtUtc = x.AttemptedAtUtc,
                    CompletedAtUtc = x.CompletedAtUtc,
                    FailureMessage = x.FailureMessage
                }).ToList(),
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

        [HttpGet]
        public async Task<IActionResult> Details(Guid businessId, CancellationToken ct = default)
        {
            var profile = await _getProfile.HandleAsync(businessId, ct).ConfigureAwait(false);
            if (profile is null)
            {
                TempData["Error"] = "Business communication profile not found.";
                return RedirectToAction(nameof(Index));
            }

            var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);
            var (recentAudits, _) = await _getEmailDispatchAuditsPage
                .HandleAsync(1, 5, null, null, null, businessId, ct)
                .ConfigureAwait(false);
            var emailTransportConfigured = settings.SmtpEnabled &&
                                           !string.IsNullOrWhiteSpace(settings.SmtpHost) &&
                                           settings.SmtpPort.HasValue &&
                                           !string.IsNullOrWhiteSpace(settings.SmtpFromAddress);
            var smsTransportConfigured = settings.SmsEnabled &&
                                         !string.IsNullOrWhiteSpace(settings.SmsProvider) &&
                                         !string.IsNullOrWhiteSpace(settings.SmsFromPhoneE164);
            var whatsAppTransportConfigured = settings.WhatsAppEnabled &&
                                              !string.IsNullOrWhiteSpace(settings.WhatsAppBusinessPhoneId) &&
                                              !string.IsNullOrWhiteSpace(settings.WhatsAppAccessToken);
            var adminAlertRoutingConfigured = !string.IsNullOrWhiteSpace(settings.AdminAlertEmailsCsv) ||
                                              !string.IsNullOrWhiteSpace(settings.AdminAlertSmsRecipientsCsv);

            var vm = new BusinessCommunicationProfileVm
            {
                Id = profile.Id,
                Name = profile.Name,
                LegalName = profile.LegalName,
                ContactEmail = profile.ContactEmail,
                DefaultCulture = profile.DefaultCulture,
                DefaultTimeZoneId = profile.DefaultTimeZoneId,
                IsActive = profile.IsActive,
                OperationalStatus = profile.OperationalStatus,
                SupportEmail = profile.SupportEmail,
                CommunicationSenderName = profile.CommunicationSenderName,
                CommunicationReplyToEmail = profile.CommunicationReplyToEmail,
                CustomerEmailNotificationsEnabled = profile.CustomerEmailNotificationsEnabled,
                CustomerMarketingEmailsEnabled = profile.CustomerMarketingEmailsEnabled,
                OperationalAlertEmailsEnabled = profile.OperationalAlertEmailsEnabled,
                MissingSupportEmail = profile.MissingSupportEmail,
                MissingSenderIdentity = profile.MissingSenderIdentity,
                OpenInvitationCount = profile.OpenInvitationCount,
                PendingActivationMemberCount = profile.PendingActivationMemberCount,
                LockedMemberCount = profile.LockedMemberCount,
                EmailTransportConfigured = emailTransportConfigured,
                SmsTransportConfigured = smsTransportConfigured,
                WhatsAppTransportConfigured = whatsAppTransportConfigured,
                AdminAlertRoutingConfigured = adminAlertRoutingConfigured,
                ActiveFlowNames = BuildActiveFlowNames(profile),
                ReadinessIssues = BuildReadinessIssues(profile, emailTransportConfigured, adminAlertRoutingConfigured),
                RecommendedActions = BuildRecommendedActions(profile, emailTransportConfigured, adminAlertRoutingConfigured),
                RecentEmailAudits = recentAudits.Select(x => new EmailDispatchAuditListItemVm
                {
                    Id = x.Id,
                    Provider = x.Provider,
                    FlowKey = x.FlowKey,
                    BusinessId = x.BusinessId,
                    RecipientEmail = x.RecipientEmail,
                    Subject = x.Subject,
                    Status = x.Status,
                    AttemptedAtUtc = x.AttemptedAtUtc,
                    CompletedAtUtc = x.CompletedAtUtc,
                    FailureMessage = x.FailureMessage
                }).ToList()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> EmailAudits(
            int page = 1,
            int pageSize = 20,
            string? query = null,
            string? status = null,
            string? flowKey = null,
            Guid? businessId = null,
            CancellationToken ct = default)
        {
            var (items, total) = await _getEmailDispatchAuditsPage.HandleAsync(page, pageSize, query, status, flowKey, businessId, ct).ConfigureAwait(false);

            var vm = new EmailDispatchAuditsListVm
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = query ?? string.Empty,
                Status = status ?? string.Empty,
                FlowKey = flowKey ?? string.Empty,
                BusinessId = businessId,
                PageSizeItems = BuildPageSizeItems(pageSize),
                StatusItems = BuildAuditStatusItems(status),
                FlowItems = BuildAuditFlowItems(flowKey),
                Items = items.Select(x => new EmailDispatchAuditListItemVm
                {
                    Id = x.Id,
                    Provider = x.Provider,
                    FlowKey = x.FlowKey,
                    BusinessId = x.BusinessId,
                    RecipientEmail = x.RecipientEmail,
                    Subject = x.Subject,
                    Status = x.Status,
                    AttemptedAtUtc = x.AttemptedAtUtc,
                    CompletedAtUtc = x.CompletedAtUtc,
                    FailureMessage = x.FailureMessage
                }).ToList()
            };

            return View(vm);
        }

        private static IEnumerable<SelectListItem> BuildPageSizeItems(int selectedPageSize)
        {
            var sizes = new[] { 10, 20, 50, 100 };
            return sizes.Select(x => new SelectListItem(x.ToString(), x.ToString(), x == selectedPageSize)).ToList();
        }

        private static IEnumerable<SelectListItem> BuildFilterItems(BusinessCommunicationSetupFilter selectedFilter)
        {
            yield return new SelectListItem("Needs setup", BusinessCommunicationSetupFilter.NeedsSetup.ToString(), selectedFilter == BusinessCommunicationSetupFilter.NeedsSetup);
            yield return new SelectListItem("Missing support email", BusinessCommunicationSetupFilter.MissingSupportEmail.ToString(), selectedFilter == BusinessCommunicationSetupFilter.MissingSupportEmail);
            yield return new SelectListItem("Missing sender identity", BusinessCommunicationSetupFilter.MissingSenderIdentity.ToString(), selectedFilter == BusinessCommunicationSetupFilter.MissingSenderIdentity);
            yield return new SelectListItem("Transactional enabled", BusinessCommunicationSetupFilter.TransactionalEnabled.ToString(), selectedFilter == BusinessCommunicationSetupFilter.TransactionalEnabled);
            yield return new SelectListItem("Marketing enabled", BusinessCommunicationSetupFilter.MarketingEnabled.ToString(), selectedFilter == BusinessCommunicationSetupFilter.MarketingEnabled);
            yield return new SelectListItem("Operational alerts enabled", BusinessCommunicationSetupFilter.OperationalAlertsEnabled.ToString(), selectedFilter == BusinessCommunicationSetupFilter.OperationalAlertsEnabled);
            yield return new SelectListItem("All businesses", BusinessCommunicationSetupFilter.All.ToString(), selectedFilter == BusinessCommunicationSetupFilter.All);
        }

        private static IEnumerable<SelectListItem> BuildAuditStatusItems(string? selectedStatus)
        {
            yield return new SelectListItem("All statuses", string.Empty, string.IsNullOrWhiteSpace(selectedStatus));
            yield return new SelectListItem("Sent", "Sent", string.Equals(selectedStatus, "Sent", System.StringComparison.OrdinalIgnoreCase));
            yield return new SelectListItem("Failed", "Failed", string.Equals(selectedStatus, "Failed", System.StringComparison.OrdinalIgnoreCase));
            yield return new SelectListItem("Pending", "Pending", string.Equals(selectedStatus, "Pending", System.StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<SelectListItem> BuildAuditFlowItems(string? selectedFlowKey)
        {
            yield return new SelectListItem("All flows", string.Empty, string.IsNullOrWhiteSpace(selectedFlowKey));
            yield return new SelectListItem("Business Invitation", "BusinessInvitation", string.Equals(selectedFlowKey, "BusinessInvitation", System.StringComparison.OrdinalIgnoreCase));
            yield return new SelectListItem("Account Activation", "AccountActivation", string.Equals(selectedFlowKey, "AccountActivation", System.StringComparison.OrdinalIgnoreCase));
            yield return new SelectListItem("Password Reset", "PasswordReset", string.Equals(selectedFlowKey, "PasswordReset", System.StringComparison.OrdinalIgnoreCase));
        }

        private static List<BuiltInCommunicationFlowVm> BuildBuiltInFlows()
        {
            return new List<BuiltInCommunicationFlowVm>
            {
                new()
                {
                    Name = "Business Invitation",
                    Channel = "Email",
                    Trigger = "Create invitation / resend invitation",
                    DeliveryPath = "SMTP via IEmailSender",
                    CurrentImplementationStatus = "Live, hard-coded transactional composition",
                    NextStep = "Move to Communication Core template + delivery log"
                },
                new()
                {
                    Name = "Account Activation",
                    Channel = "Email",
                    Trigger = "Registration or resend activation",
                    DeliveryPath = "SMTP via IEmailSender",
                    CurrentImplementationStatus = "Live, token-based hard-coded composition",
                    NextStep = "Move to template engine + confirmation analytics/logging"
                },
                new()
                {
                    Name = "Password Reset",
                    Channel = "Email",
                    Trigger = "Forgot password or admin reset support",
                    DeliveryPath = "SMTP via IEmailSender",
                    CurrentImplementationStatus = "Live, token-based hard-coded composition",
                    NextStep = "Move to template engine + delivery/audit visibility"
                },
                new()
                {
                    Name = "Admin Alerts",
                    Channel = "Email/SMS/WhatsApp",
                    Trigger = "Platform/operator escalation paths",
                    DeliveryPath = "Configuration exists, full flow not yet centralized",
                    CurrentImplementationStatus = "Partially configured, not a complete Communication Core workflow",
                    NextStep = "Implement reusable alert pipeline + logs"
                }
            };
        }

        private static List<CommunicationCapabilityCoverageVm> BuildCapabilityCoverage()
        {
            return new List<CommunicationCapabilityCoverageVm>
            {
                new()
                {
                    Capability = "Template Engine",
                    CurrentState = "Not implemented as a reusable platform capability",
                    OperatorVisibility = "Flows are documented in the workspace, but body/subject editing is not exposed",
                    NextStep = "Move invitation, activation, and password reset into Communication Core templates"
                },
                new()
                {
                    Capability = "Delivery Logging",
                    CurrentState = "Phase-1 SMTP audit rows exist with flow and optional business correlation",
                    OperatorVisibility = "Dashboard preview and full email-audit screen are available",
                    NextStep = "Expand into searchable multi-channel Communication Core delivery logs"
                },
                new()
                {
                    Capability = "Retry / Resend Workflow",
                    CurrentState = "Only business invitations have explicit resend actions today",
                    OperatorVisibility = "Operators can resend invitations, but generic failed-email retry does not exist",
                    NextStep = "Design controlled retry/resend policies per flow before adding a shared retry action"
                },
                new()
                {
                    Capability = "Per-Business Policy Visibility",
                    CurrentState = "Branding, sender defaults, reply-to, and phase-1 communication toggles are stored on Business",
                    OperatorVisibility = "Queue, detail, and setup screens now expose these policies",
                    NextStep = "Split further into template policy, channel policy, and delivery visibility"
                }
            };
        }

        private static List<string> BuildActiveFlowNames(BusinessCommunicationProfileDto profile)
        {
            var flows = new List<string>();

            if (profile.OpenInvitationCount > 0)
            {
                flows.Add("Business Invitation");
            }

            if (profile.PendingActivationMemberCount > 0)
            {
                flows.Add("Account Activation");
            }

            if (profile.CustomerEmailNotificationsEnabled)
            {
                flows.Add("Password Reset / Transactional Email Readiness");
            }

            if (profile.OperationalAlertEmailsEnabled)
            {
                flows.Add("Admin Alerts");
            }

            return flows;
        }

        private static List<string> BuildReadinessIssues(
            BusinessCommunicationProfileDto profile,
            bool emailTransportConfigured,
            bool adminAlertRoutingConfigured)
        {
            var issues = new List<string>();

            if (profile.MissingSupportEmail)
            {
                issues.Add("Business support email is missing.");
            }

            if (profile.MissingSenderIdentity)
            {
                issues.Add("Sender name or reply-to email is incomplete.");
            }

            if ((profile.CustomerEmailNotificationsEnabled || profile.CustomerMarketingEmailsEnabled) && !emailTransportConfigured)
            {
                issues.Add("Email policies are enabled, but global SMTP transport is not fully configured.");
            }

            if (profile.OperationalAlertEmailsEnabled && !adminAlertRoutingConfigured)
            {
                issues.Add("Operational alerts are enabled, but global admin alert routing is not configured.");
            }

            if (string.Equals(profile.OperationalStatus, "PendingApproval", System.StringComparison.OrdinalIgnoreCase))
            {
                issues.Add("Business is still pending approval; communication readiness should be reviewed before go-live.");
            }

            if (!profile.IsActive)
            {
                issues.Add("Business is inactive; communication settings may be complete but operational use is currently blocked.");
            }

            return issues;
        }

        private static List<string> BuildRecommendedActions(
            BusinessCommunicationProfileDto profile,
            bool emailTransportConfigured,
            bool adminAlertRoutingConfigured)
        {
            var actions = new List<string>();

            if (profile.MissingSupportEmail || profile.MissingSenderIdentity)
            {
                actions.Add("Open business setup and complete support email, sender name, and reply-to defaults.");
            }

            if ((profile.CustomerEmailNotificationsEnabled || profile.CustomerMarketingEmailsEnabled) && !emailTransportConfigured)
            {
                actions.Add("Open global SMTP settings before relying on transactional or marketing email for this business.");
            }

            if (profile.OperationalAlertEmailsEnabled && !adminAlertRoutingConfigured)
            {
                actions.Add("Configure admin alert routing so business operational alerts have a real escalation target.");
            }

            if (profile.PendingActivationMemberCount > 0)
            {
                actions.Add("Review business members and send activation email or confirm email where policy allows.");
            }

            if (profile.OpenInvitationCount > 0)
            {
                actions.Add("Review open invitations and resend or revoke stale invites before go-live.");
            }

            if (profile.LockedMemberCount > 0)
            {
                actions.Add("Review locked members and unlock only after support validation.");
            }

            if (string.Equals(profile.OperationalStatus, "PendingApproval", System.StringComparison.OrdinalIgnoreCase))
            {
                actions.Add("Complete communication setup before approving the business for live operations.");
            }

            if (actions.Count == 0)
            {
                actions.Add("No immediate operator action is recommended from communication readiness alone.");
            }

            return actions;
        }
    }
}
