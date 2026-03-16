using Darwin.Application.Abstractions.Persistence;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Billing;

/// <summary>
/// Validates checkout-intent prerequisites for subscription upgrades.
/// </summary>
public sealed class CreateSubscriptionCheckoutIntentHandler
{
    private readonly IAppDbContext _db;

    public CreateSubscriptionCheckoutIntentHandler(IAppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<Result> ValidateAsync(Guid businessId, Guid planId, CancellationToken ct = default)
    {
        if (businessId == Guid.Empty || planId == Guid.Empty)
        {
            return Result.Fail("Business and plan identifiers are required.");
        }

        var hasPlan = await _db.Set<Darwin.Domain.Entities.Billing.BillingPlan>()
            .AsNoTracking()
            .AnyAsync(x => !x.IsDeleted && x.IsActive && x.Id == planId, ct)
            .ConfigureAwait(false);

        if (!hasPlan)
        {
            return Result.Fail("Selected billing plan is not available.");
        }

        var hasBusiness = await _db.Set<Darwin.Domain.Entities.Businesses.Business>()
            .AsNoTracking()
            .AnyAsync(x => !x.IsDeleted && x.Id == businessId, ct)
            .ConfigureAwait(false);

        if (!hasBusiness)
        {
            return Result.Fail("Business not found.");
        }

        return Result.Ok();
    }
}
