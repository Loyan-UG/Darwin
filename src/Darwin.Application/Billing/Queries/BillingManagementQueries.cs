using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Billing.Commands;
using Darwin.Application.Billing.DTOs;
using Darwin.Application.Common;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Darwin.Application.Billing.Queries
{
    public sealed class GetPaymentsPageHandler
    {
        private const int MaxPageSize = 200;
        private static readonly string DisputeLostMarkerPattern = QueryLikePattern.Contains("[DisputeReview:Lost;");
        private static readonly string DisputeWonMarkerPattern = QueryLikePattern.Contains("[DisputeReview:Won;");

        private readonly IAppDbContext _db;
        private readonly IClock _clock;

        public GetPaymentsPageHandler(IAppDbContext db, IClock clock)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

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
                         (!EF.Functions.Like(x.FailureReason, DisputeWonMarkerPattern, QueryLikePattern.EscapeCharacter) &&
                          !EF.Functions.Like(x.FailureReason, DisputeLostMarkerPattern, QueryLikePattern.EscapeCharacter)))),
                    _ => paymentsQuery
                };
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = QueryLikePattern.Contains(query);
                paymentsQuery = paymentsQuery.Where(x =>
                    EF.Functions.Like(x.Provider, term, QueryLikePattern.EscapeCharacter) ||
                    EF.Functions.Like(x.Currency, term, QueryLikePattern.EscapeCharacter) ||
                    (x.ProviderTransactionRef != null && EF.Functions.Like(x.ProviderTransactionRef, term, QueryLikePattern.EscapeCharacter)) ||
                    (x.ProviderPaymentIntentRef != null && EF.Functions.Like(x.ProviderPaymentIntentRef, term, QueryLikePattern.EscapeCharacter)) ||
                    (x.ProviderCheckoutSessionRef != null && EF.Functions.Like(x.ProviderCheckoutSessionRef, term, QueryLikePattern.EscapeCharacter)) ||
                    (x.OrderId.HasValue && _db.Set<Order>().Any(o => o.Id == x.OrderId.Value && !o.IsDeleted && EF.Functions.Like(o.OrderNumber, term, QueryLikePattern.EscapeCharacter))) ||
                    (x.CustomerId.HasValue && _db.Set<Customer>().Any(c =>
                        c.Id == x.CustomerId.Value &&
                        !c.IsDeleted &&
                        (EF.Functions.Like(c.FirstName, term, QueryLikePattern.EscapeCharacter) ||
                         EF.Functions.Like(c.LastName, term, QueryLikePattern.EscapeCharacter) ||
                         EF.Functions.Like(c.Email, term, QueryLikePattern.EscapeCharacter) ||
                         (c.CompanyName != null && EF.Functions.Like(c.CompanyName, term, QueryLikePattern.EscapeCharacter))))) ||
                    (x.UserId.HasValue && _db.Set<User>().Any(u =>
                        u.Id == x.UserId.Value &&
                        !u.IsDeleted &&
                        (EF.Functions.Like(u.Email, term, QueryLikePattern.EscapeCharacter) ||
                         (u.FirstName != null && EF.Functions.Like(u.FirstName, term, QueryLikePattern.EscapeCharacter)) ||
                         (u.LastName != null && EF.Functions.Like(u.LastName, term, QueryLikePattern.EscapeCharacter))))));
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

            var nowUtc = _clock.UtcNow;
            await EnrichPaymentsAsync(items, nowUtc, ct).ConfigureAwait(false);
            return (items, total);
        }

        private async Task EnrichPaymentsAsync(List<PaymentListItemDto> items, DateTime nowUtc, CancellationToken ct)
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
                item.OpenAgeHours = BillingPaymentTimelineFormatter.CalculateOpenAgeHours(item.CreatedAtUtc, item.PaidAtUtc, nowUtc);
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
                item.FailureReason = OperatorDisplayTextSanitizer.SanitizeFailureText(item.FailureReason);
            }
        }
    }

    public sealed class GetBusinessSubscriptionsPageHandler
    {
        private const int MaxPageSize = 200;

        private readonly IAppDbContext _db;

        public GetBusinessSubscriptionsPageHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<(List<BusinessSubscriptionListItemDto> Items, int Total)> HandleAsync(
            int page,
            int pageSize,
            string? query = null,
            BusinessSubscriptionQueueFilter filter = BusinessSubscriptionQueueFilter.All,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var subscriptionsQuery = _db.Set<BusinessSubscription>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted);

            subscriptionsQuery = filter switch
            {
                BusinessSubscriptionQueueFilter.Active => subscriptionsQuery.Where(x => x.Status == SubscriptionStatus.Active),
                BusinessSubscriptionQueueFilter.Trialing => subscriptionsQuery.Where(x => x.Status == SubscriptionStatus.Trialing),
                BusinessSubscriptionQueueFilter.PastDue => subscriptionsQuery.Where(x => x.Status == SubscriptionStatus.PastDue),
                BusinessSubscriptionQueueFilter.Canceled => subscriptionsQuery.Where(x => x.Status == SubscriptionStatus.Canceled),
                BusinessSubscriptionQueueFilter.Stripe => subscriptionsQuery.Where(x => x.Provider == "Stripe"),
                BusinessSubscriptionQueueFilter.MissingProviderReference => subscriptionsQuery.Where(x =>
                    (x.ProviderSubscriptionId == null || x.ProviderSubscriptionId == string.Empty) &&
                    (x.ProviderCheckoutSessionId == null || x.ProviderCheckoutSessionId == string.Empty)),
                BusinessSubscriptionQueueFilter.CancelAtPeriodEnd => subscriptionsQuery.Where(x => x.CancelAtPeriodEnd),
                _ => subscriptionsQuery
            };

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = QueryLikePattern.Contains(query);
                subscriptionsQuery = subscriptionsQuery.Where(x =>
                    EF.Functions.Like(x.Provider, term, QueryLikePattern.EscapeCharacter) ||
                    (x.ProviderCustomerId != null && EF.Functions.Like(x.ProviderCustomerId, term, QueryLikePattern.EscapeCharacter)) ||
                    (x.ProviderSubscriptionId != null && EF.Functions.Like(x.ProviderSubscriptionId, term, QueryLikePattern.EscapeCharacter)) ||
                    (x.ProviderCheckoutSessionId != null && EF.Functions.Like(x.ProviderCheckoutSessionId, term, QueryLikePattern.EscapeCharacter)) ||
                    _db.Set<BillingPlan>().Any(p =>
                        p.Id == x.BillingPlanId &&
                        !p.IsDeleted &&
                        (EF.Functions.Like(p.Code, term, QueryLikePattern.EscapeCharacter) ||
                         EF.Functions.Like(p.Name, term, QueryLikePattern.EscapeCharacter))) ||
                    (x.BusinessId.HasValue && _db.Set<Business>().Any(b =>
                        b.Id == x.BusinessId.Value &&
                        !b.IsDeleted &&
                        (EF.Functions.Like(b.Name, term, QueryLikePattern.EscapeCharacter) ||
                         (b.LegalName != null && EF.Functions.Like(b.LegalName, term, QueryLikePattern.EscapeCharacter)) ||
                         (b.ContactEmail != null && EF.Functions.Like(b.ContactEmail, term, QueryLikePattern.EscapeCharacter))))));
            }

            var total = await subscriptionsQuery.CountAsync(ct).ConfigureAwait(false);

            var items = await subscriptionsQuery
                .OrderByDescending(x => x.CurrentPeriodEndUtc ?? x.TrialEndsAtUtc ?? x.StartedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new BusinessSubscriptionListItemDto
                {
                    Id = x.Id,
                    BusinessId = x.BusinessId,
                    BusinessName = x.BusinessId.HasValue
                        ? _db.Set<Business>()
                            .Where(b => b.Id == x.BusinessId.Value)
                            .Select(b => b.Name)
                            .FirstOrDefault() ?? string.Empty
                        : string.Empty,
                    BusinessContactEmail = x.BusinessId.HasValue
                        ? _db.Set<Business>()
                            .Where(b => b.Id == x.BusinessId.Value)
                            .Select(b => b.ContactEmail)
                            .FirstOrDefault()
                        : null,
                    BillingPlanId = x.BillingPlanId,
                    PlanCode = _db.Set<BillingPlan>()
                        .Where(p => p.Id == x.BillingPlanId)
                        .Select(p => p.Code)
                        .FirstOrDefault() ?? string.Empty,
                    PlanName = _db.Set<BillingPlan>()
                        .Where(p => p.Id == x.BillingPlanId)
                        .Select(p => p.Name)
                        .FirstOrDefault() ?? string.Empty,
                    Provider = x.Provider,
                    ProviderCustomerId = x.ProviderCustomerId,
                    ProviderSubscriptionId = x.ProviderSubscriptionId,
                    ProviderCheckoutSessionId = x.ProviderCheckoutSessionId,
                    Status = x.Status,
                    StartedAtUtc = x.StartedAtUtc,
                    CurrentPeriodEndUtc = x.CurrentPeriodEndUtc,
                    TrialEndsAtUtc = x.TrialEndsAtUtc,
                    CanceledAtUtc = x.CanceledAtUtc,
                    CancelAtPeriodEnd = x.CancelAtPeriodEnd,
                    UnitPriceMinor = x.UnitPriceMinor,
                    Currency = x.Currency,
                    IsStripe = x.Provider == "Stripe",
                    MissingProviderReference =
                        (x.ProviderSubscriptionId == null || x.ProviderSubscriptionId == string.Empty) &&
                        (x.ProviderCheckoutSessionId == null || x.ProviderCheckoutSessionId == string.Empty),
                    ProviderReferenceState = ResolveProviderReferenceState(
                        x.Provider,
                        x.ProviderSubscriptionId,
                        x.ProviderCheckoutSessionId,
                        x.Status,
                        x.CancelAtPeriodEnd),
                    RowVersion = x.RowVersion
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return (items, total);
        }

        private static string ResolveProviderReferenceState(
            string? provider,
            string? providerSubscriptionId,
            string? providerCheckoutSessionId,
            SubscriptionStatus status,
            bool cancelAtPeriodEnd)
        {
            if (string.IsNullOrWhiteSpace(providerSubscriptionId) &&
                string.IsNullOrWhiteSpace(providerCheckoutSessionId))
            {
                return string.Equals(provider, "Stripe", StringComparison.OrdinalIgnoreCase)
                    ? "Stripe subscription ref missing"
                    : "Reference missing";
            }

            if (cancelAtPeriodEnd)
            {
                return "Cancel at period end";
            }

            return status switch
            {
                SubscriptionStatus.Active => "Active on provider",
                SubscriptionStatus.Trialing => "Trialing on provider",
                SubscriptionStatus.PastDue => "Provider payment attention",
                SubscriptionStatus.Canceled => "Canceled on provider",
                SubscriptionStatus.Unpaid => "Provider unpaid",
                SubscriptionStatus.Incomplete => "Provider setup incomplete",
                SubscriptionStatus.IncompleteExpired => "Provider setup expired",
                SubscriptionStatus.Paused => "Paused on provider",
                _ => "Provider reference linked"
            };
        }
    }

    public sealed class GetBusinessSubscriptionOpsSummaryHandler
    {
        private readonly IAppDbContext _db;

        public GetBusinessSubscriptionOpsSummaryHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<BusinessSubscriptionOpsSummaryDto> HandleAsync(CancellationToken ct = default)
        {
            var query = _db.Set<BusinessSubscription>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted);

            return new BusinessSubscriptionOpsSummaryDto
            {
                TotalCount = await query.CountAsync(ct).ConfigureAwait(false),
                ActiveCount = await query.CountAsync(x => x.Status == SubscriptionStatus.Active, ct).ConfigureAwait(false),
                TrialingCount = await query.CountAsync(x => x.Status == SubscriptionStatus.Trialing, ct).ConfigureAwait(false),
                PastDueCount = await query.CountAsync(x => x.Status == SubscriptionStatus.PastDue, ct).ConfigureAwait(false),
                CanceledCount = await query.CountAsync(x => x.Status == SubscriptionStatus.Canceled, ct).ConfigureAwait(false),
                StripeCount = await query.CountAsync(x => x.Provider == "Stripe", ct).ConfigureAwait(false),
                MissingProviderReferenceCount = await query.CountAsync(x =>
                    (x.ProviderSubscriptionId == null || x.ProviderSubscriptionId == string.Empty) &&
                    (x.ProviderCheckoutSessionId == null || x.ProviderCheckoutSessionId == string.Empty), ct).ConfigureAwait(false),
                CancelAtPeriodEndCount = await query.CountAsync(x => x.CancelAtPeriodEnd, ct).ConfigureAwait(false)
            };
        }
    }

    public sealed class GetPaymentOpsSummaryHandler
    {
        private static readonly string DisputeLostMarkerPattern = QueryLikePattern.Contains("[DisputeReview:Lost;");
        private static readonly string DisputeWonMarkerPattern = QueryLikePattern.Contains("[DisputeReview:Won;");

        private readonly IAppDbContext _db;

        public GetPaymentOpsSummaryHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<PaymentOpsSummaryDto> HandleAsync(Guid businessId, CancellationToken ct = default)
        {
            var payments = _db.Set<Payment>()
                .AsNoTracking()
                .Where(x => x.BusinessId == businessId && !x.IsDeleted);

            var paymentSummaryRows = payments.Select(x => new
            {
                Payment = x,
                HasCompletedRefund = _db.Set<Refund>().Any(r =>
                    !r.IsDeleted &&
                    r.PaymentId == x.Id &&
                    r.Status == RefundStatus.Completed)
            });

            return await paymentSummaryRows
                .GroupBy(_ => 1)
                .Select(group => new PaymentOpsSummaryDto
                {
                    PendingCount = group.Count(x => x.Payment.Status == PaymentStatus.Pending || x.Payment.Status == PaymentStatus.Authorized),
                    FailedCount = group.Count(x => x.Payment.Status == PaymentStatus.Failed),
                    UnlinkedCount = group.Count(x => !x.Payment.OrderId.HasValue && !x.Payment.InvoiceId.HasValue),
                    ProviderLinkedCount = group.Count(x =>
                        (x.Payment.ProviderTransactionRef != null && x.Payment.ProviderTransactionRef != string.Empty) ||
                        (x.Payment.ProviderPaymentIntentRef != null && x.Payment.ProviderPaymentIntentRef != string.Empty) ||
                        (x.Payment.ProviderCheckoutSessionRef != null && x.Payment.ProviderCheckoutSessionRef != string.Empty)),
                    RefundedCount = group.Count(x =>
                        x.Payment.Status == PaymentStatus.Refunded ||
                        x.HasCompletedRefund),
                    StripeCount = group.Count(x => x.Payment.Provider == "Stripe"),
                    MissingProviderRefCount = group.Count(x =>
                        (x.Payment.ProviderTransactionRef == null || x.Payment.ProviderTransactionRef == string.Empty) &&
                        (x.Payment.ProviderPaymentIntentRef == null || x.Payment.ProviderPaymentIntentRef == string.Empty) &&
                        (x.Payment.ProviderCheckoutSessionRef == null || x.Payment.ProviderCheckoutSessionRef == string.Empty)),
                    FailedStripeCount = group.Count(x => x.Payment.Provider == "Stripe" && x.Payment.Status == PaymentStatus.Failed),
                    NeedsReconciliationCount = group.Count(x =>
                        x.Payment.Status == PaymentStatus.Pending ||
                        x.Payment.Status == PaymentStatus.Authorized ||
                        x.Payment.Status == PaymentStatus.Failed ||
                        (x.Payment.Provider == "Stripe" &&
                         (x.Payment.ProviderTransactionRef == null || x.Payment.ProviderTransactionRef == string.Empty) &&
                         (x.Payment.ProviderPaymentIntentRef == null || x.Payment.ProviderPaymentIntentRef == string.Empty) &&
                         (x.Payment.ProviderCheckoutSessionRef == null || x.Payment.ProviderCheckoutSessionRef == string.Empty)) ||
                        x.HasCompletedRefund),
                    DisputeFollowUpCount = group.Count(x =>
                        x.Payment.Provider == "Stripe" &&
                        (x.Payment.Status == PaymentStatus.Failed ||
                         x.Payment.Status == PaymentStatus.Refunded ||
                         x.HasCompletedRefund) &&
                        (x.Payment.FailureReason == null ||
                         (!EF.Functions.Like(x.Payment.FailureReason, DisputeWonMarkerPattern, QueryLikePattern.EscapeCharacter) &&
                          !EF.Functions.Like(x.Payment.FailureReason, DisputeLostMarkerPattern, QueryLikePattern.EscapeCharacter))))
                })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false) ?? new PaymentOpsSummaryDto();
        }
    }

    public sealed class GetPaymentForEditHandler
    {
        private readonly IAppDbContext _db;
        private readonly IClock _clock;

        public GetPaymentForEditHandler(IAppDbContext db, IClock clock)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

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
            var nowUtc = _clock.UtcNow;
            dto.OpenAgeHours = BillingPaymentTimelineFormatter.CalculateOpenAgeHours(dto.CreatedAtUtc, dto.PaidAtUtc, nowUtc);
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
            dto.FailureReason = OperatorDisplayTextSanitizer.SanitizeFailureText(dto.FailureReason);

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

            var paymentIntentRef = string.IsNullOrWhiteSpace(dto.ProviderPaymentIntentRef) ? null : dto.ProviderPaymentIntentRef.Trim();
            var checkoutSessionRef = string.IsNullOrWhiteSpace(dto.ProviderCheckoutSessionRef) ? null : dto.ProviderCheckoutSessionRef.Trim();
            var providerTransactionRef = string.IsNullOrWhiteSpace(dto.ProviderTransactionRef) ? null : dto.ProviderTransactionRef.Trim();

            if (paymentIntentRef is null && checkoutSessionRef is null && providerTransactionRef is null)
            {
                return new List<PaymentProviderEventItemDto>();
            }

            var paymentIntentPattern = paymentIntentRef is null ? null : QueryLikePattern.Contains(paymentIntentRef);
            var checkoutSessionPattern = checkoutSessionRef is null ? null : QueryLikePattern.Contains(checkoutSessionRef);
            var providerTransactionPattern = providerTransactionRef is null ? null : QueryLikePattern.Contains(providerTransactionRef);

            var events = await _db.Set<EventLog>()
                .AsNoTracking()
                .Where(x =>
                    !x.IsDeleted &&
                    x.Type.StartsWith("StripeWebhook:") &&
                    ((paymentIntentPattern != null && EF.Functions.Like(x.PropertiesJson, paymentIntentPattern, QueryLikePattern.EscapeCharacter)) ||
                     (checkoutSessionPattern != null && EF.Functions.Like(x.PropertiesJson, checkoutSessionPattern, QueryLikePattern.EscapeCharacter)) ||
                     (providerTransactionPattern != null && EF.Functions.Like(x.PropertiesJson, providerTransactionPattern, QueryLikePattern.EscapeCharacter))))
                .OrderByDescending(x => x.OccurredAtUtc)
                .Take(50)
                .Select(x => new
                {
                    EventType = x.Type,
                    OccurredAtUtc = x.OccurredAtUtc,
                    IdempotencyKey = x.IdempotencyKey,
                    x.PropertiesJson
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            return events
                .Where(x => BillingProviderAuditFormatter.HasCorrelation(
                    x.PropertiesJson,
                    paymentIntentRef,
                    checkoutSessionRef,
                    providerTransactionRef))
                .Take(12)
                .Select(x => new PaymentProviderEventItemDto
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
        private readonly IClock _clock;

        public GetRefundsPageHandler(IAppDbContext db, IClock clock)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

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
                var term = QueryLikePattern.Contains(query);
                refundsQuery = refundsQuery.Where(x =>
                    EF.Functions.Like(x.Refund.Reason, term, QueryLikePattern.EscapeCharacter) ||
                    EF.Functions.Like(x.Payment.Provider, term, QueryLikePattern.EscapeCharacter) ||
                    (x.Payment.ProviderTransactionRef != null && EF.Functions.Like(x.Payment.ProviderTransactionRef, term, QueryLikePattern.EscapeCharacter)) ||
                    (x.Payment.ProviderPaymentIntentRef != null && EF.Functions.Like(x.Payment.ProviderPaymentIntentRef, term, QueryLikePattern.EscapeCharacter)) ||
                    (x.Payment.ProviderCheckoutSessionRef != null && EF.Functions.Like(x.Payment.ProviderCheckoutSessionRef, term, QueryLikePattern.EscapeCharacter)) ||
                    _db.Set<Order>().Any(o => o.Id == x.Refund.OrderId && !o.IsDeleted && EF.Functions.Like(o.OrderNumber, term, QueryLikePattern.EscapeCharacter)) ||
                    (x.Payment.CustomerId.HasValue && _db.Set<Customer>().Any(c =>
                        c.Id == x.Payment.CustomerId.Value &&
                        !c.IsDeleted &&
                        (EF.Functions.Like(c.FirstName, term, QueryLikePattern.EscapeCharacter) ||
                         EF.Functions.Like(c.LastName, term, QueryLikePattern.EscapeCharacter) ||
                         EF.Functions.Like(c.Email, term, QueryLikePattern.EscapeCharacter) ||
                         (c.CompanyName != null && EF.Functions.Like(c.CompanyName, term, QueryLikePattern.EscapeCharacter)))))
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
                    RefundProvider = x.Refund.Provider,
                    RefundProviderReference = x.Refund.ProviderRefundReference,
                    RefundProviderPaymentReference = x.Refund.ProviderPaymentReference,
                    RefundProviderStatus = x.Refund.ProviderStatus,
                    FailureReason = x.Refund.FailureReason,
                    RequestedAtUtc = x.Refund.RequestedAtUtc,
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

            var nowUtc = _clock.UtcNow;
            await EnrichRefundsAsync(items, nowUtc, ct).ConfigureAwait(false);
            return (items, total);
        }

        private async Task EnrichRefundsAsync(List<BillingRefundListItemDto> items, DateTime nowUtc, CancellationToken ct)
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
                item.OpenAgeHours = BillingRefundTimelineFormatter.CalculateOpenAgeHours(item.CreatedAtUtc, item.CompletedAtUtc, nowUtc);
                item.ProviderReferenceState = BillingRefundTimelineFormatter.ResolveProviderReferenceState(
                    item.RefundProviderReference ?? item.PaymentProviderReference,
                    item.RefundProviderPaymentReference ?? item.PaymentProviderPaymentIntentRef,
                    item.PaymentProviderCheckoutSessionRef,
                    item.IsStripe,
                    item.Status,
                    item.PaymentStatus);
                item.NeedsSupportAttention =
                    item.Status == RefundStatus.Pending ||
                    item.Status == RefundStatus.Failed ||
                    (item.IsStripe &&
                     string.IsNullOrWhiteSpace(item.RefundProviderReference) &&
                     string.IsNullOrWhiteSpace(item.RefundProviderPaymentReference) &&
                     string.IsNullOrWhiteSpace(item.PaymentProviderReference) &&
                     string.IsNullOrWhiteSpace(item.PaymentProviderPaymentIntentRef) &&
                     string.IsNullOrWhiteSpace(item.PaymentProviderCheckoutSessionRef));
                item.FailureReason = OperatorDisplayTextSanitizer.SanitizeFailureText(item.FailureReason);
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
                var term = QueryLikePattern.Contains(query);
                accountsQuery = accountsQuery.Where(x =>
                    EF.Functions.Like(x.Name, term, QueryLikePattern.EscapeCharacter) ||
                    (x.Code != null && EF.Functions.Like(x.Code, term, QueryLikePattern.EscapeCharacter)));
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
        private readonly IClock _clock;

        public GetExpensesPageHandler(IAppDbContext db, IClock clock)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

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
                var term = QueryLikePattern.Contains(query);
                expensesQuery = expensesQuery.Where(x =>
                    EF.Functions.Like(x.Category, term, QueryLikePattern.EscapeCharacter) ||
                    EF.Functions.Like(x.Description, term, QueryLikePattern.EscapeCharacter));
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

            var recentCutoffUtc = _clock.UtcNow.AddDays(-30);
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
        private readonly IClock _clock;

        public GetJournalEntriesPageHandler(IAppDbContext db, IClock clock)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

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
                var recentCutoffUtc = _clock.UtcNow.AddDays(-7);
                journalEntriesQuery = filter.Value switch
                {
                    JournalEntryQueueFilter.Recent => journalEntriesQuery.Where(x => x.EntryDateUtc >= recentCutoffUtc),
                    JournalEntryQueueFilter.MultiLine => journalEntriesQuery.Where(x => x.Lines.Count(l => !l.IsDeleted) > 2),
                    _ => journalEntriesQuery
                };
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = QueryLikePattern.Contains(query);
                journalEntriesQuery = journalEntriesQuery.Where(x =>
                    EF.Functions.Like(x.Description, term, QueryLikePattern.EscapeCharacter) ||
                    x.Lines.Any(l =>
                        !l.IsDeleted &&
                        _db.Set<FinancialAccount>().Any(a =>
                            a.Id == l.AccountId &&
                            !a.IsDeleted &&
                            (EF.Functions.Like(a.Name, term, QueryLikePattern.EscapeCharacter) ||
                             (a.Code != null && EF.Functions.Like(a.Code, term, QueryLikePattern.EscapeCharacter))))));
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

            var recentCutoffUtc = _clock.UtcNow.AddDays(-7);

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

        public static int CalculateOpenAgeHours(DateTime createdAtUtc, DateTime? paidAtUtc, DateTime nowUtc)
        {
            var endAtUtc = paidAtUtc ?? nowUtc;
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
        public static int CalculateOpenAgeHours(DateTime createdAtUtc, DateTime? completedAtUtc, DateTime nowUtc)
        {
            var endAtUtc = completedAtUtc ?? nowUtc;
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
        public static bool HasCorrelation(
            string propertiesJson,
            string? paymentIntentRef,
            string? checkoutSessionRef,
            string? providerTransactionRef)
        {
            return Contains(propertiesJson, paymentIntentRef) ||
                   Contains(propertiesJson, checkoutSessionRef) ||
                   Contains(propertiesJson, providerTransactionRef);
        }

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
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(propertiesJson))
            {
                return false;
            }

            try
            {
                using var document = JsonDocument.Parse(propertiesJson);
                return ContainsJsonValue(document.RootElement, value);
            }
            catch (JsonException)
            {
                return propertiesJson.Contains(value, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static bool ContainsJsonValue(JsonElement element, string value)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        if (ContainsJsonValue(property.Value, value))
                        {
                            return true;
                        }
                    }

                    return false;

                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                    {
                        if (ContainsJsonValue(item, value))
                        {
                            return true;
                        }
                    }

                    return false;

                case JsonValueKind.String:
                    return string.Equals(element.GetString(), value, StringComparison.OrdinalIgnoreCase);

                case JsonValueKind.Number:
                    return string.Equals(element.GetRawText(), value, StringComparison.OrdinalIgnoreCase);

                default:
                    return false;
            }
        }
    }
}
