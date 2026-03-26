using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Billing.Queries;
using Darwin.Application.Orders.DTOs;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Orders.Queries
{
    /// <summary>
    /// Returns paged payments of an order for admin listing screens.
    /// </summary>
    public sealed class GetOrderPaymentsPageHandler
    {
        private readonly IAppDbContext _db;
        public GetOrderPaymentsPageHandler(IAppDbContext db) => _db = db;

        /// <summary>
        /// Executes a paged query over payments of a given order.
        /// </summary>
        public async Task<(List<PaymentListItemDto> Items, int Total)> HandleAsync(Guid orderId, int page, int pageSize, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var baseQuery = _db.Set<Payment>().AsNoTracking().Where(p => p.OrderId == orderId);
            var total = await baseQuery.CountAsync(ct);

            var items = await baseQuery
                .OrderByDescending(p => p.CreatedAtUtc)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(p => new PaymentListItemDto
                {
                    Id = p.Id,
                    OrderId = p.OrderId ?? Guid.Empty,
                    Provider = p.Provider,
                    InvoiceId = p.InvoiceId,
                    ProviderReference = p.ProviderTransactionRef,
                    AmountMinor = p.AmountMinor,
                    Currency = p.Currency,
                    Status = p.Status,
                    FailureReason = p.FailureReason,
                    CreatedAtUtc = p.CreatedAtUtc,
                    PaidAtUtc = p.PaidAtUtc,
                    RowVersion = p.RowVersion
                })
                .ToListAsync(ct);

            var paymentIds = items.Select(x => x.Id).ToList();
            if (paymentIds.Count > 0)
            {
                var invoiceMap = await _db.Set<Invoice>()
                    .AsNoTracking()
                    .Where(x => x.PaymentId.HasValue && paymentIds.Contains(x.PaymentId.Value))
                    .Select(x => new { PaymentId = x.PaymentId!.Value, x.Id, x.Status })
                    .ToDictionaryAsync(x => x.PaymentId, ct)
                    .ConfigureAwait(false);
                var refundTotals = await _db.Set<Refund>()
                    .AsNoTracking()
                    .Where(x => x.Status == RefundStatus.Completed && paymentIds.Contains(x.PaymentId))
                    .GroupBy(x => x.PaymentId)
                    .Select(x => new { PaymentId = x.Key, AmountMinor = x.Sum(r => r.AmountMinor) })
                    .ToDictionaryAsync(x => x.PaymentId, x => x.AmountMinor, ct)
                    .ConfigureAwait(false);

                foreach (var item in items)
                {
                    if (invoiceMap.TryGetValue(item.Id, out var invoice))
                    {
                        item.InvoiceId = invoice.Id;
                        item.InvoiceStatus = invoice.Status;
                    }

                    item.RefundedAmountMinor = BillingReconciliationCalculator.ClampRefundedAmount(
                        item.AmountMinor,
                        refundTotals.TryGetValue(item.Id, out var refundedAmountMinor) ? refundedAmountMinor : 0L);
                    item.NetCapturedAmountMinor = BillingReconciliationCalculator.CalculateNetCollectedAmount(item.AmountMinor, item.RefundedAmountMinor);
                }
            }

            return (items, total);
        }
    }
}
