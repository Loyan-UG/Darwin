using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly GetBusinessSupportSummaryHandler _getBusinessSupportSummary;
        private readonly GetBusinessSubscriptionStatusHandler _getBusinessSubscriptionStatus;
        private readonly GetBusinessSubscriptionInvoicesPageHandler _getBusinessSubscriptionInvoicesPage;
        private readonly GetBusinessSubscriptionInvoiceOpsSummaryHandler _getBusinessSubscriptionInvoiceOpsSummary;
        private readonly GetBillingPlansHandler _getBillingPlans;
        private readonly SetCancelAtPeriodEndHandler _setCancelAtPeriodEnd;
        private readonly CreateSubscriptionCheckoutIntentHandler _createSubscriptionCheckoutIntent;
        private readonly GetEmailDispatchAuditsPageHandler _getEmailDispatchAuditsPage;
        private readonly CreateBusinessHandler _createBusiness;
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

        public BusinessesController(
            GetBusinessesPageHandler getBusinessesPage,
            GetBusinessForEditHandler getBusinessForEdit,
            GetBusinessSupportSummaryHandler getBusinessSupportSummary,
            GetBusinessSubscriptionStatusHandler getBusinessSubscriptionStatus,
            GetBusinessSubscriptionInvoicesPageHandler getBusinessSubscriptionInvoicesPage,
            GetBusinessSubscriptionInvoiceOpsSummaryHandler getBusinessSubscriptionInvoiceOpsSummary,
            GetBillingPlansHandler getBillingPlans,
            SetCancelAtPeriodEndHandler setCancelAtPeriodEnd,
            CreateSubscriptionCheckoutIntentHandler createSubscriptionCheckoutIntent,
            GetEmailDispatchAuditsPageHandler getEmailDispatchAuditsPage,
            CreateBusinessHandler createBusiness,
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
            ISiteSettingCache siteSettingCache)
        {
            _getBusinessesPage = getBusinessesPage;
            _getBusinessForEdit = getBusinessForEdit;
            _getBusinessSupportSummary = getBusinessSupportSummary;
            _getBusinessSubscriptionStatus = getBusinessSubscriptionStatus;
            _getBusinessSubscriptionInvoicesPage = getBusinessSubscriptionInvoicesPage;
            _getBusinessSubscriptionInvoiceOpsSummary = getBusinessSubscriptionInvoiceOpsSummary;
            _getBillingPlans = getBillingPlans;
            _setCancelAtPeriodEnd = setCancelAtPeriodEnd;
            _createSubscriptionCheckoutIntent = createSubscriptionCheckoutIntent;
            _getEmailDispatchAuditsPage = getEmailDispatchAuditsPage;
            _createBusiness = createBusiness;
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
        public async Task<IActionResult> SupportQueue(CancellationToken ct = default)
        {
            var summary = await _getBusinessSupportSummary.HandleAsync(null, ct).ConfigureAwait(false);
            var (attentionBusinesses, _) = await _getBusinessesPage.HandleAsync(1, 10, null, null, true, readinessFilter: null, ct).ConfigureAwait(false);
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

            var vm = new BusinessSupportQueueVm
            {
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
                AttentionBusinesses = attentionBusinesses.Select(x => new BusinessListItemVm
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
                }).ToList(),
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
                }).ToList()
            };

            return RenderSupportQueueWorkspace(vm);
        }

        [HttpGet]
        [PermissionAuthorize(PermissionKeys.ManageBusinessSupport)]
        public async Task<IActionResult> MerchantReadiness(CancellationToken ct = default)
        {
            var summary = await _getBusinessSupportSummary.HandleAsync(null, ct).ConfigureAwait(false);
            var (attentionBusinesses, _) = await _getBusinessesPage.HandleAsync(1, 12, null, null, true, readinessFilter: null, ct).ConfigureAwait(false);

            var items = new List<MerchantReadinessItemVm>();
            foreach (var business in attentionBusinesses)
            {
                var subscription = await BuildBusinessSubscriptionSnapshotAsync(business.Id, ct).ConfigureAwait(false);
                items.Add(new MerchantReadinessItemVm
                {
                    Id = business.Id,
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
                Summary = MapSupportSummaryVm(summary),
                Items = items,
                Playbooks = BuildMerchantReadinessPlaybooks()
            };

            return RenderMerchantReadinessWorkspace(vm);
        }

        [HttpGet]
        [PermissionAuthorize(PermissionKeys.ManageBusinessSupport)]
        public async Task<IActionResult> SupportQueueSummaryFragment(CancellationToken ct = default)
        {
            var summary = await _getBusinessSupportSummary.HandleAsync(null, ct).ConfigureAwait(false);
            return PartialView("~/Views/Businesses/_SupportQueueSummary.cshtml", MapSupportSummaryVm(summary));
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

                TempData["Success"] = vm.OwnerUserId.HasValue
                    ? "Business created and owner assigned."
                    : "Business created. Next, add a primary location and assign an owner.";
                return RedirectOrHtmx(nameof(Edit), new { id = businessId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateBusinessFormOptionsAsync(vm, ct);
                return RenderBusinessEditor(vm, isCreate: true);
            }
        }

        [HttpGet]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)
        {
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
            var parsedRowVersion = string.IsNullOrWhiteSpace(rowVersion)
                ? Array.Empty<byte>()
                : Convert.FromBase64String(rowVersion);

            var result = await _setCancelAtPeriodEnd.HandleAsync(
                businessId,
                subscriptionId,
                cancelAtPeriodEnd,
                parsedRowVersion,
                ct);

            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? (cancelAtPeriodEnd ? "Subscription will cancel at period end." : "Subscription renewal restored.")
                : (result.Error ?? "Failed to update subscription cancellation policy.");

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
            var attentionMembers = items
                .Where(x => !x.EmailConfirmed || (x.LockoutEndUtc.HasValue && x.LockoutEndUtc.Value > DateTime.UtcNow))
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
                AttentionCount = items.Count(x => !x.EmailConfirmed || (x.LockoutEndUtc.HasValue && x.LockoutEndUtc.Value > DateTime.UtcNow)),
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
                    Note = x.Note
                })
                .ToList();

            return PartialView("~/Views/Businesses/_SetupInvitationsPreview.cshtml", new BusinessSetupInvitationsPreviewVm
            {
                BusinessId = businessId,
                OpenCount = items.Count(x => x.Status == BusinessInvitationStatus.Pending || x.Status == BusinessInvitationStatus.Expired),
                Items = openInvitations
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> Edit(BusinessEditVm vm, CancellationToken ct = default)
        {
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
                SetSuccessMessage("BusinessUpdated");
                return RedirectOrHtmx(nameof(Edit), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("BusinessConcurrencyConflict");
                return RedirectOrHtmx(nameof(Edit), new { id = vm.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateBusinessFormOptionsAsync(vm, ct);
                return RenderBusinessEditor(vm, isCreate: false);
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> Setup(BusinessEditVm vm, CancellationToken ct = default)
        {
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
                SetSuccessMessage("BusinessSetupSaved");
                return RedirectOrHtmx(nameof(Setup), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("BusinessConcurrencyConflict");
                return RedirectOrHtmx(nameof(Setup), new { id = vm.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateBusinessFormOptionsAsync(vm, ct);
                return RenderBusinessSetupEditor(vm);
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> Delete([FromForm] Guid id, [FromForm] byte[]? rowVersion, CancellationToken ct = default)
        {
            Result result = await _deleteBusiness.HandleAsync(new BusinessDeleteDto
            {
                Id = id,
                RowVersion = rowVersion ?? Array.Empty<byte>()
            }, ct);

            TempData[result.Succeeded ? "Success" : "Error"] =
                result.Succeeded ? "Business archived." : (result.Error ?? "Failed to archive business.");

            return RedirectOrHtmx(nameof(Index), new { });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> Approve([FromForm] Guid id, [FromForm] byte[]? rowVersion, CancellationToken ct = default)
        {
            try
            {
                await _approveBusiness.HandleAsync(new BusinessLifecycleActionDto
                {
                    Id = id,
                    RowVersion = rowVersion ?? Array.Empty<byte>()
                }, ct);

                SetSuccessMessage("BusinessApproved");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectOrHtmx(nameof(Edit), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> Suspend([FromForm] Guid id, [FromForm] byte[]? rowVersion, [FromForm] string? note, CancellationToken ct = default)
        {
            try
            {
                await _suspendBusiness.HandleAsync(new BusinessLifecycleActionDto
                {
                    Id = id,
                    RowVersion = rowVersion ?? Array.Empty<byte>(),
                    Note = note
                }, ct);

                SetSuccessMessage("BusinessSuspended");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectOrHtmx(nameof(Edit), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> Reactivate([FromForm] Guid id, [FromForm] byte[]? rowVersion, CancellationToken ct = default)
        {
            try
            {
                await _reactivateBusiness.HandleAsync(new BusinessLifecycleActionDto
                {
                    Id = id,
                    RowVersion = rowVersion ?? Array.Empty<byte>()
                }, ct);

                SetSuccessMessage("BusinessReactivated");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectOrHtmx(nameof(Edit), new { id });
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
                Playbooks = BuildBusinessLocationPlaybooks(),
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
        public async Task<IActionResult> CreateLocation(Guid businessId, CancellationToken ct = default)
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
                CountryCode = "DE",
                Business = business
            }, isCreate: true);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> CreateLocation(BusinessLocationEditVm vm, CancellationToken ct = default)
        {
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
                return RedirectOrHtmx(nameof(Locations), new { businessId = vm.BusinessId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateBusinessContextAsync(vm, ct);
                return RenderLocationEditor(vm, isCreate: true);
            }
        }

        [HttpGet]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> EditLocation(Guid id, CancellationToken ct = default)
        {
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
                return RedirectOrHtmx(nameof(Locations), new { businessId = vm.BusinessId });
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("BusinessLocationConcurrencyConflict");
                return RedirectOrHtmx(nameof(EditLocation), new { id = vm.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateBusinessContextAsync(vm, ct);
                return RenderLocationEditor(vm, isCreate: false);
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> DeleteLocation([FromForm] Guid id, [FromForm(Name = "userId")] Guid businessId, [FromForm] byte[]? rowVersion, CancellationToken ct = default)
        {
            var result = await _deleteBusinessLocation.HandleAsync(new BusinessLocationDeleteDto
            {
                Id = id,
                RowVersion = rowVersion ?? Array.Empty<byte>()
            }, ct);

            TempData[result.Succeeded ? "Success" : "Error"] =
                result.Succeeded ? "Business location archived." : (result.Error ?? "Failed to archive location.");

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
                    Note = x.Note
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
        public async Task<IActionResult> CreateInvitation(Guid businessId, CancellationToken ct = default)
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
                return RedirectOrHtmx(nameof(Invitations), new { businessId = vm.BusinessId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateBusinessContextAsync(vm, ct);
                PopulateInvitationFormOptions(vm);
                return RenderInvitationEditor(vm);
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.ManageBusinessSupport)]
        public async Task<IActionResult> ResendInvitation([FromForm] Guid id, [FromForm] Guid businessId, CancellationToken ct = default)
        {
            try
            {
                await _resendBusinessInvitation.HandleAsync(new BusinessInvitationResendDto
                {
                    Id = id,
                    ExpiresInDays = 7
                }, ct);
                SetSuccessMessage("BusinessInvitationReissued");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectOrHtmx(nameof(Invitations), new { businessId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.ManageBusinessSupport)]
        public async Task<IActionResult> RevokeInvitation([FromForm] Guid id, [FromForm] Guid businessId, CancellationToken ct = default)
        {
            try
            {
                await _revokeBusinessInvitation.HandleAsync(new BusinessInvitationRevokeDto
                {
                    Id = id,
                    Note = "Revoked from WebAdmin."
                }, ct);
                SetSuccessMessage("BusinessInvitationRevoked");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectOrHtmx(nameof(Invitations), new { businessId });
        }

        [HttpGet]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> CreateMember(Guid businessId, CancellationToken ct = default)
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

                SetSuccessMessage("BusinessMemberAssigned");
                return RedirectOrHtmx(nameof(Members), new { businessId = vm.BusinessId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateBusinessContextAsync(vm, ct);
                await PopulateMemberFormOptionsAsync(vm, includeUserSelection: true, ct);
                return RenderMemberEditor(vm, isCreate: true);
            }
        }

        [HttpGet]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> EditMember(Guid id, CancellationToken ct = default)
        {
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

            var issuedAtUtc = DateTime.UtcNow;
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

                SetSuccessMessage("BusinessMemberUpdated");
                return RedirectOrHtmx(nameof(Members), new { businessId = vm.BusinessId });
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("BusinessMemberConcurrencyConflict");
                return RedirectOrHtmx(nameof(EditMember), new { id = vm.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateBusinessContextAsync(vm, ct);
                await PopulateMemberFormOptionsAsync(vm, includeUserSelection: false, ct);
                return RenderMemberEditor(vm, isCreate: false);
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> DeleteMember([FromForm] Guid id, [FromForm(Name = "userId")] Guid businessId, [FromForm] byte[]? rowVersion, CancellationToken ct = default)
        {
            try
            {
                await _deleteBusinessMember.HandleAsync(new BusinessMemberDeleteDto
                {
                    Id = id,
                    RowVersion = rowVersion ?? Array.Empty<byte>()
                }, ct);

                SetSuccessMessage("BusinessMemberRemoved");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectOrHtmx(nameof(Members), new { businessId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.FullAdminAccess)]
        public async Task<IActionResult> ForceDeleteMember(
            [FromForm] Guid id,
            [FromForm] Guid businessId,
            [FromForm] byte[]? rowVersion,
            [FromForm] string? overrideReason,
            CancellationToken ct = default)
        {
            try
            {
                await _deleteBusinessMember.HandleAsync(new BusinessMemberDeleteDto
                {
                    Id = id,
                    RowVersion = rowVersion ?? Array.Empty<byte>(),
                    AllowLastOwnerOverride = true,
                    OverrideReason = overrideReason,
                    OverrideActorDisplayName = GetCurrentActorDisplayName()
                }, ct);

                SetSuccessMessage("BusinessMemberRemovedOverride");
                return RedirectOrHtmx(nameof(Members), new { businessId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return RedirectOrHtmx(nameof(EditMember), new { id });
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.ManageBusinessSupport)]
        public async Task<IActionResult> SendMemberActivationEmail(
            [FromForm] Guid id,
            [FromForm] Guid businessId,
            [FromForm] bool returnToEdit = false,
            CancellationToken ct = default)
        {
            var member = await _getBusinessMemberForEdit.HandleAsync(id, ct);
            if (member is null)
            {
                SetErrorMessage("BusinessMemberNotFound");
                return RedirectMemberSupport(returnToEdit, id, businessId);
            }

            var result = await _requestEmailConfirmation.HandleAsync(
                new RequestEmailConfirmationDto
                {
                    Email = member.UserEmail
                },
                ct);

            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? T("BusinessMemberActivationEmailSent")
                : (result.Error ?? T("BusinessMemberActivationEmailFailed"));

            return RedirectMemberSupport(returnToEdit, id, businessId);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.ManageBusinessSupport)]
        public async Task<IActionResult> ConfirmMemberEmail(
            [FromForm] Guid id,
            [FromForm] Guid businessId,
            [FromForm] bool returnToEdit = false,
            CancellationToken ct = default)
        {
            var member = await _getBusinessMemberForEdit.HandleAsync(id, ct);
            if (member is null)
            {
                SetErrorMessage("BusinessMemberNotFound");
                return RedirectMemberSupport(returnToEdit, id, businessId);
            }

            var result = await _confirmUserEmail.HandleAsync(new UserAdminActionDto
            {
                Id = member.UserId
            }, ct);

            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? T("BusinessMemberEmailConfirmed")
                : (result.Error ?? T("BusinessMemberEmailConfirmFailed"));

            return RedirectMemberSupport(returnToEdit, id, businessId);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.ManageBusinessSupport)]
        public async Task<IActionResult> SendMemberPasswordReset(
            [FromForm] Guid id,
            [FromForm] Guid businessId,
            [FromForm] bool returnToEdit = false,
            CancellationToken ct = default)
        {
            var member = await _getBusinessMemberForEdit.HandleAsync(id, ct);
            if (member is null)
            {
                SetErrorMessage("BusinessMemberNotFound");
                return RedirectMemberSupport(returnToEdit, id, businessId);
            }

            var result = await _requestPasswordReset.HandleAsync(
                new RequestPasswordResetDto
                {
                    Email = member.UserEmail
                },
                ct);

            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? T("BusinessMemberPasswordResetSent")
                : (result.Error ?? T("BusinessMemberPasswordResetFailed"));

            return RedirectMemberSupport(returnToEdit, id, businessId);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.ManageBusinessSupport)]
        public async Task<IActionResult> LockMemberUser(
            [FromForm] Guid id,
            [FromForm] Guid businessId,
            [FromForm] bool returnToEdit = false,
            CancellationToken ct = default)
        {
            var member = await _getBusinessMemberForEdit.HandleAsync(id, ct);
            if (member is null)
            {
                SetErrorMessage("BusinessMemberNotFound");
                return RedirectMemberSupport(returnToEdit, id, businessId);
            }

            var result = await _lockUser.HandleAsync(new UserAdminActionDto
            {
                Id = member.UserId
            }, ct);

            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? T("BusinessMemberAccountLocked")
                : (result.Error ?? T("BusinessMemberAccountLockFailed"));

            return RedirectMemberSupport(returnToEdit, id, businessId);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [PermissionAuthorize(PermissionKeys.ManageBusinessSupport)]
        public async Task<IActionResult> UnlockMemberUser(
            [FromForm] Guid id,
            [FromForm] Guid businessId,
            [FromForm] bool returnToEdit = false,
            CancellationToken ct = default)
        {
            var member = await _getBusinessMemberForEdit.HandleAsync(id, ct);
            if (member is null)
            {
                SetErrorMessage("BusinessMemberNotFound");
                return RedirectMemberSupport(returnToEdit, id, businessId);
            }

            var result = await _unlockUser.HandleAsync(new UserAdminActionDto
            {
                Id = member.UserId
            }, ct);

            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? T("BusinessMemberAccountUnlocked")
                : (result.Error ?? T("BusinessMemberAccountUnlockFailed"));

            return RedirectMemberSupport(returnToEdit, id, businessId);
        }

        private async Task PopulateBusinessFormOptionsAsync(BusinessEditVm vm, CancellationToken ct)
        {
            var settings = await _siteSettingCache.GetAsync(ct);
            vm.DefaultCurrency = string.IsNullOrWhiteSpace(vm.DefaultCurrency) ? settings.DefaultCurrency : vm.DefaultCurrency;
            vm.DefaultCulture = string.IsNullOrWhiteSpace(vm.DefaultCulture) ? settings.DefaultCulture : vm.DefaultCulture;
            vm.DefaultTimeZoneId = string.IsNullOrWhiteSpace(vm.DefaultTimeZoneId) ? (settings.TimeZone ?? string.Empty) : vm.DefaultTimeZoneId;

            vm.CategoryOptions = Enum.GetValues<BusinessCategoryKind>()
                .Select(x => new SelectListItem(x.ToString(), x.ToString(), vm.Category == x))
                .ToList();

            vm.OwnerUserOptions = await _referenceData.GetUserOptionsAsync(vm.OwnerUserId, includeEmpty: true, ct);
            vm.CommunicationReadiness = await BuildBusinessCommunicationReadinessAsync(ct);
            vm.Subscription = await BuildBusinessSubscriptionSnapshotAsync(vm.Id, ct);
        }

        private async Task PopulateMemberFormOptionsAsync(BusinessMemberEditVm vm, bool includeUserSelection, CancellationToken ct)
        {
            vm.RoleOptions = Enum.GetValues<BusinessMemberRole>()
                .Select(x => new SelectListItem(x.ToString(), x.ToString(), vm.Role == x))
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

        private async Task<BusinessCommunicationReadinessVm> BuildBusinessCommunicationReadinessAsync(CancellationToken ct)
        {
            var settings = await _siteSettingCache.GetAsync(ct);

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
                    ? "SMTP is enabled and has the minimum sender configuration required for business transactional email."
                    : "SMTP is not fully configured yet. Business transactional email defaults may be saved, but delivery is not operational.",
                SmsTransportSummary = smsConfigured
                    ? "SMS transport is enabled and has a provider configured."
                    : "SMS transport is still incomplete or disabled at platform level.",
                WhatsAppTransportSummary = whatsAppConfigured
                    ? "WhatsApp transport is enabled and has phone, token, and sender configuration."
                    : "WhatsApp transport is still incomplete or disabled at platform level.",
                AdminRoutingSummary = adminEmailRoutingConfigured || adminSmsRoutingConfigured
                    ? "Global admin alert routing exists for at least one channel."
                    : "Admin alert routing is not configured yet, so business operational alerts may lack an escalation target."
            };
        }

        private static void PopulateInvitationFormOptions(BusinessInvitationCreateVm vm)
        {
            vm.RoleOptions = Enum.GetValues<BusinessMemberRole>()
                .Select(x => new SelectListItem(x.ToString(), x.ToString(), vm.Role == x))
                .ToList();
        }

        private async Task<BusinessContextVm?> LoadBusinessContextAsync(Guid id, CancellationToken ct)
        {
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

        private IActionResult RedirectMemberSupport(bool returnToEdit, Guid membershipId, Guid businessId)
        {
            return returnToEdit
                ? RedirectOrHtmx(nameof(EditMember), new { id = membershipId })
                : RedirectOrHtmx(nameof(Members), new { businessId });
        }

        private static IEnumerable<SelectListItem> BuildBusinessStatusItems(BusinessOperationalStatus? selectedStatus)
        {
            yield return new SelectListItem("All statuses", string.Empty, !selectedStatus.HasValue);

            foreach (var status in Enum.GetValues<BusinessOperationalStatus>())
            {
                yield return new SelectListItem(status.ToString(), status.ToString(), selectedStatus == status);
            }
        }

        private static IEnumerable<SelectListItem> BuildBusinessMemberFilterItems(BusinessMemberSupportFilter selectedFilter)
        {
            yield return new SelectListItem("All members", BusinessMemberSupportFilter.All.ToString(), selectedFilter == BusinessMemberSupportFilter.All);
            yield return new SelectListItem("Needs attention", BusinessMemberSupportFilter.Attention.ToString(), selectedFilter == BusinessMemberSupportFilter.Attention);
            yield return new SelectListItem("Pending activation", BusinessMemberSupportFilter.PendingActivation.ToString(), selectedFilter == BusinessMemberSupportFilter.PendingActivation);
            yield return new SelectListItem("Locked", BusinessMemberSupportFilter.Locked.ToString(), selectedFilter == BusinessMemberSupportFilter.Locked);
        }

        private static IEnumerable<SelectListItem> BuildBusinessInvitationFilterItems(BusinessInvitationQueueFilter selectedFilter)
        {
            yield return new SelectListItem("All invitations", BusinessInvitationQueueFilter.All.ToString(), selectedFilter == BusinessInvitationQueueFilter.All);
            yield return new SelectListItem("Open invitations", BusinessInvitationQueueFilter.Open.ToString(), selectedFilter == BusinessInvitationQueueFilter.Open);
            yield return new SelectListItem("Pending", BusinessInvitationQueueFilter.Pending.ToString(), selectedFilter == BusinessInvitationQueueFilter.Pending);
            yield return new SelectListItem("Expired", BusinessInvitationQueueFilter.Expired.ToString(), selectedFilter == BusinessInvitationQueueFilter.Expired);
            yield return new SelectListItem("Accepted", BusinessInvitationQueueFilter.Accepted.ToString(), selectedFilter == BusinessInvitationQueueFilter.Accepted);
            yield return new SelectListItem("Revoked", BusinessInvitationQueueFilter.Revoked.ToString(), selectedFilter == BusinessInvitationQueueFilter.Revoked);
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

            var result = await _getBusinessSubscriptionStatus.HandleAsync(businessId, ct).ConfigureAwait(false);
            if (!result.Succeeded || result.Value is null)
            {
                return new BusinessSubscriptionSnapshotVm
                {
                    HasSubscription = false,
                    Status = "Unavailable"
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
            var plans = await _getBillingPlans.HandleAsync(activeOnly: true, ct);
            var recentInvoices = await _getBusinessSubscriptionInvoicesPage.HandleAsync(
                business.Id,
                page: 1,
                pageSize: 5,
                query: null,
                filter: BusinessSubscriptionInvoiceQueueFilter.All,
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
                    CheckoutReadinessLabel = validation.Succeeded ? "Checkout-ready" : (validation.Error ?? "Not ready"),
                    IsCurrentPlan = isCurrentPlan,
                    CanOpenManagementWebsite = canOpenManagementWebsite,
                    ManagementWebsiteUrl = canOpenManagementWebsite ? planManagementWebsiteUrl : null,
                    HandoffActionLabel = isCurrentPlan
                        ? "Manage Current Plan"
                        : subscription.HasSubscription ? "Upgrade To This Plan" : "Start With This Plan",
                    HandoffLabel = isCurrentPlan
                        ? "Current plan"
                        : canOpenManagementWebsite
                            ? "Open external billing website"
                            : managementWebsiteConfigured
                                ? "Resolve plan prerequisites first"
                                : "Configure management website first"
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
                Playbooks = BuildSubscriptionPlaybooks(subscription, managementWebsiteConfigured)
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
                OpenInvitationCount = summary.OpenInvitationCount,
                PendingActivationMemberCount = summary.PendingActivationMemberCount,
                LockedMemberCount = summary.LockedMemberCount
            };
        }

        private static string BuildSupportAuditRecommendedAction(EmailDispatchAuditListItemDto item)
        {
            if (string.Equals(item.FlowKey, "BusinessInvitation", StringComparison.OrdinalIgnoreCase))
            {
                return item.BusinessId.HasValue
                    ? "Open invitations for the business and use resend or revoke after checking transport readiness."
                    : "Review the source invitation workflow and transport readiness before resending.";
            }

            if (string.Equals(item.FlowKey, "AccountActivation", StringComparison.OrdinalIgnoreCase))
            {
                return item.BusinessId.HasValue
                    ? "Open members for the business and use activation support only after verifying the account state."
                    : "Use activation support only after verifying the account state.";
            }

            if (string.Equals(item.FlowKey, "PasswordReset", StringComparison.OrdinalIgnoreCase))
            {
                return "Reissue reset only after support validation and communication checks.";
            }

            return "Review the related workflow and communication readiness before manual intervention.";
        }

        private List<MerchantReadinessPlaybookVm> BuildMerchantReadinessPlaybooks()
        {
            return new List<MerchantReadinessPlaybookVm>
            {
                new()
                {
                    Title = T("MerchantReadinessPlaybookApprovalTitle"),
                    ScopeNote = T("MerchantReadinessPlaybookApprovalScope"),
                    OperatorAction = T("MerchantReadinessPlaybookApprovalAction")
                },
                new()
                {
                    Title = T("MerchantReadinessPlaybookSetupTitle"),
                    ScopeNote = T("MerchantReadinessPlaybookSetupScope"),
                    OperatorAction = T("MerchantReadinessPlaybookSetupAction")
                },
                new()
                {
                    Title = T("MerchantReadinessPlaybookBillingTitle"),
                    ScopeNote = T("MerchantReadinessPlaybookBillingScope"),
                    OperatorAction = T("MerchantReadinessPlaybookBillingAction")
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

        private static List<BusinessSubscriptionPlaybookVm> BuildSubscriptionPlaybooks(BusinessSubscriptionSnapshotVm subscription, bool managementWebsiteConfigured)
        {
            var items = new List<BusinessSubscriptionPlaybookVm>
            {
                new()
                {
                    QueueLabel = "Management Website",
                    WhyItMatters = "Business app subscription changes rely on the external management website handoff in phase 1.",
                    OperatorAction = managementWebsiteConfigured
                        ? "Use the configured management website for upgrade, checkout, and billing handoff."
                        : "Configure the business management website in site settings before handing operators off to external billing management."
                },
                new()
                {
                    QueueLabel = "Cancellation Policy",
                    WhyItMatters = "Cancel-at-period-end changes directly affect business continuity and renewal expectations.",
                    OperatorAction = subscription.HasSubscription
                        ? "Review renewal intent and toggle cancel-at-period-end only after confirming the business wants to stop renewal."
                        : "No active subscription exists yet, so cancellation control is not applicable."
                }
            };

            if (!subscription.HasSubscription)
            {
                items.Add(new BusinessSubscriptionPlaybookVm
                {
                    QueueLabel = "No Active Plan",
                    WhyItMatters = "Businesses without an active subscription may still expect access or billing support from admin.",
                    OperatorAction = "Review available plans and use the external billing-management handoff when the business is ready to start or upgrade."
                });
            }

            return items;
        }

        private static IEnumerable<SelectListItem> BuildBusinessSubscriptionInvoiceFilterItems(BusinessSubscriptionInvoiceQueueFilter selectedFilter)
        {
            yield return new SelectListItem("All invoices", BusinessSubscriptionInvoiceQueueFilter.All.ToString(), selectedFilter == BusinessSubscriptionInvoiceQueueFilter.All);
            yield return new SelectListItem("Open", BusinessSubscriptionInvoiceQueueFilter.Open.ToString(), selectedFilter == BusinessSubscriptionInvoiceQueueFilter.Open);
            yield return new SelectListItem("Paid", BusinessSubscriptionInvoiceQueueFilter.Paid.ToString(), selectedFilter == BusinessSubscriptionInvoiceQueueFilter.Paid);
            yield return new SelectListItem("Draft", BusinessSubscriptionInvoiceQueueFilter.Draft.ToString(), selectedFilter == BusinessSubscriptionInvoiceQueueFilter.Draft);
            yield return new SelectListItem("Uncollectible", BusinessSubscriptionInvoiceQueueFilter.Uncollectible.ToString(), selectedFilter == BusinessSubscriptionInvoiceQueueFilter.Uncollectible);
            yield return new SelectListItem("Hosted Link Missing", BusinessSubscriptionInvoiceQueueFilter.HostedLinkMissing.ToString(), selectedFilter == BusinessSubscriptionInvoiceQueueFilter.HostedLinkMissing);
            yield return new SelectListItem("Stripe", BusinessSubscriptionInvoiceQueueFilter.Stripe.ToString(), selectedFilter == BusinessSubscriptionInvoiceQueueFilter.Stripe);
            yield return new SelectListItem("Overdue", BusinessSubscriptionInvoiceQueueFilter.Overdue.ToString(), selectedFilter == BusinessSubscriptionInvoiceQueueFilter.Overdue);
            yield return new SelectListItem("PDF Missing", BusinessSubscriptionInvoiceQueueFilter.PdfMissing.ToString(), selectedFilter == BusinessSubscriptionInvoiceQueueFilter.PdfMissing);
        }

        private static IEnumerable<SelectListItem> BuildBusinessLocationFilterItems(BusinessLocationQueueFilter selectedFilter)
        {
            yield return new SelectListItem("All locations", BusinessLocationQueueFilter.All.ToString(), selectedFilter == BusinessLocationQueueFilter.All);
            yield return new SelectListItem("Primary", BusinessLocationQueueFilter.Primary.ToString(), selectedFilter == BusinessLocationQueueFilter.Primary);
            yield return new SelectListItem("Missing address", BusinessLocationQueueFilter.MissingAddress.ToString(), selectedFilter == BusinessLocationQueueFilter.MissingAddress);
            yield return new SelectListItem("Missing coordinates", BusinessLocationQueueFilter.MissingCoordinates.ToString(), selectedFilter == BusinessLocationQueueFilter.MissingCoordinates);
        }

        private static List<BusinessLocationPlaybookVm> BuildBusinessLocationPlaybooks()
        {
            return new List<BusinessLocationPlaybookVm>
            {
                new()
                {
                    QueueLabel = "Primary location",
                    WhyItMatters = "Business-facing operational flows usually assume one clear primary location for default fulfillment and storefront context.",
                    OperatorAction = "Review whether the current primary location still represents the live business entry point before onboarding or go-live approval."
                },
                new()
                {
                    QueueLabel = "Missing address",
                    WhyItMatters = "Incomplete address data weakens shipping, invoicing, and public business visibility across admin and mobile workflows.",
                    OperatorAction = "Open the location and complete street, city, and country before relying on the location for live operations."
                },
                new()
                {
                    QueueLabel = "Missing coordinates",
                    WhyItMatters = "Geo-dependent discovery, mapping, and future nearby-business experiences depend on a stored coordinate.",
                    OperatorAction = "Open the location and add coordinates when the business expects map-aware or proximity-aware experiences."
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
