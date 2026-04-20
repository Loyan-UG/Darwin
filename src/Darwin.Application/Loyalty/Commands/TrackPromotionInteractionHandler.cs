using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Loyalty.Commands;

/// <summary>
/// Records promotion interaction analytics for the current authenticated user.
/// </summary>
public sealed class TrackPromotionInteractionHandler
{
    private const string MetadataKeyPromotionImpressionCount = "promotionImpressionCount";
    private const string MetadataKeyPromotionOpenCount = "promotionOpenCount";
    private const string MetadataKeyPromotionClaimCount = "promotionClaimCount";
    private const string MetadataKeyLastPromotionInteractionAtUtc = "lastPromotionInteractionAtUtc";
    private const string MetadataKeyLastPromotionInteractionType = "lastPromotionInteractionType";
    private const string MetadataKeyLastPromotionBusinessId = "lastPromotionBusinessId";

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IClock _clock;
    private readonly IStringLocalizer<ValidationResource> _localizer;

    public TrackPromotionInteractionHandler(IAppDbContext db, ICurrentUserService currentUser, IClock clock, IStringLocalizer<ValidationResource> localizer)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    /// <summary>
    /// Tracks one promotion event for current user and updates engagement snapshot metadata.
    /// </summary>
    public async Task<Result> HandleAsync(TrackPromotionInteractionDto dto, CancellationToken ct = default)
    {
        if (dto is null)
        {
            return Result.Fail(_localizer["RequestPayloadRequired"]);
        }

        if (dto.BusinessId == Guid.Empty)
        {
            return Result.Fail(_localizer["BusinessIdRequired"]);
        }

        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            return Result.Fail(_localizer["TitleRequired"]);
        }

        var userId = _currentUser.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return Result.Fail(_localizer["CurrentUserIdMissing"]);
        }

        var snapshot = await _db.Set<UserEngagementSnapshot>()
            .FirstOrDefaultAsync(x => x.UserId == userId, ct)
            .ConfigureAwait(false);

        if (snapshot is null)
        {
            snapshot = new UserEngagementSnapshot
            {
                UserId = userId,
                LastActivityAtUtc = null,
                EventCount = 0,
                EngagementScore30d = 0,
                CalculatedAtUtc = _clock.UtcNow,
                SnapshotJson = "{}"
            };

            _db.Set<UserEngagementSnapshot>().Add(snapshot);
        }

        var metadata = DeserializeMetadata(snapshot.SnapshotJson);
        IncrementInteractionCounter(metadata, dto.EventType);

        var occurredAtUtc = dto.OccurredAtUtc ?? _clock.UtcNow;
        metadata[MetadataKeyLastPromotionInteractionAtUtc] = occurredAtUtc;
        metadata[MetadataKeyLastPromotionInteractionType] = dto.EventType.ToString();
        metadata[MetadataKeyLastPromotionBusinessId] = dto.BusinessId.ToString("D");

        snapshot.CalculatedAtUtc = _clock.UtcNow;
        snapshot.SnapshotJson = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
        {
            WriteIndented = false
        });

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Ok();
    }

    /// <summary>
    /// Increments interaction counters by event type.
    /// </summary>
    private static void IncrementInteractionCounter(Dictionary<string, object?> metadata, PromotionInteractionEventType eventType)
    {
        var key = eventType switch
        {
            PromotionInteractionEventType.Open => MetadataKeyPromotionOpenCount,
            PromotionInteractionEventType.Claim => MetadataKeyPromotionClaimCount,
            _ => MetadataKeyPromotionImpressionCount
        };

        metadata[key] = TryReadLong(metadata, key) + 1;
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
