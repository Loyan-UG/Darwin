using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Businesses.Commands;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Businesses.Queries;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Settings.DTOs;
using Darwin.Domain.Entities.Integration;
using Darwin.Infrastructure.Notifications;
using Darwin.Infrastructure.Notifications.Brevo;
using Darwin.WebAdmin.Security;
using Darwin.WebAdmin.Services.Settings;
using Darwin.WebAdmin.ViewModels.Admin;
using Darwin.WebAdmin.ViewModels.Businesses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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
        private readonly GetProviderCallbackInboxPageHandler _getProviderCallbackInboxPage;
        private readonly RetryEmailDispatchAuditHandler _retryEmailDispatchAudit;
        private readonly CancelCommunicationDispatchOperationHandler _cancelCommunicationDispatchOperation;
        private readonly UpdateProviderCallbackInboxMessageHandler _updateProviderCallbackInboxMessage;
        private readonly IAppDbContext _db;
        private readonly ISiteSettingCache _siteSettingCache;
        private readonly EmailDeliveryOptions _emailDeliveryOptions;
        private readonly BrevoEmailOptions _brevoEmailOptions;
        private readonly IClock _clock;

        public BusinessCommunicationsController(
            GetBusinessCommunicationOpsSummaryHandler getSummary,
            GetBusinessCommunicationSetupPageHandler getSetupPage,
            GetBusinessCommunicationProfileHandler getProfile,
            GetEmailDispatchAuditsPageHandler getEmailDispatchAuditsPage,
            GetChannelDispatchActivityHandler getChannelDispatchActivity,
            GetProviderCallbackInboxPageHandler getProviderCallbackInboxPage,
            RetryEmailDispatchAuditHandler retryEmailDispatchAudit,
            CancelCommunicationDispatchOperationHandler cancelCommunicationDispatchOperation,
            UpdateProviderCallbackInboxMessageHandler updateProviderCallbackInboxMessage,
            IAppDbContext db,
            ISiteSettingCache siteSettingCache,
            IOptions<EmailDeliveryOptions> emailDeliveryOptions,
            IOptions<BrevoEmailOptions> brevoEmailOptions,
            IClock clock)
        {
            _getSummary = getSummary ?? throw new ArgumentNullException(nameof(getSummary));
            _getSetupPage = getSetupPage ?? throw new ArgumentNullException(nameof(getSetupPage));
            _getProfile = getProfile ?? throw new ArgumentNullException(nameof(getProfile));
            _getEmailDispatchAuditsPage = getEmailDispatchAuditsPage ?? throw new ArgumentNullException(nameof(getEmailDispatchAuditsPage));
            _getChannelDispatchActivity = getChannelDispatchActivity ?? throw new ArgumentNullException(nameof(getChannelDispatchActivity));
            _getProviderCallbackInboxPage = getProviderCallbackInboxPage ?? throw new ArgumentNullException(nameof(getProviderCallbackInboxPage));
            _retryEmailDispatchAudit = retryEmailDispatchAudit ?? throw new ArgumentNullException(nameof(retryEmailDispatchAudit));
            _cancelCommunicationDispatchOperation = cancelCommunicationDispatchOperation ?? throw new ArgumentNullException(nameof(cancelCommunicationDispatchOperation));
            _updateProviderCallbackInboxMessage = updateProviderCallbackInboxMessage ?? throw new ArgumentNullException(nameof(updateProviderCallbackInboxMessage));
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _siteSettingCache = siteSettingCache ?? throw new ArgumentNullException(nameof(siteSettingCache));
            _emailDeliveryOptions = (emailDeliveryOptions ?? throw new ArgumentNullException(nameof(emailDeliveryOptions))).Value;
            _brevoEmailOptions = (brevoEmailOptions ?? throw new ArgumentNullException(nameof(brevoEmailOptions))).Value;
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
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
            var (_, _, providerCallbackSummary, _) = await _getProviderCallbackInboxPage
                .HandleAsync(1, 10, new ProviderCallbackInboxFilterDto(), ct)
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
                    EmailTransportConfigured = IsEmailTransportConfigured(settings),
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
                    CanSendTestEmail = IsEmailTransportConfigured(settings) &&
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
                ChannelTemplateFamilies = BuildChannelTemplateFamilies(settings, null),
                ResendPolicies = BuildResendPolicies(),
                ProviderCallbackSummary = MapProviderCallbackSummary(providerCallbackSummary),
                ChannelAuditSummary = new ChannelDispatchAuditSummaryVm
                {
                    TotalCount = channelAuditSummary.TotalCount,
                    FailedCount = channelAuditSummary.FailedCount,
                    PendingCount = channelAuditSummary.PendingCount,
                    QueuedPendingCount = channelAuditSummary.QueuedPendingCount,
                    QueuedFailedCount = channelAuditSummary.QueuedFailedCount,
                    Recent24HourCount = channelAuditSummary.Recent24HourCount,
                    SmsCount = channelAuditSummary.SmsCount,
                    WhatsAppCount = channelAuditSummary.WhatsAppCount,
                    PhoneVerificationCount = channelAuditSummary.PhoneVerificationCount,
                    AdminTestCount = channelAuditSummary.AdminTestCount
                },
                RecentEmailAudits = emailAudits.Select(x => new EmailDispatchAuditListItemVm
                {
                    Id = x.Id,
                    RowVersion = x.RowVersion,
                    IsQueueOperation = x.IsQueueOperation,
                    Provider = x.Provider,
                    FlowKey = x.FlowKey,
                    TemplateKey = x.TemplateKey,
                    CorrelationKey = x.CorrelationKey,
                    BusinessId = x.BusinessId,
                    BusinessName = x.BusinessName,
                    RecipientEmail = x.RecipientEmail,
                    IntendedRecipientEmail = x.IntendedRecipientEmail,
                    Subject = x.Subject,
                    ProviderMessageId = x.ProviderMessageId,
                    LatestProviderCallbackEvent = x.LatestProviderCallbackEvent,
                    LatestProviderCallbackStatus = x.LatestProviderCallbackStatus,
                    LatestProviderCallbackReason = x.LatestProviderCallbackReason,
                    LatestProviderCallbackOccurredAtUtc = x.LatestProviderCallbackOccurredAtUtc,
                    Status = x.Status,
                    AttemptedAtUtc = x.AttemptedAtUtc,
                    CompletedAtUtc = x.CompletedAtUtc,
                    FailureMessage = x.FailureMessage,
                    QueueAttemptCount = x.QueueAttemptCount,
                    AttemptAgeMinutes = x.AttemptAgeMinutes,
                    CompletionLatencySeconds = x.CompletionLatencySeconds,
                    NeedsOperatorFollowUp = x.NeedsOperatorFollowUp,
                    Severity = x.Severity,
                    CanRetryNow = x.CanRetryNow,
                    RetryPolicyState = x.RetryPolicyState,
                    RetryBlockedReason = BuildEmailAuditRetryBlockedReason(x),
                    RetryAvailableAtUtc = x.RetryAvailableAtUtc,
                    RecentAttemptCount24h = x.RecentAttemptCount24h,
                    ChainStartedAtUtc = x.ChainStartedAtUtc,
                    ChainLastAttemptAtUtc = x.ChainLastAttemptAtUtc,
                    ChainSpanHours = x.ChainSpanHours,
                    ChainStatusMix = BuildEmailAuditChainStatusMix(x.ChainStatusMix),
                    PriorAttemptCount = x.PriorAttemptCount,
                    PriorFailureCount = x.PriorFailureCount,
                    LastSuccessfulAttemptAtUtc = x.LastSuccessfulAttemptAtUtc,
                    RecommendedAction = BuildAuditRecommendedAction(x)
                }).ToList(),
                RecentChannelAudits = channelAudits.Select(x => new ChannelDispatchAuditListItemVm
                {
                    Id = x.Id,
                    RowVersion = x.RowVersion,
                    IsQueueOperation = x.IsQueueOperation,
                    Channel = x.Channel,
                    Provider = x.Provider,
                    FlowKey = x.FlowKey,
                    TemplateKey = x.TemplateKey,
                    CorrelationKey = x.CorrelationKey,
                    BusinessId = x.BusinessId,
                    RecipientAddress = x.RecipientAddress,
                    IntendedRecipientAddress = x.IntendedRecipientAddress,
                    MessagePreview = x.MessagePreview,
                    ProviderMessageId = x.ProviderMessageId,
                    Status = x.Status,
                    AttemptedAtUtc = x.AttemptedAtUtc,
                    CompletedAtUtc = x.CompletedAtUtc,
                    FailureMessage = x.FailureMessage,
                    QueueAttemptCount = x.QueueAttemptCount
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
        public async Task<IActionResult> TemplatePreviews(string? flowKey = null, string? channel = null, CancellationToken ct = default)
        {
            var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);
            var previewedAtUtc = _clock.UtcNow;
            var vm = new CommunicationTemplatePreviewsVm
            {
                FlowKey = flowKey ?? string.Empty,
                Channel = channel ?? string.Empty,
                FlowItems = BuildTemplatePreviewFlowItems(flowKey),
                ChannelItems = BuildTemplatePreviewChannelItems(channel),
                Items = BuildTemplatePreviews(settings, flowKey, channel, previewedAtUtc)
            };

            return RenderTemplatePreviewsWorkspace(vm);
        }

        [HttpGet]
        public async Task<IActionResult> ProviderCallbacks(
            int page = 1,
            int pageSize = 20,
            string? query = null,
            string? provider = null,
            string? status = null,
            bool stalePendingOnly = false,
            bool failedOnly = false,
            bool deliveryFailureOnly = false,
            CancellationToken ct = default)
        {
            var filter = new ProviderCallbackInboxFilterDto
            {
                Query = query ?? string.Empty,
                Provider = provider ?? string.Empty,
                Status = status ?? string.Empty,
                StalePendingOnly = stalePendingOnly,
                FailedOnly = failedOnly,
                DeliveryFailureOnly = deliveryFailureOnly
            };

            var (items, total, summary, providers) = await _getProviderCallbackInboxPage
                .HandleAsync(page, pageSize, filter, ct)
                .ConfigureAwait(false);

            var vm = new ProviderCallbackInboxListVm
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = filter.Query,
                Provider = filter.Provider,
                Status = filter.Status,
                StalePendingOnly = stalePendingOnly,
                FailedOnly = failedOnly,
                DeliveryFailureOnly = deliveryFailureOnly,
                Summary = MapProviderCallbackSummary(summary),
                PageSizeItems = BuildPageSizeItems(pageSize),
                ProviderItems = BuildProviderCallbackProviderItems(providers, provider),
                StatusItems = BuildProviderCallbackStatusItems(status),
                Items = items.Select(x => new ProviderCallbackInboxListItemVm
                {
                    Id = x.Id,
                    RowVersion = x.RowVersion,
                    Provider = x.Provider,
                    CallbackType = x.CallbackType,
                    Status = x.Status,
                    IdempotencyKey = x.IdempotencyKey,
                    AttemptCount = x.AttemptCount,
                    LastAttemptAtUtc = x.LastAttemptAtUtc,
                    ProcessedAtUtc = x.ProcessedAtUtc,
                    CreatedAtUtc = x.CreatedAtUtc,
                    AgeMinutes = x.AgeMinutes,
                    IsStalePending = x.IsStalePending,
                    FailureReason = x.FailureReason,
                    PayloadPreview = x.PayloadPreview
                }).ToList()
            };

            return RenderProviderCallbacksWorkspace(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProviderCallback(
            Guid id,
            string rowVersion,
            string action,
            string? failureReason = null,
            int page = 1,
            int pageSize = 20,
            string? query = null,
            string? provider = null,
            string? status = null,
            bool stalePendingOnly = false,
            bool failedOnly = false,
            bool deliveryFailureOnly = false,
            CancellationToken ct = default)
        {
            if (id == Guid.Empty || string.IsNullOrWhiteSpace(action))
            {
                SetErrorMessage("ProviderCallbackUpdateFailedMessage");
                return RedirectOrHtmx(nameof(ProviderCallbacks), new
                {
                    page,
                    pageSize,
                    query,
                    provider,
                    status,
                    stalePendingOnly,
                    failedOnly,
                    deliveryFailureOnly
                });
            }

            var version = DecodeBase64RowVersion(rowVersion);

            var result = await _updateProviderCallbackInboxMessage
                .HandleAsync(new UpdateProviderCallbackInboxMessageDto
                {
                    Id = id,
                    RowVersion = version,
                    Action = action,
                    FailureReason = failureReason
                }, ct)
                .ConfigureAwait(false);

            if (result.Succeeded)
            {
                SetSuccessMessage(action switch
                {
                    "MarkProcessed" => "ProviderCallbackMarkedProcessedMessage",
                    "MarkFailed" => "ProviderCallbackMarkedFailedMessage",
                    "Requeue" => "ProviderCallbackRequeuedMessage",
                    _ => "ProviderCallbackUpdatedMessage"
                });
            }
            else
            {
                SetErrorMessage("ProviderCallbackUpdateFailedMessage");
            }

            return RedirectOrHtmx(nameof(ProviderCallbacks), new
            {
                page,
                pageSize,
                query,
                provider,
                status,
                stalePendingOnly,
                failedOnly,
                deliveryFailureOnly
            });
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid businessId, CancellationToken ct = default)
        {
            if (businessId == Guid.Empty)
            {
                SetErrorMessage("BusinessCommunicationProfileNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var profile = await _getProfile.HandleAsync(businessId, ct).ConfigureAwait(false);
            if (profile is null)
            {
                SetErrorMessage("BusinessCommunicationProfileNotFound");
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
            var emailTransportConfigured = IsEmailTransportConfigured(settings);
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
                PendingInvitationCount = profile.PendingInvitationCount,
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
                ChannelTemplateFamilies = BuildChannelTemplateFamilies(settings, null),
                ResendPolicies = BuildResendPolicies(),
                ReadinessIssues = BuildReadinessIssues(profile, emailTransportConfigured, adminAlertRoutingConfigured),
                RecommendedActions = BuildRecommendedActions(profile, emailTransportConfigured, adminAlertRoutingConfigured),
                RecentEmailAudits = recentAudits.Select(x => new EmailDispatchAuditListItemVm
                {
                    Id = x.Id,
                    RowVersion = x.RowVersion,
                    IsQueueOperation = x.IsQueueOperation,
                    Provider = x.Provider,
                    FlowKey = x.FlowKey,
                    TemplateKey = x.TemplateKey,
                    CorrelationKey = x.CorrelationKey,
                    BusinessId = x.BusinessId,
                    BusinessName = x.BusinessName,
                    RecipientEmail = x.RecipientEmail,
                    IntendedRecipientEmail = x.IntendedRecipientEmail,
                    Subject = x.Subject,
                    ProviderMessageId = x.ProviderMessageId,
                    LatestProviderCallbackEvent = x.LatestProviderCallbackEvent,
                    LatestProviderCallbackStatus = x.LatestProviderCallbackStatus,
                    LatestProviderCallbackReason = x.LatestProviderCallbackReason,
                    LatestProviderCallbackOccurredAtUtc = x.LatestProviderCallbackOccurredAtUtc,
                    Status = x.Status,
                    AttemptedAtUtc = x.AttemptedAtUtc,
                    CompletedAtUtc = x.CompletedAtUtc,
                    FailureMessage = x.FailureMessage,
                    QueueAttemptCount = x.QueueAttemptCount,
                    AttemptAgeMinutes = x.AttemptAgeMinutes,
                    CompletionLatencySeconds = x.CompletionLatencySeconds,
                    NeedsOperatorFollowUp = x.NeedsOperatorFollowUp,
                    Severity = x.Severity,
                    CanRetryNow = x.CanRetryNow,
                    RetryPolicyState = x.RetryPolicyState,
                    RetryBlockedReason = BuildEmailAuditRetryBlockedReason(x),
                    RetryAvailableAtUtc = x.RetryAvailableAtUtc,
                    RecentAttemptCount24h = x.RecentAttemptCount24h,
                    ChainStartedAtUtc = x.ChainStartedAtUtc,
                    ChainLastAttemptAtUtc = x.ChainLastAttemptAtUtc,
                    ChainSpanHours = x.ChainSpanHours,
                    ChainStatusMix = BuildEmailAuditChainStatusMix(x.ChainStatusMix),
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
                QueuedPendingCount = channelAuditSummary.QueuedPendingCount,
                QueuedFailedCount = channelAuditSummary.QueuedFailedCount,
                Recent24HourCount = channelAuditSummary.Recent24HourCount,
                SmsCount = channelAuditSummary.SmsCount,
                WhatsAppCount = channelAuditSummary.WhatsAppCount,
                PhoneVerificationCount = channelAuditSummary.PhoneVerificationCount,
                AdminTestCount = channelAuditSummary.AdminTestCount
            };
            vm.RecentChannelAudits = channelAudits.Select(x => new ChannelDispatchAuditListItemVm
            {
                Id = x.Id,
                RowVersion = x.RowVersion,
                IsQueueOperation = x.IsQueueOperation,
                Channel = x.Channel,
                Provider = x.Provider,
                FlowKey = x.FlowKey,
                TemplateKey = x.TemplateKey,
                CorrelationKey = x.CorrelationKey,
                BusinessId = x.BusinessId,
                RecipientAddress = x.RecipientAddress,
                IntendedRecipientAddress = x.IntendedRecipientAddress,
                MessagePreview = x.MessagePreview,
                ProviderMessageId = x.ProviderMessageId,
                Status = x.Status,
                AttemptedAtUtc = x.AttemptedAtUtc,
                CompletedAtUtc = x.CompletedAtUtc,
                FailureMessage = x.FailureMessage,
                QueueAttemptCount = x.QueueAttemptCount
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
                    HighChainVolumeCount = summary.HighChainVolumeCount,
                    QueuedPendingCount = summary.QueuedPendingCount,
                    QueuedFailedCount = summary.QueuedFailedCount
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
                    StatusMix = BuildEmailAuditChainStatusMix(chainSummary.StatusMix),
                    RecentHistory = chainSummary.RecentHistory.Select(x => new EmailDispatchAuditChainHistoryItemVm
                    {
                        AttemptedAtUtc = x.AttemptedAtUtc,
                        Status = x.Status,
                        Provider = x.Provider,
                        TemplateKey = x.TemplateKey,
                        CorrelationKey = x.CorrelationKey,
                        Subject = x.Subject,
                        IntendedRecipientEmail = x.IntendedRecipientEmail,
                        ProviderMessageId = x.ProviderMessageId,
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
                    RowVersion = x.RowVersion,
                    IsQueueOperation = x.IsQueueOperation,
                    Provider = x.Provider,
                    FlowKey = x.FlowKey,
                    TemplateKey = x.TemplateKey,
                    CorrelationKey = x.CorrelationKey,
                    BusinessId = x.BusinessId,
                    BusinessName = x.BusinessName,
                    RecipientEmail = x.RecipientEmail,
                    IntendedRecipientEmail = x.IntendedRecipientEmail,
                    Subject = x.Subject,
                    ProviderMessageId = x.ProviderMessageId,
                    LatestProviderCallbackEvent = x.LatestProviderCallbackEvent,
                    LatestProviderCallbackStatus = x.LatestProviderCallbackStatus,
                    LatestProviderCallbackReason = x.LatestProviderCallbackReason,
                    LatestProviderCallbackOccurredAtUtc = x.LatestProviderCallbackOccurredAtUtc,
                    Status = x.Status,
                    AttemptedAtUtc = x.AttemptedAtUtc,
                    CompletedAtUtc = x.CompletedAtUtc,
                    FailureMessage = x.FailureMessage,
                    QueueAttemptCount = x.QueueAttemptCount,
                    AttemptAgeMinutes = x.AttemptAgeMinutes,
                    CompletionLatencySeconds = x.CompletionLatencySeconds,
                    NeedsOperatorFollowUp = x.NeedsOperatorFollowUp,
                    Severity = x.Severity,
                    CanRetryNow = x.CanRetryNow,
                    RetryPolicyState = x.RetryPolicyState,
                    RetryBlockedReason = BuildEmailAuditRetryBlockedReason(x),
                    RetryAvailableAtUtc = x.RetryAvailableAtUtc,
                    RecentAttemptCount24h = x.RecentAttemptCount24h,
                    ChainStartedAtUtc = x.ChainStartedAtUtc,
                    ChainLastAttemptAtUtc = x.ChainLastAttemptAtUtc,
                    ChainSpanHours = x.ChainSpanHours,
                    ChainStatusMix = BuildEmailAuditChainStatusMix(x.ChainStatusMix),
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
            string? provider = null,
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
            bool escalationCandidatesOnly = false,
            bool heavyChainsOnly = false,
            bool providerReviewOnly = false,
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
                Provider = provider ?? string.Empty,
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
                EscalationCandidatesOnly = escalationCandidatesOnly,
                HeavyChainsOnly = heavyChainsOnly,
                ProviderReviewOnly = providerReviewOnly,
                ChainFollowUpOnly = chainFollowUpOnly,
                ChainResolvedOnly = chainResolvedOnly
            };

            var (items, total, summary, chainSummary, providerSummary) = await _getChannelDispatchActivity
                .HandlePageAsync(page, pageSize, filter, ct)
                .ConfigureAwait(false);

            var vm = new ChannelDispatchAuditsListVm
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = filter.Query,
                RecipientAddress = filter.RecipientAddress,
                Provider = filter.Provider,
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
                EscalationCandidatesOnly = escalationCandidatesOnly,
                HeavyChainsOnly = heavyChainsOnly,
                ProviderReviewOnly = providerReviewOnly,
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
                    QueuedPendingCount = summary.QueuedPendingCount,
                    QueuedFailedCount = summary.QueuedFailedCount,
                    Recent24HourCount = summary.Recent24HourCount,
                    SmsCount = summary.SmsCount,
                    WhatsAppCount = summary.WhatsAppCount,
                    PhoneVerificationCount = summary.PhoneVerificationCount,
                    AdminTestCount = summary.AdminTestCount,
                    RepeatedFailureCount = summary.RepeatedFailureCount,
                    PriorSuccessContextCount = summary.PriorSuccessContextCount,
                    ActionReadyCount = summary.ActionReadyCount,
                    ActionBlockedCount = summary.ActionBlockedCount,
                    EscalationCandidateCount = summary.EscalationCandidateCount,
                    HeavyChainCount = summary.HeavyChainCount,
                    ProviderReviewCount = summary.ProviderReviewCount,
                    ProviderRecoveredCount = summary.ProviderRecoveredCount
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
                    LastSuccessfulAttemptAtUtc = chainSummary.LastSuccessfulAttemptAtUtc,
                    StatusMix = chainSummary.StatusMix,
                    RecommendedAction = BuildChannelChainRecommendedAction(chainSummary.RecommendedAction),
                    EscalationHint = BuildChannelChainEscalationHint(chainSummary.EscalationHint),
                    RecentHistory = chainSummary.RecentHistory.Select(x => new ChannelDispatchAuditChainHistoryItemVm
                    {
                        AttemptedAtUtc = x.AttemptedAtUtc,
                        Channel = x.Channel,
                        Status = x.Status,
                        Provider = x.Provider,
                        TemplateKey = x.TemplateKey,
                        CorrelationKey = x.CorrelationKey,
                        MessagePreview = x.MessagePreview,
                        IntendedRecipientAddress = x.IntendedRecipientAddress,
                        ProviderMessageId = x.ProviderMessageId,
                        FailureMessage = x.FailureMessage,
                        CompletedAtUtc = x.CompletedAtUtc
                    }).ToList()
                },
                ProviderSummary = providerSummary == null ? null : new ChannelDispatchProviderSummaryVm
                {
                    Provider = providerSummary.Provider,
                    Channel = providerSummary.Channel,
                    FlowKey = providerSummary.FlowKey,
                    RecentAttemptCount24h = providerSummary.RecentAttemptCount24h,
                    FailureCount24h = providerSummary.FailureCount24h,
                    SentCount24h = providerSummary.SentCount24h,
                    PendingCount24h = providerSummary.PendingCount24h,
                    PressureState = providerSummary.PressureState,
                    RecoveryState = providerSummary.RecoveryState,
                    LastSuccessfulAttemptAtUtc = providerSummary.LastSuccessfulAttemptAtUtc,
                    RecommendedAction = BuildChannelProviderRecommendedAction(providerSummary.RecommendedAction),
                    EscalationHint = BuildChannelProviderEscalationHint(providerSummary.EscalationHint)
                },
                TemplateFamilies = BuildChannelTemplateFamilies(settings, filter.FlowKey),
                Items = items.Select(x => new ChannelDispatchAuditListItemVm
                {
                    Id = x.Id,
                    RowVersion = x.RowVersion,
                    IsQueueOperation = x.IsQueueOperation,
                    Channel = x.Channel,
                    Provider = x.Provider,
                    FlowKey = x.FlowKey,
                    TemplateKey = x.TemplateKey,
                    CorrelationKey = x.CorrelationKey,
                    BusinessId = x.BusinessId,
                    RecipientAddress = x.RecipientAddress,
                    IntendedRecipientAddress = x.IntendedRecipientAddress,
                    MessagePreview = x.MessagePreview,
                    ProviderMessageId = x.ProviderMessageId,
                    Status = x.Status,
                    AttemptedAtUtc = x.AttemptedAtUtc,
                    CompletedAtUtc = x.CompletedAtUtc,
                    FailureMessage = x.FailureMessage,
                    QueueAttemptCount = x.QueueAttemptCount,
                    NeedsOperatorFollowUp = x.NeedsOperatorFollowUp,
                    ChainAttemptCount = x.ChainAttemptCount,
                    ChainStatusMix = x.ChainStatusMix,
                    PriorAttemptCount = x.PriorAttemptCount,
                    PriorFailureCount = x.PriorFailureCount,
                    LastSuccessfulAttemptAtUtc = x.LastSuccessfulAttemptAtUtc,
                    CanRerunNow = x.CanRerunNow,
                    ActionPolicyState = BuildChannelAuditActionPolicyState(x.ActionPolicyState),
                        ActionBlockedReason = BuildChannelAuditActionBlockedReason(x),
                        ActionAvailableAtUtc = x.ActionAvailableAtUtc,
                        NeedsEscalationReview = x.NeedsEscalationReview,
                        EscalationReason = BuildChannelAuditEscalationReason(x),
                        ProviderRecentAttemptCount24h = x.ProviderRecentAttemptCount24h,
                        ProviderFailureCount24h = x.ProviderFailureCount24h,
                        ProviderPressureState = x.ProviderPressureState,
                        ProviderRecoveryState = x.ProviderRecoveryState,
                        ProviderLastSuccessfulAttemptAtUtc = x.ProviderLastSuccessfulAttemptAtUtc
                    }).ToList(),
                PageSizeItems = BuildPageSizeItems(pageSize),
                ProviderItems = BuildChannelProviderItems(items.Select(x => x.Provider), provider),
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
            if (id == Guid.Empty)
            {
                SetErrorMessage("CommunicationEmailRetryFailedFallback");
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

            var result = await _retryEmailDispatchAudit
                .HandleAsync(new RetryEmailDispatchAuditDto { AuditId = id }, ct)
                .ConfigureAwait(false);

            if (result.Succeeded)
            {
                SetSuccessMessage("EmailFlowRetriedSuccessfully");
            }
            else
            {
                SetErrorMessage("CommunicationEmailRetryFailedFallback");
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
        public async Task<IActionResult> CancelEmailDispatchOperation(
            Guid id,
            string? rowVersion,
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
            if (id == Guid.Empty)
            {
                SetErrorMessage("CommunicationQueuedDispatchCancelFailedMessage");
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

            var result = await _cancelCommunicationDispatchOperation
                .HandleAsync(new CancelCommunicationDispatchOperationDto
                {
                    Id = id,
                    RowVersion = DecodeBase64RowVersion(rowVersion),
                    Channel = "Email"
                }, ct)
                .ConfigureAwait(false);

            if (result.Succeeded)
            {
                SetSuccessMessage("CommunicationQueuedDispatchCancelledMessage");
            }
            else
            {
                SetErrorMessage("CommunicationQueuedDispatchCancelFailedMessage");
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
        public async Task<IActionResult> CancelChannelDispatchOperation(
            Guid id,
            string? rowVersion,
            int page = 1,
            int pageSize = 20,
            string? query = null,
            string? recipientAddress = null,
            string? provider = null,
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
            bool escalationCandidatesOnly = false,
            bool heavyChainsOnly = false,
            bool providerReviewOnly = false,
            bool chainFollowUpOnly = false,
            bool chainResolvedOnly = false,
            Guid? businessId = null,
            CancellationToken ct = default)
        {
            if (id == Guid.Empty || string.IsNullOrWhiteSpace(channel))
            {
                SetErrorMessage("CommunicationQueuedDispatchCancelFailedMessage");
                return RedirectOrHtmx(
                    nameof(ChannelAudits),
                    new
                    {
                        page,
                        pageSize,
                        query,
                        recipientAddress,
                        provider,
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
                        escalationCandidatesOnly,
                        heavyChainsOnly,
                        providerReviewOnly,
                        chainFollowUpOnly,
                        chainResolvedOnly,
                        businessId
                    });
            }

            var result = await _cancelCommunicationDispatchOperation
                .HandleAsync(new CancelCommunicationDispatchOperationDto
                {
                    Id = id,
                    RowVersion = DecodeBase64RowVersion(rowVersion),
                    Channel = channel ?? string.Empty
                }, ct)
                .ConfigureAwait(false);

            if (result.Succeeded)
            {
                SetSuccessMessage("CommunicationQueuedDispatchCancelledMessage");
            }
            else
            {
                SetErrorMessage("CommunicationQueuedDispatchCancelFailedMessage");
            }

            return RedirectOrHtmx(
                nameof(ChannelAudits),
                new
                {
                    page,
                    pageSize,
                    query,
                    recipientAddress,
                    provider,
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
                    escalationCandidatesOnly,
                    heavyChainsOnly,
                    providerReviewOnly,
                    chainFollowUpOnly,
                    chainResolvedOnly,
                    businessId
                });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelChannelDispatchOperationsBatch(
            int page = 1,
            int pageSize = 20,
            string? query = null,
            string? recipientAddress = null,
            string? provider = null,
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
            bool escalationCandidatesOnly = false,
            bool heavyChainsOnly = false,
            bool providerReviewOnly = false,
            bool chainFollowUpOnly = false,
            bool chainResolvedOnly = false,
            Guid? businessId = null,
            CancellationToken ct = default)
        {
            var result = await _cancelCommunicationDispatchOperation
                .HandleChannelBatchAsync(new CancelChannelDispatchOperationsBatchDto
                {
                    Query = query ?? string.Empty,
                    RecipientAddress = recipientAddress ?? string.Empty,
                    Provider = provider ?? string.Empty,
                    Channel = channel ?? string.Empty,
                    FlowKey = flowKey ?? string.Empty,
                    Status = status ?? string.Empty,
                    BusinessId = businessId,
                    FailedOnly = failedOnly,
                    PhoneVerificationOnly = phoneVerificationOnly,
                    AdminTestOnly = adminTestOnly
                }, ct)
                .ConfigureAwait(false);

            if (result.Succeeded)
            {
                TempData["Success"] = string.Format(CultureInfo.CurrentCulture, T("CommunicationQueuedDispatchBatchCancelledMessage"), result.Value);
            }
            else
            {
                SetErrorMessage("CommunicationQueuedDispatchCancelFailedMessage");
            }

            return RedirectOrHtmx(
                nameof(ChannelAudits),
                new
                {
                    page,
                    pageSize,
                    query,
                    recipientAddress,
                    provider,
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
                    escalationCandidatesOnly,
                    heavyChainsOnly,
                    providerReviewOnly,
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
            var emailTransportConfigured = IsEmailTransportConfigured(settings);

            if (!emailTransportConfigured)
            {
                SetErrorMessage("EmailTransportNotReadyForCommunicationTest");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            if (string.IsNullOrWhiteSpace(settings.CommunicationTestInboxEmail))
            {
                SetErrorMessage("CommunicationTestInboxNotConfigured");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var requestedBy = User?.Identity?.Name ?? T("CommunicationChannelFamilyOperatorPlaceholder");
            var nowUtc = _clock.UtcNow;
            var placeholders = BuildCommunicationTestPlaceholders(
                channel: DescribeCommunicationChannel("Email"),
                requestedBy: requestedBy,
                attemptedAtUtc: nowUtc,
                testTarget: settings.CommunicationTestInboxEmail,
                transportState: DescribeCommunicationTransportState(emailTransportConfigured));
            var prefix = string.IsNullOrWhiteSpace(settings.TransactionalEmailSubjectPrefix)
                ? string.Empty
                : $"{settings.TransactionalEmailSubjectPrefix.Trim()} ";
            var subject = prefix + RenderTemplate(
                settings.CommunicationTestEmailSubjectTemplate,
                T("CommunicationTemplateInventoryAdminTestSubjectFallback"),
                placeholders);
            var htmlBody = RenderTemplate(
                settings.CommunicationTestEmailBodyTemplate,
                T("CommunicationTestEmailBodyRuntimeFallback"),
                placeholders);

            _db.Set<EmailDispatchOperation>().Add(new EmailDispatchOperation
            {
                Provider = GetConfiguredEmailProvider(),
                RecipientEmail = settings.CommunicationTestInboxEmail!,
                IntendedRecipientEmail = settings.CommunicationTestInboxEmail,
                Subject = subject,
                HtmlBody = htmlBody,
                FlowKey = "AdminCommunicationTest",
                TemplateKey = "AdminCommunicationTestEmail",
                CorrelationKey = Guid.NewGuid().ToString("N"),
                Status = "Pending"
            });
            if (!await TrySaveCommunicationTestQueueAsync(ct).ConfigureAwait(false))
            {
                return RedirectOrHtmx(nameof(Index), new { });
            }

            TempData["Success"] = string.Format(T("CommunicationTestEmailQueuedMessage"), settings.CommunicationTestInboxEmail);
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
            string? provider = null,
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
            bool escalationCandidatesOnly = false,
            bool heavyChainsOnly = false,
            bool providerReviewOnly = false,
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
                SetErrorMessage("SmsTransportNotReadyForCommunicationTest");
                return RedirectToChannelAuditsOrIndex(
                    returnToChannelAudits,
                    page,
                    pageSize,
                    query,
                    recipientAddress,
                    provider,
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
                    escalationCandidatesOnly,
                    heavyChainsOnly,
                    providerReviewOnly,
                    chainFollowUpOnly,
                    chainResolvedOnly,
                    businessId);
            }

            if (string.IsNullOrWhiteSpace(settings.CommunicationTestSmsRecipientE164))
            {
                SetErrorMessage("CommunicationTestSmsRecipientNotConfigured");
                return RedirectToChannelAuditsOrIndex(
                    returnToChannelAudits,
                    page,
                    pageSize,
                    query,
                    recipientAddress,
                    provider,
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
                    escalationCandidatesOnly,
                    heavyChainsOnly,
                    providerReviewOnly,
                    chainFollowUpOnly,
                    chainResolvedOnly,
                    businessId);
            }

            var nowUtc = _clock.UtcNow;
            var smsCooldownUntilUtc = await GetChannelTestCooldownUntilUtcAsync(
                "SMS",
                settings.CommunicationTestSmsRecipientE164,
                nowUtc,
                ct).ConfigureAwait(false);
            if (smsCooldownUntilUtc.HasValue)
            {
                TempData["Error"] = string.Format(CultureInfo.InvariantCulture, T("CommunicationTestSmsCooldownMessage"), smsCooldownUntilUtc.Value);
                return RedirectToChannelAuditsOrIndex(
                    returnToChannelAudits,
                    page,
                    pageSize,
                    query,
                    recipientAddress,
                    provider,
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
                    escalationCandidatesOnly,
                    heavyChainsOnly,
                    providerReviewOnly,
                    chainFollowUpOnly,
                    chainResolvedOnly,
                    businessId);
            }

            var requestedBy = User?.Identity?.Name ?? T("CommunicationChannelFamilyOperatorPlaceholder");
            var text = RenderTemplate(
                settings.CommunicationTestSmsTemplate,
                T("CommunicationTemplateInventoryAdminTestSmsBodyFallback"),
                BuildCommunicationTestPlaceholders(
                    channel: DescribeCommunicationChannel("SMS"),
                    requestedBy: requestedBy,
                    attemptedAtUtc: nowUtc,
                    testTarget: settings.CommunicationTestSmsRecipientE164,
                    transportState: DescribeCommunicationTransportState(smsTransportConfigured)));
            _db.Set<ChannelDispatchOperation>().Add(new ChannelDispatchOperation
            {
                Channel = "SMS",
                Provider = settings.SmsProvider!.Trim(),
                RecipientAddress = settings.CommunicationTestSmsRecipientE164,
                IntendedRecipientAddress = settings.CommunicationTestSmsRecipientE164,
                MessageText = text,
                FlowKey = "AdminCommunicationTest",
                TemplateKey = "AdminCommunicationTestSms",
                CorrelationKey = Guid.NewGuid().ToString("N"),
                Status = "Pending"
            });
            if (!await TrySaveCommunicationTestQueueAsync(ct).ConfigureAwait(false))
            {
                return RedirectToChannelAuditsOrIndex(
                    returnToChannelAudits,
                    page,
                    pageSize,
                    query,
                    recipientAddress,
                    provider,
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
                    escalationCandidatesOnly,
                    heavyChainsOnly,
                    providerReviewOnly,
                    chainFollowUpOnly,
                    chainResolvedOnly,
                    businessId);
            }

            TempData["Success"] = string.Format(T("CommunicationTestSmsQueuedMessage"), settings.CommunicationTestSmsRecipientE164);
            return RedirectToChannelAuditsOrIndex(
                returnToChannelAudits,
                page,
                pageSize,
                query,
                recipientAddress,
                provider,
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
                escalationCandidatesOnly,
                heavyChainsOnly,
                providerReviewOnly,
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
            string? provider = null,
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
            bool escalationCandidatesOnly = false,
            bool heavyChainsOnly = false,
            bool providerReviewOnly = false,
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
                SetErrorMessage("WhatsAppTransportNotReadyForCommunicationTest");
                return RedirectToChannelAuditsOrIndex(
                    returnToChannelAudits,
                    page,
                    pageSize,
                    query,
                    recipientAddress,
                    provider,
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
                    escalationCandidatesOnly,
                    heavyChainsOnly,
                    providerReviewOnly,
                    chainFollowUpOnly,
                    chainResolvedOnly,
                    businessId);
            }

            if (string.IsNullOrWhiteSpace(settings.CommunicationTestWhatsAppRecipientE164))
            {
                SetErrorMessage("CommunicationTestWhatsAppRecipientNotConfigured");
                return RedirectToChannelAuditsOrIndex(
                    returnToChannelAudits,
                    page,
                    pageSize,
                    query,
                    recipientAddress,
                    provider,
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
                    escalationCandidatesOnly,
                    heavyChainsOnly,
                    providerReviewOnly,
                    chainFollowUpOnly,
                    chainResolvedOnly,
                    businessId);
            }

            var nowUtc = _clock.UtcNow;
            var whatsAppCooldownUntilUtc = await GetChannelTestCooldownUntilUtcAsync(
                "WhatsApp",
                settings.CommunicationTestWhatsAppRecipientE164,
                nowUtc,
                ct).ConfigureAwait(false);
            if (whatsAppCooldownUntilUtc.HasValue)
            {
                TempData["Error"] = string.Format(CultureInfo.InvariantCulture, T("CommunicationTestWhatsAppCooldownMessage"), whatsAppCooldownUntilUtc.Value);
                return RedirectToChannelAuditsOrIndex(
                    returnToChannelAudits,
                    page,
                    pageSize,
                    query,
                    recipientAddress,
                    provider,
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
                    escalationCandidatesOnly,
                    heavyChainsOnly,
                    providerReviewOnly,
                    chainFollowUpOnly,
                    chainResolvedOnly,
                    businessId);
            }

            var requestedBy = User?.Identity?.Name ?? T("CommunicationChannelFamilyOperatorPlaceholder");
            var text = RenderTemplate(
                settings.CommunicationTestWhatsAppTemplate,
                T("CommunicationTemplateInventoryAdminTestWhatsAppBodyFallback"),
                BuildCommunicationTestPlaceholders(
                    channel: DescribeCommunicationChannel("WhatsApp"),
                    requestedBy: requestedBy,
                    attemptedAtUtc: nowUtc,
                    testTarget: settings.CommunicationTestWhatsAppRecipientE164,
                    transportState: DescribeCommunicationTransportState(whatsAppTransportConfigured)));
            _db.Set<ChannelDispatchOperation>().Add(new ChannelDispatchOperation
            {
                Channel = "WhatsApp",
                Provider = "Meta",
                RecipientAddress = settings.CommunicationTestWhatsAppRecipientE164,
                IntendedRecipientAddress = settings.CommunicationTestWhatsAppRecipientE164,
                MessageText = text,
                FlowKey = "AdminCommunicationTest",
                TemplateKey = "AdminCommunicationTestWhatsApp",
                CorrelationKey = Guid.NewGuid().ToString("N"),
                Status = "Pending"
            });
            if (!await TrySaveCommunicationTestQueueAsync(ct).ConfigureAwait(false))
            {
                return RedirectToChannelAuditsOrIndex(
                    returnToChannelAudits,
                    page,
                    pageSize,
                    query,
                    recipientAddress,
                    provider,
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
                    escalationCandidatesOnly,
                    heavyChainsOnly,
                    providerReviewOnly,
                    chainFollowUpOnly,
                    chainResolvedOnly,
                    businessId);
            }

            TempData["Success"] = string.Format(T("CommunicationTestWhatsAppQueuedMessage"), settings.CommunicationTestWhatsAppRecipientE164);
            return RedirectToChannelAuditsOrIndex(
                returnToChannelAudits,
                page,
                pageSize,
                query,
                recipientAddress,
                provider,
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
                escalationCandidatesOnly,
                heavyChainsOnly,
                providerReviewOnly,
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
            string? provider,
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
            bool escalationCandidatesOnly,
            bool heavyChainsOnly,
            bool providerReviewOnly,
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
                    provider,
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
                    escalationCandidatesOnly,
                    heavyChainsOnly,
                    providerReviewOnly,
                    chainFollowUpOnly,
                    chainResolvedOnly,
                    businessId
                });
        }

        private async Task<DateTime?> GetChannelTestCooldownUntilUtcAsync(
            string channel,
            string recipientAddress,
            DateTime nowUtc,
            CancellationToken ct)
        {
            var latestAttemptAtUtc = await _db.Set<ChannelDispatchAudit>()
                .AsNoTracking()
                .Where(x =>
                    !x.IsDeleted &&
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
            return cooldownUntilUtc > nowUtc ? cooldownUntilUtc : null;
        }

        private static IEnumerable<SelectListItem> BuildPageSizeItems(int selectedPageSize)
        {
            var sizes = new[] { 10, 20, 50, 100 };
            return sizes.Select(x => new SelectListItem(x.ToString(), x.ToString(), x == selectedPageSize)).ToList();
        }

        private async Task<bool> TrySaveCommunicationTestQueueAsync(CancellationToken ct)
        {
            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                return true;
            }
            catch (DbUpdateException)
            {
                SetErrorMessage("CommunicationTestQueueFailedMessage");
                return false;
            }
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

        private IActionResult RenderTemplatePreviewsWorkspace(CommunicationTemplatePreviewsVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/BusinessCommunications/TemplatePreviews.cshtml", vm);
            }

            return View("TemplatePreviews", vm);
        }

        private IActionResult RenderProviderCallbacksWorkspace(ProviderCallbackInboxListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/BusinessCommunications/ProviderCallbacks.cshtml", vm);
            }

            return View("ProviderCallbacks", vm);
        }

        private static ProviderCallbackInboxSummaryVm MapProviderCallbackSummary(ProviderCallbackInboxSummaryDto summary)
        {
            return new ProviderCallbackInboxSummaryVm
            {
                TotalCount = summary.TotalCount,
                PendingCount = summary.PendingCount,
                FailedCount = summary.FailedCount,
                ProcessedCount = summary.ProcessedCount,
                StalePendingCount = summary.StalePendingCount,
                RetriedCount = summary.RetriedCount,
                BrevoTotalCount = summary.BrevoTotalCount,
                BrevoPendingCount = summary.BrevoPendingCount,
                BrevoFailedCount = summary.BrevoFailedCount,
                BrevoProcessedCount = summary.BrevoProcessedCount,
                BrevoStalePendingCount = summary.BrevoStalePendingCount,
                BrevoDeliveryFailureEventCount = summary.BrevoDeliveryFailureEventCount,
                BrevoRecent24HourCount = summary.BrevoRecent24HourCount
            };
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
            return IsEmailTransportConfigured(settings) &&
                   !string.IsNullOrWhiteSpace(settings.CommunicationTestInboxEmail);
        }

        private bool IsEmailTransportConfigured(SiteSettingDto settings)
        {
            return IsBrevoTransportConfigured() ||
                   (settings.SmtpEnabled &&
                    !string.IsNullOrWhiteSpace(settings.SmtpHost) &&
                    settings.SmtpPort.HasValue &&
                    !string.IsNullOrWhiteSpace(settings.SmtpFromAddress));
        }

        private bool IsBrevoTransportConfigured()
        {
            return string.Equals(GetConfiguredEmailProvider(), EmailProviderNames.Brevo, StringComparison.OrdinalIgnoreCase) &&
                   !string.IsNullOrWhiteSpace(_brevoEmailOptions.ApiKey) &&
                   !string.IsNullOrWhiteSpace(_brevoEmailOptions.SenderEmail);
        }

        private string GetConfiguredEmailProvider()
        {
            return EmailProviderNames.Normalize(_emailDeliveryOptions.Provider);
        }

        private IEnumerable<SelectListItem> BuildFilterItems(BusinessCommunicationSetupFilter selectedFilter)
        {
            yield return new SelectListItem(T("CommunicationSetupFilterNeedsSetup"), BusinessCommunicationSetupFilter.NeedsSetup.ToString(), selectedFilter == BusinessCommunicationSetupFilter.NeedsSetup);
            yield return new SelectListItem(T("CommunicationSetupFilterMissingSupportEmail"), BusinessCommunicationSetupFilter.MissingSupportEmail.ToString(), selectedFilter == BusinessCommunicationSetupFilter.MissingSupportEmail);
            yield return new SelectListItem(T("CommunicationSetupFilterMissingSenderIdentity"), BusinessCommunicationSetupFilter.MissingSenderIdentity.ToString(), selectedFilter == BusinessCommunicationSetupFilter.MissingSenderIdentity);
            yield return new SelectListItem(T("CommunicationSetupFilterTransactionalEnabled"), BusinessCommunicationSetupFilter.TransactionalEnabled.ToString(), selectedFilter == BusinessCommunicationSetupFilter.TransactionalEnabled);
            yield return new SelectListItem(T("CommunicationSetupFilterMarketingEnabled"), BusinessCommunicationSetupFilter.MarketingEnabled.ToString(), selectedFilter == BusinessCommunicationSetupFilter.MarketingEnabled);
            yield return new SelectListItem(T("CommunicationSetupFilterOperationalAlertsEnabled"), BusinessCommunicationSetupFilter.OperationalAlertsEnabled.ToString(), selectedFilter == BusinessCommunicationSetupFilter.OperationalAlertsEnabled);
            yield return new SelectListItem(T("CommunicationSetupFilterAllBusinesses"), BusinessCommunicationSetupFilter.All.ToString(), selectedFilter == BusinessCommunicationSetupFilter.All);
        }

        private IEnumerable<SelectListItem> BuildAuditStatusItems(string? selectedStatus)
        {
            yield return new SelectListItem(T("CommunicationAuditStatusAll"), string.Empty, string.IsNullOrWhiteSpace(selectedStatus));
            yield return new SelectListItem(DescribeDeliveryStatus("Sent"), "Sent", string.Equals(selectedStatus, "Sent", System.StringComparison.OrdinalIgnoreCase));
            yield return new SelectListItem(DescribeDeliveryStatus("Failed"), "Failed", string.Equals(selectedStatus, "Failed", System.StringComparison.OrdinalIgnoreCase));
            yield return new SelectListItem(DescribeDeliveryStatus("Pending"), "Pending", string.Equals(selectedStatus, "Pending", System.StringComparison.OrdinalIgnoreCase));
        }

        private IEnumerable<SelectListItem> BuildAuditFlowItems(string? selectedFlowKey)
        {
            yield return new SelectListItem(T("CommunicationAuditFlowAll"), string.Empty, string.IsNullOrWhiteSpace(selectedFlowKey));
            yield return new SelectListItem(T("CommunicationDetailsActiveFlowInvitation"), "BusinessInvitation", string.Equals(selectedFlowKey, "BusinessInvitation", System.StringComparison.OrdinalIgnoreCase));
            yield return new SelectListItem(T("CommunicationDetailsActiveFlowActivation"), "AccountActivation", string.Equals(selectedFlowKey, "AccountActivation", System.StringComparison.OrdinalIgnoreCase));
            yield return new SelectListItem(T("CommunicationTemplateInventoryPasswordResetFlow"), "PasswordReset", string.Equals(selectedFlowKey, "PasswordReset", System.StringComparison.OrdinalIgnoreCase));
            yield return new SelectListItem(T("CommunicationTemplateInventoryAdminTestFlow"), "AdminCommunicationTest", string.Equals(selectedFlowKey, "AdminCommunicationTest", System.StringComparison.OrdinalIgnoreCase));
        }

        private IEnumerable<SelectListItem> BuildChannelItems(string? selectedChannel)
        {
            yield return new SelectListItem(T("CommunicationChannelAll"), string.Empty, string.IsNullOrWhiteSpace(selectedChannel));
            yield return new SelectListItem(DescribeCommunicationChannel("SMS"), "SMS", string.Equals(selectedChannel, "SMS", StringComparison.OrdinalIgnoreCase));
            yield return new SelectListItem(DescribeCommunicationChannel("WhatsApp"), "WhatsApp", string.Equals(selectedChannel, "WhatsApp", StringComparison.OrdinalIgnoreCase));
        }

        private IEnumerable<SelectListItem> BuildChannelProviderItems(IEnumerable<string> providers, string? selectedProvider)
        {
            yield return new SelectListItem(T("CommunicationProviderAll"), string.Empty, string.IsNullOrWhiteSpace(selectedProvider));
            foreach (var provider in providers
                         .Concat(string.IsNullOrWhiteSpace(selectedProvider) ? Array.Empty<string>() : new[] { selectedProvider! })
                         .Where(x => !string.IsNullOrWhiteSpace(x))
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                yield return new SelectListItem(provider, provider, string.Equals(selectedProvider, provider, StringComparison.OrdinalIgnoreCase));
            }
        }

        private IEnumerable<SelectListItem> BuildChannelFlowItems(string? selectedFlowKey)
        {
            yield return new SelectListItem(T("CommunicationChannelFlowAll"), string.Empty, string.IsNullOrWhiteSpace(selectedFlowKey));
            yield return new SelectListItem(T("CommunicationTemplateInventoryPhoneVerificationFlow"), "PhoneVerification", string.Equals(selectedFlowKey, "PhoneVerification", StringComparison.OrdinalIgnoreCase));
            yield return new SelectListItem(T("CommunicationTemplateInventoryAdminTestFlow"), "AdminCommunicationTest", string.Equals(selectedFlowKey, "AdminCommunicationTest", StringComparison.OrdinalIgnoreCase));
        }

        private IEnumerable<SelectListItem> BuildTemplatePreviewFlowItems(string? selectedFlowKey)
        {
            yield return new SelectListItem(T("CommunicationAuditFlowAll"), string.Empty, string.IsNullOrWhiteSpace(selectedFlowKey));
            yield return new SelectListItem(T("CommunicationDetailsActiveFlowInvitation"), "BusinessInvitation", string.Equals(selectedFlowKey, "BusinessInvitation", StringComparison.OrdinalIgnoreCase));
            yield return new SelectListItem(T("CommunicationDetailsActiveFlowActivation"), "AccountActivation", string.Equals(selectedFlowKey, "AccountActivation", StringComparison.OrdinalIgnoreCase));
            yield return new SelectListItem(T("CommunicationTemplateInventoryPasswordResetFlow"), "PasswordReset", string.Equals(selectedFlowKey, "PasswordReset", StringComparison.OrdinalIgnoreCase));
            yield return new SelectListItem(T("CommunicationTemplateInventoryPhoneVerificationFlow"), "PhoneVerification", string.Equals(selectedFlowKey, "PhoneVerification", StringComparison.OrdinalIgnoreCase));
            yield return new SelectListItem(T("CommunicationTemplateInventoryAdminTestFlow"), "AdminCommunicationTest", string.Equals(selectedFlowKey, "AdminCommunicationTest", StringComparison.OrdinalIgnoreCase));
        }

        private IEnumerable<SelectListItem> BuildTemplatePreviewChannelItems(string? selectedChannel)
        {
            yield return new SelectListItem(T("CommunicationChannelAll"), string.Empty, string.IsNullOrWhiteSpace(selectedChannel));
            yield return new SelectListItem(DescribeCommunicationChannel("Email"), "Email", string.Equals(selectedChannel, "Email", StringComparison.OrdinalIgnoreCase));
            yield return new SelectListItem(DescribeCommunicationChannel("SMS"), "SMS", string.Equals(selectedChannel, "SMS", StringComparison.OrdinalIgnoreCase));
            yield return new SelectListItem(DescribeCommunicationChannel("WhatsApp"), "WhatsApp", string.Equals(selectedChannel, "WhatsApp", StringComparison.OrdinalIgnoreCase));
        }

        private IEnumerable<SelectListItem> BuildProviderCallbackProviderItems(IEnumerable<string> providers, string? selectedProvider)
        {
            yield return new SelectListItem(T("CommunicationProviderAll"), string.Empty, string.IsNullOrWhiteSpace(selectedProvider));
            foreach (var provider in providers
                         .Concat(string.IsNullOrWhiteSpace(selectedProvider) ? Array.Empty<string>() : new[] { selectedProvider! })
                         .Where(x => !string.IsNullOrWhiteSpace(x))
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                yield return new SelectListItem(provider, provider, string.Equals(selectedProvider, provider, StringComparison.OrdinalIgnoreCase));
            }
        }

        private IEnumerable<SelectListItem> BuildProviderCallbackStatusItems(string? selectedStatus)
        {
            yield return new SelectListItem(T("CommunicationAuditStatusAll"), string.Empty, string.IsNullOrWhiteSpace(selectedStatus));
            yield return new SelectListItem(DescribeProviderCallbackStatus("Pending"), "Pending", string.Equals(selectedStatus, "Pending", StringComparison.OrdinalIgnoreCase));
            yield return new SelectListItem(DescribeProviderCallbackStatus("Failed"), "Failed", string.Equals(selectedStatus, "Failed", StringComparison.OrdinalIgnoreCase));
            yield return new SelectListItem(DescribeProviderCallbackStatus("Processed"), "Processed", string.Equals(selectedStatus, "Processed", StringComparison.OrdinalIgnoreCase));
        }

        private List<BuiltInCommunicationFlowVm> BuildBuiltInFlows()
        {
            return new List<BuiltInCommunicationFlowVm>
            {
                new()
                {
                    Name = T("CommunicationBuiltInFlowInvitationName"),
                    Channel = DescribeBuiltInFlowChannel("Email"),
                    Trigger = T("CommunicationBuiltInFlowInvitationTrigger"),
                    DeliveryPath = T("CommunicationBuiltInFlowInvitationDeliveryPath"),
                    CurrentImplementationStatus = T("CommunicationBuiltInFlowInvitationStatus"),
                    NextStep = T("CommunicationBuiltInFlowInvitationNextStep")
                },
                new()
                {
                    Name = T("CommunicationBuiltInFlowActivationName"),
                    Channel = DescribeBuiltInFlowChannel("Email"),
                    Trigger = T("CommunicationBuiltInFlowActivationTrigger"),
                    DeliveryPath = T("CommunicationBuiltInFlowActivationDeliveryPath"),
                    CurrentImplementationStatus = T("CommunicationBuiltInFlowActivationStatus"),
                    NextStep = T("CommunicationBuiltInFlowActivationNextStep")
                },
                new()
                {
                    Name = T("CommunicationBuiltInFlowPasswordResetName"),
                    Channel = DescribeBuiltInFlowChannel("Email"),
                    Trigger = T("CommunicationBuiltInFlowPasswordResetTrigger"),
                    DeliveryPath = T("CommunicationBuiltInFlowPasswordResetDeliveryPath"),
                    CurrentImplementationStatus = T("CommunicationBuiltInFlowPasswordResetStatus"),
                    NextStep = T("CommunicationBuiltInFlowPasswordResetNextStep")
                },
                new()
                {
                    Name = T("CommunicationBuiltInFlowPhoneVerificationName"),
                    Channel = DescribeBuiltInFlowChannel("SmsWhatsApp"),
                    Trigger = T("CommunicationBuiltInFlowPhoneVerificationTrigger"),
                    DeliveryPath = T("CommunicationBuiltInFlowPhoneVerificationDeliveryPath"),
                    CurrentImplementationStatus = T("CommunicationBuiltInFlowPhoneVerificationStatus"),
                    NextStep = T("CommunicationBuiltInFlowPhoneVerificationNextStep")
                },
                new()
                {
                    Name = T("CommunicationBuiltInFlowAdminTestName"),
                    Channel = DescribeBuiltInFlowChannel("EmailSmsWhatsApp"),
                    Trigger = T("CommunicationBuiltInFlowAdminTestTrigger"),
                    DeliveryPath = T("CommunicationBuiltInFlowAdminTestDeliveryPath"),
                    CurrentImplementationStatus = T("CommunicationBuiltInFlowAdminTestStatus"),
                    NextStep = T("CommunicationBuiltInFlowAdminTestNextStep")
                },
                new()
                {
                    Name = T("CommunicationBuiltInFlowAdminAlertsName"),
                    Channel = DescribeBuiltInFlowChannel("EmailSmsWhatsAppCompact"),
                    Trigger = T("CommunicationBuiltInFlowAdminAlertsTrigger"),
                    DeliveryPath = T("CommunicationBuiltInFlowAdminAlertsDeliveryPath"),
                    CurrentImplementationStatus = T("CommunicationBuiltInFlowAdminAlertsStatus"),
                    NextStep = T("CommunicationBuiltInFlowAdminAlertsNextStep")
                },
                new()
                {
                    Name = T("CommunicationBuiltInFlowTestTargetsName"),
                    Channel = DescribeBuiltInFlowChannel("SmsWhatsApp"),
                    Trigger = T("CommunicationBuiltInFlowTestTargetsTrigger"),
                    DeliveryPath = T("CommunicationBuiltInFlowTestTargetsDeliveryPath"),
                    CurrentImplementationStatus = T("CommunicationBuiltInFlowTestTargetsStatus"),
                    NextStep = T("CommunicationBuiltInFlowTestTargetsNextStep")
                }
            };
        }

        private List<CommunicationCapabilityCoverageVm> BuildCapabilityCoverage()
        {
            return new List<CommunicationCapabilityCoverageVm>
            {
                new()
                {
                    Capability = T("CommunicationCapabilityTemplateEngine"),
                    CurrentState = T("CommunicationCapabilityTemplateEngineState"),
                    OperatorVisibility = T("CommunicationCapabilityTemplateEngineVisibility"),
                    NextStep = T("CommunicationCapabilityTemplateEngineNextStep")
                },
                new()
                {
                    Capability = T("CommunicationCapabilityDeliveryLogging"),
                    CurrentState = T("CommunicationCapabilityDeliveryLoggingState"),
                    OperatorVisibility = T("CommunicationCapabilityDeliveryLoggingVisibility"),
                    NextStep = T("CommunicationCapabilityDeliveryLoggingNextStep")
                },
                new()
                {
                    Capability = T("CommunicationCapabilityRetryWorkflow"),
                    CurrentState = T("CommunicationCapabilityRetryWorkflowState"),
                    OperatorVisibility = T("CommunicationCapabilityRetryWorkflowVisibility"),
                    NextStep = T("CommunicationCapabilityRetryWorkflowNextStep")
                },
                new()
                {
                    Capability = T("CommunicationCapabilityBusinessPolicyVisibility"),
                    CurrentState = T("CommunicationCapabilityBusinessPolicyVisibilityState"),
                    OperatorVisibility = T("CommunicationCapabilityBusinessPolicyVisibilityVisibility"),
                    NextStep = T("CommunicationCapabilityBusinessPolicyVisibilityNextStep")
                },
                new()
                {
                    Capability = T("CommunicationCapabilityChannelTestTargets"),
                    CurrentState = T("CommunicationCapabilityChannelTestTargetsState"),
                    OperatorVisibility = T("CommunicationCapabilityChannelTestTargetsVisibility"),
                    NextStep = T("CommunicationCapabilityChannelTestTargetsNextStep")
                }
            };
        }

        private List<CommunicationChannelOpsVm> BuildChannelOperations(SiteSettingDto settings)
        {
            var emailReady = IsEmailTransportConfigured(settings);
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
                    Channel = DescribeCommunicationChannel("Email"),
                    CurrentState = emailReady ? T("CommunicationChannelOpsEmailReadyState") : T("CommunicationChannelOpsNotReadyState"),
                    LiveFlows = T("CommunicationChannelOpsEmailLiveFlows"),
                    SafeOperatorActions = emailReady
                        ? T("CommunicationChannelOpsEmailReadyActions")
                        : T("CommunicationChannelOpsEmailNotReadyActions"),
                    RiskBoundary = T("CommunicationChannelOpsEmailRiskBoundary"),
                    NextStep = T("CommunicationChannelOpsEmailNextStep")
                },
                new()
                {
                    Channel = "SMS",
                    CurrentState = smsReady ? T("CommunicationChannelOpsProviderReadyState") : T("CommunicationChannelOpsNotReadyState"),
                    LiveFlows = T("CommunicationChannelOpsSmsLiveFlows"),
                    SafeOperatorActions = smsReady
                        ? T("CommunicationChannelOpsSmsReadyActions")
                        : T("CommunicationChannelOpsSmsNotReadyActions"),
                    RiskBoundary = settings.PhoneVerificationAllowFallback
                        ? string.Format(CultureInfo.InvariantCulture, T("CommunicationChannelOpsSmsFallbackRiskBoundary"), DescribePreferredPhoneVerificationChannel(settings.PhoneVerificationPreferredChannel))
                        : T("CommunicationChannelOpsSmsStrictRiskBoundary"),
                    NextStep = T("CommunicationChannelOpsSmsNextStep")
                },
                new()
                {
                    Channel = "WhatsApp",
                    CurrentState = whatsAppReady ? T("CommunicationChannelOpsProviderReadyState") : T("CommunicationChannelOpsNotReadyState"),
                    LiveFlows = T("CommunicationChannelOpsWhatsAppLiveFlows"),
                    SafeOperatorActions = whatsAppReady
                        ? T("CommunicationChannelOpsWhatsAppReadyActions")
                        : T("CommunicationChannelOpsWhatsAppNotReadyActions"),
                    RiskBoundary = settings.PhoneVerificationAllowFallback
                        ? string.Format(CultureInfo.InvariantCulture, T("CommunicationChannelOpsWhatsAppFallbackRiskBoundary"), DescribePreferredPhoneVerificationChannel(settings.PhoneVerificationPreferredChannel))
                        : T("CommunicationChannelOpsWhatsAppStrictRiskBoundary"),
                    NextStep = T("CommunicationChannelOpsWhatsAppNextStep")
                }
            };
        }

        private List<CommunicationTemplateInventoryVm> BuildTemplateInventory(SiteSettingDto settings)
        {
            return new List<CommunicationTemplateInventoryVm>
            {
                new()
                {
                    FlowName = T("CommunicationTemplateInventoryInvitationFlow"),
                    TemplateSurface = T("CommunicationTemplateInventoryInvitationSurface"),
                    SubjectSource = T("CommunicationTemplateInventoryInvitationSubjectSource"),
                    BodySource = T("CommunicationTemplateInventoryInvitationBodySource"),
                    CurrentSubjectTemplate = SummarizeTemplate(settings.BusinessInvitationEmailSubjectTemplate, T("CommunicationTemplateInventoryInvitationSubjectFallback")),
                    CurrentBodyTemplate = SummarizeTemplate(settings.BusinessInvitationEmailBodyTemplate, T("CommunicationTemplateInventoryInvitationBodyFallback")),
                    SupportedTokens = "{recipient_name}, {business_name}, {invitation_link}, {support_email}",
                    OperatorControl = T("CommunicationTemplateInventoryInvitationOperatorControl"),
                    AuditFlowKey = "BusinessInvitation",
                    PreviewFlowKey = "BusinessInvitation",
                    OperatorActionLabel = T("OpenInvitations"),
                    OperatorActionTarget = "Invitations",
                    NextStep = T("CommunicationTemplateInventoryInvitationNextStep")
                },
                new()
                {
                    FlowName = T("CommunicationTemplateInventoryActivationFlow"),
                    TemplateSurface = T("CommunicationTemplateInventoryActivationSurface"),
                    SubjectSource = T("CommunicationTemplateInventoryActivationSubjectSource"),
                    BodySource = T("CommunicationTemplateInventoryActivationBodySource"),
                    CurrentSubjectTemplate = SummarizeTemplate(settings.AccountActivationEmailSubjectTemplate, T("CommunicationTemplateInventoryActivationSubjectFallback")),
                    CurrentBodyTemplate = SummarizeTemplate(settings.AccountActivationEmailBodyTemplate, T("CommunicationTemplateInventoryActivationBodyFallback")),
                    SupportedTokens = "{recipient_name}, {confirmation_link}, {support_email}",
                    OperatorControl = T("CommunicationTemplateInventoryActivationOperatorControl"),
                    AuditFlowKey = "AccountActivation",
                    PreviewFlowKey = "AccountActivation",
                    OperatorActionLabel = T("OpenUsers"),
                    OperatorActionTarget = "Users",
                    NextStep = T("CommunicationTemplateInventoryActivationNextStep")
                },
                new()
                {
                    FlowName = T("CommunicationTemplateInventoryPasswordResetFlow"),
                    TemplateSurface = T("CommunicationTemplateInventoryPasswordResetSurface"),
                    SubjectSource = T("CommunicationTemplateInventoryPasswordResetSubjectSource"),
                    BodySource = T("CommunicationTemplateInventoryPasswordResetBodySource"),
                    CurrentSubjectTemplate = SummarizeTemplate(settings.PasswordResetEmailSubjectTemplate, T("CommunicationTemplateInventoryPasswordResetSubjectFallback")),
                    CurrentBodyTemplate = SummarizeTemplate(settings.PasswordResetEmailBodyTemplate, T("CommunicationTemplateInventoryPasswordResetBodyFallback")),
                    SupportedTokens = "{recipient_name}, {reset_link}, {support_email}",
                    OperatorControl = T("CommunicationTemplateInventoryPasswordResetOperatorControl"),
                    AuditFlowKey = "PasswordReset",
                    PreviewFlowKey = "PasswordReset",
                    OperatorActionLabel = T("OpenUsers"),
                    OperatorActionTarget = "Users",
                    NextStep = T("CommunicationTemplateInventoryPasswordResetNextStep")
                },
                new()
                {
                    FlowName = T("CommunicationTemplateInventoryPhoneVerificationFlow"),
                    TemplateSurface = T("CommunicationTemplateInventoryPhoneVerificationSurface"),
                    SubjectSource = T("CommunicationTemplateInventoryPhoneVerificationSubjectSource"),
                    BodySource = T("CommunicationTemplateInventoryPhoneVerificationBodySource"),
                    CurrentSubjectTemplate = T("CommunicationTemplateInventoryPhoneVerificationNoSubject"),
                    CurrentBodyTemplate = $"{SummarizeTemplate(settings.PhoneVerificationSmsTemplate, T("CommunicationTemplateInventoryPhoneVerificationSmsFallback"))} / {SummarizeTemplate(settings.PhoneVerificationWhatsAppTemplate, T("CommunicationTemplateInventoryPhoneVerificationWhatsAppFallback"))}",
                    SupportedTokens = "{phone_e164}, {token}, {expires_at_utc}",
                    OperatorControl = T("CommunicationTemplateInventoryPhoneVerificationOperatorControl"),
                    AuditFlowKey = string.Empty,
                    PreviewFlowKey = "PhoneVerification",
                    OperatorActionLabel = T("BusinessCommunicationOpenPolicyAction"),
                    OperatorActionTarget = "SiteSettings",
                    NextStep = T("CommunicationTemplateInventoryPhoneVerificationNextStep")
                },
                new()
                {
                    FlowName = T("CommunicationTemplateInventoryAdminTestFlow"),
                    TemplateSurface = T("CommunicationTemplateInventoryAdminTestSurface"),
                    SubjectSource = T("CommunicationTemplateInventoryAdminTestSubjectSource"),
                    BodySource = T("CommunicationTemplateInventoryAdminTestBodySource"),
                    CurrentSubjectTemplate = SummarizeTemplate(settings.CommunicationTestEmailSubjectTemplate, T("CommunicationTemplateInventoryAdminTestSubjectFallback")),
                    CurrentBodyTemplate = $"{SummarizeTemplate(settings.CommunicationTestEmailBodyTemplate, T("CommunicationTemplateInventoryAdminTestEmailBodyFallback"))} / {SummarizeTemplate(settings.CommunicationTestSmsTemplate, T("CommunicationTemplateInventoryAdminTestSmsBodyFallback"))} / {SummarizeTemplate(settings.CommunicationTestWhatsAppTemplate, T("CommunicationTemplateInventoryAdminTestWhatsAppBodyFallback"))}",
                    SupportedTokens = "{channel}, {requested_by}, {attempted_at_utc}, {test_target}, {transport_state}",
                    OperatorControl = T("CommunicationTemplateInventoryAdminTestOperatorControl"),
                    AuditFlowKey = "AdminCommunicationTest",
                    PreviewFlowKey = "AdminCommunicationTest",
                    OperatorActionLabel = T("CommunicationResendPolicyOpenAuditLog"),
                    OperatorActionTarget = "EmailAudits",
                    NextStep = T("CommunicationTemplateInventoryAdminTestNextStep")
                },
                new()
                {
                    FlowName = T("CommunicationTemplateInventoryAdminAlertsFlow"),
                    TemplateSurface = T("CommunicationTemplateInventoryAdminAlertsSurface"),
                    SubjectSource = T("CommunicationTemplateInventoryAdminAlertsNotCentralized"),
                    BodySource = T("CommunicationTemplateInventoryAdminAlertsNotCentralized"),
                    CurrentSubjectTemplate = T("CommunicationTemplateInventoryAdminAlertsNoSharedSurface"),
                    CurrentBodyTemplate = T("CommunicationTemplateInventoryAdminAlertsNoSharedSurface"),
                    SupportedTokens = T("CommunicationTemplateInventoryAdminAlertsNotCentralized"),
                    OperatorControl = T("CommunicationTemplateInventoryAdminAlertsOperatorControl"),
                    AuditFlowKey = string.Empty,
                    PreviewFlowKey = string.Empty,
                    OperatorActionLabel = T("CommunicationResendPolicyOpenAlertSettings"),
                    OperatorActionTarget = "AdminAlerts",
                    NextStep = T("CommunicationTemplateInventoryAdminAlertsNextStep")
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

        private string DescribeCommunicationTransportState(bool isReady)
        {
            return isReady ? T("Ready") : T("CommunicationTransportStateNotReady");
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

        private List<CommunicationTemplatePreviewItemVm> BuildTemplatePreviews(SiteSettingDto settings, string? flowKey, string? channel, DateTime previewedAtUtc)
        {
            var emailSample = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["email"] = "operator@example.com",
                ["token"] = "731904",
                ["expires_at_utc"] = previewedAtUtc.AddHours(2).ToString("yyyy-MM-dd HH:mm:ss"),
                ["business_name"] = "Darwin Demo Store",
                ["acceptance_link_html"] = "<a href=\"https://admin.example.test/invitations/accept\">Accept invitation</a>",
                ["invitation_intro_html"] = "You have been invited to manage Darwin Demo Store.",
                ["support_email"] = settings.SmtpFromAddress ?? "support@example.com",
                ["recipient_name"] = "Operator"
            };

            var items = new List<CommunicationTemplatePreviewItemVm>
            {
                BuildEmailTemplatePreview(
                    flowKey: "BusinessInvitation",
                    flowName: T("CommunicationDetailsActiveFlowInvitation"),
                    templateKey: "BusinessInvitationEmail",
                    targetSurface: T("CommunicationTemplateInventoryInvitationSurface"),
                    subjectTemplate: settings.BusinessInvitationEmailSubjectTemplate,
                    subjectFallback: T("CommunicationTemplateInventoryInvitationSubjectFallback"),
                    bodyTemplate: settings.BusinessInvitationEmailBodyTemplate,
                    bodyFallback: T("CommunicationTemplateInventoryInvitationBodyFallback"),
                    supportedTokens: "{business_name}, {acceptance_link_html}, {invitation_intro_html}, {token}, {expires_at_utc}, {email}, {support_email}",
                    sample: emailSample),
                BuildEmailTemplatePreview(
                    flowKey: "AccountActivation",
                    flowName: T("CommunicationDetailsActiveFlowActivation"),
                    templateKey: "AccountActivationEmail",
                    targetSurface: T("CommunicationTemplateInventoryActivationSurface"),
                    subjectTemplate: settings.AccountActivationEmailSubjectTemplate,
                    subjectFallback: T("CommunicationTemplateInventoryActivationSubjectFallback"),
                    bodyTemplate: settings.AccountActivationEmailBodyTemplate,
                    bodyFallback: T("CommunicationTemplateInventoryActivationBodyFallback"),
                    supportedTokens: "{email}, {token}, {expires_at_utc}, {recipient_name}, {support_email}",
                    sample: emailSample),
                BuildEmailTemplatePreview(
                    flowKey: "PasswordReset",
                    flowName: T("CommunicationTemplateInventoryPasswordResetFlow"),
                    templateKey: "PasswordResetEmail",
                    targetSurface: T("CommunicationTemplateInventoryPasswordResetSurface"),
                    subjectTemplate: settings.PasswordResetEmailSubjectTemplate,
                    subjectFallback: T("CommunicationTemplateInventoryPasswordResetSubjectFallback"),
                    bodyTemplate: settings.PasswordResetEmailBodyTemplate,
                    bodyFallback: T("CommunicationTemplateInventoryPasswordResetBodyFallback"),
                    supportedTokens: "{email}, {token}, {expires_at_utc}, {recipient_name}, {support_email}",
                    sample: emailSample),
                BuildEmailTemplatePreview(
                    flowKey: "AdminCommunicationTest",
                    flowName: T("CommunicationTemplateInventoryAdminTestFlow"),
                    templateKey: "AdminCommunicationTestEmail",
                    targetSurface: settings.CommunicationTestInboxEmail ?? T("CommunicationChannelFamilyReservedTestTargetMissing"),
                    subjectTemplate: settings.CommunicationTestEmailSubjectTemplate,
                    subjectFallback: T("CommunicationTemplateInventoryAdminTestSubjectFallback"),
                    bodyTemplate: settings.CommunicationTestEmailBodyTemplate,
                    bodyFallback: T("CommunicationTemplateInventoryAdminTestEmailBodyFallback"),
                    supportedTokens: "{channel}, {requested_by}, {attempted_at_utc}, {test_target}, {transport_state}",
                    sample: BuildCommunicationTestPlaceholders(DescribeCommunicationChannel("Email"), T("CommunicationChannelFamilyOperatorPlaceholder"), previewedAtUtc, settings.CommunicationTestInboxEmail ?? "operator@example.com", DescribeCommunicationTransportState(true))),
                BuildChannelTemplatePreview(
                    flowKey: "PhoneVerification",
                    flowName: T("CommunicationTemplateInventoryPhoneVerificationFlow"),
                    channel: "SMS",
                    templateKey: "PhoneVerificationSms",
                    targetSurface: "+4915112345678",
                    template: settings.PhoneVerificationSmsTemplate,
                    fallback: T("CommunicationTemplateInventoryPhoneVerificationSmsFallback"),
                    supportedTokens: "{phone_e164}, {token}, {expires_at_utc}",
                    sample: BuildPhoneVerificationPlaceholders("+4915112345678", "731904", previewedAtUtc.AddMinutes(10))),
                BuildChannelTemplatePreview(
                    flowKey: "PhoneVerification",
                    flowName: T("CommunicationTemplateInventoryPhoneVerificationFlow"),
                    channel: "WhatsApp",
                    templateKey: "PhoneVerificationWhatsApp",
                    targetSurface: "+4915112345678",
                    template: settings.PhoneVerificationWhatsAppTemplate,
                    fallback: T("CommunicationTemplateInventoryPhoneVerificationWhatsAppFallback"),
                    supportedTokens: "{phone_e164}, {token}, {expires_at_utc}",
                    sample: BuildPhoneVerificationPlaceholders("+4915112345678", "731904", previewedAtUtc.AddMinutes(10))),
                BuildChannelTemplatePreview(
                    flowKey: "AdminCommunicationTest",
                    flowName: T("CommunicationTemplateInventoryAdminTestFlow"),
                    channel: "SMS",
                    templateKey: "AdminCommunicationTestSms",
                    targetSurface: settings.CommunicationTestSmsRecipientE164 ?? T("CommunicationChannelFamilyReservedTestTargetMissing"),
                    template: settings.CommunicationTestSmsTemplate,
                    fallback: T("CommunicationTemplateInventoryAdminTestSmsBodyFallback"),
                    supportedTokens: "{channel}, {requested_by}, {attempted_at_utc}, {test_target}, {transport_state}",
                    sample: BuildCommunicationTestPlaceholders(DescribeCommunicationChannel("SMS"), T("CommunicationChannelFamilyOperatorPlaceholder"), previewedAtUtc, settings.CommunicationTestSmsRecipientE164 ?? "+4915112345678", DescribeCommunicationTransportState(true))),
                BuildChannelTemplatePreview(
                    flowKey: "AdminCommunicationTest",
                    flowName: T("CommunicationTemplateInventoryAdminTestFlow"),
                    channel: "WhatsApp",
                    templateKey: "AdminCommunicationTestWhatsApp",
                    targetSurface: settings.CommunicationTestWhatsAppRecipientE164 ?? T("CommunicationChannelFamilyReservedTestTargetMissing"),
                    template: settings.CommunicationTestWhatsAppTemplate,
                    fallback: T("CommunicationTemplateInventoryAdminTestWhatsAppBodyFallback"),
                    supportedTokens: "{channel}, {requested_by}, {attempted_at_utc}, {test_target}, {transport_state}",
                    sample: BuildCommunicationTestPlaceholders(DescribeCommunicationChannel("WhatsApp"), T("CommunicationChannelFamilyOperatorPlaceholder"), previewedAtUtc, settings.CommunicationTestWhatsAppRecipientE164 ?? "+4915112345678", DescribeCommunicationTransportState(true)))
            };

            return items
                .Where(x => string.IsNullOrWhiteSpace(flowKey) || string.Equals(x.FlowKey, flowKey, StringComparison.OrdinalIgnoreCase))
                .Where(x => string.IsNullOrWhiteSpace(channel) || string.Equals(x.Channel, channel, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private CommunicationTemplatePreviewItemVm BuildEmailTemplatePreview(
            string flowKey,
            string flowName,
            string templateKey,
            string targetSurface,
            string? subjectTemplate,
            string subjectFallback,
            string? bodyTemplate,
            string bodyFallback,
            string supportedTokens,
            IReadOnlyDictionary<string, string?> sample)
        {
            return new CommunicationTemplatePreviewItemVm
            {
                FlowKey = flowKey,
                FlowName = flowName,
                Channel = "Email",
                TemplateKey = templateKey,
                TargetSurface = targetSurface,
                SubjectTemplate = string.IsNullOrWhiteSpace(subjectTemplate) ? subjectFallback : subjectTemplate,
                BodyTemplate = string.IsNullOrWhiteSpace(bodyTemplate) ? bodyFallback : bodyTemplate,
                RenderedSubject = RenderTemplate(subjectTemplate, subjectFallback, sample),
                RenderedBody = RenderTemplate(bodyTemplate, bodyFallback, sample),
                SupportedTokens = supportedTokens,
                SampleData = BuildSampleDataPreview(sample),
                EditFragment = "site-settings-communications-policy",
                AuditFlowKey = flowKey,
                IsEmail = true
            };
        }

        private CommunicationTemplatePreviewItemVm BuildChannelTemplatePreview(
            string flowKey,
            string flowName,
            string channel,
            string templateKey,
            string targetSurface,
            string? template,
            string fallback,
            string supportedTokens,
            IReadOnlyDictionary<string, string?> sample)
        {
            return new CommunicationTemplatePreviewItemVm
            {
                FlowKey = flowKey,
                FlowName = flowName,
                Channel = channel,
                TemplateKey = templateKey,
                TargetSurface = targetSurface,
                SubjectTemplate = T("CommunicationTemplatePreviewNoSubject"),
                BodyTemplate = string.IsNullOrWhiteSpace(template) ? fallback : template,
                RenderedSubject = T("CommunicationTemplatePreviewNoSubject"),
                RenderedBody = RenderTemplate(template, fallback, sample),
                SupportedTokens = supportedTokens,
                SampleData = BuildSampleDataPreview(sample),
                EditFragment = "site-settings-communications-policy",
                AuditFlowKey = flowKey,
                IsEmail = false
            };
        }

        private static string BuildSampleDataPreview(IReadOnlyDictionary<string, string?> sample)
        {
            return string.Join(", ", sample.Select(x => $"{x.Key}={x.Value}"));
        }

        private List<CommunicationResendPolicyVm> BuildResendPolicies()
        {
            return new List<CommunicationResendPolicyVm>
            {
                new()
                {
                    FlowName = T("CommunicationResendPolicyInvitationFlow"),
                    CurrentSafeAction = T("CommunicationResendPolicyInvitationSafeAction"),
                    GenericRetryStatus = T("CommunicationResendPolicyInvitationRetryStatus"),
                    OperatorEntryPoint = T("CommunicationResendPolicyInvitationEntryPoint"),
                    AuditFlowKey = "BusinessInvitation",
                    OperatorActionLabel = T("OpenInvitations"),
                    OperatorActionTarget = "Invitations",
                    EscalationRule = T("CommunicationResendPolicyInvitationEscalation")
                },
                new()
                {
                    FlowName = T("CommunicationResendPolicyActivationFlow"),
                    CurrentSafeAction = T("CommunicationResendPolicyActivationSafeAction"),
                    GenericRetryStatus = T("CommunicationResendPolicyActivationRetryStatus"),
                    OperatorEntryPoint = T("CommunicationResendPolicyActivationEntryPoint"),
                    AuditFlowKey = "AccountActivation",
                    OperatorActionLabel = T("OpenUsers"),
                    OperatorActionTarget = "Users",
                    EscalationRule = T("CommunicationResendPolicyActivationEscalation")
                },
                new()
                {
                    FlowName = T("CommunicationResendPolicyPasswordResetFlow"),
                    CurrentSafeAction = T("CommunicationResendPolicyPasswordResetSafeAction"),
                    GenericRetryStatus = T("CommunicationResendPolicyPasswordResetRetryStatus"),
                    OperatorEntryPoint = T("CommunicationResendPolicyPasswordResetEntryPoint"),
                    AuditFlowKey = "PasswordReset",
                    OperatorActionLabel = T("OpenUsers"),
                    OperatorActionTarget = "Users",
                    EscalationRule = T("CommunicationResendPolicyPasswordResetEscalation")
                },
                new()
                {
                    FlowName = T("CommunicationResendPolicyPhoneVerificationFlow"),
                    CurrentSafeAction = T("CommunicationResendPolicyPhoneVerificationSafeAction"),
                    GenericRetryStatus = T("CommunicationResendPolicyPhoneVerificationRetryStatus"),
                    OperatorEntryPoint = T("CommunicationResendPolicyPhoneVerificationEntryPoint"),
                    AuditFlowKey = string.Empty,
                    OperatorActionLabel = T("BusinessCommunicationOpenPolicyAction"),
                    OperatorActionTarget = "SiteSettings",
                    EscalationRule = T("CommunicationResendPolicyPhoneVerificationEscalation")
                },
                new()
                {
                    FlowName = T("CommunicationResendPolicyAdminTestFlow"),
                    CurrentSafeAction = T("CommunicationResendPolicyAdminTestSafeAction"),
                    GenericRetryStatus = T("CommunicationResendPolicyAdminTestRetryStatus"),
                    OperatorEntryPoint = T("CommunicationResendPolicyAdminTestEntryPoint"),
                    AuditFlowKey = "AdminCommunicationTest",
                    OperatorActionLabel = T("CommunicationResendPolicyOpenAuditLog"),
                    OperatorActionTarget = "EmailAudits",
                    EscalationRule = T("CommunicationResendPolicyAdminTestEscalation")
                },
                new()
                {
                    FlowName = T("CommunicationResendPolicyAdminAlertsFlow"),
                    CurrentSafeAction = T("CommunicationResendPolicyAdminAlertsSafeAction"),
                    GenericRetryStatus = T("CommunicationResendPolicyAdminAlertsRetryStatus"),
                    OperatorEntryPoint = T("CommunicationResendPolicyAdminAlertsEntryPoint"),
                    AuditFlowKey = string.Empty,
                    OperatorActionLabel = T("CommunicationResendPolicyOpenAlertSettings"),
                    OperatorActionTarget = "AdminAlerts",
                    EscalationRule = T("CommunicationResendPolicyAdminAlertsEscalation")
                }
            };
        }

        private List<ChannelMessageFamilyVm> BuildChannelTemplateFamilies(SiteSettingDto settings, string? flowKey)
        {
            var nowUtc = _clock.UtcNow;
            var phoneVerificationExpiresAtUtc = nowUtc.AddMinutes(10);
            var families = new List<ChannelMessageFamilyVm>();

            if (string.IsNullOrWhiteSpace(flowKey) || string.Equals(flowKey, "PhoneVerification", StringComparison.OrdinalIgnoreCase))
            {
                families.Add(new ChannelMessageFamilyVm
                {
                    FamilyName = T("CommunicationChannelFamilyPhoneVerificationName"),
                    FamilyKey = "PhoneVerification",
                    Channel = "SMS",
                    ChannelValue = "SMS",
                    CurrentTemplate = SummarizeTemplate(settings.PhoneVerificationSmsTemplate, T("CommunicationTemplateInventoryPhoneVerificationSmsFallback")),
                    ExamplePreview = SummarizeTemplate(
                        RenderTemplate(
                            settings.PhoneVerificationSmsTemplate,
                            T("CommunicationTemplateInventoryPhoneVerificationSmsFallback"),
                            BuildPhoneVerificationPlaceholders("+4915112345678", "731904", phoneVerificationExpiresAtUtc)),
                        T("CommunicationChannelFamilyPhoneVerificationSmsPreviewFallback")),
                    SupportedTokens = "{phone_e164}, {token}, {expires_at_utc}",
                    TargetSurface = T("CommunicationChannelFamilyPhoneVerificationTargetSurface"),
                    PolicyNote = string.Format(CultureInfo.InvariantCulture, T("CommunicationChannelFamilyPhoneVerificationPolicyNote"), DescribePreferredPhoneVerificationChannel(settings.PhoneVerificationPreferredChannel), settings.PhoneVerificationAllowFallback ? T("Enabled") : T("Disabled")),
                    SafeUsageNote = T("CommunicationChannelFamilyPhoneVerificationSafeUsage"),
                    RolloutBoundary = T("CommunicationChannelFamilyPhoneVerificationRolloutBoundary"),
                    ActionLabel = T("BusinessCommunicationOpenPolicyAction"),
                    ActionTarget = "SiteSettings",
                    ActionFragment = "site-settings-communications-policy"
                });
                families.Add(new ChannelMessageFamilyVm
                {
                    FamilyName = T("CommunicationChannelFamilyPhoneVerificationName"),
                    FamilyKey = "PhoneVerification",
                    Channel = "WhatsApp",
                    ChannelValue = "WhatsApp",
                    CurrentTemplate = SummarizeTemplate(settings.PhoneVerificationWhatsAppTemplate, T("CommunicationTemplateInventoryPhoneVerificationWhatsAppFallback")),
                    ExamplePreview = SummarizeTemplate(
                        RenderTemplate(
                            settings.PhoneVerificationWhatsAppTemplate,
                            T("CommunicationTemplateInventoryPhoneVerificationWhatsAppFallback"),
                            BuildPhoneVerificationPlaceholders("+4915112345678", "731904", phoneVerificationExpiresAtUtc)),
                        T("CommunicationChannelFamilyPhoneVerificationWhatsAppPreviewFallback")),
                    SupportedTokens = "{phone_e164}, {token}, {expires_at_utc}",
                    TargetSurface = T("CommunicationChannelFamilyPhoneVerificationTargetSurface"),
                    PolicyNote = string.Format(CultureInfo.InvariantCulture, T("CommunicationChannelFamilyPhoneVerificationPolicyNote"), DescribePreferredPhoneVerificationChannel(settings.PhoneVerificationPreferredChannel), settings.PhoneVerificationAllowFallback ? T("Enabled") : T("Disabled")),
                    SafeUsageNote = T("CommunicationChannelFamilyPhoneVerificationSafeUsage"),
                    RolloutBoundary = T("CommunicationChannelFamilyPhoneVerificationRolloutBoundary"),
                    ActionLabel = T("BusinessCommunicationOpenPolicyAction"),
                    ActionTarget = "SiteSettings",
                    ActionFragment = "site-settings-communications-policy"
                });
            }

            if (string.IsNullOrWhiteSpace(flowKey) || string.Equals(flowKey, "AdminCommunicationTest", StringComparison.OrdinalIgnoreCase))
            {
                families.Add(new ChannelMessageFamilyVm
                {
                    FamilyName = T("CommunicationChannelFamilyAdminTestName"),
                    FamilyKey = "AdminCommunicationTest",
                    Channel = "SMS",
                    ChannelValue = "SMS",
                    CurrentTemplate = SummarizeTemplate(settings.CommunicationTestSmsTemplate, T("CommunicationTemplateInventoryAdminTestSmsBodyFallback")),
                    ExamplePreview = SummarizeTemplate(
                        RenderTemplate(
                            settings.CommunicationTestSmsTemplate,
                            T("CommunicationTemplateInventoryAdminTestSmsBodyFallback"),
                            BuildCommunicationTestPlaceholders(DescribeCommunicationChannel("SMS"), T("CommunicationChannelFamilyOperatorPlaceholder"), nowUtc, settings.CommunicationTestSmsRecipientE164 ?? "+4915112345678", T("Ready"))),
                        T("CommunicationChannelFamilyAdminTestSmsPreviewFallback")),
                    SupportedTokens = "{channel}, {requested_by}, {attempted_at_utc}, {test_target}, {transport_state}",
                    TargetSurface = settings.CommunicationTestSmsRecipientE164 ?? T("CommunicationChannelFamilyReservedTestTargetMissing"),
                    PolicyNote = T("CommunicationChannelFamilyAdminTestPolicyNote"),
                    SafeUsageNote = T("CommunicationChannelFamilyAdminTestSmsSafeUsage"),
                    RolloutBoundary = T("CommunicationChannelFamilyAdminTestRolloutBoundary"),
                    ActionLabel = T("CommunicationChannelFamilyOpenTestTargetsAction"),
                    ActionTarget = "SiteSettings",
                    ActionFragment = "site-settings-communications-policy"
                });
                families.Add(new ChannelMessageFamilyVm
                {
                    FamilyName = T("CommunicationChannelFamilyAdminTestName"),
                    FamilyKey = "AdminCommunicationTest",
                    Channel = "WhatsApp",
                    ChannelValue = "WhatsApp",
                    CurrentTemplate = SummarizeTemplate(settings.CommunicationTestWhatsAppTemplate, T("CommunicationTemplateInventoryAdminTestWhatsAppBodyFallback")),
                    ExamplePreview = SummarizeTemplate(
                        RenderTemplate(
                            settings.CommunicationTestWhatsAppTemplate,
                            T("CommunicationTemplateInventoryAdminTestWhatsAppBodyFallback"),
                            BuildCommunicationTestPlaceholders(DescribeCommunicationChannel("WhatsApp"), T("CommunicationChannelFamilyOperatorPlaceholder"), nowUtc, settings.CommunicationTestWhatsAppRecipientE164 ?? "+4915112345678", T("Ready"))),
                        T("CommunicationChannelFamilyAdminTestWhatsAppPreviewFallback")),
                    SupportedTokens = "{channel}, {requested_by}, {attempted_at_utc}, {test_target}, {transport_state}",
                    TargetSurface = settings.CommunicationTestWhatsAppRecipientE164 ?? T("CommunicationChannelFamilyReservedTestTargetMissing"),
                    PolicyNote = T("CommunicationChannelFamilyAdminTestPolicyNote"),
                    SafeUsageNote = T("CommunicationChannelFamilyAdminTestWhatsAppSafeUsage"),
                    RolloutBoundary = T("CommunicationChannelFamilyAdminTestRolloutBoundary"),
                    ActionLabel = T("CommunicationChannelFamilyOpenTestTargetsAction"),
                    ActionTarget = "SiteSettings",
                    ActionFragment = "site-settings-communications-policy"
                });
            }

            return families;
        }

        private string DescribePreferredPhoneVerificationChannel(string? preferredChannel)
        {
            return string.Equals(preferredChannel, "WhatsApp", StringComparison.OrdinalIgnoreCase)
                ? DescribeCommunicationChannel("WhatsApp")
                : DescribeCommunicationChannel("SMS");
        }

        private string DescribeCommunicationChannel(string? channel)
        {
            return channel switch
            {
                "Email" => T("CommunicationBuiltInFlowEmailChannel"),
                "WhatsApp" => T("BusinessCommunicationWhatsAppShort"),
                _ => T("BusinessCommunicationSmsShort")
            };
        }

        private string DescribeDeliveryStatus(string? status)
        {
            return status switch
            {
                "Sent" => T("Sent"),
                "Failed" => T("Failed"),
                "Pending" => T("Pending"),
                _ => string.IsNullOrWhiteSpace(status) ? T("CommonUnclassified") : T(status)
            };
        }

        private string DescribeProviderCallbackStatus(string? status)
        {
            return status switch
            {
                "Pending" => T("Pending"),
                "Failed" => T("Failed"),
                "Processed" => T("Processed"),
                _ => string.IsNullOrWhiteSpace(status) ? T("CommonUnclassified") : T(status)
            };
        }

        private string DescribeBuiltInFlowChannel(string? channelGroup)
        {
            return channelGroup switch
            {
                "Email" => DescribeCommunicationChannel("Email"),
                "SmsWhatsApp" => T("CommunicationBuiltInFlowSmsWhatsAppChannel"),
                "EmailSmsWhatsApp" => T("CommunicationBuiltInFlowEmailSmsWhatsAppChannel"),
                "EmailSmsWhatsAppCompact" => T("CommunicationBuiltInFlowEmailSmsWhatsAppCompactChannel"),
                _ => string.IsNullOrWhiteSpace(channelGroup) ? T("CommonUnclassified") : T(channelGroup)
            };
        }

        private static Dictionary<string, string?> BuildPhoneVerificationPlaceholders(
            string phoneE164,
            string token,
            DateTime expiresAtUtc)
        {
            return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["phone_e164"] = phoneE164,
                ["token"] = token,
                ["expires_at_utc"] = expiresAtUtc.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }

        private List<string> BuildActiveFlowNames(BusinessCommunicationProfileDto profile)
        {
            var flows = new List<string>();

            if (profile.OpenInvitationCount > 0)
            {
                flows.Add(T("CommunicationDetailsActiveFlowInvitation"));
            }

            if (profile.PendingActivationMemberCount > 0)
            {
                flows.Add(T("CommunicationDetailsActiveFlowActivation"));
            }

            if (profile.CustomerEmailNotificationsEnabled)
            {
                flows.Add(T("CommunicationDetailsActiveFlowPasswordReset"));
            }

            if (profile.OperationalAlertEmailsEnabled)
            {
                flows.Add(T("CommunicationDetailsActiveFlowAdminAlerts"));
            }

            return flows;
        }

        private List<string> BuildReadinessIssues(
            BusinessCommunicationProfileDto profile,
            bool emailTransportConfigured,
            bool adminAlertRoutingConfigured)
        {
            var issues = new List<string>();

            if (profile.MissingSupportEmail)
            {
                issues.Add(T("CommunicationDetailsReadinessIssueMissingSupportEmail"));
            }

            if (profile.MissingSenderIdentity)
            {
                issues.Add(T("CommunicationDetailsReadinessIssueMissingSenderIdentity"));
            }

            if ((profile.CustomerEmailNotificationsEnabled || profile.CustomerMarketingEmailsEnabled) && !emailTransportConfigured)
            {
                issues.Add(T("CommunicationDetailsReadinessIssueMissingSmtp"));
            }

            if (profile.OperationalAlertEmailsEnabled && !adminAlertRoutingConfigured)
            {
                issues.Add(T("CommunicationDetailsReadinessIssueMissingAdminRouting"));
            }

            if (string.Equals(profile.OperationalStatus, "PendingApproval", System.StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(T("CommunicationDetailsReadinessIssuePendingApproval"));
            }

            if (!profile.IsActive)
            {
                issues.Add(T("CommunicationDetailsReadinessIssueInactive"));
            }

            return issues;
        }

        private List<string> BuildRecommendedActions(
            BusinessCommunicationProfileDto profile,
            bool emailTransportConfigured,
            bool adminAlertRoutingConfigured)
        {
            var actions = new List<string>();

            if (profile.MissingSupportEmail || profile.MissingSenderIdentity)
            {
                actions.Add(T("CommunicationDetailsRecommendedActionCompleteBusinessDefaults"));
            }

            if ((profile.CustomerEmailNotificationsEnabled || profile.CustomerMarketingEmailsEnabled) && !emailTransportConfigured)
            {
                actions.Add(T("CommunicationDetailsRecommendedActionOpenSmtp"));
            }

            if (profile.OperationalAlertEmailsEnabled && !adminAlertRoutingConfigured)
            {
                actions.Add(T("CommunicationDetailsRecommendedActionConfigureAdminRouting"));
            }

            if (profile.PendingActivationMemberCount > 0)
            {
                actions.Add(T("CommunicationDetailsRecommendedActionReviewMembers"));
            }

            if (profile.OpenInvitationCount > 0)
            {
                actions.Add(T("CommunicationDetailsRecommendedActionReviewInvitations"));
            }

            if (profile.LockedMemberCount > 0)
            {
                actions.Add(T("CommunicationDetailsRecommendedActionReviewLockedMembers"));
            }

            if (string.Equals(profile.OperationalStatus, "PendingApproval", System.StringComparison.OrdinalIgnoreCase))
            {
                actions.Add(T("CommunicationDetailsRecommendedActionCompleteBeforeApproval"));
            }

            if (actions.Count == 0)
            {
                actions.Add(T("CommunicationDetailsRecommendedActionNoImmediateAction"));
            }

            return actions;
        }

        private string BuildAuditRecommendedAction(EmailDispatchAuditListItemDto item)
        {
            if (item.IsQueueOperation)
            {
                return T("CommunicationChannelAwaitingWorker");
            }

            if (string.Equals(item.Status, "Sent", System.StringComparison.OrdinalIgnoreCase))
            {
                return T("CommunicationAuditRecommendedActionNoImmediateAction");
            }

            if (string.Equals(item.FlowKey, "BusinessInvitation", System.StringComparison.OrdinalIgnoreCase))
            {
                return item.BusinessId.HasValue
                    ? T("CommunicationAuditRecommendedActionInvitationBusiness")
                    : T("CommunicationAuditRecommendedActionInvitationGeneric");
            }

            if (string.Equals(item.FlowKey, "AccountActivation", System.StringComparison.OrdinalIgnoreCase))
            {
                return item.BusinessId.HasValue
                    ? T("CommunicationAuditRecommendedActionActivationBusiness")
                    : T("CommunicationAuditRecommendedActionActivationGeneric");
            }

            if (string.Equals(item.FlowKey, "PasswordReset", System.StringComparison.OrdinalIgnoreCase))
            {
                return item.BusinessId.HasValue
                    ? T("CommunicationAuditRecommendedActionPasswordResetBusiness")
                    : T("CommunicationAuditRecommendedActionPasswordResetGeneric");
            }

            return T("CommunicationAuditRecommendedActionGeneric");
        }

        private string BuildChannelAuditActionPolicyState(string? state)
        {
            return state switch
            {
                ChannelDispatchAuditVocabulary.ActionPolicyStates.CanonicalFlow => T("CommunicationChannelActionPolicyCanonicalFlow"),
                ChannelDispatchAuditVocabulary.ActionPolicyStates.Cooldown => T("CommunicationChannelActionPolicyCooldown"),
                ChannelDispatchAuditVocabulary.ActionPolicyStates.RetryReady => T("CommunicationChannelActionPolicyRetryReady"),
                ChannelDispatchAuditVocabulary.ActionPolicyStates.Ready => T("CommunicationChannelActionPolicyReady"),
                ChannelDispatchAuditVocabulary.ActionPolicyStates.Unsupported => T("CommunicationChannelActionPolicyUnsupported"),
                _ => string.IsNullOrWhiteSpace(state) ? string.Empty : T(state)
            };
        }

        private string? BuildChannelAuditActionBlockedReason(ChannelDispatchAuditListItemDto item)
        {
            if (string.IsNullOrWhiteSpace(item.ActionBlockedReason))
            {
                return item.ActionBlockedReason;
            }

            if (string.Equals(item.FlowKey, ChannelDispatchAuditVocabulary.FlowKeys.PhoneVerification, System.StringComparison.OrdinalIgnoreCase))
            {
                return T("CommunicationChannelActionBlockedCanonicalFlow");
            }

            if (string.Equals(item.ActionPolicyState, ChannelDispatchAuditVocabulary.ActionPolicyStates.Cooldown, System.StringComparison.OrdinalIgnoreCase))
            {
                return T("CommunicationChannelActionBlockedCooldown");
            }

            if (string.Equals(item.ActionPolicyState, ChannelDispatchAuditVocabulary.ActionPolicyStates.Unsupported, System.StringComparison.OrdinalIgnoreCase))
            {
                return T("CommunicationChannelActionBlockedUnsupported");
            }

            return T(item.ActionBlockedReason);
        }

        private string? BuildChannelAuditEscalationReason(ChannelDispatchAuditListItemDto item)
        {
            if (string.IsNullOrWhiteSpace(item.EscalationReason))
            {
                return item.EscalationReason;
            }

            if (string.Equals(item.FlowKey, ChannelDispatchAuditVocabulary.FlowKeys.PhoneVerification, System.StringComparison.OrdinalIgnoreCase))
            {
                return T("CommunicationChannelEscalationPhoneVerification");
            }

            if (string.Equals(item.FlowKey, ChannelDispatchAuditVocabulary.FlowKeys.AdminCommunicationTest, System.StringComparison.OrdinalIgnoreCase))
            {
                return T("CommunicationChannelEscalationAdminTest");
            }

            return T(item.EscalationReason);
        }

        private string BuildChannelProviderRecommendedAction(string recommendedAction)
        {
            return recommendedAction switch
            {
                ChannelDispatchAuditVocabulary.Guidance.ProviderRecommendedVerificationElevated => T("CommunicationChannelProviderRecommendedVerificationElevated"),
                ChannelDispatchAuditVocabulary.Guidance.ProviderRecommendedVerificationStable => T("CommunicationChannelProviderRecommendedVerificationStable"),
                ChannelDispatchAuditVocabulary.Guidance.ProviderRecommendedAdminTestElevated => T("CommunicationChannelProviderRecommendedAdminTestElevated"),
                ChannelDispatchAuditVocabulary.Guidance.ProviderRecommendedAdminTestStable => T("CommunicationChannelProviderRecommendedAdminTestStable"),
                ChannelDispatchAuditVocabulary.Guidance.ProviderRecommendedGenericPending => T("CommunicationChannelProviderRecommendedGenericPending"),
                ChannelDispatchAuditVocabulary.Guidance.ProviderRecommendedGenericStable => T("CommunicationChannelProviderRecommendedGenericStable"),
                _ => string.IsNullOrWhiteSpace(recommendedAction) ? string.Empty : T(recommendedAction)
            };
        }

        private string BuildChannelProviderEscalationHint(string escalationHint)
        {
            return escalationHint switch
            {
                ChannelDispatchAuditVocabulary.Guidance.ProviderEscalationVerificationElevated => T("CommunicationChannelProviderEscalationVerificationElevated"),
                ChannelDispatchAuditVocabulary.Guidance.ProviderEscalationVerificationStable => T("CommunicationChannelProviderEscalationVerificationStable"),
                ChannelDispatchAuditVocabulary.Guidance.ProviderEscalationAdminTestElevated => T("CommunicationChannelProviderEscalationAdminTestElevated"),
                ChannelDispatchAuditVocabulary.Guidance.ProviderEscalationAdminTestStable => T("CommunicationChannelProviderEscalationAdminTestStable"),
                ChannelDispatchAuditVocabulary.Guidance.ProviderEscalationGenericElevated => T("CommunicationChannelProviderEscalationGenericElevated"),
                ChannelDispatchAuditVocabulary.Guidance.ProviderEscalationGenericStable => T("CommunicationChannelProviderEscalationGenericStable"),
                _ => string.IsNullOrWhiteSpace(escalationHint) ? string.Empty : T(escalationHint)
            };
        }

        private string BuildChannelChainRecommendedAction(string recommendedAction)
        {
            return recommendedAction switch
            {
                ChannelDispatchAuditVocabulary.Guidance.ChainRecommendedVerificationRecovered => T("CommunicationChannelChainRecommendedVerificationRecovered"),
                ChannelDispatchAuditVocabulary.Guidance.ChainRecommendedVerificationBlocked => T("CommunicationChannelChainRecommendedVerificationBlocked"),
                ChannelDispatchAuditVocabulary.Guidance.ChainRecommendedAdminTest => T("CommunicationChannelChainRecommendedAdminTest"),
                ChannelDispatchAuditVocabulary.Guidance.ChainRecommendedGenericPending => T("CommunicationChannelChainRecommendedGenericPending"),
                ChannelDispatchAuditVocabulary.Guidance.ChainRecommendedGenericStable => T("CommunicationChannelChainRecommendedGenericStable"),
                _ => string.IsNullOrWhiteSpace(recommendedAction) ? string.Empty : T(recommendedAction)
            };
        }

        private string BuildChannelChainEscalationHint(string escalationHint)
        {
            return escalationHint switch
            {
                ChannelDispatchAuditVocabulary.Guidance.ChainEscalationVerificationBlocked => T("CommunicationChannelChainEscalationVerificationBlocked"),
                ChannelDispatchAuditVocabulary.Guidance.ChainEscalationVerificationStable => T("CommunicationChannelChainEscalationVerificationStable"),
                ChannelDispatchAuditVocabulary.Guidance.ChainEscalationAdminTestBlocked => T("CommunicationChannelChainEscalationAdminTestBlocked"),
                ChannelDispatchAuditVocabulary.Guidance.ChainEscalationAdminTestStable => T("CommunicationChannelChainEscalationAdminTestStable"),
                ChannelDispatchAuditVocabulary.Guidance.ChainEscalationGenericRecovered => T("CommunicationChannelChainEscalationGenericRecovered"),
                ChannelDispatchAuditVocabulary.Guidance.ChainEscalationGenericBlocked => T("CommunicationChannelChainEscalationGenericBlocked"),
                _ => string.IsNullOrWhiteSpace(escalationHint) ? string.Empty : T(escalationHint)
            };
        }

        private string BuildEmailAuditChainStatusMix(string? statusMix)
        {
            return statusMix switch
            {
                ChannelDispatchAuditVocabulary.ChainStatusMixes.Mixed => T("CommunicationChainStatusMixed"),
                ChannelDispatchAuditVocabulary.ChainStatusMixes.OpenFailure => T("CommunicationChainStatusOpenFailure"),
                ChannelDispatchAuditVocabulary.ChainStatusMixes.FailureOnly => T("CommunicationChainStatusFailureOnly"),
                ChannelDispatchAuditVocabulary.ChainStatusMixes.PendingOnly => T("CommunicationChainStatusPendingOnly"),
                ChannelDispatchAuditVocabulary.ChainStatusMixes.SuccessOnly => T("CommunicationChainStatusSuccessOnly"),
                ChannelDispatchAuditVocabulary.ChainStatusMixes.SingleAttempt => T("CommunicationChainStatusSingleAttempt"),
                _ => statusMix ?? string.Empty
            };
        }

        private string? BuildEmailAuditRetryBlockedReason(EmailDispatchAuditListItemDto item)
        {
            if (string.IsNullOrWhiteSpace(item.RetryBlockedReason))
            {
                return item.RetryBlockedReason;
            }

            if (string.Equals(item.RetryPolicyState, "Unsupported", System.StringComparison.OrdinalIgnoreCase))
            {
                return T("CommunicationEmailRetryBlockedUnsupported");
            }

            if (string.Equals(item.RetryPolicyState, "Closed", System.StringComparison.OrdinalIgnoreCase))
            {
                return T("CommunicationEmailRetryBlockedClosed");
            }

            if (string.Equals(item.RetryPolicyState, "RateLimited", System.StringComparison.OrdinalIgnoreCase))
            {
                return string.Format(CultureInfo.InvariantCulture, T("CommunicationEmailRetryBlockedRateLimited"), item.RecentAttemptCount24h);
            }

            if (string.Equals(item.RetryPolicyState, "Cooldown", System.StringComparison.OrdinalIgnoreCase) && item.RetryAvailableAtUtc.HasValue)
            {
                return string.Format(CultureInfo.InvariantCulture, T("CommunicationEmailRetryBlockedCooldownUntil"), item.RetryAvailableAtUtc.Value);
            }

            return item.RetryBlockedReason;
        }

        private List<CommunicationFlowPlaybookVm> BuildAuditPlaybooks()
        {
            return new List<CommunicationFlowPlaybookVm>
            {
                new()
                {
                    FlowKey = "BusinessInvitation",
                    Title = T("CommunicationAuditPlaybookInvitationTitle"),
                    ScopeNote = T("CommunicationAuditPlaybookInvitationScope"),
                    AllowedAction = T("CommunicationAuditPlaybookInvitationAllowedAction"),
                    EscalationRule = T("CommunicationAuditPlaybookInvitationEscalation")
                },
                new()
                {
                    FlowKey = "AccountActivation",
                    Title = T("CommunicationAuditPlaybookActivationTitle"),
                    ScopeNote = T("CommunicationAuditPlaybookActivationScope"),
                    AllowedAction = T("CommunicationAuditPlaybookActivationAllowedAction"),
                    EscalationRule = T("CommunicationAuditPlaybookActivationEscalation")
                },
                new()
                {
                    FlowKey = "PasswordReset",
                    Title = T("CommunicationAuditPlaybookPasswordResetTitle"),
                    ScopeNote = T("CommunicationAuditPlaybookPasswordResetScope"),
                    AllowedAction = T("CommunicationAuditPlaybookPasswordResetAllowedAction"),
                    EscalationRule = T("CommunicationAuditPlaybookPasswordResetEscalation")
                },
                new()
                {
                    FlowKey = "AdminCommunicationTest",
                    Title = T("CommunicationAuditPlaybookAdminTestTitle"),
                    ScopeNote = T("CommunicationAuditPlaybookAdminTestScope"),
                    AllowedAction = T("CommunicationAuditPlaybookAdminTestAllowedAction"),
                    EscalationRule = T("CommunicationAuditPlaybookAdminTestEscalation")
                }
            };
        }
    }
}
