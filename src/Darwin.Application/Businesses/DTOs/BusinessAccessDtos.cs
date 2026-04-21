using Darwin.Domain.Enums;

namespace Darwin.Application.Businesses.DTOs;

/// <summary>
/// Read model that describes the current operational access state for a business-facing client.
/// </summary>
public sealed class BusinessAccessStateDto
{
    /// <summary>
    /// Gets or sets the authenticated user identifier associated with the business session.
    /// </summary>
    public Guid UserId { get; set; }

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
    /// Gets or sets a value indicating whether the authenticated user still has an active membership in this business.
    /// </summary>
    public bool HasActiveMembership { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the authenticated user account remains active.
    /// </summary>
    public bool IsUserActive { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the authenticated user email is confirmed.
    /// </summary>
    public bool IsUserEmailConfirmed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the authenticated user is currently locked.
    /// </summary>
    public bool IsUserLockedOut { get; set; }

    /// <summary>
    /// Gets a value indicating whether the current business is still pending approval.
    /// </summary>
    public bool IsApprovalPending => OperationalStatus == BusinessOperationalStatus.PendingApproval;

    /// <summary>
    /// Gets a value indicating whether the current business is suspended.
    /// </summary>
    public bool IsSuspended => OperationalStatus == BusinessOperationalStatus.Suspended;

    /// <summary>
    /// Gets a value indicating whether the current business client session is still valid for onboarding-safe access.
    /// </summary>
    public bool IsBusinessClientAccessAllowed =>
        HasActiveMembership &&
        IsUserActive &&
        IsUserEmailConfirmed &&
        !IsUserLockedOut;

    /// <summary>
    /// Gets a value indicating whether live operational workflows are currently allowed.
    /// </summary>
    public bool IsOperationsAllowed =>
        IsBusinessClientAccessAllowed &&
        OperationalStatus == BusinessOperationalStatus.Approved &&
        IsActive;

    /// <summary>
    /// Gets a value indicating whether the minimum onboarding checklist is complete.
    /// </summary>
    public bool IsSetupComplete => HasActiveOwner && HasPrimaryLocation && HasContactEmail && HasLegalName;

    /// <summary>
    /// Gets a value indicating whether the current user must resolve an activation or account-state issue.
    /// </summary>
    public bool HasActivationBlockingIssues => !IsBusinessClientAccessAllowed;

    /// <summary>
    /// Gets the number of missing setup checklist items.
    /// </summary>
    public int SetupIncompleteItemCount =>
        (HasActiveOwner ? 0 : 1) +
        (HasPrimaryLocation ? 0 : 1) +
        (HasContactEmail ? 0 : 1) +
        (HasLegalName ? 0 : 1);

    /// <summary>
    /// Gets a stable machine-readable token that explains the primary blocking state.
    /// </summary>
    public string? PrimaryBlockingCode => OperationalStatus switch
    {
        _ when !HasActiveMembership => "membership_inactive",
        _ when !IsUserActive => "user_inactive",
        _ when !IsUserEmailConfirmed => "email_confirmation_required",
        _ when IsUserLockedOut => "user_locked",
        BusinessOperationalStatus.PendingApproval => "business_pending_approval",
        BusinessOperationalStatus.Suspended => "business_suspended",
        _ when !IsActive => "business_inactive",
        _ when !IsSetupComplete => "setup_incomplete",
        _ => null
    };

    /// <summary>
    /// Gets an optional stable blocking token or support-authored suspension reason.
    /// </summary>
    public string? BlockingReason => OperationalStatus switch
    {
        _ when !HasActiveMembership => "membership_inactive",
        _ when !IsUserActive => "user_inactive",
        _ when !IsUserEmailConfirmed => "email_confirmation_required",
        _ when IsUserLockedOut => "user_locked",
        BusinessOperationalStatus.PendingApproval => "business_pending_approval",
        BusinessOperationalStatus.Suspended when !string.IsNullOrWhiteSpace(SuspensionReason) => SuspensionReason,
        BusinessOperationalStatus.Suspended => "business_suspended",
        _ when !IsActive => "business_inactive",
        _ when !IsSetupComplete => "setup_incomplete",
        _ => null
    };
}
