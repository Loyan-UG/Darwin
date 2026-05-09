using Darwin.Domain.Enums;
using Darwin.Application.Settings.DTOs;

namespace Darwin.Application.CRM.DTOs
{
    public enum CustomerQueueFilter
    {
        All = 0,
        LinkedUser = 1,
        NeedsSegmentation = 2,
        HasOpportunities = 3,
        Business = 4,
        MissingVatId = 5,
        UsesPlatformLocaleFallback = 6
    }

    public enum LeadQueueFilter
    {
        All = 0,
        Qualified = 1,
        Unassigned = 2,
        Unconverted = 3
    }

    public sealed class CustomerListItemDto
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

    public sealed class CustomerAddressDto
    {
        public Guid? Id { get; set; }
        public Guid? AddressId { get; set; }
        public string Line1 { get; set; } = string.Empty;
        public string? Line2 { get; set; }
        public string City { get; set; } = string.Empty;
        public string? State { get; set; }
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = SiteSettingDto.DefaultCountryDefault;
        public bool IsDefaultShipping { get; set; }
        public bool IsDefaultBilling { get; set; }
    }

    public sealed class IdentityAddressSummaryDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Street1 { get; set; } = string.Empty;
        public string? Street2 { get; set; }
        public string PostalCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string? State { get; set; }
        public string CountryCode { get; set; } = SiteSettingDto.DefaultCountryDefault;
        public string? PhoneE164 { get; set; }
        public bool IsDefaultBilling { get; set; }
        public bool IsDefaultShipping { get; set; }
    }

    public class CustomerCreateDto
    {
        public Guid? UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? CompanyName { get; set; }
        public CustomerTaxProfileType TaxProfileType { get; set; } = CustomerTaxProfileType.Consumer;
        public string? VatId { get; set; }
        public string? Notes { get; set; }
        public List<CustomerAddressDto> Addresses { get; set; } = new();
    }

    public sealed class CustomerEditDto : CustomerCreateDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
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
        public IdentityAddressSummaryDto? DefaultBillingAddress { get; set; }
        public IdentityAddressSummaryDto? DefaultShippingAddress { get; set; }
    }

    public sealed class CustomerTaxProfileUpdateDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public CustomerTaxProfileType TaxProfileType { get; set; } = CustomerTaxProfileType.Business;
        public string? CompanyName { get; set; }
        public string? VatId { get; set; }
    }

    public sealed class CustomerVatValidationDecisionDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public CustomerVatValidationStatus Status { get; set; } = CustomerVatValidationStatus.Unknown;
        public string? Source { get; set; }
        public string? Message { get; set; }
    }

    public sealed class CustomerVatValidationLookupDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class LeadListItemDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public LeadStatus Status { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public string? AssignedToUserDisplayName { get; set; }
        public Guid? CustomerId { get; set; }
        public int InteractionCount { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public class LeadCreateDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Source { get; set; }
        public string? Notes { get; set; }
        public LeadStatus Status { get; set; } = LeadStatus.New;
        public Guid? AssignedToUserId { get; set; }
        public Guid? CustomerId { get; set; }
    }

    public sealed class LeadEditDto : LeadCreateDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public string? AssignedToUserDisplayName { get; set; }
        public string? CustomerDisplayName { get; set; }
        public int InteractionCount { get; set; }
    }

    public sealed class ConvertLeadToCustomerDto
    {
        public Guid LeadId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public Guid? UserId { get; set; }
        public bool CopyNotesToCustomer { get; set; } = true;
    }

    public sealed class UpdateLeadLifecycleDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public string Action { get; set; } = string.Empty;
    }

    public sealed class CrmSummaryDto
    {
        public int CustomerCount { get; set; }
        public int LeadCount { get; set; }
        public int QualifiedLeadCount { get; set; }
        public int OpenOpportunityCount { get; set; }
        public long OpenPipelineMinor { get; set; }
        public int SegmentCount { get; set; }
        public int RecentInteractionCount { get; set; }
    }

    /// <summary>
    /// Member-facing CRM customer context summary linked to the current identity.
    /// </summary>
    public sealed class MemberCustomerContextDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? CompanyName { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? LastInteractionAtUtc { get; set; }
        public int InteractionCount { get; set; }
        public IReadOnlyList<MemberCustomerSegmentDto> Segments { get; set; } = Array.Empty<MemberCustomerSegmentDto>();
        public IReadOnlyList<MemberCustomerConsentDto> Consents { get; set; } = Array.Empty<MemberCustomerConsentDto>();
        public IReadOnlyList<MemberCustomerInteractionDto> RecentInteractions { get; set; } = Array.Empty<MemberCustomerInteractionDto>();
    }

    /// <summary>
    /// Member-facing CRM segment summary.
    /// </summary>
    public sealed class MemberCustomerSegmentDto
    {
        public Guid SegmentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    /// <summary>
    /// Member-facing CRM consent history row.
    /// </summary>
    public sealed class MemberCustomerConsentDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public bool Granted { get; set; }
        public DateTime GrantedAtUtc { get; set; }
        public DateTime? RevokedAtUtc { get; set; }
    }

    /// <summary>
    /// Member-facing CRM interaction timeline row.
    /// </summary>
    public sealed class MemberCustomerInteractionDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public string? Subject { get; set; }
        public string? ContentPreview { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
