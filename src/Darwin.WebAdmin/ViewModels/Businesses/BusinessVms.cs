using System;
using System.Collections.Generic;
using Darwin.Domain.Enums;
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
        public List<BusinessListItemVm> Items { get; set; } = new();
        public IEnumerable<SelectListItem> PageSizeItems { get; set; } = Array.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> OperationalStatusItems { get; set; } = Array.Empty<SelectListItem>();
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
        public string DefaultCurrency { get; set; } = "EUR";
        public string DefaultCulture { get; set; } = "de-DE";
        public string DefaultTimeZoneId { get; set; } = "Europe/Berlin";
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
        public int MemberCount { get; set; }
        public int ActiveOwnerCount { get; set; }
        public int LocationCount { get; set; }
        public int PrimaryLocationCount { get; set; }
        public int InvitationCount { get; set; }
        public bool HasContactEmailConfigured { get; set; }
        public bool HasLegalNameConfigured { get; set; }
        public IEnumerable<SelectListItem> OwnerUserOptions { get; set; } = Array.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> CategoryOptions { get; set; } = Array.Empty<SelectListItem>();
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
        public List<BusinessLocationListItemVm> Items { get; set; } = new();
    }

    /// <summary>
    /// Form state for business-location create and edit flows.
    /// </summary>
    public sealed class BusinessLocationEditVm
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public string Name { get; set; } = string.Empty;
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? Region { get; set; }
        public string? CountryCode { get; set; } = "DE";
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
        public List<BusinessMemberListItemVm> Items { get; set; } = new();
    }

    /// <summary>
    /// Form state for business-member create and edit flows.
    /// </summary>
    public sealed class BusinessMemberEditVm
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public Guid UserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public string UserDisplayName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
        public DateTime? LockoutEndUtc { get; set; }
        public BusinessMemberRole Role { get; set; } = BusinessMemberRole.Staff;
        public bool IsActive { get; set; } = true;
        public BusinessContextVm Business { get; set; } = new();
        public IEnumerable<SelectListItem> UserOptions { get; set; } = Array.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> RoleOptions { get; set; } = Array.Empty<SelectListItem>();
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
        public List<BusinessInvitationListItemVm> Items { get; set; } = new();
    }

    /// <summary>
    /// Form state for business-invitation create flows.
    /// </summary>
    public sealed class BusinessInvitationCreateVm
    {
        public Guid BusinessId { get; set; }
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
        public List<BusinessInvitationListItemVm> Items { get; set; } = new();
    }
}
