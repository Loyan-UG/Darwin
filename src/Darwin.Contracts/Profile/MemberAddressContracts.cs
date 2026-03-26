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

/// <summary>
/// Member-facing CRM customer context summary linked to the current identity account.
/// </summary>
public sealed class MemberCustomerContext
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

    /// <summary>Gets or sets operator-visible notes that may also be shown in self-service customer context.</summary>
    public string? Notes { get; set; }

    /// <summary>Gets or sets the customer creation timestamp in UTC.</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Gets or sets the UTC timestamp of the latest recorded interaction.</summary>
    public DateTime? LastInteractionAtUtc { get; set; }

    /// <summary>Gets or sets the total interaction count.</summary>
    public int InteractionCount { get; set; }

    /// <summary>Gets or sets the CRM segments currently associated with the member.</summary>
    public IReadOnlyList<MemberCustomerSegment> Segments { get; set; } = Array.Empty<MemberCustomerSegment>();

    /// <summary>Gets or sets recent consent history rows.</summary>
    public IReadOnlyList<MemberCustomerConsent> Consents { get; set; } = Array.Empty<MemberCustomerConsent>();

    /// <summary>Gets or sets recent CRM interaction rows.</summary>
    public IReadOnlyList<MemberCustomerInteraction> RecentInteractions { get; set; } = Array.Empty<MemberCustomerInteraction>();
}

/// <summary>
/// Member-facing CRM segment row.
/// </summary>
public sealed class MemberCustomerSegment
{
    /// <summary>Gets or sets the CRM segment identifier.</summary>
    public Guid SegmentId { get; set; }

    /// <summary>Gets or sets the segment name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional segment description.</summary>
    public string? Description { get; set; }
}

/// <summary>
/// Member-facing CRM consent history row.
/// </summary>
public sealed class MemberCustomerConsent
{
    /// <summary>Gets or sets the consent identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the consent type label.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether consent is currently granted.</summary>
    public bool Granted { get; set; }

    /// <summary>Gets or sets the UTC timestamp when consent was granted.</summary>
    public DateTime GrantedAtUtc { get; set; }

    /// <summary>Gets or sets the UTC timestamp when consent was revoked, if applicable.</summary>
    public DateTime? RevokedAtUtc { get; set; }
}

/// <summary>
/// Member-facing CRM interaction row.
/// </summary>
public sealed class MemberCustomerInteraction
{
    /// <summary>Gets or sets the interaction identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the interaction type label.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets the interaction channel label.</summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional interaction subject.</summary>
    public string? Subject { get; set; }

    /// <summary>Gets or sets a truncated content preview.</summary>
    public string? ContentPreview { get; set; }

    /// <summary>Gets or sets the interaction creation timestamp in UTC.</summary>
    public DateTime CreatedAtUtc { get; set; }
}
