using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Common;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Billing;

/// <summary>
/// Returns provider invoice history for a business subscription support workspace.
/// </summary>
public sealed class GetBusinessSubscriptionInvoicesPageHandler
{
    private const int MaxPageSize = 200;

    private readonly IAppDbContext _db;

    public GetBusinessSubscriptionInvoicesPageHandler(IAppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<GetBusinessSubscriptionInvoicesPageDto> HandleAsync(
        Guid businessId,
        int page = 1,
        int pageSize = 20,
        string? query = null,
        BusinessSubscriptionInvoiceQueueFilter filter = BusinessSubscriptionInvoiceQueueFilter.All,
        string? culture = null,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;

        var invoices = from invoice in _db.Set<SubscriptionInvoice>().AsNoTracking()
                       join subscription in _db.Set<BusinessSubscription>().AsNoTracking()
                           on invoice.BusinessSubscriptionId equals subscription.Id
                       join plan in _db.Set<BillingPlan>().AsNoTracking()
                           on subscription.BillingPlanId equals plan.Id into planJoin
                       from plan in planJoin.DefaultIfEmpty()
                       where invoice.BusinessId == businessId
                             && !invoice.IsDeleted
                             && !subscription.IsDeleted
                       select new
                       {
                           Id = invoice.Id,
                           BusinessId = invoice.BusinessId,
                           BusinessSubscriptionId = invoice.BusinessSubscriptionId,
                           Provider = invoice.Provider,
                           ProviderInvoiceId = invoice.ProviderInvoiceId,
                           Status = invoice.Status,
                           TotalMinor = invoice.TotalMinor,
                           Currency = invoice.Currency,
                           IssuedAtUtc = invoice.IssuedAtUtc,
                           DueAtUtc = invoice.DueAtUtc,
                           PaidAtUtc = invoice.PaidAtUtc,
                           HostedInvoiceUrl = invoice.HostedInvoiceUrl,
                           PdfUrl = invoice.PdfUrl,
                           FailureReason = invoice.FailureReason,
                           PlanName = plan != null ? plan.Name : null,
                           PlanCode = plan != null ? plan.Code : null,
                           PlanFeaturesJson = plan != null ? plan.FeaturesJson : null
                       };

        var nowUtc = DateTime.UtcNow;
        invoices = filter switch
        {
            BusinessSubscriptionInvoiceQueueFilter.Open => invoices.Where(x => x.Status == SubscriptionInvoiceStatus.Open),
            BusinessSubscriptionInvoiceQueueFilter.Paid => invoices.Where(x => x.Status == SubscriptionInvoiceStatus.Paid),
            BusinessSubscriptionInvoiceQueueFilter.Draft => invoices.Where(x => x.Status == SubscriptionInvoiceStatus.Draft),
            BusinessSubscriptionInvoiceQueueFilter.Uncollectible => invoices.Where(x => x.Status == SubscriptionInvoiceStatus.Uncollectible),
            BusinessSubscriptionInvoiceQueueFilter.HostedLinkMissing => invoices.Where(x => string.IsNullOrWhiteSpace(x.HostedInvoiceUrl)),
            BusinessSubscriptionInvoiceQueueFilter.Stripe => invoices.Where(x => x.Provider == "Stripe"),
            BusinessSubscriptionInvoiceQueueFilter.Overdue => invoices.Where(x =>
                x.Status == SubscriptionInvoiceStatus.Open &&
                x.DueAtUtc.HasValue &&
                x.DueAtUtc.Value < nowUtc),
            BusinessSubscriptionInvoiceQueueFilter.PdfMissing => invoices.Where(x => string.IsNullOrWhiteSpace(x.PdfUrl)),
            _ => invoices
        };

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = QueryLikePattern.Contains(query);
            invoices = invoices.Where(x =>
                EF.Functions.Like(x.Provider, term, QueryLikePattern.EscapeCharacter) ||
                (x.ProviderInvoiceId != null && EF.Functions.Like(x.ProviderInvoiceId, term, QueryLikePattern.EscapeCharacter)) ||
                (x.PlanName != null && EF.Functions.Like(x.PlanName, term, QueryLikePattern.EscapeCharacter)) ||
                (x.PlanCode != null && EF.Functions.Like(x.PlanCode, term, QueryLikePattern.EscapeCharacter)) ||
                (x.FailureReason != null && EF.Functions.Like(x.FailureReason, term, QueryLikePattern.EscapeCharacter)));
        }

        var total = await invoices.CountAsync(ct).ConfigureAwait(false);
        var invoiceRows = await invoices
            .OrderByDescending(x => x.IssuedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new GetBusinessSubscriptionInvoicesPageDto
        {
            Items = invoiceRows
                .Select(x => new BusinessSubscriptionInvoiceListItemDto
                {
                    Id = x.Id,
                    BusinessId = x.BusinessId,
                    BusinessSubscriptionId = x.BusinessSubscriptionId,
                    Provider = x.Provider,
                    ProviderInvoiceId = x.ProviderInvoiceId,
                    Status = x.Status,
                    TotalMinor = x.TotalMinor,
                    Currency = x.Currency,
                    IssuedAtUtc = x.IssuedAtUtc,
                    DueAtUtc = x.DueAtUtc,
                    PaidAtUtc = x.PaidAtUtc,
                    HostedInvoiceUrl = x.HostedInvoiceUrl,
                    PdfUrl = x.PdfUrl,
                    FailureReason = x.FailureReason,
                    PlanName = x.PlanName is null
                        ? null
                        : BillingLocalizedTextResolver.ResolvePlanName(x.PlanName, x.PlanFeaturesJson, culture),
                    PlanCode = x.PlanCode,
                    IsOverdue = x.Status == SubscriptionInvoiceStatus.Open &&
                                x.DueAtUtc.HasValue &&
                                x.DueAtUtc.Value < nowUtc
                })
                .ToList(),
            Total = total
        };
    }
}

/// <summary>
/// Summary counters for business subscription invoice support.
/// </summary>
public sealed class GetBusinessSubscriptionInvoiceOpsSummaryHandler
{
    private readonly IAppDbContext _db;

    public GetBusinessSubscriptionInvoiceOpsSummaryHandler(IAppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<BusinessSubscriptionInvoiceOpsSummaryDto> HandleAsync(Guid businessId, CancellationToken ct = default)
    {
        var nowUtc = DateTime.UtcNow;
        var invoices = _db.Set<SubscriptionInvoice>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.BusinessId == businessId);

        return new BusinessSubscriptionInvoiceOpsSummaryDto
        {
            TotalCount = await invoices.CountAsync(ct).ConfigureAwait(false),
            OpenCount = await invoices.CountAsync(x => x.Status == SubscriptionInvoiceStatus.Open, ct).ConfigureAwait(false),
            PaidCount = await invoices.CountAsync(x => x.Status == SubscriptionInvoiceStatus.Paid, ct).ConfigureAwait(false),
            DraftCount = await invoices.CountAsync(x => x.Status == SubscriptionInvoiceStatus.Draft, ct).ConfigureAwait(false),
            UncollectibleCount = await invoices.CountAsync(x => x.Status == SubscriptionInvoiceStatus.Uncollectible, ct).ConfigureAwait(false),
            HostedLinkMissingCount = await invoices.CountAsync(x => x.HostedInvoiceUrl == null || x.HostedInvoiceUrl == string.Empty, ct).ConfigureAwait(false),
            StripeCount = await invoices.CountAsync(x => x.Provider == "Stripe", ct).ConfigureAwait(false),
            OverdueCount = await invoices.CountAsync(x =>
                x.Status == SubscriptionInvoiceStatus.Open &&
                x.DueAtUtc.HasValue &&
                x.DueAtUtc.Value < nowUtc, ct).ConfigureAwait(false),
            PdfMissingCount = await invoices.CountAsync(x => x.PdfUrl == null || x.PdfUrl == string.Empty, ct).ConfigureAwait(false)
        };
    }
}
