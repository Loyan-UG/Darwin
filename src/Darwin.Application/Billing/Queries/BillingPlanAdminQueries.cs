using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Common;
using Darwin.Application.Billing.DTOs;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Billing.Queries;

public sealed class GetBillingPlansAdminPageHandler
{
    private const int MaxPageSize = 200;

    private readonly IAppDbContext _db;

    public GetBillingPlansAdminPageHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<(List<BillingPlanListItemDto> Items, int Total)> HandleAsync(
        int page,
        int pageSize,
        string? query = null,
        BillingPlanQueueFilter filter = BillingPlanQueueFilter.All,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;

        var baseQuery = _db.Set<BillingPlan>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = QueryLikePattern.Contains(query);
            baseQuery = baseQuery.Where(x =>
                EF.Functions.Like(x.Code, term, QueryLikePattern.EscapeCharacter) ||
                EF.Functions.Like(x.Name, term, QueryLikePattern.EscapeCharacter) ||
                (x.Description != null && EF.Functions.Like(x.Description, term, QueryLikePattern.EscapeCharacter)));
        }

        baseQuery = filter switch
        {
            BillingPlanQueueFilter.Active => baseQuery.Where(x => x.IsActive),
            BillingPlanQueueFilter.Inactive => baseQuery.Where(x => !x.IsActive),
            BillingPlanQueueFilter.Trial => baseQuery.Where(x => x.TrialDays != null && x.TrialDays > 0),
            BillingPlanQueueFilter.MissingFeatures => baseQuery.Where(x => x.FeaturesJson == "{}" || x.FeaturesJson == "[]" || x.FeaturesJson == string.Empty),
            BillingPlanQueueFilter.InUse => baseQuery.Where(x => _db.Set<BusinessSubscription>().Any(s => !s.IsDeleted && s.BillingPlanId == x.Id)),
            _ => baseQuery
        };

        var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);

        var planRows = await baseQuery
            .OrderBy(x => x.PriceMinor)
            .ThenBy(x => x.Name)
            .Select(x => new
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Description = x.Description,
                PriceMinor = x.PriceMinor,
                Currency = x.Currency,
                Interval = x.Interval,
                IntervalCount = x.IntervalCount,
                TrialDays = x.TrialDays,
                IsActive = x.IsActive,
                HasFeatures = x.FeaturesJson != "{}" && x.FeaturesJson != "[]" && x.FeaturesJson != string.Empty,
                FeaturesJson = x.FeaturesJson,
                ActiveSubscriptionCount = _db.Set<BusinessSubscription>().Count(s =>
                    !s.IsDeleted &&
                    s.BillingPlanId == x.Id &&
                    (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing || s.Status == SubscriptionStatus.PastDue)),
                ModifiedAtUtc = x.ModifiedAtUtc,
                RowVersion = x.RowVersion
            })
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var items = planRows
            .Select(x => new BillingPlanListItemDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = BillingLocalizedTextResolver.ResolvePlanName(x.Name, x.FeaturesJson),
                Description = BillingLocalizedTextResolver.ResolvePlanDescription(x.Description, x.FeaturesJson),
                PriceMinor = x.PriceMinor,
                Currency = x.Currency,
                Interval = x.Interval,
                IntervalCount = x.IntervalCount,
                TrialDays = x.TrialDays,
                IsActive = x.IsActive,
                HasFeatures = x.HasFeatures,
                ActiveSubscriptionCount = x.ActiveSubscriptionCount,
                ModifiedAtUtc = x.ModifiedAtUtc,
                RowVersion = x.RowVersion
            })
            .ToList();

        return (items, total);
    }
}

public sealed class GetBillingPlanOpsSummaryHandler
{
    private readonly IAppDbContext _db;

    public GetBillingPlanOpsSummaryHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<BillingPlanOpsSummaryDto> HandleAsync(CancellationToken ct = default)
    {
        var plans = _db.Set<BillingPlan>().AsNoTracking().Where(x => !x.IsDeleted);

        var planSummary = await plans
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalCount = g.Count(),
                ActiveCount = g.Count(x => x.IsActive),
                InactiveCount = g.Count(x => !x.IsActive),
                TrialCount = g.Count(x => x.TrialDays != null && x.TrialDays > 0),
                MissingFeaturesCount = g.Count(x => x.FeaturesJson == "{}" || x.FeaturesJson == "[]" || x.FeaturesJson == string.Empty)
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return new BillingPlanOpsSummaryDto
        {
            TotalCount = planSummary?.TotalCount ?? 0,
            ActiveCount = planSummary?.ActiveCount ?? 0,
            InactiveCount = planSummary?.InactiveCount ?? 0,
            TrialCount = planSummary?.TrialCount ?? 0,
            MissingFeaturesCount = planSummary?.MissingFeaturesCount ?? 0,
            InUseCount = await _db.Set<BusinessSubscription>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .Select(x => x.BillingPlanId)
                .Distinct()
                .CountAsync(ct)
                .ConfigureAwait(false)
        };
    }
}

public sealed class GetBillingPlanForEditHandler
{
    private readonly IAppDbContext _db;

    public GetBillingPlanForEditHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<BillingPlanEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Set<BillingPlan>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Id == id)
            .Select(x => new BillingPlanEditDto
            {
                Id = x.Id,
                RowVersion = x.RowVersion,
                Code = x.Code,
                Name = x.Name,
                Description = x.Description,
                PriceMinor = x.PriceMinor,
                Currency = x.Currency,
                Interval = x.Interval,
                IntervalCount = x.IntervalCount,
                TrialDays = x.TrialDays,
                IsActive = x.IsActive,
                FeaturesJson = x.FeaturesJson
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }
}
