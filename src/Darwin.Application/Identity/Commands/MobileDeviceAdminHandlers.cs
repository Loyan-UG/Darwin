using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Identity.Commands;

public sealed class ClearUserDevicePushTokenHandler
{
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
}

public sealed class DeactivateUserDeviceHandler
{
    private readonly IAppDbContext _db;
    private readonly IStringLocalizer<ValidationResource> _localizer;

    public DeactivateUserDeviceHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
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
}
