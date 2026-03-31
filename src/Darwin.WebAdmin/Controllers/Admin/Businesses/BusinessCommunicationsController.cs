using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Businesses.Commands;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Businesses.Queries;
using Darwin.Application.Abstractions.Notifications;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Settings.DTOs;
using Darwin.Domain.Entities.Integration;
using Darwin.WebAdmin.Security;
using Darwin.WebAdmin.Services.Settings;
using Darwin.WebAdmin.ViewModels.Admin;
using Darwin.WebAdmin.ViewModels.Businesses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Darwin.WebAdmin.Controllers.Admin.Businesses
{
    /// <summary>
    /// Communication operations workspace for onboarding and support operators.
    /// </summary>
    [PermissionAuthorize(PermissionKeys.ManageBusinessSupport)]
    public sealed class BusinessCommunicationsController : AdminBaseController
    {
        private readonly GetBusinessCommunicationOpsSummaryHandler _getSummary;
        private readonly GetBusinessCommunicationSetupPageHandler _getSetupPage;
        private readonly GetBusinessCommunicationProfileHandler _getProfile;
        private readonly GetEmailDispatchAuditsPageHandler _getEmailDispatchAuditsPage;
        private readonly GetChannelDispatchActivityHandler _getChannelDispatchActivity;
        private readonly RetryEmailDispatchAuditHandler _retryEmailDispatchAudit;
        private readonly IAppDbContext _db;
        private readonly ISiteSettingCache _siteSettingCache;
        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;
        private readonly IWhatsAppSender _whatsAppSender;

        public BusinessCommunicationsController(
            GetBusinessCommunicationOpsSummaryHandler getSummary,
            GetBusinessCommunicationSetupPageHandler getSetupPage,
            GetBusinessCommunicationProfileHandler getProfile,
            GetEmailDispatchAuditsPageHandler getEmailDispatchAuditsPage,
            GetChannelDispatchActivityHandler getChannelDispatchActivity,
            RetryEmailDispatchAuditHandler retryEmailDispatchAudit,
            IAppDbContext db,
            ISiteSettingCache siteSettingCache,
            IEmailSender emailSender,
            ISmsSender smsSender,
            IWhatsAppSender whatsAppSender)
        {
            _getSummary = getSummary;
            _getSetupPage = getSetupPage;
            _getProfile = getProfile;
            _getEmailDispatchAuditsPage = getEmailDispatchAuditsPage;
            _getChannelDispatchActivity = getChannelDispatchActivity;
            _retryEmailDispatchAudit = retryEmailDispatchAudit;
            _db = db;
            _siteSettingCache = siteSettingCache;
            _emailSender = emailSender;
            _smsSender = smsSender;
            _whatsAppSender = whatsAppSender;
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
            var (emailAudits, _, _) = await _getEmailDispatchAuditsPage
                .HandleAsync(
                    page: 1,
                    pageSize: 10,
                    query: null,
                    recipientEmail: null,
                    status: null,
                    flowKey: null,
                    stalePendingOnly: false,
                    businessLinkedFailuresOnly: false,
                    repeatedFailuresOnly: false,
                    priorSuccessOnly: false,
                    retryReadyOnly: false,
                    retryBlockedOnly: false,
                    highChainVolumeOnly: false,
                    businessId: null,
                    ct: ct)
                .ConfigureAwait(false);
            var (channelAudits, channelAuditSummary) = await _getChannelDispatchActivity
                .HandleAsync(null, 8, ct)
                .ConfigureAwait(false);

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
                                       !string.IsNullOrWhiteSpace(settings.CommunicationTestInboxEmail),
                    CanSendTestSms = settings.SmsEnabled &&
                                     !string.IsNullOrWhiteSpace(settings.SmsProvider) &&
                                     !string.IsNullOrWhiteSpace(settings.SmsFromPhoneE164) &&
                                     !string.IsNullOrWhiteSpace(settings.CommunicationTestSmsRecipientE164),
                    CanSendTestWhatsApp = settings.WhatsAppEnabled &&
                                          !string.IsNullOrWhiteSpace(settings.WhatsAppBusinessPhoneId) &&
                                          !string.IsNullOrWhiteSpace(settings.WhatsAppAccessToken) &&
                                          !string.IsNullOrWhiteSpace(settings.CommunicationTestWhatsAppRecipientE164)
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
                TemplateInventory = BuildTemplateInventory(settings),
                CapabilityCoverage = BuildCapabilityCoverage(),
                ChannelOperations = BuildChannelOperations(settings),
                ResendPolicies = BuildResendPolicies(),
                ChannelAuditSummary = new ChannelDispatchAuditSummaryVm
                {
                    TotalCount = channelAuditSummary.TotalCount,
                    FailedCount = channelAuditSummary.FailedCount,
                    PendingCount = channelAuditSummary.PendingCount,
                    Recent24HourCount = channelAuditSummary.Recent24HourCount,
                    SmsCount = channelAuditSummary.SmsCount,
                    WhatsAppCount = channelAuditSummary.WhatsAppCount,
                    PhoneVerificationCount = channelAuditSummary.PhoneVerificationCount,
                    AdminTestCount = channelAuditSummary.AdminTestCount
                },
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
                    AttemptAgeMinutes = x.AttemptAgeMinutes,
                    CompletionLatencySeconds = x.CompletionLatencySeconds,
                    NeedsOperatorFollowUp = x.NeedsOperatorFollowUp,
                    Severity = x.Severity,
                    CanRetryNow = x.CanRetryNow,
                    RetryPolicyState = x.RetryPolicyState,
                    RetryBlockedReason = x.RetryBlockedReason,
                    RetryAvailableAtUtc = x.RetryAvailableAtUtc,
                    RecentAttemptCount24h = x.RecentAttemptCount24h,
                    ChainStartedAtUtc = x.ChainStartedAtUtc,
                    ChainLastAttemptAtUtc = x.ChainLastAttemptAtUtc,
                    ChainSpanHours = x.ChainSpanHours,
                    ChainStatusMix = x.ChainStatusMix,
                    PriorAttemptCount = x.PriorAttemptCount,
                    PriorFailureCount = x.PriorFailureCount,
                    LastSuccessfulAttemptAtUtc = x.LastSuccessfulAttemptAtUtc,
                    RecommendedAction = BuildAuditRecommendedAction(x)
                }).ToList(),
                RecentChannelAudits = channelAudits.Select(x => new ChannelDispatchAuditListItemVm
                {
                    Id = x.Id,
                    Channel = x.Channel,
                    Provider = x.Provider,
                    FlowKey = x.FlowKey,
                    BusinessId = x.BusinessId,
                    RecipientAddress = x.RecipientAddress,
                    MessagePreview = x.MessagePreview,
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
            var (recentAudits, _, _) = await _getEmailDispatchAuditsPage
                .HandleAsync(
                    page: 1,
                    pageSize: 5,
                    query: null,
                    recipientEmail: null,
                    status: null,
                    flowKey: null,
                    stalePendingOnly: false,
                    businessLinkedFailuresOnly: false,
                    repeatedFailuresOnly: false,
                    priorSuccessOnly: false,
                    retryReadyOnly: false,
                    retryBlockedOnly: false,
                    highChainVolumeOnly: false,
                    businessId: businessId,
                    ct: ct)
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
                CanSendTestEmail = emailTransportConfigured && testInboxConfigured,
                CanSendTestSms = smsTransportConfigured && !string.IsNullOrWhiteSpace(settings.CommunicationTestSmsRecipientE164),
                CanSendTestWhatsApp = whatsAppTransportConfigured && !string.IsNullOrWhiteSpace(settings.CommunicationTestWhatsAppRecipientE164),
                TransactionalSubjectPrefix = settings.TransactionalEmailSubjectPrefix,
                TestInboxEmail = settings.CommunicationTestInboxEmail,
                ActiveFlowNames = BuildActiveFlowNames(profile),
                TemplateInventory = BuildTemplateInventory(settings),
                ChannelOperations = BuildChannelOperations(settings),
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
                    AttemptAgeMinutes = x.AttemptAgeMinutes,
                    CompletionLatencySeconds = x.CompletionLatencySeconds,
                    NeedsOperatorFollowUp = x.NeedsOperatorFollowUp,
                    Severity = x.Severity,
                    CanRetryNow = x.CanRetryNow,
                    RetryPolicyState = x.RetryPolicyState,
                    RetryBlockedReason = x.RetryBlockedReason,
                    RetryAvailableAtUtc = x.RetryAvailableAtUtc,
                    RecentAttemptCount24h = x.RecentAttemptCount24h,
                    ChainStartedAtUtc = x.ChainStartedAtUtc,
                    ChainLastAttemptAtUtc = x.ChainLastAttemptAtUtc,
                    ChainSpanHours = x.ChainSpanHours,
                    ChainStatusMix = x.ChainStatusMix,
                    PriorAttemptCount = x.PriorAttemptCount,
                    PriorFailureCount = x.PriorFailureCount,
                    LastSuccessfulAttemptAtUtc = x.LastSuccessfulAttemptAtUtc,
                    RecommendedAction = BuildAuditRecommendedAction(x)
                }).ToList()
            };

            var (channelAudits, channelAuditSummary) = await _getChannelDispatchActivity
                .HandleAsync(businessId, 6, ct)
                .ConfigureAwait(false);
            vm.ChannelAuditSummary = new ChannelDispatchAuditSummaryVm
            {
                TotalCount = channelAuditSummary.TotalCount,
                FailedCount = channelAuditSummary.FailedCount,
                PendingCount = channelAuditSummary.PendingCount,
                Recent24HourCount = channelAuditSummary.Recent24HourCount,
                SmsCount = channelAuditSummary.SmsCount,
                WhatsAppCount = channelAuditSummary.WhatsAppCount,
                PhoneVerificationCount = channelAuditSummary.PhoneVerificationCount,
                AdminTestCount = channelAuditSummary.AdminTestCount
            };
            vm.RecentChannelAudits = channelAudits.Select(x => new ChannelDispatchAuditListItemVm
            {
                Id = x.Id,
                Channel = x.Channel,
                Provider = x.Provider,
                FlowKey = x.FlowKey,
                BusinessId = x.BusinessId,
                RecipientAddress = x.RecipientAddress,
                MessagePreview = x.MessagePreview,
                Status = x.Status,
                AttemptedAtUtc = x.AttemptedAtUtc,
                CompletedAtUtc = x.CompletedAtUtc,
                FailureMessage = x.FailureMessage
            }).ToList();

            return RenderCommunicationProfileWorkspace(vm);
        }

        [HttpGet]
        public async Task<IActionResult> EmailAudits(
            int page = 1,
            int pageSize = 20,
            string? query = null,
            string? recipientEmail = null,
            string? status = null,
            string? flowKey = null,
            bool stalePendingOnly = false,
            bool businessLinkedFailuresOnly = false,
            bool repeatedFailuresOnly = false,
            bool priorSuccessOnly = false,
            bool retryReadyOnly = false,
            bool retryBlockedOnly = false,
            bool highChainVolumeOnly = false,
            bool chainFollowUpOnly = false,
            bool chainResolvedOnly = false,
            Guid? businessId = null,
            CancellationToken ct = default)
        {
            var (items, total, chainSummary) = await _getEmailDispatchAuditsPage
                .HandleAsync(
                    page: page,
                    pageSize: pageSize,
                    query: query,
                    recipientEmail: recipientEmail,
                    status: status,
                    flowKey: flowKey,
                    stalePendingOnly: stalePendingOnly,
                    businessLinkedFailuresOnly: businessLinkedFailuresOnly,
                    repeatedFailuresOnly: repeatedFailuresOnly,
                    priorSuccessOnly: priorSuccessOnly,
                    retryReadyOnly: retryReadyOnly,
                    retryBlockedOnly: retryBlockedOnly,
                    highChainVolumeOnly: highChainVolumeOnly,
                    chainFollowUpOnly: chainFollowUpOnly,
                    chainResolvedOnly: chainResolvedOnly,
                    businessId: businessId,
                    ct: ct)
                .ConfigureAwait(false);
            var summary = await _getEmailDispatchAuditsPage.GetSummaryAsync(businessId, ct).ConfigureAwait(false);

            var vm = new EmailDispatchAuditsListVm
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = query ?? string.Empty,
                RecipientEmail = recipientEmail ?? string.Empty,
                Status = status ?? string.Empty,
                FlowKey = flowKey ?? string.Empty,
                StalePendingOnly = stalePendingOnly,
                BusinessLinkedFailuresOnly = businessLinkedFailuresOnly,
                RepeatedFailuresOnly = repeatedFailuresOnly,
                PriorSuccessOnly = priorSuccessOnly,
                RetryReadyOnly = retryReadyOnly,
                RetryBlockedOnly = retryBlockedOnly,
                HighChainVolumeOnly = highChainVolumeOnly,
                ChainFollowUpOnly = chainFollowUpOnly,
                ChainResolvedOnly = chainResolvedOnly,
                BusinessId = businessId,
                CanSendTestEmail = await CanSendTestEmailAsync(ct).ConfigureAwait(false),
                Summary = new EmailDispatchAuditSummaryVm
                {
                    TotalCount = summary.TotalCount,
                    FailedCount = summary.FailedCount,
                    SentCount = summary.SentCount,
                    PendingCount = summary.PendingCount,
                    StalePendingCount = summary.StalePendingCount,
                    BusinessLinkedFailureCount = summary.BusinessLinkedFailureCount,
                    Recent24HourCount = summary.Recent24HourCount,
                    FailedInvitationCount = summary.FailedInvitationCount,
                    FailedActivationCount = summary.FailedActivationCount,
                    FailedPasswordResetCount = summary.FailedPasswordResetCount,
                    FailedAdminTestCount = summary.FailedAdminTestCount,
                    NeedsOperatorFollowUpCount = summary.NeedsOperatorFollowUpCount,
                    SlowCompletedCount = summary.SlowCompletedCount,
                    RetriedFlowCount = summary.RetriedFlowCount,
                    PriorSuccessContextCount = summary.PriorSuccessContextCount,
                    RepeatedFailureCount = summary.RepeatedFailureCount,
                    RetryReadyCount = summary.RetryReadyCount,
                    RetryBlockedCount = summary.RetryBlockedCount,
                    HighChainVolumeCount = summary.HighChainVolumeCount
                },
                ChainSummary = chainSummary == null ? null : new EmailDispatchAuditChainSummaryVm
                {
                    TotalAttempts = chainSummary.TotalAttempts,
                    FailedCount = chainSummary.FailedCount,
                    SentCount = chainSummary.SentCount,
                    PendingCount = chainSummary.PendingCount,
                    NeedsOperatorFollowUpCount = chainSummary.NeedsOperatorFollowUpCount,
                    FirstAttemptAtUtc = chainSummary.FirstAttemptAtUtc,
                    LastAttemptAtUtc = chainSummary.LastAttemptAtUtc,
                    LastSuccessfulAttemptAtUtc = chainSummary.LastSuccessfulAttemptAtUtc,
                    StatusMix = chainSummary.StatusMix,
                    RecentHistory = chainSummary.RecentHistory.Select(x => new EmailDispatchAuditChainHistoryItemVm
                    {
                        AttemptedAtUtc = x.AttemptedAtUtc,
                        Status = x.Status,
                        Provider = x.Provider,
                        Subject = x.Subject,
                        FailureMessage = x.FailureMessage,
                        CompletedAtUtc = x.CompletedAtUtc
                    }).ToList()
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
                    AttemptAgeMinutes = x.AttemptAgeMinutes,
                    CompletionLatencySeconds = x.CompletionLatencySeconds,
                    NeedsOperatorFollowUp = x.NeedsOperatorFollowUp,
                    Severity = x.Severity,
                    CanRetryNow = x.CanRetryNow,
                    RetryPolicyState = x.RetryPolicyState,
                    RetryBlockedReason = x.RetryBlockedReason,
                    RetryAvailableAtUtc = x.RetryAvailableAtUtc,
                    RecentAttemptCount24h = x.RecentAttemptCount24h,
                    ChainStartedAtUtc = x.ChainStartedAtUtc,
                    ChainLastAttemptAtUtc = x.ChainLastAttemptAtUtc,
                    ChainSpanHours = x.ChainSpanHours,
                    ChainStatusMix = x.ChainStatusMix,
                    PriorAttemptCount = x.PriorAttemptCount,
                    PriorFailureCount = x.PriorFailureCount,
                    LastSuccessfulAttemptAtUtc = x.LastSuccessfulAttemptAtUtc,
                    RecommendedAction = BuildAuditRecommendedAction(x)
                }).ToList()
            };

            return RenderEmailAuditsWorkspace(vm);
        }

        [HttpGet]
        public async Task<IActionResult> ChannelAudits(
            int page = 1,
            int pageSize = 20,
            string? query = null,
            string? recipientAddress = null,
            string? channel = null,
            string? flowKey = null,
            string? status = null,
            bool failedOnly = false,
            bool phoneVerificationOnly = false,
            bool adminTestOnly = false,
            bool repeatedFailuresOnly = false,
            bool priorSuccessOnly = false,
            bool actionReadyOnly = false,
            bool actionBlockedOnly = false,
            bool chainFollowUpOnly = false,
            bool chainResolvedOnly = false,
            Guid? businessId = null,
            CancellationToken ct = default)
        {
            var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);
            var filter = new ChannelDispatchAuditFilterDto
            {
                Query = query ?? string.Empty,
                RecipientAddress = recipientAddress ?? string.Empty,
                Channel = channel ?? string.Empty,
                FlowKey = flowKey ?? string.Empty,
                Status = status ?? string.Empty,
                BusinessId = businessId,
                FailedOnly = failedOnly,
                PhoneVerificationOnly = phoneVerificationOnly,
                AdminTestOnly = adminTestOnly,
                RepeatedFailuresOnly = repeatedFailuresOnly,
                PriorSuccessOnly = priorSuccessOnly,
                ActionReadyOnly = actionReadyOnly,
                ActionBlockedOnly = actionBlockedOnly,
                ChainFollowUpOnly = chainFollowUpOnly,
                ChainResolvedOnly = chainResolvedOnly
            };

            var (items, total, summary, chainSummary) = await _getChannelDispatchActivity
                .HandlePageAsync(page, pageSize, filter, ct)
                .ConfigureAwait(false);

            var vm = new ChannelDispatchAuditsListVm
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = filter.Query,
                RecipientAddress = filter.RecipientAddress,
                Channel = filter.Channel,
                FlowKey = filter.FlowKey,
                Status = filter.Status,
                BusinessId = businessId,
                FailedOnly = failedOnly,
                PhoneVerificationOnly = phoneVerificationOnly,
                AdminTestOnly = adminTestOnly,
                RepeatedFailuresOnly = repeatedFailuresOnly,
                PriorSuccessOnly = priorSuccessOnly,
                ActionReadyOnly = actionReadyOnly,
                ActionBlockedOnly = actionBlockedOnly,
                ChainFollowUpOnly = chainFollowUpOnly,
                ChainResolvedOnly = chainResolvedOnly,
                CanSendTestSms = settings.SmsEnabled &&
                                 !string.IsNullOrWhiteSpace(settings.SmsProvider) &&
                                 !string.IsNullOrWhiteSpace(settings.SmsFromPhoneE164) &&
                                 !string.IsNullOrWhiteSpace(settings.CommunicationTestSmsRecipientE164),
                CanSendTestWhatsApp = settings.WhatsAppEnabled &&
                                      !string.IsNullOrWhiteSpace(settings.WhatsAppBusinessPhoneId) &&
                                      !string.IsNullOrWhiteSpace(settings.WhatsAppAccessToken) &&
                                      !string.IsNullOrWhiteSpace(settings.CommunicationTestWhatsAppRecipientE164),
                Summary = new ChannelDispatchAuditSummaryVm
                {
                    TotalCount = summary.TotalCount,
                    FailedCount = summary.FailedCount,
                    PendingCount = summary.PendingCount,
                    Recent24HourCount = summary.Recent24HourCount,
                    SmsCount = summary.SmsCount,
                    WhatsAppCount = summary.WhatsAppCount,
                    PhoneVerificationCount = summary.PhoneVerificationCount,
                    AdminTestCount = summary.AdminTestCount,
                    RepeatedFailureCount = summary.RepeatedFailureCount,
                    PriorSuccessContextCount = summary.PriorSuccessContextCount,
                    ActionReadyCount = summary.ActionReadyCount,
                    ActionBlockedCount = summary.ActionBlockedCount
                },
                ChainSummary = chainSummary == null ? null : new ChannelDispatchAuditChainSummaryVm
                {
                    TotalAttempts = chainSummary.TotalAttempts,
                    FailedCount = chainSummary.FailedCount,
                    SentCount = chainSummary.SentCount,
                    PendingCount = chainSummary.PendingCount,
                    NeedsOperatorFollowUpCount = chainSummary.NeedsOperatorFollowUpCount,
                    FirstAttemptAtUtc = chainSummary.FirstAttemptAtUtc,
                    LastAttemptAtUtc = chainSummary.LastAttemptAtUtc,
                    StatusMix = chainSummary.StatusMix,
                    RecentHistory = chainSummary.RecentHistory.Select(x => new ChannelDispatchAuditChainHistoryItemVm
                    {
                        AttemptedAtUtc = x.AttemptedAtUtc,
                        Channel = x.Channel,
                        Status = x.Status,
                        Provider = x.Provider,
                        MessagePreview = x.MessagePreview,
                        FailureMessage = x.FailureMessage,
                        CompletedAtUtc = x.CompletedAtUtc
                    }).ToList()
                },
                Items = items.Select(x => new ChannelDispatchAuditListItemVm
                {
                    Id = x.Id,
                    Channel = x.Channel,
                    Provider = x.Provider,
                    FlowKey = x.FlowKey,
                    BusinessId = x.BusinessId,
                    RecipientAddress = x.RecipientAddress,
                    MessagePreview = x.MessagePreview,
                    Status = x.Status,
                    AttemptedAtUtc = x.AttemptedAtUtc,
                    CompletedAtUtc = x.CompletedAtUtc,
                    FailureMessage = x.FailureMessage,
                    NeedsOperatorFollowUp = x.NeedsOperatorFollowUp,
                    PriorAttemptCount = x.PriorAttemptCount,
                    PriorFailureCount = x.PriorFailureCount,
                    LastSuccessfulAttemptAtUtc = x.LastSuccessfulAttemptAtUtc,
                    CanRerunNow = x.CanRerunNow,
                    ActionPolicyState = x.ActionPolicyState,
                    ActionBlockedReason = x.ActionBlockedReason,
                    ActionAvailableAtUtc = x.ActionAvailableAtUtc
                }).ToList(),
                PageSizeItems = BuildPageSizeItems(pageSize),
                ChannelItems = BuildChannelItems(channel),
                FlowItems = BuildChannelFlowItems(flowKey),
                StatusItems = BuildAuditStatusItems(status)
            };

            return RenderChannelAuditsWorkspace(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RetryEmailAudit(
            Guid id,
            int page = 1,
            int pageSize = 20,
            string? query = null,
            string? recipientEmail = null,
            string? status = null,
            string? flowKey = null,
            bool stalePendingOnly = false,
            bool businessLinkedFailuresOnly = false,
            bool repeatedFailuresOnly = false,
            bool priorSuccessOnly = false,
            bool retryReadyOnly = false,
            bool retryBlockedOnly = false,
            bool highChainVolumeOnly = false,
            bool chainFollowUpOnly = false,
            bool chainResolvedOnly = false,
            Guid? businessId = null,
            CancellationToken ct = default)
        {
            var result = await _retryEmailDispatchAudit
                .HandleAsync(new RetryEmailDispatchAuditDto { AuditId = id }, ct)
                .ConfigureAwait(false);

            if (result.Succeeded)
            {
                TempData["Success"] = "Email flow retried successfully using the current live target.";
            }
            else
            {
                TempData["Error"] = result.Error ?? "Email retry failed.";
            }

            return RedirectOrHtmx(
                nameof(EmailAudits),
                new
                {
                    page,
                    pageSize,
                    query,
                    recipientEmail,
                    status,
                    flowKey,
                    stalePendingOnly,
                    businessLinkedFailuresOnly,
                    repeatedFailuresOnly,
                    priorSuccessOnly,
                    retryReadyOnly,
                    retryBlockedOnly,
                    highChainVolumeOnly,
                    chainFollowUpOnly,
                    chainResolvedOnly,
                    businessId
                });
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

            var requestedBy = User?.Identity?.Name ?? "WebAdmin operator";
            var placeholders = BuildCommunicationTestPlaceholders(
                channel: "Email",
                requestedBy: requestedBy,
                attemptedAtUtc: DateTime.UtcNow,
                testTarget: settings.CommunicationTestInboxEmail,
                transportState: emailTransportConfigured ? "Ready" : "Not ready");
            var prefix = string.IsNullOrWhiteSpace(settings.TransactionalEmailSubjectPrefix)
                ? string.Empty
                : $"{settings.TransactionalEmailSubjectPrefix.Trim()} ";
            var subject = prefix + RenderTemplate(
                settings.CommunicationTestEmailSubjectTemplate,
                "Darwin communication test for {channel}",
                placeholders);
            var htmlBody = RenderTemplate(
                settings.CommunicationTestEmailBodyTemplate,
                "<p>This is a Darwin {channel} communication test.</p><ul><li><strong>Requested by:</strong> {requested_by}</li><li><strong>Attempted at (UTC):</strong> {attempted_at_utc}</li><li><strong>Target:</strong> {test_target}</li><li><strong>Transport state:</strong> {transport_state}</li></ul><p>This diagnostic is intended only for the configured communication test target.</p>",
                placeholders);

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendTestSms(
            bool returnToChannelAudits = false,
            int page = 1,
            int pageSize = 20,
            string? query = null,
            string? recipientAddress = null,
            string? channel = null,
            string? flowKey = null,
            string? status = null,
            bool failedOnly = false,
            bool phoneVerificationOnly = false,
            bool adminTestOnly = false,
            bool repeatedFailuresOnly = false,
            bool priorSuccessOnly = false,
            bool actionReadyOnly = false,
            bool actionBlockedOnly = false,
            bool chainFollowUpOnly = false,
            bool chainResolvedOnly = false,
            Guid? businessId = null,
            CancellationToken ct = default)
        {
            var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);
            var smsTransportConfigured = settings.SmsEnabled &&
                                         !string.IsNullOrWhiteSpace(settings.SmsProvider) &&
                                         !string.IsNullOrWhiteSpace(settings.SmsFromPhoneE164);

            if (!smsTransportConfigured)
            {
                TempData["Error"] = "SMS transport is not ready. Complete SMS provider settings before sending a test SMS.";
                return RedirectToChannelAuditsOrIndex(
                    returnToChannelAudits,
                    page,
                    pageSize,
                    query,
                    recipientAddress,
                    channel,
                    flowKey,
                    status,
                    failedOnly,
                    phoneVerificationOnly,
                    adminTestOnly,
                    repeatedFailuresOnly,
                    priorSuccessOnly,
                    actionReadyOnly,
                    actionBlockedOnly,
                    chainFollowUpOnly,
                    chainResolvedOnly,
                    businessId);
            }

            if (string.IsNullOrWhiteSpace(settings.CommunicationTestSmsRecipientE164))
            {
                TempData["Error"] = "Communication test SMS recipient is not configured.";
                return RedirectToChannelAuditsOrIndex(
                    returnToChannelAudits,
                    page,
                    pageSize,
                    query,
                    recipientAddress,
                    channel,
                    flowKey,
                    status,
                    failedOnly,
                    phoneVerificationOnly,
                    adminTestOnly,
                    repeatedFailuresOnly,
                    priorSuccessOnly,
                    actionReadyOnly,
                    actionBlockedOnly,
                    chainFollowUpOnly,
                    chainResolvedOnly,
                    businessId);
            }

            var smsCooldownUntilUtc = await GetChannelTestCooldownUntilUtcAsync(
                "SMS",
                settings.CommunicationTestSmsRecipientE164,
                ct).ConfigureAwait(false);
            if (smsCooldownUntilUtc.HasValue)
            {
                TempData["Error"] = $"SMS test rerun is cooling down. Try again after {smsCooldownUntilUtc.Value:yyyy-MM-dd HH:mm} UTC.";
                return RedirectToChannelAuditsOrIndex(
                    returnToChannelAudits,
                    page,
                    pageSize,
                    query,
                    recipientAddress,
                    channel,
                    flowKey,
                    status,
                    failedOnly,
                    phoneVerificationOnly,
                    adminTestOnly,
                    repeatedFailuresOnly,
                    priorSuccessOnly,
                    actionReadyOnly,
                    actionBlockedOnly,
                    chainFollowUpOnly,
                    chainResolvedOnly,
                    businessId);
            }

            var requestedBy = User?.Identity?.Name ?? "WebAdmin operator";
            var text = RenderTemplate(
                settings.CommunicationTestSmsTemplate,
                "Darwin SMS transport test requested by {requested_by} at {attempted_at_utc} UTC for {test_target}.",
                BuildCommunicationTestPlaceholders(
                    channel: "SMS",
                    requestedBy: requestedBy,
                    attemptedAtUtc: DateTime.UtcNow,
                    testTarget: settings.CommunicationTestSmsRecipientE164,
                    transportState: smsTransportConfigured ? "Ready" : "Not ready"));
            await _smsSender.SendAsync(
                settings.CommunicationTestSmsRecipientE164,
                text,
                ct,
                new ChannelDispatchContext
                {
                    FlowKey = "AdminCommunicationTest"
                }).ConfigureAwait(false);

            TempData["Success"] = $"Communication test SMS sent to {settings.CommunicationTestSmsRecipientE164}.";
            return RedirectToChannelAuditsOrIndex(
                returnToChannelAudits,
                page,
                pageSize,
                query,
                recipientAddress,
                channel,
                flowKey,
                status,
                failedOnly,
                phoneVerificationOnly,
                adminTestOnly,
                repeatedFailuresOnly,
                priorSuccessOnly,
                actionReadyOnly,
                actionBlockedOnly,
                chainFollowUpOnly,
                chainResolvedOnly,
                businessId);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendTestWhatsApp(
            bool returnToChannelAudits = false,
            int page = 1,
            int pageSize = 20,
            string? query = null,
            string? recipientAddress = null,
            string? channel = null,
            string? flowKey = null,
            string? status = null,
            bool failedOnly = false,
            bool phoneVerificationOnly = false,
            bool adminTestOnly = false,
            bool repeatedFailuresOnly = false,
            bool priorSuccessOnly = false,
            bool actionReadyOnly = false,
            bool actionBlockedOnly = false,
            bool chainFollowUpOnly = false,
            bool chainResolvedOnly = false,
            Guid? businessId = null,
            CancellationToken ct = default)
        {
            var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);
            var whatsAppTransportConfigured = settings.WhatsAppEnabled &&
                                              !string.IsNullOrWhiteSpace(settings.WhatsAppBusinessPhoneId) &&
                                              !string.IsNullOrWhiteSpace(settings.WhatsAppAccessToken);

            if (!whatsAppTransportConfigured)
            {
                TempData["Error"] = "WhatsApp transport is not ready. Complete WhatsApp provider settings before sending a test message.";
                return RedirectToChannelAuditsOrIndex(
                    returnToChannelAudits,
                    page,
                    pageSize,
                    query,
                    recipientAddress,
                    channel,
                    flowKey,
                    status,
                    failedOnly,
                    phoneVerificationOnly,
                    adminTestOnly,
                    repeatedFailuresOnly,
                    priorSuccessOnly,
                    actionReadyOnly,
                    actionBlockedOnly,
                    chainFollowUpOnly,
                    chainResolvedOnly,
                    businessId);
            }

            if (string.IsNullOrWhiteSpace(settings.CommunicationTestWhatsAppRecipientE164))
            {
                TempData["Error"] = "Communication test WhatsApp recipient is not configured.";
                return RedirectToChannelAuditsOrIndex(
                    returnToChannelAudits,
                    page,
                    pageSize,
                    query,
                    recipientAddress,
                    channel,
                    flowKey,
                    status,
                    failedOnly,
                    phoneVerificationOnly,
                    adminTestOnly,
                    repeatedFailuresOnly,
                    priorSuccessOnly,
                    actionReadyOnly,
                    actionBlockedOnly,
                    chainFollowUpOnly,
                    chainResolvedOnly,
                    businessId);
            }

            var whatsAppCooldownUntilUtc = await GetChannelTestCooldownUntilUtcAsync(
                "WhatsApp",
                settings.CommunicationTestWhatsAppRecipientE164,
                ct).ConfigureAwait(false);
            if (whatsAppCooldownUntilUtc.HasValue)
            {
                TempData["Error"] = $"WhatsApp test rerun is cooling down. Try again after {whatsAppCooldownUntilUtc.Value:yyyy-MM-dd HH:mm} UTC.";
                return RedirectToChannelAuditsOrIndex(
                    returnToChannelAudits,
                    page,
                    pageSize,
                    query,
                    recipientAddress,
                    channel,
                    flowKey,
                    status,
                    failedOnly,
                    phoneVerificationOnly,
                    adminTestOnly,
                    repeatedFailuresOnly,
                    priorSuccessOnly,
                    actionReadyOnly,
                    actionBlockedOnly,
                    chainFollowUpOnly,
                    chainResolvedOnly,
                    businessId);
            }

            var requestedBy = User?.Identity?.Name ?? "WebAdmin operator";
            var text = RenderTemplate(
                settings.CommunicationTestWhatsAppTemplate,
                "Darwin WhatsApp transport test requested by {requested_by} at {attempted_at_utc} UTC for {test_target}.",
                BuildCommunicationTestPlaceholders(
                    channel: "WhatsApp",
                    requestedBy: requestedBy,
                    attemptedAtUtc: DateTime.UtcNow,
                    testTarget: settings.CommunicationTestWhatsAppRecipientE164,
                    transportState: whatsAppTransportConfigured ? "Ready" : "Not ready"));
            await _whatsAppSender.SendTextAsync(
                settings.CommunicationTestWhatsAppRecipientE164,
                text,
                ct,
                new ChannelDispatchContext
                {
                    FlowKey = "AdminCommunicationTest"
                }).ConfigureAwait(false);

            TempData["Success"] = $"Communication test WhatsApp message sent to {settings.CommunicationTestWhatsAppRecipientE164}.";
            return RedirectToChannelAuditsOrIndex(
                returnToChannelAudits,
                page,
                pageSize,
                query,
                recipientAddress,
                channel,
                flowKey,
                status,
                failedOnly,
                phoneVerificationOnly,
                adminTestOnly,
                repeatedFailuresOnly,
                priorSuccessOnly,
                actionReadyOnly,
                actionBlockedOnly,
                chainFollowUpOnly,
                chainResolvedOnly,
                businessId);
        }

        private IActionResult RedirectToChannelAuditsOrIndex(
            bool returnToChannelAudits,
            int page,
            int pageSize,
            string? query,
            string? recipientAddress,
            string? channel,
            string? flowKey,
            string? status,
            bool failedOnly,
            bool phoneVerificationOnly,
            bool adminTestOnly,
            bool repeatedFailuresOnly,
            bool priorSuccessOnly,
            bool actionReadyOnly,
            bool actionBlockedOnly,
            bool chainFollowUpOnly,
            bool chainResolvedOnly,
            Guid? businessId)
        {
            if (!returnToChannelAudits)
            {
                return RedirectOrHtmx(nameof(Index), new { });
            }

            return RedirectOrHtmx(
                nameof(ChannelAudits),
                new
                {
                    page,
                    pageSize,
                    query,
                    recipientAddress,
                    channel,
                    flowKey,
                    status,
                    failedOnly,
                    phoneVerificationOnly,
                    adminTestOnly,
                    repeatedFailuresOnly,
                    priorSuccessOnly,
                    actionReadyOnly,
                    actionBlockedOnly,
                    chainFollowUpOnly,
                    chainResolvedOnly,
                    businessId
                });
        }

        private async Task<DateTime?> GetChannelTestCooldownUntilUtcAsync(
            string channel,
            string recipientAddress,
            CancellationToken ct)
        {
            var latestAttemptAtUtc = await _db.Set<ChannelDispatchAudit>()
                .AsNoTracking()
                .Where(x =>
                    x.FlowKey == "AdminCommunicationTest" &&
                    x.Channel == channel &&
                    x.RecipientAddress == recipientAddress)
                .OrderByDescending(x => x.AttemptedAtUtc)
                .Select(x => (DateTime?)x.AttemptedAtUtc)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            if (!latestAttemptAtUtc.HasValue)
            {
                return null;
            }

            var cooldownUntilUtc = latestAttemptAtUtc.Value.AddMinutes(5);
            return cooldownUntilUtc > DateTime.UtcNow ? cooldownUntilUtc : null;
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

        private IActionResult RenderChannelAuditsWorkspace(ChannelDispatchAuditsListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/BusinessCommunications/ChannelAudits.cshtml", vm);
            }

            return View("ChannelAudits", vm);
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

        private async Task<bool> CanSendTestEmailAsync(CancellationToken ct)
        {
            var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);
            return settings.SmtpEnabled &&
                   !string.IsNullOrWhiteSpace(settings.SmtpHost) &&
                   settings.SmtpPort.HasValue &&
                   !string.IsNullOrWhiteSpace(settings.SmtpFromAddress) &&
                   !string.IsNullOrWhiteSpace(settings.CommunicationTestInboxEmail);
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

        private static IEnumerable<SelectListItem> BuildChannelItems(string? selectedChannel)
        {
            yield return new SelectListItem("All channels", string.Empty, string.IsNullOrWhiteSpace(selectedChannel));
            yield return new SelectListItem("SMS", "SMS", string.Equals(selectedChannel, "SMS", StringComparison.OrdinalIgnoreCase));
            yield return new SelectListItem("WhatsApp", "WhatsApp", string.Equals(selectedChannel, "WhatsApp", StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<SelectListItem> BuildChannelFlowItems(string? selectedFlowKey)
        {
            yield return new SelectListItem("All channel flows", string.Empty, string.IsNullOrWhiteSpace(selectedFlowKey));
            yield return new SelectListItem("Phone Verification", "PhoneVerification", string.Equals(selectedFlowKey, "PhoneVerification", StringComparison.OrdinalIgnoreCase));
            yield return new SelectListItem("Admin Communication Test", "AdminCommunicationTest", string.Equals(selectedFlowKey, "AdminCommunicationTest", StringComparison.OrdinalIgnoreCase));
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
                    Name = "Phone Verification",
                    Channel = "SMS / WhatsApp",
                    Trigger = "Current user requests mobile verification",
                    DeliveryPath = "Twilio SMS or WhatsApp Cloud API via provider-backed senders",
                    CurrentImplementationStatus = "Live, token-based mobile verification with customizable text templates",
                    NextStep = "Extend into richer delivery history and channel policy analytics"
                },
                new()
                {
                    Name = "Admin Communication Test",
                    Channel = "Email / SMS / WhatsApp",
                    Trigger = "Manual operator validation from WebAdmin",
                    DeliveryPath = "Configured channel transport to operator test targets only",
                    CurrentImplementationStatus = "Live, operator-safe diagnostic actions for configured channels",
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
                    DeliveryPath = "Provider-backed test send actions from Business Communications",
                    CurrentImplementationStatus = "Live, operator-safe transport validation with configured test recipients",
                    NextStep = "Extend from transport validation into richer multi-channel delivery history"
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
                    OperatorVisibility = "Workspace and business profile now show configured subject/body previews, supported tokens, and direct policy handoffs",
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
                    CurrentState = "Controlled generic retry now exists for invitation, activation, and password-reset audit rows after safe target resolution",
                    OperatorVisibility = "Operators can retry supported live flows from the failed/stale audit queue and still fall back to flow-specific support surfaces",
                    NextStep = "Keep generic retry constrained to resolvable live flows until delivery logs and richer replay safeguards exist"
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
                    NextStep = "Keep expanding from transport tests into richer multi-channel delivery history"
                }
            };
        }

        private static List<CommunicationChannelOpsVm> BuildChannelOperations(SiteSettingDto settings)
        {
            var emailReady = settings.SmtpEnabled &&
                             !string.IsNullOrWhiteSpace(settings.SmtpHost) &&
                             settings.SmtpPort.HasValue &&
                             !string.IsNullOrWhiteSpace(settings.SmtpFromAddress);
            var smsReady = settings.SmsEnabled &&
                           !string.IsNullOrWhiteSpace(settings.SmsProvider) &&
                           !string.IsNullOrWhiteSpace(settings.SmsFromPhoneE164);
            var whatsAppReady = settings.WhatsAppEnabled &&
                                !string.IsNullOrWhiteSpace(settings.WhatsAppBusinessPhoneId) &&
                                !string.IsNullOrWhiteSpace(settings.WhatsAppAccessToken);

            return new List<CommunicationChannelOpsVm>
            {
                new()
                {
                    Channel = "Email",
                    CurrentState = emailReady ? "Live transport and live transactional flows" : "Transport not ready",
                    LiveFlows = "Business invitation, account activation, password reset, admin communication test",
                    SafeOperatorActions = emailReady
                        ? "Send test email, review failed audits, controlled retry for supported live flows"
                        : "Complete SMTP and test-inbox settings before relying on email follow-up",
                    RiskBoundary = "Do not treat the audit queue as a blind replay engine; only supported flows can retry safely.",
                    NextStep = "Move deeper into richer delivery-log and template CRUD."
                },
                new()
                {
                    Channel = "SMS",
                    CurrentState = smsReady ? "Provider-backed transport is live" : "Transport not ready",
                    LiveFlows = "Phone verification, operator test SMS",
                    SafeOperatorActions = smsReady
                        ? "Send test SMS and use canonical phone-verification flow"
                        : "Finish provider credentials and test-recipient setup",
                    RiskBoundary = settings.PhoneVerificationAllowFallback
                        ? $"SMS is not yet a generic notification bus; it is currently verification-first and may be used as fallback when {settings.PhoneVerificationPreferredChannel ?? "Sms"} is unavailable."
                        : "SMS is not yet a generic notification bus; live use is currently verification-first, not broad campaign or alert fan-out.",
                    NextStep = "Add broader delivery history and channel-policy visibility when multi-channel delivery logs deepen."
                },
                new()
                {
                    Channel = "WhatsApp",
                    CurrentState = whatsAppReady ? "Provider-backed transport is live" : "Transport not ready",
                    LiveFlows = "Phone verification, operator test WhatsApp",
                    SafeOperatorActions = whatsAppReady
                        ? "Send test WhatsApp and use canonical phone-verification flow"
                        : "Finish WhatsApp Cloud API credentials and test-recipient setup",
                    RiskBoundary = settings.PhoneVerificationAllowFallback
                        ? $"WhatsApp is intentionally narrow today: safe verification and operator transport validation, with fallback allowed when {settings.PhoneVerificationPreferredChannel ?? "Sms"} cannot be used."
                        : "WhatsApp is intentionally narrow today: safe verification and operator transport validation, not a generic alert/replay surface.",
                    NextStep = "Expand into broader delivery telemetry only after message-policy and history depth are stronger."
                }
            };
        }

        private static List<CommunicationTemplateInventoryVm> BuildTemplateInventory(SiteSettingDto settings)
        {
            return new List<CommunicationTemplateInventoryVm>
            {
                new()
                {
                    FlowName = "Business Invitation",
                    TemplateSurface = "Site-setting template fields feeding the invitation handler",
                    SubjectSource = "Transactional subject prefix + BusinessInvitationEmailSubjectTemplate",
                    BodySource = "BusinessInvitationEmailBodyTemplate",
                    CurrentSubjectTemplate = SummarizeTemplate(settings.BusinessInvitationEmailSubjectTemplate, "Invitation to join {business_name} on Darwin"),
                    CurrentBodyTemplate = SummarizeTemplate(settings.BusinessInvitationEmailBodyTemplate, "Hello {recipient_name}, use {invitation_link} to join {business_name}."),
                    SupportedTokens = "{recipient_name}, {business_name}, {invitation_link}, {support_email}",
                    OperatorControl = "Operators can edit subject/body in communication policy and resend/revoke invitations from support surfaces.",
                    AuditFlowKey = "BusinessInvitation",
                    OperatorActionLabel = "Open Invitations",
                    OperatorActionTarget = "Invitations",
                    NextStep = "Promote to Communication Core template catalog with preview and delivery logging."
                },
                new()
                {
                    FlowName = "Account Activation",
                    TemplateSurface = "Site-setting template fields feeding the confirmation handler",
                    SubjectSource = "Transactional subject prefix + AccountActivationEmailSubjectTemplate",
                    BodySource = "AccountActivationEmailBodyTemplate",
                    CurrentSubjectTemplate = SummarizeTemplate(settings.AccountActivationEmailSubjectTemplate, "Confirm your Darwin account email"),
                    CurrentBodyTemplate = SummarizeTemplate(settings.AccountActivationEmailBodyTemplate, "Hello {recipient_name}, confirm your email with {confirmation_link}."),
                    SupportedTokens = "{recipient_name}, {confirmation_link}, {support_email}",
                    OperatorControl = "Operators can edit subject/body in communication policy and trigger activation support, but not bypass confirmation policy.",
                    AuditFlowKey = "AccountActivation",
                    OperatorActionLabel = "Open Users",
                    OperatorActionTarget = "Users",
                    NextStep = "Move into reusable template + confirmation analytics flow."
                },
                new()
                {
                    FlowName = "Password Reset",
                    TemplateSurface = "Site-setting template fields feeding the password-reset handler",
                    SubjectSource = "Transactional subject prefix + PasswordResetEmailSubjectTemplate",
                    BodySource = "PasswordResetEmailBodyTemplate",
                    CurrentSubjectTemplate = SummarizeTemplate(settings.PasswordResetEmailSubjectTemplate, "Reset your Darwin account password"),
                    CurrentBodyTemplate = SummarizeTemplate(settings.PasswordResetEmailBodyTemplate, "Hello {recipient_name}, reset your password with {reset_link}."),
                    SupportedTokens = "{recipient_name}, {reset_link}, {support_email}",
                    OperatorControl = "Operators can edit subject/body in communication policy and reissue reset support only after identity validation.",
                    AuditFlowKey = "PasswordReset",
                    OperatorActionLabel = "Open Users",
                    OperatorActionTarget = "Users",
                    NextStep = "Move into Communication Core template inventory with controlled support resend."
                },
                new()
                {
                    FlowName = "Phone Verification",
                    TemplateSurface = "Site-setting text template fields feeding the current-user mobile verification handler",
                    SubjectSource = "Not used for SMS/WhatsApp code delivery",
                    BodySource = "PhoneVerificationSmsTemplate + PhoneVerificationWhatsAppTemplate",
                    CurrentSubjectTemplate = "No subject surface",
                    CurrentBodyTemplate = $"{SummarizeTemplate(settings.PhoneVerificationSmsTemplate, "Your Darwin verification code is {token}. It expires at {expires_at_utc} UTC.")} / {SummarizeTemplate(settings.PhoneVerificationWhatsAppTemplate, "Confirm your Darwin mobile number with code {token}. It expires at {expires_at_utc} UTC.")}",
                    SupportedTokens = "{phone_e164}, {token}, {expires_at_utc}",
                    OperatorControl = "Operators can configure both SMS and WhatsApp text templates while backend policy picks the preferred or fallback channel for Web and Mobile clients.",
                    AuditFlowKey = string.Empty,
                    OperatorActionLabel = "Open Communication Policy",
                    OperatorActionTarget = "SiteSettings",
                    NextStep = "Expand from policy-driven delivery into richer multi-channel history so verification incidents can be audited across SMS and WhatsApp."
                },
                new()
                {
                    FlowName = "Admin Communication Test",
                    TemplateSurface = "Site-setting templates for operator-side diagnostic email, SMS, and WhatsApp tests",
                    SubjectSource = "Transactional subject prefix + CommunicationTestEmailSubjectTemplate",
                    BodySource = "CommunicationTestEmailBodyTemplate + CommunicationTestSmsTemplate + CommunicationTestWhatsAppTemplate",
                    CurrentSubjectTemplate = SummarizeTemplate(settings.CommunicationTestEmailSubjectTemplate, "Darwin communication test for {channel}"),
                    CurrentBodyTemplate = $"{SummarizeTemplate(settings.CommunicationTestEmailBodyTemplate, "<p>This is a Darwin {channel} communication test.</p>")} / {SummarizeTemplate(settings.CommunicationTestSmsTemplate, "Darwin SMS transport test requested by {requested_by} at {attempted_at_utc} UTC for {test_target}.")} / {SummarizeTemplate(settings.CommunicationTestWhatsAppTemplate, "Darwin WhatsApp transport test requested by {requested_by} at {attempted_at_utc} UTC for {test_target}.")}",
                    SupportedTokens = "{channel}, {requested_by}, {attempted_at_utc}, {test_target}, {transport_state}",
                    OperatorControl = "Operators can customize and send channel-specific diagnostic messages when the corresponding transport and test target are configured.",
                    AuditFlowKey = "AdminCommunicationTest",
                    OperatorActionLabel = "Open Audit Log",
                    OperatorActionTarget = "EmailAudits",
                    NextStep = "Keep these templates aligned with rollout diagnostics so email, SMS, and WhatsApp tests validate the real message family."
                },
                new()
                {
                    FlowName = "Admin Alerts",
                    TemplateSurface = "Configuration visibility only",
                    SubjectSource = "Not centralized",
                    BodySource = "Not centralized",
                    CurrentSubjectTemplate = "No shared template surface",
                    CurrentBodyTemplate = "No shared template surface",
                    SupportedTokens = "Not centralized",
                    OperatorControl = "No template catalog yet; only routing readiness and policy visibility are exposed.",
                    AuditFlowKey = string.Empty,
                    OperatorActionLabel = "Open Alert Settings",
                    OperatorActionTarget = "AdminAlerts",
                    NextStep = "Create alert template + routing inventory before adding delivery actions."
                }
            };
        }

        private static string SummarizeTemplate(string? template, string fallback)
        {
            var value = string.IsNullOrWhiteSpace(template) ? fallback : template.Trim();
            return value.Length <= 120 ? value : string.Concat(value.AsSpan(0, 117), "...");
        }

        private static Dictionary<string, string?> BuildCommunicationTestPlaceholders(
            string channel,
            string requestedBy,
            DateTime attemptedAtUtc,
            string? testTarget,
            string transportState)
        {
            return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["channel"] = channel,
                ["requested_by"] = requestedBy,
                ["attempted_at_utc"] = attemptedAtUtc.ToString("yyyy-MM-dd HH:mm:ss"),
                ["test_target"] = testTarget,
                ["transport_state"] = transportState
            };
        }

        private static string RenderTemplate(string? template, string fallback, IReadOnlyDictionary<string, string?> placeholders)
        {
            var output = string.IsNullOrWhiteSpace(template) ? fallback : template;
            foreach (var pair in placeholders)
            {
                output = output.Replace("{" + pair.Key + "}", pair.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            }

            return output;
        }

        private static List<CommunicationResendPolicyVm> BuildResendPolicies()
        {
            return new List<CommunicationResendPolicyVm>
            {
                new()
                {
                    FlowName = "Business Invitation",
                    CurrentSafeAction = "Use controlled audit retry or the invitation workspace resend/revoke after SMTP readiness is green.",
                    GenericRetryStatus = "Supported from failed or pending audit rows when a current invitation can be resolved safely.",
                    OperatorEntryPoint = "Business setup, invitations list, and email-audit queue.",
                    AuditFlowKey = "BusinessInvitation",
                    OperatorActionLabel = "Open Invitations",
                    OperatorActionTarget = "Invitations",
                    EscalationRule = "If repeated resends fail after transport is ready, escalate as Communication Core/platform debt instead of repeating sends."
                },
                new()
                {
                    FlowName = "Account Activation",
                    CurrentSafeAction = "Prefer self-service resend; use controlled audit retry or admin activation support only where current policy allows.",
                    GenericRetryStatus = "Supported from failed or pending audit rows after resolving the live user by recipient email.",
                    OperatorEntryPoint = "Users queue, business members queue, and email-audit queue.",
                    AuditFlowKey = "AccountActivation",
                    OperatorActionLabel = "Open Users",
                    OperatorActionTarget = "Users",
                    EscalationRule = "Do not bypass confirmation policy. Persistent failures after a validated resend are auth/communication troubleshooting."
                },
                new()
                {
                    FlowName = "Password Reset",
                    CurrentSafeAction = "Reissue reset only after identity validation and transport checks, whether from audit retry or support surfaces.",
                    GenericRetryStatus = "Supported from failed or pending audit rows after resolving the live user by recipient email.",
                    OperatorEntryPoint = "Users queue, business member support actions, and email-audit queue.",
                    AuditFlowKey = "PasswordReset",
                    OperatorActionLabel = "Open Users",
                    OperatorActionTarget = "Users",
                    EscalationRule = "Avoid repeated resets without verification. Persistent failures escalate as support/security or transport issues."
                },
                new()
                {
                    FlowName = "Phone Verification",
                    CurrentSafeAction = "Request a fresh phone-verification code through the canonical profile flow after confirming the current phone number and chosen channel.",
                    GenericRetryStatus = "No audit-driven retry yet; re-request only through the live verification flow.",
                    OperatorEntryPoint = "Profile/mobile verification surfaces and site settings communication policy.",
                    AuditFlowKey = string.Empty,
                    OperatorActionLabel = "Open Communication Policy",
                    OperatorActionTarget = "SiteSettings",
                    EscalationRule = "Do not manually replay old codes. If SMS or WhatsApp delivery fails repeatedly, fix the transport or switch channel before issuing another code."
                },
                new()
                {
                    FlowName = "Admin Communication Test",
                    CurrentSafeAction = "Rerun the diagnostic only to the configured channel test target after fixing provider or policy settings.",
                    GenericRetryStatus = "No retry queue; manual rerun only.",
                    OperatorEntryPoint = "Business Communications workspace.",
                    AuditFlowKey = "AdminCommunicationTest",
                    OperatorActionLabel = "Open Audit Log",
                    OperatorActionTarget = "EmailAudits",
                    EscalationRule = "Never switch diagnostics to live recipients. Keep tests scoped to the configured test target for each channel."
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
                    ? "Check SMTP readiness, then use controlled retry or open the recipient-scoped invitation queue for resend/revoke from the business workspace."
                    : "Check SMTP readiness, then review the invitation source before retrying.";
            }

            if (string.Equals(item.FlowKey, "AccountActivation", System.StringComparison.OrdinalIgnoreCase))
            {
                return item.BusinessId.HasValue
                    ? "Check SMTP readiness, then use controlled retry or open member support or the user queue with the failed recipient prefilled before sending activation support."
                    : "Check SMTP readiness, then use controlled retry, self-service resend activation, or admin activation support as policy allows.";
            }

            if (string.Equals(item.FlowKey, "PasswordReset", System.StringComparison.OrdinalIgnoreCase))
            {
                return item.BusinessId.HasValue
                    ? "Check SMTP readiness, then use controlled retry or open member support or the user queue with the failed recipient prefilled and reissue reset only after support validation."
                    : "Check SMTP readiness, then use controlled retry or reissue password reset only after support validation.";
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
                    AllowedAction = "Check SMTP readiness, then use controlled retry from the audit row or open the business invitation workspace and resend or revoke the invitation.",
                    EscalationRule = "If repeated failures continue after transport readiness is green, treat it as communication-platform debt instead of repeatedly sending the same invitation."
                },
                new()
                {
                    FlowKey = "AccountActivation",
                    Title = "Activation failures",
                    ScopeNote = "Prefer self-service resend; admin override only where current policy allows.",
                    AllowedAction = "Check SMTP readiness, then use controlled retry or direct the user to resend activation from login or use the admin activation-support action.",
                    EscalationRule = "Do not silently bypass confirmation policy. If failures persist after resend, escalate as auth/communication troubleshooting."
                },
                new()
                {
                    FlowKey = "PasswordReset",
                    Title = "Password reset failures",
                    ScopeNote = "Reset support must stay identity-safe.",
                    AllowedAction = "Validate the requester first, then use controlled retry or reissue password reset only after support validation and transport checks.",
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
