using System;

namespace Darwin.Application.Loyalty.DTOs
{
    /// <summary>
    /// Request for adding points to a consumer during an active scan session.
    /// </summary>
    public sealed record AccruePointsRequestDto
    {
        public required Guid ScanSessionId { get; init; }
        public required int PointsToAdd { get; init; }
        public string? Note { get; init; }
    }

    /// <summary>
    /// Result of a points accrual operation.
    /// </summary>
    public sealed record AccruePointsResponseDto
    {
        public required Guid LoyaltyAccountId { get; init; }
        public required int NewPointsBalance { get; init; }
        public required Guid TransactionId { get; init; }
    }
}
