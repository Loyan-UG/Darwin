using Darwin.Application.Abstractions.Persistence;
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
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var invoices = from invoice in _db.Set<SubscriptionInvoice>().AsNoTracking()
                       join subscription in _db.Set<BusinessSubscription>().AsNoTracking()
                           on invoice.BusinessSubscriptionId equals subscription.Id
                       join plan in _db.Set<BillingPlan>().AsNoTracking()
                           on subscription.BillingPlanId equals plan.Id into planJoin
                       from plan in planJoin.DefaultIfEmpty()
                       where invoice.BusinessId == businessId
                             && !invoice.IsDeleted
                             && !subscription.IsDeleted
                       select new BusinessSubscriptionInvoiceListItemDto
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
                           PlanCode = plan != null ? plan.Code : null
                       };

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
                x.DueAtUtc.Value < DateTime.UtcNow),
            BusinessSubscriptionInvoiceQueueFilter.PdfMissing => invoices.Where(x => string.IsNullOrWhiteSpace(x.PdfUrl)),
            _ => invoices
        };

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            invoices = invoices.Where(x =>
                x.Provider.Contains(term) ||
                (x.ProviderInvoiceId != null && x.ProviderInvoiceId.Contains(term)) ||
                (x.PlanName != null && x.PlanName.Contains(term)) ||
                (x.PlanCode != null && x.PlanCode.Contains(term)) ||
                (x.FailureReason != null && x.FailureReason.Contains(term)));
        }

        var total = await invoices.CountAsync(ct).ConfigureAwait(false);
        var items = await invoices
            .OrderByDescending(x => x.IssuedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new GetBusinessSubscriptionInvoicesPageDto
        {
            Items = items,
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
                x.DueAtUtc.Value < DateTime.UtcNow, ct).ConfigureAwait(false),
            PdfMissingCount = await invoices.CountAsync(x => x.PdfUrl == null || x.PdfUrl == string.Empty, ct).ConfigureAwait(false)
        };
    }
}
