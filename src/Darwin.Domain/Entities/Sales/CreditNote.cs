using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Sales;

/// <summary>
/// Formal finance-gated sales credit document derived from issued invoice evidence.
/// </summary>
public sealed class CreditNote : BaseEntity
{
    public Guid? BusinessId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid? ReturnOrderId { get; set; }
    public Guid? RefundId { get; set; }
    public string? CreditNoteNumber { get; set; }
    public CreditNoteStatus Status { get; set; } = CreditNoteStatus.Draft;
    public CreditNoteReason Reason { get; set; } = CreditNoteReason.PostIssueCorrection;
    public string Currency { get; set; } = DomainDefaults.DefaultCurrency;
    public string? OriginalInvoiceNumber { get; set; }
    public DateTime? IssuedAtUtc { get; set; }
    public Guid? IssuedByUserId { get; set; }
    public DateTime? VoidedAtUtc { get; set; }
    public Guid? VoidedByUserId { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public Guid? CancelledByUserId { get; set; }
    public long TotalNetMinor { get; set; }
    public long TotalTaxMinor { get; set; }
    public long TotalGrossMinor { get; set; }
    public string SourceModelJson { get; set; } = "{}";
    public string? SourceModelHashSha256 { get; set; }
    public DateTime? ArchiveGeneratedAtUtc { get; set; }
    public DateTime? ArchiveRetainUntilUtc { get; set; }
    public string? ArchiveRetentionPolicyVersion { get; set; }
    public DateTime? ArchivePurgedAtUtc { get; set; }
    public string? ArchivePurgeReason { get; set; }
    public Guid? PostingJournalEntryId { get; set; }
    public string? InternalNotes { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public List<CreditNoteLine> Lines { get; set; } = new();
}

/// <summary>
/// Credit note line snapshot copied from issued invoice evidence, never recomputed from live catalog data.
/// </summary>
public sealed class CreditNoteLine : BaseEntity
{
    public Guid CreditNoteId { get; set; }
    public Guid? InvoiceLineId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int OriginalQuantity { get; set; }
    public int CreditedQuantity { get; set; }
    public long UnitPriceNetMinor { get; set; }
    public decimal TaxRate { get; set; }
    public long TotalNetMinor { get; set; }
    public long TotalTaxMinor { get; set; }
    public long TotalGrossMinor { get; set; }
    public string SourceLineJson { get; set; } = "{}";
    public int SortOrder { get; set; }
}
