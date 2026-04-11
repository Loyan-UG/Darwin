using System;
using Darwin.Domain.Enums;

namespace Darwin.Application.Loyalty.DTOs
{
    /// <summary>
    /// Lightweight admin list row for loyalty account operations.
    /// </summary>
    public sealed class LoyaltyAccountAdminListItemDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public Guid UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string UserDisplayName { get; set; } = string.Empty;
        public LoyaltyAccountStatus Status { get; set; }
        public int PointsBalance { get; set; }
        public int LifetimePoints { get; set; }
        public DateTime? LastAccrualAtUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class LoyaltyAccountOpsSummaryDto
    {
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int SuspendedCount { get; set; }
        public int ZeroBalanceCount { get; set; }
        public int RecentAccrualCount { get; set; }
    }

    public sealed class LoyaltyScanSessionAdminListItemDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public Guid LoyaltyAccountId { get; set; }
        public string CustomerDisplayName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public LoyaltyScanMode Mode { get; set; }
        public LoyaltyScanStatus Status { get; set; }
        public string Outcome { get; set; } = string.Empty;
        public string? FailureReason { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
    }

    public sealed class LoyaltyScanSessionOpsSummaryDto
    {
        public int TotalCount { get; set; }
        public int AccrualCount { get; set; }
        public int RedemptionCount { get; set; }
        public int PendingCount { get; set; }
        public int ExpiredCount { get; set; }
        public int FailureCount { get; set; }
    }
}
