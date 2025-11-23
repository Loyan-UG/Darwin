using System;

namespace Darwin.Application.Loyalty.DTOs
{
    /// <summary>
    /// Request for redeeming a reward tier during an active scan session.
    /// </summary>
    public sealed record RedeemRewardRequestDto
    {
        public required Guid ScanSessionId { get; init; }
        public required Guid RewardTierId { get; init; }
        public string? Note { get; init; }
    }

    /// <summary>
    /// Result of reward redemption.
    /// </summary>
    public sealed record RedeemRewardResponseDto
    {
        public required Guid LoyaltyAccountId { get; init; }
        public required int NewPointsBalance { get; init; }
        public required Guid RedemptionTransactionId { get; init; }
    }
}
