using Darwin.Domain.Enums;
using Darwin.Application.CRM.DTOs;
using Darwin.Application.Settings.DTOs;
using Darwin.WebAdmin.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Darwin.WebAdmin.ViewModels.CRM
{
    /// <summary>
    /// Paged list view model for CRM customers.
    /// </summary>
    public sealed class CustomersListVm
    {
        public CrmSummaryVm Summary { get; set; } = new();
        public CustomerOpsSummaryVm OpsSummary { get; set; } = new();
        public List<CrmPlaybookVm> Playbooks { get; set; } = new();
        public List<CustomerListItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
        public string Query { get; set; } = string.Empty;
        public CustomerQueueFilter Filter { get; set; } = CustomerQueueFilter.All;
        public string PlatformDefaultCulture { get; set; } = AdminCultureCatalog.DefaultCulture;
        public IEnumerable<SelectListItem> FilterItems { get; set; } = Array.Empty<SelectListItem>();
    }

    public sealed class CustomerOpsSummaryVm
    {
        public int LinkedUserCount { get; set; }
        public int LocaleFallbackCount { get; set; }
        public int BusinessCount { get; set; }
        public int MissingVatIdCount { get; set; }
        public int NeedsSegmentationCount { get; set; }
        public int HasOpportunitiesCount { get; set; }
    }

    public sealed class CustomerListItemVm
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? CompanyName { get; set; }
        public CustomerTaxProfileType TaxProfileType { get; set; }
        public string? VatId { get; set; }
        public string? Locale { get; set; }
        public bool UsesPlatformLocaleFallback { get; set; }
        public int SegmentCount { get; set; }
        public int OpportunityCount { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class CustomerAddressVm
    {
        public Guid? Id { get; set; }
        public Guid? AddressId { get; set; }

        [Required]
        [StringLength(200)]
        public string Line1 { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Line2 { get; set; }

        [Required]
        [StringLength(120)]
        public string City { get; set; } = string.Empty;

        [StringLength(120)]
        public string? State { get; set; }

        [Required]
        [StringLength(32)]
        public string PostalCode { get; set; } = string.Empty;

        [Required]
        [StringLength(8)]
        public string Country { get; set; } = SiteSettingDto.DefaultCountryDefault;

        public bool IsDefaultShipping { get; set; }
        public bool IsDefaultBilling { get; set; }
    }

    public sealed class IdentityAddressSummaryVm
    {
        public string FullName { get; set; } = string.Empty;
        public string Street1 { get; set; } = string.Empty;
        public string? Street2 { get; set; }
        public string PostalCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string? State { get; set; }
        public string CountryCode { get; set; } = SiteSettingDto.DefaultCountryDefault;
        public string? PhoneE164 { get; set; }
    }

    public sealed class CustomerEditVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        [Display(Name = "CustomerLinkedUser")]
        public Guid? UserId { get; set; }

        [Display(Name = "CustomerFirstName")]
        [StringLength(120)]
        public string FirstName { get; set; } = string.Empty;

        [Display(Name = "CustomerLastName")]
        [StringLength(120)]
        public string LastName { get; set; } = string.Empty;

        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Phone { get; set; }

        [Display(Name = "CustomerCompanyName")]
        [StringLength(200)]
        public string? CompanyName { get; set; }

        [Display(Name = "CustomerTaxProfileType")]
        public CustomerTaxProfileType TaxProfileType { get; set; } = CustomerTaxProfileType.Consumer;

        [Display(Name = "CustomerVatId")]
        [StringLength(64)]
        public string? VatId { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }

        public string EffectiveFirstName { get; set; } = string.Empty;
        public string EffectiveLastName { get; set; } = string.Empty;
        public string EffectiveEmail { get; set; } = string.Empty;
        public string? EffectivePhone { get; set; }
        public string? EffectiveLocale { get; set; }
        public bool UsesPlatformLocaleFallback { get; set; }
        public int SegmentCount { get; set; }
        public int OpportunityCount { get; set; }
        public int InteractionCount { get; set; }
        public int ConsentCount { get; set; }
        public IdentityAddressSummaryVm? DefaultBillingAddress { get; set; }
        public IdentityAddressSummaryVm? DefaultShippingAddress { get; set; }
        public List<CustomerAddressVm> Addresses { get; set; } = new();
        public List<SelectListItem> UserOptions { get; set; } = new();
        public InteractionCreateVm NewInteraction { get; set; } = new();
        public ConsentCreateVm NewConsent { get; set; } = new();
        public AssignCustomerSegmentVm SegmentAssignment { get; set; } = new();
        public List<SelectListItem> SegmentOptions { get; set; } = new();
    }

    public sealed class LeadsListVm
    {
        public CrmSummaryVm Summary { get; set; } = new();
        public LeadOpsSummaryVm OpsSummary { get; set; } = new();
        public List<CrmPlaybookVm> Playbooks { get; set; } = new();
        public List<LeadListItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
        public string Query { get; set; } = string.Empty;
        public LeadQueueFilter Filter { get; set; } = LeadQueueFilter.All;
        public IEnumerable<SelectListItem> FilterItems { get; set; } = Array.Empty<SelectListItem>();
    }

    public sealed class LeadOpsSummaryVm
    {
        public int QualifiedCount { get; set; }
        public int UnassignedCount { get; set; }
        public int UnconvertedCount { get; set; }
        public int LinkedCustomerCount { get; set; }
        public int HighInteractionCount { get; set; }
    }

    public sealed class LeadListItemVm
    {
        public Guid Id { get; set; }
        public Guid? CustomerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public LeadStatus Status { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public string? AssignedToUserDisplayName { get; set; }
        public int InteractionCount { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class LeadEditVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        [Required]
        [StringLength(120)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(120)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? CompanyName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(120)]
        public string? Source { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }

        public LeadStatus Status { get; set; } = LeadStatus.New;
        public Guid? AssignedToUserId { get; set; }
        public string? AssignedToUserDisplayName { get; set; }
        public Guid? CustomerId { get; set; }
        public string? CustomerDisplayName { get; set; }
        public int InteractionCount { get; set; }
        public ConvertLeadVm Conversion { get; set; } = new();
        public List<SelectListItem> UserOptions { get; set; } = new();
        public List<SelectListItem> CustomerOptions { get; set; } = new();
        public InteractionCreateVm NewInteraction { get; set; } = new();
    }

    public sealed class ConvertLeadVm
    {
        public Guid LeadId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public Guid? UserId { get; set; }
        public bool CopyNotesToCustomer { get; set; } = true;
        public List<SelectListItem> UserOptions { get; set; } = new();
    }

    public sealed class OpportunitiesListVm
    {
        public CrmSummaryVm Summary { get; set; } = new();
        public OpportunityOpsSummaryVm OpsSummary { get; set; } = new();
        public List<CrmPlaybookVm> Playbooks { get; set; } = new();
        public List<OpportunityListItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
        public string Query { get; set; } = string.Empty;
        public OpportunityQueueFilter Filter { get; set; } = OpportunityQueueFilter.All;
        public IEnumerable<SelectListItem> FilterItems { get; set; } = Array.Empty<SelectListItem>();
    }

    public sealed class OpportunityOpsSummaryVm
    {
        public int OpenCount { get; set; }
        public int ClosingSoonCount { get; set; }
        public int HighValueCount { get; set; }
        public int UnassignedCount { get; set; }
        public int HighInteractionCount { get; set; }
    }

    public sealed class OpportunityListItemVm
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public string CustomerDisplayName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public long EstimatedValueMinor { get; set; }
        public OpportunityStage Stage { get; set; }
        public DateTime? ExpectedCloseDateUtc { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public string? AssignedToUserDisplayName { get; set; }
        public int ItemCount { get; set; }
        public int InteractionCount { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class OpportunityItemVm
    {
        public Guid? Id { get; set; }

        [Required]
        public Guid ProductVariantId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        [Range(0, long.MaxValue)]
        public long UnitPriceMinor { get; set; }
    }

    public sealed class OpportunityEditVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        [Required]
        public Guid CustomerId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string Currency { get; set; } = string.Empty;

        [Range(0, long.MaxValue)]
        public long EstimatedValueMinor { get; set; }

        public OpportunityStage Stage { get; set; } = OpportunityStage.Qualification;
        public DateTime? ExpectedCloseDateUtc { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public string? AssignedToUserDisplayName { get; set; }
        public string CustomerDisplayName { get; set; } = string.Empty;
        public int InteractionCount { get; set; }
        public List<OpportunityItemVm> Items { get; set; } = new();
        public List<SelectListItem> CustomerOptions { get; set; } = new();
        public List<SelectListItem> UserOptions { get; set; } = new();
        public List<SelectListItem> VariantOptions { get; set; } = new();
        public InteractionCreateVm NewInteraction { get; set; } = new();
    }

    public sealed class InteractionCreateVm
    {
        public Guid? CustomerId { get; set; }
        public Guid? LeadId { get; set; }
        public Guid? OpportunityId { get; set; }
        public InteractionType Type { get; set; } = InteractionType.Email;
        public InteractionChannel Channel { get; set; } = InteractionChannel.Email;

        [StringLength(300)]
        public string? Subject { get; set; }

        [StringLength(4000)]
        public string? Content { get; set; }

        public Guid? UserId { get; set; }
        public List<SelectListItem> UserOptions { get; set; } = new();
    }

    public sealed class InteractionsPageVm
    {
        public string Scope { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public List<InteractionListItemVm> Items { get; set; } = new();
    }

    public sealed class InteractionListItemVm
    {
        public Guid Id { get; set; }
        public InteractionType Type { get; set; }
        public InteractionChannel Channel { get; set; }
        public string? Subject { get; set; }
        public string? Content { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    public sealed class ConsentCreateVm
    {
        public Guid CustomerId { get; set; }
        public ConsentType Type { get; set; } = ConsentType.MarketingEmail;
        public bool Granted { get; set; } = true;
        public DateTime GrantedAtUtc { get; set; }
        public DateTime? RevokedAtUtc { get; set; }
    }

    public sealed class ConsentsPageVm
    {
        public Guid CustomerId { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public List<ConsentListItemVm> Items { get; set; } = new();
    }

    public sealed class ConsentListItemVm
    {
        public Guid Id { get; set; }
        public ConsentType Type { get; set; }
        public bool Granted { get; set; }
        public DateTime GrantedAtUtc { get; set; }
        public DateTime? RevokedAtUtc { get; set; }
    }

    public sealed class CustomerSegmentsListVm
    {
        public CrmSummaryVm Summary { get; set; } = new();
        public CustomerSegmentOpsSummaryVm SegmentSummary { get; set; } = new();
        public List<CrmPlaybookVm> Playbooks { get; set; } = new();
        public List<CustomerSegmentListItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
        public string Query { get; set; } = string.Empty;
        public CustomerSegmentQueueFilter Filter { get; set; } = CustomerSegmentQueueFilter.All;
        public IEnumerable<SelectListItem> FilterItems { get; set; } = Array.Empty<SelectListItem>();
    }

    public sealed class CustomerSegmentOpsSummaryVm
    {
        public int TotalCount { get; set; }
        public int EmptyCount { get; set; }
        public int InUseCount { get; set; }
        public int MissingDescriptionCount { get; set; }
    }

    public sealed class CrmPlaybookVm
    {
        public string Title { get; set; } = string.Empty;
        public string ScopeNote { get; set; } = string.Empty;
        public string OperatorAction { get; set; } = string.Empty;
    }

    public sealed class CustomerSegmentListItemVm
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MemberCount { get; set; }
        public bool HasDescription { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class CustomerSegmentEditVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }
    }

    public sealed class CustomerMembershipsVm
    {
        public Guid CustomerId { get; set; }
        public List<CustomerSegmentMembershipVm> Items { get; set; } = new();
    }

    public sealed class CustomerSegmentMembershipVm
    {
        public Guid MembershipId { get; set; }
        public Guid SegmentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public sealed class AssignCustomerSegmentVm
    {
        public Guid CustomerId { get; set; }

        [Required]
        public Guid CustomerSegmentId { get; set; }
    }

    public sealed class InvoicesListVm
    {
        public CrmSummaryVm Summary { get; set; } = new();
        public InvoiceOpsSummaryVm OpsSummary { get; set; } = new();
        public List<CrmPlaybookVm> Playbooks { get; set; } = new();
        public TaxPolicySnapshotVm TaxPolicy { get; set; } = new();
        public List<InvoiceListItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
        public string Query { get; set; } = string.Empty;
        public Darwin.Application.CRM.DTOs.InvoiceQueueFilter Filter { get; set; }
    }

    public sealed class InvoiceOpsSummaryVm
    {
        public int DraftCount { get; set; }
        public int DueSoonCount { get; set; }
        public int OverdueCount { get; set; }
        public int MissingVatIdCount { get; set; }
        public int RefundedCount { get; set; }
    }

    public sealed class InvoiceListItemVm
    {
        public Guid Id { get; set; }
        public Guid? BusinessId { get; set; }
        public Guid? CustomerId { get; set; }
        public string CustomerDisplayName { get; set; } = string.Empty;
        public CustomerTaxProfileType? CustomerTaxProfileType { get; set; }
        public string? CustomerVatId { get; set; }
        public Guid? OrderId { get; set; }
        public string? OrderNumber { get; set; }
        public Guid? PaymentId { get; set; }
        public string PaymentSummary { get; set; } = string.Empty;
        public InvoiceStatus Status { get; set; }
        public string Currency { get; set; } = string.Empty;
        public long TotalNetMinor { get; set; }
        public long TotalTaxMinor { get; set; }
        public long TotalGrossMinor { get; set; }
        public long RefundedAmountMinor { get; set; }
        public long SettledAmountMinor { get; set; }
        public long BalanceMinor { get; set; }
        public DateTime DueDateUtc { get; set; }
        public DateTime? PaidAtUtc { get; set; }
        public DateTime? IssuedAtUtc { get; set; }
        public bool HasIssuedSnapshot { get; set; }
        public string? IssuedSnapshotHashSha256 { get; set; }
        public DateTime? ArchiveGeneratedAtUtc { get; set; }
        public DateTime? ArchiveRetainUntilUtc { get; set; }
        public string? ArchiveRetentionPolicyVersion { get; set; }
        public DateTime? ArchivePurgedAtUtc { get; set; }
        public string? ArchivePurgeReason { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class InvoiceEditVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public TaxPolicySnapshotVm TaxPolicy { get; set; } = new();
        public Guid? BusinessId { get; set; }
        public Guid? CustomerId { get; set; }
        public string CustomerDisplayName { get; set; } = string.Empty;
        public CustomerTaxProfileType? CustomerTaxProfileType { get; set; }
        public string? CustomerVatId { get; set; }
        public Guid? OrderId { get; set; }
        public string? OrderNumber { get; set; }
        public Guid? PaymentId { get; set; }
        public string PaymentSummary { get; set; } = string.Empty;
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string Currency { get; set; } = string.Empty;

        [Range(0, long.MaxValue)]
        public long TotalNetMinor { get; set; }

        [Range(0, long.MaxValue)]
        public long TotalTaxMinor { get; set; }

        [Range(0, long.MaxValue)]
        public long TotalGrossMinor { get; set; }

        public long RefundedAmountMinor { get; set; }
        public long SettledAmountMinor { get; set; }
        public long BalanceMinor { get; set; }
        public DateTime DueDateUtc { get; set; }
        public DateTime? PaidAtUtc { get; set; }
        public DateTime? IssuedAtUtc { get; set; }
        public bool HasIssuedSnapshot { get; set; }
        public string? IssuedSnapshotHashSha256 { get; set; }
        public DateTime? ArchiveGeneratedAtUtc { get; set; }
        public DateTime? ArchiveRetainUntilUtc { get; set; }
        public string? ArchiveRetentionPolicyVersion { get; set; }
        public DateTime? ArchivePurgedAtUtc { get; set; }
        public string? ArchivePurgeReason { get; set; }
        public bool IsFinancialContentLocked { get; set; }
        public string FinancialEditLockReason { get; set; } = string.Empty;
        public InvoiceRefundCreateVm Refund { get; set; } = new();
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<SelectListItem> CustomerOptions { get; set; } = new();
        public List<SelectListItem> PaymentOptions { get; set; } = new();
    }

    public sealed class TaxPolicySnapshotVm
    {
        public bool VatEnabled { get; set; }
        public decimal DefaultVatRatePercent { get; set; }
        public bool PricesIncludeVat { get; set; }
        public bool AllowReverseCharge { get; set; }
        public bool IssuerConfigured { get; set; }
        public string InvoiceIssuerLegalName { get; set; } = string.Empty;
        public string InvoiceIssuerCountry { get; set; } = string.Empty;
        public bool InvoiceIssuerTaxIdConfigured { get; set; }
        public bool ArchiveReadinessComplete { get; set; }
        public string ArchiveReadinessLabel { get; set; } = string.Empty;
        public bool EInvoiceBaselineReady { get; set; }
        public string EInvoiceBaselineLabel { get; set; } = string.Empty;
        public bool StructuredExportBaselineReady { get; set; }
        public string StructuredExportBaselineLabel { get; set; } = string.Empty;
        public string ComplianceScopeNote { get; set; } = string.Empty;
    }

    public sealed class InvoiceStatusTransitionVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public InvoiceStatus TargetStatus { get; set; }
        public DateTime? PaidAtUtc { get; set; }
    }

    public sealed class InvoiceRefundCreateVm
    {
        public Guid InvoiceId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        [Range(1, long.MaxValue)]
        public long AmountMinor { get; set; }

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string Currency { get; set; } = string.Empty;

        [Required]
        [StringLength(256)]
        public string Reason { get; set; } = string.Empty;
    }

    public sealed class CrmSummaryVm
    {
        public int CustomerCount { get; set; }
        public int LeadCount { get; set; }
        public int QualifiedLeadCount { get; set; }
        public int OpenOpportunityCount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public long OpenPipelineMinor { get; set; }
        public int SegmentCount { get; set; }
        public int RecentInteractionCount { get; set; }
    }
}

