using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Foundation;

public sealed class DocumentRecordService
{
    private readonly IAppDbContext _db;

    public DocumentRecordService(IAppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<Result<Guid>> RegisterDocumentAsync(RegisterDocumentRecordCommand command, CancellationToken ct = default)
    {
        if (command.EntityId == Guid.Empty)
        {
            return Result<Guid>.Fail("Entity id is required.");
        }

        var entityType = FoundationInputNormalizer.Required(command.EntityType);
        var title = FoundationInputNormalizer.Required(command.Title);
        var fileName = FoundationInputNormalizer.Required(command.FileName);
        var contentType = FoundationInputNormalizer.Required(command.ContentType);
        var storageProvider = FoundationInputNormalizer.Required(command.StorageProvider);
        var storageContainer = FoundationInputNormalizer.Required(command.StorageContainer);
        var storageKey = FoundationInputNormalizer.Required(command.StorageKey);

        if (entityType is null)
        {
            return Result<Guid>.Fail("Entity type is required.");
        }

        if (title is null)
        {
            return Result<Guid>.Fail("Document title is required.");
        }

        if (fileName is null || contentType is null || storageProvider is null || storageContainer is null || storageKey is null)
        {
            return Result<Guid>.Fail("Document storage metadata is required.");
        }

        if (command.SizeBytes.HasValue && command.SizeBytes.Value < 0)
        {
            return Result<Guid>.Fail("Document size must not be negative.");
        }

        if (FoundationInputNormalizer.LooksSensitive(title) ||
            FoundationInputNormalizer.LooksSensitive(fileName) ||
            FoundationInputNormalizer.LooksSensitive(storageKey))
        {
            return Result<Guid>.Fail("Sensitive secrets must not be stored in document metadata.");
        }

        var record = new DocumentRecord
        {
            EntityType = entityType,
            EntityId = command.EntityId,
            DocumentKind = command.DocumentKind,
            Title = title,
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = command.SizeBytes,
            ContentHash = FoundationInputNormalizer.Optional(command.ContentHash),
            StorageProvider = storageProvider,
            StorageContainer = storageContainer,
            StorageKey = storageKey,
            MediaAssetId = command.MediaAssetId,
            Visibility = command.Visibility,
            MetadataJson = FoundationInputNormalizer.Json(command.MetadataJson)
        };

        _db.Set<DocumentRecord>().Add(record);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result<Guid>.Ok(record.Id);
    }

    public async Task<IReadOnlyList<DocumentRecordDto>> GetDocumentsForEntityAsync(
        string entityType,
        Guid entityId,
        FoundationVisibility? maxVisibility = null,
        CancellationToken ct = default)
    {
        var normalizedEntityType = FoundationInputNormalizer.Required(entityType);
        if (normalizedEntityType is null || entityId == Guid.Empty)
        {
            return Array.Empty<DocumentRecordDto>();
        }

        var query = _db.Set<DocumentRecord>()
            .AsNoTracking()
            .Where(x => x.EntityType == normalizedEntityType && x.EntityId == entityId && !x.IsDeleted);

        if (maxVisibility.HasValue)
        {
            var allowedVisibilities = ResolveAllowedVisibilities(maxVisibility.Value);
            query = query.Where(x => allowedVisibilities.Contains(x.Visibility));
        }

        return await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenBy(x => x.Title)
            .Select(x => new DocumentRecordDto(
                x.Id,
                x.EntityType,
                x.EntityId,
                x.DocumentKind,
                x.Title,
                x.FileName,
                x.ContentType,
                x.SizeBytes,
                x.ContentHash,
                x.StorageProvider,
                x.StorageContainer,
                x.StorageKey,
                x.MediaAssetId,
                x.Visibility,
                x.MetadataJson))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private static FoundationVisibility[] ResolveAllowedVisibilities(FoundationVisibility maxVisibility)
        => Enum.GetValues<FoundationVisibility>()
            .Where(value => Convert.ToInt32(value) <= Convert.ToInt32(maxVisibility))
            .ToArray();
}

public sealed record RegisterDocumentRecordCommand(
    string? EntityType,
    Guid EntityId,
    DocumentRecordKind DocumentKind,
    string? Title,
    string? FileName,
    string? ContentType,
    long? SizeBytes,
    string? ContentHash,
    string? StorageProvider,
    string? StorageContainer,
    string? StorageKey,
    Guid? MediaAssetId = null,
    FoundationVisibility Visibility = FoundationVisibility.Internal,
    string? MetadataJson = null);

public sealed record DocumentRecordDto(
    Guid Id,
    string EntityType,
    Guid EntityId,
    DocumentRecordKind DocumentKind,
    string Title,
    string FileName,
    string ContentType,
    long? SizeBytes,
    string? ContentHash,
    string StorageProvider,
    string StorageContainer,
    string StorageKey,
    Guid? MediaAssetId,
    FoundationVisibility Visibility,
    string MetadataJson);
