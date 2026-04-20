using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Identity;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Identity.Commands;

/// <summary>
/// Upserts a mobile device installation for an authenticated user.
/// This handler is the single write path for push registration metadata.
/// </summary>
public sealed class RegisterOrUpdateUserDeviceHandler
{
    private const string SnapshotMetadataKeyDeviceHeartbeatCount = "deviceHeartbeatCount";
    private const string SnapshotMetadataKeyLastDeviceHeartbeatAtUtc = "lastDeviceHeartbeatAtUtc";

    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly IStringLocalizer<ValidationResource> _localizer;

    public RegisterOrUpdateUserDeviceHandler(IAppDbContext db, IClock clock, IStringLocalizer<ValidationResource> localizer)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    /// <summary>
    /// Registers a new device record or updates an existing one by (UserId, DeviceId).
    /// </summary>
    public async Task<Result<RegisterUserDeviceResultDto>> HandleAsync(RegisterUserDeviceDto dto, CancellationToken ct = default)
    {
        if (dto is null)
        {
            return Result<RegisterUserDeviceResultDto>.Fail(_localizer["RequestPayloadRequired"]);
        }

        if (dto.UserId == Guid.Empty)
        {
            return Result<RegisterUserDeviceResultDto>.Fail(_localizer["UserIdRequired"]);
        }

        if (string.IsNullOrWhiteSpace(dto.DeviceId))
        {
            return Result<RegisterUserDeviceResultDto>.Fail(_localizer["DeviceIdRequired"]);
        }

        var normalizedDeviceId = dto.DeviceId.Trim();
        if (normalizedDeviceId.Length > 128)
        {
            return Result<RegisterUserDeviceResultDto>.Fail(_localizer["DeviceIdTooLong"]);
        }

        var normalizedPushToken = string.IsNullOrWhiteSpace(dto.PushToken) ? null : dto.PushToken.Trim();
        if (normalizedPushToken is not null && normalizedPushToken.Length > 512)
        {
            return Result<RegisterUserDeviceResultDto>.Fail(_localizer["PushTokenTooLong"]);
        }

        var normalizedAppVersion = string.IsNullOrWhiteSpace(dto.AppVersion) ? null : dto.AppVersion.Trim();
        if (normalizedAppVersion is not null && normalizedAppVersion.Length > 64)
        {
            return Result<RegisterUserDeviceResultDto>.Fail(_localizer["AppVersionTooLong"]);
        }

        var normalizedDeviceModel = string.IsNullOrWhiteSpace(dto.DeviceModel) ? null : dto.DeviceModel.Trim();
        if (normalizedDeviceModel is not null && normalizedDeviceModel.Length > 128)
        {
            return Result<RegisterUserDeviceResultDto>.Fail(_localizer["DeviceModelTooLong"]);
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

        await UpsertEngagementSnapshotAsync(dto.UserId, now, ct).ConfigureAwait(false);

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result<RegisterUserDeviceResultDto>.Ok(new RegisterUserDeviceResultDto
        {
            DeviceId = normalizedDeviceId,
            RegisteredAtUtc = now
        });
    }

    /// <summary>
    /// Maintains minimal engagement projection on each authenticated device heartbeat.
    /// This provides a reliable trigger/measurement baseline for inactive-reminder workflows.
    /// </summary>
    private async Task UpsertEngagementSnapshotAsync(Guid userId, DateTime nowUtc, CancellationToken ct)
    {
        var snapshot = await _db.Set<UserEngagementSnapshot>()
            .FirstOrDefaultAsync(x => x.UserId == userId, ct)
            .ConfigureAwait(false);

        var metadata = DeserializeSnapshotMetadata(snapshot?.SnapshotJson);
        var heartbeatCount = TryGetLong(metadata, ReminderMetadataKeys.DeviceHeartbeatCount) + 1;

        metadata[ReminderMetadataKeys.DeviceHeartbeatCount] = heartbeatCount;
        metadata[ReminderMetadataKeys.LastDeviceHeartbeatAtUtc] = nowUtc;

        if (snapshot is null)
        {
            snapshot = new UserEngagementSnapshot
            {
                UserId = userId,
                LastActivityAtUtc = nowUtc,
                EventCount = 1,
                EngagementScore30d = 1,
                CalculatedAtUtc = nowUtc,
                SnapshotJson = SerializeSnapshotMetadata(metadata)
            };

            _db.Set<UserEngagementSnapshot>().Add(snapshot);
            return;
        }

        snapshot.LastActivityAtUtc = nowUtc;
        snapshot.CalculatedAtUtc = nowUtc;
        snapshot.EventCount = Math.Max(0, snapshot.EventCount) + 1;
        snapshot.EngagementScore30d = ComputeEngagementScore(snapshot.EventCount, snapshot.LastLoginAtUtc, nowUtc);
        snapshot.SnapshotJson = SerializeSnapshotMetadata(metadata);
    }

    /// <summary>
    /// Computes a lightweight engagement score with recency bias for segmentation.
    /// </summary>
    private static int ComputeEngagementScore(long eventCount, DateTime? lastLoginAtUtc, DateTime nowUtc)
    {
        var cappedEventScore = (int)Math.Min(80, Math.Max(0, eventCount));

        if (!lastLoginAtUtc.HasValue)
        {
            return cappedEventScore;
        }

        var inactiveDays = Math.Max(0, (int)(nowUtc - lastLoginAtUtc.Value).TotalDays);
        var recencyBonus = Math.Max(0, 20 - inactiveDays);
        return Math.Clamp(cappedEventScore + recencyBonus, 0, 100);
    }

    /// <summary>
    /// Deserializes snapshot metadata JSON safely and normalizes to mutable dictionary.
    /// </summary>
    private static Dictionary<string, object?> DeserializeSnapshotMetadata(string? snapshotJson)
    {
        if (string.IsNullOrWhiteSpace(snapshotJson))
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(snapshotJson);
            var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            if (payload is null)
            {
                return result;
            }

            foreach (var entry in payload)
            {
                result[entry.Key] = entry.Value.ValueKind switch
                {
                    JsonValueKind.String => entry.Value.GetString(),
                    JsonValueKind.Number when entry.Value.TryGetInt64(out var longValue) => longValue,
                    JsonValueKind.Number when entry.Value.TryGetDouble(out var doubleValue) => doubleValue,
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => entry.Value.GetRawText()
                };
            }

            return result;
        }
        catch
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Serializes snapshot metadata with relaxed escaping for compact, readable payloads.
    /// </summary>
    private static string SerializeSnapshotMetadata(Dictionary<string, object?> metadata)
    {
        return JsonSerializer.Serialize(metadata, new JsonSerializerOptions
        {
            WriteIndented = false
        });
    }

    /// <summary>
    /// Parses a metadata value as <see cref="long"/> with safe fallback.
    /// </summary>
    private static long TryGetLong(IReadOnlyDictionary<string, object?> metadata, string key)
    {
        if (!metadata.TryGetValue(key, out var value) || value is null)
        {
            return 0;
        }

        return value switch
        {
            long longValue => longValue,
            int intValue => intValue,
            string text when long.TryParse(text, out var parsed) => parsed,
            _ => 0
        };
    }
}
