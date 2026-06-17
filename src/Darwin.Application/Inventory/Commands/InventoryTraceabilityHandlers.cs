using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Foundation;
using Darwin.Application.Inventory.DTOs;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.Inventory;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Inventory.Commands;

public sealed class CreateProductTrackingPolicyHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<ProductTrackingPolicyCreateDto> _validator;
    private readonly IStringLocalizer<ValidationResource> _localizer;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public CreateProductTrackingPolicyHandler(IAppDbContext db, IValidator<ProductTrackingPolicyCreateDto> validator, IStringLocalizer<ValidationResource> localizer, IClock clock, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events;
    }

    public async Task<Guid> HandleAsync(ProductTrackingPolicyCreateDto dto, CancellationToken ct = default)
    {
        InventoryTraceabilitySupport.Normalize(dto);
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        InventoryTraceabilitySupport.EnsureSafe(dto.Notes, dto.MetadataJson);
        await InventoryTraceabilitySupport.EnsureVariantExistsAsync(_db, dto.ProductVariantId, _localizer, ct).ConfigureAwait(false);
        await InventoryTraceabilitySupport.EnsurePolicyAvailableAsync(_db, dto.BusinessId, dto.ProductVariantId, null, _localizer, ct).ConfigureAwait(false);

        var policy = new ProductTrackingPolicy
        {
            BusinessId = dto.BusinessId,
            ProductVariantId = dto.ProductVariantId,
            TrackingMode = dto.TrackingMode,
            Status = dto.Status,
            RequiresSupplierLot = dto.RequiresSupplierLot,
            RequiresExpiryDate = dto.RequiresExpiryDate || dto.TrackingMode is ProductTrackingMode.LotAndExpiryTracked or ProductTrackingMode.SerialAndExpiryTracked,
            RequiresHandlingUnit = dto.RequiresHandlingUnit,
            Notes = InventoryManagementHandlerSupport.NormalizeOptional(dto.Notes),
            MetadataJson = InventoryManagementHandlerSupport.NormalizeMetadataJson(dto.MetadataJson)
        };

        _db.Set<ProductTrackingPolicy>().Add(policy);
        await InventoryTraceabilitySupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, policy.BusinessId, "ProductTrackingPolicy", policy.Id, "inventory.product_tracking_policy.created", AuditTrailAction.Created, ct).ConfigureAwait(false);
        return policy.Id;
    }
}

public sealed class UpdateProductTrackingPolicyHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<ProductTrackingPolicyEditDto> _validator;
    private readonly IStringLocalizer<ValidationResource> _localizer;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public UpdateProductTrackingPolicyHandler(IAppDbContext db, IValidator<ProductTrackingPolicyEditDto> validator, IStringLocalizer<ValidationResource> localizer, IClock clock, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events;
    }

    public async Task HandleAsync(ProductTrackingPolicyEditDto dto, CancellationToken ct = default)
    {
        InventoryTraceabilitySupport.Normalize(dto);
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        InventoryTraceabilitySupport.EnsureSafe(dto.Notes, dto.MetadataJson);
        await InventoryTraceabilitySupport.EnsureVariantExistsAsync(_db, dto.ProductVariantId, _localizer, ct).ConfigureAwait(false);

        var policy = await _db.Set<ProductTrackingPolicy>().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (policy is null) throw new InvalidOperationException(_localizer["ProductTrackingPolicyNotFound"]);
        InventoryTraceabilitySupport.EnsureRowVersion(policy.RowVersion, dto.RowVersion, _localizer);
        await InventoryTraceabilitySupport.EnsurePolicyAvailableAsync(_db, dto.BusinessId, dto.ProductVariantId, dto.Id, _localizer, ct).ConfigureAwait(false);

        policy.BusinessId = dto.BusinessId;
        policy.ProductVariantId = dto.ProductVariantId;
        policy.TrackingMode = dto.TrackingMode;
        policy.Status = dto.Status;
        policy.RequiresSupplierLot = dto.RequiresSupplierLot;
        policy.RequiresExpiryDate = dto.RequiresExpiryDate || dto.TrackingMode is ProductTrackingMode.LotAndExpiryTracked or ProductTrackingMode.SerialAndExpiryTracked;
        policy.RequiresHandlingUnit = dto.RequiresHandlingUnit;
        policy.Notes = InventoryManagementHandlerSupport.NormalizeOptional(dto.Notes);
        policy.MetadataJson = InventoryManagementHandlerSupport.NormalizeMetadataJson(dto.MetadataJson);

        await InventoryTraceabilitySupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, policy.BusinessId, "ProductTrackingPolicy", policy.Id, "inventory.product_tracking_policy.updated", AuditTrailAction.Updated, ct).ConfigureAwait(false);
    }
}

public sealed class ArchiveProductTrackingPolicyHandler
{
    private readonly IAppDbContext _db;
    private readonly IStringLocalizer<ValidationResource> _localizer;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public ArchiveProductTrackingPolicyHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer, IClock clock, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events;
    }

    public async Task<Result> HandleAsync(ProductTrackingPolicyArchiveDto dto, CancellationToken ct = default)
    {
        var policy = await _db.Set<ProductTrackingPolicy>().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (policy is null) return Result.Fail(_localizer["ProductTrackingPolicyNotFound"]);
        var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
        if (rowVersion.Length == 0 || !(policy.RowVersion ?? Array.Empty<byte>()).SequenceEqual(rowVersion)) return Result.Fail(_localizer["ItemConcurrencyConflict"]);
        policy.Status = ProductTrackingPolicyStatus.Archived;
        policy.IsDeleted = true;
        await InventoryTraceabilitySupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, policy.BusinessId, "ProductTrackingPolicy", policy.Id, "inventory.product_tracking_policy.archived", AuditTrailAction.Deleted, ct).ConfigureAwait(false);
        return Result.Ok();
    }
}

public sealed class CreateInventoryLotHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<InventoryLotCreateDto> _validator;
    private readonly IStringLocalizer<ValidationResource> _localizer;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public CreateInventoryLotHandler(IAppDbContext db, IValidator<InventoryLotCreateDto> validator, IStringLocalizer<ValidationResource> localizer, IClock clock, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events;
    }

    public async Task<Guid> HandleAsync(InventoryLotCreateDto dto, CancellationToken ct = default)
    {
        InventoryTraceabilitySupport.Normalize(dto);
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        InventoryTraceabilitySupport.EnsureSafe(dto.LotCode, dto.SupplierLotCode, dto.Notes, dto.MetadataJson);
        await InventoryTraceabilitySupport.EnsureVariantExistsAsync(_db, dto.ProductVariantId, _localizer, ct).ConfigureAwait(false);
        await InventoryTraceabilitySupport.EnsureLotAvailableAsync(_db, dto.BusinessId, dto.ProductVariantId, dto.LotCode, null, _localizer, ct).ConfigureAwait(false);

        var lot = new InventoryLot
        {
            BusinessId = dto.BusinessId,
            ProductVariantId = dto.ProductVariantId,
            LotCode = dto.LotCode,
            SupplierLotCode = InventoryManagementHandlerSupport.NormalizeOptional(dto.SupplierLotCode),
            ManufactureDateUtc = dto.ManufactureDateUtc,
            ExpiryDateUtc = dto.ExpiryDateUtc,
            Status = dto.Status,
            Notes = InventoryManagementHandlerSupport.NormalizeOptional(dto.Notes),
            MetadataJson = InventoryManagementHandlerSupport.NormalizeMetadataJson(dto.MetadataJson)
        };
        _db.Set<InventoryLot>().Add(lot);
        await InventoryTraceabilitySupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, lot.BusinessId, "InventoryLot", lot.Id, "inventory.lot.created", AuditTrailAction.Created, ct).ConfigureAwait(false);
        return lot.Id;
    }
}

public sealed class UpdateInventoryLotHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<InventoryLotEditDto> _validator;
    private readonly IStringLocalizer<ValidationResource> _localizer;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public UpdateInventoryLotHandler(IAppDbContext db, IValidator<InventoryLotEditDto> validator, IStringLocalizer<ValidationResource> localizer, IClock clock, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events;
    }

    public async Task HandleAsync(InventoryLotEditDto dto, CancellationToken ct = default)
    {
        InventoryTraceabilitySupport.Normalize(dto);
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        InventoryTraceabilitySupport.EnsureSafe(dto.LotCode, dto.SupplierLotCode, dto.Notes, dto.MetadataJson);
        await InventoryTraceabilitySupport.EnsureVariantExistsAsync(_db, dto.ProductVariantId, _localizer, ct).ConfigureAwait(false);

        var lot = await _db.Set<InventoryLot>().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (lot is null) throw new InvalidOperationException(_localizer["InventoryLotNotFound"]);
        InventoryTraceabilitySupport.EnsureRowVersion(lot.RowVersion, dto.RowVersion, _localizer);
        await InventoryTraceabilitySupport.EnsureLotAvailableAsync(_db, dto.BusinessId, dto.ProductVariantId, dto.LotCode, dto.Id, _localizer, ct).ConfigureAwait(false);

        lot.BusinessId = dto.BusinessId;
        lot.ProductVariantId = dto.ProductVariantId;
        lot.LotCode = dto.LotCode;
        lot.SupplierLotCode = InventoryManagementHandlerSupport.NormalizeOptional(dto.SupplierLotCode);
        lot.ManufactureDateUtc = dto.ManufactureDateUtc;
        lot.ExpiryDateUtc = dto.ExpiryDateUtc;
        lot.Status = dto.Status;
        lot.Notes = InventoryManagementHandlerSupport.NormalizeOptional(dto.Notes);
        lot.MetadataJson = InventoryManagementHandlerSupport.NormalizeMetadataJson(dto.MetadataJson);
        await InventoryTraceabilitySupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, lot.BusinessId, "InventoryLot", lot.Id, "inventory.lot.updated", AuditTrailAction.Updated, ct).ConfigureAwait(false);
    }
}

public sealed class ArchiveInventoryLotHandler
{
    private readonly IAppDbContext _db;
    private readonly IStringLocalizer<ValidationResource> _localizer;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public ArchiveInventoryLotHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer, IClock clock, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events;
    }

    public async Task<Result> HandleAsync(InventoryLotArchiveDto dto, CancellationToken ct = default)
    {
        var lot = await _db.Set<InventoryLot>().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (lot is null) return Result.Fail(_localizer["InventoryLotNotFound"]);
        if ((dto.RowVersion ?? Array.Empty<byte>()).Length == 0 || !(lot.RowVersion ?? Array.Empty<byte>()).SequenceEqual(dto.RowVersion)) return Result.Fail(_localizer["ItemConcurrencyConflict"]);
        var hasSerials = await _db.Set<InventorySerialUnit>().AsNoTracking().AnyAsync(x => x.InventoryLotId == lot.Id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (hasSerials) return Result.Fail(_localizer["InventoryLotArchiveHasSerials"]);
        lot.IsDeleted = true;
        await InventoryTraceabilitySupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, lot.BusinessId, "InventoryLot", lot.Id, "inventory.lot.archived", AuditTrailAction.Deleted, ct).ConfigureAwait(false);
        return Result.Ok();
    }
}

public sealed class CreateInventorySerialUnitHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<InventorySerialUnitCreateDto> _validator;
    private readonly IStringLocalizer<ValidationResource> _localizer;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public CreateInventorySerialUnitHandler(IAppDbContext db, IValidator<InventorySerialUnitCreateDto> validator, IStringLocalizer<ValidationResource> localizer, IClock clock, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events;
    }

    public async Task<Guid> HandleAsync(InventorySerialUnitCreateDto dto, CancellationToken ct = default)
    {
        InventoryTraceabilitySupport.Normalize(dto);
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        InventoryTraceabilitySupport.EnsureSafe(dto.SerialNumber, dto.Notes, dto.MetadataJson);
        await InventoryTraceabilitySupport.EnsureVariantExistsAsync(_db, dto.ProductVariantId, _localizer, ct).ConfigureAwait(false);
        await InventoryTraceabilitySupport.EnsureLotMatchesAsync(_db, dto.BusinessId, dto.ProductVariantId, dto.InventoryLotId, _localizer, ct).ConfigureAwait(false);
        await InventoryTraceabilitySupport.EnsureSerialAvailableAsync(_db, dto.BusinessId, dto.ProductVariantId, dto.SerialNumber, null, _localizer, ct).ConfigureAwait(false);

        var serial = new InventorySerialUnit
        {
            BusinessId = dto.BusinessId,
            ProductVariantId = dto.ProductVariantId,
            InventoryLotId = dto.InventoryLotId,
            SerialNumber = dto.SerialNumber,
            ManufactureDateUtc = dto.ManufactureDateUtc,
            ExpiryDateUtc = dto.ExpiryDateUtc,
            Status = dto.Status,
            Notes = InventoryManagementHandlerSupport.NormalizeOptional(dto.Notes),
            MetadataJson = InventoryManagementHandlerSupport.NormalizeMetadataJson(dto.MetadataJson)
        };
        _db.Set<InventorySerialUnit>().Add(serial);
        await InventoryTraceabilitySupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, serial.BusinessId, "InventorySerialUnit", serial.Id, "inventory.serial.created", AuditTrailAction.Created, ct).ConfigureAwait(false);
        return serial.Id;
    }
}

public sealed class UpdateInventorySerialUnitHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<InventorySerialUnitEditDto> _validator;
    private readonly IStringLocalizer<ValidationResource> _localizer;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public UpdateInventorySerialUnitHandler(IAppDbContext db, IValidator<InventorySerialUnitEditDto> validator, IStringLocalizer<ValidationResource> localizer, IClock clock, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events;
    }

    public async Task HandleAsync(InventorySerialUnitEditDto dto, CancellationToken ct = default)
    {
        InventoryTraceabilitySupport.Normalize(dto);
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        InventoryTraceabilitySupport.EnsureSafe(dto.SerialNumber, dto.Notes, dto.MetadataJson);
        await InventoryTraceabilitySupport.EnsureVariantExistsAsync(_db, dto.ProductVariantId, _localizer, ct).ConfigureAwait(false);
        await InventoryTraceabilitySupport.EnsureLotMatchesAsync(_db, dto.BusinessId, dto.ProductVariantId, dto.InventoryLotId, _localizer, ct).ConfigureAwait(false);

        var serial = await _db.Set<InventorySerialUnit>().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (serial is null) throw new InvalidOperationException(_localizer["InventorySerialUnitNotFound"]);
        InventoryTraceabilitySupport.EnsureRowVersion(serial.RowVersion, dto.RowVersion, _localizer);
        await InventoryTraceabilitySupport.EnsureSerialAvailableAsync(_db, dto.BusinessId, dto.ProductVariantId, dto.SerialNumber, dto.Id, _localizer, ct).ConfigureAwait(false);

        serial.BusinessId = dto.BusinessId;
        serial.ProductVariantId = dto.ProductVariantId;
        serial.InventoryLotId = dto.InventoryLotId;
        serial.SerialNumber = dto.SerialNumber;
        serial.ManufactureDateUtc = dto.ManufactureDateUtc;
        serial.ExpiryDateUtc = dto.ExpiryDateUtc;
        serial.Status = dto.Status;
        serial.Notes = InventoryManagementHandlerSupport.NormalizeOptional(dto.Notes);
        serial.MetadataJson = InventoryManagementHandlerSupport.NormalizeMetadataJson(dto.MetadataJson);
        await InventoryTraceabilitySupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, serial.BusinessId, "InventorySerialUnit", serial.Id, "inventory.serial.updated", AuditTrailAction.Updated, ct).ConfigureAwait(false);
    }
}

public sealed class ArchiveInventorySerialUnitHandler
{
    private readonly IAppDbContext _db;
    private readonly IStringLocalizer<ValidationResource> _localizer;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public ArchiveInventorySerialUnitHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer, IClock clock, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events;
    }

    public async Task<Result> HandleAsync(InventorySerialUnitArchiveDto dto, CancellationToken ct = default)
    {
        var serial = await _db.Set<InventorySerialUnit>().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (serial is null) return Result.Fail(_localizer["InventorySerialUnitNotFound"]);
        if ((dto.RowVersion ?? Array.Empty<byte>()).Length == 0 || !(serial.RowVersion ?? Array.Empty<byte>()).SequenceEqual(dto.RowVersion)) return Result.Fail(_localizer["ItemConcurrencyConflict"]);
        serial.IsDeleted = true;
        await InventoryTraceabilitySupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, serial.BusinessId, "InventorySerialUnit", serial.Id, "inventory.serial.archived", AuditTrailAction.Deleted, ct).ConfigureAwait(false);
        return Result.Ok();
    }
}

public sealed class CreateHandlingUnitHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<HandlingUnitCreateDto> _validator;
    private readonly IStringLocalizer<ValidationResource> _localizer;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public CreateHandlingUnitHandler(IAppDbContext db, IValidator<HandlingUnitCreateDto> validator, IStringLocalizer<ValidationResource> localizer, IClock clock, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events;
    }

    public async Task<Guid> HandleAsync(HandlingUnitCreateDto dto, CancellationToken ct = default)
    {
        InventoryTraceabilitySupport.Normalize(dto);
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        InventoryTraceabilitySupport.EnsureSafe(dto.Code, dto.DisplayName, dto.Barcode, dto.Notes, dto.MetadataJson);
        await InventoryTraceabilitySupport.EnsureHandlingUnitLinksAsync(_db, dto, null, _localizer, ct).ConfigureAwait(false);
        await InventoryTraceabilitySupport.EnsureHandlingUnitCodeAvailableAsync(_db, dto.BusinessId, dto.Code, null, _localizer, ct).ConfigureAwait(false);

        var handlingUnit = new HandlingUnit
        {
            BusinessId = dto.BusinessId,
            WarehouseId = dto.WarehouseId,
            LocationId = dto.LocationId,
            ParentHandlingUnitId = dto.ParentHandlingUnitId,
            Code = dto.Code,
            DisplayName = dto.DisplayName.Trim(),
            Barcode = InventoryManagementHandlerSupport.NormalizeOptional(dto.Barcode),
            HandlingUnitType = dto.HandlingUnitType,
            Status = dto.Status,
            Notes = InventoryManagementHandlerSupport.NormalizeOptional(dto.Notes),
            MetadataJson = InventoryManagementHandlerSupport.NormalizeMetadataJson(dto.MetadataJson),
            Contents = dto.Contents.Select((line, index) => InventoryTraceabilitySupport.MapHandlingUnitContent(line, index)).ToList()
        };
        _db.Set<HandlingUnit>().Add(handlingUnit);
        await InventoryTraceabilitySupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, handlingUnit.BusinessId, "HandlingUnit", handlingUnit.Id, "inventory.handling_unit.created", AuditTrailAction.Created, ct).ConfigureAwait(false);
        return handlingUnit.Id;
    }
}

public sealed class UpdateHandlingUnitHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<HandlingUnitEditDto> _validator;
    private readonly IStringLocalizer<ValidationResource> _localizer;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public UpdateHandlingUnitHandler(IAppDbContext db, IValidator<HandlingUnitEditDto> validator, IStringLocalizer<ValidationResource> localizer, IClock clock, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events;
    }

    public async Task HandleAsync(HandlingUnitEditDto dto, CancellationToken ct = default)
    {
        InventoryTraceabilitySupport.Normalize(dto);
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        InventoryTraceabilitySupport.EnsureSafe(dto.Code, dto.DisplayName, dto.Barcode, dto.Notes, dto.MetadataJson);

        var handlingUnit = await _db.Set<HandlingUnit>().Include(x => x.Contents).FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (handlingUnit is null) throw new InvalidOperationException(_localizer["HandlingUnitNotFound"]);
        InventoryTraceabilitySupport.EnsureRowVersion(handlingUnit.RowVersion, dto.RowVersion, _localizer);
        await InventoryTraceabilitySupport.EnsureHandlingUnitLinksAsync(_db, dto, dto.Id, _localizer, ct).ConfigureAwait(false);
        await InventoryTraceabilitySupport.EnsureHandlingUnitCodeAvailableAsync(_db, dto.BusinessId, dto.Code, dto.Id, _localizer, ct).ConfigureAwait(false);

        handlingUnit.BusinessId = dto.BusinessId;
        handlingUnit.WarehouseId = dto.WarehouseId;
        handlingUnit.LocationId = dto.LocationId;
        handlingUnit.ParentHandlingUnitId = dto.ParentHandlingUnitId;
        handlingUnit.Code = dto.Code;
        handlingUnit.DisplayName = dto.DisplayName.Trim();
        handlingUnit.Barcode = InventoryManagementHandlerSupport.NormalizeOptional(dto.Barcode);
        handlingUnit.HandlingUnitType = dto.HandlingUnitType;
        handlingUnit.Status = dto.Status;
        handlingUnit.Notes = InventoryManagementHandlerSupport.NormalizeOptional(dto.Notes);
        handlingUnit.MetadataJson = InventoryManagementHandlerSupport.NormalizeMetadataJson(dto.MetadataJson);

        _db.Set<HandlingUnitContent>().RemoveRange(handlingUnit.Contents);
        handlingUnit.Contents = dto.Contents.Select((line, index) => InventoryTraceabilitySupport.MapHandlingUnitContent(line, index)).ToList();
        await InventoryTraceabilitySupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, handlingUnit.BusinessId, "HandlingUnit", handlingUnit.Id, "inventory.handling_unit.updated", AuditTrailAction.Updated, ct).ConfigureAwait(false);
    }
}

public sealed class ArchiveHandlingUnitHandler
{
    private readonly IAppDbContext _db;
    private readonly IStringLocalizer<ValidationResource> _localizer;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public ArchiveHandlingUnitHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer, IClock clock, BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events;
    }

    public async Task<Result> HandleAsync(HandlingUnitArchiveDto dto, CancellationToken ct = default)
    {
        var handlingUnit = await _db.Set<HandlingUnit>().FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (handlingUnit is null) return Result.Fail(_localizer["HandlingUnitNotFound"]);
        if ((dto.RowVersion ?? Array.Empty<byte>()).Length == 0 || !(handlingUnit.RowVersion ?? Array.Empty<byte>()).SequenceEqual(dto.RowVersion)) return Result.Fail(_localizer["ItemConcurrencyConflict"]);
        var hasChildren = await _db.Set<HandlingUnit>().AsNoTracking().AnyAsync(x => x.ParentHandlingUnitId == handlingUnit.Id && !x.IsDeleted, ct).ConfigureAwait(false);
        if (hasChildren) return Result.Fail(_localizer["HandlingUnitArchiveHasChildren"]);
        handlingUnit.IsDeleted = true;
        await InventoryTraceabilitySupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, handlingUnit.BusinessId, "HandlingUnit", handlingUnit.Id, "inventory.handling_unit.archived", AuditTrailAction.Deleted, ct).ConfigureAwait(false);
        return Result.Ok();
    }
}

internal static class InventoryTraceabilitySupport
{
    public static void Normalize(ProductTrackingPolicyCreateDto dto)
    {
        dto.Notes = InventoryManagementHandlerSupport.NormalizeOptional(dto.Notes);
        dto.MetadataJson = InventoryManagementHandlerSupport.NormalizeMetadataJson(dto.MetadataJson);
    }

    public static void Normalize(InventoryLotCreateDto dto)
    {
        dto.LotCode = InventoryManagementHandlerSupport.NormalizeRequiredCode(dto.LotCode);
        dto.SupplierLotCode = InventoryManagementHandlerSupport.NormalizeOptional(dto.SupplierLotCode);
        dto.Notes = InventoryManagementHandlerSupport.NormalizeOptional(dto.Notes);
        dto.MetadataJson = InventoryManagementHandlerSupport.NormalizeMetadataJson(dto.MetadataJson);
    }

    public static void Normalize(InventorySerialUnitCreateDto dto)
    {
        dto.SerialNumber = InventoryManagementHandlerSupport.NormalizeRequiredCode(dto.SerialNumber);
        dto.Notes = InventoryManagementHandlerSupport.NormalizeOptional(dto.Notes);
        dto.MetadataJson = InventoryManagementHandlerSupport.NormalizeMetadataJson(dto.MetadataJson);
    }

    public static void Normalize(HandlingUnitCreateDto dto)
    {
        dto.Code = InventoryManagementHandlerSupport.NormalizeRequiredCode(dto.Code);
        dto.DisplayName = dto.DisplayName?.Trim() ?? string.Empty;
        dto.Barcode = InventoryManagementHandlerSupport.NormalizeOptional(dto.Barcode);
        dto.Notes = InventoryManagementHandlerSupport.NormalizeOptional(dto.Notes);
        dto.MetadataJson = InventoryManagementHandlerSupport.NormalizeMetadataJson(dto.MetadataJson);
        dto.Contents ??= new List<HandlingUnitContentDto>();
        foreach (var line in dto.Contents)
        {
            line.SkuSnapshot = InventoryManagementHandlerSupport.NormalizeOptional(line.SkuSnapshot);
            line.Description = line.Description?.Trim() ?? string.Empty;
            line.MetadataJson = InventoryManagementHandlerSupport.NormalizeMetadataJson(line.MetadataJson);
        }
    }

    public static void EnsureSafe(params string?[] values)
    {
        if (values.Any(FoundationInputNormalizer.LooksSensitive))
        {
            throw new ArgumentException("InventoryTraceabilitySensitiveMetadataRejected");
        }
    }

    public static void EnsureRowVersion(byte[] current, byte[] requested, IStringLocalizer<ValidationResource> localizer)
    {
        var currentVersion = current ?? Array.Empty<byte>();
        var requestedVersion = requested ?? Array.Empty<byte>();
        if (requestedVersion.Length == 0 || !currentVersion.SequenceEqual(requestedVersion))
        {
            throw new DbUpdateConcurrencyException(localizer["ConcurrencyConflictDetected"]);
        }
    }

    public static async Task EnsureVariantExistsAsync(IAppDbContext db, Guid productVariantId, IStringLocalizer<ValidationResource> localizer, CancellationToken ct)
    {
        var exists = await db.Set<ProductVariant>().AsNoTracking().AnyAsync(x => x.Id == productVariantId && !x.IsDeleted, ct).ConfigureAwait(false);
        if (!exists) throw new InvalidOperationException(localizer["ProductVariantNotFound"]);
    }

    public static async Task EnsurePolicyAvailableAsync(IAppDbContext db, Guid businessId, Guid productVariantId, Guid? currentId, IStringLocalizer<ValidationResource> localizer, CancellationToken ct)
    {
        var exists = await db.Set<ProductTrackingPolicy>().AsNoTracking().AnyAsync(x => x.BusinessId == businessId && x.ProductVariantId == productVariantId && !x.IsDeleted && (!currentId.HasValue || x.Id != currentId.Value), ct).ConfigureAwait(false);
        if (exists) throw new InvalidOperationException(localizer["ProductTrackingPolicyAlreadyExists"]);
    }

    public static async Task EnsureLotAvailableAsync(IAppDbContext db, Guid businessId, Guid productVariantId, string lotCode, Guid? currentId, IStringLocalizer<ValidationResource> localizer, CancellationToken ct)
    {
        var exists = await db.Set<InventoryLot>().AsNoTracking().AnyAsync(x => x.BusinessId == businessId && x.ProductVariantId == productVariantId && x.LotCode == lotCode && !x.IsDeleted && (!currentId.HasValue || x.Id != currentId.Value), ct).ConfigureAwait(false);
        if (exists) throw new InvalidOperationException(localizer["InventoryLotAlreadyExists"]);
    }

    public static async Task EnsureSerialAvailableAsync(IAppDbContext db, Guid businessId, Guid productVariantId, string serialNumber, Guid? currentId, IStringLocalizer<ValidationResource> localizer, CancellationToken ct)
    {
        var exists = await db.Set<InventorySerialUnit>().AsNoTracking().AnyAsync(x => x.BusinessId == businessId && x.ProductVariantId == productVariantId && x.SerialNumber == serialNumber && !x.IsDeleted && (!currentId.HasValue || x.Id != currentId.Value), ct).ConfigureAwait(false);
        if (exists) throw new InvalidOperationException(localizer["InventorySerialUnitAlreadyExists"]);
    }

    public static async Task EnsureLotMatchesAsync(IAppDbContext db, Guid businessId, Guid productVariantId, Guid? lotId, IStringLocalizer<ValidationResource> localizer, CancellationToken ct)
    {
        if (!lotId.HasValue) return;
        var exists = await db.Set<InventoryLot>().AsNoTracking().AnyAsync(x => x.Id == lotId.Value && x.BusinessId == businessId && x.ProductVariantId == productVariantId && !x.IsDeleted, ct).ConfigureAwait(false);
        if (!exists) throw new InvalidOperationException(localizer["InventoryLotNotFound"]);
    }

    public static async Task EnsureHandlingUnitCodeAvailableAsync(IAppDbContext db, Guid businessId, string code, Guid? currentId, IStringLocalizer<ValidationResource> localizer, CancellationToken ct)
    {
        var exists = await db.Set<HandlingUnit>().AsNoTracking().AnyAsync(x => x.BusinessId == businessId && x.Code == code && !x.IsDeleted && (!currentId.HasValue || x.Id != currentId.Value), ct).ConfigureAwait(false);
        if (exists) throw new InvalidOperationException(localizer["HandlingUnitCodeAlreadyExists"]);
    }

    public static async Task EnsureHandlingUnitLinksAsync(IAppDbContext db, HandlingUnitCreateDto dto, Guid? currentId, IStringLocalizer<ValidationResource> localizer, CancellationToken ct)
    {
        if (dto.WarehouseId.HasValue)
        {
            var warehouseExists = await db.Set<Warehouse>().AsNoTracking().AnyAsync(x => x.Id == dto.WarehouseId.Value && x.BusinessId == dto.BusinessId && !x.IsDeleted, ct).ConfigureAwait(false);
            if (!warehouseExists) throw new InvalidOperationException(localizer["WarehouseNotFound"]);
        }

        if (dto.LocationId.HasValue)
        {
            var locationExists = await db.Set<WarehouseLocation>().AsNoTracking().AnyAsync(x => x.Id == dto.LocationId.Value && x.BusinessId == dto.BusinessId && !x.IsDeleted && (!dto.WarehouseId.HasValue || x.WarehouseId == dto.WarehouseId.Value), ct).ConfigureAwait(false);
            if (!locationExists) throw new InvalidOperationException(localizer["WarehouseLocationNotFound"]);
        }

        if (dto.ParentHandlingUnitId.HasValue)
        {
            if (currentId.HasValue && dto.ParentHandlingUnitId.Value == currentId.Value) throw new InvalidOperationException(localizer["HandlingUnitParentInvalid"]);
            var parent = await db.Set<HandlingUnit>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == dto.ParentHandlingUnitId.Value && !x.IsDeleted, ct).ConfigureAwait(false);
            if (parent is null || parent.BusinessId != dto.BusinessId) throw new InvalidOperationException(localizer["HandlingUnitParentInvalid"]);
            await EnsureNoHandlingUnitCycleAsync(db, currentId, dto.ParentHandlingUnitId, localizer, ct).ConfigureAwait(false);
        }

        foreach (var line in dto.Contents)
        {
            await EnsureVariantExistsAsync(db, line.ProductVariantId, localizer, ct).ConfigureAwait(false);
            await EnsureLotMatchesAsync(db, dto.BusinessId, line.ProductVariantId, line.InventoryLotId, localizer, ct).ConfigureAwait(false);
            if (line.InventorySerialUnitId.HasValue)
            {
                var serialExists = await db.Set<InventorySerialUnit>().AsNoTracking().AnyAsync(x => x.Id == line.InventorySerialUnitId.Value && x.BusinessId == dto.BusinessId && x.ProductVariantId == line.ProductVariantId && (!line.InventoryLotId.HasValue || x.InventoryLotId == line.InventoryLotId.Value) && !x.IsDeleted, ct).ConfigureAwait(false);
                if (!serialExists) throw new InvalidOperationException(localizer["InventorySerialUnitNotFound"]);
                if (line.Quantity != 1) throw new InvalidOperationException(localizer["HandlingUnitSerialQuantityMustBeOne"]);
            }
        }
    }

    private static async Task EnsureNoHandlingUnitCycleAsync(IAppDbContext db, Guid? currentId, Guid? parentId, IStringLocalizer<ValidationResource> localizer, CancellationToken ct)
    {
        if (!currentId.HasValue) return;
        var seen = new HashSet<Guid> { currentId.Value };
        var cursor = parentId;
        while (cursor.HasValue)
        {
            if (!seen.Add(cursor.Value)) throw new InvalidOperationException(localizer["HandlingUnitParentInvalid"]);
            cursor = await db.Set<HandlingUnit>().AsNoTracking().Where(x => x.Id == cursor.Value && !x.IsDeleted).Select(x => x.ParentHandlingUnitId).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        }
    }

    public static HandlingUnitContent MapHandlingUnitContent(HandlingUnitContentDto dto, int index) => new()
    {
        ProductVariantId = dto.ProductVariantId,
        InventoryLotId = dto.InventoryLotId,
        InventorySerialUnitId = dto.InventorySerialUnitId,
        Quantity = dto.Quantity,
        SkuSnapshot = InventoryManagementHandlerSupport.NormalizeOptional(dto.SkuSnapshot),
        Description = dto.Description.Trim(),
        SortOrder = dto.SortOrder == 0 ? index + 1 : dto.SortOrder,
        MetadataJson = InventoryManagementHandlerSupport.NormalizeMetadataJson(dto.MetadataJson)
    };

    public static async Task RecordEvidenceOrSaveAsync(IAppDbContext db, BusinessEventService? events, IClock clock, Guid businessId, string entityType, Guid entityId, string eventKeyPrefix, AuditTrailAction auditAction, CancellationToken ct)
    {
        if (events is null)
        {
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            return;
        }

        var now = clock.UtcNow;
        var payload = $$"""{"entityType":"{{entityType}}","entityId":"{{entityId}}","businessId":"{{businessId}}"}""";
        var eventResult = await events.AddEventAsync(
            new AddBusinessEventCommand(
                businessId,
                entityType,
                entityId,
                eventKeyPrefix,
                $"{eventKeyPrefix}:{entityId}",
                now,
                null,
                BusinessEventSource.User,
                BusinessEventSeverity.Info,
                FoundationVisibility.Internal,
                eventKeyPrefix,
                null,
                null,
                null,
                payload),
            ct).ConfigureAwait(false);
        if (!eventResult.Succeeded) throw new InvalidOperationException(eventResult.Error);

        var auditResult = await events.AddAuditTrailAsync(new AddAuditTrailCommand(businessId, entityType, entityId, auditAction, now, null, eventResult.Value, eventKeyPrefix, null, payload), ct).ConfigureAwait(false);
        if (!auditResult.Succeeded) throw new InvalidOperationException(auditResult.Error);
    }
}
