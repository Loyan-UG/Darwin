using System;
using Darwin.Domain.Enums;

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
        public string? Note { get; set; }
    }

    /// <summary>
    /// Admin-friendly redemption row with enough context for troubleshooting and confirmation workflows.
    /// </summary>
    public sealed class LoyaltyRewardRedemptionListItemDto
    {
        public Guid Id { get; set; }
        public Guid LoyaltyAccountId { get; set; }
        public Guid BusinessId { get; set; }
        public Guid ConsumerUserId { get; set; }
        public Guid RewardTierId { get; set; }
        public string RewardLabel { get; set; } = string.Empty;
        public int PointsSpent { get; set; }
        public LoyaltyRedemptionStatus Status { get; set; }
        public DateTime RedeemedAtUtc { get; set; }
        public string? Note { get; set; }
        public string ConsumerDisplayName { get; set; } = string.Empty;
        public string ConsumerEmail { get; set; } = string.Empty;
        public LoyaltyScanStatus? ScanStatus { get; set; }
        public string? ScanOutcome { get; set; }
        public string? ScanFailureReason { get; set; }
        public Guid? BusinessLocationId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// DTO used to confirm a previously created, pending loyalty reward redemption.
    /// </summary>
    public sealed class ConfirmLoyaltyRewardRedemptionDto
    {
        public Guid RedemptionId { get; set; }
        public Guid BusinessId { get; set; }
        public Guid? BusinessLocationId { get; set; }
        public Guid? PerformedByUserId { get; set; }
        public byte[]? RowVersion { get; set; }
    }

    /// <summary>
    /// Result DTO returned after a successful confirmation of a pending loyalty reward redemption.
    /// </summary>
    public sealed class ConfirmLoyaltyRewardRedemptionResultDto
    {
        public Guid RedemptionId { get; set; }
        public Guid LoyaltyAccountId { get; set; }
        public Guid BusinessId { get; set; }
        public Guid? BusinessLocationId { get; set; }
        public Guid TransactionId { get; set; }
        public int NewPointsBalance { get; set; }
        public int NewLifetimePoints { get; set; }
    }
}
