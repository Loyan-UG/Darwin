using System;
using System.Collections.Generic;
using Darwin.Application.Billing;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Enums;
using Darwin.WebAdmin.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Darwin.WebAdmin.ViewModels.Businesses
{
    /// <summary>
    /// Lightweight business row for the admin listing page.
    /// </summary>
    public sealed class BusinessListItemVm
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? LegalName { get; set; }
        public BusinessCategoryKind Category { get; set; }
        public bool IsActive { get; set; }
        public BusinessOperationalStatus OperationalStatus { get; set; } = BusinessOperationalStatus.PendingApproval;
        public int MemberCount { get; set; }
        public int ActiveOwnerCount { get; set; }
        public int LocationCount { get; set; }
        public int PrimaryLocationCount { get; set; }
        public int InvitationCount { get; set; }
        public bool HasContactEmailConfigured { get; set; }
        public bool HasLegalNameConfigured { get; set; }
        public DateTime? CreatedAtUtc { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Listing page state for businesses.
    /// </summary>
    public sealed class BusinessesListVm
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public string Query { get; set; } = string.Empty;
        public BusinessOperationalStatus? OperationalStatus { get; set; }
        public bool AttentionOnly { get; set; }
        public BusinessReadinessQueueFilter? ReadinessFilter { get; set; }
        public BusinessSupportSummaryVm Summary { get; set; } = new();
        public List<BusinessListItemVm> Items { get; set; } = new();
        public List<MerchantReadinessPlaybookVm> Playbooks { get; set; } = new();
        public IEnumerable<SelectListItem> PageSizeItems { get; set; } = Array.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> OperationalStatusItems { get; set; } = Array.Empty<SelectListItem>();
    }

    /// <summary>
    /// Operator-facing support queue that combines business attention and failed communication signals.
    /// </summary>
    public sealed class BusinessSupportQueueVm
    {
        public Guid? BusinessId { get; set; }
        public BusinessSupportSummaryVm Summary { get; set; } = new();
        public List<BusinessListItemVm> AttentionBusinesses { get; set; } = new();
        public List<BusinessSupportFailedEmailVm> FailedEmails { get; set; } = new();
        public List<MerchantReadinessPlaybookVm> Playbooks { get; set; } = new();
    }

    public sealed class MerchantReadinessWorkspaceVm
    {
        public Guid? BusinessId { get; set; }
        public BusinessSupportSummaryVm Summary { get; set; } = new();
        public List<MerchantReadinessItemVm> Items { get; set; } = new();
        public List<MerchantReadinessPlaybookVm> Playbooks { get; set; } = new();
    }

    public sealed class MerchantReadinessItemVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public string Name { get; set; } = string.Empty;
        public string? LegalName { get; set; }
        public bool IsActive { get; set; }
        public BusinessOperationalStatus OperationalStatus { get; set; } = BusinessOperationalStatus.PendingApproval;
        public bool HasContactEmailConfigured { get; set; }
        public bool HasLegalNameConfigured { get; set; }
        public int ActiveOwnerCount { get; set; }
        public int PrimaryLocationCount { get; set; }
        public int InvitationCount { get; set; }
        public bool HasSubscription { get; set; }
        public string SubscriptionStatus { get; set; } = string.Empty;
        public string SubscriptionPlanName { get; set; } = string.Empty;
        public bool CancelAtPeriodEnd { get; set; }
        public DateTime? CurrentPeriodEndUtc { get; set; }
    }

public sealed class MerchantReadinessPlaybookVm
{
    public string Title { get; set; } = string.Empty;
    public string ScopeNote { get; set; } = string.Empty;
    public string OperatorAction { get; set; } = string.Empty;
    public string QueueActionLabel { get; set; } = string.Empty;
    public string QueueActionUrl { get; set; } = string.Empty;
    public string FollowUpLabel { get; set; } = string.Empty;
    public string FollowUpUrl { get; set; } = string.Empty;
}

    public sealed class BusinessSupportSummaryVm
    {
        public int AttentionBusinessCount { get; set; }
        public int PendingApprovalBusinessCount { get; set; }
        public int SuspendedBusinessCount { get; set; }
        public int ApprovedInactiveBusinessCount { get; set; }
        public int MissingOwnerBusinessCount { get; set; }
        public int MissingPrimaryLocationBusinessCount { get; set; }
        public int MissingContactEmailBusinessCount { get; set; }
        public int MissingLegalNameBusinessCount { get; set; }
        public int PendingInvitationCount { get; set; }
        public int OpenInvitationCount { get; set; }
        public int PendingActivationMemberCount { get; set; }
        public int LockedMemberCount { get; set; }
        public int FailedInvitationCount { get; set; }
        public int FailedActivationCount { get; set; }
        public int FailedPasswordResetCount { get; set; }
        public int FailedAdminTestCount { get; set; }
    }

    public sealed class BusinessSupportFailedEmailVm
    {
        public Guid Id { get; set; }
        public string FlowKey { get; set; } = string.Empty;
        public Guid? BusinessId { get; set; }
        public string? BusinessName { get; set; }
        public string RecipientEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public DateTime AttemptedAtUtc { get; set; }
        public string? FailureMessage { get; set; }
        public string RecommendedAction { get; set; } = string.Empty;
    }

    /// <summary>
    /// Form state for business create and edit flows.
    /// </summary>
    public sealed class BusinessEditVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public string Name { get; set; } = string.Empty;
        public string? LegalName { get; set; }
        public string? TaxId { get; set; }
        public string? ShortDescription { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhoneE164 { get; set; }
        public BusinessCategoryKind Category { get; set; } = BusinessCategoryKind.Unknown;
        public string DefaultCurrency { get; set; } = string.Empty;
        public string DefaultCulture { get; set; } = AdminCultureCatalog.DefaultCulture;
        public string DefaultTimeZoneId { get; set; } = string.Empty;
        public string? AdminTextOverridesJson { get; set; }
        public string? BrandDisplayName { get; set; }
        public string? BrandLogoUrl { get; set; }
        public string? BrandPrimaryColorHex { get; set; }
        public string? BrandSecondaryColorHex { get; set; }
        public string? SupportEmail { get; set; }
        public string? CommunicationSenderName { get; set; }
        public string? CommunicationReplyToEmail { get; set; }
        public bool CustomerEmailNotificationsEnabled { get; set; } = true;
        public bool CustomerMarketingEmailsEnabled { get; set; }
        public bool OperationalAlertEmailsEnabled { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public BusinessOperationalStatus OperationalStatus { get; set; } = BusinessOperationalStatus.PendingApproval;
        public DateTime? ApprovedAtUtc { get; set; }
        public DateTime? SuspendedAtUtc { get; set; }
        public string? SuspensionReason { get; set; }
        public Guid? OwnerUserId { get; set; }
        public string? OwnerInviteEmail { get; set; }
        public int MemberCount { get; set; }
        public int ActiveOwnerCount { get; set; }
        public int LocationCount { get; set; }
        public int PrimaryLocationCount { get; set; }
        public int InvitationCount { get; set; }
        public bool HasContactEmailConfigured { get; set; }
        public bool HasLegalNameConfigured { get; set; }
        public Guid? SupportCustomerId { get; set; }
        public bool SupportCustomerProvisioned { get; set; }
        public bool CanProvisionSupportCustomer { get; set; }
        public string? SupportCustomerProvisioningIssue { get; set; }
        public string? SupportCustomerProvisioningEmail { get; set; }
        public string? SupportCustomerProvisioningCompanyName { get; set; }
        public BusinessCommunicationReadinessVm CommunicationReadiness { get; set; } = new();
        public BusinessSubscriptionSnapshotVm Subscription { get; set; } = new();
        public IEnumerable<SelectListItem> OwnerUserOptions { get; set; } = Array.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> CategoryOptions { get; set; } = Array.Empty<SelectListItem>();
    }

    public sealed class BusinessSubscriptionSnapshotVm
    {
        public bool HasSubscription { get; set; }
        public Guid SubscriptionId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public string Status { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string PlanCode { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public long UnitPriceMinor { get; set; }
        public DateTime? StartedAtUtc { get; set; }
        public DateTime? CurrentPeriodEndUtc { get; set; }
        public DateTime? TrialEndsAtUtc { get; set; }
        public DateTime? CanceledAtUtc { get; set; }
        public bool CancelAtPeriodEnd { get; set; }
    }

public sealed class BusinessSubscriptionWorkspaceVm
{
    public BusinessContextVm Business { get; set; } = new();
    public BusinessSubscriptionSnapshotVm Subscription { get; set; } = new();
    public bool ManagementWebsiteConfigured { get; set; }
    public string? ManagementWebsiteUrl { get; set; }
    public BusinessSubscriptionHandoffSummaryVm HandoffSummary { get; set; } = new();
    public List<BusinessBillingPlanVm> Plans { get; set; } = new();
    public BusinessSubscriptionInvoiceOpsSummaryVm InvoiceSummary { get; set; } = new();
    public List<BusinessSubscriptionInvoiceListItemVm> RecentInvoices { get; set; } = new();
    public List<BusinessSubscriptionPlaybookVm> Playbooks { get; set; } = new();
}

public sealed class BusinessSubscriptionHandoffSummaryVm
{
    public int TotalPlans { get; set; }
    public int ReadyPlanCount { get; set; }
    public int BlockedPlanCount { get; set; }
    public int CurrentPlanCount { get; set; }
}

    public sealed class BusinessBillingPlanVm
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public long PriceMinor { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Interval { get; set; } = string.Empty;
        public int IntervalCount { get; set; }
        public int? TrialDays { get; set; }
        public bool IsActive { get; set; }
        public bool CheckoutReady { get; set; }
        public string CheckoutReadinessLabel { get; set; } = string.Empty;
        public bool IsCurrentPlan { get; set; }
        public bool CanOpenManagementWebsite { get; set; }
        public string? ManagementWebsiteUrl { get; set; }
        public string HandoffActionLabel { get; set; } = string.Empty;
        public string HandoffLabel { get; set; } = string.Empty;
    }

public sealed class BusinessSubscriptionPlaybookVm
{
    public string QueueLabel { get; set; } = string.Empty;
    public string WhyItMatters { get; set; } = string.Empty;
    public string OperatorAction { get; set; } = string.Empty;
    public string QueueActionLabel { get; set; } = string.Empty;
    public string QueueActionUrl { get; set; } = string.Empty;
    public string FollowUpLabel { get; set; } = string.Empty;
    public string FollowUpUrl { get; set; } = string.Empty;
}

public sealed class BusinessSubscriptionInvoiceOpsSummaryVm
{
    public int TotalCount { get; set; }
    public int OpenCount { get; set; }
    public int PaidCount { get; set; }
    public int DraftCount { get; set; }
    public int UncollectibleCount { get; set; }
    public int HostedLinkMissingCount { get; set; }
    public int StripeCount { get; set; }
    public int OverdueCount { get; set; }
    public int PdfMissingCount { get; set; }
}

public sealed class BusinessSubscriptionInvoiceListItemVm
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid BusinessSubscriptionId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? ProviderInvoiceId { get; set; }
    public SubscriptionInvoiceStatus Status { get; set; }
    public long TotalMinor { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime IssuedAtUtc { get; set; }
    public DateTime? DueAtUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public string? HostedInvoiceUrl { get; set; }
    public string? PdfUrl { get; set; }
    public string? FailureReason { get; set; }
    public string? PlanName { get; set; }
    public string? PlanCode { get; set; }
    public bool HasHostedInvoiceUrl { get; set; }
    public bool HasPdfUrl { get; set; }
    public bool IsStripe { get; set; }
    public bool IsOverdue { get; set; }
}

public sealed class BusinessSubscriptionInvoicesListVm
{
    public BusinessContextVm Business { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public string Query { get; set; } = string.Empty;
    public BusinessSubscriptionInvoiceQueueFilter Filter { get; set; } = BusinessSubscriptionInvoiceQueueFilter.All;
    public IEnumerable<SelectListItem> FilterItems { get; set; } = Array.Empty<SelectListItem>();
    public BusinessSubscriptionInvoiceOpsSummaryVm Summary { get; set; } = new();
    public List<BusinessSubscriptionInvoiceListItemVm> Items { get; set; } = new();
}

    /// <summary>
    /// Summarizes whether business-scoped communication preferences can be executed
    /// given the currently configured global transport settings.
    /// </summary>
    public sealed class BusinessCommunicationReadinessVm
    {
        public bool EmailTransportEnabled { get; set; }
        public bool EmailTransportConfigured { get; set; }
        public bool SmsTransportEnabled { get; set; }
        public bool SmsTransportConfigured { get; set; }
        public bool WhatsAppTransportEnabled { get; set; }
        public bool WhatsAppTransportConfigured { get; set; }
        public bool AdminAlertEmailsConfigured { get; set; }
        public bool AdminAlertSmsConfigured { get; set; }
        public string EmailTransportSummary { get; set; } = string.Empty;
        public string SmsTransportSummary { get; set; } = string.Empty;
        public string WhatsAppTransportSummary { get; set; } = string.Empty;
        public string AdminRoutingSummary { get; set; } = string.Empty;
        public string EffectiveCulture { get; set; } = string.Empty;
        public string EffectiveCurrency { get; set; } = string.Empty;
        public string EffectiveTimeZoneId { get; set; } = string.Empty;
        public string EffectiveBrandDisplayName { get; set; } = string.Empty;
        public string? EffectiveSupportEmail { get; set; }
        public string? EffectiveSenderName { get; set; }
        public string? EffectiveReplyToEmail { get; set; }
        public bool UsesPlatformCultureFallback { get; set; }
        public bool UsesPlatformCurrencyFallback { get; set; }
        public bool UsesPlatformTimeZoneFallback { get; set; }
        public bool UsesPlatformContactEmailFallback { get; set; }
        public bool UsesPlatformSenderFallback { get; set; }
        public bool TenantCommunicationIdentityConfigured { get; set; }
        public int FailedInvitationCount { get; set; }
        public int FailedActivationCount { get; set; }
        public int FailedPasswordResetCount { get; set; }
        public int FailedAdminTestCount { get; set; }
    }

    /// <summary>
    /// Header context shared by business sub-pages such as members and locations.
    /// </summary>
    public sealed class BusinessContextVm
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? LegalName { get; set; }
        public BusinessCategoryKind Category { get; set; }
        public bool IsActive { get; set; }
        public BusinessOperationalStatus OperationalStatus { get; set; } = BusinessOperationalStatus.PendingApproval;
        public DateTime? ApprovedAtUtc { get; set; }
        public DateTime? SuspendedAtUtc { get; set; }
        public string? SuspensionReason { get; set; }
        public int MemberCount { get; set; }
        public int ActiveOwnerCount { get; set; }
        public int LocationCount { get; set; }
        public int PrimaryLocationCount { get; set; }
        public int InvitationCount { get; set; }
        public bool HasContactEmailConfigured { get; set; }
        public bool HasLegalNameConfigured { get; set; }
    }

    /// <summary>
    /// Lightweight business-location row for the admin listing page.
    /// </summary>
    public sealed class BusinessLocationListItemVm
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? City { get; set; }
        public string? Region { get; set; }
        public string? CountryCode { get; set; }
        public bool IsPrimary { get; set; }
        public bool HasAddress { get; set; }
        public bool HasCoordinates { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Listing page state for business locations.
    /// </summary>
    public sealed class BusinessLocationsListVm
    {
        public BusinessContextVm Business { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public string Query { get; set; } = string.Empty;
        public BusinessLocationQueueFilter Filter { get; set; } = BusinessLocationQueueFilter.All;
        public IEnumerable<SelectListItem> FilterItems { get; set; } = Array.Empty<SelectListItem>();
        public BusinessLocationOpsSummaryVm Summary { get; set; } = new();
        public List<BusinessLocationPlaybookVm> Playbooks { get; set; } = new();
        public List<BusinessLocationListItemVm> Items { get; set; } = new();
    }

    public sealed class BusinessLocationOpsSummaryVm
    {
        public int TotalCount { get; set; }
        public int PrimaryCount { get; set; }
        public int MissingAddressCount { get; set; }
        public int MissingCoordinatesCount { get; set; }
    }

    public sealed class BusinessLocationPlaybookVm
    {
        public string QueueLabel { get; set; } = string.Empty;
        public string WhyItMatters { get; set; } = string.Empty;
        public string OperatorAction { get; set; } = string.Empty;
        public string QueueActionUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// Form state for business-location create and edit flows.
    /// </summary>
    public sealed class BusinessLocationEditVm
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string Query { get; set; } = string.Empty;
        public BusinessLocationQueueFilter Filter { get; set; } = BusinessLocationQueueFilter.All;
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public string Name { get; set; } = string.Empty;
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? Region { get; set; }
        public string? CountryCode { get; set; } = Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCountryDefault;
        public string? PostalCode { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? AltitudeMeters { get; set; }
        public bool IsPrimary { get; set; }
        public string? OpeningHoursJson { get; set; }
        public string? InternalNote { get; set; }
        public BusinessContextVm Business { get; set; } = new();
    }

    /// <summary>
    /// Lightweight business-member row for the admin listing page.
    /// </summary>
    public sealed class BusinessMemberListItemVm
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public Guid UserId { get; set; }
        public string UserDisplayName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
        public DateTime? LockoutEndUtc { get; set; }
        public BusinessMemberRole Role { get; set; } = BusinessMemberRole.Staff;
        public bool IsActive { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Listing page state for business members.
    /// </summary>
    public sealed class BusinessMembersListVm
    {
        public BusinessContextVm Business { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public string Query { get; set; } = string.Empty;
        public BusinessMemberSupportFilter Filter { get; set; } = BusinessMemberSupportFilter.All;
        public IEnumerable<SelectListItem> FilterItems { get; set; } = Array.Empty<SelectListItem>();
        public BusinessMemberOpsSummaryVm Summary { get; set; } = new();
        public List<BusinessMemberPlaybookVm> Playbooks { get; set; } = new();
        public List<BusinessMemberListItemVm> Items { get; set; } = new();
    }

    public sealed class BusinessMemberOpsSummaryVm
    {
        public int TotalCount { get; set; }
        public int PendingActivationCount { get; set; }
        public int LockedCount { get; set; }
        public int AttentionCount { get; set; }
    }

    public sealed class BusinessMemberPlaybookVm
    {
        public string QueueLabel { get; set; } = string.Empty;
        public string WhyItMatters { get; set; } = string.Empty;
        public string OperatorAction { get; set; } = string.Empty;
        public string QueueActionLabel { get; set; } = string.Empty;
        public string QueueActionUrl { get; set; } = string.Empty;
        public string FollowUpLabel { get; set; } = string.Empty;
        public string FollowUpUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// Form state for business-member create and edit flows.
    /// </summary>
    public sealed class BusinessMemberEditVm
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public Guid UserId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string Query { get; set; } = string.Empty;
        public BusinessMemberSupportFilter Filter { get; set; } = BusinessMemberSupportFilter.All;
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public string UserDisplayName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
        public DateTime? LockoutEndUtc { get; set; }
        public BusinessMemberRole Role { get; set; } = BusinessMemberRole.Staff;
        public bool IsActive { get; set; } = true;
        public bool IsLastActiveOwner { get; set; }
        public bool AllowLastOwnerOverride { get; set; }
        public string? OverrideReason { get; set; }
        public BusinessContextVm Business { get; set; } = new();
        public IEnumerable<SelectListItem> UserOptions { get; set; } = Array.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> RoleOptions { get; set; } = Array.Empty<SelectListItem>();
    }

    /// <summary>
    /// Admin-side preview of the rotating staff access badge used in the business app.
    /// </summary>
    public sealed class BusinessStaffAccessBadgeVm
    {
        public BusinessContextVm Business { get; set; } = new();
        public Guid MembershipId { get; set; }
        public Guid UserId { get; set; }
        public string UserDisplayName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public BusinessMemberRole Role { get; set; } = BusinessMemberRole.Staff;
        public bool IsActive { get; set; }
        public bool EmailConfirmed { get; set; }
        public DateTime? LockoutEndUtc { get; set; }
        public DateTime IssuedAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public string BadgePayload { get; set; } = string.Empty;
        public string BadgeImageDataUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// Lightweight business-invitation row for the admin listing page.
    /// </summary>
    public sealed class BusinessInvitationListItemVm
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public string Email { get; set; } = string.Empty;
        public BusinessMemberRole Role { get; set; } = BusinessMemberRole.Staff;
        public BusinessInvitationStatus Status { get; set; } = BusinessInvitationStatus.Pending;
        public string InvitedByDisplayName { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime? AcceptedAtUtc { get; set; }
        public DateTime? RevokedAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string? Note { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Listing page state for business invitations.
    /// </summary>
    public sealed class BusinessInvitationsListVm
    {
        public BusinessContextVm Business { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public string Query { get; set; } = string.Empty;
        public BusinessInvitationQueueFilter Filter { get; set; } = BusinessInvitationQueueFilter.All;
        public IEnumerable<SelectListItem> FilterItems { get; set; } = Array.Empty<SelectListItem>();
        public BusinessInvitationOpsSummaryVm Summary { get; set; } = new();
        public List<BusinessInvitationPlaybookVm> Playbooks { get; set; } = new();
        public List<BusinessInvitationListItemVm> Items { get; set; } = new();
    }

    public sealed class BusinessInvitationOpsSummaryVm
    {
        public int TotalCount { get; set; }
        public int OpenCount { get; set; }
        public int PendingCount { get; set; }
        public int ExpiredCount { get; set; }
    }

    public sealed class BusinessInvitationPlaybookVm
    {
        public string QueueLabel { get; set; } = string.Empty;
        public string WhyItMatters { get; set; } = string.Empty;
        public string OperatorAction { get; set; } = string.Empty;
        public string QueueActionLabel { get; set; } = string.Empty;
        public string QueueActionUrl { get; set; } = string.Empty;
        public string FollowUpLabel { get; set; } = string.Empty;
        public string FollowUpUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// Form state for business-invitation create flows.
    /// </summary>
    public sealed class BusinessInvitationCreateVm
    {
        public Guid BusinessId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string Query { get; set; } = string.Empty;
        public BusinessInvitationQueueFilter Filter { get; set; } = BusinessInvitationQueueFilter.All;
        public string Email { get; set; } = string.Empty;
        public BusinessMemberRole Role { get; set; } = BusinessMemberRole.Staff;
        public int ExpiresInDays { get; set; } = 7;
        public string? Note { get; set; }
        public BusinessContextVm Business { get; set; } = new();
        public IEnumerable<SelectListItem> RoleOptions { get; set; } = Array.Empty<SelectListItem>();
    }

    /// <summary>
    /// Compact member preview for the business setup workspace.
    /// </summary>
    public sealed class BusinessSetupMembersPreviewVm
    {
        public Guid BusinessId { get; set; }
        public int AttentionCount { get; set; }
        public List<BusinessMemberListItemVm> Items { get; set; } = new();
    }

    /// <summary>
    /// Compact invitation preview for the business setup workspace.
    /// </summary>
    public sealed class BusinessSetupInvitationsPreviewVm
    {
        public Guid BusinessId { get; set; }
        public int OpenCount { get; set; }
        public int PendingCount { get; set; }
        public List<BusinessInvitationListItemVm> Items { get; set; } = new();
    }

    /// <summary>
    /// Lightweight owner-override audit row for admin review.
    /// </summary>
    public sealed class BusinessOwnerOverrideAuditListItemVm
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public Guid BusinessMemberId { get; set; }
        public Guid AffectedUserId { get; set; }
        public string AffectedUserDisplayName { get; set; } = string.Empty;
        public string AffectedUserEmail { get; set; } = string.Empty;
        public BusinessOwnerOverrideActionKind ActionKind { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? ActorDisplayName { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    /// <summary>
    /// Listing state for owner-override audit rows.
    /// </summary>
    public sealed class BusinessOwnerOverrideAuditsListVm
    {
        public BusinessContextVm Business { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public string Query { get; set; } = string.Empty;
        public List<BusinessOwnerOverrideAuditPlaybookVm> Playbooks { get; set; } = new();
        public List<BusinessOwnerOverrideAuditListItemVm> Items { get; set; } = new();
    }

    public sealed class BusinessOwnerOverrideAuditPlaybookVm
    {
        public string QueueLabel { get; set; } = string.Empty;
        public string WhyItMatters { get; set; } = string.Empty;
        public string OperatorAction { get; set; } = string.Empty;
        public string QueueActionLabel { get; set; } = string.Empty;
        public string QueueActionUrl { get; set; } = string.Empty;
        public string FollowUpLabel { get; set; } = string.Empty;
        public string FollowUpUrl { get; set; } = string.Empty;
    }

    public sealed class BusinessOnboardingWizardVm
    {
        public BusinessEditVm Business { get; set; } = new();
        public List<BusinessOnboardingWizardStepVm> Steps { get; set; } = new();
        public int RequiredStepCount { get; set; }
        public int CompletedRequiredStepCount { get; set; }
        public bool CanFinalize { get; set; }
    }

    public sealed class BusinessOnboardingWizardStepVm
    {
        public int Number { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public bool IsRequired { get; set; } = true;
        public bool IsComplete { get; set; }
        public string ActionLabel { get; set; } = string.Empty;
        public string ActionUrl { get; set; } = string.Empty;
    }
}
