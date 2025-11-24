using System;

namespace Darwin.Application.Loyalty.DTOs
{
    /// <summary>
    /// DTO for creating a reward redemption (system/business app flow).
    /// </summary>
    public sealed class LoyaltyRewardRedemptionCreateDto
    {
        public Guid LoyaltyAccountId { get; set; }
        public Guid BusinessId { get; set; }
        public Guid ConsumerUserId { get; set; }

        public Guid RewardTierId { get; set; }
        public int PointsSpent { get; set; }

        public DateTime RedeemedAtUtc { get; set; }

        public Guid? ScanSessionId { get; set; }
        public string? Note { get; set; }
    }

    /// <summary>
    /// Lightweight list item for grids.
    /// </summary>
    public sealed class LoyaltyRewardRedemptionListItemDto
    {
        public Guid Id { get; set; }
        public Guid LoyaltyAccountId { get; set; }
        public Guid BusinessId { get; set; }
        public Guid ConsumerUserId { get; set; }

        public Guid RewardTierId { get; set; }
        public int PointsSpent { get; set; }

        public DateTime RedeemedAtUtc { get; set; }

        public Guid? ScanSessionId { get; set; }
        public string? Note { get; set; }
    }
}
