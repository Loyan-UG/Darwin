using System;

namespace Darwin.Application.Loyalty.DTOs
{
    /// <summary>
    /// Request for starting a scan session from the business app after scanning a consumer QR.
    /// </summary>
    public sealed record StartScanRequestDto
    {
        /// <summary>
        /// Raw QR token string scanned from the consumer screen.
        /// </summary>
        public required string Token { get; init; }
    }

    /// <summary>
    /// Response created after a successful scan exchange.
    /// WebApi can map this to Contracts.StartScanResponse.
    /// </summary>
    public sealed record StartScanResponseDto
    {
        /// <summary>
        /// Server-side scan session identifier.
        /// </summary>
        public required Guid ScanSessionId { get; init; }

        /// <summary>
        /// The consumer user id participating in the session.
        /// Provided so business can confirm identity verbally.
        /// </summary>
        public required Guid ConsumerUserId { get; init; }

        /// <summary>
        /// The business user id participating in the session.
        /// </summary>
        public required Guid BusinessUserId { get; init; }

        /// <summary>
        /// Snapshot of existing points before accrual/redeem.
        /// </summary>
        public required int CurrentPointsBalance { get; init; }
    }
}
