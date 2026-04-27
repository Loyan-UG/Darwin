namespace Darwin.Application.Orders.DTOs
{
    public sealed class ShipmentProviderOperationListItemDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public Guid ShipmentId { get; set; }
        public Guid OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string OperationType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int AttemptCount { get; set; }
        public DateTime? LastAttemptAtUtc { get; set; }
        public DateTime? ProcessedAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public int AgeMinutes { get; set; }
        public bool IsStalePending { get; set; }
        public string? FailureReason { get; set; }
        public string? TrackingNumber { get; set; }
        public string? LabelUrl { get; set; }
    }

    public sealed class ShipmentProviderOperationSummaryDto
    {
        public int TotalCount { get; set; }
        public int PendingCount { get; set; }
        public int FailedCount { get; set; }
        public int ProcessedCount { get; set; }
        public int StalePendingCount { get; set; }
        public int CancelledCount { get; set; }
    }

    public sealed class ShipmentProviderOperationFilterDto
    {
        public string Query { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string OperationType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool StalePendingOnly { get; set; }
        public bool FailedOnly { get; set; }
    }

    public sealed class UpdateShipmentProviderOperationDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public string Action { get; set; } = string.Empty;
        public string? FailureReason { get; set; }
    }
}
