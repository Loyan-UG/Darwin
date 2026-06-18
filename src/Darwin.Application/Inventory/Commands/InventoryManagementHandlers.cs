using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Foundation;
using Darwin.Application.Inventory.DTOs;
using Darwin.Application.Inventory;
using Darwin.Domain.Entities.Inventory;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Localization;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Inventory.Commands
{
    public sealed class CreateWarehouseHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<WarehouseCreateDto> _validator;

        public CreateWarehouseHandler(IAppDbContext db, IValidator<WarehouseCreateDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task<Guid> HandleAsync(WarehouseCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            if (dto.IsDefault)
            {
                var existingDefaults = await _db.Set<Warehouse>()
                    .Where(x => x.BusinessId == dto.BusinessId && x.IsDefault)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                foreach (var warehouse in existingDefaults)
                {
                    warehouse.IsDefault = false;
                }
            }

            var warehouseEntity = new Warehouse
            {
                BusinessId = dto.BusinessId,
                Name = dto.Name.Trim(),
                Description = InventoryManagementHandlerSupport.NormalizeOptional(dto.Description),
                Location = InventoryManagementHandlerSupport.NormalizeOptional(dto.Location),
                IsDefault = dto.IsDefault
            };

            _db.Set<Warehouse>().Add(warehouseEntity);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return warehouseEntity.Id;
        }

    }

    public sealed class UpdateWarehouseHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<WarehouseEditDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateWarehouseHandler(
            IAppDbContext db,
            IValidator<WarehouseEditDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task HandleAsync(WarehouseEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var warehouse = await _db.Set<Warehouse>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (warehouse is null)
            {
                throw new InvalidOperationException(_localizer["WarehouseNotFound"]);
            }

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            var currentVersion = warehouse.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0 || !currentVersion.SequenceEqual(rowVersion))
            {
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
            }

            if (dto.IsDefault)
            {
                var existingDefaults = await _db.Set<Warehouse>()
                    .Where(x => x.BusinessId == dto.BusinessId && x.IsDefault && x.Id != dto.Id)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                foreach (var existing in existingDefaults)
                {
                    existing.IsDefault = false;
                }
            }

            warehouse.BusinessId = dto.BusinessId;
            warehouse.Name = dto.Name.Trim();
            warehouse.Description = InventoryManagementHandlerSupport.NormalizeOptional(dto.Description);
            warehouse.Location = InventoryManagementHandlerSupport.NormalizeOptional(dto.Location);
            warehouse.IsDefault = dto.IsDefault;

            await InventoryManagementHandlerSupport.SaveChangesOrThrowConcurrencyAsync(_db, _localizer, ct).ConfigureAwait(false);
        }

    }

    public sealed class CreateWarehouseLocationHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<WarehouseLocationCreateDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;
        private readonly IClock _clock;
        private readonly BusinessEventService? _events;

        public CreateWarehouseLocationHandler(
            IAppDbContext db,
            IValidator<WarehouseLocationCreateDto> validator,
            IStringLocalizer<ValidationResource> localizer,
            IClock clock,
            BusinessEventService? events = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _events = events;
        }

        public async Task<Guid> HandleAsync(WarehouseLocationCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);
            EnsureSafeWarehouseLocationText(dto);

            var normalizedCode = InventoryManagementHandlerSupport.NormalizeRequiredCode(dto.Code);
            var normalizedBarcode = InventoryManagementHandlerSupport.NormalizeOptional(dto.Barcode);
            var normalizedMetadata = InventoryManagementHandlerSupport.NormalizeMetadataJson(dto.MetadataJson);

            await EnsureWarehouseAndParentAsync(dto.BusinessId, dto.WarehouseId, dto.ParentLocationId, null, ct).ConfigureAwait(false);
            await EnsureWarehouseLocationCodeAvailableAsync(dto.BusinessId, dto.WarehouseId, normalizedCode, null, ct).ConfigureAwait(false);

            var location = new WarehouseLocation
            {
                BusinessId = dto.BusinessId,
                WarehouseId = dto.WarehouseId,
                ParentLocationId = dto.ParentLocationId,
                Code = normalizedCode,
                DisplayName = dto.DisplayName.Trim(),
                LocationType = dto.LocationType,
                Status = dto.Status,
                Barcode = normalizedBarcode,
                SortOrder = dto.SortOrder,
                Description = InventoryManagementHandlerSupport.NormalizeOptional(dto.Description),
                MetadataJson = normalizedMetadata
            };

            _db.Set<WarehouseLocation>().Add(location);
            await RecordWarehouseLocationEvidenceOrSaveAsync(location, "created", AuditTrailAction.Created, ct).ConfigureAwait(false);
            return location.Id;
        }

        private async Task EnsureWarehouseAndParentAsync(Guid businessId, Guid warehouseId, Guid? parentLocationId, Guid? currentLocationId, CancellationToken ct)
        {
            var warehouseExists = await _db.Set<Warehouse>()
                .AsNoTracking()
                .AnyAsync(x => x.Id == warehouseId && x.BusinessId == businessId && !x.IsDeleted, ct)
                .ConfigureAwait(false);
            if (!warehouseExists)
            {
                throw new InvalidOperationException(_localizer["WarehouseNotFound"]);
            }

            if (!parentLocationId.HasValue)
            {
                return;
            }

            if (currentLocationId.HasValue && parentLocationId.Value == currentLocationId.Value)
            {
                throw new InvalidOperationException(_localizer["WarehouseLocationParentInvalid"]);
            }

            var parent = await _db.Set<WarehouseLocation>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == parentLocationId.Value && !x.IsDeleted, ct)
                .ConfigureAwait(false);
            if (parent is null || parent.BusinessId != businessId || parent.WarehouseId != warehouseId)
            {
                throw new InvalidOperationException(_localizer["WarehouseLocationParentInvalid"]);
            }
        }

        private async Task EnsureWarehouseLocationCodeAvailableAsync(Guid businessId, Guid warehouseId, string code, Guid? currentLocationId, CancellationToken ct)
        {
            var exists = await _db.Set<WarehouseLocation>()
                .AsNoTracking()
                .AnyAsync(x => x.BusinessId == businessId
                    && x.WarehouseId == warehouseId
                    && x.Code == code
                    && !x.IsDeleted
                    && (!currentLocationId.HasValue || x.Id != currentLocationId.Value), ct)
                .ConfigureAwait(false);
            if (exists)
            {
                throw new InvalidOperationException(_localizer["WarehouseLocationCodeAlreadyExists"]);
            }
        }

        private async Task RecordWarehouseLocationEvidenceOrSaveAsync(WarehouseLocation location, string action, AuditTrailAction auditAction, CancellationToken ct)
        {
            if (_events is null)
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                return;
            }

            var now = _clock.UtcNow;
            var payload = $$"""
                {"warehouseLocationId":"{{location.Id}}","businessId":"{{location.BusinessId}}","warehouseId":"{{location.WarehouseId}}","parentLocationId":"{{location.ParentLocationId}}","code":"{{location.Code}}","type":"{{location.LocationType}}","status":"{{location.Status}}"}
                """;
            var eventResult = await _events.AddEventAsync(
                    new AddBusinessEventCommand(
                        location.BusinessId,
                        "WarehouseLocation",
                        location.Id,
                        $"inventory.warehouse_location.{action}",
                        $"inventory.warehouse_location.{action}:{location.Id}:{location.Status}",
                        now,
                        null,
                        BusinessEventSource.User,
                        BusinessEventSeverity.Info,
                        FoundationVisibility.Internal,
                        $"Warehouse location {action}",
                        null,
                        null,
                        null,
                        payload),
                    ct)
                .ConfigureAwait(false);
            if (!eventResult.Succeeded) throw new InvalidOperationException(eventResult.Error);

            var auditResult = await _events.AddAuditTrailAsync(
                    new AddAuditTrailCommand(
                        location.BusinessId,
                        "WarehouseLocation",
                        location.Id,
                        auditAction,
                        now,
                        null,
                        eventResult.Value,
                        $"Warehouse location {action}",
                        null,
                        payload),
                    ct)
                .ConfigureAwait(false);
            if (!auditResult.Succeeded) throw new InvalidOperationException(auditResult.Error);
        }

        internal static void EnsureSafeWarehouseLocationText(WarehouseLocationCreateDto dto)
        {
            if (FoundationInputNormalizer.LooksSensitive(dto.Code) ||
                FoundationInputNormalizer.LooksSensitive(dto.DisplayName) ||
                FoundationInputNormalizer.LooksSensitive(dto.Barcode) ||
                FoundationInputNormalizer.LooksSensitive(dto.Description) ||
                FoundationInputNormalizer.LooksSensitive(dto.MetadataJson))
            {
                throw new ArgumentException("WarehouseLocationSensitiveMetadataRejected");
            }
        }
    }

    public sealed class UpdateWarehouseLocationHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<WarehouseLocationEditDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;
        private readonly IClock _clock;
        private readonly BusinessEventService? _events;

        public UpdateWarehouseLocationHandler(
            IAppDbContext db,
            IValidator<WarehouseLocationEditDto> validator,
            IStringLocalizer<ValidationResource> localizer,
            IClock clock,
            BusinessEventService? events = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _events = events;
        }

        public async Task HandleAsync(WarehouseLocationEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);
            CreateWarehouseLocationHandler.EnsureSafeWarehouseLocationText(dto);

            var location = await _db.Set<WarehouseLocation>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct)
                .ConfigureAwait(false);
            if (location is null)
            {
                throw new InvalidOperationException(_localizer["WarehouseLocationNotFound"]);
            }

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            var currentVersion = location.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0 || !currentVersion.SequenceEqual(rowVersion))
            {
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
            }

            var normalizedCode = InventoryManagementHandlerSupport.NormalizeRequiredCode(dto.Code);
            await EnsureWarehouseAndParentAsync(dto.BusinessId, dto.WarehouseId, dto.ParentLocationId, dto.Id, ct).ConfigureAwait(false);
            await EnsureNoParentCycleAsync(dto.Id, dto.ParentLocationId, ct).ConfigureAwait(false);
            await EnsureWarehouseLocationCodeAvailableAsync(dto.BusinessId, dto.WarehouseId, normalizedCode, dto.Id, ct).ConfigureAwait(false);

            location.BusinessId = dto.BusinessId;
            location.WarehouseId = dto.WarehouseId;
            location.ParentLocationId = dto.ParentLocationId;
            location.Code = normalizedCode;
            location.DisplayName = dto.DisplayName.Trim();
            location.LocationType = dto.LocationType;
            location.Status = dto.Status;
            location.Barcode = InventoryManagementHandlerSupport.NormalizeOptional(dto.Barcode);
            location.SortOrder = dto.SortOrder;
            location.Description = InventoryManagementHandlerSupport.NormalizeOptional(dto.Description);
            location.MetadataJson = InventoryManagementHandlerSupport.NormalizeMetadataJson(dto.MetadataJson);

            await RecordWarehouseLocationEvidenceOrSaveAsync(location, "updated", AuditTrailAction.Updated, ct).ConfigureAwait(false);
        }

        private async Task EnsureWarehouseAndParentAsync(Guid businessId, Guid warehouseId, Guid? parentLocationId, Guid currentLocationId, CancellationToken ct)
        {
            var warehouseExists = await _db.Set<Warehouse>()
                .AsNoTracking()
                .AnyAsync(x => x.Id == warehouseId && x.BusinessId == businessId && !x.IsDeleted, ct)
                .ConfigureAwait(false);
            if (!warehouseExists)
            {
                throw new InvalidOperationException(_localizer["WarehouseNotFound"]);
            }

            if (!parentLocationId.HasValue)
            {
                return;
            }

            if (parentLocationId.Value == currentLocationId)
            {
                throw new InvalidOperationException(_localizer["WarehouseLocationParentInvalid"]);
            }

            var parent = await _db.Set<WarehouseLocation>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == parentLocationId.Value && !x.IsDeleted, ct)
                .ConfigureAwait(false);
            if (parent is null || parent.BusinessId != businessId || parent.WarehouseId != warehouseId)
            {
                throw new InvalidOperationException(_localizer["WarehouseLocationParentInvalid"]);
            }
        }

        private async Task EnsureWarehouseLocationCodeAvailableAsync(Guid businessId, Guid warehouseId, string code, Guid currentLocationId, CancellationToken ct)
        {
            var exists = await _db.Set<WarehouseLocation>()
                .AsNoTracking()
                .AnyAsync(x => x.BusinessId == businessId
                    && x.WarehouseId == warehouseId
                    && x.Code == code
                    && !x.IsDeleted
                    && x.Id != currentLocationId, ct)
                .ConfigureAwait(false);
            if (exists)
            {
                throw new InvalidOperationException(_localizer["WarehouseLocationCodeAlreadyExists"]);
            }
        }

        private async Task EnsureNoParentCycleAsync(Guid locationId, Guid? parentLocationId, CancellationToken ct)
        {
            var seen = new HashSet<Guid> { locationId };
            var cursor = parentLocationId;
            while (cursor.HasValue)
            {
                if (!seen.Add(cursor.Value))
                {
                    throw new InvalidOperationException(_localizer["WarehouseLocationParentInvalid"]);
                }

                cursor = await _db.Set<WarehouseLocation>()
                    .AsNoTracking()
                    .Where(x => x.Id == cursor.Value && !x.IsDeleted)
                    .Select(x => x.ParentLocationId)
                    .FirstOrDefaultAsync(ct)
                    .ConfigureAwait(false);
            }
        }

        private async Task RecordWarehouseLocationEvidenceOrSaveAsync(WarehouseLocation location, string action, AuditTrailAction auditAction, CancellationToken ct)
        {
            if (_events is null)
            {
                await InventoryManagementHandlerSupport.SaveChangesOrThrowConcurrencyAsync(_db, _localizer, ct).ConfigureAwait(false);
                return;
            }

            var now = _clock.UtcNow;
            var payload = $$"""
                {"warehouseLocationId":"{{location.Id}}","businessId":"{{location.BusinessId}}","warehouseId":"{{location.WarehouseId}}","parentLocationId":"{{location.ParentLocationId}}","code":"{{location.Code}}","type":"{{location.LocationType}}","status":"{{location.Status}}"}
                """;
            var eventResult = await _events.AddEventAsync(new AddBusinessEventCommand(location.BusinessId, "WarehouseLocation", location.Id, $"inventory.warehouse_location.{action}", $"inventory.warehouse_location.{action}:{location.Id}:{Convert.ToBase64String(location.RowVersion ?? Array.Empty<byte>())}", now, null, BusinessEventSource.User, BusinessEventSeverity.Info, FoundationVisibility.Internal, $"Warehouse location {action}", null, null, null, payload), ct).ConfigureAwait(false);
            if (!eventResult.Succeeded) throw new InvalidOperationException(eventResult.Error);
            var auditResult = await _events.AddAuditTrailAsync(new AddAuditTrailCommand(location.BusinessId, "WarehouseLocation", location.Id, auditAction, now, null, eventResult.Value, $"Warehouse location {action}", null, payload), ct).ConfigureAwait(false);
            if (!auditResult.Succeeded) throw new InvalidOperationException(auditResult.Error);
        }
    }

    public sealed class ArchiveWarehouseLocationHandler
    {
        private readonly IAppDbContext _db;
        private readonly IStringLocalizer<ValidationResource> _localizer;
        private readonly IClock _clock;
        private readonly BusinessEventService? _events;

        public ArchiveWarehouseLocationHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer, IClock clock, BusinessEventService? events = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _events = events;
        }

        public async Task<Result> HandleAsync(WarehouseLocationArchiveDto dto, CancellationToken ct = default)
        {
            if (dto.Id == Guid.Empty)
            {
                return Result.Fail(_localizer["InvalidDeleteRequest"]);
            }

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0)
            {
                return Result.Fail(_localizer["RowVersionRequired"]);
            }

            var location = await _db.Set<WarehouseLocation>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct)
                .ConfigureAwait(false);
            if (location is null)
            {
                return Result.Fail(_localizer["WarehouseLocationNotFound"]);
            }

            if (!(location.RowVersion ?? Array.Empty<byte>()).SequenceEqual(rowVersion))
            {
                return Result.Fail(_localizer["ItemConcurrencyConflict"]);
            }

            var hasChildren = await _db.Set<WarehouseLocation>()
                .AsNoTracking()
                .AnyAsync(x => x.ParentLocationId == location.Id && !x.IsDeleted, ct)
                .ConfigureAwait(false);
            if (hasChildren)
            {
                return Result.Fail(_localizer["WarehouseLocationArchiveHasChildren"]);
            }

            location.IsDeleted = true;
            location.ModifiedAtUtc = _clock.UtcNow;

            if (_events is null)
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                return Result.Ok();
            }

            var payload = $$"""
                {"warehouseLocationId":"{{location.Id}}","businessId":"{{location.BusinessId}}","warehouseId":"{{location.WarehouseId}}","code":"{{location.Code}}","type":"{{location.LocationType}}","status":"{{location.Status}}"}
                """;
            var eventResult = await _events.AddEventAsync(new AddBusinessEventCommand(location.BusinessId, "WarehouseLocation", location.Id, "inventory.warehouse_location.archived", $"inventory.warehouse_location.archived:{location.Id}", _clock.UtcNow, null, BusinessEventSource.User, BusinessEventSeverity.Info, FoundationVisibility.Internal, "Warehouse location archived", null, null, null, payload), ct).ConfigureAwait(false);
            if (!eventResult.Succeeded) return Result.Fail(eventResult.Error ?? "WarehouseLocationArchiveFailed");
            var auditResult = await _events.AddAuditTrailAsync(new AddAuditTrailCommand(location.BusinessId, "WarehouseLocation", location.Id, AuditTrailAction.Deleted, _clock.UtcNow, null, eventResult.Value, "Warehouse location archived", null, payload), ct).ConfigureAwait(false);
            if (!auditResult.Succeeded) return Result.Fail(auditResult.Error ?? "WarehouseLocationArchiveFailed");
            return Result.Ok();
        }
    }

    public sealed class CreateWarehouseLabelTemplateHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<WarehouseLabelTemplateCreateDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public CreateWarehouseLabelTemplateHandler(IAppDbContext db, IValidator<WarehouseLabelTemplateCreateDto> validator, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<Guid> HandleAsync(WarehouseLabelTemplateCreateDto dto, CancellationToken ct = default)
        {
            dto.Name = dto.Name?.Trim() ?? string.Empty;
            dto.TemplateKey = dto.TemplateKey?.Trim() ?? string.Empty;
            dto.ContentTemplate = dto.ContentTemplate?.Trim() ?? string.Empty;
            await _validator.ValidateAndThrowAsync(dto, ct);
            EnsureSafeWarehouseLabelTemplate(dto);
            var templateKey = InventoryManagementHandlerSupport.NormalizeRequiredCode(dto.TemplateKey);
            await EnsureTemplateKeyAvailableAsync(dto.BusinessId, templateKey, null, ct).ConfigureAwait(false);

            if (dto.IsDefault)
            {
                await ClearDefaultTemplatesAsync(dto.BusinessId, ct).ConfigureAwait(false);
            }

            var template = new WarehouseLabelTemplate
            {
                BusinessId = dto.BusinessId,
                Name = dto.Name.Trim(),
                TemplateKey = templateKey,
                Status = dto.Status,
                Format = dto.Format,
                IsDefault = dto.IsDefault,
                WidthMm = dto.WidthMm,
                HeightMm = dto.HeightMm,
                ContentTemplate = dto.ContentTemplate.Trim(),
                Description = InventoryManagementHandlerSupport.NormalizeOptional(dto.Description),
                MetadataJson = InventoryManagementHandlerSupport.NormalizeMetadataJson(dto.MetadataJson)
            };

            _db.Set<WarehouseLabelTemplate>().Add(template);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return template.Id;
        }

        private async Task EnsureTemplateKeyAvailableAsync(Guid businessId, string templateKey, Guid? currentTemplateId, CancellationToken ct)
        {
            var exists = await _db.Set<WarehouseLabelTemplate>()
                .AsNoTracking()
                .AnyAsync(x => x.BusinessId == businessId
                    && x.TemplateKey == templateKey
                    && !x.IsDeleted
                    && (!currentTemplateId.HasValue || x.Id != currentTemplateId.Value), ct)
                .ConfigureAwait(false);
            if (exists)
            {
                throw new InvalidOperationException(_localizer["WarehouseLabelTemplateKeyAlreadyExists"]);
            }
        }

        private async Task ClearDefaultTemplatesAsync(Guid businessId, CancellationToken ct)
        {
            var defaults = await _db.Set<WarehouseLabelTemplate>()
                .Where(x => x.BusinessId == businessId && x.IsDefault && !x.IsDeleted)
                .ToListAsync(ct)
                .ConfigureAwait(false);
            foreach (var item in defaults)
            {
                item.IsDefault = false;
            }
        }

        internal static void EnsureSafeWarehouseLabelTemplate(WarehouseLabelTemplateCreateDto dto)
        {
            if (FoundationInputNormalizer.LooksSensitive(dto.Name) ||
                FoundationInputNormalizer.LooksSensitive(dto.TemplateKey) ||
                FoundationInputNormalizer.LooksSensitive(dto.ContentTemplate) ||
                FoundationInputNormalizer.LooksSensitive(dto.Description) ||
                FoundationInputNormalizer.LooksSensitive(dto.MetadataJson))
            {
                throw new ArgumentException("WarehouseLabelTemplateSensitiveMetadataRejected");
            }
        }
    }

    public sealed class UpdateWarehouseLabelTemplateHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<WarehouseLabelTemplateEditDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateWarehouseLabelTemplateHandler(IAppDbContext db, IValidator<WarehouseLabelTemplateEditDto> validator, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task HandleAsync(WarehouseLabelTemplateEditDto dto, CancellationToken ct = default)
        {
            dto.Name = dto.Name?.Trim() ?? string.Empty;
            dto.TemplateKey = dto.TemplateKey?.Trim() ?? string.Empty;
            dto.ContentTemplate = dto.ContentTemplate?.Trim() ?? string.Empty;
            await _validator.ValidateAndThrowAsync(dto, ct);
            CreateWarehouseLabelTemplateHandler.EnsureSafeWarehouseLabelTemplate(dto);
            var template = await _db.Set<WarehouseLabelTemplate>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct)
                .ConfigureAwait(false);
            if (template is null)
            {
                throw new InvalidOperationException(_localizer["WarehouseLabelTemplateNotFound"]);
            }

            if (!(template.RowVersion ?? Array.Empty<byte>()).SequenceEqual(dto.RowVersion ?? Array.Empty<byte>()))
            {
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
            }

            var templateKey = InventoryManagementHandlerSupport.NormalizeRequiredCode(dto.TemplateKey);
            await EnsureTemplateKeyAvailableAsync(dto.BusinessId, templateKey, dto.Id, ct).ConfigureAwait(false);
            if (dto.IsDefault)
            {
                await ClearDefaultTemplatesAsync(dto.BusinessId, dto.Id, ct).ConfigureAwait(false);
            }

            template.BusinessId = dto.BusinessId;
            template.Name = dto.Name.Trim();
            template.TemplateKey = templateKey;
            template.Status = dto.Status;
            template.Format = dto.Format;
            template.IsDefault = dto.IsDefault;
            template.WidthMm = dto.WidthMm;
            template.HeightMm = dto.HeightMm;
            template.ContentTemplate = dto.ContentTemplate.Trim();
            template.Description = InventoryManagementHandlerSupport.NormalizeOptional(dto.Description);
            template.MetadataJson = InventoryManagementHandlerSupport.NormalizeMetadataJson(dto.MetadataJson);

            await InventoryManagementHandlerSupport.SaveChangesOrThrowConcurrencyAsync(_db, _localizer, ct).ConfigureAwait(false);
        }

        private async Task EnsureTemplateKeyAvailableAsync(Guid businessId, string templateKey, Guid currentTemplateId, CancellationToken ct)
        {
            var exists = await _db.Set<WarehouseLabelTemplate>()
                .AsNoTracking()
                .AnyAsync(x => x.BusinessId == businessId
                    && x.TemplateKey == templateKey
                    && !x.IsDeleted
                    && x.Id != currentTemplateId, ct)
                .ConfigureAwait(false);
            if (exists)
            {
                throw new InvalidOperationException(_localizer["WarehouseLabelTemplateKeyAlreadyExists"]);
            }
        }

        private async Task ClearDefaultTemplatesAsync(Guid businessId, Guid currentTemplateId, CancellationToken ct)
        {
            var defaults = await _db.Set<WarehouseLabelTemplate>()
                .Where(x => x.BusinessId == businessId && x.Id != currentTemplateId && x.IsDefault && !x.IsDeleted)
                .ToListAsync(ct)
                .ConfigureAwait(false);
            foreach (var item in defaults)
            {
                item.IsDefault = false;
            }
        }
    }

    public sealed class ArchiveWarehouseLabelTemplateHandler
    {
        private readonly IAppDbContext _db;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public ArchiveWarehouseLabelTemplateHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<Result> HandleAsync(WarehouseLabelTemplateArchiveDto dto, CancellationToken ct = default)
        {
            if (dto.Id == Guid.Empty || (dto.RowVersion ?? Array.Empty<byte>()).Length == 0)
            {
                return Result.Fail(_localizer["InvalidDeleteRequest"]);
            }

            var template = await _db.Set<WarehouseLabelTemplate>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct)
                .ConfigureAwait(false);
            if (template is null)
            {
                return Result.Fail(_localizer["WarehouseLabelTemplateNotFound"]);
            }

            if (!(template.RowVersion ?? Array.Empty<byte>()).SequenceEqual(dto.RowVersion))
            {
                return Result.Fail(_localizer["ItemConcurrencyConflict"]);
            }

            template.Status = WarehouseLabelTemplateStatus.Archived;
            template.IsDeleted = true;
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return Result.Ok();
        }
    }

    public sealed class CreateSupplierHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<SupplierCreateDto> _validator;

        public CreateSupplierHandler(IAppDbContext db, IValidator<SupplierCreateDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task<Guid> HandleAsync(SupplierCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var supplier = new Supplier
            {
                BusinessId = dto.BusinessId,
                Name = dto.Name.Trim(),
                Code = InventoryManagementHandlerSupport.NormalizeCode(dto.Code),
                Status = InventoryManagementHandlerSupport.ParseSupplierStatus(dto.Status),
                Email = dto.Email.Trim(),
                Phone = dto.Phone.Trim(),
                Address = InventoryManagementHandlerSupport.NormalizeOptional(dto.Address),
                Notes = InventoryManagementHandlerSupport.NormalizeOptional(dto.Notes),
                PreferredCurrency = InventoryManagementHandlerSupport.NormalizeCurrency(dto.PreferredCurrency),
                PaymentTermDays = dto.PaymentTermDays,
                LeadTimeDays = dto.LeadTimeDays,
                Website = InventoryManagementHandlerSupport.NormalizeOptional(dto.Website),
                TaxRegistrationNumber = InventoryManagementHandlerSupport.NormalizeOptional(dto.TaxRegistrationNumber),
                ExternalNotes = InventoryManagementHandlerSupport.NormalizeOptional(dto.ExternalNotes)
            };

            _db.Set<Supplier>().Add(supplier);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return supplier.Id;
        }

    }

    public sealed class UpdateSupplierHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<SupplierEditDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateSupplierHandler(
            IAppDbContext db,
            IValidator<SupplierEditDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task HandleAsync(SupplierEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var supplier = await _db.Set<Supplier>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (supplier is null)
            {
                throw new InvalidOperationException(_localizer["SupplierNotFound"]);
            }

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            var currentVersion = supplier.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0 || !currentVersion.SequenceEqual(rowVersion))
            {
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
            }

            supplier.BusinessId = dto.BusinessId;
            supplier.Name = dto.Name.Trim();
            supplier.Code = InventoryManagementHandlerSupport.NormalizeCode(dto.Code);
            supplier.Status = InventoryManagementHandlerSupport.ParseSupplierStatus(dto.Status);
            supplier.Email = dto.Email.Trim();
            supplier.Phone = dto.Phone.Trim();
            supplier.Address = InventoryManagementHandlerSupport.NormalizeOptional(dto.Address);
            supplier.Notes = InventoryManagementHandlerSupport.NormalizeOptional(dto.Notes);
            supplier.PreferredCurrency = InventoryManagementHandlerSupport.NormalizeCurrency(dto.PreferredCurrency);
            supplier.PaymentTermDays = dto.PaymentTermDays;
            supplier.LeadTimeDays = dto.LeadTimeDays;
            supplier.Website = InventoryManagementHandlerSupport.NormalizeOptional(dto.Website);
            supplier.TaxRegistrationNumber = InventoryManagementHandlerSupport.NormalizeOptional(dto.TaxRegistrationNumber);
            supplier.ExternalNotes = InventoryManagementHandlerSupport.NormalizeOptional(dto.ExternalNotes);

            await InventoryManagementHandlerSupport.SaveChangesOrThrowConcurrencyAsync(_db, _localizer, ct).ConfigureAwait(false);
        }

    }

    public sealed class CreateStockLevelHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<StockLevelCreateDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public CreateStockLevelHandler(
            IAppDbContext db,
            IValidator<StockLevelCreateDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<Guid> HandleAsync(StockLevelCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var exists = await _db.Set<StockLevel>()
                .AsNoTracking()
                .AnyAsync(x => x.WarehouseId == dto.WarehouseId && x.ProductVariantId == dto.ProductVariantId, ct)
                .ConfigureAwait(false);

            if (exists)
            {
                throw new InvalidOperationException(_localizer["StockLevelAlreadyExistsForWarehouseAndVariant"]);
            }

            var stockLevel = new StockLevel
            {
                WarehouseId = dto.WarehouseId,
                ProductVariantId = dto.ProductVariantId,
                AvailableQuantity = dto.AvailableQuantity,
                ReservedQuantity = dto.ReservedQuantity,
                ReorderPoint = dto.ReorderPoint,
                ReorderQuantity = dto.ReorderQuantity,
                InTransitQuantity = dto.InTransitQuantity
            };

            _db.Set<StockLevel>().Add(stockLevel);
            await Darwin.Application.Inventory.InventoryStockHelper.RefreshLegacyVariantStockAsync(_db, dto.ProductVariantId, _localizer, ct);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return stockLevel.Id;
        }
    }

    public sealed class UpdateStockLevelHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<StockLevelEditDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateStockLevelHandler(
            IAppDbContext db,
            IValidator<StockLevelEditDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task HandleAsync(StockLevelEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var stockLevel = await _db.Set<StockLevel>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (stockLevel is null)
            {
                throw new InvalidOperationException(_localizer["StockLevelNotFound"]);
            }

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            var currentVersion = stockLevel.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0 || !currentVersion.SequenceEqual(rowVersion))
            {
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
            }

            stockLevel.WarehouseId = dto.WarehouseId;
            stockLevel.ProductVariantId = dto.ProductVariantId;
            stockLevel.AvailableQuantity = dto.AvailableQuantity;
            stockLevel.ReservedQuantity = dto.ReservedQuantity;
            stockLevel.ReorderPoint = dto.ReorderPoint;
            stockLevel.ReorderQuantity = dto.ReorderQuantity;
            stockLevel.InTransitQuantity = dto.InTransitQuantity;

            await Darwin.Application.Inventory.InventoryStockHelper.RefreshLegacyVariantStockAsync(_db, dto.ProductVariantId, _localizer, ct);
            await InventoryManagementHandlerSupport.SaveChangesOrThrowConcurrencyAsync(_db, _localizer, ct).ConfigureAwait(false);
        }
    }

    public sealed class CreateStockTransferHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<StockTransferCreateDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public CreateStockTransferHandler(
            IAppDbContext db,
            IValidator<StockTransferCreateDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<Guid> HandleAsync(StockTransferCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);
            InventoryManagementHandlerSupport.NormalizeStockTransferIdentities(dto);
            if (InventoryManagementHandlerSupport.HasStockTransferIdentityEvidence(dto))
            {
                var businessId = await InventoryManagementHandlerSupport.EnsureStockTransferWarehousesAsync(_db, dto.FromWarehouseId, dto.ToWarehouseId, _localizer, ct).ConfigureAwait(false);
                await InventoryManagementHandlerSupport.PopulateStockTransferIdentitySnapshotsAsync(_db, dto, businessId, _localizer, ct).ConfigureAwait(false);
            }

            var transfer = new StockTransfer
            {
                FromWarehouseId = dto.FromWarehouseId,
                ToWarehouseId = dto.ToWarehouseId,
                Status = InventoryManagementHandlerSupport.ParseTransferStatus(dto.Status, _localizer),
                Lines = dto.Lines.Select(x => new StockTransferLine
                {
                    ProductVariantId = x.ProductVariantId,
                    Quantity = x.Quantity,
                    Identities = x.Identities
                        .Where(identity => !InventoryIdentityEvidenceSupport.IsBlank(identity))
                        .Select((identity, index) => InventoryManagementHandlerSupport.CreateStockTransferIdentity(identity, index + 1))
                        .ToList()
                }).ToList()
            };

            _db.Set<StockTransfer>().Add(transfer);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return transfer.Id;
        }
    }

    public sealed class UpdateStockTransferHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<StockTransferEditDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateStockTransferHandler(
            IAppDbContext db,
            IValidator<StockTransferEditDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task HandleAsync(StockTransferEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);
            InventoryManagementHandlerSupport.NormalizeStockTransferIdentities(dto);

            var transfer = await _db.Set<StockTransfer>()
                .Include(x => x.Lines)
                    .ThenInclude(x => x.Identities)
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (transfer is null)
            {
                throw new InvalidOperationException(_localizer["StockTransferNotFound"]);
            }

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            var currentVersion = transfer.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0 || !currentVersion.SequenceEqual(rowVersion))
            {
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
            }

            transfer.FromWarehouseId = dto.FromWarehouseId;
            transfer.ToWarehouseId = dto.ToWarehouseId;
            transfer.Status = InventoryManagementHandlerSupport.ParseTransferStatus(dto.Status, _localizer);

            _db.Set<StockTransferLineIdentity>().RemoveRange(transfer.Lines.SelectMany(x => x.Identities));
            _db.Set<StockTransferLine>().RemoveRange(transfer.Lines);
            transfer.Lines = dto.Lines.Select(x => new StockTransferLine
            {
                ProductVariantId = x.ProductVariantId,
                Quantity = x.Quantity,
                Identities = x.Identities
                    .Where(identity => !InventoryIdentityEvidenceSupport.IsBlank(identity))
                    .Select((identity, index) => InventoryManagementHandlerSupport.CreateStockTransferIdentity(identity, index + 1))
                    .ToList()
            }).ToList();

            await InventoryManagementHandlerSupport.SaveChangesOrThrowConcurrencyAsync(_db, _localizer, ct).ConfigureAwait(false);
        }
    }

    public sealed class UpdateStockTransferLifecycleHandler
    {
        public const string MarkInTransitAction = "MarkInTransit";
        public const string CompleteAction = "Complete";
        public const string CancelAction = "Cancel";

        private readonly IAppDbContext _db;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public UpdateStockTransferLifecycleHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<Result> HandleAsync(StockTransferLifecycleActionDto dto, CancellationToken ct = default)
        {
            if (dto.Id == Guid.Empty)
            {
                return Result.Fail(_localizer["InvalidDeleteRequest"]);
            }

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0)
            {
                return Result.Fail(_localizer["RowVersionRequired"]);
            }

            var transfer = await _db.Set<StockTransfer>()
                .Include(x => x.Lines)
                    .ThenInclude(x => x.Identities)
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (transfer is null)
            {
                return Result.Fail(_localizer["StockTransferNotFound"]);
            }

            var currentVersion = transfer.RowVersion ?? Array.Empty<byte>();
            if (!currentVersion.SequenceEqual(rowVersion))
            {
                return Result.Fail(_localizer["ItemConcurrencyConflict"]);
            }

            var variantIdsToRefresh = new HashSet<Guid>();
            var action = (dto.Action ?? string.Empty).Trim();

            if (string.Equals(action, MarkInTransitAction, StringComparison.OrdinalIgnoreCase))
            {
                var result = await MarkInTransitAsync(transfer, variantIdsToRefresh, ct).ConfigureAwait(false);
                if (!result.Succeeded)
                {
                    return result;
                }
            }
            else if (string.Equals(action, CompleteAction, StringComparison.OrdinalIgnoreCase))
            {
                var result = await CompleteAsync(transfer, variantIdsToRefresh, ct).ConfigureAwait(false);
                if (!result.Succeeded)
                {
                    return result;
                }
            }
            else if (string.Equals(action, CancelAction, StringComparison.OrdinalIgnoreCase))
            {
                var result = await CancelAsync(transfer, variantIdsToRefresh, ct).ConfigureAwait(false);
                if (!result.Succeeded)
                {
                    return result;
                }
            }
            else
            {
                return Result.Fail(_localizer["StockTransferLifecycleUnsupportedAction"]);
            }

            foreach (var variantId in variantIdsToRefresh)
            {
                await InventoryStockHelper.RefreshLegacyVariantStockAsync(_db, variantId, _localizer, ct).ConfigureAwait(false);
            }

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Fail(_localizer["ItemConcurrencyConflict"]);
            }

            return Result.Ok();
        }

        private async Task<Result> MarkInTransitAsync(StockTransfer transfer, HashSet<Guid> variantIdsToRefresh, CancellationToken ct)
        {
            if (transfer.Status != TransferStatus.Draft)
            {
                return Result.Fail(_localizer["StockTransferLifecycleUnsupportedAction"]);
            }

            var businessId = await InventoryManagementHandlerSupport.GetWarehouseBusinessIdAsync(_db, transfer.FromWarehouseId, _localizer, ct).ConfigureAwait(false);
            var identityValidation = await ValidateTransferIdentityEvidenceAsync(transfer, businessId, ct).ConfigureAwait(false);
            if (!identityValidation.Succeeded) return identityValidation;

            foreach (var line in transfer.Lines)
            {
                var source = await InventoryStockHelper.GetOrCreateStockLevelAsync(_db, transfer.FromWarehouseId, line.ProductVariantId, ct).ConfigureAwait(false);
                var alreadyDispatched = await InventoryMovementReferencePolicy.ExistsAsync(
                        _db,
                        transfer.Id,
                        InventoryMovementReferencePolicy.StockTransferDispatched,
                        transfer.FromWarehouseId,
                        line.ProductVariantId,
                        ct)
                    .ConfigureAwait(false);
                if (!alreadyDispatched)
                {
                    if (source.AvailableQuantity < line.Quantity)
                    {
                        return Result.Fail(_localizer["StockTransferLifecycleInsufficientSourceStock"]);
                    }

                    source.AvailableQuantity -= line.Quantity;
                    source.InTransitQuantity += line.Quantity;
                    AddInventoryTransaction(transfer.FromWarehouseId, line.ProductVariantId, -line.Quantity, InventoryMovementReferencePolicy.StockTransferDispatched, transfer.Id);
                }
                variantIdsToRefresh.Add(line.ProductVariantId);
            }

            transfer.Status = TransferStatus.InTransit;
            return Result.Ok();
        }

        private async Task<Result> CompleteAsync(StockTransfer transfer, HashSet<Guid> variantIdsToRefresh, CancellationToken ct)
        {
            if (transfer.Status != TransferStatus.InTransit)
            {
                return Result.Fail(_localizer["StockTransferLifecycleUnsupportedAction"]);
            }

            var businessId = await InventoryManagementHandlerSupport.GetWarehouseBusinessIdAsync(_db, transfer.FromWarehouseId, _localizer, ct).ConfigureAwait(false);
            var identityValidation = await ValidateTransferIdentityEvidenceAsync(transfer, businessId, ct).ConfigureAwait(false);
            if (!identityValidation.Succeeded) return identityValidation;

            foreach (var line in transfer.Lines)
            {
                var source = await InventoryStockHelper.GetOrCreateStockLevelAsync(_db, transfer.FromWarehouseId, line.ProductVariantId, ct).ConfigureAwait(false);
                var destination = await InventoryStockHelper.GetOrCreateStockLevelAsync(_db, transfer.ToWarehouseId, line.ProductVariantId, ct).ConfigureAwait(false);
                var alreadyReceived = await InventoryMovementReferencePolicy.ExistsAsync(
                        _db,
                        transfer.Id,
                        InventoryMovementReferencePolicy.StockTransferReceived,
                        transfer.ToWarehouseId,
                        line.ProductVariantId,
                        ct)
                    .ConfigureAwait(false);
                if (!alreadyReceived)
                {
                    if (source.InTransitQuantity < line.Quantity)
                    {
                        return Result.Fail(_localizer["StockTransferLifecycleInsufficientInTransitStock"]);
                    }

                    source.InTransitQuantity -= line.Quantity;
                    destination.AvailableQuantity += line.Quantity;
                    AddInventoryTransaction(transfer.ToWarehouseId, line.ProductVariantId, line.Quantity, InventoryMovementReferencePolicy.StockTransferReceived, transfer.Id);
                }
                variantIdsToRefresh.Add(line.ProductVariantId);
            }

            transfer.Status = TransferStatus.Completed;
            return Result.Ok();
        }

        private async Task<Result> CancelAsync(StockTransfer transfer, HashSet<Guid> variantIdsToRefresh, CancellationToken ct)
        {
            if (transfer.Status == TransferStatus.Draft)
            {
                transfer.Status = TransferStatus.Cancelled;
                return Result.Ok();
            }

            if (transfer.Status != TransferStatus.InTransit)
            {
                return Result.Fail(_localizer["StockTransferLifecycleUnsupportedAction"]);
            }

            foreach (var line in transfer.Lines)
            {
                var source = await InventoryStockHelper.GetOrCreateStockLevelAsync(_db, transfer.FromWarehouseId, line.ProductVariantId, ct).ConfigureAwait(false);
                var alreadyCancelled = await InventoryMovementReferencePolicy.ExistsAsync(
                        _db,
                        transfer.Id,
                        InventoryMovementReferencePolicy.StockTransferCancelled,
                        transfer.FromWarehouseId,
                        line.ProductVariantId,
                        ct)
                    .ConfigureAwait(false);
                if (!alreadyCancelled)
                {
                    if (source.InTransitQuantity < line.Quantity)
                    {
                        return Result.Fail(_localizer["StockTransferLifecycleInsufficientInTransitStock"]);
                    }

                    source.InTransitQuantity -= line.Quantity;
                    source.AvailableQuantity += line.Quantity;
                    AddInventoryTransaction(transfer.FromWarehouseId, line.ProductVariantId, line.Quantity, InventoryMovementReferencePolicy.StockTransferCancelled, transfer.Id);
                }
                variantIdsToRefresh.Add(line.ProductVariantId);
            }

            transfer.Status = TransferStatus.Cancelled;
            return Result.Ok();
        }

        private void AddInventoryTransaction(Guid warehouseId, Guid productVariantId, int quantityDelta, string reason, Guid referenceId)
        {
            InventoryMovementReferencePolicy.EnsureReferencePolicy(reason, referenceId);
            InventoryMovementReferencePolicy.AddLedgerRow(_db, warehouseId, productVariantId, quantityDelta, reason, referenceId);
        }

        private Task<Result> ValidateTransferIdentityEvidenceAsync(StockTransfer transfer, Guid businessId, CancellationToken ct)
            => InventoryIdentityEvidenceSupport.ValidateRequiredEvidenceAsync(
                _db,
                businessId,
                transfer.Lines.Where(x => !x.IsDeleted),
                line => line.Quantity > 0,
                line => line.ProductVariantId,
                line => line.Quantity,
                line => line.Identities.Where(identity => !identity.IsDeleted),
                identity => identity.InventoryLotId,
                identity => identity.InventorySerialUnitId,
                identity => identity.HandlingUnitId,
                identity => identity.Quantity,
                identity => identity.ExpiryDateUtc,
                identity => identity.SupplierLotCodeSnapshot,
                _localizer,
                "StockTransferIdentityRequired",
                "StockTransferInvalidIdentityQuantity",
                "StockTransferLotIdentityRequired",
                "StockTransferSerialIdentityRequired",
                "StockTransferExpiryIdentityRequired",
                "StockTransferSupplierLotIdentityRequired",
                "StockTransferHandlingUnitIdentityRequired",
                ct);
    }

    public sealed class CreatePurchaseOrderHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<PurchaseOrderCreateDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;
        private readonly NumberSequenceService _numberSequenceService;
        private readonly IClock _clock;
        private readonly BusinessEventService? _businessEventService;

        public CreatePurchaseOrderHandler(
            IAppDbContext db,
            IValidator<PurchaseOrderCreateDto> validator,
            IStringLocalizer<ValidationResource> localizer,
            NumberSequenceService numberSequenceService,
            IClock clock,
            BusinessEventService? businessEventService = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            _numberSequenceService = numberSequenceService ?? throw new ArgumentNullException(nameof(numberSequenceService));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _businessEventService = businessEventService;
        }

        public CreatePurchaseOrderHandler(
            IAppDbContext db,
            IValidator<PurchaseOrderCreateDto> validator,
            IStringLocalizer<ValidationResource> localizer)
            : this(db, validator, localizer, new NumberSequenceService(db, InventoryManagementHandlerSupport.DefaultClock), InventoryManagementHandlerSupport.DefaultClock)
        {
        }

        public async Task<Guid> HandleAsync(PurchaseOrderCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            await InventoryManagementHandlerSupport.EnsureSupplierReadyForPurchaseOrderAsync(_db, dto.SupplierId, dto.BusinessId, _localizer, ct)
                .ConfigureAwait(false);

            var orderNumber = await InventoryManagementHandlerSupport.ResolvePurchaseOrderNumberAsync(
                    _numberSequenceService,
                    dto.BusinessId,
                    dto.OrderNumber,
                    _localizer,
                    ct)
                .ConfigureAwait(false);

            var order = new PurchaseOrder
            {
                SupplierId = dto.SupplierId,
                BusinessId = dto.BusinessId,
                OrderNumber = orderNumber,
                OrderedAtUtc = dto.OrderedAtUtc == default ? _clock.UtcNow : dto.OrderedAtUtc,
                Status = InventoryManagementHandlerSupport.ParsePurchaseOrderStatus(dto.Status, _localizer),
                Currency = InventoryManagementHandlerSupport.NormalizeCurrency(dto.Currency) ?? "EUR",
                ExpectedDeliveryDateUtc = dto.ExpectedDeliveryDateUtc,
                InternalNotes = InventoryManagementHandlerSupport.NormalizeOptional(dto.InternalNotes),
                Lines = dto.Lines.Select(x => new PurchaseOrderLine
                {
                    ProductVariantId = x.ProductVariantId,
                    SupplierSku = InventoryManagementHandlerSupport.NormalizeOptional(x.SupplierSku),
                    Description = InventoryManagementHandlerSupport.NormalizeOptional(x.Description),
                    Quantity = x.Quantity,
                    ReceivedQuantity = 0,
                    CancelledQuantity = 0,
                    UnitCostMinor = x.UnitCostMinor,
                    TotalCostMinor = x.TotalCostMinor
                }).ToList()
            };

            _db.Set<PurchaseOrder>().Add(order);
            await InventoryManagementHandlerSupport.RecordPurchaseOrderEvidenceOrSaveAsync(
                    _db,
                    _businessEventService,
                    order,
                    "purchasing.purchase_order.created",
                    $"purchasing.purchase_order.created:{order.Id}",
                    AuditTrailAction.Created,
                    "Purchase order created",
                    ct)
                .ConfigureAwait(false);
            return order.Id;
        }

        private async Task PopulateStockTransferIdentitySnapshotsAsync(StockTransferCreateDto dto, Guid businessId, CancellationToken ct)
        {
            foreach (var line in dto.Lines)
            {
                foreach (var identity in line.Identities.Where(identity => !InventoryIdentityEvidenceSupport.IsBlank(identity)))
                {
                    var snapshots = await InventoryIdentityEvidenceSupport.PopulateSnapshotsAsync(
                            _db,
                            businessId,
                            line.ProductVariantId,
                            identity.InventoryLotId,
                            identity.InventorySerialUnitId,
                            identity.HandlingUnitId,
                            _localizer,
                            ct)
                        .ConfigureAwait(false);
                    if (!snapshots.Succeeded) throw new InvalidOperationException(snapshots.Error);
                    var value = snapshots.Value ?? throw new InvalidOperationException("InventoryIdentitySnapshotsMissing");
                    identity.LotCodeSnapshot = value.LotCodeSnapshot ?? identity.LotCodeSnapshot;
                    identity.SupplierLotCodeSnapshot = value.SupplierLotCodeSnapshot ?? identity.SupplierLotCodeSnapshot;
                    identity.ExpiryDateUtc = value.ExpiryDateUtc ?? identity.ExpiryDateUtc;
                    identity.SerialNumberSnapshot = value.SerialNumberSnapshot ?? identity.SerialNumberSnapshot;
                    identity.HandlingUnitCodeSnapshot = value.HandlingUnitCodeSnapshot ?? identity.HandlingUnitCodeSnapshot;
                }
            }
        }

        private static void NormalizeStockTransferIdentities(StockTransferCreateDto dto)
        {
            dto.Lines ??= new List<StockTransferLineDto>();
            foreach (var line in dto.Lines)
            {
                line.Identities ??= new List<InventoryIdentityEvidenceDto>();
                InventoryIdentityEvidenceSupport.Normalize(line.Identities);
            }
        }

        private static StockTransferLineIdentity CreateStockTransferIdentity(InventoryIdentityEvidenceDto dto, int sortOrder)
            => new()
            {
                InventoryLotId = dto.InventoryLotId,
                InventorySerialUnitId = dto.InventorySerialUnitId,
                HandlingUnitId = dto.HandlingUnitId,
                Quantity = dto.Quantity,
                LotCodeSnapshot = InventoryManagementHandlerSupport.NormalizeOptional(dto.LotCodeSnapshot),
                SupplierLotCodeSnapshot = InventoryManagementHandlerSupport.NormalizeOptional(dto.SupplierLotCodeSnapshot),
                ExpiryDateUtc = dto.ExpiryDateUtc,
                SerialNumberSnapshot = InventoryManagementHandlerSupport.NormalizeOptional(dto.SerialNumberSnapshot),
                HandlingUnitCodeSnapshot = InventoryManagementHandlerSupport.NormalizeOptional(dto.HandlingUnitCodeSnapshot),
                SortOrder = dto.SortOrder <= 0 ? sortOrder : dto.SortOrder,
                MetadataJson = InventoryManagementHandlerSupport.NormalizeMetadataJson(dto.MetadataJson)
            };
    }

    public sealed class UpdatePurchaseOrderHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<PurchaseOrderEditDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;
        private readonly NumberSequenceService _numberSequenceService;
        private readonly BusinessEventService? _businessEventService;

        public UpdatePurchaseOrderHandler(
            IAppDbContext db,
            IValidator<PurchaseOrderEditDto> validator,
            IStringLocalizer<ValidationResource> localizer,
            NumberSequenceService numberSequenceService,
            BusinessEventService? businessEventService = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            _numberSequenceService = numberSequenceService ?? throw new ArgumentNullException(nameof(numberSequenceService));
            _businessEventService = businessEventService;
        }

        public UpdatePurchaseOrderHandler(
            IAppDbContext db,
            IValidator<PurchaseOrderEditDto> validator,
            IStringLocalizer<ValidationResource> localizer)
            : this(db, validator, localizer, new NumberSequenceService(db, InventoryManagementHandlerSupport.DefaultClock))
        {
        }

        public async Task HandleAsync(PurchaseOrderEditDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var order = await _db.Set<PurchaseOrder>()
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (order is null)
            {
                throw new InvalidOperationException(_localizer["PurchaseOrderNotFound"]);
            }

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            var currentVersion = order.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0 || !currentVersion.SequenceEqual(rowVersion))
            {
                throw new DbUpdateConcurrencyException(_localizer["ConcurrencyConflictDetected"]);
            }

            if (order.Status != PurchaseOrderStatus.Draft)
            {
                throw new InvalidOperationException(_localizer["PurchaseOrderCannotEditAfterDraft"]);
            }

            await InventoryManagementHandlerSupport.EnsureSupplierReadyForPurchaseOrderAsync(_db, dto.SupplierId, dto.BusinessId, _localizer, ct)
                .ConfigureAwait(false);

            order.SupplierId = dto.SupplierId;
            order.BusinessId = dto.BusinessId;
            order.OrderNumber = await InventoryManagementHandlerSupport.ResolvePurchaseOrderNumberAsync(
                    _numberSequenceService,
                    dto.BusinessId,
                    dto.OrderNumber,
                    _localizer,
                    ct)
                .ConfigureAwait(false);
            order.OrderedAtUtc = dto.OrderedAtUtc;
            order.Status = InventoryManagementHandlerSupport.ParsePurchaseOrderStatus(dto.Status, _localizer);
            order.Currency = InventoryManagementHandlerSupport.NormalizeCurrency(dto.Currency) ?? "EUR";
            order.ExpectedDeliveryDateUtc = dto.ExpectedDeliveryDateUtc;
            order.InternalNotes = InventoryManagementHandlerSupport.NormalizeOptional(dto.InternalNotes);

            _db.Set<PurchaseOrderLine>().RemoveRange(order.Lines);
            order.Lines = dto.Lines.Select(x => new PurchaseOrderLine
            {
                ProductVariantId = x.ProductVariantId,
                SupplierSku = InventoryManagementHandlerSupport.NormalizeOptional(x.SupplierSku),
                Description = InventoryManagementHandlerSupport.NormalizeOptional(x.Description),
                Quantity = x.Quantity,
                ReceivedQuantity = 0,
                CancelledQuantity = 0,
                UnitCostMinor = x.UnitCostMinor,
                TotalCostMinor = x.TotalCostMinor
            }).ToList();

            await InventoryManagementHandlerSupport.RecordPurchaseOrderEvidenceOrSaveAsync(
                    _db,
                    _businessEventService,
                    order,
                    "purchasing.purchase_order.updated",
                    $"purchasing.purchase_order.updated:{order.Id}:{Convert.ToBase64String(rowVersion)}",
                    AuditTrailAction.Updated,
                    "Purchase order updated",
                    ct)
                .ConfigureAwait(false);
        }
    }

    public sealed class UpdatePurchaseOrderLifecycleHandler
    {
        public const string IssueAction = "Issue";
        public const string ReceiveAction = "Receive";
        public const string CancelAction = "Cancel";

        private readonly IAppDbContext _db;
        private readonly IStringLocalizer<ValidationResource> _localizer;
        private readonly IClock _clock;
        private readonly BusinessEventService? _businessEventService;

        public UpdatePurchaseOrderLifecycleHandler(
            IAppDbContext db,
            IStringLocalizer<ValidationResource> localizer,
            IClock clock,
            BusinessEventService? businessEventService = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _businessEventService = businessEventService;
        }

        public UpdatePurchaseOrderLifecycleHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
            : this(db, localizer, InventoryManagementHandlerSupport.DefaultClock)
        {
        }

        public async Task<Result> HandleAsync(PurchaseOrderLifecycleActionDto dto, CancellationToken ct = default)
        {
            if (dto.Id == Guid.Empty)
            {
                return Result.Fail(_localizer["InvalidDeleteRequest"]);
            }

            var rowVersion = dto.RowVersion ?? Array.Empty<byte>();
            if (rowVersion.Length == 0)
            {
                return Result.Fail(_localizer["RowVersionRequired"]);
            }

            var order = await _db.Set<PurchaseOrder>()
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                .ConfigureAwait(false);

            if (order is null)
            {
                return Result.Fail(_localizer["PurchaseOrderNotFound"]);
            }

            var currentVersion = order.RowVersion ?? Array.Empty<byte>();
            if (!currentVersion.SequenceEqual(rowVersion))
            {
                return Result.Fail(_localizer["ItemConcurrencyConflict"]);
            }

            var action = (dto.Action ?? string.Empty).Trim();
            if (string.Equals(action, IssueAction, StringComparison.OrdinalIgnoreCase))
            {
                if (order.Status != PurchaseOrderStatus.Draft)
                {
                    return Result.Fail(_localizer["PurchaseOrderLifecycleUnsupportedAction"]);
                }

                order.Status = PurchaseOrderStatus.Issued;
                order.IssuedAtUtc ??= _clock.UtcNow;
            }
            else if (string.Equals(action, ReceiveAction, StringComparison.OrdinalIgnoreCase))
            {
                var result = await ReceiveAsync(order, ct).ConfigureAwait(false);
                if (!result.Succeeded)
                {
                    return result;
                }
            }
            else if (string.Equals(action, CancelAction, StringComparison.OrdinalIgnoreCase))
            {
                if (order.Status is not (PurchaseOrderStatus.Draft or PurchaseOrderStatus.Issued))
                {
                    return Result.Fail(_localizer["PurchaseOrderLifecycleUnsupportedAction"]);
                }

                order.Status = PurchaseOrderStatus.Cancelled;
                order.CancelledAtUtc ??= _clock.UtcNow;
                foreach (var line in order.Lines.Where(x => !x.IsDeleted))
                {
                    line.CancelledQuantity = Math.Max(line.CancelledQuantity, line.Quantity - line.ReceivedQuantity);
                }
            }
            else
            {
                return Result.Fail(_localizer["PurchaseOrderLifecycleUnsupportedAction"]);
            }

            try
            {
                await InventoryManagementHandlerSupport.RecordPurchaseOrderEvidenceOrSaveAsync(
                        _db,
                        _businessEventService,
                        order,
                        $"purchasing.purchase_order.{order.Status.ToString().ToLowerInvariant()}",
                        $"purchasing.purchase_order.{order.Status.ToString().ToLowerInvariant()}:{order.Id}",
                        AuditTrailAction.StatusChanged,
                        $"Purchase order {order.Status}",
                        ct)
                    .ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Fail(_localizer["ItemConcurrencyConflict"]);
            }

            return Result.Ok();
        }

        private async Task<Result> ReceiveAsync(PurchaseOrder order, CancellationToken ct)
        {
            if (order.Status != PurchaseOrderStatus.Issued)
            {
                return Result.Fail(_localizer["PurchaseOrderLifecycleUnsupportedAction"]);
            }

            var warehouseId = await _db.Set<Warehouse>()
                .AsNoTracking()
                .Where(x => x.BusinessId == order.BusinessId)
                .OrderByDescending(x => x.IsDefault)
                .ThenBy(x => x.Name)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            if (warehouseId == Guid.Empty)
            {
                return Result.Fail(_localizer["NoWarehouseIsConfigured"]);
            }

            var receipt = new GoodsReceipt
            {
                BusinessId = order.BusinessId,
                SupplierId = order.SupplierId,
                PurchaseOrderId = order.Id,
                WarehouseId = warehouseId,
                Status = GoodsReceiptStatus.Posted,
                ReceivedAtUtc = _clock.UtcNow,
                InspectedAtUtc = _clock.UtcNow,
                PostedAtUtc = _clock.UtcNow,
                Lines = order.Lines
                    .Where(x => !x.IsDeleted && x.Quantity > x.ReceivedQuantity + x.CancelledQuantity)
                    .OrderBy(x => x.CreatedAtUtc)
                    .Select((line, index) =>
                    {
                        var remaining = line.Quantity - line.ReceivedQuantity - line.CancelledQuantity;
                        return new GoodsReceiptLine
                        {
                            PurchaseOrderLineId = line.Id,
                            ProductVariantId = line.ProductVariantId,
                            SupplierSku = InventoryManagementHandlerSupport.NormalizeOptional(line.SupplierSku),
                            Description = InventoryManagementHandlerSupport.NormalizeOptional(line.Description),
                            OrderedQuantity = line.Quantity,
                            PreviouslyReceivedQuantity = line.ReceivedQuantity,
                            ReceivedQuantity = remaining,
                            AcceptedQuantity = remaining,
                            RejectedQuantity = 0,
                            DamagedQuantity = 0,
                            UnitCostMinor = line.UnitCostMinor,
                            TotalCostMinor = line.TotalCostMinor,
                            SortOrder = index
                        };
                    })
                    .ToList()
            };

            var receiptNumber = await new NumberSequenceService(_db, _clock)
                .ReserveNextAsync(new NumberSequenceRequest(order.BusinessId, NumberSequenceDocumentType.GoodsReceipt, NumberSequenceService.GlobalScopeKey), ct)
                .ConfigureAwait(false);
            if (receiptNumber.Succeeded && !string.IsNullOrWhiteSpace(receiptNumber.Value))
            {
                receipt.GoodsReceiptNumber = receiptNumber.Value;
            }

            if (receipt.Lines.Count == 0)
            {
                return Result.Fail(_localizer["PurchaseOrderLifecycleUnsupportedAction"]);
            }

            _db.Set<GoodsReceipt>().Add(receipt);
            var variantIdsToRefresh = new HashSet<Guid>();
            foreach (var line in order.Lines.Where(x => !x.IsDeleted && x.Quantity > x.ReceivedQuantity + x.CancelledQuantity))
            {
                var remaining = line.Quantity - line.ReceivedQuantity - line.CancelledQuantity;
                var stockLevel = await InventoryStockHelper.GetOrCreateStockLevelAsync(_db, warehouseId, line.ProductVariantId, ct).ConfigureAwait(false);
                stockLevel.AvailableQuantity += remaining;
                line.ReceivedQuantity += remaining;
                InventoryMovementReferencePolicy.AddLedgerRow(
                    _db,
                    warehouseId,
                    line.ProductVariantId,
                    remaining,
                    InventoryMovementReferencePolicy.GoodsReceiptPosted,
                    receipt.Id);
                variantIdsToRefresh.Add(line.ProductVariantId);
            }

            order.Status = PurchaseOrderStatus.Received;
            order.ReceivedAtUtc ??= _clock.UtcNow;

            foreach (var variantId in variantIdsToRefresh)
            {
                await InventoryStockHelper.RefreshLegacyVariantStockAsync(_db, variantId, _localizer, ct).ConfigureAwait(false);
            }

            return Result.Ok();
        }
    }

    internal static class InventoryManagementHandlerSupport
    {
        public static IClock DefaultClock { get; } = new InventorySystemClock();

        public static TransferStatus ParseTransferStatus(string value, IStringLocalizer<ValidationResource> localizer)
        {
            if (!Enum.TryParse<TransferStatus>(value, true, out var status))
            {
                throw new ValidationException(localizer["InvalidStockTransferStatus"]);
            }

            return status;
        }

        public static PurchaseOrderStatus ParsePurchaseOrderStatus(string value, IStringLocalizer<ValidationResource> localizer)
        {
            if (!Enum.TryParse<PurchaseOrderStatus>(value, true, out var status))
            {
                throw new ValidationException(localizer["InvalidPurchaseOrderStatus"]);
            }

            return status;
        }

        public static SupplierStatus ParseSupplierStatus(string value)
        {
            if (!Enum.TryParse<SupplierStatus>(value, true, out var status))
            {
                throw new ValidationException("Invalid supplier status.");
            }

            return status;
        }

        public static string? NormalizeOptional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        public static string? NormalizeCode(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

        public static string NormalizeRequiredCode(string value) => value.Trim().ToUpperInvariant();

        public static string? NormalizeCurrency(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

        public static string? NormalizeMetadataJson(string? value)
        {
            var normalized = NormalizeOptional(value);
            return string.Equals(normalized, "{}", StringComparison.Ordinal) ? null : normalized;
        }

        public static async Task<Guid> EnsureStockTransferWarehousesAsync(
            IAppDbContext db,
            Guid fromWarehouseId,
            Guid toWarehouseId,
            IStringLocalizer<ValidationResource> localizer,
            CancellationToken ct)
        {
            var warehouses = await db.Set<Warehouse>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted && (x.Id == fromWarehouseId || x.Id == toWarehouseId))
                .Select(x => new { x.Id, x.BusinessId })
                .ToListAsync(ct)
                .ConfigureAwait(false);
            var from = warehouses.FirstOrDefault(x => x.Id == fromWarehouseId);
            var to = warehouses.FirstOrDefault(x => x.Id == toWarehouseId);
            if (from is null || to is null || from.BusinessId != to.BusinessId)
            {
                throw new InvalidOperationException(localizer["WarehouseNotFound"]);
            }

            return from.BusinessId;
        }

        public static async Task<Guid> GetWarehouseBusinessIdAsync(IAppDbContext db, Guid warehouseId, IStringLocalizer<ValidationResource> localizer, CancellationToken ct)
        {
            var businessId = await db.Set<Warehouse>()
                .AsNoTracking()
                .Where(x => x.Id == warehouseId && !x.IsDeleted)
                .Select(x => (Guid?)x.BusinessId)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);
            if (!businessId.HasValue) throw new InvalidOperationException(localizer["WarehouseNotFound"]);
            return businessId.Value;
        }

        public static void NormalizeStockTransferIdentities(StockTransferCreateDto dto)
        {
            dto.Lines ??= new List<StockTransferLineDto>();
            foreach (var line in dto.Lines)
            {
                line.Identities ??= new List<InventoryIdentityEvidenceDto>();
                InventoryIdentityEvidenceSupport.Normalize(line.Identities);
            }
        }

        public static bool HasStockTransferIdentityEvidence(StockTransferCreateDto dto)
            => dto.Lines?.Any(line => line.Identities?.Any(identity => !InventoryIdentityEvidenceSupport.IsBlank(identity)) == true) == true;

        public static async Task PopulateStockTransferIdentitySnapshotsAsync(
            IAppDbContext db,
            StockTransferCreateDto dto,
            Guid businessId,
            IStringLocalizer<ValidationResource> localizer,
            CancellationToken ct)
        {
            foreach (var line in dto.Lines)
            {
                foreach (var identity in line.Identities.Where(identity => !InventoryIdentityEvidenceSupport.IsBlank(identity)))
                {
                    var snapshots = await InventoryIdentityEvidenceSupport.PopulateSnapshotsAsync(
                            db,
                            businessId,
                            line.ProductVariantId,
                            identity.InventoryLotId,
                            identity.InventorySerialUnitId,
                            identity.HandlingUnitId,
                            localizer,
                            ct)
                        .ConfigureAwait(false);
                    if (!snapshots.Succeeded) throw new InvalidOperationException(snapshots.Error);
                    var value = snapshots.Value ?? throw new InvalidOperationException("InventoryIdentitySnapshotsMissing");
                    identity.LotCodeSnapshot = value.LotCodeSnapshot ?? identity.LotCodeSnapshot;
                    identity.SupplierLotCodeSnapshot = value.SupplierLotCodeSnapshot ?? identity.SupplierLotCodeSnapshot;
                    identity.ExpiryDateUtc = value.ExpiryDateUtc ?? identity.ExpiryDateUtc;
                    identity.SerialNumberSnapshot = value.SerialNumberSnapshot ?? identity.SerialNumberSnapshot;
                    identity.HandlingUnitCodeSnapshot = value.HandlingUnitCodeSnapshot ?? identity.HandlingUnitCodeSnapshot;
                }
            }
        }

        public static StockTransferLineIdentity CreateStockTransferIdentity(InventoryIdentityEvidenceDto dto, int sortOrder)
            => new()
            {
                InventoryLotId = dto.InventoryLotId,
                InventorySerialUnitId = dto.InventorySerialUnitId,
                HandlingUnitId = dto.HandlingUnitId,
                Quantity = dto.Quantity,
                LotCodeSnapshot = NormalizeOptional(dto.LotCodeSnapshot),
                SupplierLotCodeSnapshot = NormalizeOptional(dto.SupplierLotCodeSnapshot),
                ExpiryDateUtc = dto.ExpiryDateUtc,
                SerialNumberSnapshot = NormalizeOptional(dto.SerialNumberSnapshot),
                HandlingUnitCodeSnapshot = NormalizeOptional(dto.HandlingUnitCodeSnapshot),
                SortOrder = dto.SortOrder <= 0 ? sortOrder : dto.SortOrder,
                MetadataJson = NormalizeMetadataJson(dto.MetadataJson)
            };

        public static async Task EnsureSupplierReadyForPurchaseOrderAsync(
            IAppDbContext db,
            Guid supplierId,
            Guid businessId,
            IStringLocalizer<ValidationResource> localizer,
            CancellationToken ct)
        {
            var supplier = await db.Set<Supplier>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == supplierId && !x.IsDeleted, ct)
                .ConfigureAwait(false);

            if (supplier is null)
            {
                throw new InvalidOperationException(localizer["SupplierNotFound"]);
            }

            if (supplier.BusinessId != businessId)
            {
                throw new InvalidOperationException(localizer["SupplierBusinessMismatch"]);
            }

            if (supplier.Status != SupplierStatus.Active)
            {
                throw new InvalidOperationException(localizer["SupplierNotActive"]);
            }
        }

        public static async Task<string> ResolvePurchaseOrderNumberAsync(
            NumberSequenceService numberSequenceService,
            Guid businessId,
            string? requestedNumber,
            IStringLocalizer<ValidationResource> localizer,
            CancellationToken ct)
        {
            var normalized = NormalizeOptional(requestedNumber);
            if (normalized is not null)
            {
                return normalized;
            }

            var result = await numberSequenceService.ReserveNextAsync(
                    new NumberSequenceRequest(businessId, NumberSequenceDocumentType.PurchaseOrder, NumberSequenceService.GlobalScopeKey),
                    ct)
                .ConfigureAwait(false);

            if (!result.Succeeded || string.IsNullOrWhiteSpace(result.Value))
            {
                throw new InvalidOperationException(localizer["PurchaseOrderNumberRequired"]);
            }

            return result.Value;
        }

        public static async Task RecordPurchaseOrderEvidenceOrSaveAsync(
            IAppDbContext db,
            BusinessEventService? businessEventService,
            PurchaseOrder order,
            string eventType,
            string eventKey,
            AuditTrailAction action,
            string title,
            CancellationToken ct)
        {
            if (businessEventService is null)
            {
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
                return;
            }

            var now = DateTime.UtcNow;
            var payload = $$"""
                {"purchaseOrderId":"{{order.Id}}","businessId":"{{order.BusinessId}}","supplierId":"{{order.SupplierId}}","status":"{{order.Status}}","currency":"{{order.Currency}}","lineCount":{{order.Lines.Count(x => !x.IsDeleted)}}}
                """;

            var eventResult = await businessEventService.AddEventAsync(
                    new AddBusinessEventCommand(
                        order.BusinessId,
                        "PurchaseOrder",
                        order.Id,
                        eventType,
                        eventKey,
                        now,
                        null,
                        BusinessEventSource.User,
                        BusinessEventSeverity.Info,
                        FoundationVisibility.Internal,
                        title,
                        null,
                        null,
                        null,
                        payload),
                    ct)
                .ConfigureAwait(false);

            if (!eventResult.Succeeded)
            {
                throw new InvalidOperationException(eventResult.Error);
            }

            var auditResult = await businessEventService.AddAuditTrailAsync(
                    new AddAuditTrailCommand(
                        order.BusinessId,
                        "PurchaseOrder",
                        order.Id,
                        action,
                        now,
                        null,
                        eventResult.Value,
                        title,
                        null,
                        payload),
                    ct)
                .ConfigureAwait(false);

            if (!auditResult.Succeeded)
            {
                throw new InvalidOperationException(auditResult.Error);
            }
        }

        public static async Task SaveChangesOrThrowConcurrencyAsync(
            IAppDbContext db,
            IStringLocalizer<ValidationResource> localizer,
            CancellationToken ct)
        {
            try
            {
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new DbUpdateConcurrencyException(localizer["ConcurrencyConflictDetected"]);
            }
        }

        private sealed class InventorySystemClock : IClock
        {
            public DateTime UtcNow => DateTime.UtcNow;
        }
    }
}
