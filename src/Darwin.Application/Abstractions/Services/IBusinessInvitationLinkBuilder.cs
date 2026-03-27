using System;

namespace Darwin.Application.Abstractions.Services;

/// <summary>
/// Builds optional acceptance links for business-invitation onboarding emails.
/// Implementations may return null when no magic-link base URL is configured.
/// </summary>
public interface IBusinessInvitationLinkBuilder
{
    /// <summary>
    /// Builds an invitation-acceptance link that already contains the invitation token.
    /// Returns null when the current host does not expose a configured magic-link base URL.
    /// </summary>
    string? BuildAcceptanceLink(string token);
}
