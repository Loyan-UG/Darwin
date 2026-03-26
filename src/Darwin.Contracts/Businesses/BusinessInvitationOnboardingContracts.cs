using System;

namespace Darwin.Contracts.Businesses;

/// <summary>
/// Public preview payload for a business invitation acceptance flow.
/// This is intentionally small so unauthenticated clients can validate a token
/// before collecting the rest of the onboarding form.
/// </summary>
public sealed class BusinessInvitationPreviewResponse
{
    /// <summary>
    /// Invitation identifier.
    /// </summary>
    public Guid InvitationId { get; init; }

    /// <summary>
    /// Target business identifier.
    /// </summary>
    public Guid BusinessId { get; init; }

    /// <summary>
    /// Display name of the business the user was invited to.
    /// </summary>
    public string BusinessName { get; init; } = string.Empty;

    /// <summary>
    /// Invited email address.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Intended business role expressed as a stable string value.
    /// </summary>
    public string Role { get; init; } = string.Empty;

    /// <summary>
    /// Effective invitation status as a stable string value.
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Expiration timestamp in UTC.
    /// </summary>
    public DateTime ExpiresAtUtc { get; init; }

    /// <summary>
    /// Indicates whether the invited email already belongs to an existing platform account.
    /// </summary>
    public bool HasExistingUser { get; init; }
}

/// <summary>
/// Request payload for accepting a business invitation.
/// When the invited email already maps to an existing account, profile/password fields may remain empty.
/// </summary>
public sealed class AcceptBusinessInvitationRequest
{
    /// <summary>
    /// Invitation token delivered to the invited email address.
    /// </summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>
    /// Device identifier used for device-bound refresh tokens.
    /// </summary>
    public string? DeviceId { get; init; }

    /// <summary>
    /// Optional first name for newly created accounts.
    /// </summary>
    public string? FirstName { get; init; }

    /// <summary>
    /// Optional last name for newly created accounts.
    /// </summary>
    public string? LastName { get; init; }

    /// <summary>
    /// Optional password for newly created accounts.
    /// Existing invited users do not need to provide it.
    /// </summary>
    public string? Password { get; init; }
}
