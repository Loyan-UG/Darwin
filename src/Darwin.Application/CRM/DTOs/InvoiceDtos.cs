using Darwin.Domain.Enums;

namespace Darwin.Application.CRM.DTOs
{
    public sealed class InvoiceListItemDto
    {
        public Guid Id { get; set; }
        public Guid? BusinessId { get; set; }
        public Guid? CustomerId { get; set; }
        public string CustomerDisplayName { get; set; } = string.Empty;
        public CustomerTaxProfileType? CustomerTaxProfileType { get; set; }
        public string? CustomerVatId { get; set; }
        public Guid? OrderId { get; set; }
        public string? OrderNumber { get; set; }
        public Guid? PaymentId { get; set; }
        public string PaymentSummary { get; set; } = string.Empty;
        public InvoiceStatus Status { get; set; }
        public string Currency { get; set; } = Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCurrencyDefault;
        public long TotalNetMinor { get; set; }
        public long TotalTaxMinor { get; set; }
        public long TotalGrossMinor { get; set; }
        public long RefundedAmountMinor { get; set; }
        public long SettledAmountMinor { get; set; }
        public long BalanceMinor { get; set; }
        public DateTime DueDateUtc { get; set; }
        public DateTime? PaidAtUtc { get; set; }
        public DateTime? IssuedAtUtc { get; set; }
        public bool HasIssuedSnapshot { get; set; }
        public string? IssuedSnapshotHashSha256 { get; set; }
        public DateTime? ArchiveGeneratedAtUtc { get; set; }
        public DateTime? ArchiveRetainUntilUtc { get; set; }
        public string? ArchiveRetentionPolicyVersion { get; set; }
        public DateTime? ArchivePurgedAtUtc { get; set; }
        public string? ArchivePurgeReason { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class InvoiceEditDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public Guid? BusinessId { get; set; }
        public Guid? CustomerId { get; set; }
        public string CustomerDisplayName { get; set; } = string.Empty;
        public CustomerTaxProfileType? CustomerTaxProfileType { get; set; }
        public string? CustomerVatId { get; set; }
        public Guid? OrderId { get; set; }
        public string? OrderNumber { get; set; }
        public Guid? PaymentId { get; set; }
        public string PaymentSummary { get; set; } = string.Empty;
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
        public string Currency { get; set; } = Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCurrencyDefault;
        public long TotalNetMinor { get; set; }
        public long TotalTaxMinor { get; set; }
        public long TotalGrossMinor { get; set; }
        public long RefundedAmountMinor { get; set; }
        public long SettledAmountMinor { get; set; }
        public long BalanceMinor { get; set; }
        public DateTime DueDateUtc { get; set; }
        public DateTime? PaidAtUtc { get; set; }
        public DateTime? IssuedAtUtc { get; set; }
        public bool HasIssuedSnapshot { get; set; }
        public string? IssuedSnapshotHashSha256 { get; set; }
        public DateTime? ArchiveGeneratedAtUtc { get; set; }
        public DateTime? ArchiveRetainUntilUtc { get; set; }
        public string? ArchiveRetentionPolicyVersion { get; set; }
        public DateTime? ArchivePurgedAtUtc { get; set; }
        public string? ArchivePurgeReason { get; set; }
    }

    public sealed class PurgeExpiredInvoiceArchivesResultDto
    {
        public int EvaluatedCount { get; init; }
        public int PurgedCount { get; init; }
        public IReadOnlyList<Guid> PurgedInvoiceIds { get; init; } = Array.Empty<Guid>();
    }

    public sealed class InvoiceArchiveSnapshotDto
    {
        public Guid InvoiceId { get; set; }
        public DateTime IssuedAtUtc { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string SnapshotJson { get; set; } = string.Empty;
    }

    public sealed class InvoiceArchiveDocumentDto
    {
        public Guid InvoiceId { get; set; }
        public DateTime IssuedAtUtc { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Html { get; set; } = string.Empty;
    }

    public sealed class InvoiceStructuredDataExportDto
    {
        public Guid InvoiceId { get; set; }
        public DateTime IssuedAtUtc { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Json { get; set; } = string.Empty;
    }

    public sealed class InvoiceStatusTransitionDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public InvoiceStatus TargetStatus { get; set; }
        public DateTime? PaidAtUtc { get; set; }
    }

    public sealed class InvoiceReverseChargeDecisionDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public bool Applies { get; set; }
        public string? Note { get; set; }
    }

    public sealed class InvoiceRefundCreateDto
    {
        public Guid InvoiceId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public long AmountMinor { get; set; }
        public string Currency { get; set; } = Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCurrencyDefault;
        public string Reason { get; set; } = string.Empty;
    }
}
