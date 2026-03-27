using Darwin.Domain.Enums;

namespace Darwin.Application.Businesses.DTOs;

/// <summary>
/// Read model that describes the current operational access state for a business-facing client.
/// </summary>
public sealed class BusinessAccessStateDto
{
    /// <summary>
    /// Gets or sets the business identifier.
    /// </summary>
    public Guid BusinessId { get; set; }

    /// <summary>
    /// Gets or sets the business display name.
    /// </summary>
    public string BusinessName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current operational lifecycle status.
    /// </summary>
    public BusinessOperationalStatus OperationalStatus { get; set; } = BusinessOperationalStatus.PendingApproval;

    /// <summary>
    /// Gets or sets a value indicating whether the business is operationally active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the business was approved.
    /// </summary>
    public DateTime? ApprovedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the business was suspended.
    /// </summary>
    public DateTime? SuspendedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the optional suspension reason visible to support and operators.
    /// </summary>
    public string? SuspensionReason { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether at least one active owner is assigned.
    /// </summary>
    public bool HasActiveOwner { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a primary location exists.
    /// </summary>
    public bool HasPrimaryLocation { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the business has a contact email configured.
    /// </summary>
    public bool HasContactEmail { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the legal business name is configured.
    /// </summary>
    public bool HasLegalName { get; set; }

    /// <summary>
    /// Gets a value indicating whether live operational workflows are currently allowed.
    /// </summary>
    public bool IsOperationsAllowed => OperationalStatus == BusinessOperationalStatus.Approved && IsActive;

    /// <summary>
    /// Gets a value indicating whether the minimum onboarding checklist is complete.
    /// </summary>
    public bool IsSetupComplete => HasActiveOwner && HasPrimaryLocation && HasContactEmail && HasLegalName;

    /// <summary>
    /// Gets a human-readable reason that explains why operations are currently blocked.
    /// </summary>
    public string? BlockingReason => OperationalStatus switch
    {
        BusinessOperationalStatus.PendingApproval => "Business approval is still pending.",
        BusinessOperationalStatus.Suspended when !string.IsNullOrWhiteSpace(SuspensionReason) => SuspensionReason,
        BusinessOperationalStatus.Suspended => "Business access is currently suspended.",
        _ when !IsActive => "Business access is currently inactive.",
        _ => null
    };
}
