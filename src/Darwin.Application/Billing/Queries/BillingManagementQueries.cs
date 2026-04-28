using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Billing.Commands;
using Darwin.Application.Billing.DTOs;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Billing.Queries
{
    public sealed class GetPaymentsPageHandler
    {
        private const int MaxPageSize = 200;

        private readonly IAppDbContext _db;

        public GetPaymentsPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<(List<PaymentListItemDto> Items, int Total)> HandleAsync(
            Guid businessId,
            int page,
            int pageSize,
            string? query = null,
            PaymentQueueFilter? filter = null,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var paymentsQuery = _db.Set<Payment>()
                .AsNoTracking()
                .Where(x => x.BusinessId == businessId && !x.IsDeleted);

            if (filter.HasValue)
            {
                paymentsQuery = filter.Value switch
                {
                    PaymentQueueFilter.Pending => paymentsQuery.Where(x =>
                        x.Status == PaymentStatus.Pending || x.Status == PaymentStatus.Authorized),
                    PaymentQueueFilter.Failed => paymentsQuery.Where(x => x.Status == PaymentStatus.Failed),
                    PaymentQueueFilter.Refunded => paymentsQuery.Where(x =>
                        x.Status == PaymentStatus.Refunded ||
                        _db.Set<Refund>().Any(r => !r.IsDeleted && r.PaymentId == x.Id && r.Status == RefundStatus.Completed)),
                    PaymentQueueFilter.Unlinked => paymentsQuery.Where(x => !x.OrderId.HasValue && !x.InvoiceId.HasValue),
                    PaymentQueueFilter.ProviderLinked => paymentsQuery.Where(x =>
                        (x.ProviderTransactionRef != null && x.ProviderTransactionRef != string.Empty) ||
                        (x.ProviderPaymentIntentRef != null && x.ProviderPaymentIntentRef != string.Empty) ||
                        (x.ProviderCheckoutSessionRef != null && x.ProviderCheckoutSessionRef != string.Empty)),
                    PaymentQueueFilter.Stripe => paymentsQuery.Where(x => x.Provider == "Stripe"),
                    PaymentQueueFilter.MissingProviderRef => paymentsQuery.Where(x =>
                        (x.ProviderTransactionRef == null || x.ProviderTransactionRef == string.Empty) &&
                        (x.ProviderPaymentIntentRef == null || x.ProviderPaymentIntentRef == string.Empty) &&
                        (x.ProviderCheckoutSessionRef == null || x.ProviderCheckoutSessionRef == string.Empty)),
                    PaymentQueueFilter.FailedStripe => paymentsQuery.Where(x => x.Provider == "Stripe" && x.Status == PaymentStatus.Failed),
                    PaymentQueueFilter.NeedsReconciliation => paymentsQuery.Where(x =>
                        x.Status == PaymentStatus.Pending ||
                        x.Status == PaymentStatus.Authorized ||
                        x.Status == PaymentStatus.Failed ||
                        (x.Provider == "Stripe" &&
                         (x.ProviderTransactionRef == null || x.ProviderTransactionRef == string.Empty) &&
                         (x.ProviderPaymentIntentRef == null || x.ProviderPaymentIntentRef == string.Empty) &&
                         (x.ProviderCheckoutSessionRef == null || x.ProviderCheckoutSessionRef == string.Empty)) ||
                        _db.Set<Refund>().Any(r => !r.IsDeleted && r.PaymentId == x.Id && r.Status == RefundStatus.Completed)),
                    PaymentQueueFilter.DisputeFollowUp => paymentsQuery.Where(x =>
                        x.Provider == "Stripe" &&
                        (x.Status == PaymentStatus.Failed ||
                         x.Status == PaymentStatus.Refunded ||
                         _db.Set<Refund>().Any(r => !r.IsDeleted && r.PaymentId == x.Id && r.Status == RefundStatus.Completed)) &&
                        (x.FailureReason == null ||
                         (!x.FailureReason.Contains("[DisputeReview:Won;") &&
                          !x.FailureReason.Contains("[DisputeReview:Lost;")))),
                    _ => paymentsQuery
                };
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.Trim().ToLowerInvariant();
                paymentsQuery = paymentsQuery.Where(x =>
                    x.Provider.ToLower().Contains(term) ||
                    x.Currency.ToLower().Contains(term) ||
                    (x.ProviderTransactionRef != null && x.ProviderTransactionRef.ToLower().Contains(term)) ||
                    (x.ProviderPaymentIntentRef != null && x.ProviderPaymentIntentRef.ToLower().Contains(term)) ||
                    (x.ProviderCheckoutSessionRef != null && x.ProviderCheckoutSessionRef.ToLower().Contains(term)) ||
                    (x.OrderId.HasValue && _db.Set<Order>().Any(o => o.Id == x.OrderId.Value && !o.IsDeleted && o.OrderNumber.ToLower().Contains(term))) ||
                    (x.CustomerId.HasValue && _db.Set<Customer>().Any(c =>
                        c.Id == x.CustomerId.Value &&
                        !c.IsDeleted &&
                        (c.FirstName.ToLower().Contains(term) ||
                         c.LastName.ToLower().Contains(term) ||
                         c.Email.ToLower().Contains(term) ||
                         (c.CompanyName != null && c.CompanyName.ToLower().Contains(term))))) ||
                    (x.UserId.HasValue && _db.Set<User>().Any(u =>
                        u.Id == x.UserId.Value &&
                        !u.IsDeleted &&
                        (u.Email.ToLower().Contains(term) ||
                         (u.FirstName != null && u.FirstName.ToLower().Contains(term)) ||
                         (u.LastName != null && u.LastName.ToLower().Contains(term))))));
            }

            var total = await paymentsQuery.CountAsync(ct).ConfigureAwait(false);

            var items = await paymentsQuery
                .OrderByDescending(x => x.PaidAtUtc ?? x.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new PaymentListItemDto
                {
                    Id = x.Id,
                    BusinessId = x.BusinessId!.Value,
                    OrderId = x.OrderId,
                    InvoiceId = x.InvoiceId,
                    CustomerId = x.CustomerId,
                    UserId = x.UserId,
                    AmountMinor = x.AmountMinor,
                    Currency = x.Currency,
                    Status = x.Status,
                    Provider = x.Provider,
                    ProviderTransactionRef = x.ProviderTransactionRef,
                    ProviderPaymentIntentRef = x.ProviderPaymentIntentRef,
                    ProviderCheckoutSessionRef = x.ProviderCheckoutSessionRef,
                    FailureReason = x.FailureReason,
                    PaidAtUtc = x.PaidAtUtc,
                    CreatedAtUtc = x.CreatedAtUtc,
                    IsStripe = x.Provider == "Stripe",
                    RowVersion = x.RowVersion
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            await EnrichPaymentsAsync(items, ct).ConfigureAwait(false);
            return (items, total);
        }

        private async Task EnrichPaymentsAsync(List<PaymentListItemDto> items, CancellationToken ct)
        {
            if (items.Count == 0)
            {
                return;
            }

            var orderIds = items.Where(x => x.OrderId.HasValue).Select(x => x.OrderId!.Value).Distinct().ToList();
            var invoiceIds = items.Where(x => x.InvoiceId.HasValue).Select(x => x.InvoiceId!.Value).Distinct().ToList();
            var paymentUserIds = items.Where(x => x.UserId.HasValue).Select(x => x.UserId!.Value).Distinct().ToList();
            var customerIds = items.Where(x => x.CustomerId.HasValue).Select(x => x.CustomerId!.Value).Distinct().ToList();

            var orderMap = orderIds.Count == 0
                ? new Dictionary<Guid, string>()
                : await _db.Set<Order>()
                    .AsNoTracking()
                    .Where(x => orderIds.Contains(x.Id) && !x.IsDeleted)
                    .ToDictionaryAsync(x => x.Id, x => x.OrderNumber, ct)
                    .ConfigureAwait(false);

            var invoiceMap = invoiceIds.Count == 0
                ? new Dictionary<Guid, Invoice>()
                : await _db.Set<Invoice>()
                    .AsNoTracking()
                    .Where(x => invoiceIds.Contains(x.Id) && !x.IsDeleted)
                    .ToDictionaryAsync(x => x.Id, ct)
                    .ConfigureAwait(false);
            var paymentIds = items.Select(x => x.Id).ToList();
            var refundTotals = paymentIds.Count == 0
                ? new Dictionary<Guid, long>()
                : await _db.Set<Refund>()
                    .AsNoTracking()
                    .Where(x => x.Status == RefundStatus.Completed && paymentIds.Contains(x.PaymentId) && !x.IsDeleted)
                    .GroupBy(x => x.PaymentId)
                    .Select(x => new { PaymentId = x.Key, AmountMinor = x.Sum(r => r.AmountMinor) })
                    .ToDictionaryAsync(x => x.PaymentId, x => x.AmountMinor, ct)
                    .ConfigureAwait(false);
            var latestRefundEvents = paymentIds.Count == 0
                ? new Dictionary<Guid, DateTime?>()
                : await _db.Set<Refund>()
                    .AsNoTracking()
                    .Where(x => paymentIds.Contains(x.PaymentId) && !x.IsDeleted)
                    .GroupBy(x => x.PaymentId)
                    .Select(x => new
                    {
                        PaymentId = x.Key,
                        LastEventAtUtc = x.Max(r => r.CompletedAtUtc ?? r.CreatedAtUtc)
                    })
                    .ToDictionaryAsync(x => x.PaymentId, x => (DateTime?)x.LastEventAtUtc, ct)
                    .ConfigureAwait(false);

            foreach (var customerId in invoiceMap.Values.Where(x => x.CustomerId.HasValue).Select(x => x.CustomerId!.Value))
            {
                if (!customerIds.Contains(customerId))
                {
                    customerIds.Add(customerId);
                }
            }

            var customers = customerIds.Count == 0
                ? new List<Customer>()
                : await _db.Set<Customer>()
                    .AsNoTracking()
                    .Where(x => customerIds.Contains(x.Id) && !x.IsDeleted)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

            var identityUserIds = paymentUserIds.ToHashSet();
            foreach (var linkedUserId in customers.Where(x => x.UserId.HasValue).Select(x => x.UserId!.Value))
            {
                identityUserIds.Add(linkedUserId);
            }

            var userMap = identityUserIds.Count == 0
                ? new Dictionary<Guid, User>()
                : await _db.Set<User>()
                    .AsNoTracking()
                    .Where(x => identityUserIds.Contains(x.Id) && !x.IsDeleted)
                    .ToDictionaryAsync(x => x.Id, ct)
                    .ConfigureAwait(false);

            var customerMap = customers.ToDictionary(x => x.Id);

            foreach (var item in items)
            {
                if (item.OrderId.HasValue && orderMap.TryGetValue(item.OrderId.Value, out var orderNumber))
                {
                    item.OrderNumber = orderNumber;
                }

                if (item.InvoiceId.HasValue && invoiceMap.TryGetValue(item.InvoiceId.Value, out var invoice))
                {
                    item.InvoiceStatus = invoice.Status;
                    item.InvoiceDueAtUtc = invoice.DueDateUtc;
                    item.InvoiceTotalGrossMinor = invoice.TotalGrossMinor;

                    if (!item.CustomerId.HasValue && invoice.CustomerId.HasValue)
                    {
                        item.CustomerId = invoice.CustomerId;
                    }
                }

                if (item.CustomerId.HasValue && customerMap.TryGetValue(item.CustomerId.Value, out var customer))
                {
                    item.CustomerDisplayName = BillingPaymentDisplayFormatter.BuildCustomerDisplayName(customer, userMap);
                    item.CustomerEmail = BillingPaymentDisplayFormatter.ResolveCustomerEmail(customer, userMap);
                }

                if (item.UserId.HasValue && userMap.TryGetValue(item.UserId.Value, out var user))
                {
                    item.UserDisplayName = BillingPaymentDisplayFormatter.BuildUserDisplayName(user);
                    item.UserEmail = user.Email;
                }

                item.RefundedAmountMinor = BillingReconciliationCalculator.ClampRefundedAmount(
                    item.AmountMinor,
                    refundTotals.TryGetValue(item.Id, out var refundedAmountMinor) ? refundedAmountMinor : 0L);
                item.NetCapturedAmountMinor = BillingReconciliationCalculator.CalculateNetCollectedAmount(item.AmountMinor, item.RefundedAmountMinor);
                item.LastFinancialEventAtUtc = BillingPaymentTimelineFormatter.ResolveLastFinancialEventAtUtc(
                    item.CreatedAtUtc,
                    item.PaidAtUtc,
                    latestRefundEvents.TryGetValue(item.Id, out var lastRefundEventAtUtc) ? lastRefundEventAtUtc : null);
                item.OpenAgeHours = BillingPaymentTimelineFormatter.CalculateOpenAgeHours(item.CreatedAtUtc, item.PaidAtUtc);
                item.ProviderReferenceState = BillingPaymentTimelineFormatter.ResolveProviderReferenceState(
                    item.ProviderTransactionRef,
                    item.ProviderPaymentIntentRef,
                    item.ProviderCheckoutSessionRef,
                    item.IsStripe,
                    item.Status,
                    item.RefundedAmountMinor);
                item.NeedsReconciliation =
                    item.Status == PaymentStatus.Failed ||
                    item.Status == PaymentStatus.Pending ||
                    item.Status == PaymentStatus.Authorized ||
                    item.RefundedAmountMinor > 0 ||
                    (item.IsStripe &&
                     string.IsNullOrWhiteSpace(item.ProviderTransactionRef) &&
                     string.IsNullOrWhiteSpace(item.ProviderPaymentIntentRef) &&
                     string.IsNullOrWhiteSpace(item.ProviderCheckoutSessionRef));
                item.NeedsDisputeFollowUp =
                    item.IsStripe &&
                    !UpdatePaymentDisputeReviewHandler.IsDisputeReviewResolved(item.FailureReason) &&
                    (item.Status == PaymentStatus.Failed ||
                     item.Status == PaymentStatus.Refunded ||
                     item.RefundedAmountMinor > 0);
                item.DisputeReviewState = UpdatePaymentDisputeReviewHandler.ResolveDisputeReviewState(item.FailureReason);
                item.NeedsSupportAttention =
                    item.NeedsReconciliation ||
                    item.NeedsDisputeFollowUp ||
                    (!item.OrderId.HasValue && !item.InvoiceId.HasValue);
            }
        }
    }

    public sealed class GetPaymentOpsSummaryHandler
    {
        private readonly IAppDbContext _db;

        public GetPaymentOpsSummaryHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<PaymentOpsSummaryDto> HandleAsync(Guid businessId, CancellationToken ct = default)
        {
            var payments = _db.Set<Payment>()
                .AsNoTracking()
                .Where(x => x.BusinessId == businessId && !x.IsDeleted);

            var paymentIds = await payments.Select(x => x.Id).ToListAsync(ct).ConfigureAwait(false);
            var refundedPaymentIds = paymentIds.Count == 0
                ? new HashSet<Guid>()
                : (await _db.Set<Refund>()
                    .AsNoTracking()
                    .Where(x => x.Status == RefundStatus.Completed && paymentIds.Contains(x.PaymentId) && !x.IsDeleted)
                    .Select(x => x.PaymentId)
                    .Distinct()
                    .ToListAsync(ct)
                    .ConfigureAwait(false))
                .ToHashSet();

            var refundedStatusIds = await payments
                .Where(x => x.Status == PaymentStatus.Refunded)
                .Select(x => x.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var refundedIds = refundedStatusIds.ToHashSet();
            refundedIds.UnionWith(refundedPaymentIds);

            var summary = new PaymentOpsSummaryDto
            {
                PendingCount = await payments.CountAsync(x => x.Status == PaymentStatus.Pending || x.Status == PaymentStatus.Authorized, ct).ConfigureAwait(false),
                FailedCount = await payments.CountAsync(x => x.Status == PaymentStatus.Failed, ct).ConfigureAwait(false),
                UnlinkedCount = await payments.CountAsync(x => !x.OrderId.HasValue && !x.InvoiceId.HasValue, ct).ConfigureAwait(false),
                ProviderLinkedCount = await payments.CountAsync(x =>
                    (x.ProviderTransactionRef != null && x.ProviderTransactionRef != string.Empty) ||
                    (x.ProviderPaymentIntentRef != null && x.ProviderPaymentIntentRef != string.Empty) ||
                    (x.ProviderCheckoutSessionRef != null && x.ProviderCheckoutSessionRef != string.Empty), ct).ConfigureAwait(false),
                RefundedCount = refundedIds.Count,
                StripeCount = await payments.CountAsync(x => x.Provider == "Stripe", ct).ConfigureAwait(false),
                MissingProviderRefCount = await payments.CountAsync(x =>
                    (x.ProviderTransactionRef == null || x.ProviderTransactionRef == string.Empty) &&
                    (x.ProviderPaymentIntentRef == null || x.ProviderPaymentIntentRef == string.Empty) &&
                    (x.ProviderCheckoutSessionRef == null || x.ProviderCheckoutSessionRef == string.Empty), ct).ConfigureAwait(false),
                FailedStripeCount = await payments.CountAsync(x => x.Provider == "Stripe" && x.Status == PaymentStatus.Failed, ct).ConfigureAwait(false),
                NeedsReconciliationCount = await payments.CountAsync(x =>
                    x.Status == PaymentStatus.Pending ||
                    x.Status == PaymentStatus.Authorized ||
                    x.Status == PaymentStatus.Failed ||
                    (x.Provider == "Stripe" &&
                     (x.ProviderTransactionRef == null || x.ProviderTransactionRef == string.Empty) &&
                     (x.ProviderPaymentIntentRef == null || x.ProviderPaymentIntentRef == string.Empty) &&
                     (x.ProviderCheckoutSessionRef == null || x.ProviderCheckoutSessionRef == string.Empty)) ||
                    _db.Set<Refund>().Any(r => !r.IsDeleted && r.PaymentId == x.Id && r.Status == RefundStatus.Completed), ct).ConfigureAwait(false),
                DisputeFollowUpCount = await payments.CountAsync(x =>
                    x.Provider == "Stripe" &&
                    (x.Status == PaymentStatus.Failed ||
                     x.Status == PaymentStatus.Refunded ||
                     _db.Set<Refund>().Any(r => !r.IsDeleted && r.PaymentId == x.Id && r.Status == RefundStatus.Completed)) &&
                    (x.FailureReason == null ||
                     (!x.FailureReason.Contains("[DisputeReview:Won;") &&
                      !x.FailureReason.Contains("[DisputeReview:Lost;"))), ct).ConfigureAwait(false)
            };
            return summary;
        }
    }

    public sealed class GetPaymentForEditHandler
    {
        private readonly IAppDbContext _db;

        public GetPaymentForEditHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public Task<PaymentEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            return HandleInternalAsync(id, ct);
        }

        private async Task<PaymentEditDto?> HandleInternalAsync(Guid id, CancellationToken ct)
        {
            var dto = await _db.Set<Payment>()
                .AsNoTracking()
                .Where(x => x.Id == id && !x.IsDeleted)
                .Select(x => new PaymentEditDto
                {
                    Id = x.Id,
                    RowVersion = x.RowVersion,
                    BusinessId = x.BusinessId!.Value,
                    OrderId = x.OrderId,
                    InvoiceId = x.InvoiceId,
                    CustomerId = x.CustomerId,
                    UserId = x.UserId,
                    AmountMinor = x.AmountMinor,
                    Currency = x.Currency,
                    Status = x.Status,
                    Provider = x.Provider,
                    ProviderTransactionRef = x.ProviderTransactionRef,
                    ProviderPaymentIntentRef = x.ProviderPaymentIntentRef,
                    ProviderCheckoutSessionRef = x.ProviderCheckoutSessionRef,
                    PaidAtUtc = x.PaidAtUtc,
                    FailureReason = x.FailureReason,
                    CreatedAtUtc = x.CreatedAtUtc,
                    IsStripe = x.Provider == "Stripe"
                })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            if (dto is null)
            {
                return null;
            }

            if (dto.OrderId.HasValue)
            {
                dto.OrderNumber = await _db.Set<Order>()
                    .AsNoTracking()
                    .Where(x => x.Id == dto.OrderId.Value && !x.IsDeleted)
                    .Select(x => x.OrderNumber)
                    .FirstOrDefaultAsync(ct)
                    .ConfigureAwait(false);
            }

            if (dto.InvoiceId.HasValue)
            {
                var invoice = await _db.Set<Invoice>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == dto.InvoiceId.Value && !x.IsDeleted, ct)
                    .ConfigureAwait(false);

                if (invoice is not null)
                {
                    dto.InvoiceStatus = invoice.Status;
                    dto.InvoiceDueAtUtc = invoice.DueDateUtc;
                    dto.InvoiceTotalGrossMinor = invoice.TotalGrossMinor;

                    if (!dto.CustomerId.HasValue && invoice.CustomerId.HasValue)
                    {
                        dto.CustomerId = invoice.CustomerId;
                    }
                }
            }

            dto.RefundedAmountMinor = BillingReconciliationCalculator.ClampRefundedAmount(
                dto.AmountMinor,
                await _db.Set<Refund>()
                    .AsNoTracking()
                    .Where(x => x.PaymentId == dto.Id && x.Status == RefundStatus.Completed && !x.IsDeleted)
                    .SumAsync(x => (long?)x.AmountMinor, ct)
                    .ConfigureAwait(false) ?? 0L);
            dto.NetCapturedAmountMinor = BillingReconciliationCalculator.CalculateNetCollectedAmount(dto.AmountMinor, dto.RefundedAmountMinor);
            dto.Refunds = await _db.Set<Refund>()
                .AsNoTracking()
                .Where(x => x.PaymentId == dto.Id && !x.IsDeleted)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => new PaymentRefundHistoryItemDto
                {
                    Id = x.Id,
                    AmountMinor = x.AmountMinor,
                    Currency = x.Currency,
                    Reason = x.Reason,
                    Status = x.Status,
                    CreatedAtUtc = x.CreatedAtUtc,
                    CompletedAtUtc = x.CompletedAtUtc
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);
            dto.ProviderEvents = await GetProviderEventsAsync(dto, ct).ConfigureAwait(false);
            dto.LastFinancialEventAtUtc = BillingPaymentTimelineFormatter.ResolveLastFinancialEventAtUtc(
                dto.CreatedAtUtc,
                dto.PaidAtUtc,
                dto.Refunds.Count == 0 ? null : dto.Refunds.Max(x => x.CompletedAtUtc ?? x.CreatedAtUtc));
            dto.OpenAgeHours = BillingPaymentTimelineFormatter.CalculateOpenAgeHours(dto.CreatedAtUtc, dto.PaidAtUtc);
            dto.ProviderReferenceState = BillingPaymentTimelineFormatter.ResolveProviderReferenceState(
                dto.ProviderTransactionRef,
                dto.ProviderPaymentIntentRef,
                dto.ProviderCheckoutSessionRef,
                dto.IsStripe,
                dto.Status,
                dto.RefundedAmountMinor);
            dto.NeedsReconciliation =
                dto.Status == PaymentStatus.Failed ||
                dto.Status == PaymentStatus.Pending ||
                dto.Status == PaymentStatus.Authorized ||
                dto.RefundedAmountMinor > 0 ||
                (dto.IsStripe &&
                 string.IsNullOrWhiteSpace(dto.ProviderTransactionRef) &&
                 string.IsNullOrWhiteSpace(dto.ProviderPaymentIntentRef) &&
                 string.IsNullOrWhiteSpace(dto.ProviderCheckoutSessionRef));
            dto.NeedsDisputeFollowUp =
                dto.IsStripe &&
                !UpdatePaymentDisputeReviewHandler.IsDisputeReviewResolved(dto.FailureReason) &&
                (dto.Status == PaymentStatus.Failed ||
                 dto.Status == PaymentStatus.Refunded ||
                 dto.RefundedAmountMinor > 0);
            dto.DisputeReviewState = UpdatePaymentDisputeReviewHandler.ResolveDisputeReviewState(dto.FailureReason);

            User? paymentUser = null;
            if (dto.UserId.HasValue)
            {
                paymentUser = await _db.Set<User>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == dto.UserId.Value && !x.IsDeleted, ct)
                    .ConfigureAwait(false);

                if (paymentUser is not null)
                {
                    dto.UserDisplayName = BillingPaymentDisplayFormatter.BuildUserDisplayName(paymentUser);
                    dto.UserEmail = paymentUser.Email;
                }
            }

            if (dto.CustomerId.HasValue)
            {
                var customer = await _db.Set<Customer>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == dto.CustomerId.Value && !x.IsDeleted, ct)
                    .ConfigureAwait(false);

                if (customer is not null)
                {
                    User? linkedUser = null;
                    if (customer.UserId.HasValue)
                    {
                        linkedUser = paymentUser is not null && paymentUser.Id == customer.UserId.Value
                            ? paymentUser
                            : await _db.Set<User>()
                                .AsNoTracking()
                                .FirstOrDefaultAsync(x => x.Id == customer.UserId.Value && !x.IsDeleted, ct)
                                .ConfigureAwait(false);
                    }

                    dto.CustomerDisplayName = BillingPaymentDisplayFormatter.BuildCustomerDisplayName(customer, linkedUser);
                    dto.CustomerEmail = BillingPaymentDisplayFormatter.ResolveCustomerEmail(customer, linkedUser);
                }
            }

            return dto;
        }

        private async Task<List<PaymentProviderEventItemDto>> GetProviderEventsAsync(PaymentEditDto dto, CancellationToken ct)
        {
            if (!dto.IsStripe)
            {
                return new List<PaymentProviderEventItemDto>();
            }

            var paymentIntentRef = string.IsNullOrWhiteSpace(dto.ProviderPaymentIntentRef) ? null : dto.ProviderPaymentIntentRef.Trim().ToLowerInvariant();
            var checkoutSessionRef = string.IsNullOrWhiteSpace(dto.ProviderCheckoutSessionRef) ? null : dto.ProviderCheckoutSessionRef.Trim().ToLowerInvariant();
            var providerTransactionRef = string.IsNullOrWhiteSpace(dto.ProviderTransactionRef) ? null : dto.ProviderTransactionRef.Trim().ToLowerInvariant();

            if (paymentIntentRef is null && checkoutSessionRef is null && providerTransactionRef is null)
            {
                return new List<PaymentProviderEventItemDto>();
            }

            var events = await _db.Set<EventLog>()
                .AsNoTracking()
                .Where(x =>
                    !x.IsDeleted &&
                    x.Type.StartsWith("StripeWebhook:") &&
                    ((paymentIntentRef != null && x.PropertiesJson.ToLower().Contains(paymentIntentRef)) ||
                     (checkoutSessionRef != null && x.PropertiesJson.ToLower().Contains(checkoutSessionRef)) ||
                     (providerTransactionRef != null && x.PropertiesJson.ToLower().Contains(providerTransactionRef))))
                .OrderByDescending(x => x.OccurredAtUtc)
                .Take(12)
                .Select(x => new
                {
                    EventType = x.Type,
                    OccurredAtUtc = x.OccurredAtUtc,
                    IdempotencyKey = x.IdempotencyKey,
                    x.PropertiesJson
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return events.Select(x => new PaymentProviderEventItemDto
            {
                EventType = x.EventType,
                OccurredAtUtc = x.OccurredAtUtc,
                IdempotencyKey = x.IdempotencyKey,
                CorrelationKind = BillingProviderAuditFormatter.ResolveCorrelationKind(
                    x.PropertiesJson,
                    paymentIntentRef,
                    checkoutSessionRef,
                    providerTransactionRef),
                CorrelationReference = BillingProviderAuditFormatter.ResolveCorrelationReference(
                    x.PropertiesJson,
                    paymentIntentRef,
                    checkoutSessionRef,
                    providerTransactionRef)
            }).ToList();
        }
    }

    public sealed class GetRefundsPageHandler
    {
        private const int MaxPageSize = 200;

        private readonly IAppDbContext _db;

        public GetRefundsPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<(List<BillingRefundListItemDto> Items, int Total)> HandleAsync(
            Guid businessId,
            int page,
            int pageSize,
            string? query = null,
            BillingRefundQueueFilter? filter = null,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var refundsQuery =
                from refund in _db.Set<Refund>().AsNoTracking()
                join payment in _db.Set<Payment>().AsNoTracking() on refund.PaymentId equals payment.Id
                where payment.BusinessId == businessId && !refund.IsDeleted && !payment.IsDeleted
                select new { Refund = refund, Payment = payment };

            if (filter.HasValue)
            {
                refundsQuery = filter.Value switch
                {
                    BillingRefundQueueFilter.Pending => refundsQuery.Where(x => x.Refund.Status == RefundStatus.Pending),
                    BillingRefundQueueFilter.Completed => refundsQuery.Where(x => x.Refund.Status == RefundStatus.Completed),
                    BillingRefundQueueFilter.Failed => refundsQuery.Where(x => x.Refund.Status == RefundStatus.Failed),
                    BillingRefundQueueFilter.Stripe => refundsQuery.Where(x => x.Payment.Provider == "Stripe"),
                    BillingRefundQueueFilter.NeedsSupport => refundsQuery.Where(x =>
                        x.Refund.Status == RefundStatus.Pending ||
                        x.Refund.Status == RefundStatus.Failed ||
                        (x.Payment.Provider == "Stripe" &&
                         (x.Payment.ProviderTransactionRef == null || x.Payment.ProviderTransactionRef == string.Empty) &&
                         (x.Payment.ProviderPaymentIntentRef == null || x.Payment.ProviderPaymentIntentRef == string.Empty) &&
                         (x.Payment.ProviderCheckoutSessionRef == null || x.Payment.ProviderCheckoutSessionRef == string.Empty))),
                    _ => refundsQuery
                };
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.Trim().ToLowerInvariant();
                refundsQuery = refundsQuery.Where(x =>
                    x.Refund.Reason.ToLower().Contains(term) ||
                    x.Payment.Provider.ToLower().Contains(term) ||
                    (x.Payment.ProviderTransactionRef != null && x.Payment.ProviderTransactionRef.ToLower().Contains(term)) ||
                    (x.Payment.ProviderPaymentIntentRef != null && x.Payment.ProviderPaymentIntentRef.ToLower().Contains(term)) ||
                    (x.Payment.ProviderCheckoutSessionRef != null && x.Payment.ProviderCheckoutSessionRef.ToLower().Contains(term)) ||
                    _db.Set<Order>().Any(o => o.Id == x.Refund.OrderId && !o.IsDeleted && o.OrderNumber.ToLower().Contains(term)) ||
                    (x.Payment.CustomerId.HasValue && _db.Set<Customer>().Any(c =>
                        c.Id == x.Payment.CustomerId.Value &&
                        !c.IsDeleted &&
                        (c.FirstName.ToLower().Contains(term) ||
                         c.LastName.ToLower().Contains(term) ||
                         c.Email.ToLower().Contains(term) ||
                         (c.CompanyName != null && c.CompanyName.ToLower().Contains(term)))))
                );
            }

            var total = await refundsQuery.CountAsync(ct).ConfigureAwait(false);
            var items = await refundsQuery
                .OrderByDescending(x => x.Refund.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new BillingRefundListItemDto
                {
                    Id = x.Refund.Id,
                    OrderId = x.Refund.OrderId ?? Guid.Empty,
                    PaymentId = x.Refund.PaymentId,
                    PaymentProvider = x.Payment.Provider,
                    PaymentProviderReference = x.Payment.ProviderTransactionRef,
                    PaymentProviderPaymentIntentRef = x.Payment.ProviderPaymentIntentRef,
                    PaymentProviderCheckoutSessionRef = x.Payment.ProviderCheckoutSessionRef,
                    PaymentStatus = x.Payment.Status,
                    CustomerId = x.Payment.CustomerId,
                    AmountMinor = x.Refund.AmountMinor,
                    Currency = x.Refund.Currency,
                    Reason = x.Refund.Reason,
                    Status = x.Refund.Status,
                    CreatedAtUtc = x.Refund.CreatedAtUtc,
                    CompletedAtUtc = x.Refund.CompletedAtUtc,
                    IsStripe = x.Payment.Provider == "Stripe",
                    RowVersion = x.Refund.RowVersion
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            await EnrichRefundsAsync(items, ct).ConfigureAwait(false);
            return (items, total);
        }

        private async Task EnrichRefundsAsync(List<BillingRefundListItemDto> items, CancellationToken ct)
        {
            if (items.Count == 0)
            {
                return;
            }

            var orderIds = items.Select(x => x.OrderId).Where(x => x != Guid.Empty).Distinct().ToList();
            var customerIds = items.Where(x => x.CustomerId.HasValue).Select(x => x.CustomerId!.Value).Distinct().ToList();

            var orders = orderIds.Count == 0
                ? new Dictionary<Guid, string>()
                : await _db.Set<Order>()
                    .AsNoTracking()
                    .Where(x => orderIds.Contains(x.Id) && !x.IsDeleted)
                    .ToDictionaryAsync(x => x.Id, x => x.OrderNumber, ct)
                    .ConfigureAwait(false);

            var customers = customerIds.Count == 0
                ? new List<Customer>()
                : await _db.Set<Customer>()
                    .AsNoTracking()
                    .Where(x => customerIds.Contains(x.Id) && !x.IsDeleted)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

            var userIds = customers.Where(x => x.UserId.HasValue).Select(x => x.UserId!.Value).Distinct().ToList();
            var users = userIds.Count == 0
                ? new Dictionary<Guid, User>()
                : await _db.Set<User>()
                    .AsNoTracking()
                    .Where(x => userIds.Contains(x.Id) && !x.IsDeleted)
                    .ToDictionaryAsync(x => x.Id, ct)
                    .ConfigureAwait(false);

            var customerMap = customers.ToDictionary(x => x.Id);

            foreach (var item in items)
            {
                if (item.OrderId != Guid.Empty && orders.TryGetValue(item.OrderId, out var orderNumber))
                {
                    item.OrderNumber = orderNumber;
                }

                if (item.CustomerId.HasValue && customerMap.TryGetValue(item.CustomerId.Value, out var customer))
                {
                    item.CustomerDisplayName = BillingPaymentDisplayFormatter.BuildCustomerDisplayName(customer, users);
                    item.CustomerEmail = BillingPaymentDisplayFormatter.ResolveCustomerEmail(customer, users);
                }

                item.LastRefundEventAtUtc = item.CompletedAtUtc ?? item.CreatedAtUtc;
                item.OpenAgeHours = BillingRefundTimelineFormatter.CalculateOpenAgeHours(item.CreatedAtUtc, item.CompletedAtUtc);
                item.ProviderReferenceState = BillingRefundTimelineFormatter.ResolveProviderReferenceState(
                    item.PaymentProviderReference,
                    item.PaymentProviderPaymentIntentRef,
                    item.PaymentProviderCheckoutSessionRef,
                    item.IsStripe,
                    item.Status,
                    item.PaymentStatus);
                item.NeedsSupportAttention =
                    item.Status == RefundStatus.Pending ||
                    item.Status == RefundStatus.Failed ||
                    (item.IsStripe &&
                     string.IsNullOrWhiteSpace(item.PaymentProviderReference) &&
                     string.IsNullOrWhiteSpace(item.PaymentProviderPaymentIntentRef) &&
                     string.IsNullOrWhiteSpace(item.PaymentProviderCheckoutSessionRef));
            }
        }
    }

    public sealed class GetRefundOpsSummaryHandler
    {
        private readonly IAppDbContext _db;

        public GetRefundOpsSummaryHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<RefundOpsSummaryDto> HandleAsync(Guid businessId, CancellationToken ct = default)
        {
            var refundsQuery =
                from refund in _db.Set<Refund>().AsNoTracking()
                join payment in _db.Set<Payment>().AsNoTracking() on refund.PaymentId equals payment.Id
                where payment.BusinessId == businessId && !refund.IsDeleted && !payment.IsDeleted
                select new { Refund = refund, Payment = payment };

            return new RefundOpsSummaryDto
            {
                PendingCount = await refundsQuery.CountAsync(x => x.Refund.Status == RefundStatus.Pending, ct).ConfigureAwait(false),
                CompletedCount = await refundsQuery.CountAsync(x => x.Refund.Status == RefundStatus.Completed, ct).ConfigureAwait(false),
                FailedCount = await refundsQuery.CountAsync(x => x.Refund.Status == RefundStatus.Failed, ct).ConfigureAwait(false),
                StripeCount = await refundsQuery.CountAsync(x => x.Payment.Provider == "Stripe", ct).ConfigureAwait(false),
                NeedsSupportCount = await refundsQuery.CountAsync(x =>
                    x.Refund.Status == RefundStatus.Pending ||
                    x.Refund.Status == RefundStatus.Failed ||
                    (x.Payment.Provider == "Stripe" &&
                     (x.Payment.ProviderTransactionRef == null || x.Payment.ProviderTransactionRef == string.Empty) &&
                     (x.Payment.ProviderPaymentIntentRef == null || x.Payment.ProviderPaymentIntentRef == string.Empty) &&
                     (x.Payment.ProviderCheckoutSessionRef == null || x.Payment.ProviderCheckoutSessionRef == string.Empty)), ct).ConfigureAwait(false)
            };
        }
    }

    public sealed class GetFinancialAccountsPageHandler
    {
        private const int MaxPageSize = 200;

        private readonly IAppDbContext _db;

        public GetFinancialAccountsPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<(List<FinancialAccountListItemDto> Items, int Total)> HandleAsync(
            Guid businessId,
            int page,
            int pageSize,
            string? query = null,
            AccountType? type = null,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var accountsQuery = _db.Set<FinancialAccount>()
                .AsNoTracking()
                .Where(x => x.BusinessId == businessId && !x.IsDeleted);

            if (type.HasValue)
            {
                accountsQuery = accountsQuery.Where(x => x.Type == type.Value);
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.Trim().ToLowerInvariant();
                accountsQuery = accountsQuery.Where(x =>
                    x.Name.ToLower().Contains(term) ||
                    (x.Code != null && x.Code.ToLower().Contains(term)));
            }

            var total = await accountsQuery.CountAsync(ct).ConfigureAwait(false);

            var items = await accountsQuery
                .OrderBy(x => x.Code)
                .ThenBy(x => x.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new FinancialAccountListItemDto
                {
                    Id = x.Id,
                    BusinessId = x.BusinessId,
                    Name = x.Name,
                    Type = x.Type,
                    Code = x.Code,
                    RowVersion = x.RowVersion
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return (items, total);
        }

        public async Task<FinancialAccountOpsSummaryDto> GetSummaryAsync(Guid businessId, CancellationToken ct = default)
        {
            var accountsQuery = _db.Set<FinancialAccount>()
                .AsNoTracking()
                .Where(x => x.BusinessId == businessId && !x.IsDeleted);

            return new FinancialAccountOpsSummaryDto
            {
                TotalCount = await accountsQuery.CountAsync(ct).ConfigureAwait(false),
                AssetCount = await accountsQuery.CountAsync(x => x.Type == AccountType.Asset, ct).ConfigureAwait(false),
                RevenueCount = await accountsQuery.CountAsync(x => x.Type == AccountType.Revenue, ct).ConfigureAwait(false),
                ExpenseCount = await accountsQuery.CountAsync(x => x.Type == AccountType.Expense, ct).ConfigureAwait(false),
                MissingCodeCount = await accountsQuery.CountAsync(x => string.IsNullOrWhiteSpace(x.Code), ct).ConfigureAwait(false)
            };
        }
    }

    public sealed class GetFinancialAccountForEditHandler
    {
        private readonly IAppDbContext _db;

        public GetFinancialAccountForEditHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public Task<FinancialAccountEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            return _db.Set<FinancialAccount>()
                .AsNoTracking()
                .Where(x => x.Id == id && !x.IsDeleted)
                .Select(x => new FinancialAccountEditDto
                {
                    Id = x.Id,
                    RowVersion = x.RowVersion,
                    BusinessId = x.BusinessId,
                    Name = x.Name,
                    Type = x.Type,
                    Code = x.Code
                })
                .FirstOrDefaultAsync(ct);
        }
    }

    public sealed class GetExpensesPageHandler
    {
        private const int MaxPageSize = 200;

        private readonly IAppDbContext _db;

        public GetExpensesPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<(List<ExpenseListItemDto> Items, int Total)> HandleAsync(
            Guid businessId,
            int page,
            int pageSize,
            string? query = null,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var expensesQuery = _db.Set<Expense>()
                .AsNoTracking()
                .Where(x => x.BusinessId == businessId && !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.Trim().ToLowerInvariant();
                expensesQuery = expensesQuery.Where(x =>
                    x.Category.ToLower().Contains(term) ||
                    x.Description.ToLower().Contains(term));
            }

            var total = await expensesQuery.CountAsync(ct).ConfigureAwait(false);

            var items = await expensesQuery
                .OrderByDescending(x => x.ExpenseDateUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ExpenseListItemDto
                {
                    Id = x.Id,
                    BusinessId = x.BusinessId,
                    SupplierId = x.SupplierId,
                    Category = x.Category,
                    Description = x.Description,
                    AmountMinor = x.AmountMinor,
                    ExpenseDateUtc = x.ExpenseDateUtc,
                    RowVersion = x.RowVersion
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return (items, total);
        }

        public async Task<ExpenseOpsSummaryDto> GetSummaryAsync(Guid businessId, CancellationToken ct = default)
        {
            var expensesQuery = _db.Set<Expense>()
                .AsNoTracking()
                .Where(x => x.BusinessId == businessId && !x.IsDeleted);

            var recentCutoffUtc = DateTime.UtcNow.AddDays(-30);
            const long highValueThresholdMinor = 100_00L;

            return new ExpenseOpsSummaryDto
            {
                TotalCount = await expensesQuery.CountAsync(ct).ConfigureAwait(false),
                SupplierLinkedCount = await expensesQuery.CountAsync(x => x.SupplierId != null, ct).ConfigureAwait(false),
                RecentCount = await expensesQuery.CountAsync(x => x.ExpenseDateUtc >= recentCutoffUtc, ct).ConfigureAwait(false),
                HighValueCount = await expensesQuery.CountAsync(x => x.AmountMinor >= highValueThresholdMinor, ct).ConfigureAwait(false)
            };
        }
    }

    public sealed class GetExpenseForEditHandler
    {
        private readonly IAppDbContext _db;

        public GetExpenseForEditHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public Task<ExpenseEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            return _db.Set<Expense>()
                .AsNoTracking()
                .Where(x => x.Id == id && !x.IsDeleted)
                .Select(x => new ExpenseEditDto
                {
                    Id = x.Id,
                    RowVersion = x.RowVersion,
                    BusinessId = x.BusinessId,
                    SupplierId = x.SupplierId,
                    Category = x.Category,
                    Description = x.Description,
                    AmountMinor = x.AmountMinor,
                    ExpenseDateUtc = x.ExpenseDateUtc
                })
                .FirstOrDefaultAsync(ct);
        }
    }

    public sealed class GetJournalEntriesPageHandler
    {
        private const int MaxPageSize = 200;

        private readonly IAppDbContext _db;

        public GetJournalEntriesPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<(List<JournalEntryListItemDto> Items, int Total)> HandleAsync(
            Guid businessId,
            int page,
            int pageSize,
            string? query = null,
            JournalEntryQueueFilter? filter = null,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var journalEntriesQuery = _db.Set<JournalEntry>()
                .AsNoTracking()
                .Where(x => x.BusinessId == businessId && !x.IsDeleted);

            if (filter.HasValue)
            {
                var recentCutoffUtc = DateTime.UtcNow.AddDays(-7);
                journalEntriesQuery = filter.Value switch
                {
                    JournalEntryQueueFilter.Recent => journalEntriesQuery.Where(x => x.EntryDateUtc >= recentCutoffUtc),
                    JournalEntryQueueFilter.MultiLine => journalEntriesQuery.Where(x => x.Lines.Count(l => !l.IsDeleted) > 2),
                    _ => journalEntriesQuery
                };
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.Trim().ToLowerInvariant();
                journalEntriesQuery = journalEntriesQuery.Where(x =>
                    x.Description.ToLower().Contains(term) ||
                    x.Lines.Any(l =>
                        !l.IsDeleted &&
                        _db.Set<FinancialAccount>().Any(a =>
                            a.Id == l.AccountId &&
                            !a.IsDeleted &&
                            (a.Name.ToLower().Contains(term) ||
                             (a.Code != null && a.Code.ToLower().Contains(term))))));
            }

            var total = await journalEntriesQuery.CountAsync(ct).ConfigureAwait(false);

            var items = await journalEntriesQuery
                .OrderByDescending(x => x.EntryDateUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new JournalEntryListItemDto
                {
                    Id = x.Id,
                    BusinessId = x.BusinessId,
                    EntryDateUtc = x.EntryDateUtc,
                    Description = x.Description,
                    LineCount = x.Lines.Count(l => !l.IsDeleted),
                    TotalDebitMinor = x.Lines.Where(l => !l.IsDeleted).Sum(l => l.DebitMinor),
                    TotalCreditMinor = x.Lines.Where(l => !l.IsDeleted).Sum(l => l.CreditMinor),
                    RowVersion = x.RowVersion
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return (items, total);
        }

        public async Task<JournalEntryOpsSummaryDto> GetSummaryAsync(Guid businessId, CancellationToken ct = default)
        {
            var journalEntriesQuery = _db.Set<JournalEntry>()
                .AsNoTracking()
                .Where(x => x.BusinessId == businessId && !x.IsDeleted);

            var recentCutoffUtc = DateTime.UtcNow.AddDays(-7);

            return new JournalEntryOpsSummaryDto
            {
                TotalCount = await journalEntriesQuery.CountAsync(ct).ConfigureAwait(false),
                RecentCount = await journalEntriesQuery.CountAsync(x => x.EntryDateUtc >= recentCutoffUtc, ct).ConfigureAwait(false),
                MultiLineCount = await journalEntriesQuery.CountAsync(x => x.Lines.Count(l => !l.IsDeleted) > 2, ct).ConfigureAwait(false)
            };
        }
    }

    public sealed class GetJournalEntryForEditHandler
    {
        private readonly IAppDbContext _db;

        public GetJournalEntryForEditHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<JournalEntryEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            var entry = await _db.Set<JournalEntry>()
                .AsNoTracking()
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct)
                .ConfigureAwait(false);

            if (entry is null)
            {
                return null;
            }

            return new JournalEntryEditDto
            {
                Id = entry.Id,
                RowVersion = entry.RowVersion,
                BusinessId = entry.BusinessId,
                EntryDateUtc = entry.EntryDateUtc,
                Description = entry.Description,
                Lines = entry.Lines
                    .Where(x => !x.IsDeleted)
                    .OrderBy(x => x.CreatedAtUtc)
                    .Select(x => new JournalEntryLineDto
                    {
                        Id = x.Id,
                        AccountId = x.AccountId,
                        DebitMinor = x.DebitMinor,
                        CreditMinor = x.CreditMinor,
                        Memo = x.Memo
                    })
                    .ToList()
            };
        }
    }

    internal static class BillingPaymentDisplayFormatter
    {
        public static string BuildCustomerDisplayName(Customer customer, IReadOnlyDictionary<Guid, User> users)
        {
            if (customer.UserId.HasValue && users.TryGetValue(customer.UserId.Value, out var linkedUser))
            {
                return BuildUserDisplayName(linkedUser);
            }

            return BuildFallbackDisplayName(customer.FirstName, customer.LastName, customer.Email);
        }

        public static string BuildCustomerDisplayName(Customer customer, User? linkedUser)
        {
            if (linkedUser is not null)
            {
                return BuildUserDisplayName(linkedUser);
            }

            return BuildFallbackDisplayName(customer.FirstName, customer.LastName, customer.Email);
        }

        public static string? ResolveCustomerEmail(Customer customer, IReadOnlyDictionary<Guid, User> users)
        {
            if (customer.UserId.HasValue && users.TryGetValue(customer.UserId.Value, out var linkedUser))
            {
                return linkedUser.Email;
            }

            return string.IsNullOrWhiteSpace(customer.Email) ? null : customer.Email;
        }

        public static string? ResolveCustomerEmail(Customer customer, User? linkedUser)
        {
            if (linkedUser is not null)
            {
                return linkedUser.Email;
            }

            return string.IsNullOrWhiteSpace(customer.Email) ? null : customer.Email;
        }

        public static string BuildUserDisplayName(User user)
        {
            return BuildFallbackDisplayName(user.FirstName, user.LastName, user.Email);
        }

        private static string BuildFallbackDisplayName(string? firstName, string? lastName, string emailFallback)
        {
            var fullName = $"{firstName ?? string.Empty} {lastName ?? string.Empty}".Trim();
            return string.IsNullOrWhiteSpace(fullName) ? emailFallback : fullName;
        }
    }

    internal static class BillingPaymentTimelineFormatter
    {
        public static DateTime? ResolveLastFinancialEventAtUtc(
            DateTime createdAtUtc,
            DateTime? paidAtUtc,
            DateTime? refundEventAtUtc)
        {
            var lastEventAtUtc = createdAtUtc;

            if (paidAtUtc.HasValue && paidAtUtc.Value > lastEventAtUtc)
            {
                lastEventAtUtc = paidAtUtc.Value;
            }

            if (refundEventAtUtc.HasValue && refundEventAtUtc.Value > lastEventAtUtc)
            {
                lastEventAtUtc = refundEventAtUtc.Value;
            }

            return lastEventAtUtc;
        }

        public static int CalculateOpenAgeHours(DateTime createdAtUtc, DateTime? paidAtUtc)
        {
            var endAtUtc = paidAtUtc ?? DateTime.UtcNow;
            var age = endAtUtc - createdAtUtc;
            return age.TotalHours <= 0 ? 0 : (int)Math.Floor(age.TotalHours);
        }

        public static string ResolveProviderReferenceState(
            string? providerTransactionRef,
            string? providerPaymentIntentRef,
            string? providerCheckoutSessionRef,
            bool isStripe,
            PaymentStatus status,
            long refundedAmountMinor)
        {
            if (string.IsNullOrWhiteSpace(providerTransactionRef) &&
                string.IsNullOrWhiteSpace(providerPaymentIntentRef) &&
                string.IsNullOrWhiteSpace(providerCheckoutSessionRef))
            {
                return isStripe ? "Stripe ref missing" : "Reference missing";
            }

            if (refundedAmountMinor > 0)
            {
                return "Refund activity recorded";
            }

            return status switch
            {
                PaymentStatus.Pending => "Waiting for provider completion",
                PaymentStatus.Authorized => "Authorized on provider",
                PaymentStatus.Captured => "Captured on provider",
                PaymentStatus.Completed => "Completed on provider",
                PaymentStatus.Failed => "Provider failure recorded",
                PaymentStatus.Refunded => "Refunded against provider ref",
                PaymentStatus.Voided => "Voided on provider",
                _ => "Provider reference linked"
            };
        }
    }

    internal static class BillingRefundTimelineFormatter
    {
        public static int CalculateOpenAgeHours(DateTime createdAtUtc, DateTime? completedAtUtc)
        {
            var endAtUtc = completedAtUtc ?? DateTime.UtcNow;
            var age = endAtUtc - createdAtUtc;
            return age.TotalHours <= 0 ? 0 : (int)Math.Floor(age.TotalHours);
        }

        public static string ResolveProviderReferenceState(
            string? providerReference,
            string? providerPaymentIntentRef,
            string? providerCheckoutSessionRef,
            bool isStripe,
            RefundStatus refundStatus,
            PaymentStatus paymentStatus)
        {
            if (string.IsNullOrWhiteSpace(providerReference) &&
                string.IsNullOrWhiteSpace(providerPaymentIntentRef) &&
                string.IsNullOrWhiteSpace(providerCheckoutSessionRef))
            {
                return isStripe ? "Stripe ref missing" : "Reference missing";
            }

            return refundStatus switch
            {
                RefundStatus.Pending => "Waiting for provider refund completion",
                RefundStatus.Completed => "Refund recorded against provider ref",
                RefundStatus.Failed => "Provider-side refund failure needs review",
                _ => paymentStatus == PaymentStatus.Refunded
                    ? "Payment fully refunded on provider"
                    : "Provider reference linked"
            };
        }
    }

    internal static class BillingProviderAuditFormatter
    {
        public static string ResolveCorrelationKind(
            string propertiesJson,
            string? paymentIntentRef,
            string? checkoutSessionRef,
            string? providerTransactionRef)
        {
            var hasPaymentIntent = Contains(propertiesJson, paymentIntentRef);
            var hasCheckoutSession = Contains(propertiesJson, checkoutSessionRef);
            var hasProviderTransaction = Contains(propertiesJson, providerTransactionRef);

            var matchCount =
                (hasPaymentIntent ? 1 : 0) +
                (hasCheckoutSession ? 1 : 0) +
                (hasProviderTransaction ? 1 : 0);

            if (matchCount > 1)
            {
                return "Multiple";
            }

            if (hasPaymentIntent)
            {
                return "PaymentIntent";
            }

            if (hasCheckoutSession)
            {
                return "CheckoutSession";
            }

            if (hasProviderTransaction)
            {
                return "ProviderTransaction";
            }

            return "ProviderReference";
        }

        public static string ResolveCorrelationReference(
            string propertiesJson,
            string? paymentIntentRef,
            string? checkoutSessionRef,
            string? providerTransactionRef)
        {
            var matches = new List<string>(3);

            if (Contains(propertiesJson, paymentIntentRef))
            {
                matches.Add(paymentIntentRef!);
            }

            if (Contains(propertiesJson, checkoutSessionRef) && !matches.Contains(checkoutSessionRef!))
            {
                matches.Add(checkoutSessionRef!);
            }

            if (Contains(propertiesJson, providerTransactionRef) && !matches.Contains(providerTransactionRef!))
            {
                matches.Add(providerTransactionRef!);
            }

            return matches.Count == 0 ? string.Empty : string.Join(", ", matches);
        }

        private static bool Contains(string propertiesJson, string? value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   propertiesJson.Contains(value, StringComparison.OrdinalIgnoreCase);
        }
    }
}
