using Darwin.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Darwin.WebAdmin.ViewModels.CRM
{
    /// <summary>
    /// Paged list view model for CRM customers.
    /// </summary>
    public sealed class CustomersListVm
    {
        public List<CustomerListItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
        public string Query { get; set; } = string.Empty;
    }

    public sealed class CustomerListItemVm
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? CompanyName { get; set; }
        public int SegmentCount { get; set; }
        public int OpportunityCount { get; set; }
        public DateTime CreatedAtUtc { get; set; }
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
        public string Country { get; set; } = "DE";

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
        public string CountryCode { get; set; } = "DE";
        public string? PhoneE164 { get; set; }
    }

    public sealed class CustomerEditVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        [Display(Name = "Linked user")]
        public Guid? UserId { get; set; }

        [Display(Name = "First name")]
        [StringLength(120)]
        public string FirstName { get; set; } = string.Empty;

        [Display(Name = "Last name")]
        [StringLength(120)]
        public string LastName { get; set; } = string.Empty;

        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Phone { get; set; }

        [Display(Name = "Company")]
        [StringLength(200)]
        public string? CompanyName { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }

        public string EffectiveFirstName { get; set; } = string.Empty;
        public string EffectiveLastName { get; set; } = string.Empty;
        public string EffectiveEmail { get; set; } = string.Empty;
        public string? EffectivePhone { get; set; }
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
        public List<LeadListItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
        public string Query { get; set; } = string.Empty;
    }

    public sealed class LeadListItemVm
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public LeadStatus Status { get; set; }
        public int InteractionCount { get; set; }
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
        public Guid? CustomerId { get; set; }
        public List<SelectListItem> UserOptions { get; set; } = new();
        public List<SelectListItem> CustomerOptions { get; set; } = new();
        public InteractionCreateVm NewInteraction { get; set; } = new();
    }

    public sealed class OpportunitiesListVm
    {
        public List<OpportunityListItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
        public string Query { get; set; } = string.Empty;
    }

    public sealed class OpportunityListItemVm
    {
        public Guid Id { get; set; }
        public string CustomerDisplayName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public long EstimatedValueMinor { get; set; }
        public OpportunityStage Stage { get; set; }
        public DateTime? ExpectedCloseDateUtc { get; set; }
        public int ItemCount { get; set; }
        public int InteractionCount { get; set; }
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

        [Range(0, long.MaxValue)]
        public long EstimatedValueMinor { get; set; }

        public OpportunityStage Stage { get; set; } = OpportunityStage.Qualification;
        public DateTime? ExpectedCloseDateUtc { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public string CustomerDisplayName { get; set; } = string.Empty;
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
        public DateTime GrantedAtUtc { get; set; } = DateTime.UtcNow;
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
        public List<CustomerSegmentListItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
        public string Query { get; set; } = string.Empty;
    }

    public sealed class CustomerSegmentListItemVm
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MemberCount { get; set; }
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
}
