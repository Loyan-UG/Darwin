namespace Darwin.Contracts.Businesses;

/// <summary>
/// Business-facing access-state payload used to gate live operational workflows in client applications.
/// </summary>
public sealed class BusinessAccessStateResponse
{
    /// <summary>
    /// Gets or sets the authenticated user identifier.
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
    /// Gets or sets the lifecycle status label.
    /// </summary>
    public string OperationalStatus { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the business is currently active.
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
    /// Gets or sets the optional suspension reason.
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
    /// Gets or sets a value indicating whether a contact email is configured.
    /// </summary>
    public bool HasContactEmail { get; set; }

    /// <summary>
     /// Gets or sets a value indicating whether a legal business name is configured.
     /// </summary>
    public bool HasLegalName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the current user still has an active membership in this business.
    /// </summary>
    public bool HasActiveMembership { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the current user account remains active.
    /// </summary>
    public bool IsUserActive { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the current user email is confirmed.
    /// </summary>
    public bool IsUserEmailConfirmed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the current user is locked out.
    /// </summary>
    public bool IsUserLockedOut { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether approval is still pending.
    /// </summary>
    public bool IsApprovalPending { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the business is suspended.
    /// </summary>
    public bool IsSuspended { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether onboarding-safe business access is still allowed for this user.
    /// </summary>
    public bool IsBusinessClientAccessAllowed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether live operational workflows are allowed.
    /// </summary>
    public bool IsOperationsAllowed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the minimum onboarding checklist is complete.
    /// </summary>
    public bool IsSetupComplete { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the current user still has activation/account-state blockers.
    /// </summary>
    public bool HasActivationBlockingIssues { get; set; }

    /// <summary>
    /// Gets or sets the number of missing setup checklist items.
    /// </summary>
    public int SetupIncompleteItemCount { get; set; }

    /// <summary>
    /// Gets or sets a stable machine-readable blocking token.
    /// </summary>
    public string? PrimaryBlockingCode { get; set; }

    /// <summary>
    /// Gets or sets an optional operator-facing blocking reason.
    /// </summary>
    public string? BlockingReason { get; set; }
}
