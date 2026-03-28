using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Identity.Commands;

public sealed class ClearUserDevicePushTokenHandler
{
    private readonly IAppDbContext _db;

    public ClearUserDevicePushTokenHandler(IAppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<Result> HandleAsync(Guid id, byte[]? rowVersion = null, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            return Result.Fail("Device is required.");
        }

        var device = await _db.Set<UserDevice>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct)
            .ConfigureAwait(false);

        if (device is null)
        {
            return Result.Fail("Device not found.");
        }

        if (rowVersion is not null && !device.RowVersion.SequenceEqual(rowVersion))
        {
            return Result.Fail("Concurrency conflict. The device record was changed by another process.");
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

    public DeactivateUserDeviceHandler(IAppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<Result> HandleAsync(Guid id, byte[]? rowVersion = null, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            return Result.Fail("Device is required.");
        }

        var device = await _db.Set<UserDevice>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct)
            .ConfigureAwait(false);

        if (device is null)
        {
            return Result.Fail("Device not found.");
        }

        if (rowVersion is not null && !device.RowVersion.SequenceEqual(rowVersion))
        {
            return Result.Fail("Concurrency conflict. The device record was changed by another process.");
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
