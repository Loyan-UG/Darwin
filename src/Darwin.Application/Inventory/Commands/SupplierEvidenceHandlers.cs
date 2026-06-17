using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Foundation;
using Darwin.Application.Inventory.DTOs;
using Darwin.Domain.Entities.Inventory;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Inventory.Commands;

public sealed class CreateSupplierContactHandler
{
    private readonly IAppDbContext _db;

    public CreateSupplierContactHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<Guid> HandleAsync(SupplierContactEditDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        SupplierEvidenceSupport.ValidateContact(dto, requireRowVersion: false);
        await SupplierEvidenceSupport.ValidateSupplierAsync(_db, dto.BusinessId, dto.SupplierId, ct).ConfigureAwait(false);
        await SupplierEvidenceSupport.EnsureNoDuplicateContactAsync(_db, dto.BusinessId, dto.SupplierId, dto.Email, null, ct).ConfigureAwait(false);

        if (dto.IsPrimary)
        {
            await SupplierEvidenceSupport.ClearPrimaryContactsAsync(_db, dto.SupplierId, null, ct).ConfigureAwait(false);
        }

        var contact = new SupplierContact
        {
            BusinessId = dto.BusinessId,
            SupplierId = dto.SupplierId,
            Role = dto.Role,
            Name = SupplierEvidenceSupport.Required(dto.Name, 200),
            JobTitle = SupplierEvidenceSupport.Optional(dto.JobTitle, 200),
            Email = SupplierEvidenceSupport.Optional(dto.Email, 320),
            Phone = SupplierEvidenceSupport.Optional(dto.Phone, 50),
            LanguageCode = SupplierEvidenceSupport.Optional(dto.LanguageCode, 16)?.ToUpperInvariant(),
            IsPrimary = dto.IsPrimary,
            Notes = SupplierEvidenceSupport.Optional(dto.Notes, 1000)
        };
        _db.Set<SupplierContact>().Add(contact);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return contact.Id;
    }
}

public sealed class UpdateSupplierContactHandler
{
    private readonly IAppDbContext _db;

    public UpdateSupplierContactHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task HandleAsync(SupplierContactEditDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        SupplierEvidenceSupport.ValidateContact(dto, requireRowVersion: true);
        var contact = await SupplierEvidenceSupport.LoadContactForUpdateAsync(_db, dto.Id, dto.RowVersion, ct).ConfigureAwait(false);
        await SupplierEvidenceSupport.ValidateSupplierAsync(_db, dto.BusinessId, dto.SupplierId, ct).ConfigureAwait(false);
        await SupplierEvidenceSupport.EnsureNoDuplicateContactAsync(_db, dto.BusinessId, dto.SupplierId, dto.Email, dto.Id, ct).ConfigureAwait(false);

        if (dto.IsPrimary)
        {
            await SupplierEvidenceSupport.ClearPrimaryContactsAsync(_db, dto.SupplierId, dto.Id, ct).ConfigureAwait(false);
        }

        contact.BusinessId = dto.BusinessId;
        contact.SupplierId = dto.SupplierId;
        contact.Role = dto.Role;
        contact.Name = SupplierEvidenceSupport.Required(dto.Name, 200);
        contact.JobTitle = SupplierEvidenceSupport.Optional(dto.JobTitle, 200);
        contact.Email = SupplierEvidenceSupport.Optional(dto.Email, 320);
        contact.Phone = SupplierEvidenceSupport.Optional(dto.Phone, 50);
        contact.LanguageCode = SupplierEvidenceSupport.Optional(dto.LanguageCode, 16)?.ToUpperInvariant();
        contact.IsPrimary = dto.IsPrimary;
        contact.Notes = SupplierEvidenceSupport.Optional(dto.Notes, 1000);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}

public sealed class ArchiveSupplierContactHandler
{
    private readonly IAppDbContext _db;

    public ArchiveSupplierContactHandler(IAppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task HandleAsync(Guid id, byte[] rowVersion, CancellationToken ct = default)
    {
        var contact = await SupplierEvidenceSupport.LoadContactForUpdateAsync(_db, id, rowVersion, ct).ConfigureAwait(false);
        contact.IsDeleted = true;
        contact.IsPrimary = false;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}

public sealed class RegisterSupplierDocumentHandler
{
    private readonly IAppDbContext _db;
    private readonly DocumentRecordService _documents;

    public RegisterSupplierDocumentHandler(IAppDbContext db, DocumentRecordService documents)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _documents = documents ?? throw new ArgumentNullException(nameof(documents));
    }

    public async Task<Guid> HandleAsync(SupplierDocumentRegisterDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.BusinessId == Guid.Empty || dto.SupplierId == Guid.Empty) throw new ArgumentException("SupplierDocumentInvalidSupplier");
        await SupplierEvidenceSupport.ValidateSupplierAsync(_db, dto.BusinessId, dto.SupplierId, ct).ConfigureAwait(false);
        if (FoundationInputNormalizer.LooksSensitive(dto.Title) ||
            FoundationInputNormalizer.LooksSensitive(dto.FileName) ||
            FoundationInputNormalizer.LooksSensitive(dto.StorageKey) ||
            FoundationInputNormalizer.LooksSensitive(dto.MetadataJson))
        {
            throw new ArgumentException("SupplierDocumentSensitiveMetadataRejected");
        }

        var result = await _documents.RegisterDocumentAsync(new RegisterDocumentRecordCommand(
            "Supplier",
            dto.SupplierId,
            dto.DocumentKind,
            dto.Title,
            dto.FileName,
            dto.ContentType,
            dto.SizeBytes,
            dto.ContentHash,
            dto.StorageProvider,
            dto.StorageContainer,
            dto.StorageKey,
            null,
            dto.Visibility,
            dto.MetadataJson), ct).ConfigureAwait(false);
        if (!result.Succeeded) throw new InvalidOperationException(result.Error);
        return result.Value;
    }
}

internal static class SupplierEvidenceSupport
{
    public static void ValidateContact(SupplierContactEditDto dto, bool requireRowVersion)
    {
        if (dto.BusinessId == Guid.Empty || dto.SupplierId == Guid.Empty) throw new ArgumentException("SupplierContactInvalidSupplier");
        if (requireRowVersion && (dto.Id == Guid.Empty || dto.RowVersion.Length == 0)) throw new ArgumentException("SupplierContactInvalidUpdate");
        if (!Enum.IsDefined(typeof(SupplierContactRole), dto.Role)) throw new ArgumentException("SupplierContactInvalidRole");
        _ = Required(dto.Name, 200);
        var email = Optional(dto.Email, 320);
        var phone = Optional(dto.Phone, 50);
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone)) throw new ArgumentException("SupplierContactChannelRequired");
        if (FoundationInputNormalizer.LooksSensitive(dto.Name) ||
            FoundationInputNormalizer.LooksSensitive(dto.JobTitle) ||
            FoundationInputNormalizer.LooksSensitive(dto.Email) ||
            FoundationInputNormalizer.LooksSensitive(dto.Phone) ||
            FoundationInputNormalizer.LooksSensitive(dto.LanguageCode) ||
            FoundationInputNormalizer.LooksSensitive(dto.Notes))
        {
            throw new ArgumentException("SupplierContactSensitiveMetadataRejected");
        }
    }

    public static async Task ValidateSupplierAsync(IAppDbContext db, Guid businessId, Guid supplierId, CancellationToken ct)
    {
        var exists = await db.Set<Supplier>()
            .AsNoTracking()
            .AnyAsync(x => x.Id == supplierId && x.BusinessId == businessId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (!exists) throw new InvalidOperationException("SupplierNotFound");
    }

    public static async Task<SupplierContact> LoadContactForUpdateAsync(IAppDbContext db, Guid id, byte[] rowVersion, CancellationToken ct)
    {
        if (id == Guid.Empty || rowVersion.Length == 0) throw new ArgumentException("SupplierContactInvalidUpdate");
        var contact = await db.Set<SupplierContact>().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("SupplierContactNotFound");
        if (!(contact.RowVersion ?? Array.Empty<byte>()).SequenceEqual(rowVersion)) throw new DbUpdateConcurrencyException("ConcurrencyConflictDetected");
        return contact;
    }

    public static async Task EnsureNoDuplicateContactAsync(IAppDbContext db, Guid businessId, Guid supplierId, string? email, Guid? excludingId, CancellationToken ct)
    {
        var normalized = Optional(email, 320);
        if (string.IsNullOrWhiteSpace(normalized)) return;
        var exists = await db.Set<SupplierContact>()
            .AsNoTracking()
            .AnyAsync(x =>
                x.BusinessId == businessId &&
                x.SupplierId == supplierId &&
                x.Email == normalized &&
                !x.IsDeleted &&
                (!excludingId.HasValue || x.Id != excludingId.Value), ct)
            .ConfigureAwait(false);
        if (exists) throw new InvalidOperationException("SupplierContactDuplicateEmail");
    }

    public static async Task ClearPrimaryContactsAsync(IAppDbContext db, Guid supplierId, Guid? excludingId, CancellationToken ct)
    {
        var contacts = await db.Set<SupplierContact>()
            .Where(x => x.SupplierId == supplierId && x.IsPrimary && !x.IsDeleted && (!excludingId.HasValue || x.Id != excludingId.Value))
            .ToListAsync(ct)
            .ConfigureAwait(false);
        foreach (var contact in contacts)
        {
            contact.IsPrimary = false;
        }
    }

    public static string Required(string? value, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) throw new ArgumentException("SupplierContactRequiredValue");
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    public static string? Optional(string? value, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) return null;
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }
}
