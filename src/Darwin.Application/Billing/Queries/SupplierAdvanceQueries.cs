using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Billing.Commands;
using Darwin.Application.Billing.DTOs;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Inventory;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Billing.Queries;

public sealed class GetSupplierAdvancesPageHandler
{
    private readonly IAppDbContext _db;

    public GetSupplierAdvancesPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<SupplierAdvancesPageDto> HandleAsync(Guid? businessId = null, string? query = null, SupplierAdvanceStatus? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var normalizedQuery = query?.Trim() ?? string.Empty;
        var context = await FinanceReportingQuerySupport.ResolveBusinessContextAsync(_db, businessId, ct).ConfigureAwait(false);
        var dto = new SupplierAdvancesPageDto
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

        var advances = _db.Set<SupplierAdvance>()
            .AsNoTracking()
            .Where(x => x.BusinessId == context.BusinessId.Value && !x.IsDeleted);
        dto.DraftCount = await advances.CountAsync(x => x.Status == SupplierAdvanceStatus.Draft, ct).ConfigureAwait(false);
        dto.PostedCount = await advances.CountAsync(x => x.Status == SupplierAdvanceStatus.Posted, ct).ConfigureAwait(false);
        dto.AppliedCount = await advances.CountAsync(x => x.Status == SupplierAdvanceStatus.Applied, ct).ConfigureAwait(false);
        dto.CancelledCount = await advances.CountAsync(x => x.Status == SupplierAdvanceStatus.Cancelled, ct).ConfigureAwait(false);
        dto.ReversedCount = await advances.CountAsync(x => x.Status == SupplierAdvanceStatus.Reversed, ct).ConfigureAwait(false);

        if (status.HasValue) advances = advances.Where(x => x.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            advances = advances.Where(x =>
                (x.AdvanceNumber != null && x.AdvanceNumber.Contains(normalizedQuery)) ||
                (x.Reference != null && x.Reference.Contains(normalizedQuery)) ||
                _db.Set<Supplier>().Any(s => s.Id == x.SupplierId && s.Name.Contains(normalizedQuery)));
        }

        dto.Total = await advances.CountAsync(ct).ConfigureAwait(false);
        dto.Items = await advances
            .OrderByDescending(x => x.AdvanceDateUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new SupplierAdvanceListItemDto
            {
                Id = x.Id,
                BusinessId = x.BusinessId,
                SupplierId = x.SupplierId,
                SupplierName = _db.Set<Supplier>().Where(s => s.Id == x.SupplierId).Select(s => s.Name).FirstOrDefault() ?? string.Empty,
                AdvanceNumber = x.AdvanceNumber ?? string.Empty,
                Status = x.Status,
                PaymentMethod = x.PaymentMethod,
                AdvanceDateUtc = x.AdvanceDateUtc,
                Currency = x.Currency,
                TotalAmountMinor = x.TotalAmountMinor,
                OpenAmountMinor = x.OpenAmountMinor,
                Reference = x.Reference ?? string.Empty,
                ApplicationCount = x.Applications.Count(a => !a.IsDeleted),
                RowVersion = x.RowVersion
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return dto;
    }
}

public sealed class GetSupplierAdvanceDetailHandler
{
    private readonly IAppDbContext _db;

    public GetSupplierAdvanceDetailHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<SupplierAdvanceEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty) return null;
        var advance = await _db.Set<SupplierAdvance>()
            .AsNoTracking()
            .Include(x => x.Applications)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (advance is null) return null;

        var applicationInvoiceIds = advance.Applications.Where(x => !x.IsDeleted).Select(x => x.SupplierInvoiceId).ToArray();
        var invoices = await _db.Set<SupplierInvoice>()
            .AsNoTracking()
            .Where(x => applicationInvoiceIds.Contains(x.Id) && !x.IsDeleted)
            .ToDictionaryAsync(x => x.Id, ct)
            .ConfigureAwait(false);
        var paid = await SupplierPaymentQuerySupport.GetPostedPaidByInvoiceAsync(_db, applicationInvoiceIds, null, ct).ConfigureAwait(false);
        var applied = await SupplierAdvanceSupport.GetAppliedByInvoiceAsync(_db, applicationInvoiceIds, null, ct).ConfigureAwait(false);

        var dto = new SupplierAdvanceEditDto
        {
            Id = advance.Id,
            RowVersion = advance.RowVersion,
            BusinessId = advance.BusinessId,
            SupplierId = advance.SupplierId,
            AdvanceNumber = advance.AdvanceNumber ?? string.Empty,
            Status = advance.Status,
            PaymentMethod = advance.PaymentMethod,
            AdvanceDateUtc = advance.AdvanceDateUtc,
            Currency = advance.Currency,
            TotalAmountMinor = advance.TotalAmountMinor,
            OpenAmountMinor = advance.OpenAmountMinor,
            Reference = advance.Reference,
            PostingJournalEntryId = advance.PostingJournalEntryId,
            ReversalJournalEntryId = advance.ReversalJournalEntryId,
            PostedAtUtc = advance.PostedAtUtc,
            CancelledAtUtc = advance.CancelledAtUtc,
            ReversedAtUtc = advance.ReversedAtUtc,
            ReversalReason = advance.ReversalReason,
            InternalNotes = advance.InternalNotes,
            MetadataJson = advance.MetadataJson,
            Applications = advance.Applications.Where(x => !x.IsDeleted).OrderBy(x => x.AppliedAtUtc).Select(application =>
            {
                invoices.TryGetValue(application.SupplierInvoiceId, out var invoice);
                var settled = paid.GetValueOrDefault(application.SupplierInvoiceId) + applied.GetValueOrDefault(application.SupplierInvoiceId);
                var gross = invoice?.TotalGrossMinor ?? 0;
                return new SupplierAdvanceApplicationDto
                {
                    Id = application.Id,
                    SupplierInvoiceId = application.SupplierInvoiceId,
                    SupplierInvoiceNumber = invoice?.SupplierInvoiceNumber ?? string.Empty,
                    InternalInvoiceNumber = invoice?.InternalInvoiceNumber ?? string.Empty,
                    DueDateUtc = invoice?.DueDateUtc,
                    InvoiceGrossMinor = gross,
                    AlreadySettledMinor = settled,
                    InvoiceOpenAmountMinor = Math.Max(0, gross - settled),
                    AmountMinor = application.AmountMinor,
                    Memo = application.Memo,
                    PostingJournalEntryId = application.PostingJournalEntryId,
                    ReversalJournalEntryId = application.ReversalJournalEntryId,
                    AppliedAtUtc = application.AppliedAtUtc,
                    ReversedAtUtc = application.ReversedAtUtc,
                    ReversalReason = application.ReversalReason
                };
            }).ToList()
        };
        dto.ApplicationCandidates = await GetApplicationCandidatesAsync(advance, ct).ConfigureAwait(false);
        return dto;
    }

    private async Task<List<SupplierAdvanceApplicationDto>> GetApplicationCandidatesAsync(SupplierAdvance advance, CancellationToken ct)
    {
        if (advance.Status != SupplierAdvanceStatus.Posted || advance.OpenAmountMinor <= 0)
        {
            return new List<SupplierAdvanceApplicationDto>();
        }

        var invoices = await _db.Set<SupplierInvoice>()
            .AsNoTracking()
            .Where(x =>
                x.BusinessId == advance.BusinessId &&
                x.SupplierId == advance.SupplierId &&
                x.Status == SupplierInvoiceStatus.Posted &&
                x.PostingJournalEntryId.HasValue &&
                x.Currency == advance.Currency &&
                !x.IsDeleted)
            .OrderBy(x => x.DueDateUtc ?? x.InvoiceDateUtc)
            .ThenBy(x => x.SupplierInvoiceNumber)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var invoiceIds = invoices.Select(x => x.Id).ToArray();
        var paid = await SupplierPaymentQuerySupport.GetPostedPaidByInvoiceAsync(_db, invoiceIds, null, ct).ConfigureAwait(false);
        var applied = await SupplierAdvanceSupport.GetAppliedByInvoiceAsync(_db, invoiceIds, null, ct).ConfigureAwait(false);

        return invoices
            .Select(invoice =>
            {
                var settled = paid.GetValueOrDefault(invoice.Id) + applied.GetValueOrDefault(invoice.Id);
                var open = Math.Max(0, invoice.TotalGrossMinor - settled);
                return new SupplierAdvanceApplicationDto
                {
                    SupplierInvoiceId = invoice.Id,
                    SupplierInvoiceNumber = invoice.SupplierInvoiceNumber,
                    InternalInvoiceNumber = invoice.InternalInvoiceNumber ?? string.Empty,
                    DueDateUtc = invoice.DueDateUtc,
                    InvoiceGrossMinor = invoice.TotalGrossMinor,
                    AlreadySettledMinor = settled,
                    InvoiceOpenAmountMinor = open,
                    AmountMinor = Math.Min(open, advance.OpenAmountMinor)
                };
            })
            .Where(x => x.InvoiceOpenAmountMinor > 0)
            .ToList();
    }
}

public sealed class GetSupplierAdvanceDraftHandler
{
    private readonly IAppDbContext _db;

    public GetSupplierAdvanceDraftHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<SupplierAdvanceEditDto> HandleAsync(Guid? businessId, Guid? supplierId, CancellationToken ct = default)
    {
        var context = await FinanceReportingQuerySupport.ResolveBusinessContextAsync(_db, businessId, ct).ConfigureAwait(false);
        return new SupplierAdvanceEditDto
        {
            BusinessId = context.BusinessId ?? Guid.Empty,
            SupplierId = supplierId ?? Guid.Empty,
            AdvanceDateUtc = DateTime.UtcNow,
            Currency = "EUR",
            MetadataJson = "{}"
        };
    }
}
