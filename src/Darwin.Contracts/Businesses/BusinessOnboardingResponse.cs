using System;

namespace Darwin.Contracts.Businesses;

/// <summary>
/// Result payload returned after successful business onboarding.
/// </summary>
public sealed class BusinessOnboardingResponse
{
    /// <summary>
    /// Newly created business identifier.
    /// </summary>
    public Guid BusinessId { get; init; }

    /// <summary>
    /// Membership identifier that links current user to the business as owner.
    /// </summary>
    public Guid BusinessMemberId { get; init; }
}
