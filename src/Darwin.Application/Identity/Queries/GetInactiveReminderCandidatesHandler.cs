using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application;
using Darwin.Application.Identity;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Identity.Queries;

/// <summary>
/// Selects users who are eligible for inactive reminder dispatch,
/// including cooldown suppression checks.
/// </summary>
public sealed class GetInactiveReminderCandidatesHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly IStringLocalizer<ValidationResource> _localizer;

    public GetInactiveReminderCandidatesHandler(IAppDbContext db, IClock clock, IStringLocalizer<ValidationResource> localizer)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    /// <summary>
    /// Returns candidate users ordered by oldest activity first.
    /// </summary>
    public async Task<Result<IReadOnlyList<InactiveReminderCandidateDto>>> HandleAsync(
        GetInactiveReminderCandidatesDto request,
        CancellationToken ct = default)
    {
        if (request is null)
        {
            return Result<IReadOnlyList<InactiveReminderCandidateDto>>.Fail(_localizer["RequestPayloadRequired"]);
        }

        if (request.InactiveThresholdDays <= 0)
        {
            return Result<IReadOnlyList<InactiveReminderCandidateDto>>.Fail(_localizer["InactiveThresholdDaysGreaterThanZero"]);
        }

        if (request.CooldownHours < 0)
        {
            return Result<IReadOnlyList<InactiveReminderCandidateDto>>.Fail(_localizer["CooldownHoursMustNotBeNegative"]);
        }

        var maxItems = Math.Clamp(request.MaxItems, 1, 1000);
        var nowUtc = _clock.UtcNow;
        var inactiveCutoffUtc = nowUtc.AddDays(-request.InactiveThresholdDays);
        var cooldown = TimeSpan.FromHours(request.CooldownHours);

        var snapshots = await (
                from snapshot in _db.Set<UserEngagementSnapshot>().AsNoTracking()
                join user in _db.Set<User>().AsNoTracking() on snapshot.UserId equals user.Id
                where !snapshot.IsDeleted &&
                      !user.IsDeleted &&
                      user.IsActive &&
                      snapshot.LastActivityAtUtc.HasValue &&
                      snapshot.LastActivityAtUtc.Value <= inactiveCutoffUtc
                select snapshot)
            .OrderBy(x => x.LastActivityAtUtc)
            .Take(maxItems * 4)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (snapshots.Count == 0)
        {
            return Result<IReadOnlyList<InactiveReminderCandidateDto>>.Ok(Array.Empty<InactiveReminderCandidateDto>());
        }

        var userIds = snapshots.Select(x => x.UserId).Distinct().ToList();
        var pushDevices = await _db.Set<UserDevice>()
            .AsNoTracking()
            .Where(x => userIds.Contains(x.UserId)
                        && !x.IsDeleted
                        && x.IsActive
                        && x.NotificationsEnabled
                        && x.PushToken != null
                        && x.PushToken != string.Empty)
            .OrderByDescending(x => x.LastSeenAtUtc)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var destinations = pushDevices
            .GroupBy(x => x.UserId)
            .Select(group => group.First())
            .ToDictionary(x => x.UserId);

        var result = new List<InactiveReminderCandidateDto>(capacity: maxItems);

        foreach (var snapshot in snapshots)
        {
            if (!snapshot.LastActivityAtUtc.HasValue)
            {
                continue;
            }

            if (!destinations.TryGetValue(snapshot.UserId, out var destination)
                || string.IsNullOrWhiteSpace(destination.DeviceId)
                || string.IsNullOrWhiteSpace(destination.PushToken))
            {
                continue;
            }

            var metadata = DeserializeMetadata(snapshot.SnapshotJson);
            var lastReminderSentAtUtc = TryReadDateTimeUtc(metadata, ReminderMetadataKeys.LastInactiveReminderSentAtUtc);
            var cooldownEndsAtUtc = lastReminderSentAtUtc?.Add(cooldown);
            var isSuppressedByCooldown = cooldownEndsAtUtc.HasValue && cooldownEndsAtUtc.Value > nowUtc;
            if (isSuppressedByCooldown && !request.IncludeSuppressedByCooldown)
            {
                continue;
            }

            var inactiveDays = Math.Max(0, (int)(nowUtc - snapshot.LastActivityAtUtc.Value).TotalDays);
            result.Add(new InactiveReminderCandidateDto
            {
                UserId = snapshot.UserId,
                LastActivityAtUtc = snapshot.LastActivityAtUtc.Value,
                InactiveDays = inactiveDays,
                LastReminderSentAtUtc = lastReminderSentAtUtc,
                CooldownEndsAtUtc = cooldownEndsAtUtc,
                PushDestinationDeviceId = destination.DeviceId,
                PushToken = destination.PushToken,
                Platform = destination.Platform.ToString(),
                IsSuppressed = isSuppressedByCooldown,
                SuppressionCode = isSuppressedByCooldown ? "CooldownActive" : null
            });

            if (result.Count >= maxItems)
            {
                break;
            }
        }

        return Result<IReadOnlyList<InactiveReminderCandidateDto>>.Ok(result);
    }

    /// <summary>
    /// Parses snapshot metadata JSON into a normalized dictionary.
    /// </summary>
    private static Dictionary<string, string> DeserializeMetadata(string? snapshotJson)
    {
        if (string.IsNullOrWhiteSpace(snapshotJson))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(snapshotJson);
            if (payload is null)
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in payload)
            {
                metadata[entry.Key] = entry.Value.ValueKind switch
                {
                    JsonValueKind.String => entry.Value.GetString() ?? string.Empty,
                    _ => entry.Value.GetRawText()
                };
            }

            return metadata;
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Attempts to parse a metadata value as UTC timestamp.
    /// </summary>
    private static DateTime? TryReadDateTimeUtc(IReadOnlyDictionary<string, string> metadata, string key)
    {
        if (!metadata.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (DateTime.TryParse(raw, out var parsed))
        {
            return parsed.Kind == DateTimeKind.Utc ? parsed : parsed.ToUniversalTime();
        }

        return null;
    }
}
