using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
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
    private readonly IClock _clock;

    public GetBusinessSubscriptionInvoicesPageHandler(IAppDbContext db, IClock clock)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
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

        var nowUtc = _clock.UtcNow;
        invoices = filter switch
        {
            BusinessSubscriptionInvoiceQueueFilter.Open => invoices.Where(x => x.Status == SubscriptionInvoiceStatus.Open),
            BusinessSubscriptionInvoiceQueueFilter.Paid => invoices.Where(x => x.Status == SubscriptionInvoiceStatus.Paid),
            BusinessSubscriptionInvoiceQueueFilter.Draft => invoices.Where(x => x.Status == SubscriptionInvoiceStatus.Draft),
            BusinessSubscriptionInvoiceQueueFilter.Uncollectible => invoices.Where(x => x.Status == SubscriptionInvoiceStatus.Uncollectible),
            BusinessSubscriptionInvoiceQueueFilter.HostedLinkMissing => invoices.Where(x =>
                x.HostedInvoiceUrl == null ||
                x.HostedInvoiceUrl.Trim() == string.Empty),
            BusinessSubscriptionInvoiceQueueFilter.Stripe => invoices.Where(x => x.Provider == "Stripe"),
            BusinessSubscriptionInvoiceQueueFilter.Overdue => invoices.Where(x =>
                x.Status == SubscriptionInvoiceStatus.Open &&
                x.DueAtUtc.HasValue &&
                x.DueAtUtc.Value < nowUtc),
            BusinessSubscriptionInvoiceQueueFilter.PdfMissing => invoices.Where(x =>
                x.PdfUrl == null ||
                x.PdfUrl.Trim() == string.Empty),
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
                    FailureReason = OperatorDisplayTextSanitizer.SanitizeFailureText(x.FailureReason),
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
    private readonly IClock _clock;

    public GetBusinessSubscriptionInvoiceOpsSummaryHandler(IAppDbContext db, IClock clock)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<BusinessSubscriptionInvoiceOpsSummaryDto> HandleAsync(Guid businessId, CancellationToken ct = default)
    {
        var nowUtc = _clock.UtcNow;
        var invoices = _db.Set<SubscriptionInvoice>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.BusinessId == businessId);

        return await invoices
            .GroupBy(_ => 1)
            .Select(g => new BusinessSubscriptionInvoiceOpsSummaryDto
            {
                TotalCount = g.Count(),
                OpenCount = g.Count(x => x.Status == SubscriptionInvoiceStatus.Open),
                PaidCount = g.Count(x => x.Status == SubscriptionInvoiceStatus.Paid),
                DraftCount = g.Count(x => x.Status == SubscriptionInvoiceStatus.Draft),
                UncollectibleCount = g.Count(x => x.Status == SubscriptionInvoiceStatus.Uncollectible),
                HostedLinkMissingCount = g.Count(x =>
                    x.HostedInvoiceUrl == null ||
                    x.HostedInvoiceUrl.Trim() == string.Empty),
                StripeCount = g.Count(x => x.Provider == "Stripe"),
                OverdueCount = g.Count(x =>
                    x.Status == SubscriptionInvoiceStatus.Open &&
                    x.DueAtUtc.HasValue &&
                    x.DueAtUtc.Value < nowUtc),
                PdfMissingCount = g.Count(x =>
                    x.PdfUrl == null ||
                    x.PdfUrl.Trim() == string.Empty)
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false) ?? new BusinessSubscriptionInvoiceOpsSummaryDto();
    }
}
