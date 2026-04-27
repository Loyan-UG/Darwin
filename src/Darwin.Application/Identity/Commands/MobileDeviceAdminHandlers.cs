using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Identity.Commands;

public sealed class ClearUserDevicePushTokenHandler
{
    private const int MaxBatchSize = 200;

    private readonly IAppDbContext _db;
    private readonly IStringLocalizer<ValidationResource> _localizer;

    public ClearUserDevicePushTokenHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    public async Task<Result> HandleAsync(Guid id, byte[]? rowVersion = null, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            return Result.Fail(_localizer["DeviceRequired"]);
        }

        var device = await _db.Set<UserDevice>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct)
            .ConfigureAwait(false);

        if (device is null)
        {
            return Result.Fail(_localizer["DeviceNotFound"]);
        }

        if (rowVersion is not null && !device.RowVersion.SequenceEqual(rowVersion))
        {
            return Result.Fail(_localizer["DeviceConcurrencyConflict"]);
        }

        if (string.IsNullOrWhiteSpace(device.PushToken))
        {
            return Result.Ok();
        }

        device.PushToken = null;
        device.PushTokenUpdatedAtUtc = null;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Ok();
    }

    public async Task<Result<MobileDeviceBatchOperationResultDto>> HandleNotificationsDisabledBatchAsync(Guid? businessId = null, MobilePlatform? platform = null, int take = MaxBatchSize, CancellationToken ct = default)
    {
        var normalizedTake = Math.Clamp(take, 1, MaxBatchSize);
        var query = _db.Set<UserDevice>()
            .Where(x => !x.IsDeleted && x.IsActive && !x.NotificationsEnabled && x.PushToken != null && x.PushToken != string.Empty);

        if (platform.HasValue)
        {
            query = query.Where(x => x.Platform == platform.Value);
        }

        if (businessId.HasValue)
        {
            query = query.Where(x => _db.Set<BusinessMember>()
                .Any(member => !member.IsDeleted && member.IsActive && member.BusinessId == businessId.Value && member.UserId == x.UserId));
        }

        var devices = await query
            .OrderBy(x => x.LastSeenAtUtc ?? x.CreatedAtUtc)
            .Take(normalizedTake)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var device in devices)
        {
            device.PushToken = null;
            device.PushTokenUpdatedAtUtc = null;
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result<MobileDeviceBatchOperationResultDto>.Ok(new MobileDeviceBatchOperationResultDto
        {
            AffectedCount = devices.Count
        });
    }
}

public sealed class DeactivateUserDeviceHandler
{
    private const int MaxBatchSize = 200;
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromDays(30);

    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly IStringLocalizer<ValidationResource> _localizer;

    public DeactivateUserDeviceHandler(IAppDbContext db, IClock clock, IStringLocalizer<ValidationResource> localizer)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    public async Task<Result> HandleAsync(Guid id, byte[]? rowVersion = null, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            return Result.Fail(_localizer["DeviceRequired"]);
        }

        var device = await _db.Set<UserDevice>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct)
            .ConfigureAwait(false);

        if (device is null)
        {
            return Result.Fail(_localizer["DeviceNotFound"]);
        }

        if (rowVersion is not null && !device.RowVersion.SequenceEqual(rowVersion))
        {
            return Result.Fail(_localizer["DeviceConcurrencyConflict"]);
        }

        if (!device.IsActive && string.IsNullOrWhiteSpace(device.PushToken) && !device.NotificationsEnabled)
        {
            return Result.Ok();
        }

        device.IsActive = false;
        device.NotificationsEnabled = false;
        device.PushToken = null;
        device.PushTokenUpdatedAtUtc = null;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Ok();
    }

    public async Task<Result<MobileDeviceBatchOperationResultDto>> HandleStaleBatchAsync(Guid? businessId = null, MobilePlatform? platform = null, int take = MaxBatchSize, CancellationToken ct = default)
    {
        var normalizedTake = Math.Clamp(take, 1, MaxBatchSize);
        var staleCutoffUtc = _clock.UtcNow.Subtract(StaleThreshold);
        var query = _db.Set<UserDevice>()
            .Where(x => !x.IsDeleted && x.IsActive && (!x.LastSeenAtUtc.HasValue || x.LastSeenAtUtc < staleCutoffUtc));

        if (platform.HasValue)
        {
            query = query.Where(x => x.Platform == platform.Value);
        }

        if (businessId.HasValue)
        {
            query = query.Where(x => _db.Set<BusinessMember>()
                .Any(member => !member.IsDeleted && member.IsActive && member.BusinessId == businessId.Value && member.UserId == x.UserId));
        }

        var devices = await query
            .OrderBy(x => x.LastSeenAtUtc ?? x.CreatedAtUtc)
            .Take(normalizedTake)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var device in devices)
        {
            device.IsActive = false;
            device.NotificationsEnabled = false;
            device.PushToken = null;
            device.PushTokenUpdatedAtUtc = null;
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result<MobileDeviceBatchOperationResultDto>.Ok(new MobileDeviceBatchOperationResultDto
        {
            AffectedCount = devices.Count
        });
    }
}

public sealed class MobileDeviceBatchOperationResultDto
{
    public int AffectedCount { get; init; }
}
