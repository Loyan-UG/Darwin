using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Billing.DTOs;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Inventory;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Billing.Queries;

public sealed class GetSupplierInvoicesPageHandler
{
    private readonly IAppDbContext _db;

    public GetSupplierInvoicesPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<SupplierInvoicesPageDto> HandleAsync(Guid? businessId = null, string? query = null, SupplierInvoiceStatus? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var normalizedQuery = query?.Trim() ?? string.Empty;
        var context = await FinanceReportingQuerySupport.ResolveBusinessContextAsync(_db, businessId, ct).ConfigureAwait(false);
        var dto = new SupplierInvoicesPageDto
        {
            BusinessId = context.BusinessId,
            BusinessName = context.BusinessName,
            BusinessOptions = context.BusinessOptions,
            Query = normalizedQuery,
            Status = status,
            Page = page,
            PageSize = pageSize
        };
        if (!context.BusinessId.HasValue) return dto;

        var invoices = _db.Set<SupplierInvoice>()
            .AsNoTracking()
            .Where(x => x.BusinessId == context.BusinessId.Value && !x.IsDeleted);
        dto.DraftCount = await invoices.CountAsync(x => x.Status == SupplierInvoiceStatus.Draft, ct).ConfigureAwait(false);
        dto.MatchedCount = await invoices.CountAsync(x => x.Status == SupplierInvoiceStatus.Matched, ct).ConfigureAwait(false);
        dto.ApprovedCount = await invoices.CountAsync(x => x.Status == SupplierInvoiceStatus.Approved, ct).ConfigureAwait(false);
        dto.PostedCount = await invoices.CountAsync(x => x.Status == SupplierInvoiceStatus.Posted, ct).ConfigureAwait(false);
        dto.VoidedCount = await invoices.CountAsync(x => x.Status == SupplierInvoiceStatus.Voided, ct).ConfigureAwait(false);

        if (status.HasValue) invoices = invoices.Where(x => x.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            invoices = invoices.Where(x => x.SupplierInvoiceNumber.Contains(normalizedQuery) ||
                                           (x.InternalInvoiceNumber != null && x.InternalInvoiceNumber.Contains(normalizedQuery)) ||
                                           x.Lines.Any(line => !line.IsDeleted && (line.Description.Contains(normalizedQuery) || (line.SupplierSku != null && line.SupplierSku.Contains(normalizedQuery)))));
        }

        dto.Total = await invoices.CountAsync(ct).ConfigureAwait(false);
        dto.Items = await MapListItems(_db, invoices
                .OrderByDescending(x => x.InvoiceDateUtc)
                .ThenByDescending(x => x.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize))
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return dto;
    }

    private static IQueryable<SupplierInvoiceListItemDto> MapListItems(IAppDbContext db, IQueryable<SupplierInvoice> query)
        => query.Select(x => new SupplierInvoiceListItemDto
        {
            Id = x.Id,
            BusinessId = x.BusinessId,
            SupplierId = x.SupplierId,
            SupplierName = db.Set<Supplier>().Where(s => s.Id == x.SupplierId).Select(s => s.Name).FirstOrDefault() ?? string.Empty,
            SupplierInvoiceNumber = x.SupplierInvoiceNumber,
            InternalInvoiceNumber = x.InternalInvoiceNumber ?? string.Empty,
            Status = x.Status,
            InvoiceDateUtc = x.InvoiceDateUtc,
            DueDateUtc = x.DueDateUtc,
            Currency = x.Currency,
            TotalNetMinor = x.TotalNetMinor,
            TotalTaxMinor = x.TotalTaxMinor,
            TotalGrossMinor = x.TotalGrossMinor,
            LineCount = x.Lines.Count(line => !line.IsDeleted),
            DiscrepancyCount = x.Lines.Count(line => !line.IsDeleted && line.MatchStatus == SupplierInvoiceLineMatchStatus.Discrepancy),
            RowVersion = x.RowVersion
        });
}

public sealed class GetSupplierInvoiceDetailHandler
{
    private readonly IAppDbContext _db;

    public GetSupplierInvoiceDetailHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<SupplierInvoiceEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty) return null;
        return await _db.Set<SupplierInvoice>()
            .AsNoTracking()
            .Include(x => x.Lines)
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new SupplierInvoiceEditDto
            {
                Id = x.Id,
                RowVersion = x.RowVersion,
                BusinessId = x.BusinessId,
                SupplierId = x.SupplierId,
                PurchaseOrderId = x.PurchaseOrderId,
                GoodsReceiptId = x.GoodsReceiptId,
                SupplierInvoiceNumber = x.SupplierInvoiceNumber,
                InternalInvoiceNumber = x.InternalInvoiceNumber ?? string.Empty,
                Status = x.Status,
                InvoiceDateUtc = x.InvoiceDateUtc,
                ReceivedAtUtc = x.ReceivedAtUtc,
                DueDateUtc = x.DueDateUtc,
                PaymentTermDays = x.PaymentTermDays,
                Currency = x.Currency,
                TotalNetMinor = x.TotalNetMinor,
                TotalTaxMinor = x.TotalTaxMinor,
                TotalGrossMinor = x.TotalGrossMinor,
                MatchedAtUtc = x.MatchedAtUtc,
                ApprovedAtUtc = x.ApprovedAtUtc,
                PostedAtUtc = x.PostedAtUtc,
                VoidedAtUtc = x.VoidedAtUtc,
                PostingJournalEntryId = x.PostingJournalEntryId,
                InternalNotes = x.InternalNotes,
                MetadataJson = x.MetadataJson,
                Lines = x.Lines.Where(line => !line.IsDeleted).OrderBy(line => line.SortOrder).ThenBy(line => line.CreatedAtUtc).Select(line => new SupplierInvoiceLineDto
                {
                    Id = line.Id,
                    PurchaseOrderLineId = line.PurchaseOrderLineId,
                    GoodsReceiptLineId = line.GoodsReceiptLineId,
                    ProductVariantId = line.ProductVariantId,
                    SupplierSku = line.SupplierSku,
                    Description = line.Description,
                    InvoicedQuantity = line.InvoicedQuantity,
                    UnitNetMinor = line.UnitNetMinor,
                    UnitTaxMinor = line.UnitTaxMinor,
                    UnitGrossMinor = line.UnitGrossMinor,
                    TotalNetMinor = line.TotalNetMinor,
                    TotalTaxMinor = line.TotalTaxMinor,
                    TotalGrossMinor = line.TotalGrossMinor,
                    TaxRate = line.TaxRate,
                    MatchStatus = line.MatchStatus,
                    DiscrepancyReason = line.DiscrepancyReason,
                    SortOrder = line.SortOrder
                }).ToList()
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }
}
