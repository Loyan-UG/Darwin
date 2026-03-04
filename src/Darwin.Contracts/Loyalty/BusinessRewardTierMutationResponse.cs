using System;

namespace Darwin.Contracts.Loyalty;

/// <summary>
/// Generic mutation response for reward-tier create/update/delete operations.
/// </summary>
public sealed class BusinessRewardTierMutationResponse
{
    public Guid RewardTierId { get; init; }
    public bool Success { get; init; }
}
