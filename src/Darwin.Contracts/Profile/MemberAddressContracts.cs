namespace Darwin.Contracts.Profile;

/// <summary>
/// Member-facing reusable address-book entry.
/// </summary>
public sealed class MemberAddress
{
    /// <summary>Gets or sets the address identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the optimistic concurrency token.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    /// <summary>Gets or sets the recipient full name.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional company name.</summary>
    public string? Company { get; set; }

    /// <summary>Gets or sets the first street line.</summary>
    public string Street1 { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional second street line.</summary>
    public string? Street2 { get; set; }

    /// <summary>Gets or sets the postal code.</summary>
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the city.</summary>
    public string City { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional state or region.</summary>
    public string? State { get; set; }

    /// <summary>Gets or sets the ISO country code.</summary>
    public string CountryCode { get; set; } = "DE";

    /// <summary>Gets or sets the optional phone number in E.164 format.</summary>
    public string? PhoneE164 { get; set; }

    /// <summary>Gets or sets a value indicating whether this address is the default billing address.</summary>
    public bool IsDefaultBilling { get; set; }

    /// <summary>Gets or sets a value indicating whether this address is the default shipping address.</summary>
    public bool IsDefaultShipping { get; set; }
}

/// <summary>
/// Request payload for creating a member address.
/// </summary>
public sealed class CreateMemberAddressRequest
{
    /// <summary>Gets or sets the recipient full name.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional company name.</summary>
    public string? Company { get; set; }

    /// <summary>Gets or sets the first street line.</summary>
    public string Street1 { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional second street line.</summary>
    public string? Street2 { get; set; }

    /// <summary>Gets or sets the postal code.</summary>
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the city.</summary>
    public string City { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional state or region.</summary>
    public string? State { get; set; }

    /// <summary>Gets or sets the ISO country code.</summary>
    public string CountryCode { get; set; } = "DE";

    /// <summary>Gets or sets the optional phone number in E.164 format.</summary>
    public string? PhoneE164 { get; set; }

    /// <summary>Gets or sets a value indicating whether the new address should become the default billing address.</summary>
    public bool IsDefaultBilling { get; set; }

    /// <summary>Gets or sets a value indicating whether the new address should become the default shipping address.</summary>
    public bool IsDefaultShipping { get; set; }
}

/// <summary>
/// Request payload for updating a member address.
/// </summary>
public sealed class UpdateMemberAddressRequest
{
    /// <summary>Gets or sets the recipient full name.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional company name.</summary>
    public string? Company { get; set; }

    /// <summary>Gets or sets the first street line.</summary>
    public string Street1 { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional second street line.</summary>
    public string? Street2 { get; set; }

    /// <summary>Gets or sets the postal code.</summary>
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the city.</summary>
    public string City { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional state or region.</summary>
    public string? State { get; set; }

    /// <summary>Gets or sets the ISO country code.</summary>
    public string CountryCode { get; set; } = "DE";

    /// <summary>Gets or sets the optional phone number in E.164 format.</summary>
    public string? PhoneE164 { get; set; }

    /// <summary>Gets or sets a value indicating whether the address should become the default billing address.</summary>
    public bool IsDefaultBilling { get; set; }

    /// <summary>Gets or sets a value indicating whether the address should become the default shipping address.</summary>
    public bool IsDefaultShipping { get; set; }

    /// <summary>Gets or sets the optimistic concurrency token.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

/// <summary>
/// Request payload for deleting a member address.
/// </summary>
public sealed class DeleteMemberAddressRequest
{
    /// <summary>Gets or sets the optimistic concurrency token.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

/// <summary>
/// Request payload for setting the default address roles.
/// </summary>
public sealed class SetMemberDefaultAddressRequest
{
    /// <summary>Gets or sets a value indicating whether the address should become default billing.</summary>
    public bool AsBilling { get; set; }

    /// <summary>Gets or sets a value indicating whether the address should become default shipping.</summary>
    public bool AsShipping { get; set; }
}

/// <summary>
/// Member-facing summary of the CRM customer linked to the current identity account.
/// </summary>
public sealed class LinkedCustomerProfile
{
    /// <summary>Gets or sets the CRM customer identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the linked identity user identifier.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the effective display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the effective email address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the effective phone number.</summary>
    public string? Phone { get; set; }

    /// <summary>Gets or sets the optional CRM company name.</summary>
    public string? CompanyName { get; set; }

    /// <summary>Gets or sets the customer creation timestamp in UTC.</summary>
    public DateTime CreatedAtUtc { get; set; }
}
