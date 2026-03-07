using System;

namespace Darwin.Contracts.Loyalty;

/// <summary>
/// Business-manageable reward tier configuration item.
/// </summary>
public sealed class BusinessRewardTierConfigItem
{
    public Guid RewardTierId { get; init; }
    public int PointsRequired { get; init; }
    public string RewardType { get; init; } = string.Empty;
    public decimal? RewardValue { get; init; }
    public string? Description { get; init; }
    public bool AllowSelfRedemption { get; init; }
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}
