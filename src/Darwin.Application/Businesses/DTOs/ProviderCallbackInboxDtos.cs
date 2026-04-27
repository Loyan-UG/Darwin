namespace Darwin.Application.Businesses.DTOs
{
    public sealed class ProviderCallbackInboxListItemDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public string Provider { get; set; } = string.Empty;
        public string CallbackType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? IdempotencyKey { get; set; }
        public int AttemptCount { get; set; }
        public DateTime? LastAttemptAtUtc { get; set; }
        public DateTime? ProcessedAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public int AgeMinutes { get; set; }
        public bool IsStalePending { get; set; }
        public string? FailureReason { get; set; }
        public string PayloadPreview { get; set; } = string.Empty;
    }

    public sealed class ProviderCallbackInboxSummaryDto
    {
        public int TotalCount { get; set; }
        public int PendingCount { get; set; }
        public int FailedCount { get; set; }
        public int ProcessedCount { get; set; }
        public int StalePendingCount { get; set; }
        public int RetriedCount { get; set; }
    }

    public sealed class ProviderCallbackInboxFilterDto
    {
        public string Query { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool StalePendingOnly { get; set; }
        public bool FailedOnly { get; set; }
    }

    public sealed class UpdateProviderCallbackInboxMessageDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public string Action { get; set; } = string.Empty;
        public string? FailureReason { get; set; }
    }
}
