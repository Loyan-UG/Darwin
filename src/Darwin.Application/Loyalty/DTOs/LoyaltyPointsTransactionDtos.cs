using System;

namespace Darwin.Application.Loyalty.DTOs
{
    /// <summary>
    /// DTO for creating points transactions (system flow).
    /// </summary>
    public sealed class LoyaltyPointsTransactionCreateDto
    {
        public Guid LoyaltyAccountId { get; set; }
        public Guid BusinessId { get; set; }

        public Guid? BusinessUserId { get; set; }
        public Guid ConsumerUserId { get; set; }

        public int PointsDelta { get; set; }
        public string? Note { get; set; }

        public DateTime OccurredAtUtc { get; set; }

        public Guid? ScanSessionId { get; set; }
        public Guid? RewardTierId { get; set; }
    }

    /// <summary>
    /// Lightweight list item for grids.
    /// </summary>
    public sealed class LoyaltyPointsTransactionListItemDto
    {
        public Guid Id { get; set; }
        public Guid LoyaltyAccountId { get; set; }
        public Guid BusinessId { get; set; }

        public Guid? BusinessUserId { get; set; }
        public Guid ConsumerUserId { get; set; }

        public int PointsDelta { get; set; }
        public string? Note { get; set; }
        public DateTime OccurredAtUtc { get; set; }

        public Guid? ScanSessionId { get; set; }
        public Guid? RewardTierId { get; set; }
    }
}
