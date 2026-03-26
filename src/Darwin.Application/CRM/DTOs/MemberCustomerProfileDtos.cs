namespace Darwin.Application.CRM.DTOs;

/// <summary>
/// Member-facing CRM customer summary linked to the current authenticated identity.
/// </summary>
public sealed class MemberCustomerProfileDto
{
    /// <summary>Gets or sets the CRM customer identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the linked identity user identifier.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the effective display name resolved from the linked identity.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the effective email address resolved from the linked identity.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the effective phone number resolved from the linked identity.</summary>
    public string? Phone { get; set; }

    /// <summary>Gets or sets the optional company name carried by CRM.</summary>
    public string? CompanyName { get; set; }

    /// <summary>Gets or sets the customer creation timestamp in UTC.</summary>
    public DateTime CreatedAtUtc { get; set; }
}
