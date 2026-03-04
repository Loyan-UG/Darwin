using System;
using System.Collections.Generic;

namespace Darwin.Contracts.Loyalty;

/// <summary>
/// Business-facing reward configuration payload for the current loyalty program.
/// </summary>
public sealed class BusinessRewardConfigurationResponse
{
    public Guid LoyaltyProgramId { get; init; }
    public string ProgramName { get; init; } = string.Empty;
    public bool IsProgramActive { get; init; }
    public IReadOnlyList<BusinessRewardTierConfigItem> RewardTiers { get; init; } = Array.Empty<BusinessRewardTierConfigItem>();
}
