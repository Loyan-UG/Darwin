using Darwin.Application.Abstractions.Persistence;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
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
    private readonly IStringLocalizer<ValidationResource> _localizer;

    public CreateSubscriptionCheckoutIntentHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    public async Task<Result> ValidateAsync(Guid businessId, Guid planId, CancellationToken ct = default)
    {
        if (businessId == Guid.Empty || planId == Guid.Empty)
        {
            return Result.Fail(_localizer["BusinessAndPlanIdentifiersRequired"]);
        }

        var hasPlan = await _db.Set<Darwin.Domain.Entities.Billing.BillingPlan>()
            .AsNoTracking()
            .AnyAsync(x => !x.IsDeleted && x.IsActive && x.Id == planId, ct)
            .ConfigureAwait(false);

        if (!hasPlan)
        {
            return Result.Fail(_localizer["SelectedBillingPlanUnavailable"]);
        }

        var hasBusiness = await _db.Set<Darwin.Domain.Entities.Businesses.Business>()
            .AsNoTracking()
            .AnyAsync(x => !x.IsDeleted && x.Id == businessId, ct)
            .ConfigureAwait(false);

        if (!hasBusiness)
        {
            return Result.Fail(_localizer["BusinessNotFound"]);
        }

        return Result.Ok();
    }
}
