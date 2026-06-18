using Darwin.Domain.Enums;

namespace Darwin.Application.Sales.DTOs;

public enum CreditNoteDocumentFilter
{
    All = 0,
    Draft = 1,
    Issued = 2,
    Voided = 3,
    Cancelled = 4,
    Open = 5
}

public sealed class CreditNoteCreateLineDto
{
    public Guid InvoiceLineId { get; set; }
    public int CreditedQuantity { get; set; }
}

public sealed class CreditNoteCreateDto
{
    public Guid InvoiceId { get; set; }
    public Guid? ReturnOrderId { get; set; }
    public Guid? RefundId { get; set; }
    public CreditNoteReason Reason { get; set; } = CreditNoteReason.PostIssueCorrection;
    public Guid? ActorUserId { get; set; }
    public string? InternalNotes { get; set; }
    public List<CreditNoteCreateLineDto> Lines { get; set; } = new();
}

public sealed class CreditNoteLifecycleDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public Guid? ActorUserId { get; set; }
    public string? Reason { get; set; }
}

public class CreditNoteListItemDto
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid? ReturnOrderId { get; set; }
    public Guid? RefundId { get; set; }
    public string? CreditNoteNumber { get; set; }
    public string? OriginalInvoiceNumber { get; set; }
    public CreditNoteStatus Status { get; set; }
    public CreditNoteReason Reason { get; set; }
    public Guid? BusinessId { get; set; }
    public Guid? CustomerId { get; set; }
    public string Currency { get; set; } = string.Empty;
    public long TotalNetMinor { get; set; }
    public long TotalTaxMinor { get; set; }
    public long TotalGrossMinor { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? IssuedAtUtc { get; set; }
    public DateTime? VoidedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public bool HasSourceModel { get; set; }
    public bool HasArchiveMetadata { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class CreditNoteDetailDto : CreditNoteListItemDto
{
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
    public List<CreditNoteLineDetailDto> Lines { get; set; } = new();
}

public sealed class CreditNoteLineDetailDto
{
    public Guid Id { get; set; }
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

public sealed class CreditNoteSourceExportDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/json";
    public string SourceModelJson { get; set; } = "{}";
    public string SourceModelHashSha256 { get; set; } = string.Empty;
}
