using Darwin.Application.Abstractions.Persistence;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Billing;

/// <summary>
/// Updates cancel-at-period-end flag for business subscription with ownership and concurrency checks.
/// </summary>
public sealed class SetCancelAtPeriodEndHandler
{
    private readonly IAppDbContext _db;

    public SetCancelAtPeriodEndHandler(IAppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<Result<BusinessSubscriptionStatusDto>> HandleAsync(
        Guid businessId,
        Guid subscriptionId,
        bool cancelAtPeriodEnd,
        byte[] rowVersion,
        CancellationToken ct = default)
    {
        if (businessId == Guid.Empty || subscriptionId == Guid.Empty)
        {
            return Result<BusinessSubscriptionStatusDto>.Fail("Business and subscription identifiers are required.");
        }

        var entity = await _db.Set<Darwin.Domain.Entities.Billing.BusinessSubscription>()
            .SingleOrDefaultAsync(x => !x.IsDeleted && x.Id == subscriptionId && x.BusinessId == businessId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return Result<BusinessSubscriptionStatusDto>.Fail("Subscription not found.");
        }

        if (!entity.RowVersion.SequenceEqual(rowVersion ?? Array.Empty<byte>()))
        {
            return Result<BusinessSubscriptionStatusDto>.Fail("Subscription was updated by another user. Refresh and try again.");
        }

        entity.CancelAtPeriodEnd = cancelAtPeriodEnd;

        if (!cancelAtPeriodEnd)
        {
            entity.CanceledAtUtc = null;
        }
        else if (!entity.CanceledAtUtc.HasValue)
        {
            entity.CanceledAtUtc = DateTime.UtcNow;
        }

        try
        {
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<BusinessSubscriptionStatusDto>.Fail("Subscription was updated by another user. Refresh and try again.");
        }

        return Result<BusinessSubscriptionStatusDto>.Ok(new BusinessSubscriptionStatusDto
        {
            HasSubscription = true,
            SubscriptionId = entity.Id,
            RowVersion = entity.RowVersion,
            Status = entity.Status.ToString(),
            Provider = entity.Provider,
            PlanCode = string.Empty,
            PlanName = string.Empty,
            UnitPriceMinor = entity.UnitPriceMinor,
            Currency = entity.Currency,
            StartedAtUtc = entity.StartedAtUtc,
            CurrentPeriodEndUtc = entity.CurrentPeriodEndUtc,
            TrialEndsAtUtc = entity.TrialEndsAtUtc,
            CanceledAtUtc = entity.CanceledAtUtc,
            CancelAtPeriodEnd = entity.CancelAtPeriodEnd
        });
    }
}
