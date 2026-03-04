using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Identity.Commands;

/// <summary>
/// Upserts a mobile device installation for an authenticated user.
/// This handler is the single write path for push registration metadata.
/// </summary>
public sealed class RegisterOrUpdateUserDeviceHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public RegisterOrUpdateUserDeviceHandler(IAppDbContext db, IClock clock)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>
    /// Registers a new device record or updates an existing one by (UserId, DeviceId).
    /// </summary>
    public async Task<Result<RegisterUserDeviceResultDto>> HandleAsync(RegisterUserDeviceDto dto, CancellationToken ct = default)
    {
        if (dto is null)
        {
            return Result<RegisterUserDeviceResultDto>.Fail("Request payload is required.");
        }

        if (dto.UserId == Guid.Empty)
        {
            return Result<RegisterUserDeviceResultDto>.Fail("UserId is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.DeviceId))
        {
            return Result<RegisterUserDeviceResultDto>.Fail("DeviceId is required.");
        }

        var normalizedDeviceId = dto.DeviceId.Trim();
        if (normalizedDeviceId.Length > 128)
        {
            return Result<RegisterUserDeviceResultDto>.Fail("DeviceId is too long.");
        }

        var normalizedPushToken = string.IsNullOrWhiteSpace(dto.PushToken) ? null : dto.PushToken.Trim();
        if (normalizedPushToken is not null && normalizedPushToken.Length > 512)
        {
            return Result<RegisterUserDeviceResultDto>.Fail("PushToken is too long.");
        }

        var normalizedAppVersion = string.IsNullOrWhiteSpace(dto.AppVersion) ? null : dto.AppVersion.Trim();
        if (normalizedAppVersion is not null && normalizedAppVersion.Length > 64)
        {
            return Result<RegisterUserDeviceResultDto>.Fail("AppVersion is too long.");
        }

        var normalizedDeviceModel = string.IsNullOrWhiteSpace(dto.DeviceModel) ? null : dto.DeviceModel.Trim();
        if (normalizedDeviceModel is not null && normalizedDeviceModel.Length > 128)
        {
            return Result<RegisterUserDeviceResultDto>.Fail("DeviceModel is too long.");
        }

        var now = _clock.UtcNow;

        var existing = await _db.Set<UserDevice>()
            .FirstOrDefaultAsync(x => x.UserId == dto.UserId && x.DeviceId == normalizedDeviceId, ct)
            .ConfigureAwait(false);

        if (existing is null)
        {
            var device = new UserDevice
            {
                UserId = dto.UserId,
                DeviceId = normalizedDeviceId,
                Platform = dto.Platform,
                PushToken = normalizedPushToken,
                PushTokenUpdatedAtUtc = normalizedPushToken is null ? null : now,
                NotificationsEnabled = dto.NotificationsEnabled,
                LastSeenAtUtc = now,
                AppVersion = normalizedAppVersion,
                DeviceModel = normalizedDeviceModel,
                IsActive = true
            };

            _db.Set<UserDevice>().Add(device);
        }
        else
        {
            if (!string.Equals(existing.PushToken, normalizedPushToken, StringComparison.Ordinal))
            {
                existing.PushTokenUpdatedAtUtc = normalizedPushToken is null ? existing.PushTokenUpdatedAtUtc : now;
            }

            existing.Platform = dto.Platform;
            existing.PushToken = normalizedPushToken;
            existing.NotificationsEnabled = dto.NotificationsEnabled;
            existing.LastSeenAtUtc = now;
            existing.AppVersion = normalizedAppVersion;
            existing.DeviceModel = normalizedDeviceModel;
            existing.IsActive = true;
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result<RegisterUserDeviceResultDto>.Ok(new RegisterUserDeviceResultDto
        {
            DeviceId = normalizedDeviceId,
            RegisteredAtUtc = now
        });
    }
}
