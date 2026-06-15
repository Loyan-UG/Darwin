using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Billing.DTOs;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Inventory;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Billing.Queries;

public sealed class GetSupplierPaymentsPageHandler
{
    private readonly IAppDbContext _db;

    public GetSupplierPaymentsPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<SupplierPaymentsPageDto> HandleAsync(Guid? businessId = null, string? query = null, SupplierPaymentStatus? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var normalizedQuery = query?.Trim() ?? string.Empty;
        var context = await FinanceReportingQuerySupport.ResolveBusinessContextAsync(_db, businessId, ct).ConfigureAwait(false);
        var dto = new SupplierPaymentsPageDto
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

        var payments = _db.Set<SupplierPayment>()
            .AsNoTracking()
            .Where(x => x.BusinessId == context.BusinessId.Value && !x.IsDeleted);
        dto.DraftCount = await payments.CountAsync(x => x.Status == SupplierPaymentStatus.Draft, ct).ConfigureAwait(false);
        dto.PostedCount = await payments.CountAsync(x => x.Status == SupplierPaymentStatus.Posted, ct).ConfigureAwait(false);
        dto.CancelledCount = await payments.CountAsync(x => x.Status == SupplierPaymentStatus.Cancelled, ct).ConfigureAwait(false);
        dto.ReversedCount = await payments.CountAsync(x => x.Status == SupplierPaymentStatus.Reversed, ct).ConfigureAwait(false);

        if (status.HasValue) payments = payments.Where(x => x.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            payments = payments.Where(x =>
                (x.PaymentNumber != null && x.PaymentNumber.Contains(normalizedQuery)) ||
                (x.Reference != null && x.Reference.Contains(normalizedQuery)) ||
                x.Allocations.Any(a => !a.IsDeleted && _db.Set<SupplierInvoice>().Any(inv => inv.Id == a.SupplierInvoiceId && inv.SupplierInvoiceNumber.Contains(normalizedQuery))));
        }

        dto.Total = await payments.CountAsync(ct).ConfigureAwait(false);
        dto.Items = await payments
            .OrderByDescending(x => x.PaymentDateUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new SupplierPaymentListItemDto
            {
                Id = x.Id,
                BusinessId = x.BusinessId,
                SupplierId = x.SupplierId,
                SupplierName = _db.Set<Supplier>().Where(s => s.Id == x.SupplierId).Select(s => s.Name).FirstOrDefault() ?? string.Empty,
                PaymentNumber = x.PaymentNumber ?? string.Empty,
                Status = x.Status,
                PaymentMethod = x.PaymentMethod,
                PaymentDateUtc = x.PaymentDateUtc,
                Currency = x.Currency,
                TotalAmountMinor = x.TotalAmountMinor,
                AllocationCount = x.Allocations.Count(a => !a.IsDeleted),
                Reference = x.Reference ?? string.Empty,
                RowVersion = x.RowVersion
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return dto;
    }
}

public sealed class GetSupplierPaymentDetailHandler
{
    private readonly IAppDbContext _db;

    public GetSupplierPaymentDetailHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<SupplierPaymentEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty) return null;
        var payment = await _db.Set<SupplierPayment>()
            .AsNoTracking()
            .Include(x => x.Allocations)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (payment is null) return null;

        var allocationInvoiceIds = payment.Allocations.Where(x => !x.IsDeleted).Select(x => x.SupplierInvoiceId).ToArray();
        var invoices = await _db.Set<SupplierInvoice>()
            .AsNoTracking()
            .Where(x => allocationInvoiceIds.Contains(x.Id) && !x.IsDeleted)
            .ToDictionaryAsync(x => x.Id, ct)
            .ConfigureAwait(false);
        var alreadyPaid = await SupplierPaymentQuerySupport.GetPostedPaidByInvoiceAsync(_db, allocationInvoiceIds, payment.Id, ct).ConfigureAwait(false);

        return new SupplierPaymentEditDto
        {
            Id = payment.Id,
            RowVersion = payment.RowVersion,
            BusinessId = payment.BusinessId,
            SupplierId = payment.SupplierId,
            PaymentNumber = payment.PaymentNumber ?? string.Empty,
            Status = payment.Status,
            PaymentMethod = payment.PaymentMethod,
            PaymentDateUtc = payment.PaymentDateUtc,
            Currency = payment.Currency,
            TotalAmountMinor = payment.TotalAmountMinor,
            Reference = payment.Reference,
            PostingJournalEntryId = payment.PostingJournalEntryId,
            PostedAtUtc = payment.PostedAtUtc,
            ReversalJournalEntryId = payment.ReversalJournalEntryId,
            ReversedAtUtc = payment.ReversedAtUtc,
            ReversalReason = payment.ReversalReason,
            CancelledAtUtc = payment.CancelledAtUtc,
            InternalNotes = payment.InternalNotes,
            MetadataJson = payment.MetadataJson,
            Allocations = payment.Allocations.Where(x => !x.IsDeleted).OrderBy(x => x.CreatedAtUtc).Select(allocation =>
            {
                invoices.TryGetValue(allocation.SupplierInvoiceId, out var invoice);
                var paid = alreadyPaid.GetValueOrDefault(allocation.SupplierInvoiceId);
                var gross = invoice?.TotalGrossMinor ?? 0;
                return new SupplierPaymentAllocationDto
                {
                    SupplierInvoiceId = allocation.SupplierInvoiceId,
                    SupplierInvoiceNumber = invoice?.SupplierInvoiceNumber ?? string.Empty,
                    InternalInvoiceNumber = invoice?.InternalInvoiceNumber ?? string.Empty,
                    DueDateUtc = invoice?.DueDateUtc,
                    InvoiceGrossMinor = gross,
                    AlreadyPaidMinor = paid,
                    OpenAmountMinor = Math.Max(0, gross - paid),
                    AmountMinor = allocation.AmountMinor,
                    Memo = allocation.Memo
                };
            }).ToList()
        };
    }
}

public sealed class GetSupplierPaymentDraftHandler
{
    private readonly IAppDbContext _db;

    public GetSupplierPaymentDraftHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<SupplierPaymentEditDto> HandleAsync(Guid? businessId, Guid? supplierId, Guid? supplierInvoiceId, CancellationToken ct = default)
    {
        var context = await FinanceReportingQuerySupport.ResolveBusinessContextAsync(_db, businessId, ct).ConfigureAwait(false);
        var draft = new SupplierPaymentEditDto
        {
            BusinessId = context.BusinessId ?? Guid.Empty,
            SupplierId = supplierId ?? Guid.Empty,
            PaymentDateUtc = DateTime.UtcNow,
            Currency = "EUR",
            MetadataJson = "{}"
        };

        if (supplierInvoiceId is { } invoiceId && invoiceId != Guid.Empty)
        {
            var invoice = await _db.Set<SupplierInvoice>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == invoiceId && !x.IsDeleted && x.Status == SupplierInvoiceStatus.Posted, ct)
                .ConfigureAwait(false);
            if (invoice is not null)
            {
                var alreadyPaid = await SupplierPaymentQuerySupport.GetPostedPaidByInvoiceAsync(_db, [invoice.Id], null, ct).ConfigureAwait(false);
                var paid = alreadyPaid.GetValueOrDefault(invoice.Id);
                draft.BusinessId = invoice.BusinessId;
                draft.SupplierId = invoice.SupplierId;
                draft.Currency = invoice.Currency;
                draft.Allocations.Add(new SupplierPaymentAllocationDto
                {
                    SupplierInvoiceId = invoice.Id,
                    SupplierInvoiceNumber = invoice.SupplierInvoiceNumber,
                    InternalInvoiceNumber = invoice.InternalInvoiceNumber ?? string.Empty,
                    DueDateUtc = invoice.DueDateUtc,
                    InvoiceGrossMinor = invoice.TotalGrossMinor,
                    AlreadyPaidMinor = paid,
                    OpenAmountMinor = Math.Max(0, invoice.TotalGrossMinor - paid),
                    AmountMinor = Math.Max(0, invoice.TotalGrossMinor - paid)
                });
            }
        }

        return draft;
    }
}

internal static class SupplierPaymentQuerySupport
{
    public static async Task<Dictionary<Guid, long>> GetPostedPaidByInvoiceAsync(IAppDbContext db, IReadOnlyCollection<Guid> invoiceIds, Guid? excludingPaymentId, CancellationToken ct)
    {
        if (invoiceIds.Count == 0) return new Dictionary<Guid, long>();
        return await db.Set<SupplierPaymentAllocation>()
            .AsNoTracking()
            .Where(allocation =>
                invoiceIds.Contains(allocation.SupplierInvoiceId) &&
                !allocation.IsDeleted &&
                db.Set<SupplierPayment>().Any(payment =>
                    payment.Id == allocation.SupplierPaymentId &&
                    payment.Status == SupplierPaymentStatus.Posted &&
                    !payment.IsDeleted &&
                    (!excludingPaymentId.HasValue || payment.Id != excludingPaymentId.Value)))
            .GroupBy(x => x.SupplierInvoiceId)
            .Select(x => new { SupplierInvoiceId = x.Key, AmountMinor = x.Sum(a => a.AmountMinor) })
            .ToDictionaryAsync(x => x.SupplierInvoiceId, x => x.AmountMinor, ct)
            .ConfigureAwait(false);
    }
}
