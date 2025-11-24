using System;
using Darwin.Domain.Enums;

namespace Darwin.Application.Loyalty.DTOs
{
    /// <summary>
    /// DTO to request issuing a QR token for the consumer app.
    /// </summary>
    public sealed class IssueQrCodeTokenDto
    {
        public Guid UserId { get; set; }
        public Guid? LoyaltyAccountId { get; set; }
        public QrTokenPurpose Purpose { get; set; } = QrTokenPurpose.IdentityOnly;

        /// <summary>
        /// Token TTL in seconds. Must be short-lived (e.g., 60-120s).
        /// </summary>
        public int TtlSeconds { get; set; } = 90;

        /// <summary>
        /// Optional device installation id to bind issuance to a device.
        /// </summary>
        public string? IssuedDeviceId { get; set; }
    }

    /// <summary>
    /// DTO returned after issuing a QR token.
    /// </summary>
    public sealed class QrCodeTokenIssuedDto
    {
        public Guid Id { get; set; }
        public string Token { get; set; } = default!;
        public QrTokenPurpose Purpose { get; set; }
        public DateTime IssuedAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
    }

    /// <summary>
    /// DTO used by business app to process a scanned QR token.
    /// </summary>
    public sealed class ProcessQrScanDto
    {
        public string Token { get; set; } = default!;
        public Guid BusinessId { get; set; }
        public Guid? BusinessLocationId { get; set; }
        public Guid? PerformedByUserId { get; set; }

        /// <summary>
        /// Scan purpose expected by the business device.
        /// </summary>
        public QrTokenPurpose ExpectedPurpose { get; set; } = QrTokenPurpose.Accrual;

        /// <summary>
        /// For redemption scans, the reward tier to redeem.
        /// </summary>
        public Guid? LoyaltyRewardTierId { get; set; }
    }

    /// <summary>
    /// Result DTO returned to the business app after processing a scan.
    /// </summary>
    public sealed class ProcessQrScanResultDto
    {
        public bool Accepted { get; set; }
        public string Outcome { get; set; } = default!;
        public string? FailureReason { get; set; }
        public Guid? LoyaltyAccountId { get; set; }
        public int? NewPointsBalance { get; set; }
        public Guid? ResultingTransactionId { get; set; }
        public Guid? RewardRedemptionId { get; set; }
    }
}
