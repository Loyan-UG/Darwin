using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Billing.Queries;
using Darwin.Application.CRM.DTOs;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CRM.Queries;

/// <summary>
/// Returns a paged list of invoices owned by the current authenticated member.
/// </summary>
public sealed class GetMyInvoicesPageHandler
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetMyInvoicesPageHandler"/> class.
    /// </summary>
    public GetMyInvoicesPageHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    /// <summary>
    /// Returns the current member invoice history page.
    /// </summary>
    public async Task<(List<MemberInvoiceSummaryDto> Items, int Total)> HandleAsync(int page, int pageSize, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var userId = _currentUser.GetCurrentUserId();
        var baseQuery = BuildMemberInvoiceScope(userId);

        var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);
        var items = await baseQuery
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new MemberInvoiceSummaryDto
            {
                Id = x.Id,
                BusinessId = x.BusinessId,
                OrderId = x.OrderId,
                Currency = x.Currency,
                TotalGrossMinor = x.TotalGrossMinor,
                Status = x.Status,
                DueDateUtc = x.DueDateUtc,
                PaidAtUtc = x.PaidAtUtc,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        await EnrichInvoicesAsync(items, ct).ConfigureAwait(false);
        return (items, total);
    }

    private IQueryable<Invoice> BuildMemberInvoiceScope(Guid userId)
    {
        return _db.Set<Invoice>()
            .AsNoTracking()
            .Where(invoice =>
                (invoice.OrderId.HasValue && _db.Set<Order>().Any(order => order.Id == invoice.OrderId.Value && order.UserId == userId)) ||
                (invoice.CustomerId.HasValue && _db.Set<Customer>().Any(customer => customer.Id == invoice.CustomerId.Value && customer.UserId == userId)));
    }

    private async Task EnrichInvoicesAsync(List<MemberInvoiceSummaryDto> items, CancellationToken ct)
    {
        if (items.Count == 0)
        {
            return;
        }

        var businessIds = items.Where(x => x.BusinessId.HasValue).Select(x => x.BusinessId!.Value).Distinct().ToList();
        var orderIds = items.Where(x => x.OrderId.HasValue).Select(x => x.OrderId!.Value).Distinct().ToList();
        var invoiceIds = items.Select(x => x.Id).Distinct().ToList();

        var businesses = businessIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.Set<Business>().AsNoTracking().Where(x => businessIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name, ct).ConfigureAwait(false);
        var orders = orderIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.Set<Order>().AsNoTracking().Where(x => orderIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.OrderNumber, ct).ConfigureAwait(false);
        var paymentData = await LoadPaymentDataAsync(invoiceIds, ct).ConfigureAwait(false);

        foreach (var item in items)
        {
            if (item.BusinessId.HasValue && businesses.TryGetValue(item.BusinessId.Value, out var businessName))
            {
                item.BusinessName = businessName;
            }

            if (item.OrderId.HasValue && orders.TryGetValue(item.OrderId.Value, out var orderNumber))
            {
                item.OrderNumber = orderNumber;
            }

            if (paymentData.TryGetValue(item.Id, out var paymentSnapshot))
            {
                item.RefundedAmountMinor = paymentSnapshot.RefundedAmountMinor;
                item.SettledAmountMinor = paymentSnapshot.SettledAmountMinor;
                item.BalanceMinor = paymentSnapshot.BalanceMinor;
            }
            else
            {
                item.RefundedAmountMinor = 0L;
                item.SettledAmountMinor = item.Status == InvoiceStatus.Paid ? item.TotalGrossMinor : 0L;
                item.BalanceMinor = BillingReconciliationCalculator.CalculateBalanceAmount(item.TotalGrossMinor, item.SettledAmountMinor);
            }
        }
    }

    private async Task<Dictionary<Guid, MemberInvoicePaymentSnapshot>> LoadPaymentDataAsync(List<Guid> invoiceIds, CancellationToken ct)
    {
        var invoicePayments = await _db.Set<Invoice>()
            .AsNoTracking()
            .Where(x => invoiceIds.Contains(x.Id) && x.PaymentId.HasValue)
            .Select(x => new { x.Id, PaymentId = x.PaymentId!.Value, x.TotalGrossMinor })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var paymentIds = invoicePayments.Select(x => x.PaymentId).Distinct().ToList();
        if (paymentIds.Count == 0)
        {
            return new Dictionary<Guid, MemberInvoicePaymentSnapshot>();
        }

        var payments = await _db.Set<Payment>()
            .AsNoTracking()
            .Where(x => paymentIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct)
            .ConfigureAwait(false);
        var refundTotals = await _db.Set<Refund>()
            .AsNoTracking()
            .Where(x => x.Status == RefundStatus.Completed && paymentIds.Contains(x.PaymentId))
            .GroupBy(x => x.PaymentId)
            .Select(x => new { PaymentId = x.Key, AmountMinor = x.Sum(r => r.AmountMinor) })
            .ToDictionaryAsync(x => x.PaymentId, x => x.AmountMinor, ct)
            .ConfigureAwait(false);

        var snapshots = new Dictionary<Guid, MemberInvoicePaymentSnapshot>();
        foreach (var invoicePayment in invoicePayments)
        {
            if (!payments.TryGetValue(invoicePayment.PaymentId, out var payment))
            {
                continue;
            }

            var refundedAmountMinor = BillingReconciliationCalculator.ClampRefundedAmount(
                payment.AmountMinor,
                refundTotals.TryGetValue(payment.Id, out var paymentRefundedAmountMinor) ? paymentRefundedAmountMinor : 0L);
            var settledAmountMinor = BillingReconciliationCalculator.CalculateSettledAmount(
                invoicePayment.TotalGrossMinor,
                BillingReconciliationCalculator.CalculateNetCollectedAmount(payment.AmountMinor, refundedAmountMinor));

            snapshots[invoicePayment.Id] = new MemberInvoicePaymentSnapshot
            {
                RefundedAmountMinor = Math.Min(refundedAmountMinor, invoicePayment.TotalGrossMinor),
                SettledAmountMinor = settledAmountMinor,
                BalanceMinor = BillingReconciliationCalculator.CalculateBalanceAmount(invoicePayment.TotalGrossMinor, settledAmountMinor)
            };
        }

        return snapshots;
    }

    private sealed class MemberInvoicePaymentSnapshot
    {
        public long RefundedAmountMinor { get; init; }
        public long SettledAmountMinor { get; init; }
        public long BalanceMinor { get; init; }
    }
}

/// <summary>
/// Returns a detailed invoice view owned by the current authenticated member.
/// </summary>
public sealed class GetMyInvoiceDetailHandler
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetMyInvoiceDetailHandler"/> class.
    /// </summary>
    public GetMyInvoiceDetailHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    /// <summary>
    /// Returns the member-owned invoice detail or <c>null</c> when inaccessible.
    /// </summary>
    public async Task<MemberInvoiceDetailDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        var userId = _currentUser.GetCurrentUserId();

        var invoice = await _db.Set<Invoice>()
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Where(x =>
                (x.OrderId.HasValue && _db.Set<Order>().Any(order => order.Id == x.OrderId.Value && order.UserId == userId)) ||
                (x.CustomerId.HasValue && _db.Set<Customer>().Any(customer => customer.Id == x.CustomerId.Value && customer.UserId == userId)))
            .Select(x => new MemberInvoiceDetailDto
            {
                Id = x.Id,
                BusinessId = x.BusinessId,
                OrderId = x.OrderId,
                Currency = x.Currency,
                TotalGrossMinor = x.TotalGrossMinor,
                TotalNetMinor = x.TotalNetMinor,
                TotalTaxMinor = x.TotalTaxMinor,
                Status = x.Status,
                DueDateUtc = x.DueDateUtc,
                PaidAtUtc = x.PaidAtUtc,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (invoice is null)
        {
            return null;
        }

        if (invoice.BusinessId.HasValue)
        {
            invoice.BusinessName = await _db.Set<Business>()
                .AsNoTracking()
                .Where(x => x.Id == invoice.BusinessId.Value)
                .Select(x => x.Name)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);
        }

        if (invoice.OrderId.HasValue)
        {
            invoice.OrderNumber = await _db.Set<Order>()
                .AsNoTracking()
                .Where(x => x.Id == invoice.OrderId.Value)
                .Select(x => x.OrderNumber)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);
        }

        var paymentLink = await _db.Set<Invoice>()
            .AsNoTracking()
            .Where(x => x.Id == invoice.Id)
            .Select(x => x.PaymentId)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (paymentLink.HasValue)
        {
            var payment = await _db.Set<Payment>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == paymentLink.Value, ct)
                .ConfigureAwait(false);

            if (payment is not null)
            {
                invoice.PaymentSummary = $"{payment.Provider} | {payment.Currency} {(payment.AmountMinor / 100.0M):0.00} | {payment.Status}";
                var refundedAmountMinor = BillingReconciliationCalculator.ClampRefundedAmount(
                    payment.AmountMinor,
                    await _db.Set<Refund>()
                        .AsNoTracking()
                        .Where(x => x.PaymentId == payment.Id && x.Status == RefundStatus.Completed)
                        .SumAsync(x => (long?)x.AmountMinor, ct)
                        .ConfigureAwait(false) ?? 0L);
                invoice.RefundedAmountMinor = Math.Min(refundedAmountMinor, invoice.TotalGrossMinor);
                invoice.SettledAmountMinor = BillingReconciliationCalculator.CalculateSettledAmount(
                    invoice.TotalGrossMinor,
                    BillingReconciliationCalculator.CalculateNetCollectedAmount(payment.AmountMinor, refundedAmountMinor));
                invoice.BalanceMinor = BillingReconciliationCalculator.CalculateBalanceAmount(invoice.TotalGrossMinor, invoice.SettledAmountMinor);
            }
        }

        if (string.IsNullOrWhiteSpace(invoice.PaymentSummary))
        {
            invoice.RefundedAmountMinor = 0L;
            invoice.SettledAmountMinor = invoice.Status == InvoiceStatus.Paid ? invoice.TotalGrossMinor : 0L;
            invoice.BalanceMinor = BillingReconciliationCalculator.CalculateBalanceAmount(invoice.TotalGrossMinor, invoice.SettledAmountMinor);
        }

        return invoice;
    }
}
