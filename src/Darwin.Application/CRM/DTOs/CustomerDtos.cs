using Darwin.Domain.Enums;

namespace Darwin.Application.CRM.DTOs
{
    public sealed class CustomerListItemDto
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
        public string Country { get; set; } = "DE";
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
        public string CountryCode { get; set; } = "DE";
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
        public IdentityAddressSummaryDto? DefaultBillingAddress { get; set; }
        public IdentityAddressSummaryDto? DefaultShippingAddress { get; set; }
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
    }
}
