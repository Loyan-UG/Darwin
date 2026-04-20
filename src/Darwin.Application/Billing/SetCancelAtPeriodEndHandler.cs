using Darwin.Application.Abstractions.Persistence;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
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
    private readonly IStringLocalizer<ValidationResource> _localizer;

    public SetCancelAtPeriodEndHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
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
            return Result<BusinessSubscriptionStatusDto>.Fail(_localizer["BusinessAndSubscriptionIdentifiersRequired"]);
        }

        var entity = await _db.Set<Darwin.Domain.Entities.Billing.BusinessSubscription>()
            .SingleOrDefaultAsync(x => !x.IsDeleted && x.Id == subscriptionId && x.BusinessId == businessId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return Result<BusinessSubscriptionStatusDto>.Fail(_localizer["SubscriptionNotFound"]);
        }

        if (!entity.RowVersion.SequenceEqual(rowVersion ?? Array.Empty<byte>()))
        {
            return Result<BusinessSubscriptionStatusDto>.Fail(_localizer["SubscriptionConcurrencyConflict"]);
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
            return Result<BusinessSubscriptionStatusDto>.Fail(_localizer["SubscriptionConcurrencyConflict"]);
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
