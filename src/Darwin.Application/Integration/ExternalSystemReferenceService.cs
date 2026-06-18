using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Integration;

public sealed class ExternalSystemReferenceService
{
    private readonly IAppDbContext _db;

    public ExternalSystemReferenceService(IAppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<Result<Guid>> CreateExternalSystemAsync(
        CreateExternalSystemCommand command,
        CancellationToken ct = default)
    {
        var code = NormalizeCode(command.Code);
        var name = NormalizeRequired(command.Name);
        if (code is null)
        {
            return Result<Guid>.Fail("External system code is required.");
        }

        if (name is null)
        {
            return Result<Guid>.Fail("External system name is required.");
        }

        var exists = await _db.Set<ExternalSystem>()
            .AnyAsync(x => x.Code == code && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (exists)
        {
            return Result<Guid>.Fail("External system code already exists.");
        }

        var entity = new ExternalSystem
        {
            Code = code,
            Name = name,
            Kind = command.Kind,
            BaseUrl = NormalizeOptional(command.BaseUrl),
            Description = NormalizeOptional(command.Description),
            IsActive = command.IsActive,
            MetadataJson = NormalizeJson(command.MetadataJson)
        };

        _db.Set<ExternalSystem>().Add(entity);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result<Guid>.Ok(entity.Id);
    }

    public async Task<Result<Guid>> UpsertReferenceAsync(
        UpsertExternalReferenceCommand command,
        CancellationToken ct = default)
    {
        if (command.ExternalSystemId == Guid.Empty)
        {
            return Result<Guid>.Fail("External system id is required.");
        }

        if (command.EntityId == Guid.Empty)
        {
            return Result<Guid>.Fail("Entity id is required.");
        }

        var entityType = NormalizeRequired(command.EntityType);
        var externalId = NormalizeRequired(command.ExternalId);
        if (entityType is null)
        {
            return Result<Guid>.Fail("Entity type is required.");
        }

        if (externalId is null)
        {
            return Result<Guid>.Fail("External id is required.");
        }

        var externalSystemExists = await _db.Set<ExternalSystem>()
            .AnyAsync(x => x.Id == command.ExternalSystemId && x.IsActive && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (!externalSystemExists)
        {
            return Result<Guid>.Fail("Active external system was not found.");
        }

        var reference = await _db.Set<ExternalReference>()
            .FirstOrDefaultAsync(x =>
                x.ExternalSystemId == command.ExternalSystemId &&
                x.EntityType == entityType &&
                x.ReferenceKind == command.ReferenceKind &&
                x.ExternalId == externalId &&
                !x.IsDeleted,
                ct)
            .ConfigureAwait(false);

        if (reference is null)
        {
            reference = new ExternalReference
            {
                ExternalSystemId = command.ExternalSystemId,
                EntityType = entityType,
                ReferenceKind = command.ReferenceKind,
                ExternalId = externalId
            };
            _db.Set<ExternalReference>().Add(reference);
        }

        reference.EntityId = command.EntityId;
        reference.ExternalDisplayId = NormalizeOptional(command.ExternalDisplayId);
        reference.SourceOfTruth = command.SourceOfTruth;
        reference.IsPrimary = command.IsPrimary;
        reference.IsActive = command.IsActive;
        reference.LastSeenAtUtc = command.LastSeenAtUtc;
        reference.MetadataJson = NormalizeJson(command.MetadataJson);

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result<Guid>.Ok(reference.Id);
    }

    public async Task<IReadOnlyList<ExternalReferenceDto>> GetReferencesForEntityAsync(
        string entityType,
        Guid entityId,
        CancellationToken ct = default)
    {
        var normalizedEntityType = NormalizeRequired(entityType);
        if (normalizedEntityType is null || entityId == Guid.Empty)
        {
            return Array.Empty<ExternalReferenceDto>();
        }

        return await _db.Set<ExternalReference>()
            .AsNoTracking()
            .Where(x =>
                x.EntityType == normalizedEntityType &&
                x.EntityId == entityId &&
                x.IsActive &&
                !x.IsDeleted)
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.ExternalSystemId)
            .ThenBy(x => x.ReferenceKind)
            .ThenBy(x => x.ExternalId)
            .Select(x => new ExternalReferenceDto(
                x.Id,
                x.ExternalSystemId,
                x.EntityType,
                x.EntityId,
                x.ReferenceKind,
                x.ExternalId,
                x.ExternalDisplayId,
                x.SourceOfTruth,
                x.IsPrimary,
                x.LastSeenAtUtc,
                x.MetadataJson))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public static string? NormalizeCode(string? value)
    {
        var normalized = NormalizeRequired(value);
        return normalized?.ToUpperInvariant();
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

    private static string NormalizeJson(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "{}" : normalized;
    }
}

public sealed record CreateExternalSystemCommand(
    string? Code,
    string? Name,
    ExternalSystemKind Kind,
    string? BaseUrl = null,
    string? Description = null,
    bool IsActive = true,
    string? MetadataJson = null);

public sealed record UpsertExternalReferenceCommand(
    Guid ExternalSystemId,
    string? EntityType,
    Guid EntityId,
    ExternalReferenceKind ReferenceKind,
    string? ExternalId,
    string? ExternalDisplayId,
    SourceOfTruth SourceOfTruth,
    bool IsPrimary = false,
    bool IsActive = true,
    DateTime? LastSeenAtUtc = null,
    string? MetadataJson = null);

public sealed record ExternalReferenceDto(
    Guid Id,
    Guid ExternalSystemId,
    string EntityType,
    Guid EntityId,
    ExternalReferenceKind ReferenceKind,
    string ExternalId,
    string? ExternalDisplayId,
    SourceOfTruth SourceOfTruth,
    bool IsPrimary,
    DateTime? LastSeenAtUtc,
    string MetadataJson);
