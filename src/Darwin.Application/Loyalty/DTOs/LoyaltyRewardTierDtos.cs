using System;
using Darwin.Domain.Enums;

namespace Darwin.Application.Loyalty.DTOs
{
    /// <summary>
    /// DTO used to create a reward tier for a loyalty program.
    /// </summary>
    public sealed class LoyaltyRewardTierCreateDto
    {
        public Guid LoyaltyProgramId { get; set; }
        public int PointsRequired { get; set; }
        public LoyaltyRewardType RewardType { get; set; } = LoyaltyRewardType.FreeItem;
        public decimal? RewardValue { get; set; }
        public string? Description { get; set; }
        public bool AllowSelfRedemption { get; set; } = false;
        public string? MetadataJson { get; set; }
    }

    /// <summary>
    /// DTO used to edit an existing reward tier.
    /// </summary>
    public sealed class LoyaltyRewardTierEditDto
    {
        public Guid Id { get; set; }
        public Guid LoyaltyProgramId { get; set; }
        public int PointsRequired { get; set; }
        public LoyaltyRewardType RewardType { get; set; } = LoyaltyRewardType.FreeItem;
        public decimal? RewardValue { get; set; }
        public string? Description { get; set; }
        public bool AllowSelfRedemption { get; set; } = false;
        public string? MetadataJson { get; set; }
        public byte[]? RowVersion { get; set; }
    }

    /// <summary>
    /// Lightweight list item for grids.
    /// </summary>
    public sealed class LoyaltyRewardTierListItemDto
    {
        public Guid Id { get; set; }
        public Guid LoyaltyProgramId { get; set; }
        public int PointsRequired { get; set; }
        public LoyaltyRewardType RewardType { get; set; }
        public decimal? RewardValue { get; set; }
        public string? Description { get; set; }
        public bool AllowSelfRedemption { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// DTO used for soft delete in UI grids.
    /// </summary>
    public sealed class LoyaltyRewardTierDeleteDto
    {
        public Guid Id { get; set; }
        public byte[]? RowVersion { get; set; }
    }
}
