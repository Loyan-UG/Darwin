using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Integration;

public sealed class SyncStateService
{
    private static readonly string[] SensitiveTerms =
    [
        "secret",
        "token",
        "credential",
        "password",
        "privatekey",
        "private_key",
        "connectionstring",
        "connection_string",
        "rawpayload",
        "raw_payload",
        "providerpayload",
        "provider_payload"
    ];

    private readonly IAppDbContext _db;

    public SyncStateService(IAppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<Result<Guid>> UpsertStateAsync(
        UpsertSyncStateCommand command,
        CancellationToken ct = default)
    {
        var validation = await ValidateStateCommandAsync(command, ct).ConfigureAwait(false);
        if (!validation.Succeeded)
        {
            return Result<Guid>.Fail(validation.Error ?? "Sync state is invalid.");
        }

        var entityType = NormalizeRequired(command.EntityType)!;
        var syncScope = NormalizeScope(command.SyncScope);
        var metadata = NormalizeJson(command.MetadataJson);
        if (ContainsSensitiveValue(metadata) ||
            ContainsSensitiveValue(command.LastErrorCode) ||
            ContainsSensitiveValue(command.LastErrorSummary))
        {
            return Result<Guid>.Fail("Sync state metadata and error summaries must not contain secrets or raw provider payloads.");
        }

        var state = await _db.Set<SyncState>()
            .FirstOrDefaultAsync(x =>
                x.ExternalSystemId == command.ExternalSystemId &&
                x.EntityType == entityType &&
                x.EntityId == command.EntityId &&
                x.Direction == command.Direction &&
                x.SyncScope == syncScope &&
                !x.IsDeleted,
                ct)
            .ConfigureAwait(false);

        if (state is null)
        {
            state = new SyncState
            {
                ExternalSystemId = command.ExternalSystemId,
                EntityType = entityType,
                EntityId = command.EntityId,
                Direction = command.Direction,
                SyncScope = syncScope
            };
            _db.Set<SyncState>().Add(state);
        }

        state.Status = command.Status;
        state.LastSuccessfulSyncAtUtc = command.LastSuccessfulSyncAtUtc;
        state.LastAttemptAtUtc = command.LastAttemptAtUtc;
        state.NextRetryAtUtc = command.NextRetryAtUtc;
        state.AttemptCount = Math.Max(0, command.AttemptCount);
        state.LastErrorCode = NormalizeOptional(command.LastErrorCode);
        state.LastErrorSummary = NormalizeOptional(command.LastErrorSummary);
        state.RemoteVersion = NormalizeOptional(command.RemoteVersion);
        state.LocalVersion = NormalizeOptional(command.LocalVersion);
        state.MetadataJson = metadata;

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result<Guid>.Ok(state.Id);
    }

    public async Task<Result<Guid>> RecordConflictAsync(
        RecordSyncConflictCommand command,
        CancellationToken ct = default)
    {
        if (command.SyncStateId == Guid.Empty)
        {
            return Result<Guid>.Fail("Sync state id is required.");
        }

        var conflictKey = NormalizeRequired(command.ConflictKey);
        if (conflictKey is null)
        {
            return Result<Guid>.Fail("Conflict key is required.");
        }

        var metadata = NormalizeJson(command.MetadataJson);
        if (ContainsSensitiveValue(metadata) ||
            ContainsSensitiveValue(command.DarwinValueSummary) ||
            ContainsSensitiveValue(command.ExternalValueSummary))
        {
            return Result<Guid>.Fail("Sync conflict summaries and metadata must not contain secrets or raw provider payloads.");
        }

        var state = await _db.Set<SyncState>()
            .FirstOrDefaultAsync(x => x.Id == command.SyncStateId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (state is null)
        {
            return Result<Guid>.Fail("Sync state was not found.");
        }

        var conflict = await _db.Set<SyncConflict>()
            .FirstOrDefaultAsync(x =>
                x.ExternalSystemId == state.ExternalSystemId &&
                x.EntityType == state.EntityType &&
                x.EntityId == state.EntityId &&
                x.ConflictKey == conflictKey &&
                !x.IsDeleted,
                ct)
            .ConfigureAwait(false);

        if (conflict is null)
        {
            conflict = new SyncConflict
            {
                SyncStateId = state.Id,
                ExternalSystemId = state.ExternalSystemId,
                EntityType = state.EntityType,
                EntityId = state.EntityId,
                Direction = command.Direction == SyncDirection.Unknown ? state.Direction : command.Direction,
                ConflictKey = conflictKey,
                DetectedAtUtc = command.DetectedAtUtc ?? DateTime.UtcNow
            };
            _db.Set<SyncConflict>().Add(conflict);
        }

        conflict.Status = SyncConflictStatus.Open;
        conflict.Resolution = SyncConflictResolution.None;
        conflict.FieldPath = NormalizeOptional(command.FieldPath);
        conflict.DarwinValueSummary = NormalizeOptional(command.DarwinValueSummary);
        conflict.ExternalValueSummary = NormalizeOptional(command.ExternalValueSummary);
        conflict.ResolutionSummary = null;
        conflict.ResolvedAtUtc = null;
        conflict.ResolvedByUserId = null;
        conflict.MetadataJson = metadata;
        state.Status = SyncStateStatus.Conflict;

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result<Guid>.Ok(conflict.Id);
    }

    public async Task<Result> ResolveConflictAsync(
        ResolveSyncConflictCommand command,
        CancellationToken ct = default)
    {
        if (command.SyncConflictId == Guid.Empty)
        {
            return Result.Fail("Sync conflict id is required.");
        }

        if (command.Resolution == SyncConflictResolution.None)
        {
            return Result.Fail("Sync conflict resolution is required.");
        }

        if (ContainsSensitiveValue(command.ResolutionSummary))
        {
            return Result.Fail("Sync conflict resolution summary must not contain secrets or raw provider payloads.");
        }

        var conflict = await _db.Set<SyncConflict>()
            .Include(x => x.SyncState)
            .FirstOrDefaultAsync(x => x.Id == command.SyncConflictId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (conflict is null)
        {
            return Result.Fail("Sync conflict was not found.");
        }

        conflict.Status = command.Status;
        conflict.Resolution = command.Resolution;
        conflict.ResolutionSummary = NormalizeOptional(command.ResolutionSummary);
        conflict.ResolvedAtUtc = command.ResolvedAtUtc ?? DateTime.UtcNow;
        conflict.ResolvedByUserId = command.ResolvedByUserId;

        if (conflict.SyncState is not null)
        {
            var hasOpenConflict = await _db.Set<SyncConflict>()
                .AnyAsync(x =>
                    x.SyncStateId == conflict.SyncStateId &&
                    x.Id != conflict.Id &&
                    (x.Status == SyncConflictStatus.Open || x.Status == SyncConflictStatus.InReview) &&
                    !x.IsDeleted,
                    ct)
                .ConfigureAwait(false);

            if (!hasOpenConflict)
            {
                conflict.SyncState.Status = SyncStateStatus.PendingInbound;
            }
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Ok();
    }

    public async Task<IReadOnlyList<SyncConflictDto>> GetOpenConflictsAsync(
        string? entityType,
        Guid? entityId,
        CancellationToken ct = default)
    {
        var normalizedEntityType = NormalizeOptional(entityType);

        return await _db.Set<SyncConflict>()
            .AsNoTracking()
            .Where(x =>
                (x.Status == SyncConflictStatus.Open || x.Status == SyncConflictStatus.InReview) &&
                (normalizedEntityType == null || x.EntityType == normalizedEntityType) &&
                (!entityId.HasValue || entityId.Value == Guid.Empty || x.EntityId == entityId.Value))
            .OrderByDescending(x => x.DetectedAtUtc)
            .ThenBy(x => x.ExternalSystemId)
            .ThenBy(x => x.EntityType)
            .ThenBy(x => x.ConflictKey)
            .Select(x => new SyncConflictDto(
                x.Id,
                x.SyncStateId,
                x.ExternalSystemId,
                x.EntityType,
                x.EntityId,
                x.Direction,
                x.Status,
                x.ConflictKey,
                x.FieldPath,
                x.DetectedAtUtc))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private async Task<Result> ValidateStateCommandAsync(
        UpsertSyncStateCommand command,
        CancellationToken ct)
    {
        if (command.ExternalSystemId == Guid.Empty)
        {
            return Result.Fail("External system id is required.");
        }

        if (command.EntityId == Guid.Empty)
        {
            return Result.Fail("Entity id is required.");
        }

        if (NormalizeRequired(command.EntityType) is null)
        {
            return Result.Fail("Entity type is required.");
        }

        if (command.Direction == SyncDirection.Unknown)
        {
            return Result.Fail("Sync direction is required.");
        }

        var externalSystemExists = await _db.Set<ExternalSystem>()
            .AnyAsync(x => x.Id == command.ExternalSystemId && x.IsActive && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        return externalSystemExists
            ? Result.Ok()
            : Result.Fail("Active external system was not found.");
    }

    private static string? NormalizeRequired(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string NormalizeScope(string? value)
    {
        var normalized = NormalizeOptional(value);
        return normalized ?? "default";
    }

    private static string NormalizeJson(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "{}" : normalized;
    }

    private static bool ContainsSensitiveValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();
        return SensitiveTerms.Any(normalized.Contains);
    }
}

public sealed record UpsertSyncStateCommand(
    Guid ExternalSystemId,
    string? EntityType,
    Guid EntityId,
    SyncDirection Direction,
    SyncStateStatus Status,
    string? SyncScope = null,
    DateTime? LastSuccessfulSyncAtUtc = null,
    DateTime? LastAttemptAtUtc = null,
    DateTime? NextRetryAtUtc = null,
    int AttemptCount = 0,
    string? LastErrorCode = null,
    string? LastErrorSummary = null,
    string? RemoteVersion = null,
    string? LocalVersion = null,
    string? MetadataJson = null);

public sealed record RecordSyncConflictCommand(
    Guid SyncStateId,
    string? ConflictKey,
    string? FieldPath = null,
    string? DarwinValueSummary = null,
    string? ExternalValueSummary = null,
    SyncDirection Direction = SyncDirection.Unknown,
    DateTime? DetectedAtUtc = null,
    string? MetadataJson = null);

public sealed record ResolveSyncConflictCommand(
    Guid SyncConflictId,
    SyncConflictResolution Resolution,
    SyncConflictStatus Status = SyncConflictStatus.Resolved,
    string? ResolutionSummary = null,
    Guid? ResolvedByUserId = null,
    DateTime? ResolvedAtUtc = null);

public sealed record SyncConflictDto(
    Guid Id,
    Guid SyncStateId,
    Guid ExternalSystemId,
    string EntityType,
    Guid EntityId,
    SyncDirection Direction,
    SyncConflictStatus Status,
    string ConflictKey,
    string? FieldPath,
    DateTime DetectedAtUtc);
