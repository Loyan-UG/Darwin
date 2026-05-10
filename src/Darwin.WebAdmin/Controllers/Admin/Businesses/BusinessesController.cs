using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Businesses.Commands;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Businesses.Queries;
using Darwin.Application.Billing;
using Darwin.Application.Common.DTOs;
using Darwin.Application.Identity.Commands;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Darwin.WebAdmin.Controllers.Admin;
using Darwin.WebAdmin.Security;
using Darwin.WebAdmin.Services.Admin;
using Darwin.WebAdmin.Services.Settings;
using Darwin.WebAdmin.ViewModels.Businesses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System.Security.Claims;

namespace Darwin.WebAdmin.Controllers.Admin.Businesses
{
    /// <summary>
    /// Admin controller for merchant/business onboarding and lifecycle management.
    /// Most lifecycle actions remain FullAdmin-only, while selected support actions are delegated
    /// through <see cref="PermissionKeys.ManageBusinessSupport"/>.
    /// </summary>
    [PermissionAuthorize(PermissionKeys.ManageBusinessSupport)]
    public sealed class BusinessesController : AdminBaseController
    {
        private readonly GetBusinessesPageHandler _getBusinessesPage;
        private readonly GetBusinessForEditHandler _getBusinessForEdit;
        private readonly GetBusinessOnboardingCustomerProfileHandler _getBusinessOnboardingCustomerProfile;
        private readonly GetBusinessSupportSummaryHandler _getBusinessSupportSummary;
        private readonly GetBusinessSubscriptionStatusHandler _getBusinessSubscriptionStatus;
        private readonly GetBusinessSubscriptionInvoicesPageHandler _getBusinessSubscriptionInvoicesPage;
        private readonly GetBusinessSubscriptionInvoiceOpsSummaryHandler _getBusinessSubscriptionInvoiceOpsSummary;
        private readonly GetBillingPlansHandler _getBillingPlans;
        private readonly SetCancelAtPeriodEndHandler _setCancelAtPeriodEnd;
        private readonly CreateSubscriptionCheckoutIntentHandler _createSubscriptionCheckoutIntent;
        private readonly GetEmailDispatchAuditsPageHandler _getEmailDispatchAuditsPage;
        private readonly CreateBusinessHandler _createBusiness;
        private readonly EnsureBusinessOnboardingCustomerProfileHandler _ensureBusinessOnboardingCustomerProfile;
        private readonly ProvisionBusinessOnboardingHandler _provisionBusinessOnboarding;
        private readonly UpdateBusinessHandler _updateBusiness;
        private readonly SoftDeleteBusinessHandler _deleteBusiness;
        private readonly GetBusinessLocationsPageHandler _getBusinessLocationsPage;
        private readonly GetBusinessLocationForEditHandler _getBusinessLocationForEdit;
        private readonly CreateBusinessLocationHandler _createBusinessLocation;
        private readonly UpdateBusinessLocationHandler _updateBusinessLocation;
        private readonly SoftDeleteBusinessLocationHandler _deleteBusinessLocation;
        private readonly GetBusinessMembersPageHandler _getBusinessMembersPage;
        private readonly GetBusinessMemberForEditHandler _getBusinessMemberForEdit;
        private readonly GetBusinessOwnerOverrideAuditsPageHandler _getBusinessOwnerOverrideAuditsPage;
        private readonly CreateBusinessMemberHandler _createBusinessMember;
        private readonly UpdateBusinessMemberHandler _updateBusinessMember;
        private readonly DeleteBusinessMemberHandler _deleteBusinessMember;
        private readonly GetBusinessInvitationsPageHandler _getBusinessInvitationsPage;
        private readonly CreateBusinessInvitationHandler _createBusinessInvitation;
        private readonly ResendBusinessInvitationHandler _resendBusinessInvitation;
        private readonly RevokeBusinessInvitationHandler _revokeBusinessInvitation;
        private readonly ApproveBusinessHandler _approveBusiness;
        private readonly SuspendBusinessHandler _suspendBusiness;
        private readonly ReactivateBusinessHandler _reactivateBusiness;
        private readonly RequestPasswordResetHandler _requestPasswordReset;
        private readonly RequestEmailConfirmationHandler _requestEmailConfirmation;
        private readonly ConfirmUserEmailByAdminHandler _confirmUserEmail;
        private readonly LockUserByAdminHandler _lockUser;
        private readonly UnlockUserByAdminHandler _unlockUser;
        private readonly AdminReferenceDataService _referenceData;
        private readonly ISiteSettingCache _siteSettingCache;
        private readonly IBusinessEffectiveSettingsCache _businessEffectiveSettingsCache;
        private readonly IClock _clock;

        public BusinessesController(
            GetBusinessesPageHandler getBusinessesPage,
            GetBusinessForEditHandler getBusinessForEdit,
            GetBusinessOnboardingCustomerProfileHandler getBusinessOnboardingCustomerProfile,
            GetBusinessSupportSummaryHandler getBusinessSupportSummary,
            GetBusinessSubscriptionStatusHandler getBusinessSubscriptionStatus,
            GetBusinessSubscriptionInvoicesPageHandler getBusinessSubscriptionInvoicesPage,
            GetBusinessSubscriptionInvoiceOpsSummaryHandler getBusinessSubscriptionInvoiceOpsSummary,
            GetBillingPlansHandler getBillingPlans,
            SetCancelAtPeriodEndHandler setCancelAtPeriodEnd,
            CreateSubscriptionCheckoutIntentHandler createSubscriptionCheckoutIntent,
            GetEmailDispatchAuditsPageHandler getEmailDispatchAuditsPage,
            CreateBusinessHandler createBusiness,
            EnsureBusinessOnboardingCustomerProfileHandler ensureBusinessOnboardingCustomerProfile,
            ProvisionBusinessOnboardingHandler provisionBusinessOnboarding,
            UpdateBusinessHandler updateBusiness,
            SoftDeleteBusinessHandler deleteBusiness,
            GetBusinessLocationsPageHandler getBusinessLocationsPage,
            GetBusinessLocationForEditHandler getBusinessLocationForEdit,
            CreateBusinessLocationHandler createBusinessLocation,
            UpdateBusinessLocationHandler updateBusinessLocation,
            SoftDeleteBusinessLocationHandler deleteBusinessLocation,
            GetBusinessMembersPageHandler getBusinessMembersPage,
            GetBusinessMemberForEditHandler getBusinessMemberForEdit,
            GetBusinessOwnerOverrideAuditsPageHandler getBusinessOwnerOverrideAuditsPage,
            CreateBusinessMemberHandler createBusinessMember,
            UpdateBusinessMemberHandler updateBusinessMember,
            DeleteBusinessMemberHandler deleteBusinessMember,
            GetBusinessInvitationsPageHandler getBusinessInvitationsPage,
            CreateBusinessInvitationHandler createBusinessInvitation,
            ResendBusinessInvitationHandler resendBusinessInvitation,
            RevokeBusinessInvitationHandler revokeBusinessInvitation,
            ApproveBusinessHandler approveBusiness,
            SuspendBusinessHandler suspendBusiness,
            ReactivateBusinessHandler reactivateBusiness,
            RequestPasswordResetHandler requestPasswordReset,
            RequestEmailConfirmationHandler requestEmailConfirmation,
            ConfirmUserEmailByAdminHandler confirmUserEmail,
            LockUserByAdminHandler lockUser,
            UnlockUserByAdminHandler unlockUser,
            AdminReferenceDataService referenceData,
            ISiteSettingCache siteSettingCache,
            IBusinessEffectiveSettingsCache businessEffectiveSettingsCache,
            IClock clock)
        {
            _getBusinessesPage = getBusinessesPage;
            _getBusinessForEdit = getBusinessForEdit;
            _getBusinessOnboardingCustomerProfile = getBusinessOnboardingCustomerProfile;
            _getBusinessSupportSummary = getBusinessSupportSummary;
            _getBusinessSubscriptionStatus = getBusinessSubscriptionStatus;
            _getBusinessSubscriptionInvoicesPage = getBusinessSubscriptionInvoicesPage;
            _getBusinessSubscriptionInvoiceOpsSummary = getBusinessSubscriptionInvoiceOpsSummary;
            _getBillingPlans = getBillingPlans;
            _setCancelAtPeriodEnd = setCancelAtPeriodEnd;
            _createSubscriptionCheckoutIntent = createSubscriptionCheckoutIntent;
            _getEmailDispatchAuditsPage = getEmailDispatchAuditsPage;
            _createBusiness = createBusiness;
            _ensureBusinessOnboardingCustomerProfile = ensureBusinessOnboardingCustomerProfile;
            _provisionBusinessOnboarding = provisionBusinessOnboarding;
            _updateBusiness = updateBusiness;
            _deleteBusiness = deleteBusiness;
            _getBusinessLocationsPage = getBusinessLocationsPage;
            _getBusinessLocationForEdit = getBusinessLocationForEdit;
            _createBusinessLocation = createBusinessLocation;
            _updateBusinessLocation = updateBusinessLocation;
            _deleteBusinessLocation = deleteBusinessLocation;
            _getBusinessMembersPage = getBusinessMembersPage;
            _getBusinessMemberForEdit = getBusinessMemberForEdit;
            _getBusinessOwnerOverrideAuditsPage = getBusinessOwnerOverrideAuditsPage;
            _createBusinessMember = createBusinessMember;
            _updateBusinessMember = updateBusinessMember;
            _deleteBusinessMember = deleteBusinessMember;
            _getBusinessInvitationsPage = getBusinessInvitationsPage;
            _createBusinessInvitation = createBusinessInvitation;
            _resendBusinessInvitation = resendBusinessInvitation;
            _revokeBusinessInvitation = revokeBusinessInvitation;
            _approveBusiness = approveBusiness;
            _suspendBusiness = suspendBusiness;
            _reactivateBusiness = reactivateBusiness;
            _requestPasswordReset = requestPasswordReset;
            _requestEmailConfirmation = requestEmailConfirmation;
            _confirmUserEmail = confirmUserEmail;
            _lockUser = lockUser;
            _unlockUser = unlockUser;
            _referenceData = referenceData;
            _siteSettingCache = siteSettingCache;
            _businessEffectiveSettingsCache = businessEffectiveSettingsCache;
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        [HttpGet]
        [PermissionAuthorize(PermissionKeys.ManageBusinessSupport)]
        public async Task<IActionResult> Index(
            int page = 1,
            int pageSize = 20,
            string? query = null,
            BusinessOperationalStatus? operationalStatus = null,
            bool attentionOnly = false,
            BusinessReadinessQueueFilter? readinessFilter = null,
            CancellationToken ct = default)
        {
            var summary = await _getBusinessSupportSummary.HandleAsync(null, ct).ConfigureAwait(false);
            var (items, total) = await _getBusinessesPage.HandleAsync(
                page,
                pageSize,
                query,
                operationalStatus,
                attentionOnly,
                readinessFilter,
                ct);

            var vm = new BusinessesListVm
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = query ?? string.Empty,
                OperationalStatus = operationalStatus,
                AttentionOnly = attentionOnly,
                ReadinessFilter = readinessFilter,
                Summary = MapSupportSummaryVm(summary),
                Playbooks = BuildMerchantReadinessPlaybooks(null),
                PageSizeItems = BuildPageSizeItems(pageSize),
                OperationalStatusItems = BuildBusinessStatusItems(operationalStatus),
                Items = items.Select(x => new BusinessListItemVm
                {
                    Id = x.Id,
                    Name = x.Name,
                    LegalName = x.LegalName,
                    Category = x.Category,
                    IsActive = x.IsActive,
                    OperationalStatus = x.OperationalStatus,
                    MemberCount = x.MemberCount,
                    ActiveOwnerCount = x.ActiveOwnerCount,
                    LocationCount = x.LocationCount,
                    PrimaryLocationCount = x.PrimaryLocationCount,
                    InvitationCount = x.InvitationCount,
                    HasContactEmailConfigured = x.HasContactEmailConfigured,
                    HasLegalNameConfigured = x.HasLegalNameConfigured,
                    CreatedAtUtc = x.CreatedAtUtc,
                    ModifiedAtUtc = x.ModifiedAtUtc,
                    RowVersion = x.RowVersion
                }).ToList()
            };

            return RenderBusinessesWorkspace(vm);
        }

        [HttpGet]
        [PermissionAuthorize(PermissionKeys.ManageBusinessSupport)]
        public async Task<IActionResult> SupportQueue(Guid? businessId = null, CancellationToken ct = default)
        {
            var summary = await _getBusinessSupportSummary.HandleAsync(businessId, ct).ConfigureAwait(false);
            var attentionBusinesses = await BuildSupportQueueAttentionBusinessesAsync(businessId, ct).ConfigureAwait(false);
            var (failedEmails, _, _) = await _getEmailDispatchAuditsPage
                .HandleAsync(
                    page: 1,
                    pageSize: 8,
                    query: null,
                    status: "Failed",
                    flowKey: null,
                    stalePendingOnly: false,
                    businessLinkedFailuresOnly: false,
                    repeatedFailuresOnly: false,
                    priorSuccessOnly: false,
                    retryReadyOnly: false,
                    retryBlockedOnly: false,
                    businessId: businessId,
                    ct: ct)
                .ConfigureAwait(false);

            var vm = new BusinessSupportQueueVm
            {
                BusinessId = businessId,
                Summary = new BusinessSupportSummaryVm
                {
                    AttentionBusinessCount = summary.AttentionBusinessCount,
                    PendingApprovalBusinessCount = summary.PendingApprovalBusinessCount,
                    SuspendedBusinessCount = summary.SuspendedBusinessCount,
                    ApprovedInactiveBusinessCount = summary.ApprovedInactiveBusinessCount,
                    MissingOwnerBusinessCount = summary.MissingOwnerBusinessCount,
                    MissingPrimaryLocationBusinessCount = summary.MissingPrimaryLocationBusinessCount,
                    MissingContactEmailBusinessCount = summary.MissingContactEmailBusinessCount,
                    MissingLegalNameBusinessCount = summary.MissingLegalNameBusinessCount,
                    OpenInvitationCount = summary.OpenInvitationCount,
                    PendingActivationMemberCount = summary.PendingActivationMemberCount,
                    LockedMemberCount = summary.LockedMemberCount
                },
                AttentionBusinesses = attentionBusinesses,
                FailedEmails = failedEmails.Select(x => new BusinessSupportFailedEmailVm
                {
                    Id = x.Id,
                    FlowKey = x.FlowKey ?? string.Empty,
                    BusinessId = x.BusinessId,
                    BusinessName = x.BusinessName,
                    RecipientEmail = x.RecipientEmail,
                    Subject = x.Subject,
                    AttemptedAtUtc = x.AttemptedAtUtc,
                    FailureMessage = x.FailureMessage,
                    RecommendedAction = BuildSupportAuditRecommendedAction(x)
                }).ToList(),
                Playbooks = BuildMerchantReadinessPlaybooks(businessId)
            };

            return RenderSupportQueueWorkspace(vm);
        }

        [HttpGet]
        [PermissionAuthorize(PermissionKeys.ManageBusinessSupport)]
        public async Task<IActionResult> MerchantReadiness(Guid? businessId = null, CancellationToken ct = default)
        {
            var summary = await _getBusinessSupportSummary.HandleAsync(businessId, ct).ConfigureAwait(false);
            var attentionBusinesses = await BuildMerchantReadinessBusinessesAsync(businessId, ct).ConfigureAwait(false);

            var items = new List<MerchantReadinessItemVm>();
            foreach (var business in attentionBusinesses)
            {
                var subscription = await BuildBusinessSubscriptionSnapshotAsync(business.Id, ct).ConfigureAwait(false);
                items.Add(new MerchantReadinessItemVm
                {
                    Id = business.Id,
                    RowVersion = business.RowVersion,
                    Name = business.Name,
                    LegalName = business.LegalName,
                    IsActive = business.IsActive,
                    OperationalStatus = business.OperationalStatus,
                    HasContactEmailConfigured = business.HasContactEmailConfigured,
                    HasLegalNameConfigured = business.HasLegalNameConfigured,
                    ActiveOwnerCount = business.ActiveOwnerCount,
                    PrimaryLocationCount = business.PrimaryLocationCount,
                    InvitationCount = business.InvitationCount,
                    HasSubscription = subscription.HasSubscription,
                    SubscriptionStatus = subscription.Status,
                    SubscriptionPlanName = subscription.PlanName,
                    CancelAtPeriodEnd = subscription.CancelAtPeriodEnd,
                    CurrentPeriodEndUtc = subscription.CurrentPeriodEndUtc
                });
            }

            var vm = new MerchantReadinessWorkspaceVm
            {
                BusinessId = businessId,
                Summary = MapSupportSummaryVm(summary),
                Items = items,
                Playbooks = BuildMerchantReadinessPlaybooks(businessId)
            };

            return RenderMerchantReadinessWorkspace(vm);
        }

        [HttpGet]
        [PermissionAuthorize(PermissionKeys.ManageBusinessSupport)]
        public async Task<IActionResult> SupportQueueSummaryFragment(Guid? businessId = null, CancellationToken ct = default)
        {
            var summary = await _getBusinessSupportSummary.HandleAsync(businessId, ct).ConfigureAwait(false);
            return PartialView("~/Views/Businesses/_SupportQueueSummary.cshtml", new BusinessSupportQueueVm
            {
                BusinessId = businessId,
                Summary = MapSupportSummaryVm(summary)
            });
        }

        [HttpGet]
        [PermissionAuthorize(PermissionKeys.ManageBusinessSupport)]
        public async Task<IActionResult> SupportQueueAttentionFragment(CancellationToken ct = default)
        {
            var (attentionBusinesses, _) = await _getBusinessesPage.HandleAsync(1, 10, null, null, true, readinessFilter: null, ct).ConfigureAwait(false);
            var vm = attentionBusinesses.Select(x => new BusinessListItemVm
            {
                Id = x.Id,
                Name = x.Name,
                LegalName = x.LegalName,
                Category = x.Category,
                IsActive = x.IsActive,
                OperationalStatus = x.OperationalStatus,
                MemberCount = x.MemberCount,
                ActiveOwnerCount = x.ActiveOwnerCount,
                LocationCount = x.LocationCount,
                PrimaryLocationCount = x.PrimaryLocationCount,
                InvitationCount = x.InvitationCount,
                HasContactEmailConfigured = x.HasContactEmailConfigured,
                HasLegalNameConfigured = x.HasLegalNameConfigured,
                CreatedAtUtc = x.CreatedAtUtc,
                ModifiedAtUtc = x.ModifiedAtUtc,
                RowVersion = x.RowVersion
            }).ToList();

            return PartialView("~/Views/Businesses/_SupportQueueAttentionBusinesses.cshtml", vm);
        }

        private async Task<List<BusinessListItemVm>> BuildSupportQueueAttentionBusinessesAsync(Guid? businessId, CancellationToken ct)
        {
            if (businessId.HasValue)
            {
                var business = await _getBusinessForEdit.HandleAsync(businessId.Value, ct).ConfigureAwait(false);
                return business is null ? new List<BusinessListItemVm>() : new List<BusinessListItemVm> { MapBusinessListItemVm(business) };
            }

            var (attentionBusinesses, _) = await _getBusinessesPage.HandleAsync(1, 10, null, null, true, readinessFilter: null, ct).ConfigureAwait(false);
            return attentionBusinesses.Select(x => new BusinessListItemVm
            {
                Id = x.Id,
                Name = x.Name,
                LegalName = x.LegalName,
                Category = x.Category,
                IsActive = x.IsActive,
                OperationalStatus = x.OperationalStatus,
                MemberCount = x.MemberCount,
                ActiveOwnerCount = x.ActiveOwnerCount,
                LocationCount = x.LocationCount,
                PrimaryLocationCount = x.PrimaryLocationCount,
                InvitationCount = x.InvitationCount,
                HasContactEmailConfigured = x.HasContactEmailConfigured,
                HasLegalNameConfigured = x.HasLegalNameConfigured,
                CreatedAtUtc = x.CreatedAtUtc,
                ModifiedAtUtc = x.ModifiedAtUtc,
                RowVersion = x.RowVersion
            }).ToList();
        }

        private async Task<List<BusinessListItemVm>> BuildMerchantReadinessBusinessesAsync(Guid? businessId, CancellationToken ct)
        {
            if (businessId.HasValue)
            {
                var business = await _getBusinessForEdit.HandleAsync(businessId.Value, ct).ConfigureAwait(false);
                return business is null ? new List<BusinessListItemVm>() : new List<BusinessListItemVm> { MapBusinessListItemVm(business) };
            }

            var (attentionBusinesses, _) = await _getBusinessesPage.HandleAsync(1, 12, null, null, true, readinessFilter: null, ct).ConfigureAwait(false);
            return attentionBusinesses.Select(x => new BusinessListItemVm
            {
                Id = x.Id,
                Name = x.Name,
                LegalName = x.LegalName,
                Category = x.Category,
                IsActive = x.IsActive,
                OperationalStatus = x.OperationalStatus,
                MemberCount = x.MemberCount,
                ActiveOwnerCount = x.ActiveOwnerCount,
                LocationCount = x.LocationCount,
                PrimaryLocationCount = x.PrimaryLocationCount,
                InvitationCount = x.InvitationCount,
                HasContactEmailConfigured = x.HasContactEmailConfigured,
                HasLegalNameConfigured = x.HasLegalNameConfigured,
                CreatedAtUtc = x.CreatedAtUtc,
                ModifiedAtUtc = x.ModifiedAtUtc,
                RowVersion = x.RowVersion
            }).ToList();
        }

        private static BusinessListItemVm MapBusinessListItemVm(BusinessEditDto business)
        {
            return new BusinessListItemVm
            {
                Id = business.Id,
                Name = business.Name,
                LegalName = business.LegalName,
                Category = business.Category,
                IsActive = business.IsActive,
                OperationalStatus = business.OperationalStatus,
                MemberCount = business.MemberCount,
                ActiveOwnerCount = business.ActiveOwnerCount,
                LocationCount = business.LocationCount,
                PrimaryLocationCount = business.PrimaryLocationCount,
                InvitationCount = business.InvitationCount,
                HasContactEmailConfigured = business.HasContactEmailConfigured,
                HasLegalNameConfigured = business.HasLegalNameConfigured,
                RowVersion = business.RowVersion
            };
        }

        [HttpGet]
        [PermissionAuthorize(PermissionKeys.ManageBusinessSupport)]
        public async Task<IActionResult> SupportQueueFailedEmailsFragment(CancellationToken ct = default)
        {
            var (failedEmails, _, _) = await _getEmailDispatchAuditsPage
                .HandleAsync(
                    page: 1,
                    pageSize: 8,
                    query: null,
                    status: "Failed",
                    flowKey: null,
                    stalePendingOnly: false,
                    businessLinkedFailuresOnly: false,
                    repeatedFailuresOnly: false,
                    priorSuccessOnly: false,
                    retryReadyOnly: false,
                    retryBlockedOnly: false,
                    businessId: null,
                    ct: ct)
                .ConfigureAwait(false);
            var vm = failedEmails.Select(x => new BusinessSupportFailedEmailVm
            {
                Id = x.Id,
                FlowKey = x.FlowKey ?? string.Empty,
                BusinessId = x.BusinessId,
                BusinessName = x.BusinessName,
                RecipientEmail = x.RecipientEmail,
                Subject = x.Subject,
                AttemptedAtUtc = x.AttemptedAtUtc,
                FailureMessage = x.FailureMessage,
                RecommendedAction = BuildSupportAuditRecommendedAction(x)
            }).ToList();

            return PartialView("~/Views/Businesses/_SupportQueueFailedEmails.cshtml", vm);
        }

        [HttpGet]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> Create(CancellationToken ct = default)
        {
            var vm = new BusinessEditVm
            {
                IsActive = false
            };
            await PopulateBusinessFormOptionsAsync(vm, ct);
            return RenderBusinessEditor(vm, isCreate: true);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> Create(BusinessEditVm vm, CancellationToken ct = default)
        {
            if (vm.OwnerUserId.HasValue && !string.IsNullOrWhiteSpace(vm.OwnerInviteEmail))
            {
                ModelState.AddModelError(nameof(vm.OwnerInviteEmail), T("BusinessFormInitialOwnerAssignmentConflict"));
            }

            if (!ModelState.IsValid)
            {
                await PopulateBusinessFormOptionsAsync(vm, ct);
                return RenderBusinessEditor(vm, isCreate: true);
            }

            var dto = new BusinessCreateDto
            {
                Name = vm.Name,
                LegalName = vm.LegalName,
                TaxId = vm.TaxId,
                ShortDescription = vm.ShortDescription,
                WebsiteUrl = vm.WebsiteUrl,
                ContactEmail = vm.ContactEmail,
                ContactPhoneE164 = vm.ContactPhoneE164,
                Category = vm.Category,
                DefaultCurrency = vm.DefaultCurrency,
                DefaultCulture = vm.DefaultCulture,
                DefaultTimeZoneId = vm.DefaultTimeZoneId,
                AdminTextOverridesJson = vm.AdminTextOverridesJson,
                BrandDisplayName = vm.BrandDisplayName,
                BrandLogoUrl = vm.BrandLogoUrl,
                BrandPrimaryColorHex = vm.BrandPrimaryColorHex,
                BrandSecondaryColorHex = vm.BrandSecondaryColorHex,
                SupportEmail = vm.SupportEmail,
                CommunicationSenderName = vm.CommunicationSenderName,
                CommunicationReplyToEmail = vm.CommunicationReplyToEmail,
                CustomerEmailNotificationsEnabled = vm.CustomerEmailNotificationsEnabled,
                CustomerMarketingEmailsEnabled = vm.CustomerMarketingEmailsEnabled,
                OperationalAlertEmailsEnabled = vm.OperationalAlertEmailsEnabled,
                IsActive = vm.IsActive
            };

            try
            {
                var businessId = await _createBusiness.HandleAsync(dto, ct);

                if (vm.OwnerUserId.HasValue)
                {
                    await _createBusinessMember.HandleAsync(new BusinessMemberCreateDto
                    {
                        BusinessId = businessId,
                        UserId = vm.OwnerUserId.Value,
                        Role = BusinessMemberRole.Owner,
                        IsActive = true
                    }, ct);
                }

                if (!string.IsNullOrWhiteSpace(vm.OwnerInviteEmail))
                {
                    await _createBusinessInvitation.HandleAsync(new BusinessInvitationCreateDto
                    {
                        BusinessId = businessId,
                        Email = vm.OwnerInviteEmail,
                        Role = BusinessMemberRole.Owner,
                        ExpiresInDays = 7,
                        Note = T("BusinessInvitationCreatedFromBusinessCreateNote")
                    }, ct);
                }

                await SyncBusinessOnboardingCustomerProfileAsync(businessId, ct);
                _businessEffectiveSettingsCache.Invalidate(businessId);

                TempData["Success"] = vm.OwnerUserId.HasValue
                    ? T("BusinessCreateOwnerAssigned")
                    : !string.IsNullOrWhiteSpace(vm.OwnerInviteEmail)
                        ? T("BusinessCreateOwnerInvitationIssued")
                        : T("BusinessCreateNextSteps");
                return RedirectOrHtmx(nameof(Edit), new { id = businessId });
            }
            catch (Exception)
            {
                AddModelErrorMessage("BusinessCreateFailed");
                await PopulateBusinessFormOptionsAsync(vm, ct);
                return RenderBusinessEditor(vm, isCreate: true);
            }
        }

        [HttpGet]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("BusinessNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var dto = await _getBusinessForEdit.HandleAsync(id, ct);
            if (dto is null)
            {
                SetErrorMessage("BusinessNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var vm = MapBusinessEditVm(dto);
            await PopulateBusinessFormOptionsAsync(vm, ct);
            return RenderBusinessEditor(vm, isCreate: false);
        }

        [HttpGet]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> Setup(Guid id, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("BusinessNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var dto = await _getBusinessForEdit.HandleAsync(id, ct);
            if (dto is null)
            {
                SetErrorMessage("BusinessNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var vm = MapBusinessEditVm(dto);
            await PopulateBusinessFormOptionsAsync(vm, ct);
            return RenderBusinessSetupEditor(vm);
        }

        [HttpGet]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> OnboardingWizard(Guid id, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("BusinessNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var dto = await _getBusinessForEdit.HandleAsync(id, ct);
            if (dto is null)
            {
                SetErrorMessage("BusinessNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var business = MapBusinessEditVm(dto);
            await PopulateBusinessFormOptionsAsync(business, ct);

            return RenderOnboardingWizardWorkspace(BuildOnboardingWizardVm(business));
        }

        [HttpGet]
        [PermissionAuthorize(PermissionKeys.ManageBusinessSupport)]
        public async Task<IActionResult> Subscription(Guid businessId, CancellationToken ct = default)
        {
            var business = await LoadBusinessContextAsync(businessId, ct);
            if (business is null)
            {
                SetErrorMessage("BusinessNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var vm = await BuildBusinessSubscriptionWorkspaceAsync(business, ct);
            return RenderSubscriptionWorkspace(vm);
        }

        [HttpGet]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> SubscriptionInvoices(
            Guid businessId,
            int page = 1,
            int pageSize = 20,
            string? query = null,
            BusinessSubscriptionInvoiceQueueFilter filter = BusinessSubscriptionInvoiceQueueFilter.All,
            CancellationToken ct = default)
        {
            var business = await LoadBusinessContextAsync(businessId, ct);
            if (business is null)
            {
                SetErrorMessage("BusinessNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var result = await _getBusinessSubscriptionInvoicesPage.HandleAsync(
                businessId,
                page,
                pageSize,
                query,
                filter,
                culture: CultureInfo.CurrentUICulture.Name,
                ct).ConfigureAwait(false);
            var summary = await _getBusinessSubscriptionInvoiceOpsSummary.HandleAsync(businessId, ct).ConfigureAwait(false);

            var vm = new BusinessSubscriptionInvoicesListVm
            {
                Business = business,
                Page = page,
                PageSize = pageSize,
                Total = result.Total,
                Query = query ?? string.Empty,
                Filter = filter,
                FilterItems = BuildBusinessSubscriptionInvoiceFilterItems(filter),
                Summary = MapBusinessSubscriptionInvoiceOpsSummaryVm(summary),
                Items = result.Items.Select(MapBusinessSubscriptionInvoiceListItemVm).ToList()
            };

            return RenderSubscriptionInvoicesWorkspace(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> SetSubscriptionCancelAtPeriodEnd(
            [FromForm] Guid businessId,
            [FromForm] Guid subscriptionId,
            [FromForm] bool cancelAtPeriodEnd,
            [FromForm] string rowVersion,
            CancellationToken ct = default)
        {
            if (businessId == Guid.Empty || subscriptionId == Guid.Empty)
            {
                SetErrorMessage("BusinessSubscriptionCancelAtPeriodEndUpdateFailed");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var parsedRowVersion = DecodeBase64RowVersion(rowVersion);

            var result = await _setCancelAtPeriodEnd.HandleAsync(
                businessId,
                subscriptionId,
                cancelAtPeriodEnd,
                parsedRowVersion,
                ct);

            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? (cancelAtPeriodEnd ? T("BusinessSubscriptionCancelAtPeriodEndUpdated") : T("BusinessSubscriptionRenewalRestored"))
                : T("BusinessSubscriptionCancelAtPeriodEndUpdateFailed");

            return RedirectOrHtmx(nameof(Subscription), new { businessId });
        }

        [HttpGet]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> SetupMembersPreview(Guid businessId, CancellationToken ct = default)
        {
            var business = await LoadBusinessContextAsync(businessId, ct);
            if (business is null)
            {
                return PartialView("~/Views/Businesses/_SetupMembersPreview.cshtml", new BusinessSetupMembersPreviewVm
                {
                    BusinessId = businessId
                });
            }

            var (items, _) = await _getBusinessMembersPage.HandleAsync(
                businessId,
                1,
                50,
                query: null,
                filter: BusinessMemberSupportFilter.All,
                ct);
            var nowUtc = _clock.UtcNow;
            var attentionMembers = items
                .Where(x => !x.EmailConfirmed || (x.LockoutEndUtc.HasValue && x.LockoutEndUtc.Value > nowUtc))
                .Take(5)
                .Select(x => new BusinessMemberListItemVm
                {
                    Id = x.Id,
                    BusinessId = x.BusinessId,
                    UserId = x.UserId,
                    UserDisplayName = x.UserDisplayName,
                    UserEmail = x.UserEmail,
                    EmailConfirmed = x.EmailConfirmed,
                    LockoutEndUtc = x.LockoutEndUtc,
                    Role = x.Role,
                    IsActive = x.IsActive,
                    ModifiedAtUtc = x.ModifiedAtUtc,
                    RowVersion = x.RowVersion
                })
                .ToList();

            return PartialView("~/Views/Businesses/_SetupMembersPreview.cshtml", new BusinessSetupMembersPreviewVm
            {
                BusinessId = businessId,
                AttentionCount = items.Count(x => !x.EmailConfirmed || (x.LockoutEndUtc.HasValue && x.LockoutEndUtc.Value > nowUtc)),
                Items = attentionMembers
            });
        }

        [HttpGet]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> SetupInvitationsPreview(Guid businessId, CancellationToken ct = default)
        {
            var business = await LoadBusinessContextAsync(businessId, ct);
            if (business is null)
            {
                return PartialView("~/Views/Businesses/_SetupInvitationsPreview.cshtml", new BusinessSetupInvitationsPreviewVm
                {
                    BusinessId = businessId
                });
            }

            var (items, _) = await _getBusinessInvitationsPage.HandleAsync(
                businessId,
                1,
                50,
                query: null,
                filter: BusinessInvitationQueueFilter.All,
                ct);
            var openInvitations = items
                .Where(x => x.Status == BusinessInvitationStatus.Pending || x.Status == BusinessInvitationStatus.Expired)
                .Take(5)
                .Select(x => new BusinessInvitationListItemVm
                {
                    Id = x.Id,
                    BusinessId = x.BusinessId,
                    Email = x.Email,
                    Role = x.Role,
                    Status = x.Status,
                    InvitedByDisplayName = x.InvitedByDisplayName,
                    ExpiresAtUtc = x.ExpiresAtUtc,
                    AcceptedAtUtc = x.AcceptedAtUtc,
                    RevokedAtUtc = x.RevokedAtUtc,
                    CreatedAtUtc = x.CreatedAtUtc,
                    Note = x.Note,
                    RowVersion = x.RowVersion
                })
                .ToList();

            return PartialView("~/Views/Businesses/_SetupInvitationsPreview.cshtml", new BusinessSetupInvitationsPreviewVm
            {
                BusinessId = businessId,
                OpenCount = items.Count(x => x.Status == BusinessInvitationStatus.Pending || x.Status == BusinessInvitationStatus.Expired),
                PendingCount = items.Count(x => x.Status == BusinessInvitationStatus.Pending),
                Items = openInvitations
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> Edit(BusinessEditVm vm, CancellationToken ct = default)
        {
            if (vm.Id == Guid.Empty)
            {
                SetErrorMessage("BusinessNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            if (!ModelState.IsValid)
            {
                await PopulateBusinessFormOptionsAsync(vm, ct);
                return RenderBusinessEditor(vm, isCreate: false);
            }

            var dto = new BusinessEditDto
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion ?? Array.Empty<byte>(),
                Name = vm.Name,
                LegalName = vm.LegalName,
                TaxId = vm.TaxId,
                ShortDescription = vm.ShortDescription,
                WebsiteUrl = vm.WebsiteUrl,
                ContactEmail = vm.ContactEmail,
                ContactPhoneE164 = vm.ContactPhoneE164,
                Category = vm.Category,
                DefaultCurrency = vm.DefaultCurrency,
                DefaultCulture = vm.DefaultCulture,
                DefaultTimeZoneId = vm.DefaultTimeZoneId,
                AdminTextOverridesJson = vm.AdminTextOverridesJson,
                BrandDisplayName = vm.BrandDisplayName,
                BrandLogoUrl = vm.BrandLogoUrl,
                BrandPrimaryColorHex = vm.BrandPrimaryColorHex,
                BrandSecondaryColorHex = vm.BrandSecondaryColorHex,
                SupportEmail = vm.SupportEmail,
                CommunicationSenderName = vm.CommunicationSenderName,
                CommunicationReplyToEmail = vm.CommunicationReplyToEmail,
                CustomerEmailNotificationsEnabled = vm.CustomerEmailNotificationsEnabled,
                CustomerMarketingEmailsEnabled = vm.CustomerMarketingEmailsEnabled,
                OperationalAlertEmailsEnabled = vm.OperationalAlertEmailsEnabled,
                IsActive = vm.IsActive
            };

            try
            {
                await _updateBusiness.HandleAsync(dto, ct);
                await SyncBusinessOnboardingCustomerProfileAsync(vm.Id, ct);
                _businessEffectiveSettingsCache.Invalidate(vm.Id);
                SetSuccessMessage("BusinessUpdated");
                return RedirectOrHtmx(nameof(Edit), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("BusinessConcurrencyConflict");
                return RedirectOrHtmx(nameof(Edit), new { id = vm.Id });
            }
            catch (Exception)
            {
                AddModelErrorMessage("BusinessUpdateFailed");
                await PopulateBusinessFormOptionsAsync(vm, ct);
                return RenderBusinessEditor(vm, isCreate: false);
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> Setup(BusinessEditVm vm, CancellationToken ct = default)
        {
            if (vm.Id == Guid.Empty)
            {
                SetErrorMessage("BusinessNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            if (!ModelState.IsValid)
            {
                await PopulateBusinessFormOptionsAsync(vm, ct);
                return RenderBusinessSetupEditor(vm);
            }

            var dto = new BusinessEditDto
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion ?? Array.Empty<byte>(),
                Name = vm.Name,
                LegalName = vm.LegalName,
                TaxId = vm.TaxId,
                ShortDescription = vm.ShortDescription,
                WebsiteUrl = vm.WebsiteUrl,
                ContactEmail = vm.ContactEmail,
                ContactPhoneE164 = vm.ContactPhoneE164,
                Category = vm.Category,
                DefaultCurrency = vm.DefaultCurrency,
                DefaultCulture = vm.DefaultCulture,
                DefaultTimeZoneId = vm.DefaultTimeZoneId,
                AdminTextOverridesJson = vm.AdminTextOverridesJson,
                BrandDisplayName = vm.BrandDisplayName,
                BrandLogoUrl = vm.BrandLogoUrl,
                BrandPrimaryColorHex = vm.BrandPrimaryColorHex,
                BrandSecondaryColorHex = vm.BrandSecondaryColorHex,
                SupportEmail = vm.SupportEmail,
                CommunicationSenderName = vm.CommunicationSenderName,
                CommunicationReplyToEmail = vm.CommunicationReplyToEmail,
                CustomerEmailNotificationsEnabled = vm.CustomerEmailNotificationsEnabled,
                CustomerMarketingEmailsEnabled = vm.CustomerMarketingEmailsEnabled,
                OperationalAlertEmailsEnabled = vm.OperationalAlertEmailsEnabled,
                IsActive = vm.IsActive
            };

            try
            {
                await _updateBusiness.HandleAsync(dto, ct);
                await SyncBusinessOnboardingCustomerProfileAsync(vm.Id, ct);
                _businessEffectiveSettingsCache.Invalidate(vm.Id);
                SetSuccessMessage("BusinessSetupSaved");
                return RedirectOrHtmx(nameof(Setup), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("BusinessConcurrencyConflict");
                return RedirectOrHtmx(nameof(Setup), new { id = vm.Id });
            }
            catch (Exception)
            {
                AddModelErrorMessage("BusinessSetupSaveFailed");
                await PopulateBusinessFormOptionsAsync(vm, ct);
                return RenderBusinessSetupEditor(vm);
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> ProvisionSupportCustomer([FromForm] Guid businessId, [FromForm] bool returnToSetup = true, CancellationToken ct = default)
        {
            if (businessId == Guid.Empty)
            {
                SetErrorMessage("BusinessNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var result = await _ensureBusinessOnboardingCustomerProfile.HandleAsync(businessId, ct);
            if (result.CustomerId.HasValue)
            {
                SetSuccessMessage(result.WasCreated ? "BusinessSupportCustomerProvisioned" : "BusinessSupportCustomerUpdated");
            }
            else
            {
                TempData["Error"] = string.Format(
                    T("BusinessSupportCustomerProvisioningBlocked"),
                    result.MissingReason ?? T("Unspecified"));
            }

            return RedirectOrHtmx(returnToSetup ? nameof(Setup) : nameof(Edit), new { id = businessId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> ProvisionOnboarding(
            [FromForm] Guid id,
            [FromForm] string? rowVersion,
            [FromForm] bool returnToSetup = false,
            [FromForm] bool returnToReadiness = false,
            CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("BusinessNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var result = await _provisionBusinessOnboarding.HandleAsync(new BusinessLifecycleActionDto
            {
                Id = id,
                RowVersion = DecodeBase64RowVersion(rowVersion)
            }, ct).ConfigureAwait(false);

            if (result.Succeeded && result.Value is not null)
            {
                SetSuccessMessage(result.Value.CustomerCreated
                    ? "BusinessOnboardingProvisionedAndApproved"
                    : "BusinessOnboardingSyncedAndApproved");
            }
            else
            {
                SetErrorMessage("BusinessOnboardingProvisioningFailed");
            }

            if (returnToReadiness)
            {
                return RedirectOrHtmx(nameof(MerchantReadiness), new { businessId = id });
            }

            return RedirectOrHtmx(returnToSetup ? nameof(Setup) : nameof(Edit), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> Delete([FromForm] Guid id, [FromForm] string? rowVersion, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("BusinessArchiveFailed");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            Result result = await _deleteBusiness.HandleAsync(new BusinessDeleteDto
            {
                Id = id,
                RowVersion = DecodeBase64RowVersion(rowVersion)
            }, ct);

            TempData[result.Succeeded ? "Success" : "Error"] =
                result.Succeeded ? T("BusinessArchived") : T("BusinessArchiveFailed");

            return RedirectOrHtmx(nameof(Index), new { });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> Approve([FromForm] Guid id, [FromForm] string? rowVersion, [FromForm] bool returnToSetup = false, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("BusinessApproveFailed");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            try
            {
                await _approveBusiness.HandleAsync(new BusinessLifecycleActionDto
                {
                    Id = id,
                    RowVersion = DecodeBase64RowVersion(rowVersion)
                }, ct);

                SetSuccessMessage("BusinessApproved");
            }
            catch (Exception)
            {
                SetErrorMessage("BusinessApproveFailed");
            }

            return RedirectOrHtmx(returnToSetup ? nameof(Setup) : nameof(Edit), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> Suspend([FromForm] Guid id, [FromForm] string? rowVersion, [FromForm] string? note, [FromForm] bool returnToSetup = false, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("BusinessSuspendFailed");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            try
            {
                await _suspendBusiness.HandleAsync(new BusinessLifecycleActionDto
                {
                    Id = id,
                    RowVersion = DecodeBase64RowVersion(rowVersion),
                    Note = note
                }, ct);

                SetSuccessMessage("BusinessSuspended");
            }
            catch (Exception)
            {
                SetErrorMessage("BusinessSuspendFailed");
            }

            return RedirectOrHtmx(returnToSetup ? nameof(Setup) : nameof(Edit), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> Reactivate([FromForm] Guid id, [FromForm] string? rowVersion, [FromForm] bool returnToSetup = false, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("BusinessReactivateFailed");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            try
            {
                await _reactivateBusiness.HandleAsync(new BusinessLifecycleActionDto
                {
                    Id = id,
                    RowVersion = DecodeBase64RowVersion(rowVersion)
                }, ct);

                SetSuccessMessage("BusinessReactivated");
            }
            catch (Exception)
            {
                SetErrorMessage("BusinessReactivateFailed");
            }

            return RedirectOrHtmx(returnToSetup ? nameof(Setup) : nameof(Edit), new { id });
        }

        [HttpGet]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> Locations(Guid businessId, int page = 1, int pageSize = 20, string? query = null, BusinessLocationQueueFilter filter = BusinessLocationQueueFilter.All, CancellationToken ct = default)
        {
            var business = await LoadBusinessContextAsync(businessId, ct);
            if (business is null)
            {
                SetErrorMessage("BusinessNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var (items, total) = await _getBusinessLocationsPage.HandleAsync(businessId, page, pageSize, query, filter, ct);
            var summary = await _getBusinessLocationsPage.GetSummaryAsync(businessId, ct);

            var vm = new BusinessLocationsListVm
            {
                Business = business,
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = query ?? string.Empty,
                Filter = filter,
                FilterItems = BuildBusinessLocationFilterItems(filter),
                Summary = new BusinessLocationOpsSummaryVm
                {
                    TotalCount = summary.TotalCount,
                    PrimaryCount = summary.PrimaryCount,
                    MissingAddressCount = summary.MissingAddressCount,
                    MissingCoordinatesCount = summary.MissingCoordinatesCount
                },
                Playbooks = BuildBusinessLocationPlaybooks(businessId),
                Items = items.Select(x => new BusinessLocationListItemVm
                {
                    Id = x.Id,
                    BusinessId = x.BusinessId,
                    Name = x.Name,
                    City = x.City,
                    Region = x.Region,
                    CountryCode = x.CountryCode,
                    IsPrimary = x.IsPrimary,
                    HasAddress = x.HasAddress,
                    HasCoordinates = x.HasCoordinates,
                    ModifiedAtUtc = x.ModifiedAtUtc,
                    RowVersion = x.RowVersion
                }).ToList()
            };

            return RenderLocationsWorkspace(vm);
        }

        [HttpGet]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> CreateLocation(Guid businessId, int page = 1, int pageSize = 20, string? query = null, BusinessLocationQueueFilter filter = BusinessLocationQueueFilter.All, CancellationToken ct = default)
        {
            var business = await LoadBusinessContextAsync(businessId, ct);
            if (business is null)
            {
                SetErrorMessage("BusinessNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            return RenderLocationEditor(new BusinessLocationEditVm
            {
                BusinessId = businessId,
                Page = page,
                PageSize = pageSize,
                Query = query ?? string.Empty,
                Filter = filter,
                CountryCode = Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCountryDefault,
                Business = business
            }, isCreate: true);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> CreateLocation(BusinessLocationEditVm vm, CancellationToken ct = default)
        {
            if (vm.BusinessId == Guid.Empty)
            {
                SetErrorMessage("BusinessNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            if (!ModelState.IsValid)
            {
                await PopulateBusinessContextAsync(vm, ct);
                return RenderLocationEditor(vm, isCreate: true);
            }

            try
            {
                await _createBusinessLocation.HandleAsync(new BusinessLocationCreateDto
                {
                    BusinessId = vm.BusinessId,
                    Name = vm.Name,
                    AddressLine1 = vm.AddressLine1,
                    AddressLine2 = vm.AddressLine2,
                    City = vm.City,
                    Region = vm.Region,
                    CountryCode = vm.CountryCode,
                    PostalCode = vm.PostalCode,
                    Coordinate = BuildCoordinate(vm),
                    IsPrimary = vm.IsPrimary,
                    OpeningHoursJson = vm.OpeningHoursJson,
                    InternalNote = vm.InternalNote
                }, ct);

                SetSuccessMessage("BusinessLocationCreated");
                  return RedirectOrHtmx(nameof(Locations), new { businessId = vm.BusinessId, page = vm.Page, pageSize = vm.PageSize, query = vm.Query, filter = vm.Filter });
            }
            catch (Exception)
            {
                AddModelErrorMessage("BusinessLocationCreateFailed");
                await PopulateBusinessContextAsync(vm, ct);
                return RenderLocationEditor(vm, isCreate: true);
            }
        }

        [HttpGet]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> EditLocation(Guid id, int page = 1, int pageSize = 20, string? query = null, BusinessLocationQueueFilter filter = BusinessLocationQueueFilter.All, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("BusinessLocationNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var dto = await _getBusinessLocationForEdit.HandleAsync(id, ct);
            if (dto is null)
            {
                SetErrorMessage("BusinessLocationNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var business = await LoadBusinessContextAsync(dto.BusinessId, ct);
            if (business is null)
            {
                SetErrorMessage("BusinessNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

              var vm = new BusinessLocationEditVm
              {
                  Id = dto.Id,
                  BusinessId = dto.BusinessId,
                  Page = page,
                  PageSize = pageSize,
                  Query = query ?? string.Empty,
                  Filter = filter,
                  RowVersion = dto.RowVersion,
                Name = dto.Name,
                AddressLine1 = dto.AddressLine1,
                AddressLine2 = dto.AddressLine2,
                City = dto.City,
                Region = dto.Region,
                CountryCode = dto.CountryCode,
                PostalCode = dto.PostalCode,
                Latitude = dto.Coordinate?.Latitude,
                Longitude = dto.Coordinate?.Longitude,
                AltitudeMeters = dto.Coordinate?.AltitudeMeters,
                IsPrimary = dto.IsPrimary,
                OpeningHoursJson = dto.OpeningHoursJson,
                InternalNote = dto.InternalNote,
                Business = business
            };

            return RenderLocationEditor(vm, isCreate: false);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> EditLocation(BusinessLocationEditVm vm, CancellationToken ct = default)
        {
            if (vm.Id == Guid.Empty || vm.BusinessId == Guid.Empty)
            {
                SetErrorMessage(vm.BusinessId == Guid.Empty ? "BusinessNotFound" : "BusinessLocationNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            if (!ModelState.IsValid)
            {
                await PopulateBusinessContextAsync(vm, ct);
                return RenderLocationEditor(vm, isCreate: false);
            }

            try
            {
                await _updateBusinessLocation.HandleAsync(new BusinessLocationEditDto
                {
                    Id = vm.Id,
                    BusinessId = vm.BusinessId,
                    RowVersion = vm.RowVersion ?? Array.Empty<byte>(),
                    Name = vm.Name,
                    AddressLine1 = vm.AddressLine1,
                    AddressLine2 = vm.AddressLine2,
                    City = vm.City,
                    Region = vm.Region,
                    CountryCode = vm.CountryCode,
                    PostalCode = vm.PostalCode,
                    Coordinate = BuildCoordinate(vm),
                    IsPrimary = vm.IsPrimary,
                    OpeningHoursJson = vm.OpeningHoursJson,
                    InternalNote = vm.InternalNote
                }, ct);

                SetSuccessMessage("BusinessLocationUpdated");
                  return RedirectOrHtmx(nameof(Locations), new { businessId = vm.BusinessId, page = vm.Page, pageSize = vm.PageSize, query = vm.Query, filter = vm.Filter });
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("BusinessLocationConcurrencyConflict");
                  return RedirectOrHtmx(nameof(EditLocation), new { id = vm.Id, page = vm.Page, pageSize = vm.PageSize, query = vm.Query, filter = vm.Filter });
            }
            catch (Exception)
            {
                AddModelErrorMessage("BusinessLocationUpdateFailed");
                await PopulateBusinessContextAsync(vm, ct);
                return RenderLocationEditor(vm, isCreate: false);
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> DeleteLocation([FromForm] Guid id, [FromForm(Name = "userId")] Guid businessId, [FromForm] string? rowVersion, CancellationToken ct = default)
        {
            if (id == Guid.Empty || businessId == Guid.Empty)
            {
                SetErrorMessage("BusinessLocationArchiveFailed");
                return businessId == Guid.Empty
                    ? RedirectOrHtmx(nameof(Index), new { })
                    : RedirectOrHtmx(nameof(Locations), new { businessId });
            }

            var result = await _deleteBusinessLocation.HandleAsync(new BusinessLocationDeleteDto
            {
                Id = id,
                RowVersion = DecodeBase64RowVersion(rowVersion)
            }, ct);

            TempData[result.Succeeded ? "Success" : "Error"] =
                result.Succeeded ? T("BusinessLocationArchived") : T("BusinessLocationArchiveFailed");

            return RedirectOrHtmx(nameof(Locations), new { businessId });
        }

        [HttpGet]
        [PermissionAuthorize(PermissionKeys.ManageBusinessSupport)]
        public async Task<IActionResult> Members(
            Guid businessId,
            int page = 1,
            int pageSize = 20,
            string? query = null,
            BusinessMemberSupportFilter filter = BusinessMemberSupportFilter.All,
            CancellationToken ct = default)
        {
            var business = await LoadBusinessContextAsync(businessId, ct);
            if (business is null)
            {
                SetErrorMessage("BusinessNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var (items, total) = await _getBusinessMembersPage.HandleAsync(businessId, page, pageSize, query, filter, ct);

            var vm = new BusinessMembersListVm
            {
                Business = business,
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = query ?? string.Empty,
                Filter = filter,
                FilterItems = BuildBusinessMemberFilterItems(filter),
                Summary = await BuildBusinessMemberOpsSummaryAsync(businessId, ct).ConfigureAwait(false),
                Playbooks = BuildBusinessMemberPlaybooks(businessId),
                Items = items.Select(x => new BusinessMemberListItemVm
                {
                    Id = x.Id,
                    BusinessId = x.BusinessId,
                    UserId = x.UserId,
                    UserDisplayName = x.UserDisplayName,
                    UserEmail = x.UserEmail,
                    EmailConfirmed = x.EmailConfirmed,
                    LockoutEndUtc = x.LockoutEndUtc,
                    Role = x.Role,
                    IsActive = x.IsActive,
                    ModifiedAtUtc = x.ModifiedAtUtc,
                    RowVersion = x.RowVersion
                }).ToList()
            };

            return RenderMembersWorkspace(vm);
        }

        [HttpGet]
        [PermissionAuthorize(PermissionKeys.ManageBusinessSupport)]
        public async Task<IActionResult> Invitations(
            Guid businessId,
            int page = 1,
            int pageSize = 20,
            string? query = null,
            BusinessInvitationQueueFilter filter = BusinessInvitationQueueFilter.All,
            CancellationToken ct = default)
        {
            var business = await LoadBusinessContextAsync(businessId, ct);
            if (business is null)
            {
                SetErrorMessage("BusinessNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var (items, total) = await _getBusinessInvitationsPage.HandleAsync(businessId, page, pageSize, query, filter, ct);

            var vm = new BusinessInvitationsListVm
            {
                Business = business,
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = query ?? string.Empty,
                Filter = filter,
                FilterItems = BuildBusinessInvitationFilterItems(filter),
                Summary = await BuildBusinessInvitationOpsSummaryAsync(businessId, ct).ConfigureAwait(false),
                Playbooks = BuildBusinessInvitationPlaybooks(businessId),
                Items = items.Select(x => new BusinessInvitationListItemVm
                {
                    Id = x.Id,
                    BusinessId = x.BusinessId,
                    Email = x.Email,
                    Role = x.Role,
                    Status = x.Status,
                    InvitedByDisplayName = x.InvitedByDisplayName,
                    ExpiresAtUtc = x.ExpiresAtUtc,
                    AcceptedAtUtc = x.AcceptedAtUtc,
                    RevokedAtUtc = x.RevokedAtUtc,
                    CreatedAtUtc = x.CreatedAtUtc,
                    Note = x.Note,
                    RowVersion = x.RowVersion
                }).ToList()
            };

            return RenderInvitationsWorkspace(vm);
        }

        [HttpGet]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> OwnerOverrideAudits(Guid businessId, int page = 1, int pageSize = 20, string? query = null, CancellationToken ct = default)
        {
            var business = await LoadBusinessContextAsync(businessId, ct);
            if (business is null)
            {
                SetErrorMessage("BusinessNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var (items, total) = await _getBusinessOwnerOverrideAuditsPage.HandleAsync(businessId, page, pageSize, query, ct);

            var vm = new BusinessOwnerOverrideAuditsListVm
            {
                Business = business,
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = query ?? string.Empty,
                Playbooks = BuildBusinessOwnerOverrideAuditPlaybooks(businessId),
                Items = items.Select(x => new BusinessOwnerOverrideAuditListItemVm
                {
                    Id = x.Id,
                    BusinessId = x.BusinessId,
                    BusinessMemberId = x.BusinessMemberId,
                    AffectedUserId = x.AffectedUserId,
                    AffectedUserDisplayName = x.AffectedUserDisplayName,
                    AffectedUserEmail = x.AffectedUserEmail,
                    ActionKind = x.ActionKind,
                    Reason = x.Reason,
                    ActorDisplayName = x.ActorDisplayName,
                    CreatedAtUtc = x.CreatedAtUtc
                }).ToList()
            };

            return RenderOwnerOverrideAuditsWorkspace(vm);
        }

        [HttpGet]
        [PermissionAuthorize(PermissionKeys.ManageBusinessSupport)]
        public async Task<IActionResult> CreateInvitation(Guid businessId, int page = 1, int pageSize = 20, string? query = null, BusinessInvitationQueueFilter filter = BusinessInvitationQueueFilter.All, CancellationToken ct = default)
        {
            var business = await LoadBusinessContextAsync(businessId, ct);
            if (business is null)
            {
                SetErrorMessage("BusinessNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

              var vm = new BusinessInvitationCreateVm
              {
                  BusinessId = businessId,
                  Page = page,
                  PageSize = pageSize,
                  Query = query ?? string.Empty,
                  Filter = filter,
                  Business = business,
                  Role = BusinessMemberRole.Owner,
                  ExpiresInDays = 7
            };
            PopulateInvitationFormOptions(vm);
            return RenderInvitationEditor(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.ManageBusinessSupport)]
        public async Task<IActionResult> CreateInvitation(BusinessInvitationCreateVm vm, CancellationToken ct = default)
        {
            if (vm.BusinessId == Guid.Empty)
            {
                SetErrorMessage("BusinessNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            if (!ModelState.IsValid)
            {
                await PopulateBusinessContextAsync(vm, ct);
                PopulateInvitationFormOptions(vm);
                return RenderInvitationEditor(vm);
            }

            try
            {
                await _createBusinessInvitation.HandleAsync(new BusinessInvitationCreateDto
                {
                    BusinessId = vm.BusinessId,
                    Email = vm.Email,
                    Role = vm.Role,
                    ExpiresInDays = vm.ExpiresInDays,
                    Note = vm.Note
                }, ct);

                SetSuccessMessage("BusinessInvitationSent");
                  return RedirectOrHtmx(nameof(Invitations), new { businessId = vm.BusinessId, page = vm.Page, pageSize = vm.PageSize, query = vm.Query, filter = vm.Filter });
            }
            catch (Exception)
            {
                AddModelErrorMessage("BusinessInvitationCreateFailed");
                await PopulateBusinessContextAsync(vm, ct);
                PopulateInvitationFormOptions(vm);
                return RenderInvitationEditor(vm);
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.ManageBusinessSupport)]
        public async Task<IActionResult> ResendInvitation(
            [FromForm] Guid id,
            [FromForm] Guid businessId,
            [FromForm] string? rowVersion,
            [FromForm] int page = 1,
            [FromForm] int pageSize = 20,
            [FromForm] string? query = null,
            [FromForm] BusinessInvitationQueueFilter filter = BusinessInvitationQueueFilter.All,
            CancellationToken ct = default)
        {
            if (id == Guid.Empty || businessId == Guid.Empty)
            {
                SetErrorMessage("BusinessInvitationResendFailed");
                return businessId == Guid.Empty
                    ? RedirectOrHtmx(nameof(Index), new { })
                    : RedirectOrHtmx(nameof(Invitations), new { businessId, page, pageSize, query, filter });
            }

            try
            {
                await _resendBusinessInvitation.HandleAsync(new BusinessInvitationResendDto
                {
                    Id = id,
                    ExpiresInDays = 7,
                    RowVersion = DecodeBase64RowVersion(rowVersion)
                }, ct);
                SetSuccessMessage("BusinessInvitationReissued");
            }
            catch (Exception)
            {
                SetErrorMessage("BusinessInvitationResendFailed");
            }

            return RedirectOrHtmx(nameof(Invitations), new { businessId, page, pageSize, query, filter });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.ManageBusinessSupport)]
        public async Task<IActionResult> RevokeInvitation(
            [FromForm] Guid id,
            [FromForm] Guid businessId,
            [FromForm] string? rowVersion,
            [FromForm] int page = 1,
            [FromForm] int pageSize = 20,
            [FromForm] string? query = null,
            [FromForm] BusinessInvitationQueueFilter filter = BusinessInvitationQueueFilter.All,
            CancellationToken ct = default)
        {
            if (id == Guid.Empty || businessId == Guid.Empty)
            {
                SetErrorMessage("BusinessInvitationRevokeFailed");
                return businessId == Guid.Empty
                    ? RedirectOrHtmx(nameof(Index), new { })
                    : RedirectOrHtmx(nameof(Invitations), new { businessId, page, pageSize, query, filter });
            }

            try
            {
                await _revokeBusinessInvitation.HandleAsync(new BusinessInvitationRevokeDto
                {
                    Id = id,
                    Note = T("BusinessInvitationRevokedFromWebAdminNote"),
                    RowVersion = DecodeBase64RowVersion(rowVersion)
                }, ct);
                SetSuccessMessage("BusinessInvitationRevoked");
            }
            catch (Exception)
            {
                SetErrorMessage("BusinessInvitationRevokeFailed");
            }

            return RedirectOrHtmx(nameof(Invitations), new { businessId, page, pageSize, query, filter });
        }

        [HttpGet]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> CreateMember(Guid businessId, int page = 1, int pageSize = 20, string? query = null, BusinessMemberSupportFilter filter = BusinessMemberSupportFilter.All, CancellationToken ct = default)
        {
            var business = await LoadBusinessContextAsync(businessId, ct);
            if (business is null)
            {
                SetErrorMessage("BusinessNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var vm = new BusinessMemberEditVm
            {
                BusinessId = businessId,
                Page = page,
                PageSize = pageSize,
                Query = query ?? string.Empty,
                Filter = filter,
                Role = BusinessMemberRole.Owner,
                IsActive = true,
                Business = business
            };
            await PopulateMemberFormOptionsAsync(vm, includeUserSelection: true, ct);
            return RenderMemberEditor(vm, isCreate: true);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> CreateMember(BusinessMemberEditVm vm, CancellationToken ct = default)
        {
            if (vm.BusinessId == Guid.Empty || vm.UserId == Guid.Empty)
            {
                SetErrorMessage(vm.BusinessId == Guid.Empty ? "BusinessNotFound" : "BusinessMemberCreateFailed");
                return vm.BusinessId == Guid.Empty
                    ? RedirectOrHtmx(nameof(Index), new { })
                    : RedirectOrHtmx(nameof(Members), new { businessId = vm.BusinessId, page = vm.Page, pageSize = vm.PageSize, query = vm.Query, filter = vm.Filter });
            }

            if (!ModelState.IsValid)
            {
                await PopulateBusinessContextAsync(vm, ct);
                await PopulateMemberFormOptionsAsync(vm, includeUserSelection: true, ct);
                return RenderMemberEditor(vm, isCreate: true);
            }

            try
            {
                await _createBusinessMember.HandleAsync(new BusinessMemberCreateDto
                {
                    BusinessId = vm.BusinessId,
                    UserId = vm.UserId,
                    Role = vm.Role,
                    IsActive = vm.IsActive
                }, ct);

                if (vm.Role == BusinessMemberRole.Owner && vm.IsActive)
                {
                    await SyncBusinessOnboardingCustomerProfileAsync(vm.BusinessId, ct);
                }

                SetSuccessMessage("BusinessMemberAssigned");
                return RedirectOrHtmx(nameof(Members), new { businessId = vm.BusinessId, page = vm.Page, pageSize = vm.PageSize, query = vm.Query, filter = vm.Filter });
            }
            catch (Exception)
            {
                AddModelErrorMessage("BusinessMemberCreateFailed");
                await PopulateBusinessContextAsync(vm, ct);
                await PopulateMemberFormOptionsAsync(vm, includeUserSelection: true, ct);
                return RenderMemberEditor(vm, isCreate: true);
            }
        }

        [HttpGet]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> EditMember(Guid id, int page = 1, int pageSize = 20, string? query = null, BusinessMemberSupportFilter filter = BusinessMemberSupportFilter.All, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("BusinessMemberNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var dto = await _getBusinessMemberForEdit.HandleAsync(id, ct);
            if (dto is null)
            {
                SetErrorMessage("BusinessMemberNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var business = await LoadBusinessContextAsync(dto.BusinessId, ct);
            if (business is null)
            {
                SetErrorMessage("BusinessNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var vm = new BusinessMemberEditVm
            {
                Id = dto.Id,
                BusinessId = dto.BusinessId,
                UserId = dto.UserId,
                Page = page,
                PageSize = pageSize,
                Query = query ?? string.Empty,
                Filter = filter,
                RowVersion = dto.RowVersion,
                UserDisplayName = dto.UserDisplayName,
                UserEmail = dto.UserEmail,
                EmailConfirmed = dto.EmailConfirmed,
                LockoutEndUtc = dto.LockoutEndUtc,
                Role = dto.Role,
                IsActive = dto.IsActive,
                IsLastActiveOwner = dto.IsLastActiveOwner,
                OverrideReason = null,
                Business = business
            };
            await PopulateMemberFormOptionsAsync(vm, includeUserSelection: false, ct);
            return RenderMemberEditor(vm, isCreate: false);
        }

        [HttpGet]
        [PermissionAuthorize(PermissionKeys.ManageBusinessSupport)]
        public async Task<IActionResult> StaffAccessBadge(Guid id, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("BusinessMemberNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var dto = await _getBusinessMemberForEdit.HandleAsync(id, ct);
            if (dto is null)
            {
                SetErrorMessage("BusinessMemberNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var business = await LoadBusinessContextAsync(dto.BusinessId, ct);
            if (business is null)
            {
                SetErrorMessage("BusinessNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var issuedAtUtc = _clock.UtcNow;
            var expiresAtUtc = issuedAtUtc.AddMinutes(2);
            var payload = BuildStaffAccessBadgePayload(dto, business, issuedAtUtc, expiresAtUtc);

            var vm = new BusinessStaffAccessBadgeVm
            {
                MembershipId = dto.Id,
                UserId = dto.UserId,
                UserDisplayName = dto.UserDisplayName,
                UserEmail = dto.UserEmail,
                Role = dto.Role,
                IsActive = dto.IsActive,
                EmailConfirmed = dto.EmailConfirmed,
                LockoutEndUtc = dto.LockoutEndUtc,
                Business = business,
                IssuedAtUtc = issuedAtUtc,
                ExpiresAtUtc = expiresAtUtc,
                BadgePayload = payload,
                BadgeImageDataUrl = BuildQrCodeDataUrl(payload)
            };

            return RenderStaffAccessBadgeWorkspace(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> EditMember(BusinessMemberEditVm vm, CancellationToken ct = default)
        {
            if (vm.Id == Guid.Empty || vm.BusinessId == Guid.Empty || vm.UserId == Guid.Empty)
            {
                SetErrorMessage(vm.BusinessId == Guid.Empty ? "BusinessNotFound" : "BusinessMemberUpdateFailed");
                return vm.BusinessId == Guid.Empty
                    ? RedirectOrHtmx(nameof(Index), new { })
                    : RedirectOrHtmx(nameof(Members), new { businessId = vm.BusinessId, page = vm.Page, pageSize = vm.PageSize, query = vm.Query, filter = vm.Filter });
            }

            if (!ModelState.IsValid)
            {
                await PopulateBusinessContextAsync(vm, ct);
                await PopulateMemberFormOptionsAsync(vm, includeUserSelection: false, ct);
                return RenderMemberEditor(vm, isCreate: false);
            }

            try
            {
                await _updateBusinessMember.HandleAsync(new BusinessMemberEditDto
                {
                    Id = vm.Id,
                    BusinessId = vm.BusinessId,
                    UserId = vm.UserId,
                    Role = vm.Role,
                    IsActive = vm.IsActive,
                    AllowLastOwnerOverride = vm.AllowLastOwnerOverride,
                    OverrideReason = vm.OverrideReason,
                    OverrideActorDisplayName = GetCurrentActorDisplayName(),
                    RowVersion = vm.RowVersion ?? Array.Empty<byte>()
                }, ct);

                if (vm.Role == BusinessMemberRole.Owner && vm.IsActive)
                {
                    await SyncBusinessOnboardingCustomerProfileAsync(vm.BusinessId, ct);
                }

                SetSuccessMessage("BusinessMemberUpdated");
                return RedirectOrHtmx(nameof(Members), new { businessId = vm.BusinessId, page = vm.Page, pageSize = vm.PageSize, query = vm.Query, filter = vm.Filter });
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("BusinessMemberConcurrencyConflict");
                return RedirectOrHtmx(nameof(EditMember), new { id = vm.Id, page = vm.Page, pageSize = vm.PageSize, query = vm.Query, filter = vm.Filter });
            }
            catch (Exception)
            {
                AddModelErrorMessage("BusinessMemberUpdateFailed");
                await PopulateBusinessContextAsync(vm, ct);
                await PopulateMemberFormOptionsAsync(vm, includeUserSelection: false, ct);
                return RenderMemberEditor(vm, isCreate: false);
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> DeleteMember([FromForm] Guid id, [FromForm(Name = "userId")] Guid businessId, [FromForm] string? rowVersion, CancellationToken ct = default)
        {
            if (id == Guid.Empty || businessId == Guid.Empty)
            {
                SetErrorMessage("BusinessMemberDeleteFailed");
                return businessId == Guid.Empty
                    ? RedirectOrHtmx(nameof(Index), new { })
                    : RedirectOrHtmx(nameof(Members), new { businessId });
            }

            try
            {
                await _deleteBusinessMember.HandleAsync(new BusinessMemberDeleteDto
                {
                    Id = id,
                    RowVersion = DecodeBase64RowVersion(rowVersion)
                }, ct);

                SetSuccessMessage("BusinessMemberRemoved");
            }
            catch (Exception)
            {
                SetErrorMessage("BusinessMemberDeleteFailed");
            }

            return RedirectOrHtmx(nameof(Members), new { businessId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> ForceDeleteMember(
            [FromForm] Guid id,
            [FromForm] Guid businessId,
            [FromForm] string? rowVersion,
            [FromForm] string? overrideReason,
            CancellationToken ct = default)
        {
            if (id == Guid.Empty || businessId == Guid.Empty)
            {
                SetErrorMessage("BusinessMemberForceDeleteFailed");
                return businessId == Guid.Empty
                    ? RedirectOrHtmx(nameof(Index), new { })
                    : RedirectOrHtmx(nameof(Members), new { businessId });
            }

            try
            {
                await _deleteBusinessMember.HandleAsync(new BusinessMemberDeleteDto
                {
                    Id = id,
                    RowVersion = DecodeBase64RowVersion(rowVersion),
                    AllowLastOwnerOverride = true,
                    OverrideReason = overrideReason,
                    OverrideActorDisplayName = GetCurrentActorDisplayName()
                }, ct);

                SetSuccessMessage("BusinessMemberRemovedOverride");
                return RedirectOrHtmx(nameof(Members), new { businessId });
            }
            catch (Exception)
            {
                AddModelErrorMessage("BusinessMemberForceDeleteFailed");
                return RedirectOrHtmx(nameof(EditMember), new { id });
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.ManageBusinessSupport)]
        public async Task<IActionResult> SendMemberActivationEmail(
            [FromForm] Guid id,
            [FromForm] Guid businessId,
            [FromForm] bool returnToEdit = false,
            [FromForm] int page = 1,
            [FromForm] int pageSize = 20,
            [FromForm] string? query = null,
            [FromForm] BusinessMemberSupportFilter filter = BusinessMemberSupportFilter.All,
            CancellationToken ct = default)
        {
            if (id == Guid.Empty || businessId == Guid.Empty)
            {
                SetErrorMessage("BusinessMemberNotFound");
                return businessId == Guid.Empty
                    ? RedirectOrHtmx(nameof(Index), new { })
                    : RedirectMemberSupport(returnToEdit, id, businessId, page, pageSize, query, filter);
            }

            var member = await _getBusinessMemberForEdit.HandleAsync(id, ct);
            if (member is null)
            {
                SetErrorMessage("BusinessMemberNotFound");
                return RedirectMemberSupport(returnToEdit, id, businessId, page, pageSize, query, filter);
            }

            var result = await _requestEmailConfirmation.HandleAsync(
                new RequestEmailConfirmationDto
                {
                    Email = member.UserEmail
                },
                ct);

            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? T("BusinessMemberActivationEmailSent")
                : T("BusinessMemberActivationEmailFailed");

            return RedirectMemberSupport(returnToEdit, id, businessId, page, pageSize, query, filter);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.ManageBusinessSupport)]
        public async Task<IActionResult> ConfirmMemberEmail(
            [FromForm] Guid id,
            [FromForm] Guid businessId,
            [FromForm] bool returnToEdit = false,
            [FromForm] int page = 1,
            [FromForm] int pageSize = 20,
            [FromForm] string? query = null,
            [FromForm] BusinessMemberSupportFilter filter = BusinessMemberSupportFilter.All,
            CancellationToken ct = default)
        {
            if (id == Guid.Empty || businessId == Guid.Empty)
            {
                SetErrorMessage("BusinessMemberNotFound");
                return businessId == Guid.Empty
                    ? RedirectOrHtmx(nameof(Index), new { })
                    : RedirectMemberSupport(returnToEdit, id, businessId, page, pageSize, query, filter);
            }

            var member = await _getBusinessMemberForEdit.HandleAsync(id, ct);
            if (member is null)
            {
                SetErrorMessage("BusinessMemberNotFound");
                return RedirectMemberSupport(returnToEdit, id, businessId, page, pageSize, query, filter);
            }

            var result = await _confirmUserEmail.HandleAsync(new UserAdminActionDto
            {
                Id = member.UserId
            }, ct);

            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? T("BusinessMemberEmailConfirmed")
                : T("BusinessMemberEmailConfirmFailed");

            return RedirectMemberSupport(returnToEdit, id, businessId, page, pageSize, query, filter);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.ManageBusinessSupport)]
        public async Task<IActionResult> SendMemberPasswordReset(
            [FromForm] Guid id,
            [FromForm] Guid businessId,
            [FromForm] bool returnToEdit = false,
            [FromForm] int page = 1,
            [FromForm] int pageSize = 20,
            [FromForm] string? query = null,
            [FromForm] BusinessMemberSupportFilter filter = BusinessMemberSupportFilter.All,
            CancellationToken ct = default)
        {
            if (id == Guid.Empty || businessId == Guid.Empty)
            {
                SetErrorMessage("BusinessMemberNotFound");
                return businessId == Guid.Empty
                    ? RedirectOrHtmx(nameof(Index), new { })
                    : RedirectMemberSupport(returnToEdit, id, businessId, page, pageSize, query, filter);
            }

            var member = await _getBusinessMemberForEdit.HandleAsync(id, ct);
            if (member is null)
            {
                SetErrorMessage("BusinessMemberNotFound");
                return RedirectMemberSupport(returnToEdit, id, businessId, page, pageSize, query, filter);
            }

            var result = await _requestPasswordReset.HandleAsync(
                new RequestPasswordResetDto
                {
                    Email = member.UserEmail
                },
                ct);

            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? T("BusinessMemberPasswordResetSent")
                : T("BusinessMemberPasswordResetFailed");

            return RedirectMemberSupport(returnToEdit, id, businessId, page, pageSize, query, filter);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.ManageBusinessSupport)]
        public async Task<IActionResult> LockMemberUser(
            [FromForm] Guid id,
            [FromForm] Guid businessId,
            [FromForm] bool returnToEdit = false,
            [FromForm] int page = 1,
            [FromForm] int pageSize = 20,
            [FromForm] string? query = null,
            [FromForm] BusinessMemberSupportFilter filter = BusinessMemberSupportFilter.All,
            CancellationToken ct = default)
        {
            if (id == Guid.Empty || businessId == Guid.Empty)
            {
                SetErrorMessage("BusinessMemberNotFound");
                return businessId == Guid.Empty
                    ? RedirectOrHtmx(nameof(Index), new { })
                    : RedirectMemberSupport(returnToEdit, id, businessId, page, pageSize, query, filter);
            }

            var member = await _getBusinessMemberForEdit.HandleAsync(id, ct);
            if (member is null)
            {
                SetErrorMessage("BusinessMemberNotFound");
                return RedirectMemberSupport(returnToEdit, id, businessId, page, pageSize, query, filter);
            }

            var result = await _lockUser.HandleAsync(new UserAdminActionDto
            {
                Id = member.UserId
            }, ct);

            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? T("BusinessMemberAccountLocked")
                : T("BusinessMemberAccountLockFailed");

            return RedirectMemberSupport(returnToEdit, id, businessId, page, pageSize, query, filter);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.ManageBusinessSupport)]
        public async Task<IActionResult> UnlockMemberUser(
            [FromForm] Guid id,
            [FromForm] Guid businessId,
            [FromForm] bool returnToEdit = false,
            [FromForm] int page = 1,
            [FromForm] int pageSize = 20,
            [FromForm] string? query = null,
            [FromForm] BusinessMemberSupportFilter filter = BusinessMemberSupportFilter.All,
            CancellationToken ct = default)
        {
            if (id == Guid.Empty || businessId == Guid.Empty)
            {
                SetErrorMessage("BusinessMemberNotFound");
                return businessId == Guid.Empty
                    ? RedirectOrHtmx(nameof(Index), new { })
                    : RedirectMemberSupport(returnToEdit, id, businessId, page, pageSize, query, filter);
            }

            var member = await _getBusinessMemberForEdit.HandleAsync(id, ct);
            if (member is null)
            {
                SetErrorMessage("BusinessMemberNotFound");
                return RedirectMemberSupport(returnToEdit, id, businessId, page, pageSize, query, filter);
            }

            var result = await _unlockUser.HandleAsync(new UserAdminActionDto
            {
                Id = member.UserId
            }, ct);

            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? T("BusinessMemberAccountUnlocked")
                : T("BusinessMemberAccountUnlockFailed");

            return RedirectMemberSupport(returnToEdit, id, businessId, page, pageSize, query, filter);
        }

        private async Task PopulateBusinessFormOptionsAsync(BusinessEditVm vm, CancellationToken ct)
        {
            var settings = await _siteSettingCache.GetAsync(ct);
            var effectiveSettings = vm.Id == Guid.Empty
                ? null
                : await _businessEffectiveSettingsCache.GetAsync(vm.Id, ct).ConfigureAwait(false);

            vm.DefaultCurrency = string.IsNullOrWhiteSpace(vm.DefaultCurrency) ? (effectiveSettings?.DefaultCurrency ?? settings.DefaultCurrency) : vm.DefaultCurrency;
            vm.DefaultCulture = string.IsNullOrWhiteSpace(vm.DefaultCulture) ? (effectiveSettings?.DefaultCulture ?? settings.DefaultCulture) : vm.DefaultCulture;
            vm.DefaultTimeZoneId = string.IsNullOrWhiteSpace(vm.DefaultTimeZoneId) ? (effectiveSettings?.DefaultTimeZoneId ?? settings.TimeZone ?? string.Empty) : vm.DefaultTimeZoneId;
            vm.ContactEmail = string.IsNullOrWhiteSpace(vm.ContactEmail) ? settings.ContactEmail : vm.ContactEmail;
            vm.BrandDisplayName = string.IsNullOrWhiteSpace(vm.BrandDisplayName) ? (effectiveSettings?.BrandDisplayName ?? settings.Title) : vm.BrandDisplayName;
            vm.BrandLogoUrl = string.IsNullOrWhiteSpace(vm.BrandLogoUrl) ? (effectiveSettings?.BrandLogoUrl ?? settings.LogoUrl) : vm.BrandLogoUrl;
            vm.SupportEmail = string.IsNullOrWhiteSpace(vm.SupportEmail)
                ? (effectiveSettings?.SupportEmail ?? (!string.IsNullOrWhiteSpace(settings.ContactEmail) ? settings.ContactEmail : settings.SmtpFromAddress))
                : vm.SupportEmail;
            vm.CommunicationSenderName = string.IsNullOrWhiteSpace(vm.CommunicationSenderName)
                ? (effectiveSettings?.CommunicationSenderName ?? (!string.IsNullOrWhiteSpace(settings.SmtpFromDisplayName) ? settings.SmtpFromDisplayName : settings.Title))
                : vm.CommunicationSenderName;
            vm.CommunicationReplyToEmail = string.IsNullOrWhiteSpace(vm.CommunicationReplyToEmail)
                ? (effectiveSettings?.CommunicationReplyToEmail ?? (!string.IsNullOrWhiteSpace(settings.ContactEmail) ? settings.ContactEmail : settings.SmtpFromAddress))
                : vm.CommunicationReplyToEmail;

            vm.CategoryOptions = Enum.GetValues<BusinessCategoryKind>()
                .Select(x => new SelectListItem(T(x.ToString()), x.ToString(), vm.Category == x))
                .ToList();

            vm.OwnerUserOptions = await _referenceData.GetUserOptionsAsync(vm.OwnerUserId, includeEmpty: true, ct);
            vm.CommunicationReadiness = await BuildBusinessCommunicationReadinessAsync(vm.Id, ct);
            vm.Subscription = await BuildBusinessSubscriptionSnapshotAsync(vm.Id, ct);

            if (vm.Id != Guid.Empty)
            {
                var supportCustomer = await _getBusinessOnboardingCustomerProfile.HandleAsync(vm.Id, ct);
                vm.SupportCustomerId = supportCustomer.CustomerId;
                vm.SupportCustomerProvisioned = supportCustomer.IsProvisioned;
                vm.CanProvisionSupportCustomer = supportCustomer.CanProvision;
                vm.SupportCustomerProvisioningIssue = supportCustomer.MissingReason;
                vm.SupportCustomerProvisioningEmail = supportCustomer.CandidateEmail;
                vm.SupportCustomerProvisioningCompanyName = supportCustomer.CompanyName;
            }
            else
            {
                vm.CanProvisionSupportCustomer = !string.IsNullOrWhiteSpace(vm.Name) &&
                                                 !string.IsNullOrWhiteSpace(vm.ContactEmail);
                vm.SupportCustomerProvisioningEmail = vm.ContactEmail;
                vm.SupportCustomerProvisioningCompanyName = string.IsNullOrWhiteSpace(vm.LegalName) ? vm.Name : vm.LegalName;
                vm.SupportCustomerProvisioningIssue = vm.CanProvisionSupportCustomer ? null : T("BusinessSupportCustomerProvisioningPending");
            }
        }

        private async Task SyncBusinessOnboardingCustomerProfileAsync(Guid businessId, CancellationToken ct)
        {
            if (businessId == Guid.Empty)
            {
                return;
            }

            await _ensureBusinessOnboardingCustomerProfile.HandleAsync(businessId, ct);
        }

        private async Task PopulateMemberFormOptionsAsync(BusinessMemberEditVm vm, bool includeUserSelection, CancellationToken ct)
        {
            vm.RoleOptions = Enum.GetValues<BusinessMemberRole>()
                .Select(x => new SelectListItem(T(x.ToString()), x.ToString(), vm.Role == x))
                .ToList();

            if (includeUserSelection)
            {
                vm.UserOptions = await _referenceData.GetUserOptionsAsync(vm.UserId == Guid.Empty ? null : vm.UserId, includeEmpty: false, ct);
            }
        }

        private async Task PopulateBusinessContextAsync(BusinessLocationEditVm vm, CancellationToken ct)
        {
            vm.Business = await LoadBusinessContextAsync(vm.BusinessId, ct) ?? new BusinessContextVm { Id = vm.BusinessId };
        }

        private async Task PopulateBusinessContextAsync(BusinessMemberEditVm vm, CancellationToken ct)
        {
            vm.Business = await LoadBusinessContextAsync(vm.BusinessId, ct) ?? new BusinessContextVm { Id = vm.BusinessId };
        }

        private async Task PopulateBusinessContextAsync(BusinessInvitationCreateVm vm, CancellationToken ct)
        {
            vm.Business = await LoadBusinessContextAsync(vm.BusinessId, ct) ?? new BusinessContextVm { Id = vm.BusinessId };
        }

        private async Task<BusinessCommunicationReadinessVm> BuildBusinessCommunicationReadinessAsync(Guid businessId, CancellationToken ct)
        {
            var settings = await _siteSettingCache.GetAsync(ct);
            var effectiveSettings = businessId == Guid.Empty
                ? null
                : await _businessEffectiveSettingsCache.GetAsync(businessId, ct).ConfigureAwait(false);

            var emailConfigured = settings.SmtpEnabled &&
                                  !string.IsNullOrWhiteSpace(settings.SmtpHost) &&
                                  !string.IsNullOrWhiteSpace(settings.SmtpFromAddress);

            var smsConfigured = settings.SmsEnabled &&
                                !string.IsNullOrWhiteSpace(settings.SmsProvider);

            var whatsAppConfigured = settings.WhatsAppEnabled &&
                                     !string.IsNullOrWhiteSpace(settings.WhatsAppBusinessPhoneId) &&
                                     !string.IsNullOrWhiteSpace(settings.WhatsAppAccessToken) &&
                                     !string.IsNullOrWhiteSpace(settings.WhatsAppFromPhoneE164);

            var adminEmailRoutingConfigured = !string.IsNullOrWhiteSpace(settings.AdminAlertEmailsCsv);
            var adminSmsRoutingConfigured = !string.IsNullOrWhiteSpace(settings.AdminAlertSmsRecipientsCsv);
            var failedEmailSummary = await _getEmailDispatchAuditsPage
                .GetSummaryAsync(businessId, ct)
                .ConfigureAwait(false);

            return new BusinessCommunicationReadinessVm
            {
                EmailTransportEnabled = settings.SmtpEnabled,
                EmailTransportConfigured = emailConfigured,
                SmsTransportEnabled = settings.SmsEnabled,
                SmsTransportConfigured = smsConfigured,
                WhatsAppTransportEnabled = settings.WhatsAppEnabled,
                WhatsAppTransportConfigured = whatsAppConfigured,
                AdminAlertEmailsConfigured = adminEmailRoutingConfigured,
                AdminAlertSmsConfigured = adminSmsRoutingConfigured,
                EmailTransportSummary = emailConfigured
                    ? T("BusinessCommunicationReadinessEmailConfiguredSummary")
                    : T("BusinessCommunicationReadinessEmailMissingSummary"),
                SmsTransportSummary = smsConfigured
                    ? T("BusinessCommunicationReadinessSmsConfiguredSummary")
                    : T("BusinessCommunicationReadinessSmsMissingSummary"),
                WhatsAppTransportSummary = whatsAppConfigured
                    ? T("BusinessCommunicationReadinessWhatsAppConfiguredSummary")
                    : T("BusinessCommunicationReadinessWhatsAppMissingSummary"),
                AdminRoutingSummary = adminEmailRoutingConfigured || adminSmsRoutingConfigured
                    ? T("BusinessCommunicationReadinessAdminRoutingConfiguredSummary")
                    : T("BusinessCommunicationReadinessAdminRoutingMissingSummary"),
                EffectiveCulture = effectiveSettings?.DefaultCulture ?? settings.DefaultCulture,
                EffectiveCurrency = effectiveSettings?.DefaultCurrency ?? settings.DefaultCurrency,
                EffectiveTimeZoneId = effectiveSettings?.DefaultTimeZoneId ?? settings.TimeZone ?? string.Empty,
                EffectiveBrandDisplayName = effectiveSettings?.BrandDisplayName ?? settings.Title,
                EffectiveSupportEmail = effectiveSettings?.SupportEmail,
                EffectiveSenderName = effectiveSettings?.CommunicationSenderName,
                EffectiveReplyToEmail = effectiveSettings?.CommunicationReplyToEmail,
                UsesPlatformCultureFallback = effectiveSettings?.UsesPlatformCultureFallback ?? false,
                UsesPlatformCurrencyFallback = effectiveSettings?.UsesPlatformCurrencyFallback ?? false,
                UsesPlatformTimeZoneFallback = effectiveSettings?.UsesPlatformTimeZoneFallback ?? false,
                UsesPlatformContactEmailFallback = effectiveSettings?.UsesPlatformContactEmailFallback ?? false,
                UsesPlatformSenderFallback = effectiveSettings?.UsesPlatformSenderFallback ?? false,
                TenantCommunicationIdentityConfigured =
                    !string.IsNullOrWhiteSpace(effectiveSettings?.SupportEmail) &&
                    !string.IsNullOrWhiteSpace(effectiveSettings?.CommunicationSenderName) &&
                    !string.IsNullOrWhiteSpace(effectiveSettings?.CommunicationReplyToEmail),
                FailedInvitationCount = failedEmailSummary.FailedInvitationCount,
                FailedActivationCount = failedEmailSummary.FailedActivationCount,
                FailedPasswordResetCount = failedEmailSummary.FailedPasswordResetCount,
                FailedAdminTestCount = failedEmailSummary.FailedAdminTestCount
            };
        }

        private void PopulateInvitationFormOptions(BusinessInvitationCreateVm vm)
        {
            vm.RoleOptions = Enum.GetValues<BusinessMemberRole>()
                .Select(x => new SelectListItem(T(x.ToString()), x.ToString(), vm.Role == x))
                .ToList();
        }

        private async Task<BusinessContextVm?> LoadBusinessContextAsync(Guid id, CancellationToken ct)
        {
            if (id == Guid.Empty)
            {
                return null;
            }

            var dto = await _getBusinessForEdit.HandleAsync(id, ct);
            if (dto is null)
            {
                return null;
            }

            return new BusinessContextVm
            {
                Id = dto.Id,
                Name = dto.Name,
                LegalName = dto.LegalName,
                Category = dto.Category,
                IsActive = dto.IsActive,
                OperationalStatus = dto.OperationalStatus,
                ApprovedAtUtc = dto.ApprovedAtUtc,
                SuspendedAtUtc = dto.SuspendedAtUtc,
                SuspensionReason = dto.SuspensionReason,
                MemberCount = dto.MemberCount,
                ActiveOwnerCount = dto.ActiveOwnerCount,
                LocationCount = dto.LocationCount,
                PrimaryLocationCount = dto.PrimaryLocationCount,
                InvitationCount = dto.InvitationCount,
                HasContactEmailConfigured = dto.HasContactEmailConfigured,
                HasLegalNameConfigured = dto.HasLegalNameConfigured
            };
        }

        private IActionResult RenderBusinessEditor(BusinessEditVm vm, bool isCreate)
        {
            ViewData["IsCreate"] = isCreate;
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Businesses/_BusinessEditorShell.cshtml", vm);
            }

            return isCreate ? View("Create", vm) : View("Edit", vm);
        }

        private IActionResult RenderLocationEditor(BusinessLocationEditVm vm, bool isCreate)
        {
            ViewData["IsCreate"] = isCreate;
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Businesses/_BusinessLocationEditorShell.cshtml", vm);
            }

            return isCreate ? View("CreateLocation", vm) : View("EditLocation", vm);
        }

        private IActionResult RenderMemberEditor(BusinessMemberEditVm vm, bool isCreate)
        {
            ViewData["IsCreate"] = isCreate;
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Businesses/_BusinessMemberEditorShell.cshtml", vm);
            }

            return isCreate ? View("CreateMember", vm) : View("EditMember", vm);
        }

        private IActionResult RenderInvitationEditor(BusinessInvitationCreateVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Businesses/_BusinessInvitationEditorShell.cshtml", vm);
            }

            return View("CreateInvitation", vm);
        }

        private IActionResult RenderBusinessSetupEditor(BusinessEditVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Businesses/_BusinessSetupShell.cshtml", vm);
            }

            return View("Setup", vm);
        }

        private IActionResult RenderSubscriptionWorkspace(BusinessSubscriptionWorkspaceVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("Subscription", vm);
            }

            return View("Subscription", vm);
        }

        private IActionResult RenderSubscriptionInvoicesWorkspace(BusinessSubscriptionInvoicesListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("SubscriptionInvoices", vm);
            }

            return View("SubscriptionInvoices", vm);
        }

        private IActionResult RenderBusinessesWorkspace(BusinessesListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("Index", vm);
            }

            return View("Index", vm);
        }

        private IActionResult RenderSupportQueueWorkspace(BusinessSupportQueueVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("SupportQueue", vm);
            }

            return View("SupportQueue", vm);
        }

        private IActionResult RenderMerchantReadinessWorkspace(MerchantReadinessWorkspaceVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("MerchantReadiness", vm);
            }

            return View("MerchantReadiness", vm);
        }

        private IActionResult RenderOnboardingWizardWorkspace(BusinessOnboardingWizardVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("OnboardingWizard", vm);
            }

            return View("OnboardingWizard", vm);
        }

        private IActionResult RenderLocationsWorkspace(BusinessLocationsListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("Locations", vm);
            }

            return View("Locations", vm);
        }

        private IActionResult RenderMembersWorkspace(BusinessMembersListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("Members", vm);
            }

            return View("Members", vm);
        }

        private IActionResult RenderInvitationsWorkspace(BusinessInvitationsListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("Invitations", vm);
            }

            return View("Invitations", vm);
        }

        private IActionResult RenderOwnerOverrideAuditsWorkspace(BusinessOwnerOverrideAuditsListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("OwnerOverrideAudits", vm);
            }

            return View("OwnerOverrideAudits", vm);
        }

        private IActionResult RenderStaffAccessBadgeWorkspace(BusinessStaffAccessBadgeVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("StaffAccessBadge", vm);
            }

            return View("StaffAccessBadge", vm);
        }

        private IActionResult RedirectMemberSupport(bool returnToEdit, Guid membershipId, Guid businessId, int page = 1, int pageSize = 20, string? query = null, BusinessMemberSupportFilter filter = BusinessMemberSupportFilter.All)
        {
            return returnToEdit
                ? RedirectOrHtmx(nameof(EditMember), new { id = membershipId, page, pageSize, query, filter })
                : RedirectOrHtmx(nameof(Members), new { businessId, page, pageSize, query, filter });
        }

        private IEnumerable<SelectListItem> BuildBusinessStatusItems(BusinessOperationalStatus? selectedStatus)
        {
            yield return new SelectListItem(T("AllStatuses"), string.Empty, !selectedStatus.HasValue);

            foreach (var status in Enum.GetValues<BusinessOperationalStatus>())
            {
                yield return new SelectListItem(T(status.ToString()), status.ToString(), selectedStatus == status);
            }
        }

        private IEnumerable<SelectListItem> BuildBusinessMemberFilterItems(BusinessMemberSupportFilter selectedFilter)
        {
            yield return new SelectListItem(T("BusinessMembersAllLabel"), BusinessMemberSupportFilter.All.ToString(), selectedFilter == BusinessMemberSupportFilter.All);
            yield return new SelectListItem(T("NeedsAttention"), BusinessMemberSupportFilter.Attention.ToString(), selectedFilter == BusinessMemberSupportFilter.Attention);
            yield return new SelectListItem(T("PendingActivation"), BusinessMemberSupportFilter.PendingActivation.ToString(), selectedFilter == BusinessMemberSupportFilter.PendingActivation);
            yield return new SelectListItem(T("BusinessMembersLockedLabel"), BusinessMemberSupportFilter.Locked.ToString(), selectedFilter == BusinessMemberSupportFilter.Locked);
        }

        private async Task<BusinessMemberOpsSummaryVm> BuildBusinessMemberOpsSummaryAsync(Guid businessId, CancellationToken ct)
        {
            var (_, totalCount) = await _getBusinessMembersPage.HandleAsync(businessId, 1, 1, null, BusinessMemberSupportFilter.All, ct).ConfigureAwait(false);
            var (_, pendingActivationCount) = await _getBusinessMembersPage.HandleAsync(businessId, 1, 1, null, BusinessMemberSupportFilter.PendingActivation, ct).ConfigureAwait(false);
            var (_, lockedCount) = await _getBusinessMembersPage.HandleAsync(businessId, 1, 1, null, BusinessMemberSupportFilter.Locked, ct).ConfigureAwait(false);
            var (_, attentionCount) = await _getBusinessMembersPage.HandleAsync(businessId, 1, 1, null, BusinessMemberSupportFilter.Attention, ct).ConfigureAwait(false);

            return new BusinessMemberOpsSummaryVm
            {
                TotalCount = totalCount,
                PendingActivationCount = pendingActivationCount,
                LockedCount = lockedCount,
                AttentionCount = attentionCount
            };
        }

        private List<BusinessMemberPlaybookVm> BuildBusinessMemberPlaybooks(Guid businessId)
        {
            return new List<BusinessMemberPlaybookVm>
            {
                new()
                {
                    QueueLabel = T("PendingActivation"),
                    WhyItMatters = T("BusinessMembersPendingActivationNote"),
                    OperatorAction = T("BusinessMemberSendActivationAction"),
                    QueueActionLabel = T("PendingActivation"),
                    QueueActionUrl = Url.Action("Members", "Businesses", new { businessId, filter = BusinessMemberSupportFilter.PendingActivation }) ?? string.Empty,
                    FollowUpLabel = T("MobileOperationsTitle"),
                    FollowUpUrl = Url.Action("Index", "MobileOperations", new { businessId }) ?? string.Empty
                },
                new()
                {
                    QueueLabel = T("Locked"),
                    WhyItMatters = T("UsersPlaybookLockedScope"),
                    OperatorAction = T("UsersPlaybookLockedAction"),
                    QueueActionLabel = T("UsersFilterLocked"),
                    QueueActionUrl = Url.Action("Members", "Businesses", new { businessId, filter = BusinessMemberSupportFilter.Locked }) ?? string.Empty,
                    FollowUpLabel = T("UsersFilterLocked"),
                    FollowUpUrl = Url.Action("Index", "Users", new { filter = "Locked" }) ?? string.Empty
                },
                new()
                {
                    QueueLabel = T("MissingActiveOwner"),
                    WhyItMatters = T("BusinessMembersNoActiveOwnerWarning"),
                    OperatorAction = T("BusinessMembersAssignMemberAction"),
                    QueueActionLabel = T("NeedsAttention"),
                    QueueActionUrl = Url.Action("Members", "Businesses", new { businessId, filter = BusinessMemberSupportFilter.Attention }) ?? string.Empty,
                    FollowUpLabel = T("OwnerOverrideAuditTitle"),
                    FollowUpUrl = Url.Action("OwnerOverrideAudits", "Businesses", new { businessId }) ?? string.Empty
                }
            };
        }

        private IEnumerable<SelectListItem> BuildBusinessInvitationFilterItems(BusinessInvitationQueueFilter selectedFilter)
        {
            yield return new SelectListItem(DescribeBusinessInvitationQueueLabel(BusinessInvitationQueueFilter.All), BusinessInvitationQueueFilter.All.ToString(), selectedFilter == BusinessInvitationQueueFilter.All);
            yield return new SelectListItem(DescribeBusinessInvitationQueueLabel(BusinessInvitationQueueFilter.Open), BusinessInvitationQueueFilter.Open.ToString(), selectedFilter == BusinessInvitationQueueFilter.Open);
            yield return new SelectListItem(DescribeBusinessInvitationQueueLabel(BusinessInvitationQueueFilter.Pending), BusinessInvitationQueueFilter.Pending.ToString(), selectedFilter == BusinessInvitationQueueFilter.Pending);
            yield return new SelectListItem(DescribeBusinessInvitationQueueLabel(BusinessInvitationQueueFilter.Expired), BusinessInvitationQueueFilter.Expired.ToString(), selectedFilter == BusinessInvitationQueueFilter.Expired);
            yield return new SelectListItem(DescribeBusinessInvitationQueueLabel(BusinessInvitationQueueFilter.Accepted), BusinessInvitationQueueFilter.Accepted.ToString(), selectedFilter == BusinessInvitationQueueFilter.Accepted);
            yield return new SelectListItem(DescribeBusinessInvitationQueueLabel(BusinessInvitationQueueFilter.Revoked), BusinessInvitationQueueFilter.Revoked.ToString(), selectedFilter == BusinessInvitationQueueFilter.Revoked);
        }

        private async Task<BusinessInvitationOpsSummaryVm> BuildBusinessInvitationOpsSummaryAsync(Guid businessId, CancellationToken ct)
        {
            var (_, totalCount) = await _getBusinessInvitationsPage.HandleAsync(businessId, 1, 1, null, BusinessInvitationQueueFilter.All, ct).ConfigureAwait(false);
            var (_, openCount) = await _getBusinessInvitationsPage.HandleAsync(businessId, 1, 1, null, BusinessInvitationQueueFilter.Open, ct).ConfigureAwait(false);
            var (_, pendingCount) = await _getBusinessInvitationsPage.HandleAsync(businessId, 1, 1, null, BusinessInvitationQueueFilter.Pending, ct).ConfigureAwait(false);
            var (_, expiredCount) = await _getBusinessInvitationsPage.HandleAsync(businessId, 1, 1, null, BusinessInvitationQueueFilter.Expired, ct).ConfigureAwait(false);

            return new BusinessInvitationOpsSummaryVm
            {
                TotalCount = totalCount,
                OpenCount = openCount,
                PendingCount = pendingCount,
                ExpiredCount = expiredCount
            };
        }

        private List<BusinessInvitationPlaybookVm> BuildBusinessInvitationPlaybooks(Guid businessId)
        {
            return new List<BusinessInvitationPlaybookVm>
            {
                new()
                {
                    QueueLabel = T("OpenInvitations"),
                    WhyItMatters = T("BusinessInvitationsPlaybookOpenWhyItMatters"),
                    OperatorAction = T("BusinessInvitationsPlaybookOpenAction"),
                    QueueActionLabel = T("OpenInvitations"),
                    QueueActionUrl = Url.Action("Invitations", "Businesses", new { businessId, filter = BusinessInvitationQueueFilter.Open }) ?? string.Empty,
                    FollowUpLabel = T("FailedInvitations"),
                    FollowUpUrl = Url.Action("EmailAudits", "BusinessCommunications", new { flowKey = "BusinessInvitation", status = "Failed" }) ?? string.Empty
                },
                new()
                {
                    QueueLabel = T("Pending"),
                    WhyItMatters = T("BusinessInvitationsPlaybookPendingWhyItMatters"),
                    OperatorAction = T("BusinessInvitationsPlaybookPendingAction"),
                    QueueActionLabel = T("Pending"),
                    QueueActionUrl = Url.Action("Invitations", "Businesses", new { businessId, filter = BusinessInvitationQueueFilter.Pending }) ?? string.Empty,
                    FollowUpLabel = T("BusinessSupportQueueTitle"),
                    FollowUpUrl = Url.Action("SupportQueue", "Businesses", new { businessId }) ?? string.Empty
                },
                new()
                {
                    QueueLabel = T("Expired"),
                    WhyItMatters = T("BusinessInvitationsPlaybookExpiredWhyItMatters"),
                    OperatorAction = T("BusinessInvitationsPlaybookExpiredAction"),
                    QueueActionLabel = T("Expired"),
                    QueueActionUrl = Url.Action("Invitations", "Businesses", new { businessId, filter = BusinessInvitationQueueFilter.Expired }) ?? string.Empty,
                    FollowUpLabel = T("BusinessInvitationsInviteUserAction"),
                    FollowUpUrl = Url.Action("CreateInvitation", "Businesses", new { businessId }) ?? string.Empty
                }
            };
        }

        private string DescribeBusinessInvitationQueueLabel(BusinessInvitationQueueFilter filter)
        {
            return filter switch
            {
                BusinessInvitationQueueFilter.All => T("BusinessInvitationsAllLabel"),
                BusinessInvitationQueueFilter.Open => T("OpenInvitations"),
                BusinessInvitationQueueFilter.Pending => T("Pending"),
                BusinessInvitationQueueFilter.Expired => T("Expired"),
                BusinessInvitationQueueFilter.Accepted => T("Accepted"),
                BusinessInvitationQueueFilter.Revoked => T("Revoked"),
                _ => T("BusinessInvitationsAllLabel")
            };
        }

        private List<BusinessOwnerOverrideAuditPlaybookVm> BuildBusinessOwnerOverrideAuditPlaybooks(Guid businessId)
        {
            return new List<BusinessOwnerOverrideAuditPlaybookVm>
            {
                new()
                {
                    QueueLabel = T("BusinessOwnerOverrideForceRemove"),
                    WhyItMatters = T("BusinessOwnerOverridePlaybookForceRemoveWhyItMatters"),
                    OperatorAction = T("BusinessOwnerOverridePlaybookForceRemoveAction"),
                    QueueActionLabel = T("CommonMembers"),
                    QueueActionUrl = Url.Action("Members", "Businesses", new { businessId, filter = BusinessMemberSupportFilter.Attention }) ?? string.Empty,
                    FollowUpLabel = T("BusinessSupportQueueTitle"),
                    FollowUpUrl = Url.Action("SupportQueue", "Businesses", new { businessId }) ?? string.Empty
                },
                new()
                {
                    QueueLabel = T("BusinessOwnerOverrideDemoteDeactivate"),
                    WhyItMatters = T("BusinessOwnerOverridePlaybookDemoteWhyItMatters"),
                    OperatorAction = T("BusinessOwnerOverridePlaybookDemoteAction"),
                    QueueActionLabel = T("NeedsAttention"),
                    QueueActionUrl = Url.Action("Members", "Businesses", new { businessId, filter = BusinessMemberSupportFilter.Attention }) ?? string.Empty,
                    FollowUpLabel = T("MerchantReadinessTitle"),
                    FollowUpUrl = Url.Action("MerchantReadiness", "Businesses", new { businessId }) ?? string.Empty
                },
                new()
                {
                    QueueLabel = T("MissingActiveOwner"),
                    WhyItMatters = T("BusinessOwnerOverridePlaybookMissingOwnerWhyItMatters"),
                    OperatorAction = T("BusinessOwnerOverridePlaybookMissingOwnerAction"),
                    QueueActionLabel = T("CommonMembers"),
                    QueueActionUrl = Url.Action("Members", "Businesses", new { businessId }) ?? string.Empty,
                    FollowUpLabel = T("CommonSetup"),
                    FollowUpUrl = Url.Action("Setup", "Businesses", new { id = businessId }) ?? string.Empty
                }
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
            return string.Equals(Request.Headers["HX-Request"], "true", StringComparison.OrdinalIgnoreCase);
        }

        private string? GetCurrentActorDisplayName()
        {
            var explicitName = User.FindFirstValue(ClaimTypes.Name)
                               ?? User.FindFirstValue(ClaimTypes.Email)
                               ?? User.Identity?.Name;

            return string.IsNullOrWhiteSpace(explicitName) ? null : explicitName.Trim();
        }

        private static BusinessEditVm MapBusinessEditVm(BusinessEditDto dto)
        {
            return new BusinessEditVm
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion,
                Name = dto.Name,
                LegalName = dto.LegalName,
                TaxId = dto.TaxId,
                ShortDescription = dto.ShortDescription,
                WebsiteUrl = dto.WebsiteUrl,
                ContactEmail = dto.ContactEmail,
                ContactPhoneE164 = dto.ContactPhoneE164,
                Category = dto.Category,
                DefaultCurrency = dto.DefaultCurrency,
                DefaultCulture = dto.DefaultCulture,
                DefaultTimeZoneId = dto.DefaultTimeZoneId,
                AdminTextOverridesJson = dto.AdminTextOverridesJson,
                BrandDisplayName = dto.BrandDisplayName,
                BrandLogoUrl = dto.BrandLogoUrl,
                BrandPrimaryColorHex = dto.BrandPrimaryColorHex,
                BrandSecondaryColorHex = dto.BrandSecondaryColorHex,
                SupportEmail = dto.SupportEmail,
                CommunicationSenderName = dto.CommunicationSenderName,
                CommunicationReplyToEmail = dto.CommunicationReplyToEmail,
                CustomerEmailNotificationsEnabled = dto.CustomerEmailNotificationsEnabled,
                CustomerMarketingEmailsEnabled = dto.CustomerMarketingEmailsEnabled,
                OperationalAlertEmailsEnabled = dto.OperationalAlertEmailsEnabled,
                IsActive = dto.IsActive,
                OperationalStatus = dto.OperationalStatus,
                ApprovedAtUtc = dto.ApprovedAtUtc,
                SuspendedAtUtc = dto.SuspendedAtUtc,
                SuspensionReason = dto.SuspensionReason,
                MemberCount = dto.MemberCount,
                ActiveOwnerCount = dto.ActiveOwnerCount,
                LocationCount = dto.LocationCount,
                PrimaryLocationCount = dto.PrimaryLocationCount,
                InvitationCount = dto.InvitationCount,
                HasContactEmailConfigured = dto.HasContactEmailConfigured,
                HasLegalNameConfigured = dto.HasLegalNameConfigured
            };
        }

        private async Task<BusinessSubscriptionSnapshotVm> BuildBusinessSubscriptionSnapshotAsync(Guid businessId, CancellationToken ct)
        {
            if (businessId == Guid.Empty)
            {
                return new BusinessSubscriptionSnapshotVm();
            }

            var result = await _getBusinessSubscriptionStatus.HandleAsync(
                businessId,
                CultureInfo.CurrentUICulture.Name,
                ct).ConfigureAwait(false);
            if (!result.Succeeded || result.Value is null)
            {
                return new BusinessSubscriptionSnapshotVm
                {
                    HasSubscription = false,
                    Status = T("Unavailable")
                };
            }

            return new BusinessSubscriptionSnapshotVm
            {
                HasSubscription = result.Value.HasSubscription,
                SubscriptionId = result.Value.SubscriptionId,
                RowVersion = result.Value.RowVersion,
                Status = result.Value.Status,
                Provider = result.Value.Provider,
                PlanCode = result.Value.PlanCode,
                PlanName = result.Value.PlanName,
                Currency = result.Value.Currency,
                UnitPriceMinor = result.Value.UnitPriceMinor,
                StartedAtUtc = result.Value.StartedAtUtc,
                CurrentPeriodEndUtc = result.Value.CurrentPeriodEndUtc,
                TrialEndsAtUtc = result.Value.TrialEndsAtUtc,
                CanceledAtUtc = result.Value.CanceledAtUtc,
                CancelAtPeriodEnd = result.Value.CancelAtPeriodEnd
            };
        }

        private async Task<BusinessSubscriptionWorkspaceVm> BuildBusinessSubscriptionWorkspaceAsync(BusinessContextVm business, CancellationToken ct)
        {
            var subscription = await BuildBusinessSubscriptionSnapshotAsync(business.Id, ct);
            var settings = await _siteSettingCache.GetAsync(ct);
            var managementWebsiteUrl = string.IsNullOrWhiteSpace(settings.BusinessManagementWebsiteUrl)
                ? null
                : settings.BusinessManagementWebsiteUrl.Trim();
            var managementWebsiteConfigured = !string.IsNullOrWhiteSpace(managementWebsiteUrl);
            var workspaceManagementWebsiteUrl = BuildSubscriptionManagementWebsiteUrl(managementWebsiteUrl, business.Id, planCode: null);
            var plans = await _getBillingPlans.HandleAsync(
                activeOnly: true,
                culture: CultureInfo.CurrentUICulture.Name,
                ct);
            var recentInvoices = await _getBusinessSubscriptionInvoicesPage.HandleAsync(
                business.Id,
                page: 1,
                pageSize: 5,
                query: null,
                filter: BusinessSubscriptionInvoiceQueueFilter.All,
                culture: CultureInfo.CurrentUICulture.Name,
                ct).ConfigureAwait(false);
            var invoiceSummary = await _getBusinessSubscriptionInvoiceOpsSummary.HandleAsync(business.Id, ct).ConfigureAwait(false);
            var planVms = new List<BusinessBillingPlanVm>();

            foreach (var x in plans.Items)
            {
                var validation = await _createSubscriptionCheckoutIntent.ValidateAsync(business.Id, x.Id, ct);
                var isCurrentPlan = subscription.HasSubscription
                    && !string.IsNullOrWhiteSpace(subscription.PlanCode)
                    && string.Equals(subscription.PlanCode, x.Code, StringComparison.OrdinalIgnoreCase);
                var canOpenManagementWebsite = managementWebsiteConfigured && validation.Succeeded;
                var planManagementWebsiteUrl = BuildSubscriptionManagementWebsiteUrl(managementWebsiteUrl, business.Id, x.Code);

                planVms.Add(new BusinessBillingPlanVm
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    Description = x.Description,
                    PriceMinor = x.PriceMinor,
                    Currency = x.Currency,
                    Interval = x.Interval,
                    IntervalCount = x.IntervalCount,
                    TrialDays = x.TrialDays,
                    IsActive = x.IsActive,
                    CheckoutReady = validation.Succeeded,
                    CheckoutReadinessLabel = validation.Succeeded ? T("BusinessSubscriptionCheckoutReady") : (validation.Error ?? T("NotReady")),
                    IsCurrentPlan = isCurrentPlan,
                    CanOpenManagementWebsite = canOpenManagementWebsite,
                    ManagementWebsiteUrl = canOpenManagementWebsite ? planManagementWebsiteUrl : null,
                    HandoffActionLabel = isCurrentPlan
                        ? T("BusinessSubscriptionManageCurrentPlan")
                        : subscription.HasSubscription ? T("BusinessSubscriptionUpgradeToPlan") : T("BusinessSubscriptionStartWithPlan"),
                    HandoffLabel = isCurrentPlan
                        ? T("BusinessSubscriptionCurrentPlanBadge")
                        : canOpenManagementWebsite
                            ? T("BusinessSubscriptionOpenBillingWebsite")
                            : managementWebsiteConfigured
                                ? T("BusinessSubscriptionResolvePrerequisites")
                                : T("BusinessSubscriptionConfigureWebsite")
                });
            }

            return new BusinessSubscriptionWorkspaceVm
            {
                Business = business,
                Subscription = subscription,
                ManagementWebsiteConfigured = managementWebsiteConfigured,
                ManagementWebsiteUrl = workspaceManagementWebsiteUrl,
                HandoffSummary = new BusinessSubscriptionHandoffSummaryVm
                {
                    TotalPlans = planVms.Count,
                    ReadyPlanCount = planVms.Count(x => x.CanOpenManagementWebsite),
                    BlockedPlanCount = planVms.Count(x => !x.CanOpenManagementWebsite),
                    CurrentPlanCount = planVms.Count(x => x.IsCurrentPlan)
                },
                Plans = planVms,
                InvoiceSummary = MapBusinessSubscriptionInvoiceOpsSummaryVm(invoiceSummary),
                RecentInvoices = recentInvoices.Items.Select(MapBusinessSubscriptionInvoiceListItemVm).ToList(),
                Playbooks = BuildSubscriptionPlaybooks(business.Id, subscription, managementWebsiteConfigured)
            };
        }

        private static GeoCoordinateDto? BuildCoordinate(BusinessLocationEditVm vm)
        {
            if (!vm.Latitude.HasValue || !vm.Longitude.HasValue)
            {
                return null;
            }

            return new GeoCoordinateDto
            {
                Latitude = vm.Latitude.Value,
                Longitude = vm.Longitude.Value,
                AltitudeMeters = vm.AltitudeMeters
            };
        }

        private static string? BuildSubscriptionManagementWebsiteUrl(string? baseUrl, Guid businessId, string? planCode)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return null;
            }

            var separator = baseUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
            var url = $"{baseUrl}{separator}businessId={WebUtility.UrlEncode(businessId.ToString())}";

            if (!string.IsNullOrWhiteSpace(planCode))
            {
                url = $"{url}&planCode={WebUtility.UrlEncode(planCode)}";
            }

            return url;
        }

        private static IEnumerable<SelectListItem> BuildPageSizeItems(int selectedPageSize)
        {
            var sizes = new[] { 10, 20, 50, 100 };
            return sizes.Select(x => new SelectListItem(x.ToString(), x.ToString(), x == selectedPageSize)).ToList();
        }

        private static BusinessSupportSummaryVm MapSupportSummaryVm(BusinessSupportSummaryDto summary)
        {
            return new BusinessSupportSummaryVm
            {
                AttentionBusinessCount = summary.AttentionBusinessCount,
                PendingApprovalBusinessCount = summary.PendingApprovalBusinessCount,
                SuspendedBusinessCount = summary.SuspendedBusinessCount,
                ApprovedInactiveBusinessCount = summary.ApprovedInactiveBusinessCount,
                MissingOwnerBusinessCount = summary.MissingOwnerBusinessCount,
                MissingPrimaryLocationBusinessCount = summary.MissingPrimaryLocationBusinessCount,
                MissingContactEmailBusinessCount = summary.MissingContactEmailBusinessCount,
                MissingLegalNameBusinessCount = summary.MissingLegalNameBusinessCount,
                PendingInvitationCount = summary.PendingInvitationCount,
                OpenInvitationCount = summary.OpenInvitationCount,
                PendingActivationMemberCount = summary.PendingActivationMemberCount,
                LockedMemberCount = summary.LockedMemberCount,
                FailedInvitationCount = summary.FailedInvitationCount,
                FailedActivationCount = summary.FailedActivationCount,
                FailedPasswordResetCount = summary.FailedPasswordResetCount,
                FailedAdminTestCount = summary.FailedAdminTestCount
            };
        }

        private string BuildSupportAuditRecommendedAction(EmailDispatchAuditListItemDto item)
        {
            if (string.Equals(item.FlowKey, "BusinessInvitation", StringComparison.OrdinalIgnoreCase))
            {
                return item.BusinessId.HasValue
                    ? T("BusinessSupportAuditInvitationBusinessAction")
                    : T("BusinessSupportAuditInvitationGenericAction");
            }

            if (string.Equals(item.FlowKey, "AccountActivation", StringComparison.OrdinalIgnoreCase))
            {
                return item.BusinessId.HasValue
                    ? T("BusinessSupportAuditActivationBusinessAction")
                    : T("BusinessSupportAuditActivationGenericAction");
            }

            if (string.Equals(item.FlowKey, "PasswordReset", StringComparison.OrdinalIgnoreCase))
            {
                return T("BusinessSupportAuditPasswordResetAction");
            }

            return T("BusinessSupportAuditGenericAction");
        }

        private List<MerchantReadinessPlaybookVm> BuildMerchantReadinessPlaybooks(Guid? businessId)
        {
            return new List<MerchantReadinessPlaybookVm>
            {
                new()
                {
                    Title = T("MerchantReadinessPlaybookApprovalTitle"),
                    ScopeNote = T("MerchantReadinessPlaybookApprovalScope"),
                    OperatorAction = T("MerchantReadinessPlaybookApprovalAction"),
                    QueueActionLabel = T("PendingApproval"),
                    QueueActionUrl = Url.Action("Index", "Businesses", new { operationalStatus = BusinessOperationalStatus.PendingApproval }) ?? string.Empty,
                    FollowUpLabel = T("BusinessSupportQueueTitle"),
                    FollowUpUrl = Url.Action("SupportQueue", "Businesses", new { businessId }) ?? string.Empty
                },
                new()
                {
                    Title = T("MerchantReadinessPlaybookSetupTitle"),
                    ScopeNote = T("MerchantReadinessPlaybookSetupScope"),
                    OperatorAction = T("MerchantReadinessPlaybookSetupAction"),
                    QueueActionLabel = T("NeedsAttention"),
                    QueueActionUrl = Url.Action("Index", "Businesses", new { attentionOnly = true }) ?? string.Empty,
                    FollowUpLabel = T("CommonSetup"),
                    FollowUpUrl = Url.Action("MerchantReadiness", "Businesses", new { businessId }) ?? string.Empty
                },
                new()
                {
                    Title = T("MerchantReadinessPlaybookBillingTitle"),
                    ScopeNote = T("MerchantReadinessPlaybookBillingScope"),
                    OperatorAction = T("MerchantReadinessPlaybookBillingAction"),
                    QueueActionLabel = T("ApprovedInactive"),
                    QueueActionUrl = Url.Action("Index", "Businesses", new { readinessFilter = BusinessReadinessQueueFilter.ApprovedInactive }) ?? string.Empty,
                    FollowUpLabel = T("Payments"),
                    FollowUpUrl = Url.Action("Payments", "Billing") ?? string.Empty
                }
            };
        }

        private static BusinessSubscriptionInvoiceOpsSummaryVm MapBusinessSubscriptionInvoiceOpsSummaryVm(BusinessSubscriptionInvoiceOpsSummaryDto dto)
        {
            return new BusinessSubscriptionInvoiceOpsSummaryVm
            {
                TotalCount = dto.TotalCount,
                OpenCount = dto.OpenCount,
                PaidCount = dto.PaidCount,
                DraftCount = dto.DraftCount,
                UncollectibleCount = dto.UncollectibleCount,
                HostedLinkMissingCount = dto.HostedLinkMissingCount,
                StripeCount = dto.StripeCount,
                OverdueCount = dto.OverdueCount,
                PdfMissingCount = dto.PdfMissingCount
            };
        }

        private static BusinessSubscriptionInvoiceListItemVm MapBusinessSubscriptionInvoiceListItemVm(BusinessSubscriptionInvoiceListItemDto dto)
        {
            return new BusinessSubscriptionInvoiceListItemVm
            {
                Id = dto.Id,
                BusinessId = dto.BusinessId,
                BusinessSubscriptionId = dto.BusinessSubscriptionId,
                Provider = dto.Provider,
                ProviderInvoiceId = dto.ProviderInvoiceId,
                Status = dto.Status,
                TotalMinor = dto.TotalMinor,
                Currency = dto.Currency,
                IssuedAtUtc = dto.IssuedAtUtc,
                DueAtUtc = dto.DueAtUtc,
                PaidAtUtc = dto.PaidAtUtc,
                HostedInvoiceUrl = dto.HostedInvoiceUrl,
                PdfUrl = dto.PdfUrl,
                FailureReason = dto.FailureReason,
                PlanName = dto.PlanName,
                PlanCode = dto.PlanCode,
                HasHostedInvoiceUrl = dto.HasHostedInvoiceUrl,
                HasPdfUrl = dto.HasPdfUrl,
                IsStripe = dto.IsStripe,
                IsOverdue = dto.IsOverdue
            };
        }

        private BusinessOnboardingWizardVm BuildOnboardingWizardVm(BusinessEditVm business)
        {
            var profileComplete = !string.IsNullOrWhiteSpace(business.Name)
                && !string.IsNullOrWhiteSpace(business.LegalName)
                && !string.IsNullOrWhiteSpace(business.ContactEmail);
            var ownerComplete = business.ActiveOwnerCount > 0;
            var locationComplete = business.PrimaryLocationCount > 0;
            var commercialComplete = business.Subscription.HasSubscription;
            var communicationComplete = business.CustomerEmailNotificationsEnabled
                && business.CommunicationReadiness.EmailTransportConfigured
                && !string.IsNullOrWhiteSpace(business.SupportEmail)
                && !string.IsNullOrWhiteSpace(business.CommunicationSenderName)
                && !string.IsNullOrWhiteSpace(business.CommunicationReplyToEmail);
            var storefrontComplete = business.OperationalStatus == BusinessOperationalStatus.Approved && business.IsActive;
            var requiredPrerequisitesComplete = profileComplete && ownerComplete && locationComplete;

            var steps = new List<BusinessOnboardingWizardStepVm>
            {
                BuildWizardStep(1, "BusinessOnboardingWizardStepBusinessProfile", profileComplete, true, profileComplete ? "BusinessOnboardingWizardBusinessProfileComplete" : "BusinessOnboardingWizardBusinessProfileMissing", "EditBusiness", Url.Action("Edit", "Businesses", new { id = business.Id })),
                BuildWizardStep(2, "BusinessOnboardingWizardStepCommercialPlan", commercialComplete, false, commercialComplete ? "BusinessOnboardingWizardCommercialPlanComplete" : "BusinessOnboardingWizardCommercialPlanMissing", "Subscription", Url.Action("Subscription", "Businesses", new { businessId = business.Id })),
                BuildWizardStep(3, "BusinessOnboardingWizardStepOwnerStaff", ownerComplete, true, ownerComplete ? "BusinessOnboardingWizardOwnerStaffComplete" : "BusinessOnboardingWizardOwnerStaffMissing", "Members", Url.Action("Members", "Businesses", new { businessId = business.Id, filter = BusinessMemberSupportFilter.Attention })),
                BuildWizardStep(4, "BusinessOnboardingWizardStepLocations", locationComplete, true, locationComplete ? "BusinessOnboardingWizardLocationsComplete" : "BusinessOnboardingWizardLocationsMissing", "Locations", Url.Action("Locations", "Businesses", new { businessId = business.Id })),
                BuildWizardStep(5, "BusinessOnboardingWizardStepLoyalty", false, false, "BusinessOnboardingWizardLoyaltySummary", "Loyalty", Url.Action("Programs", "Loyalty", new { businessId = business.Id })),
                BuildWizardStep(6, "BusinessOnboardingWizardStepCommunication", communicationComplete, false, communicationComplete ? "BusinessOnboardingWizardCommunicationComplete" : "BusinessOnboardingWizardCommunicationMissing", "BusinessCommunicationProfileTitle", Url.Action("Details", "BusinessCommunications", new { businessId = business.Id })),
                BuildWizardStep(7, "BusinessOnboardingWizardStepStorefrontVisibility", storefrontComplete, false, storefrontComplete ? "BusinessOnboardingWizardStorefrontComplete" : "BusinessOnboardingWizardStorefrontMissing", "MerchantReadinessTitle", Url.Action("MerchantReadiness", "Businesses", new { businessId = business.Id })),
                BuildWizardStep(8, "BusinessOnboardingWizardStepReviewActivate", requiredPrerequisitesComplete && storefrontComplete, true, requiredPrerequisitesComplete ? "BusinessOnboardingWizardReviewReady" : "FinalizeBusinessOnboardingBlocked", "FinalizeBusinessOnboarding", Url.Action("OnboardingWizard", "Businesses", new { id = business.Id }))
            };

            return new BusinessOnboardingWizardVm
            {
                Business = business,
                Steps = steps,
                RequiredStepCount = steps.Count(x => x.IsRequired),
                CompletedRequiredStepCount = steps.Count(x => x.IsRequired && x.IsComplete),
                CanFinalize = requiredPrerequisitesComplete &&
                    (business.OperationalStatus != BusinessOperationalStatus.Approved || !business.IsActive)
            };
        }

        private BusinessOnboardingWizardStepVm BuildWizardStep(
            int number,
            string titleKey,
            bool isComplete,
            bool isRequired,
            string summaryKey,
            string actionLabelKey,
            string? actionUrl)
        {
            return new BusinessOnboardingWizardStepVm
            {
                Number = number,
                Title = T(titleKey),
                Summary = T(summaryKey),
                IsComplete = isComplete,
                IsRequired = isRequired,
                ActionLabel = T(actionLabelKey),
                ActionUrl = actionUrl ?? string.Empty
            };
        }

        private List<BusinessSubscriptionPlaybookVm> BuildSubscriptionPlaybooks(Guid businessId, BusinessSubscriptionSnapshotVm subscription, bool managementWebsiteConfigured)
        {
            var items = new List<BusinessSubscriptionPlaybookVm>
            {
                new()
                {
                    QueueLabel = T("BusinessManagementWebsite"),
                    WhyItMatters = T("BusinessSubscriptionPlaybookManagementWebsiteWhyItMatters"),
                    OperatorAction = managementWebsiteConfigured
                        ? T("BusinessSubscriptionPlaybookManagementWebsiteActionConfigured")
                        : T("BusinessSubscriptionPlaybookManagementWebsiteActionMissing"),
                    QueueActionLabel = T("CommonSetup"),
                    QueueActionUrl = Url.Action("Edit", "SiteSettings", new { fragment = "site-settings-business-app" }) ?? string.Empty,
                    FollowUpLabel = T("CommonPayments"),
                    FollowUpUrl = Url.Action("Payments", "Billing", new { businessId }) ?? string.Empty
                },
                new()
                {
                    QueueLabel = T("BusinessSubscriptionCancellationPolicy"),
                    WhyItMatters = T("BusinessSubscriptionPlaybookCancellationWhyItMatters"),
                    OperatorAction = subscription.HasSubscription
                        ? T("BusinessSubscriptionPlaybookCancellationActionActive")
                        : T("BusinessSubscriptionPlaybookCancellationActionInactive"),
                    QueueActionLabel = T("BusinessSubscriptionOpenInvoiceQueue"),
                    QueueActionUrl = Url.Action("SubscriptionInvoices", "Businesses", new { businessId, filter = BusinessSubscriptionInvoiceQueueFilter.Open }) ?? string.Empty,
                    FollowUpLabel = T("CommonPayments"),
                    FollowUpUrl = Url.Action("Payments", "Billing", new { businessId }) ?? string.Empty
                }
            };

            if (!subscription.HasSubscription)
            {
                items.Add(new BusinessSubscriptionPlaybookVm
                {
                    QueueLabel = T("BusinessSubscriptionNoActivePlan"),
                    WhyItMatters = T("BusinessSubscriptionPlaybookNoActivePlanWhyItMatters"),
                    OperatorAction = T("BusinessSubscriptionPlaybookNoActivePlanAction"),
                    QueueActionLabel = T("BusinessSubscriptionOpenInvoiceQueue"),
                    QueueActionUrl = Url.Action("SubscriptionInvoices", "Businesses", new { businessId, filter = BusinessSubscriptionInvoiceQueueFilter.All }) ?? string.Empty,
                    FollowUpLabel = T("BusinessSupportQueueTitle"),
                    FollowUpUrl = Url.Action("SupportQueue", "Businesses", new { businessId }) ?? string.Empty
                });
            }

            return items;
        }

        private IEnumerable<SelectListItem> BuildBusinessSubscriptionInvoiceFilterItems(BusinessSubscriptionInvoiceQueueFilter selectedFilter)
        {
            yield return new SelectListItem(T("BusinessSubscriptionAllInvoicesLabel"), BusinessSubscriptionInvoiceQueueFilter.All.ToString(), selectedFilter == BusinessSubscriptionInvoiceQueueFilter.All);
            yield return new SelectListItem(T("CommonOpen"), BusinessSubscriptionInvoiceQueueFilter.Open.ToString(), selectedFilter == BusinessSubscriptionInvoiceQueueFilter.Open);
            yield return new SelectListItem(T("CommonPaid"), BusinessSubscriptionInvoiceQueueFilter.Paid.ToString(), selectedFilter == BusinessSubscriptionInvoiceQueueFilter.Paid);
            yield return new SelectListItem(T("CommonDraft"), BusinessSubscriptionInvoiceQueueFilter.Draft.ToString(), selectedFilter == BusinessSubscriptionInvoiceQueueFilter.Draft);
            yield return new SelectListItem(T("CommonUncollectible"), BusinessSubscriptionInvoiceQueueFilter.Uncollectible.ToString(), selectedFilter == BusinessSubscriptionInvoiceQueueFilter.Uncollectible);
            yield return new SelectListItem(T("BusinessSubscriptionHostedLinkMissing"), BusinessSubscriptionInvoiceQueueFilter.HostedLinkMissing.ToString(), selectedFilter == BusinessSubscriptionInvoiceQueueFilter.HostedLinkMissing);
            yield return new SelectListItem(T("CommonStripe"), BusinessSubscriptionInvoiceQueueFilter.Stripe.ToString(), selectedFilter == BusinessSubscriptionInvoiceQueueFilter.Stripe);
            yield return new SelectListItem(T("CommonOverdue"), BusinessSubscriptionInvoiceQueueFilter.Overdue.ToString(), selectedFilter == BusinessSubscriptionInvoiceQueueFilter.Overdue);
            yield return new SelectListItem(T("BusinessSubscriptionReviewPdfMissing"), BusinessSubscriptionInvoiceQueueFilter.PdfMissing.ToString(), selectedFilter == BusinessSubscriptionInvoiceQueueFilter.PdfMissing);
        }

        private IEnumerable<SelectListItem> BuildBusinessLocationFilterItems(BusinessLocationQueueFilter selectedFilter)
        {
            yield return new SelectListItem(T("CommonAll"), BusinessLocationQueueFilter.All.ToString(), selectedFilter == BusinessLocationQueueFilter.All);
            yield return new SelectListItem(T("BusinessLocationsPrimaryLocationLabel"), BusinessLocationQueueFilter.Primary.ToString(), selectedFilter == BusinessLocationQueueFilter.Primary);
            yield return new SelectListItem(T("MissingAddress"), BusinessLocationQueueFilter.MissingAddress.ToString(), selectedFilter == BusinessLocationQueueFilter.MissingAddress);
            yield return new SelectListItem(T("BusinessLocationsMissingCoordinatesLabel"), BusinessLocationQueueFilter.MissingCoordinates.ToString(), selectedFilter == BusinessLocationQueueFilter.MissingCoordinates);
        }

        private List<BusinessLocationPlaybookVm> BuildBusinessLocationPlaybooks(Guid businessId)
        {
            return new List<BusinessLocationPlaybookVm>
            {
                new()
                {
                    QueueLabel = T("BusinessLocationsPrimaryLocationLabel"),
                    WhyItMatters = T("BusinessLocationsPlaybookPrimaryWhyItMatters"),
                    OperatorAction = T("BusinessLocationsPlaybookPrimaryAction"),
                    QueueActionUrl = Url.Action("Locations", "Businesses", new { businessId, filter = BusinessLocationQueueFilter.Primary }) ?? string.Empty
                },
                new()
                {
                    QueueLabel = T("MissingAddress"),
                    WhyItMatters = T("BusinessLocationsPlaybookMissingAddressWhyItMatters"),
                    OperatorAction = T("BusinessLocationsPlaybookMissingAddressAction"),
                    QueueActionUrl = Url.Action("Locations", "Businesses", new { businessId, filter = BusinessLocationQueueFilter.MissingAddress }) ?? string.Empty
                },
                new()
                {
                    QueueLabel = T("BusinessLocationsMissingCoordinatesLabel"),
                    WhyItMatters = T("BusinessLocationsPlaybookMissingCoordinatesWhyItMatters"),
                    OperatorAction = T("BusinessLocationsPlaybookMissingCoordinatesAction"),
                    QueueActionUrl = Url.Action("Locations", "Businesses", new { businessId, filter = BusinessLocationQueueFilter.MissingCoordinates }) ?? string.Empty
                }
            };
        }

        private static string BuildStaffAccessBadgePayload(BusinessMemberDetailDto member, BusinessContextVm business, DateTime issuedAtUtc, DateTime expiresAtUtc)
        {
            var payload = new
            {
                Type = "staff-access-badge",
                Version = 1,
                BusinessId = business.Id,
                BusinessName = business.Name,
                OperatorEmail = member.UserEmail,
                Role = member.Role.ToString(),
                IssuedAtUtc = issuedAtUtc,
                ExpiresAtUtc = expiresAtUtc,
                Nonce = Guid.NewGuid().ToString("N")
            };

            return JsonSerializer.Serialize(payload);
        }

        private static string BuildQrCodeDataUrl(string payload)
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
            var png = new PngByteQRCode(data);
            var bytes = png.GetGraphic(20);
            return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
        }
    }
}
