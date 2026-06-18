using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Sales.DTOs;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Sales;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Sales.Queries;

public sealed class GetCreditNotesPageHandler
{
    private const int MaxPageSize = 200;
    private readonly IAppDbContext _db;

    public GetCreditNotesPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<(List<CreditNoteListItemDto> Items, int Total)> HandleAsync(
        int page,
        int pageSize,
        string? query = null,
        CreditNoteDocumentFilter filter = CreditNoteDocumentFilter.All,
        Guid? businessId = null,
        Guid? customerId = null,
        DateTime? issuedFromUtc = null,
        DateTime? issuedToUtc = null,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var baseQuery = _db.Set<CreditNote>().AsNoTracking().Where(x => !x.IsDeleted);
        var q = query?.Trim();
        if (!string.IsNullOrWhiteSpace(q))
        {
            baseQuery = baseQuery.Where(x =>
                (x.CreditNoteNumber != null && x.CreditNoteNumber.Contains(q)) ||
                (x.OriginalInvoiceNumber != null && x.OriginalInvoiceNumber.Contains(q)));
        }

        if (businessId.HasValue && businessId.Value != Guid.Empty)
        {
            baseQuery = baseQuery.Where(x => x.BusinessId == businessId.Value);
        }

        if (customerId.HasValue && customerId.Value != Guid.Empty)
        {
            baseQuery = baseQuery.Where(x => x.CustomerId == customerId.Value);
        }

        if (issuedFromUtc.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.IssuedAtUtc >= issuedFromUtc.Value);
        }

        if (issuedToUtc.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.IssuedAtUtc <= issuedToUtc.Value);
        }

        baseQuery = filter switch
        {
            CreditNoteDocumentFilter.Draft => baseQuery.Where(x => x.Status == CreditNoteStatus.Draft),
            CreditNoteDocumentFilter.Issued => baseQuery.Where(x => x.Status == CreditNoteStatus.Issued),
            CreditNoteDocumentFilter.Voided => baseQuery.Where(x => x.Status == CreditNoteStatus.Voided),
            CreditNoteDocumentFilter.Cancelled => baseQuery.Where(x => x.Status == CreditNoteStatus.Cancelled),
            CreditNoteDocumentFilter.Open => baseQuery.Where(x => x.Status == CreditNoteStatus.Draft || x.Status == CreditNoteStatus.Issued),
            _ => baseQuery
        };

        var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);
        var items = await baseQuery
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new CreditNoteListItemDto
            {
                Id = x.Id,
                InvoiceId = x.InvoiceId,
                ReturnOrderId = x.ReturnOrderId,
                RefundId = x.RefundId,
                CreditNoteNumber = x.CreditNoteNumber,
                OriginalInvoiceNumber = x.OriginalInvoiceNumber,
                Status = x.Status,
                Reason = x.Reason,
                BusinessId = x.BusinessId,
                CustomerId = x.CustomerId,
                Currency = x.Currency,
                TotalNetMinor = x.TotalNetMinor,
                TotalTaxMinor = x.TotalTaxMinor,
                TotalGrossMinor = x.TotalGrossMinor,
                CreatedAtUtc = x.CreatedAtUtc,
                IssuedAtUtc = x.IssuedAtUtc,
                VoidedAtUtc = x.VoidedAtUtc,
                CancelledAtUtc = x.CancelledAtUtc,
                HasSourceModel = !string.IsNullOrWhiteSpace(x.SourceModelHashSha256),
                HasArchiveMetadata = x.ArchiveGeneratedAtUtc.HasValue,
                RowVersion = x.RowVersion
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return (items, total);
    }
}

public sealed class GetCreditNoteDetailHandler
{
    private readonly IAppDbContext _db;

    public GetCreditNoteDetailHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<CreditNoteDetailDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        return await _db.Set<CreditNote>()
            .AsNoTracking()
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new CreditNoteDetailDto
            {
                Id = x.Id,
                InvoiceId = x.InvoiceId,
                ReturnOrderId = x.ReturnOrderId,
                RefundId = x.RefundId,
                CreditNoteNumber = x.CreditNoteNumber,
                OriginalInvoiceNumber = x.OriginalInvoiceNumber,
                Status = x.Status,
                Reason = x.Reason,
                BusinessId = x.BusinessId,
                CustomerId = x.CustomerId,
                Currency = x.Currency,
                TotalNetMinor = x.TotalNetMinor,
                TotalTaxMinor = x.TotalTaxMinor,
                TotalGrossMinor = x.TotalGrossMinor,
                CreatedAtUtc = x.CreatedAtUtc,
                IssuedAtUtc = x.IssuedAtUtc,
                VoidedAtUtc = x.VoidedAtUtc,
                CancelledAtUtc = x.CancelledAtUtc,
                SourceModelJson = x.SourceModelJson,
                SourceModelHashSha256 = x.SourceModelHashSha256,
                ArchiveGeneratedAtUtc = x.ArchiveGeneratedAtUtc,
                ArchiveRetainUntilUtc = x.ArchiveRetainUntilUtc,
                ArchiveRetentionPolicyVersion = x.ArchiveRetentionPolicyVersion,
                ArchivePurgedAtUtc = x.ArchivePurgedAtUtc,
                ArchivePurgeReason = x.ArchivePurgeReason,
                PostingJournalEntryId = x.PostingJournalEntryId,
                InternalNotes = x.InternalNotes,
                MetadataJson = x.MetadataJson,
                HasSourceModel = !string.IsNullOrWhiteSpace(x.SourceModelHashSha256),
                HasArchiveMetadata = x.ArchiveGeneratedAtUtc.HasValue,
                RowVersion = x.RowVersion,
                Lines = x.Lines
                    .Where(line => !line.IsDeleted)
                    .OrderBy(line => line.SortOrder)
                    .ThenBy(line => line.CreatedAtUtc)
                    .Select(line => new CreditNoteLineDetailDto
                    {
                        Id = line.Id,
                        InvoiceLineId = line.InvoiceLineId,
                        Description = line.Description,
                        OriginalQuantity = line.OriginalQuantity,
                        CreditedQuantity = line.CreditedQuantity,
                        UnitPriceNetMinor = line.UnitPriceNetMinor,
                        TaxRate = line.TaxRate,
                        TotalNetMinor = line.TotalNetMinor,
                        TotalTaxMinor = line.TotalTaxMinor,
                        TotalGrossMinor = line.TotalGrossMinor,
                        SourceLineJson = line.SourceLineJson,
                        SortOrder = line.SortOrder
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }
}

public sealed class GetInvoiceLinesForCreditNoteHandler
{
    private readonly IAppDbContext _db;

    public GetInvoiceLinesForCreditNoteHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<List<CreditNoteCreateLineDto>> HandleAsync(Guid invoiceId, CancellationToken ct = default)
    {
        if (invoiceId == Guid.Empty)
        {
            return new List<CreditNoteCreateLineDto>();
        }

        var creditedByLine = await _db.Set<CreditNote>()
            .AsNoTracking()
            .Where(x => x.InvoiceId == invoiceId && x.Status == CreditNoteStatus.Issued && !x.IsDeleted)
            .SelectMany(x => x.Lines.Where(line => !line.IsDeleted && line.InvoiceLineId.HasValue))
            .GroupBy(x => x.InvoiceLineId!.Value)
            .Select(x => new { InvoiceLineId = x.Key, CreditedQuantity = x.Sum(line => line.CreditedQuantity) })
            .ToDictionaryAsync(x => x.InvoiceLineId, x => x.CreditedQuantity, ct)
            .ConfigureAwait(false);

        var lines = await _db.Set<InvoiceLine>()
            .AsNoTracking()
            .Where(x => x.InvoiceId == invoiceId && !x.IsDeleted)
            .OrderBy(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id)
            .Select(x => new CreditNoteCreateLineDto
            {
                InvoiceLineId = x.Id,
                CreditedQuantity = x.Quantity
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var line in lines)
        {
            line.CreditedQuantity = Math.Max(0, line.CreditedQuantity - creditedByLine.GetValueOrDefault(line.InvoiceLineId));
        }

        return lines.Where(x => x.CreditedQuantity > 0).ToList();
    }
}

public sealed class GetCreditNoteSourceExportHandler
{
    private readonly IAppDbContext _db;

    public GetCreditNoteSourceExportHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<CreditNoteSourceExportDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        return await _db.Set<CreditNote>()
            .AsNoTracking()
            .Where(x =>
                x.Id == id &&
                !x.IsDeleted &&
                !string.IsNullOrWhiteSpace(x.SourceModelJson) &&
                !string.IsNullOrWhiteSpace(x.SourceModelHashSha256) &&
                x.Status != CreditNoteStatus.Draft &&
                x.Status != CreditNoteStatus.Cancelled)
            .Select(x => new CreditNoteSourceExportDto
            {
                Id = x.Id,
                FileName = BuildSafeSourceFileName(x.CreditNoteNumber, x.Id),
                SourceModelJson = x.SourceModelJson,
                SourceModelHashSha256 = x.SourceModelHashSha256!,
                ContentType = "application/json"
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    private static string BuildSafeSourceFileName(string? creditNoteNumber, Guid id)
    {
        var source = string.IsNullOrWhiteSpace(creditNoteNumber) ? $"credit-note-{id:N}" : creditNoteNumber.Trim();
        var safe = new string(source.Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' or '.' ? ch : '-').ToArray());
        safe = safe.Trim('-', '.', '_');
        return (string.IsNullOrWhiteSpace(safe) ? $"credit-note-{id:N}" : safe) + "-source-model.json";
    }
}
