using System;

namespace Darwin.Contracts.Loyalty;

/// <summary>
/// Request payload to delete an existing reward tier from the current business loyalty program.
/// </summary>
public sealed class DeleteBusinessRewardTierRequest
{
    public Guid RewardTierId { get; init; }
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}
