using System.Text;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Foundation;

public sealed class FeatureAreaService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public FeatureAreaService(IAppDbContext db, IClock clock)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<Result<Guid>> CreateFeatureAreaAsync(CreateFeatureAreaCommand command, CancellationToken ct = default)
    {
        var code = NormalizeCode(command.Code);
        var name = FoundationInputNormalizer.Required(command.Name);
        if (code is null)
        {
            return Result<Guid>.Fail("Feature area code is required.");
        }

        if (name is null)
        {
            return Result<Guid>.Fail("Feature area name is required.");
        }

        if (!Enum.IsDefined(typeof(FeatureAreaCategory), command.Category))
        {
            return Result<Guid>.Fail("Feature area category is required.");
        }

        if (!Enum.IsDefined(typeof(FeatureAreaVisibilityScope), command.VisibilityScope))
        {
            return Result<Guid>.Fail("Feature area visibility scope is required.");
        }

        if (FoundationInputNormalizer.LooksSensitive(command.Description) ||
            FoundationInputNormalizer.LooksSensitive(command.MetadataJson))
        {
            return Result<Guid>.Fail("Sensitive secrets must not be stored in feature area metadata.");
        }

        var duplicate = await _db.Set<FeatureArea>()
            .AnyAsync(x => x.Code == code && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (duplicate)
        {
            return Result<Guid>.Fail("Feature area already exists.");
        }

        var featureArea = new FeatureArea
        {
            Code = code,
            Name = name,
            Description = FoundationInputNormalizer.Optional(command.Description),
            Category = command.Category,
            VisibilityScope = command.VisibilityScope,
            DefaultEnabled = command.DefaultEnabled,
            IsActive = command.IsActive,
            SortOrder = command.SortOrder,
            RequiredPermissionKey = FoundationInputNormalizer.Optional(command.RequiredPermissionKey),
            MetadataJson = FoundationInputNormalizer.Json(command.MetadataJson)
        };

        _db.Set<FeatureArea>().Add(featureArea);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result<Guid>.Ok(featureArea.Id);
    }

    public async Task<Result<Guid>> UpsertBusinessOverrideAsync(UpsertBusinessFeatureOverrideCommand command, CancellationToken ct = default)
    {
        if (command.BusinessId == Guid.Empty)
        {
            return Result<Guid>.Fail("Business id is required.");
        }

        if (command.FeatureAreaId == Guid.Empty)
        {
            return Result<Guid>.Fail("Feature area id is required.");
        }

        if (command.EffectiveFromUtc.HasValue &&
            command.EffectiveToUtc.HasValue &&
            command.EffectiveFromUtc.Value > command.EffectiveToUtc.Value)
        {
            return Result<Guid>.Fail("Feature override effective range is invalid.");
        }

        if (FoundationInputNormalizer.LooksSensitive(command.Reason) ||
            FoundationInputNormalizer.LooksSensitive(command.MetadataJson))
        {
            return Result<Guid>.Fail("Sensitive secrets must not be stored in feature override metadata.");
        }

        var featureExists = await _db.Set<FeatureArea>()
            .AnyAsync(x => x.Id == command.FeatureAreaId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (!featureExists)
        {
            return Result<Guid>.Fail("Feature area was not found.");
        }

        var existing = await _db.Set<BusinessFeatureOverride>()
            .FirstOrDefaultAsync(x =>
                x.BusinessId == command.BusinessId &&
                x.FeatureAreaId == command.FeatureAreaId &&
                !x.IsDeleted,
                ct)
            .ConfigureAwait(false);

        if (existing is null)
        {
            existing = new BusinessFeatureOverride
            {
                BusinessId = command.BusinessId,
                FeatureAreaId = command.FeatureAreaId
            };
            _db.Set<BusinessFeatureOverride>().Add(existing);
        }

        existing.IsEnabled = command.IsEnabled;
        existing.Reason = FoundationInputNormalizer.Optional(command.Reason);
        existing.EffectiveFromUtc = command.EffectiveFromUtc;
        existing.EffectiveToUtc = command.EffectiveToUtc;
        existing.MetadataJson = FoundationInputNormalizer.Json(command.MetadataJson);

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result<Guid>.Ok(existing.Id);
    }

    public async Task<bool> IsEnabledAsync(string? code, Guid? businessId = null, CancellationToken ct = default)
    {
        var normalizedCode = NormalizeCode(code);
        if (normalizedCode is null)
        {
            return false;
        }

        var feature = await _db.Set<FeatureArea>()
            .AsNoTracking()
            .Where(x => x.Code == normalizedCode && x.IsActive && !x.IsDeleted)
            .Select(x => new FeatureAreaState(x.Id, x.DefaultEnabled))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        if (feature is null)
        {
            return false;
        }

        if (!businessId.HasValue || businessId.Value == Guid.Empty)
        {
            return feature.DefaultEnabled;
        }

        var nowUtc = _clock.UtcNow;
        var featureOverride = await _db.Set<BusinessFeatureOverride>()
            .AsNoTracking()
            .Where(x =>
                x.BusinessId == businessId.Value &&
                x.FeatureAreaId == feature.Id &&
                !x.IsDeleted &&
                (!x.EffectiveFromUtc.HasValue || x.EffectiveFromUtc.Value <= nowUtc) &&
                (!x.EffectiveToUtc.HasValue || x.EffectiveToUtc.Value >= nowUtc))
            .Select(x => (bool?)x.IsEnabled)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return featureOverride ?? feature.DefaultEnabled;
    }

    public async Task<IReadOnlyList<FeatureAreaDto>> GetEnabledAreasForBusinessAsync(Guid? businessId = null, CancellationToken ct = default)
    {
        var areas = await GetAllAreasAsync(includeInactive: false, ct).ConfigureAwait(false);
        if (!businessId.HasValue || businessId.Value == Guid.Empty)
        {
            return areas.Where(x => x.DefaultEnabled).ToList();
        }

        var nowUtc = _clock.UtcNow;
        var overrides = await _db.Set<BusinessFeatureOverride>()
            .AsNoTracking()
            .Where(x =>
                x.BusinessId == businessId.Value &&
                !x.IsDeleted &&
                (!x.EffectiveFromUtc.HasValue || x.EffectiveFromUtc.Value <= nowUtc) &&
                (!x.EffectiveToUtc.HasValue || x.EffectiveToUtc.Value >= nowUtc))
            .Select(x => new { x.FeatureAreaId, x.IsEnabled })
            .ToDictionaryAsync(x => x.FeatureAreaId, x => x.IsEnabled, ct)
            .ConfigureAwait(false);

        return areas
            .Where(area => overrides.TryGetValue(area.Id, out var enabled) ? enabled : area.DefaultEnabled)
            .ToList();
    }

    public async Task<IReadOnlyList<FeatureAreaDto>> GetAllAreasAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        var query = _db.Set<FeatureArea>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        return await query
            .OrderBy(x => x.Category)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new FeatureAreaDto(
                x.Id,
                x.Code,
                x.Name,
                x.Description,
                x.Category,
                x.VisibilityScope,
                x.DefaultEnabled,
                x.IsActive,
                x.SortOrder,
                x.RequiredPermissionKey,
                x.MetadataJson))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public static string? NormalizeCode(string? value)
    {
        var normalized = FoundationInputNormalizer.Required(value);
        if (normalized is null)
        {
            return null;
        }

        var builder = new StringBuilder(normalized.Length);
        var previousWasSeparator = false;
        foreach (var ch in normalized)
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(char.ToLowerInvariant(ch));
                previousWasSeparator = false;
            }
            else if (!previousWasSeparator)
            {
                builder.Append('-');
                previousWasSeparator = true;
            }
        }

        var code = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(code) ? null : code;
    }

    private sealed record FeatureAreaState(Guid Id, bool DefaultEnabled);
}

public sealed record CreateFeatureAreaCommand(
    string? Code,
    string? Name,
    FeatureAreaCategory Category,
    FeatureAreaVisibilityScope VisibilityScope,
    bool DefaultEnabled = true,
    bool IsActive = true,
    int SortOrder = 0,
    string? Description = null,
    string? RequiredPermissionKey = null,
    string? MetadataJson = null);

public sealed record UpsertBusinessFeatureOverrideCommand(
    Guid BusinessId,
    Guid FeatureAreaId,
    bool IsEnabled,
    string? Reason = null,
    DateTime? EffectiveFromUtc = null,
    DateTime? EffectiveToUtc = null,
    string? MetadataJson = null);

public sealed record FeatureAreaDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    FeatureAreaCategory Category,
    FeatureAreaVisibilityScope VisibilityScope,
    bool DefaultEnabled,
    bool IsActive,
    int SortOrder,
    string? RequiredPermissionKey,
    string MetadataJson);
