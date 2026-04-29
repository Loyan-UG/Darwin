using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Billing;

/// <summary>
/// Retrieves available billing plans for operator-facing subscription workflows.
/// </summary>
public sealed class GetBillingPlansHandler
{
    private readonly IAppDbContext _db;

    public GetBillingPlansHandler(IAppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<GetBillingPlansDto> HandleAsync(bool activeOnly, string? culture = null, CancellationToken ct = default)
    {
        var query = _db.Set<BillingPlan>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (activeOnly)
        {
            query = query.Where(x => x.IsActive);
        }

        var planRows = await query
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
                FeaturesJson = x.FeaturesJson
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new GetBillingPlansDto
        {
            Items = planRows
                .Select(x => new BillingPlanSummaryDto
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = BillingLocalizedTextResolver.ResolvePlanName(x.Name, x.FeaturesJson, culture),
                    Description = BillingLocalizedTextResolver.ResolvePlanDescription(x.Description, x.FeaturesJson, culture),
                    PriceMinor = x.PriceMinor,
                    Currency = x.Currency,
                    Interval = x.Interval.ToString(),
                    IntervalCount = x.IntervalCount,
                    TrialDays = x.TrialDays,
                    IsActive = x.IsActive
                })
                .ToList()
        };
    }
}
