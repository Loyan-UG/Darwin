using System;
using Darwin.Contracts.Businesses;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// UI projection model used by the Discover page explore list.
/// </summary>
/// <remarks>
/// The backend returns plain <see cref="BusinessSummary"/> items.
/// This wrapper adds client-side membership information so Explore can decide
/// whether to route the user toward joining flow or directly to rewards.
/// </remarks>
public sealed class DiscoverExploreItem
{
    /// <summary>
    /// Raw business payload returned by discovery endpoints.
    /// </summary>
    public required BusinessSummary Business { get; init; }

    /// <summary>
    /// True when the current consumer already has a loyalty account for this business.
    /// </summary>
    public bool IsJoined { get; init; }

    /// <summary>
    /// Convenience alias for bindings that need direct business id.
    /// </summary>
    public Guid BusinessId => Business.Id;
}