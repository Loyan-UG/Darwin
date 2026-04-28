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
/// Records inactive reminder outcomes for measurement and observability.
/// </summary>
public sealed class MarkInactiveReminderAttemptHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly IStringLocalizer<ValidationResource> _localizer;

    public MarkInactiveReminderAttemptHandler(IAppDbContext db, IClock clock, IStringLocalizer<ValidationResource> localizer)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    /// <summary>
    /// Stores one reminder outcome in engagement snapshot metadata.
    /// </summary>
    public async Task<Result> HandleAsync(MarkInactiveReminderAttemptDto request, CancellationToken ct = default)
    {
        if (request is null)
        {
            return Result.Fail(_localizer["RequestPayloadRequired"]);
        }

        if (request.UserId == Guid.Empty)
        {
            return Result.Fail(_localizer["UserIdRequired"]);
        }

        var normalizedOutcome = NormalizeOutcome(request.Outcome);
        if (normalizedOutcome is null)
        {
            return Result.Fail(_localizer["InactiveReminderOutcomeInvalid"]);
        }

        var snapshot = await (
                from engagementSnapshot in _db.Set<UserEngagementSnapshot>()
                join user in _db.Set<User>().AsNoTracking() on engagementSnapshot.UserId equals user.Id
                where engagementSnapshot.UserId == request.UserId &&
                      !engagementSnapshot.IsDeleted &&
                      !user.IsDeleted &&
                      user.IsActive
                select engagementSnapshot)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (snapshot is null)
        {
            return Result.Fail(_localizer["UserEngagementSnapshotNotFound"]);
        }

        var occurredAtUtc = request.OccurredAtUtc ?? _clock.UtcNow;
        var metadata = DeserializeMetadata(snapshot.SnapshotJson);

        metadata[ReminderMetadataKeys.LastInactiveReminderAttemptAtUtc] = occurredAtUtc;
        metadata[ReminderMetadataKeys.LastInactiveReminderOutcome] = normalizedOutcome;

        var normalizedCode = string.IsNullOrWhiteSpace(request.OutcomeCode)
            ? null
            : request.OutcomeCode.Trim();

        if (!string.IsNullOrWhiteSpace(normalizedCode))
        {
            metadata[ReminderMetadataKeys.LastInactiveReminderOutcomeCode] = normalizedCode;
        }

        switch (normalizedOutcome)
        {
            case "Sent":
                metadata[ReminderMetadataKeys.InactiveReminderSentCount] = TryReadLong(metadata, ReminderMetadataKeys.InactiveReminderSentCount) + 1;
                metadata[ReminderMetadataKeys.LastInactiveReminderSentAtUtc] = occurredAtUtc;
                break;

            case "Failed":
                metadata[ReminderMetadataKeys.InactiveReminderFailedCount] = TryReadLong(metadata, ReminderMetadataKeys.InactiveReminderFailedCount) + 1;
                break;

            default:
                metadata[ReminderMetadataKeys.InactiveReminderSuppressedCount] = TryReadLong(metadata, ReminderMetadataKeys.InactiveReminderSuppressedCount) + 1;
                break;
        }

        snapshot.SnapshotJson = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
        {
            WriteIndented = false
        });

        snapshot.CalculatedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Ok();
    }

    /// <summary>
    /// Normalizes and validates outcome values.
    /// </summary>
    private static string? NormalizeOutcome(string? outcome)
    {
        if (string.IsNullOrWhiteSpace(outcome))
        {
            return null;
        }

        if (string.Equals(outcome, "Sent", StringComparison.OrdinalIgnoreCase))
        {
            return "Sent";
        }

        if (string.Equals(outcome, "Failed", StringComparison.OrdinalIgnoreCase))
        {
            return "Failed";
        }

        if (string.Equals(outcome, "Suppressed", StringComparison.OrdinalIgnoreCase))
        {
            return "Suppressed";
        }

        return null;
    }

    /// <summary>
    /// Deserializes metadata dictionary defensively.
    /// </summary>
    private static Dictionary<string, object?> DeserializeMetadata(string? snapshotJson)
    {
        if (string.IsNullOrWhiteSpace(snapshotJson))
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(snapshotJson);
            var metadata = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            if (payload is null)
            {
                return metadata;
            }

            foreach (var entry in payload)
            {
                metadata[entry.Key] = entry.Value.ValueKind switch
                {
                    JsonValueKind.String => entry.Value.GetString(),
                    JsonValueKind.Number when entry.Value.TryGetInt64(out var value) => value,
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => entry.Value.GetRawText()
                };
            }

            return metadata;
        }
        catch
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Attempts to parse metadata value as long.
    /// </summary>
    private static long TryReadLong(IReadOnlyDictionary<string, object?> metadata, string key)
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
