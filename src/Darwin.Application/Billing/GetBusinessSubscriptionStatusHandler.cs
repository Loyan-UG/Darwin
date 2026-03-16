using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Billing;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Billing;

/// <summary>
/// Resolves the latest subscription snapshot for a business.
/// </summary>
public sealed class GetBusinessSubscriptionStatusHandler
{
    private readonly IAppDbContext _db;

    public GetBusinessSubscriptionStatusHandler(IAppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<Result<BusinessSubscriptionStatusDto>> HandleAsync(Guid businessId, CancellationToken ct = default)
    {
        if (businessId == Guid.Empty)
        {
            return Result<BusinessSubscriptionStatusDto>.Fail("BusinessId is required.");
        }

        var snapshot = await (from subscription in _db.Set<BusinessSubscription>().AsNoTracking()
                              join plan in _db.Set<BillingPlan>().AsNoTracking() on subscription.BillingPlanId equals plan.Id
                              where !subscription.IsDeleted && !plan.IsDeleted && subscription.BusinessId == businessId
                              orderby subscription.StartedAtUtc descending
                              select new BusinessSubscriptionStatusDto
                              {
                                  HasSubscription = true,
                                  SubscriptionId = subscription.Id,
                                  RowVersion = subscription.RowVersion,
                                  Status = subscription.Status.ToString(),
                                  Provider = subscription.Provider,
                                  PlanCode = plan.Code,
                                  PlanName = plan.Name,
                                  UnitPriceMinor = subscription.UnitPriceMinor,
                                  Currency = subscription.Currency,
                                  StartedAtUtc = subscription.StartedAtUtc,
                                  CurrentPeriodEndUtc = subscription.CurrentPeriodEndUtc,
                                  TrialEndsAtUtc = subscription.TrialEndsAtUtc,
                                  CanceledAtUtc = subscription.CanceledAtUtc,
                                  CancelAtPeriodEnd = subscription.CancelAtPeriodEnd
                              })
                             .FirstOrDefaultAsync(ct)
                             .ConfigureAwait(false);

        if (snapshot is null)
        {
            return Result<BusinessSubscriptionStatusDto>.Ok(new BusinessSubscriptionStatusDto
            {
                HasSubscription = false,
                Status = "None"
            });
        }

        return Result<BusinessSubscriptionStatusDto>.Ok(snapshot);
    }
}
