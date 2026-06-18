using System.Text.Json;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Abstractions.Storage;
using Darwin.Application.Foundation;
using Darwin.Application.HumanResources.Commands;
using Darwin.Application.HumanResources.DTOs;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Entities.HumanResources;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.HumanResources.Services;

public sealed class PersonnelDocumentService
{
    public const string EntityType = "Employee";
    public const string ProfileName = "PersonnelDocuments";
    public const string ContainerName = "personnel-documents";

    private readonly IAppDbContext _db;
    private readonly IObjectStorageService _storage;
    private readonly DocumentRecordService _documents;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public PersonnelDocumentService(IAppDbContext db, IObjectStorageService storage, DocumentRecordService documents, IClock clock, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _documents = documents ?? throw new ArgumentNullException(nameof(documents));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events;
    }

    public bool IsStorageReady()
        => _storage.GetCapabilities(new ObjectStorageContainerSelection(ContainerName, ProfileName: ProfileName)).Provider != ObjectStorageProviderKind.Database;

    public async Task<Result<Guid>> UploadAsync(PersonnelDocumentUploadDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var validation = await ValidateUploadAsync(dto, ct).ConfigureAwait(false);
        if (!validation.Succeeded) return Result<Guid>.Fail(validation.Error ?? "PersonnelDocumentInvalid");
        if (!IsStorageReady()) return Result<Guid>.Fail("PersonnelDocumentStorageNotReady");

        var documentObjectId = Guid.NewGuid();
        var objectKey = BuildObjectKey(dto.BusinessId, dto.EmployeeId, documentObjectId, dto.FileName);
        var metadataJson = BuildMetadataJson(dto);
        var write = await _storage.SaveAsync(new ObjectStorageWriteRequest(
            ContainerName,
            objectKey,
            dto.ContentType,
            dto.FileName,
            dto.Content,
            dto.SizeBytes,
            Metadata: new Dictionary<string, string>
            {
                ["entity-type"] = EntityType,
                ["business-id"] = dto.BusinessId.ToString("N"),
                ["employee-id"] = dto.EmployeeId.ToString("N"),
                ["privacy"] = dto.PrivacyClassification.ToString()
            },
            RetentionUntilUtc: dto.RetentionUntilUtc,
            RetentionMode: dto.RetentionUntilUtc.HasValue ? ObjectRetentionMode.Governance : ObjectRetentionMode.None,
            LegalHold: dto.LegalHold,
            OverwritePolicy: ObjectOverwritePolicy.Disallow,
            ProfileName: ProfileName), ct).ConfigureAwait(false);

        var registered = await _documents.RegisterDocumentAsync(new RegisterDocumentRecordCommand(
            EntityType,
            dto.EmployeeId,
            dto.DocumentKind,
            dto.Title,
            dto.FileName,
            dto.ContentType,
            write.ContentLength,
            write.Sha256Hash,
            ProfileName,
            write.ContainerName,
            write.ObjectKey,
            Visibility: FoundationVisibility.Internal,
            MetadataJson: metadataJson), ct).ConfigureAwait(false);
        if (!registered.Succeeded) return Result<Guid>.Fail(registered.Error ?? "PersonnelDocumentRegisterFailed");

        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, dto.BusinessId, "Employee", dto.EmployeeId, "hr.personnel_document.uploaded", AuditTrailAction.Created, ct).ConfigureAwait(false);
        return registered;
    }

    public async Task<IReadOnlyList<PersonnelDocumentListItemDto>> GetDocumentsAsync(Guid employeeId, CancellationToken ct = default)
    {
        if (employeeId == Guid.Empty) return Array.Empty<PersonnelDocumentListItemDto>();
        return await _db.Set<DocumentRecord>()
            .AsNoTracking()
            .Where(x => x.EntityType == EntityType && x.EntityId == employeeId && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenBy(x => x.Title)
            .Select(x => new PersonnelDocumentListItemDto
            {
                Id = x.Id,
                RowVersion = x.RowVersion,
                EmployeeId = x.EntityId,
                DocumentKind = x.DocumentKind,
                Title = x.Title,
                FileName = x.FileName,
                ContentType = x.ContentType,
                SizeBytes = x.SizeBytes,
                ContentHash = x.ContentHash,
                CreatedAtUtc = x.CreatedAtUtc,
                PrivacyClassification = ExtractPrivacy(x.MetadataJson),
                RetentionUntilUtc = ExtractRetention(x.MetadataJson),
                LegalHold = ExtractLegalHold(x.MetadataJson)
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<Result<PersonnelDocumentDownloadResult>> DownloadAsync(Guid businessId, Guid documentId, CancellationToken ct = default)
    {
        if (businessId == Guid.Empty || documentId == Guid.Empty) return Result<PersonnelDocumentDownloadResult>.Fail("PersonnelDocumentNotFound");
        var record = await LoadEmployeeDocumentAsync(businessId, documentId, track: false, ct).ConfigureAwait(false);
        if (record is null) return Result<PersonnelDocumentDownloadResult>.Fail("PersonnelDocumentNotFound");
        var read = await _storage.ReadAsync(new ObjectStorageObjectReference(record.StorageContainer, record.StorageKey, ProfileName: ProfileName), ct).ConfigureAwait(false);
        if (read is null) return Result<PersonnelDocumentDownloadResult>.Fail("PersonnelDocumentObjectNotFound");
        return Result<PersonnelDocumentDownloadResult>.Ok(new PersonnelDocumentDownloadResult(
            read.Content,
            string.IsNullOrWhiteSpace(read.ContentType) ? record.ContentType : read.ContentType,
            string.IsNullOrWhiteSpace(read.FileName) ? record.FileName : read.FileName!,
            read.ContentLength ?? record.SizeBytes,
            read.Sha256Hash ?? record.ContentHash));
    }

    public async Task<Result> ArchiveAsync(Guid businessId, Guid documentId, byte[] rowVersion, CancellationToken ct = default)
    {
        if (businessId == Guid.Empty || documentId == Guid.Empty || rowVersion.Length == 0) return Result.Fail("PersonnelDocumentNotFound");
        var record = await LoadEmployeeDocumentAsync(businessId, documentId, track: true, ct).ConfigureAwait(false);
        if (record is null) return Result.Fail("PersonnelDocumentNotFound");
        if (!(record.RowVersion ?? Array.Empty<byte>()).SequenceEqual(rowVersion)) return Result.Fail("ItemConcurrencyConflict");
        record.IsDeleted = true;
        await HrCoreSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, businessId, EntityType, record.EntityId, "hr.personnel_document.archived", AuditTrailAction.Deleted, ct).ConfigureAwait(false);
        return Result.Ok();
    }

    private async Task<Result> ValidateUploadAsync(PersonnelDocumentUploadDto dto, CancellationToken ct)
    {
        dto.Title = Required(dto.Title, 200);
        dto.FileName = SanitizeFileName(dto.FileName);
        dto.ContentType = Required(dto.ContentType, 128).ToLowerInvariant();
        dto.MetadataJson = HrCoreSupport.NormalizeMetadataJson(dto.MetadataJson);
        HrCoreSupport.EnsureSafe(dto.Title, dto.FileName, dto.ContentType, dto.MetadataJson);
        if (dto.BusinessId == Guid.Empty || dto.EmployeeId == Guid.Empty) return Result.Fail("PersonnelDocumentEmployeeRequired");
        if (dto.SizeBytes <= 0 || dto.SizeBytes > 50L * 1024L * 1024L) return Result.Fail("PersonnelDocumentSizeInvalid");
        if (dto.Content == Stream.Null || !dto.Content.CanRead) return Result.Fail("PersonnelDocumentContentRequired");
        if (!AllowedContentTypes.Contains(dto.ContentType)) return Result.Fail("PersonnelDocumentContentTypeInvalid");
        if (dto.RetentionUntilUtc.HasValue && dto.RetentionUntilUtc.Value.Date < _clock.UtcNow.Date) return Result.Fail("PersonnelDocumentRetentionInvalid");
        var employeeExists = await _db.Set<Employee>().AsNoTracking().AnyAsync(x => x.Id == dto.EmployeeId && x.BusinessId == dto.BusinessId && !x.IsDeleted, ct).ConfigureAwait(false);
        return employeeExists ? Result.Ok() : Result.Fail("EmployeeNotFound");
    }

    private async Task<DocumentRecord?> LoadEmployeeDocumentAsync(Guid businessId, Guid documentId, bool track, CancellationToken ct)
    {
        var records = track ? _db.Set<DocumentRecord>() : _db.Set<DocumentRecord>().AsNoTracking();
        return await (
            from document in records
            join employee in _db.Set<Employee>().AsNoTracking() on document.EntityId equals employee.Id
            where document.Id == documentId &&
                  document.EntityType == EntityType &&
                  employee.BusinessId == businessId &&
                  !document.IsDeleted &&
                  !employee.IsDeleted
            select document).FirstOrDefaultAsync(ct).ConfigureAwait(false);
    }

    private static string BuildObjectKey(Guid businessId, Guid employeeId, Guid documentObjectId, string fileName)
        => ObjectStorageKeyBuilder.Build("hr", "personnel-documents", businessId.ToString("N"), employeeId.ToString("N"), documentObjectId.ToString("N") + Path.GetExtension(fileName).ToLowerInvariant());

    private static string BuildMetadataJson(PersonnelDocumentUploadDto dto)
        => JsonSerializer.Serialize(new
        {
            source = "hr-personnel-document",
            privacy = dto.PrivacyClassification.ToString(),
            retentionUntilUtc = dto.RetentionUntilUtc?.ToString("O"),
            legalHold = dto.LegalHold
        });

    private static string SanitizeFileName(string? fileName)
    {
        var normalized = Path.GetFileName(fileName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized)) throw new ArgumentException("PersonnelDocumentFileNameRequired");
        foreach (var invalid in Path.GetInvalidFileNameChars()) normalized = normalized.Replace(invalid, '-');
        return normalized.Length <= 180 ? normalized : normalized[^180..];
    }

    private static string Required(string? value, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) throw new ArgumentException("PersonnelDocumentRequiredValue");
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static HrPrivacyClassification ExtractPrivacy(string metadataJson)
    {
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(metadataJson) ? "{}" : metadataJson);
        return doc.RootElement.TryGetProperty("privacy", out var value) &&
               Enum.TryParse<HrPrivacyClassification>(value.GetString(), out var privacy)
            ? privacy
            : HrPrivacyClassification.Restricted;
    }

    private static DateTime? ExtractRetention(string metadataJson)
    {
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(metadataJson) ? "{}" : metadataJson);
        return doc.RootElement.TryGetProperty("retentionUntilUtc", out var value) &&
               DateTime.TryParse(value.GetString(), out var retention)
            ? retention
            : null;
    }

    private static bool ExtractLegalHold(string metadataJson)
    {
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(metadataJson) ? "{}" : metadataJson);
        return doc.RootElement.TryGetProperty("legalHold", out var value) && value.ValueKind == JsonValueKind.True;
    }

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/png",
        "image/jpeg",
        "text/plain",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };
}
