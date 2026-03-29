using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Businesses.Queries;
using Darwin.Application.Abstractions.Notifications;
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
        private readonly IEmailSender _emailSender;

        public BusinessCommunicationsController(
            GetBusinessCommunicationOpsSummaryHandler getSummary,
            GetBusinessCommunicationSetupPageHandler getSetupPage,
            GetBusinessCommunicationProfileHandler getProfile,
            GetEmailDispatchAuditsPageHandler getEmailDispatchAuditsPage,
            ISiteSettingCache siteSettingCache,
            IEmailSender emailSender)
        {
            _getSummary = getSummary;
            _getSetupPage = getSetupPage;
            _getProfile = getProfile;
            _getEmailDispatchAuditsPage = getEmailDispatchAuditsPage;
            _siteSettingCache = siteSettingCache;
            _emailSender = emailSender;
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
            var summary = await _getSummary.HandleAsync(ct).ConfigureAwait(false);
            var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);
            var (items, total) = await _getSetupPage.HandleAsync(page, pageSize, query, setupOnly, filter, ct).ConfigureAwait(false);
            var (emailAudits, _) = await _getEmailDispatchAuditsPage.HandleAsync(1, 10, null, null, null, null, ct).ConfigureAwait(false);

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
                                                  !string.IsNullOrWhiteSpace(settings.AdminAlertSmsRecipientsCsv),
                    TransactionalSubjectPrefixConfigured = !string.IsNullOrWhiteSpace(settings.TransactionalEmailSubjectPrefix),
                    TestInboxConfigured = !string.IsNullOrWhiteSpace(settings.CommunicationTestInboxEmail),
                    SmsTestRecipientConfigured = !string.IsNullOrWhiteSpace(settings.CommunicationTestSmsRecipientE164),
                    WhatsAppTestRecipientConfigured = !string.IsNullOrWhiteSpace(settings.CommunicationTestWhatsAppRecipientE164),
                    TransactionalSubjectPrefix = settings.TransactionalEmailSubjectPrefix,
                    TestInboxEmail = settings.CommunicationTestInboxEmail,
                    TestSmsRecipientE164 = settings.CommunicationTestSmsRecipientE164,
                    TestWhatsAppRecipientE164 = settings.CommunicationTestWhatsAppRecipientE164,
                    CanSendTestEmail = settings.SmtpEnabled &&
                                       !string.IsNullOrWhiteSpace(settings.SmtpHost) &&
                                       settings.SmtpPort.HasValue &&
                                       !string.IsNullOrWhiteSpace(settings.SmtpFromAddress) &&
                                       !string.IsNullOrWhiteSpace(settings.CommunicationTestInboxEmail)
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
                TemplateInventory = BuildTemplateInventory(),
                CapabilityCoverage = BuildCapabilityCoverage(),
                ResendPolicies = BuildResendPolicies(),
                RecentEmailAudits = emailAudits.Select(x => new EmailDispatchAuditListItemVm
                {
                    Id = x.Id,
                    Provider = x.Provider,
                    FlowKey = x.FlowKey,
                    BusinessId = x.BusinessId,
                    BusinessName = x.BusinessName,
                    RecipientEmail = x.RecipientEmail,
                    Subject = x.Subject,
                    Status = x.Status,
                    AttemptedAtUtc = x.AttemptedAtUtc,
                    CompletedAtUtc = x.CompletedAtUtc,
                    FailureMessage = x.FailureMessage,
                    RecommendedAction = BuildAuditRecommendedAction(x)
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

            return RenderCommunicationsWorkspace(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid businessId, CancellationToken ct = default)
        {
            var profile = await _getProfile.HandleAsync(businessId, ct).ConfigureAwait(false);
            if (profile is null)
            {
                TempData["Error"] = "Business communication profile not found.";
                return RedirectOrHtmx(nameof(Index), new { });
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
            var transactionalSubjectPrefixConfigured = !string.IsNullOrWhiteSpace(settings.TransactionalEmailSubjectPrefix);
            var testInboxConfigured = !string.IsNullOrWhiteSpace(settings.CommunicationTestInboxEmail);

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
                TransactionalSubjectPrefixConfigured = transactionalSubjectPrefixConfigured,
                TestInboxConfigured = testInboxConfigured,
                TransactionalSubjectPrefix = settings.TransactionalEmailSubjectPrefix,
                TestInboxEmail = settings.CommunicationTestInboxEmail,
                ActiveFlowNames = BuildActiveFlowNames(profile),
                TemplateInventory = BuildTemplateInventory(),
                ResendPolicies = BuildResendPolicies(),
                ReadinessIssues = BuildReadinessIssues(profile, emailTransportConfigured, adminAlertRoutingConfigured),
                RecommendedActions = BuildRecommendedActions(profile, emailTransportConfigured, adminAlertRoutingConfigured),
                RecentEmailAudits = recentAudits.Select(x => new EmailDispatchAuditListItemVm
                {
                    Id = x.Id,
                    Provider = x.Provider,
                    FlowKey = x.FlowKey,
                    BusinessId = x.BusinessId,
                    BusinessName = x.BusinessName,
                    RecipientEmail = x.RecipientEmail,
                    Subject = x.Subject,
                    Status = x.Status,
                    AttemptedAtUtc = x.AttemptedAtUtc,
                    CompletedAtUtc = x.CompletedAtUtc,
                    FailureMessage = x.FailureMessage,
                    RecommendedAction = BuildAuditRecommendedAction(x)
                }).ToList()
            };

            return RenderCommunicationProfileWorkspace(vm);
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
            var summary = await _getEmailDispatchAuditsPage.GetSummaryAsync(businessId, ct).ConfigureAwait(false);

            var vm = new EmailDispatchAuditsListVm
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = query ?? string.Empty,
                Status = status ?? string.Empty,
                FlowKey = flowKey ?? string.Empty,
                BusinessId = businessId,
                Summary = new EmailDispatchAuditSummaryVm
                {
                    TotalCount = summary.TotalCount,
                    FailedCount = summary.FailedCount,
                    SentCount = summary.SentCount,
                    PendingCount = summary.PendingCount,
                    Recent24HourCount = summary.Recent24HourCount,
                    FailedInvitationCount = summary.FailedInvitationCount,
                    FailedActivationCount = summary.FailedActivationCount,
                    FailedPasswordResetCount = summary.FailedPasswordResetCount,
                    FailedAdminTestCount = summary.FailedAdminTestCount
                },
                PageSizeItems = BuildPageSizeItems(pageSize),
                StatusItems = BuildAuditStatusItems(status),
                FlowItems = BuildAuditFlowItems(flowKey),
                Playbooks = BuildAuditPlaybooks(),
                Items = items.Select(x => new EmailDispatchAuditListItemVm
                {
                    Id = x.Id,
                    Provider = x.Provider,
                    FlowKey = x.FlowKey,
                    BusinessId = x.BusinessId,
                    BusinessName = x.BusinessName,
                    RecipientEmail = x.RecipientEmail,
                    Subject = x.Subject,
                    Status = x.Status,
                    AttemptedAtUtc = x.AttemptedAtUtc,
                    CompletedAtUtc = x.CompletedAtUtc,
                    FailureMessage = x.FailureMessage,
                    RecommendedAction = BuildAuditRecommendedAction(x)
                }).ToList()
            };

            return RenderEmailAuditsWorkspace(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendTestEmail(CancellationToken ct = default)
        {
            var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);
            var emailTransportConfigured = settings.SmtpEnabled &&
                                           !string.IsNullOrWhiteSpace(settings.SmtpHost) &&
                                           settings.SmtpPort.HasValue &&
                                           !string.IsNullOrWhiteSpace(settings.SmtpFromAddress);

            if (!emailTransportConfigured)
            {
                TempData["Error"] = "SMTP transport is not ready. Complete email settings before sending a communication test email.";
                return RedirectOrHtmx(nameof(Index), new { });
            }

            if (string.IsNullOrWhiteSpace(settings.CommunicationTestInboxEmail))
            {
                TempData["Error"] = "Communication test inbox is not configured. Add it in site settings before sending a test email.";
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var prefix = string.IsNullOrWhiteSpace(settings.TransactionalEmailSubjectPrefix)
                ? string.Empty
                : $"{settings.TransactionalEmailSubjectPrefix.Trim()} ";
            var requestedBy = User?.Identity?.Name ?? "WebAdmin operator";
            var subject = $"{prefix}Darwin Communication Test";
            var htmlBody = $@"
<p>This is a Darwin WebAdmin communication test email.</p>
<ul>
  <li><strong>Requested by:</strong> {System.Net.WebUtility.HtmlEncode(requestedBy)}</li>
  <li><strong>Attempted at (UTC):</strong> {System.DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}</li>
  <li><strong>SMTP configured:</strong> {(emailTransportConfigured ? "Yes" : "No")}</li>
  <li><strong>Transactional subject prefix:</strong> {System.Net.WebUtility.HtmlEncode(settings.TransactionalEmailSubjectPrefix ?? "(none)")}</li>
  <li><strong>Test inbox:</strong> {System.Net.WebUtility.HtmlEncode(settings.CommunicationTestInboxEmail)}</li>
</ul>
<p>This test is intentionally sent only to the configured communication test inbox.</p>";

            await _emailSender.SendAsync(
                settings.CommunicationTestInboxEmail!,
                subject,
                htmlBody,
                ct,
                new EmailDispatchContext
                {
                    FlowKey = "AdminCommunicationTest"
                }).ConfigureAwait(false);

            TempData["Success"] = $"Communication test email sent to {settings.CommunicationTestInboxEmail}.";
            return RedirectOrHtmx(nameof(Index), new { });
        }

        private static IEnumerable<SelectListItem> BuildPageSizeItems(int selectedPageSize)
        {
            var sizes = new[] { 10, 20, 50, 100 };
            return sizes.Select(x => new SelectListItem(x.ToString(), x.ToString(), x == selectedPageSize)).ToList();
        }

        private IActionResult RenderCommunicationsWorkspace(BusinessCommunicationOpsVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/BusinessCommunications/Index.cshtml", vm);
            }

            return View("Index", vm);
        }

        private IActionResult RenderCommunicationProfileWorkspace(BusinessCommunicationProfileVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/BusinessCommunications/Details.cshtml", vm);
            }

            return View("Details", vm);
        }

        private IActionResult RenderEmailAuditsWorkspace(EmailDispatchAuditsListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/BusinessCommunications/EmailAudits.cshtml", vm);
            }

            return View("EmailAudits", vm);
        }

        private IActionResult RedirectOrHtmx(string actionName, object routeValues)
        {
            if (IsHtmxRequest())
            {
                Response.Headers["HX-Redirect"] = Url.Action(actionName, routeValues) ?? string.Empty;
                return new EmptyResult();
            }

            return RedirectToAction(actionName, routeValues);
        }

        private bool IsHtmxRequest()
        {
            return string.Equals(Request.Headers["HX-Request"], "true", System.StringComparison.OrdinalIgnoreCase);
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
            yield return new SelectListItem("Admin Communication Test", "AdminCommunicationTest", string.Equals(selectedFlowKey, "AdminCommunicationTest", System.StringComparison.OrdinalIgnoreCase));
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
                    Name = "Admin Communication Test",
                    Channel = "Email",
                    Trigger = "Manual operator validation from WebAdmin",
                    DeliveryPath = "SMTP via IEmailSender to the configured test inbox only",
                    CurrentImplementationStatus = "Live, operator-safe diagnostic action",
                    NextStep = "Later fold into Communication Core test/delivery diagnostics"
                },
                new()
                {
                    Name = "Admin Alerts",
                    Channel = "Email/SMS/WhatsApp",
                    Trigger = "Platform/operator escalation paths",
                    DeliveryPath = "Configuration exists, full flow not yet centralized",
                    CurrentImplementationStatus = "Partially configured, not a complete Communication Core workflow",
                    NextStep = "Implement reusable alert pipeline + logs"
                },
                new()
                {
                    Name = "SMS and WhatsApp Test Targets",
                    Channel = "SMS / WhatsApp",
                    Trigger = "Operator-side staged rollout validation",
                    DeliveryPath = "Configuration only; no shared test-send workflow implemented yet",
                    CurrentImplementationStatus = "Configured in site settings for visibility and later remediation planning",
                    NextStep = "Add provider-backed test and delivery diagnostics when multi-channel Communication Core lands"
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
                },
                new()
                {
                    Capability = "Channel Test Targets",
                    CurrentState = "Email, SMS, and WhatsApp test recipients are now configurable in site settings",
                    OperatorVisibility = "Workspace shows whether each channel has a safe test target before go-live validation",
                    NextStep = "Add provider-backed test-send actions only after channel-specific delivery handling is in place"
                }
            };
        }

        private static List<CommunicationTemplateInventoryVm> BuildTemplateInventory()
        {
            return new List<CommunicationTemplateInventoryVm>
            {
                new()
                {
                    FlowName = "Business Invitation",
                    TemplateSurface = "Hard-coded transactional composition",
                    SubjectSource = "Transactional subject prefix + fixed invitation subject",
                    BodySource = "Inline HTML generated in the invitation handler",
                    OperatorControl = "No body editor. Operators can only resend or revoke the invitation from business support surfaces.",
                    AuditFlowKey = "BusinessInvitation",
                    OperatorActionLabel = "Open Invitations",
                    OperatorActionTarget = "Invitations",
                    NextStep = "Promote to Communication Core template catalog with preview and delivery logging."
                },
                new()
                {
                    FlowName = "Account Activation",
                    TemplateSurface = "Hard-coded transactional composition",
                    SubjectSource = "Transactional subject prefix + fixed activation subject",
                    BodySource = "Inline HTML generated in the confirmation handler",
                    OperatorControl = "No template editing. Operators can trigger activation support but not rewrite the message.",
                    AuditFlowKey = "AccountActivation",
                    OperatorActionLabel = "Open Users",
                    OperatorActionTarget = "Users",
                    NextStep = "Move into reusable template + confirmation analytics flow."
                },
                new()
                {
                    FlowName = "Password Reset",
                    TemplateSurface = "Hard-coded transactional composition",
                    SubjectSource = "Transactional subject prefix + fixed password-reset subject",
                    BodySource = "Inline HTML generated in the password-reset handler",
                    OperatorControl = "No template editing. Operators can reissue support actions only after identity validation.",
                    AuditFlowKey = "PasswordReset",
                    OperatorActionLabel = "Open Users",
                    OperatorActionTarget = "Users",
                    NextStep = "Move into Communication Core template inventory with controlled support resend."
                },
                new()
                {
                    FlowName = "Admin Communication Test",
                    TemplateSurface = "Operator-only diagnostic email",
                    SubjectSource = "Transactional subject prefix + fixed diagnostic subject",
                    BodySource = "Inline HTML generated in WebAdmin for the configured test inbox only",
                    OperatorControl = "Operators can send the test email when SMTP and the test inbox are configured.",
                    AuditFlowKey = "AdminCommunicationTest",
                    OperatorActionLabel = "Open Audit Log",
                    OperatorActionTarget = "EmailAudits",
                    NextStep = "Fold into multi-channel diagnostic templates once Communication Core test sends exist."
                },
                new()
                {
                    FlowName = "Admin Alerts",
                    TemplateSurface = "Configuration visibility only",
                    SubjectSource = "Not centralized",
                    BodySource = "Not centralized",
                    OperatorControl = "No template catalog yet; only routing readiness and policy visibility are exposed.",
                    AuditFlowKey = string.Empty,
                    OperatorActionLabel = "Open Alert Settings",
                    OperatorActionTarget = "AdminAlerts",
                    NextStep = "Create alert template + routing inventory before adding delivery actions."
                }
            };
        }

        private static List<CommunicationResendPolicyVm> BuildResendPolicies()
        {
            return new List<CommunicationResendPolicyVm>
            {
                new()
                {
                    FlowName = "Business Invitation",
                    CurrentSafeAction = "Use invitation resend/revoke from the business invitation workspace after SMTP readiness is green.",
                    GenericRetryStatus = "No generic retry queue. Flow-specific resend exists.",
                    OperatorEntryPoint = "Business setup, invitations list, and email-audit handoff.",
                    AuditFlowKey = "BusinessInvitation",
                    OperatorActionLabel = "Open Invitations",
                    OperatorActionTarget = "Invitations",
                    EscalationRule = "If repeated resends fail after transport is ready, escalate as Communication Core/platform debt instead of repeating sends."
                },
                new()
                {
                    FlowName = "Account Activation",
                    CurrentSafeAction = "Prefer self-service resend; use admin activation support only where current policy allows.",
                    GenericRetryStatus = "No generic retry queue and no blind resend from email audits.",
                    OperatorEntryPoint = "Users queue, business members queue, and email-audit handoff.",
                    AuditFlowKey = "AccountActivation",
                    OperatorActionLabel = "Open Users",
                    OperatorActionTarget = "Users",
                    EscalationRule = "Do not bypass confirmation policy. Persistent failures after a validated resend are auth/communication troubleshooting."
                },
                new()
                {
                    FlowName = "Password Reset",
                    CurrentSafeAction = "Reissue reset only after identity validation and transport checks.",
                    GenericRetryStatus = "No generic retry queue and no automatic replay from failed audit rows.",
                    OperatorEntryPoint = "Users queue and business member support actions.",
                    AuditFlowKey = "PasswordReset",
                    OperatorActionLabel = "Open Users",
                    OperatorActionTarget = "Users",
                    EscalationRule = "Avoid repeated resets without verification. Persistent failures escalate as support/security or transport issues."
                },
                new()
                {
                    FlowName = "Admin Communication Test",
                    CurrentSafeAction = "Rerun the diagnostic only to the configured test inbox after fixing SMTP or policy settings.",
                    GenericRetryStatus = "No retry queue; manual rerun only.",
                    OperatorEntryPoint = "Business Communications workspace.",
                    AuditFlowKey = "AdminCommunicationTest",
                    OperatorActionLabel = "Open Audit Log",
                    OperatorActionTarget = "EmailAudits",
                    EscalationRule = "Never switch the diagnostic to live recipients. Keep tests scoped to the configured test target."
                },
                new()
                {
                    FlowName = "Admin Alerts / SMS / WhatsApp",
                    CurrentSafeAction = "Visibility and readiness only. No shared resend workflow exists yet.",
                    GenericRetryStatus = "Not implemented.",
                    OperatorEntryPoint = "Site settings and Business Communications readiness workspace.",
                    AuditFlowKey = string.Empty,
                    OperatorActionLabel = "Open Alert Settings",
                    OperatorActionTarget = "AdminAlerts",
                    EscalationRule = "Treat repeated alert-channel gaps as later-phase Communication Core work, not as manual resend tasks."
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

        private static string BuildAuditRecommendedAction(EmailDispatchAuditListItemDto item)
        {
            if (string.Equals(item.Status, "Sent", System.StringComparison.OrdinalIgnoreCase))
            {
                return "No immediate operator action required.";
            }

            if (string.Equals(item.FlowKey, "BusinessInvitation", System.StringComparison.OrdinalIgnoreCase))
            {
                return item.BusinessId.HasValue
                    ? "Check SMTP readiness, then review the business invitation and use resend/revoke from the business workspace."
                    : "Check SMTP readiness, then review the invitation source before resending.";
            }

            if (string.Equals(item.FlowKey, "AccountActivation", System.StringComparison.OrdinalIgnoreCase))
            {
                return "Check SMTP readiness, then direct the user to self-service resend activation or use admin activation support.";
            }

            if (string.Equals(item.FlowKey, "PasswordReset", System.StringComparison.OrdinalIgnoreCase))
            {
                return "Check SMTP readiness, then reissue password reset only after support validation.";
            }

            return "Review global transport readiness and the source workflow before attempting manual intervention.";
        }

        private static List<CommunicationFlowPlaybookVm> BuildAuditPlaybooks()
        {
            return new List<CommunicationFlowPlaybookVm>
            {
                new()
                {
                    FlowKey = "BusinessInvitation",
                    Title = "Invitation failures",
                    ScopeNote = "Use business-scoped invitation actions only.",
                    AllowedAction = "Check SMTP readiness, then open the business invitation workspace and resend or revoke the invitation.",
                    EscalationRule = "If repeated failures continue after transport readiness is green, treat it as communication-platform debt instead of repeatedly sending the same invitation."
                },
                new()
                {
                    FlowKey = "AccountActivation",
                    Title = "Activation failures",
                    ScopeNote = "Prefer self-service resend; admin override only where current policy allows.",
                    AllowedAction = "Check SMTP readiness, then direct the user to resend activation from login or use the admin activation-support action.",
                    EscalationRule = "Do not silently bypass confirmation policy. If failures persist after resend, escalate as auth/communication troubleshooting."
                },
                new()
                {
                    FlowKey = "PasswordReset",
                    Title = "Password reset failures",
                    ScopeNote = "Reset support must stay identity-safe.",
                    AllowedAction = "Validate the requester first, then reissue password reset only after support validation and transport checks.",
                    EscalationRule = "Avoid repeated resets without user verification. Persistent failures should be escalated as communication or account-lifecycle issues."
                },
                new()
                {
                    FlowKey = "AdminCommunicationTest",
                    Title = "Communication test failures",
                    ScopeNote = "Use this only for operator-side transport validation.",
                    AllowedAction = "Check SMTP and test-inbox settings, then rerun the test email from the communication workspace after configuration is corrected.",
                    EscalationRule = "Do not send repeated test emails to production recipients. Keep tests scoped to the configured test inbox."
                }
            };
        }
    }
}
