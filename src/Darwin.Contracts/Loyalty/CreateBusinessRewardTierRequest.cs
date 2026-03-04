namespace Darwin.Contracts.Loyalty;

/// <summary>
/// Request payload to create a reward tier in the current business loyalty program.
/// </summary>
public sealed class CreateBusinessRewardTierRequest
{
    public int PointsRequired { get; init; }
    public string RewardType { get; init; } = string.Empty;
    public decimal? RewardValue { get; init; }
    public string? Description { get; init; }
    public bool AllowSelfRedemption { get; init; }
    public string? MetadataJson { get; init; }
}
