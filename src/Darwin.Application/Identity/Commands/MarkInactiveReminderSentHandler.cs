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
/// Persists successful inactive reminder dispatch metadata for cooldown/suppression logic.
/// </summary>
public sealed class MarkInactiveReminderSentHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly IStringLocalizer<ValidationResource> _localizer;

    public MarkInactiveReminderSentHandler(IAppDbContext db, IClock clock, IStringLocalizer<ValidationResource> localizer)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    /// <summary>
    /// Records a successful reminder dispatch for one user.
    /// </summary>
    public async Task<Result> HandleAsync(MarkInactiveReminderSentDto request, CancellationToken ct = default)
    {
        if (request is null)
        {
            return Result.Fail(_localizer["RequestPayloadRequired"]);
        }

        if (request.UserId == Guid.Empty)
        {
            return Result.Fail(_localizer["UserIdRequired"]);
        }

        var snapshot = await _db.Set<UserEngagementSnapshot>()
            .FirstOrDefaultAsync(x => x.UserId == request.UserId, ct)
            .ConfigureAwait(false);

        if (snapshot is null)
        {
            return Result.Fail(_localizer["UserEngagementSnapshotNotFound"]);
        }

        var sentAtUtc = request.SentAtUtc ?? _clock.UtcNow;
        var metadata = DeserializeMetadata(snapshot.SnapshotJson);

        var sentCount = TryReadLong(metadata, ReminderMetadataKeys.InactiveReminderSentCount) + 1;
        metadata[ReminderMetadataKeys.LastInactiveReminderSentAtUtc] = sentAtUtc;
        metadata[ReminderMetadataKeys.InactiveReminderSentCount] = sentCount;

        snapshot.SnapshotJson = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
        {
            WriteIndented = false
        });

        snapshot.CalculatedAtUtc = _clock.UtcNow;

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Ok();
    }

    /// <summary>
    /// Deserializes metadata dictionary defensively and keeps existing values when possible.
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
