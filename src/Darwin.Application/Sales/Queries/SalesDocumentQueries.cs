using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Billing.Queries;
using Darwin.Application.Sales.DTOs;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Sales;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Sales.Queries
{
    public sealed class GetSalesOverviewHandler
    {
        private readonly IAppDbContext _db;

        public GetSalesOverviewHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<SalesOverviewDto> HandleAsync(CancellationToken ct = default)
        {
            var orders = _db.Set<Order>().AsNoTracking().Where(x => !x.IsDeleted);
            var invoices = _db.Set<Invoice>().AsNoTracking().Where(x => !x.IsDeleted);
            var quotes = _db.Set<SalesQuote>().AsNoTracking().Where(x => !x.IsDeleted);
            var returnOrders = _db.Set<ReturnOrder>().AsNoTracking().Where(x => !x.IsDeleted);

            var orderRows = await orders
                .Select(x => new
                {
                    x.SalesChannel,
                    x.Currency,
                    x.GrandTotalGrossMinor,
                    x.TaxTotalMinor,
                    x.Status,
                    FailedPaymentCount = x.Payments.Count(payment => !payment.IsDeleted && payment.Status == PaymentStatus.Failed),
                    ShipmentCount = x.Shipments.Count(shipment => !shipment.IsDeleted)
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var invoiceRows = await invoices
                .Select(x => new SalesOverviewInvoiceRow
                {
                    Status = x.Status,
                    Currency = x.Currency,
                    TotalGrossMinor = x.TotalGrossMinor,
                    PaymentId = x.PaymentId,
                    DueDateUtc = x.DueDateUtc,
                    ArchiveGeneratedAtUtc = x.ArchiveGeneratedAtUtc,
                    IssuedSnapshotJson = x.IssuedSnapshotJson
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            await PopulateInvoiceBalancesAsync(invoiceRows, ct).ConfigureAwait(false);

            var currency = orderRows.Select(x => x.Currency)
                .Concat(invoiceRows.Select(x => x.Currency))
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;

            return new SalesOverviewDto
            {
                OrderCount = orderRows.Count,
                QuoteCount = await quotes.CountAsync(ct).ConfigureAwait(false),
                QuoteAttentionCount = await quotes.CountAsync(x =>
                    x.Status == SalesQuoteStatus.Sent &&
                    x.ValidUntilUtc.HasValue &&
                    x.ValidUntilUtc.Value <= DateTime.UtcNow.AddDays(14), ct).ConfigureAwait(false),
                ReturnOrderCount = await returnOrders.CountAsync(ct).ConfigureAwait(false),
                ReturnOrderAttentionCount = await returnOrders.CountAsync(x =>
                    x.Status == ReturnOrderStatus.Requested ||
                    x.Status == ReturnOrderStatus.Received ||
                    x.Status == ReturnOrderStatus.Inspected ||
                    x.Status == ReturnOrderStatus.RefundReady, ct).ConfigureAwait(false),
                GrossTotalMinor = orderRows.Sum(x => x.GrandTotalGrossMinor),
                TaxTotalMinor = orderRows.Sum(x => x.TaxTotalMinor),
                InvoiceCount = invoiceRows.Count,
                OpenInvoiceBalanceMinor = invoiceRows
                    .Where(x => x.Status != InvoiceStatus.Paid && x.Status != InvoiceStatus.Cancelled)
                    .Sum(x => x.BalanceMinor),
                PaymentAttentionCount = orderRows.Count(x => x.FailedPaymentCount > 0),
                FulfillmentAttentionCount = orderRows.Count(x => x.Status == OrderStatus.Paid && x.ShipmentCount == 0),
                InvoiceAttentionCount = invoiceRows.Count(x =>
                    x.Status != InvoiceStatus.Paid &&
                    x.Status != InvoiceStatus.Cancelled &&
                    (x.DueDateUtc.Date < DateTime.UtcNow.Date ||
                     string.IsNullOrWhiteSpace(x.IssuedSnapshotJson) ||
                     !x.ArchiveGeneratedAtUtc.HasValue)),
                Currency = currency.Trim().ToUpperInvariant(),
                ChannelBreakdown = orderRows
                    .GroupBy(x => x.SalesChannel)
                    .OrderBy(x => x.Key)
                    .Select(x => new SalesChannelBreakdownDto
                    {
                        SalesChannel = x.Key,
                        OrderCount = x.Count(),
                        GrossTotalMinor = x.Sum(row => row.GrandTotalGrossMinor)
                    })
                    .ToList()
            };
        }

        private async Task PopulateInvoiceBalancesAsync(List<SalesOverviewInvoiceRow> items, CancellationToken ct)
        {
            var paymentIds = items.Where(x => x.PaymentId.HasValue).Select(x => x.PaymentId!.Value).Distinct().ToList();
            var payments = paymentIds.Count == 0
                ? new Dictionary<Guid, Payment>()
                : await _db.Set<Payment>()
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted && paymentIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, ct)
                    .ConfigureAwait(false);
            var refundTotals = paymentIds.Count == 0
                ? new Dictionary<Guid, long>()
                : await _db.Set<Refund>()
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted && x.Status == RefundStatus.Completed && paymentIds.Contains(x.PaymentId))
                    .GroupBy(x => x.PaymentId)
                    .Select(x => new { PaymentId = x.Key, AmountMinor = x.Sum(r => r.AmountMinor) })
                    .ToDictionaryAsync(x => x.PaymentId, x => x.AmountMinor, ct)
                    .ConfigureAwait(false);

            foreach (var item in items)
            {
                if (item.PaymentId.HasValue && payments.TryGetValue(item.PaymentId.Value, out var payment))
                {
                    var refundedAmountMinor = BillingReconciliationCalculator.ClampRefundedAmount(
                        payment.AmountMinor,
                        refundTotals.TryGetValue(payment.Id, out var paymentRefundedAmountMinor) ? paymentRefundedAmountMinor : 0L);
                    var settledAmountMinor = BillingReconciliationCalculator.CalculateSettledAmount(
                        item.TotalGrossMinor,
                        BillingReconciliationCalculator.CalculateNetCollectedAmount(payment.AmountMinor, refundedAmountMinor));
                    item.BalanceMinor = BillingReconciliationCalculator.CalculateBalanceAmount(item.TotalGrossMinor, settledAmountMinor);
                }
                else
                {
                    var settledAmountMinor = item.Status == InvoiceStatus.Paid ? item.TotalGrossMinor : 0L;
                    item.BalanceMinor = BillingReconciliationCalculator.CalculateBalanceAmount(item.TotalGrossMinor, settledAmountMinor);
                }
            }
        }

        private sealed class SalesOverviewInvoiceRow
        {
            public InvoiceStatus Status { get; set; }
            public string Currency { get; set; } = string.Empty;
            public long TotalGrossMinor { get; set; }
            public Guid? PaymentId { get; set; }
            public DateTime DueDateUtc { get; set; }
            public DateTime? ArchiveGeneratedAtUtc { get; set; }
            public string? IssuedSnapshotJson { get; set; }
            public long BalanceMinor { get; set; }
        }
    }

    public sealed class GetSalesOrdersPageHandler
    {
        private const int MaxPageSize = 200;
        private readonly IAppDbContext _db;

        public GetSalesOrdersPageHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<(List<SalesOrderListItemDto> Items, int Total)> HandleAsync(
            int page,
            int pageSize,
            string? query = null,
            SalesOrderDocumentFilter filter = SalesOrderDocumentFilter.All,
            SalesChannel? salesChannel = null,
            DateTime? orderedFromUtc = null,
            DateTime? orderedToUtc = null,
            Guid? businessId = null,
            Guid? customerId = null,
            CancellationToken ct = default)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            var baseQuery = _db.Set<Order>().AsNoTracking().Where(x => !x.IsDeleted);
            var q = query?.Trim();
            if (!string.IsNullOrWhiteSpace(q))
            {
                baseQuery = baseQuery.Where(x => x.OrderNumber.Contains(q));
            }

            if (salesChannel.HasValue)
            {
                baseQuery = baseQuery.Where(x => x.SalesChannel == salesChannel.Value);
            }

            if (orderedFromUtc.HasValue)
            {
                baseQuery = baseQuery.Where(x => x.OrderedAtUtc >= orderedFromUtc.Value);
            }

            if (orderedToUtc.HasValue)
            {
                baseQuery = baseQuery.Where(x => x.OrderedAtUtc <= orderedToUtc.Value);
            }

            if (businessId.HasValue && businessId.Value != Guid.Empty)
            {
                baseQuery = baseQuery.Where(x => x.BusinessId == businessId.Value);
            }

            if (customerId.HasValue && customerId.Value != Guid.Empty)
            {
                baseQuery = baseQuery.Where(x => x.CustomerId == customerId.Value);
            }

            baseQuery = filter switch
            {
                SalesOrderDocumentFilter.Open => baseQuery.Where(x => x.Status != OrderStatus.Completed && x.Status != OrderStatus.Cancelled),
                SalesOrderDocumentFilter.Paid => baseQuery.Where(x => x.Status == OrderStatus.Paid),
                SalesOrderDocumentFilter.FulfillmentAttention => baseQuery.Where(x => x.Status == OrderStatus.Paid && x.Shipments.Count(shipment => !shipment.IsDeleted) == 0),
                SalesOrderDocumentFilter.Completed => baseQuery.Where(x => x.Status == OrderStatus.Completed),
                SalesOrderDocumentFilter.Cancelled => baseQuery.Where(x => x.Status == OrderStatus.Cancelled),
                _ => baseQuery
            };

            var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);
            var items = await baseQuery
                .OrderByDescending(x => x.OrderedAtUtc)
                .ThenByDescending(x => x.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new SalesOrderListItemDto
                {
                    Id = x.Id,
                    OrderNumber = x.OrderNumber,
                    BusinessId = x.BusinessId,
                    CustomerId = x.CustomerId,
                    SalesChannel = x.SalesChannel,
                    OrderedAtUtc = x.OrderedAtUtc,
                    Status = x.Status,
                    Currency = x.Currency,
                    GrandTotalGrossMinor = x.GrandTotalGrossMinor,
                    TaxTotalMinor = x.TaxTotalMinor,
                    LineCount = x.Lines.Count(line => !line.IsDeleted),
                    PaymentCount = x.Payments.Count(payment => !payment.IsDeleted),
                    FailedPaymentCount = x.Payments.Count(payment => !payment.IsDeleted && payment.Status == PaymentStatus.Failed),
                    ShipmentCount = x.Shipments.Count(shipment => !shipment.IsDeleted),
                    InvoiceCount = _db.Set<Invoice>().Count(invoice => !invoice.IsDeleted && invoice.OrderId == x.Id)
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return (items, total);
        }
    }

    public sealed class GetSalesInvoicesPageHandler
    {
        private const int MaxPageSize = 200;
        private readonly IAppDbContext _db;

        public GetSalesInvoicesPageHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<(List<SalesInvoiceListItemDto> Items, int Total)> HandleAsync(
            int page,
            int pageSize,
            string? query = null,
            SalesInvoiceDocumentFilter filter = SalesInvoiceDocumentFilter.All,
            DateTime? dateFromUtc = null,
            DateTime? dateToUtc = null,
            Guid? businessId = null,
            Guid? customerId = null,
            CancellationToken ct = default)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
            var todayUtc = DateTime.UtcNow.Date;
            var dueSoonUtc = todayUtc.AddDays(7);

            var baseQuery = _db.Set<Invoice>().AsNoTracking().Where(x => !x.IsDeleted);
            var q = query?.Trim();
            if (!string.IsNullOrWhiteSpace(q))
            {
                baseQuery = baseQuery.Where(x =>
                    (x.InvoiceNumber != null && x.InvoiceNumber.Contains(q)) ||
                    (x.OrderId.HasValue && _db.Set<Order>().Any(order => !order.IsDeleted && order.Id == x.OrderId.Value && order.OrderNumber.Contains(q))));
            }

            if (dateFromUtc.HasValue)
            {
                baseQuery = baseQuery.Where(x => (x.IssuedAtUtc ?? x.DueDateUtc) >= dateFromUtc.Value);
            }

            if (dateToUtc.HasValue)
            {
                baseQuery = baseQuery.Where(x => (x.IssuedAtUtc ?? x.DueDateUtc) <= dateToUtc.Value);
            }

            if (businessId.HasValue && businessId.Value != Guid.Empty)
            {
                baseQuery = baseQuery.Where(x => x.BusinessId == businessId.Value);
            }

            if (customerId.HasValue && customerId.Value != Guid.Empty)
            {
                baseQuery = baseQuery.Where(x => x.CustomerId == customerId.Value);
            }

            baseQuery = filter switch
            {
                SalesInvoiceDocumentFilter.Draft => baseQuery.Where(x => x.Status == InvoiceStatus.Draft),
                SalesInvoiceDocumentFilter.Open => baseQuery.Where(x => x.Status == InvoiceStatus.Open),
                SalesInvoiceDocumentFilter.DueSoon => baseQuery.Where(x => x.Status != InvoiceStatus.Paid && x.Status != InvoiceStatus.Cancelled && x.DueDateUtc >= todayUtc && x.DueDateUtc <= dueSoonUtc),
                SalesInvoiceDocumentFilter.Overdue => baseQuery.Where(x => x.Status != InvoiceStatus.Paid && x.Status != InvoiceStatus.Cancelled && x.DueDateUtc < todayUtc),
                SalesInvoiceDocumentFilter.Paid => baseQuery.Where(x => x.Status == InvoiceStatus.Paid),
                SalesInvoiceDocumentFilter.Archived => baseQuery.Where(x => x.ArchiveGeneratedAtUtc.HasValue),
                _ => baseQuery
            };

            var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);
            var items = await baseQuery
                .OrderByDescending(x => x.IssuedAtUtc ?? x.CreatedAtUtc)
                .ThenByDescending(x => x.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new SalesInvoiceListItemDto
                {
                    Id = x.Id,
                    InvoiceNumber = x.InvoiceNumber,
                    BusinessId = x.BusinessId,
                    CustomerId = x.CustomerId,
                    OrderId = x.OrderId,
                    PaymentId = x.PaymentId,
                    OrderNumber = x.OrderId.HasValue
                        ? _db.Set<Order>().Where(order => !order.IsDeleted && order.Id == x.OrderId.Value).Select(order => order.OrderNumber).FirstOrDefault()
                        : null,
                    Status = x.Status,
                    Currency = x.Currency,
                    TotalNetMinor = x.TotalNetMinor,
                    TotalTaxMinor = x.TotalTaxMinor,
                    TotalGrossMinor = x.TotalGrossMinor,
                    DueDateUtc = x.DueDateUtc,
                    IssuedAtUtc = x.IssuedAtUtc,
                    HasIssuedSnapshot = !string.IsNullOrWhiteSpace(x.IssuedSnapshotJson),
                    HasArchiveMetadata = x.ArchiveGeneratedAtUtc.HasValue
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            await PopulateInvoiceBalancesAsync(items, ct).ConfigureAwait(false);

            return (items, total);
        }

        private async Task PopulateInvoiceBalancesAsync(List<SalesInvoiceListItemDto> items, CancellationToken ct)
        {
            var paymentIds = items.Where(x => x.PaymentId.HasValue).Select(x => x.PaymentId!.Value).Distinct().ToList();
            var payments = paymentIds.Count == 0
                ? new Dictionary<Guid, Payment>()
                : await _db.Set<Payment>()
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted && paymentIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, ct)
                    .ConfigureAwait(false);
            var refundTotals = paymentIds.Count == 0
                ? new Dictionary<Guid, long>()
                : await _db.Set<Refund>()
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted && x.Status == RefundStatus.Completed && paymentIds.Contains(x.PaymentId))
                    .GroupBy(x => x.PaymentId)
                    .Select(x => new { PaymentId = x.Key, AmountMinor = x.Sum(r => r.AmountMinor) })
                    .ToDictionaryAsync(x => x.PaymentId, x => x.AmountMinor, ct)
                    .ConfigureAwait(false);

            foreach (var item in items)
            {
                if (item.PaymentId.HasValue && payments.TryGetValue(item.PaymentId.Value, out var payment))
                {
                    var refundedAmountMinor = BillingReconciliationCalculator.ClampRefundedAmount(
                        payment.AmountMinor,
                        refundTotals.TryGetValue(payment.Id, out var paymentRefundedAmountMinor) ? paymentRefundedAmountMinor : 0L);
                    var settledAmountMinor = BillingReconciliationCalculator.CalculateSettledAmount(
                        item.TotalGrossMinor,
                        BillingReconciliationCalculator.CalculateNetCollectedAmount(payment.AmountMinor, refundedAmountMinor));
                    item.BalanceMinor = BillingReconciliationCalculator.CalculateBalanceAmount(item.TotalGrossMinor, settledAmountMinor);
                }
                else
                {
                    var settledAmountMinor = item.Status == InvoiceStatus.Paid ? item.TotalGrossMinor : 0L;
                    item.BalanceMinor = BillingReconciliationCalculator.CalculateBalanceAmount(item.TotalGrossMinor, settledAmountMinor);
                }
            }
        }
    }

    /// <summary>
    /// Builds an internal sales order document projection from the current order aggregate.
    /// </summary>
    public sealed class GetSalesOrderDocumentHandler
    {
        private readonly IAppDbContext _db;

        public GetSalesOrderDocumentHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<SalesOrderDocumentDto?> HandleAsync(Guid orderId, CancellationToken ct = default)
        {
            if (orderId == Guid.Empty)
            {
                return null;
            }

            var order = await _db.Set<Order>()
                .AsNoTracking()
                .Include(x => x.Lines)
                .Include(x => x.Payments)
                .Include(x => x.Shipments)
                .FirstOrDefaultAsync(x => x.Id == orderId && !x.IsDeleted, ct)
                .ConfigureAwait(false);

            if (order is null)
            {
                return null;
            }

            var invoices = await _db.Set<Invoice>()
                .AsNoTracking()
                .Where(x => x.OrderId == order.Id && !x.IsDeleted)
                .OrderByDescending(x => x.IssuedAtUtc ?? x.CreatedAtUtc)
                .ThenBy(x => x.Id)
                .Select(x => new SalesInvoiceDocumentSummaryDto
                {
                    Id = x.Id,
                    PaymentId = x.PaymentId,
                    InvoiceNumber = x.InvoiceNumber,
                    Status = x.Status,
                    Currency = x.Currency,
                    TotalNetMinor = x.TotalNetMinor,
                    TotalTaxMinor = x.TotalTaxMinor,
                    TotalGrossMinor = x.TotalGrossMinor,
                    DueDateUtc = x.DueDateUtc,
                    PaidAtUtc = x.PaidAtUtc,
                    IssuedAtUtc = x.IssuedAtUtc,
                    HasIssuedSnapshot = !string.IsNullOrWhiteSpace(x.IssuedSnapshotJson),
                    HasArchiveMetadata = x.ArchiveGeneratedAtUtc.HasValue
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return new SalesOrderDocumentDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                UserId = order.UserId,
                BusinessId = order.BusinessId,
                CustomerId = order.CustomerId,
                Currency = order.Currency,
                PricesIncludeTax = order.PricesIncludeTax,
                SalesChannel = order.SalesChannel,
                OrderedAtUtc = order.OrderedAtUtc,
                SubtotalNetMinor = order.SubtotalNetMinor,
                TaxTotalMinor = order.TaxTotalMinor,
                ShippingTotalMinor = order.ShippingTotalMinor,
                DiscountTotalMinor = order.DiscountTotalMinor,
                GrandTotalGrossMinor = order.GrandTotalGrossMinor,
                Status = order.Status,
                BillingAddressJson = order.BillingAddressJson,
                ShippingAddressJson = order.ShippingAddressJson,
                ShippingMethodId = order.ShippingMethodId,
                ShippingMethodName = order.ShippingMethodName,
                ShippingCarrier = order.ShippingCarrier,
                ShippingService = order.ShippingService,
                InternalNotes = order.InternalNotes,
                Lines = order.Lines
                    .Where(x => !x.IsDeleted)
                    .OrderBy(x => x.CreatedAtUtc)
                    .ThenBy(x => x.Id)
                    .Select(x => new SalesOrderLineDocumentDto
                    {
                        Id = x.Id,
                        VariantId = x.VariantId,
                        WarehouseId = x.WarehouseId,
                        Name = x.Name,
                        Sku = x.Sku,
                        Quantity = x.Quantity,
                        UnitPriceNetMinor = x.UnitPriceNetMinor,
                        VatRate = x.VatRate,
                        UnitPriceGrossMinor = x.UnitPriceGrossMinor,
                        LineTaxMinor = x.LineTaxMinor,
                        LineGrossMinor = x.LineGrossMinor,
                        AddOnValueIdsJson = x.AddOnValueIdsJson,
                        AddOnPriceDeltaMinor = x.AddOnPriceDeltaMinor
                    })
                    .ToList(),
                Settlements = order.Payments
                    .Where(x => !x.IsDeleted)
                    .OrderByDescending(x => x.PaidAtUtc ?? x.CreatedAtUtc)
                    .ThenBy(x => x.Id)
                    .Select(x => new SalesDocumentSettlementDto
                    {
                        Id = x.Id,
                        InvoiceId = x.InvoiceId,
                        CustomerId = x.CustomerId,
                        UserId = x.UserId,
                        AmountMinor = x.AmountMinor,
                        Currency = x.Currency,
                        Status = x.Status,
                        Provider = x.Provider,
                        ProviderTransactionReference = x.ProviderTransactionRef,
                        ProviderPaymentIntentReference = x.ProviderPaymentIntentRef,
                        ProviderCheckoutSessionReference = x.ProviderCheckoutSessionRef,
                        PaidAtUtc = x.PaidAtUtc,
                        FailureReason = x.FailureReason
                    })
                    .ToList(),
                Fulfillments = order.Shipments
                    .Where(x => !x.IsDeleted)
                    .OrderByDescending(x => x.ShippedAtUtc ?? x.CreatedAtUtc)
                    .ThenBy(x => x.Id)
                    .Select(x => new SalesDocumentFulfillmentDto
                    {
                        Id = x.Id,
                        MethodId = x.MethodId,
                        Status = x.Status,
                        Carrier = x.Carrier,
                        Service = x.Service,
                        ProviderShipmentReference = x.ProviderShipmentReference,
                        TrackingNumber = x.TrackingNumber,
                        LabelUrl = x.LabelUrl,
                        TotalWeight = x.TotalWeight,
                        ShippedAtUtc = x.ShippedAtUtc,
                        DeliveredAtUtc = x.DeliveredAtUtc
                    })
                    .ToList(),
                Invoices = invoices
            };
        }
    }

    /// <summary>
    /// Builds an internal sales invoice document projection from the current shared invoice aggregate.
    /// </summary>
    public sealed class GetSalesInvoiceDocumentHandler
    {
        private readonly IAppDbContext _db;

        public GetSalesInvoiceDocumentHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<SalesInvoiceDocumentDto?> HandleAsync(Guid invoiceId, CancellationToken ct = default)
        {
            if (invoiceId == Guid.Empty)
            {
                return null;
            }

            var invoice = await _db.Set<Invoice>()
                .AsNoTracking()
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == invoiceId && !x.IsDeleted, ct)
                .ConfigureAwait(false);

            if (invoice is null)
            {
                return null;
            }

            string? orderNumber = null;
            if (invoice.OrderId.HasValue)
            {
                orderNumber = await _db.Set<Order>()
                    .AsNoTracking()
                    .Where(x => x.Id == invoice.OrderId.Value && !x.IsDeleted)
                    .Select(x => x.OrderNumber)
                    .FirstOrDefaultAsync(ct)
                    .ConfigureAwait(false);
            }

            return new SalesInvoiceDocumentDto
            {
                Id = invoice.Id,
                BusinessId = invoice.BusinessId,
                CustomerId = invoice.CustomerId,
                OrderId = invoice.OrderId,
                OrderNumber = orderNumber,
                PaymentId = invoice.PaymentId,
                InvoiceNumber = invoice.InvoiceNumber,
                Status = invoice.Status,
                Currency = invoice.Currency,
                TotalNetMinor = invoice.TotalNetMinor,
                TotalTaxMinor = invoice.TotalTaxMinor,
                TotalGrossMinor = invoice.TotalGrossMinor,
                DueDateUtc = invoice.DueDateUtc,
                PaidAtUtc = invoice.PaidAtUtc,
                IssuedAtUtc = invoice.IssuedAtUtc,
                ReverseChargeApplied = invoice.ReverseChargeApplied,
                ReverseChargeReviewedAtUtc = invoice.ReverseChargeReviewedAtUtc,
                ReverseChargeReviewNote = invoice.ReverseChargeReviewNote,
                IssuedSnapshotJson = invoice.IssuedSnapshotJson,
                IssuedSnapshotHashSha256 = invoice.IssuedSnapshotHashSha256,
                ArchiveGeneratedAtUtc = invoice.ArchiveGeneratedAtUtc,
                ArchiveRetainUntilUtc = invoice.ArchiveRetainUntilUtc,
                ArchiveRetentionPolicyVersion = invoice.ArchiveRetentionPolicyVersion,
                ArchivePurgedAtUtc = invoice.ArchivePurgedAtUtc,
                ArchivePurgeReason = invoice.ArchivePurgeReason,
                Lines = invoice.Lines
                    .Where(x => !x.IsDeleted)
                    .OrderBy(x => x.CreatedAtUtc)
                    .ThenBy(x => x.Id)
                    .Select(x => new SalesInvoiceLineDocumentDto
                    {
                        Id = x.Id,
                        Description = x.Description,
                        Quantity = x.Quantity,
                        UnitPriceNetMinor = x.UnitPriceNetMinor,
                        TaxRate = x.TaxRate,
                        TotalNetMinor = x.TotalNetMinor,
                        TotalTaxMinor = x.TotalTaxMinor,
                        TotalGrossMinor = x.TotalGrossMinor
                    })
                    .ToList()
            };
        }
    }
}
