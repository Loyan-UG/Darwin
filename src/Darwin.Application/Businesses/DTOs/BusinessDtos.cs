using System;
using System.Collections.Generic;
using Darwin.Domain.Enums;

namespace Darwin.Application.Businesses.DTOs
{
    /// <summary>
    /// DTO for creating a new business (merchant tenant).
    /// </summary>
    public sealed class BusinessCreateDto
    {
        public string Name { get; set; } = default!;
        public string? LegalName { get; set; }
        public string? TaxId { get; set; }
        public string? ShortDescription { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhoneE164 { get; set; }
        public BusinessCategoryKind Category { get; set; } = BusinessCategoryKind.Unknown;
        public string DefaultCurrency { get; set; } = "EUR";
        public string DefaultCulture { get; set; } = "de-DE";
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// DTO for editing an existing business.
    /// Includes concurrency token (RowVersion).
    /// </summary>
    public sealed class BusinessEditDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string? LegalName { get; set; }
        public string? TaxId { get; set; }
        public string? ShortDescription { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhoneE164 { get; set; }
        public BusinessCategoryKind Category { get; set; } = BusinessCategoryKind.Unknown;
        public string DefaultCurrency { get; set; } = "EUR";
        public string DefaultCulture { get; set; } = "de-DE";
        public bool IsActive { get; set; } = true;
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
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// DTO for soft deleting a business.
    /// </summary>
    public sealed class BusinessDeleteDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Lightweight list row for paged business grids.
    /// </summary>
    public sealed class BusinessListItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string? LegalName { get; set; }
        public BusinessCategoryKind Category { get; set; }
        public bool IsActive { get; set; }
        public BusinessOperationalStatus OperationalStatus { get; set; } = BusinessOperationalStatus.PendingApproval;
        public int MemberCount { get; set; }
        public int ActiveOwnerCount { get; set; }
        public int LocationCount { get; set; }
        public DateTime? CreatedAtUtc { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Minimal DTO used for approval, suspension, and reactivation actions.
    /// </summary>
    public sealed class BusinessLifecycleActionDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public string? Note { get; set; }
    }
    
}
