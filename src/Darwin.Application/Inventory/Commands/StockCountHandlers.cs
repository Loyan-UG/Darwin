using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Foundation;
using Darwin.Application.Inventory.DTOs;
using Darwin.Application.Inventory.Validators;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Inventory;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Inventory.Commands;

public sealed class CreateStockCountHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<StockCountCreateDto> _validator;
    private readonly IStringLocalizer<ValidationResource> _localizer;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public CreateStockCountHandler(
        IAppDbContext db,
        IValidator<StockCountCreateDto> validator,
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

    public async Task<Guid> HandleAsync(StockCountCreateDto dto, CancellationToken ct = default)
    {
        StockCountHandlerSupport.Normalize(dto);
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        StockCountHandlerSupport.EnsureSafe(dto);
        await StockCountHandlerSupport.EnsureLinksAsync(_db, dto, _localizer, ct).ConfigureAwait(false);
        await StockCountHandlerSupport.PopulateLineIdentitySnapshotsAsync(_db, dto, _localizer, ct).ConfigureAwait(false);
        StockCountHandlerSupport.EnsureNoDuplicateProducts(dto.Lines);

        var session = new StockCountSession
        {
            BusinessId = dto.BusinessId,
            WarehouseId = dto.WarehouseId,
            LocationId = dto.LocationId,
            AssignedToUserId = dto.AssignedToUserId,
            Title = dto.Title.Trim(),
            CountType = dto.CountType,
            Status = StockCountSessionStatus.Draft,
            CountWindowStartUtc = dto.CountWindowStartUtc,
            CountWindowEndUtc = dto.CountWindowEndUtc,
            InternalNotes = InventoryManagementHandlerSupport.NormalizeOptional(dto.InternalNotes),
            MetadataJson = InventoryManagementHandlerSupport.NormalizeMetadataJson(dto.MetadataJson),
            Lines = dto.Lines.Select((line, index) => new StockCountLine
            {
                ProductVariantId = line.ProductVariantId,
                LocationId = line.LocationId ?? dto.LocationId,
                SkuSnapshot = InventoryManagementHandlerSupport.NormalizeOptional(line.SkuSnapshot),
                Description = line.Description.Trim(),
                ExpectedQuantity = line.ExpectedQuantity,
                CountedQuantity = line.CountedQuantity,
                VarianceQuantity = line.CountedQuantity - line.ExpectedQuantity,
                ReviewStatus = line.ReviewStatus,
                ReviewNotes = InventoryManagementHandlerSupport.NormalizeOptional(line.ReviewNotes),
                SortOrder = line.SortOrder <= 0 ? index + 1 : line.SortOrder,
                MetadataJson = InventoryManagementHandlerSupport.NormalizeMetadataJson(line.MetadataJson),
                Identities = line.Identities
                    .Where(identity => !InventoryIdentityEvidenceSupport.IsBlank(identity))
                    .Select((identity, identityIndex) => StockCountHandlerSupport.CreateIdentity(identity, identityIndex + 1))
                    .ToList()
            }).ToList()
        };

        _db.Set<StockCountSession>().Add(session);
        await StockCountHandlerSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, session, "created", AuditTrailAction.Created, ct).ConfigureAwait(false);
        return session.Id;
    }
}

public sealed class UpdateStockCountHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<StockCountEditDto> _validator;
    private readonly IStringLocalizer<ValidationResource> _localizer;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public UpdateStockCountHandler(
        IAppDbContext db,
        IValidator<StockCountEditDto> validator,
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

    public async Task HandleAsync(StockCountEditDto dto, CancellationToken ct = default)
    {
        StockCountHandlerSupport.Normalize(dto);
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        StockCountHandlerSupport.EnsureSafe(dto);
        await StockCountHandlerSupport.EnsureLinksAsync(_db, dto, _localizer, ct).ConfigureAwait(false);
        await StockCountHandlerSupport.PopulateLineIdentitySnapshotsAsync(_db, dto, _localizer, ct).ConfigureAwait(false);
        StockCountHandlerSupport.EnsureNoDuplicateProducts(dto.Lines);

        var session = await _db.Set<StockCountSession>()
            .Include(x => x.Lines)
                .ThenInclude(x => x.Identities)
            .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (session is null) throw new InvalidOperationException(_localizer["StockCountNotFound"]);
        WarehouseTaskHandlerSupport.EnsureRowVersion(session.RowVersion, dto.RowVersion, _localizer);
        if (session.Status is StockCountSessionStatus.Approved or StockCountSessionStatus.Posted or StockCountSessionStatus.Rejected or StockCountSessionStatus.Cancelled)
        {
            throw new InvalidOperationException(_localizer["StockCountClosedEditRejected"]);
        }

        session.BusinessId = dto.BusinessId;
        session.WarehouseId = dto.WarehouseId;
        session.LocationId = dto.LocationId;
        session.AssignedToUserId = dto.AssignedToUserId;
        session.Title = dto.Title.Trim();
        session.CountType = dto.CountType;
        session.CountWindowStartUtc = dto.CountWindowStartUtc;
        session.CountWindowEndUtc = dto.CountWindowEndUtc;
        session.InternalNotes = InventoryManagementHandlerSupport.NormalizeOptional(dto.InternalNotes);
        session.MetadataJson = InventoryManagementHandlerSupport.NormalizeMetadataJson(dto.MetadataJson);

        _db.Set<StockCountLineIdentity>().RemoveRange(session.Lines.SelectMany(x => x.Identities));
        _db.Set<StockCountLine>().RemoveRange(session.Lines);
        session.Lines = dto.Lines.Select((line, index) => new StockCountLine
        {
            StockCountSessionId = session.Id,
            ProductVariantId = line.ProductVariantId,
            LocationId = line.LocationId ?? dto.LocationId,
            SkuSnapshot = InventoryManagementHandlerSupport.NormalizeOptional(line.SkuSnapshot),
            Description = line.Description.Trim(),
            ExpectedQuantity = line.ExpectedQuantity,
            CountedQuantity = line.CountedQuantity,
            VarianceQuantity = line.CountedQuantity - line.ExpectedQuantity,
            ReviewStatus = line.ReviewStatus,
            ReviewNotes = InventoryManagementHandlerSupport.NormalizeOptional(line.ReviewNotes),
            SortOrder = line.SortOrder <= 0 ? index + 1 : line.SortOrder,
            MetadataJson = InventoryManagementHandlerSupport.NormalizeMetadataJson(line.MetadataJson),
            Identities = line.Identities
                .Where(identity => !InventoryIdentityEvidenceSupport.IsBlank(identity))
                .Select((identity, identityIndex) => StockCountHandlerSupport.CreateIdentity(identity, identityIndex + 1))
                .ToList()
        }).ToList();

        await StockCountHandlerSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, session, "updated", AuditTrailAction.Updated, ct).ConfigureAwait(false);
    }
}

public sealed class UpdateStockCountLifecycleHandler
{
    private readonly IAppDbContext _db;
    private readonly IStringLocalizer<ValidationResource> _localizer;
    private readonly IClock _clock;
    private readonly NumberSequenceService _numbers;
    private readonly AdjustInventoryHandler _adjustInventory;
    private readonly BusinessEventService? _events;

    public UpdateStockCountLifecycleHandler(
        IAppDbContext db,
        IStringLocalizer<ValidationResource> localizer,
        IClock clock,
        NumberSequenceService numbers,
        AdjustInventoryHandler adjustInventory,
        BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _numbers = numbers ?? throw new ArgumentNullException(nameof(numbers));
        _adjustInventory = adjustInventory ?? throw new ArgumentNullException(nameof(adjustInventory));
        _events = events;
    }

    public async Task<Result> HandleAsync(StockCountLifecycleActionDto dto, CancellationToken ct = default)
    {
        if (dto.Id == Guid.Empty) return Result.Fail(_localizer["StockCountNotFound"]);
        if (dto.RowVersion.Length == 0) return Result.Fail(_localizer["ConcurrencyConflictDetected"]);
        if (FoundationInputNormalizer.LooksSensitive(dto.Notes)) return Result.Fail("StockCountSensitiveMetadataRejected");

        var session = await _db.Set<StockCountSession>()
            .Include(x => x.Lines.Where(line => !line.IsDeleted))
                .ThenInclude(x => x.Identities)
            .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (session is null) return Result.Fail(_localizer["StockCountNotFound"]);
        try
        {
            WarehouseTaskHandlerSupport.EnsureRowVersion(session.RowVersion, dto.RowVersion, _localizer);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return Result.Fail(ex.Message);
        }

        var transition = CanTransition(session.Status, dto.TargetStatus);
        if (!transition.Succeeded) return transition;

        switch (dto.TargetStatus)
        {
            case StockCountSessionStatus.Prepared:
                await PrepareAsync(session, ct).ConfigureAwait(false);
                break;
            case StockCountSessionStatus.Counted:
                foreach (var line in session.Lines)
                {
                    line.VarianceQuantity = line.CountedQuantity - line.ExpectedQuantity;
                }
                break;
            case StockCountSessionStatus.Approved:
                if (session.Lines.Any(line => line.VarianceQuantity != 0 && line.ReviewStatus != StockCountLineReviewStatus.Accepted))
                {
                    return Result.Fail(_localizer["StockCountVarianceApprovalRequired"]);
                }
                var identityValidation = await ValidateCountIdentityEvidenceAsync(session, ct).ConfigureAwait(false);
                if (!identityValidation.Succeeded) return identityValidation;
                break;
            case StockCountSessionStatus.Posted:
                var postResult = await PostAdjustmentsAsync(session, ct).ConfigureAwait(false);
                if (!postResult.Succeeded) return postResult;
                break;
        }

        ApplyStatus(session, dto.TargetStatus);
        if (!string.IsNullOrWhiteSpace(dto.Notes))
        {
            session.ReviewNotes = InventoryManagementHandlerSupport.NormalizeOptional(
                string.IsNullOrWhiteSpace(session.ReviewNotes) ? dto.Notes : $"{session.ReviewNotes}\n{dto.Notes}");
        }

        await StockCountHandlerSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, session, dto.TargetStatus.ToString().ToLowerInvariant(), AuditTrailAction.StatusChanged, ct).ConfigureAwait(false);
        return Result.Ok();
    }

    private async Task PrepareAsync(StockCountSession session, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(session.CountNumber))
        {
            var number = await _numbers.ReserveNextAsync(new NumberSequenceRequest(session.BusinessId, NumberSequenceDocumentType.StockCount, NumberSequenceService.GlobalScopeKey), ct).ConfigureAwait(false);
            session.CountNumber = number.Succeeded ? number.Value : null;
        }

        var variantIds = session.Lines.Select(x => x.ProductVariantId).Distinct().ToList();
        var expected = await _db.Set<StockLevel>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.WarehouseId == session.WarehouseId && variantIds.Contains(x.ProductVariantId))
            .ToDictionaryAsync(x => x.ProductVariantId, x => x.AvailableQuantity, ct)
            .ConfigureAwait(false);

        foreach (var line in session.Lines)
        {
            line.ExpectedQuantity = expected.GetValueOrDefault(line.ProductVariantId);
            line.VarianceQuantity = line.CountedQuantity - line.ExpectedQuantity;
        }
    }

    private async Task<Result> PostAdjustmentsAsync(StockCountSession session, CancellationToken ct)
    {
        if (session.Status != StockCountSessionStatus.Approved) return Result.Fail(_localizer["StockCountLifecycleUnsupportedAction"]);
        if (session.Lines.Any(line => line.VarianceQuantity != 0 && line.ReviewStatus != StockCountLineReviewStatus.Accepted))
        {
            return Result.Fail(_localizer["StockCountVarianceApprovalRequired"]);
        }

        var identityValidation = await ValidateCountIdentityEvidenceAsync(session, ct).ConfigureAwait(false);
        if (!identityValidation.Succeeded) return identityValidation;

        foreach (var line in session.Lines.Where(line => line.VarianceQuantity != 0))
        {
            if (line.AdjustmentPosted) continue;
            await _adjustInventory.HandleAsync(new InventoryAdjustDto
            {
                WarehouseId = session.WarehouseId,
                VariantId = line.ProductVariantId,
                QuantityDelta = line.VarianceQuantity,
                Reason = InventoryMovementReferencePolicy.StockCountAdjustment,
                ReferenceId = session.Id
            }, ct).ConfigureAwait(false);
            line.AdjustmentPosted = true;
        }

        return Result.Ok();
    }

    private Task<Result> ValidateCountIdentityEvidenceAsync(StockCountSession session, CancellationToken ct)
        => InventoryIdentityEvidenceSupport.ValidateRequiredEvidenceAsync(
            _db,
            session.BusinessId,
            session.Lines.Where(x => !x.IsDeleted),
            line => line.CountedQuantity > 0,
            line => line.ProductVariantId,
            line => line.CountedQuantity,
            line => line.Identities.Where(identity => !identity.IsDeleted),
            identity => identity.InventoryLotId,
            identity => identity.InventorySerialUnitId,
            identity => identity.HandlingUnitId,
            identity => identity.Quantity,
            identity => identity.ExpiryDateUtc,
            identity => identity.SupplierLotCodeSnapshot,
            _localizer,
            "StockCountIdentityRequired",
            "StockCountInvalidIdentityQuantity",
            "StockCountLotIdentityRequired",
            "StockCountSerialIdentityRequired",
            "StockCountExpiryIdentityRequired",
            "StockCountSupplierLotIdentityRequired",
            "StockCountHandlingUnitIdentityRequired",
            ct);

    private static Result CanTransition(StockCountSessionStatus current, StockCountSessionStatus target)
    {
        if (current == target) return Result.Ok();
        var allowed = current switch
        {
            StockCountSessionStatus.Draft => target is StockCountSessionStatus.Prepared or StockCountSessionStatus.Cancelled,
            StockCountSessionStatus.Prepared => target is StockCountSessionStatus.InProgress or StockCountSessionStatus.Cancelled,
            StockCountSessionStatus.InProgress => target is StockCountSessionStatus.Counted or StockCountSessionStatus.Cancelled,
            StockCountSessionStatus.Counted => target is StockCountSessionStatus.ReviewPending or StockCountSessionStatus.Cancelled,
            StockCountSessionStatus.ReviewPending => target is StockCountSessionStatus.Approved or StockCountSessionStatus.Rejected or StockCountSessionStatus.Cancelled,
            StockCountSessionStatus.Approved => target is StockCountSessionStatus.Posted,
            _ => false
        };
        return allowed ? Result.Ok() : Result.Fail("StockCountLifecycleUnsupportedAction");
    }

    private void ApplyStatus(StockCountSession session, StockCountSessionStatus target)
    {
        session.Status = target;
        var now = _clock.UtcNow;
        switch (target)
        {
            case StockCountSessionStatus.Prepared:
                session.PreparedAtUtc ??= now;
                break;
            case StockCountSessionStatus.InProgress:
                session.StartedAtUtc ??= now;
                break;
            case StockCountSessionStatus.Counted:
                session.CountedAtUtc ??= now;
                break;
            case StockCountSessionStatus.ReviewPending:
                session.ReviewRequestedAtUtc ??= now;
                break;
            case StockCountSessionStatus.Approved:
                session.ApprovedAtUtc ??= now;
                break;
            case StockCountSessionStatus.Posted:
                session.PostedAtUtc ??= now;
                break;
            case StockCountSessionStatus.Rejected:
                session.RejectedAtUtc ??= now;
                break;
            case StockCountSessionStatus.Cancelled:
                session.CancelledAtUtc ??= now;
                break;
        }
    }
}

internal static class StockCountHandlerSupport
{
    public static void Normalize(StockCountCreateDto dto)
    {
        dto.Title = dto.Title?.Trim() ?? string.Empty;
        dto.InternalNotes = InventoryManagementHandlerSupport.NormalizeOptional(dto.InternalNotes);
        dto.MetadataJson = InventoryManagementHandlerSupport.NormalizeMetadataJson(dto.MetadataJson);
        dto.Lines ??= new List<StockCountLineDto>();
        foreach (var line in dto.Lines)
        {
            line.SkuSnapshot = InventoryManagementHandlerSupport.NormalizeOptional(line.SkuSnapshot);
            line.Description = line.Description?.Trim() ?? string.Empty;
            line.ReviewNotes = InventoryManagementHandlerSupport.NormalizeOptional(line.ReviewNotes);
            line.MetadataJson = InventoryManagementHandlerSupport.NormalizeMetadataJson(line.MetadataJson);
            line.VarianceQuantity = line.CountedQuantity - line.ExpectedQuantity;
            line.Identities ??= new List<InventoryIdentityEvidenceDto>();
            InventoryIdentityEvidenceSupport.Normalize(line.Identities);
        }
    }

    public static void EnsureSafe(StockCountCreateDto dto)
    {
        if (FoundationInputNormalizer.LooksSensitive(dto.Title) ||
            FoundationInputNormalizer.LooksSensitive(dto.InternalNotes) ||
            FoundationInputNormalizer.LooksSensitive(dto.MetadataJson) ||
            dto.Lines.Any(line =>
                FoundationInputNormalizer.LooksSensitive(line.SkuSnapshot) ||
                FoundationInputNormalizer.LooksSensitive(line.Description) ||
                FoundationInputNormalizer.LooksSensitive(line.ReviewNotes) ||
                FoundationInputNormalizer.LooksSensitive(line.MetadataJson) ||
                InventoryIdentityEvidenceSupport.LooksSensitive(line.Identities)))
        {
            throw new ArgumentException("StockCountSensitiveMetadataRejected");
        }
    }

    public static void EnsureNoDuplicateProducts(IEnumerable<StockCountLineDto> lines)
    {
        if (lines.GroupBy(x => x.ProductVariantId).Any(g => g.Count() > 1))
        {
            throw new ValidationException("StockCountDuplicateProductLinesNotAllowed");
        }
    }

    public static async Task EnsureLinksAsync(IAppDbContext db, StockCountCreateDto dto, IStringLocalizer<ValidationResource> localizer, CancellationToken ct)
    {
        var warehouse = await db.Set<Warehouse>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == dto.WarehouseId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (warehouse is null || warehouse.BusinessId != dto.BusinessId) throw new InvalidOperationException(localizer["WarehouseNotFound"]);

        await EnsureLocationAsync(db, dto.BusinessId, dto.WarehouseId, dto.LocationId, localizer, ct).ConfigureAwait(false);
        if (dto.AssignedToUserId.HasValue)
        {
            var userExists = await db.Set<User>().AsNoTracking().AnyAsync(x => x.Id == dto.AssignedToUserId.Value && !x.IsDeleted && x.IsActive, ct).ConfigureAwait(false);
            if (!userExists) throw new InvalidOperationException(localizer["StockCountAssigneeInvalid"]);
        }

        foreach (var line in dto.Lines)
        {
            await EnsureLocationAsync(db, dto.BusinessId, dto.WarehouseId, line.LocationId ?? dto.LocationId, localizer, ct).ConfigureAwait(false);
            var variantExists = await db.Set<ProductVariant>().AsNoTracking().AnyAsync(x => x.Id == line.ProductVariantId && !x.IsDeleted, ct).ConfigureAwait(false);
            if (!variantExists) throw new InvalidOperationException(localizer["VariantNotFound"]);
        }
    }

    public static async Task PopulateLineIdentitySnapshotsAsync(IAppDbContext db, StockCountCreateDto dto, IStringLocalizer<ValidationResource> localizer, CancellationToken ct)
    {
        foreach (var line in dto.Lines)
        {
            foreach (var identity in line.Identities.Where(identity => !InventoryIdentityEvidenceSupport.IsBlank(identity)))
            {
                var snapshots = await InventoryIdentityEvidenceSupport.PopulateSnapshotsAsync(
                        db,
                        dto.BusinessId,
                        line.ProductVariantId,
                        identity.InventoryLotId,
                        identity.InventorySerialUnitId,
                        identity.HandlingUnitId,
                        localizer,
                        ct)
                    .ConfigureAwait(false);
                if (!snapshots.Succeeded) throw new InvalidOperationException(snapshots.Error);
                ApplySnapshots(identity, snapshots.Value ?? throw new InvalidOperationException("InventoryIdentitySnapshotsMissing"));
            }
        }
    }

    public static StockCountLineIdentity CreateIdentity(InventoryIdentityEvidenceDto dto, int sortOrder)
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

    private static void ApplySnapshots(InventoryIdentityEvidenceDto dto, InventoryIdentityEvidenceSupport.IdentitySnapshots snapshots)
    {
        dto.LotCodeSnapshot = snapshots.LotCodeSnapshot ?? dto.LotCodeSnapshot;
        dto.SupplierLotCodeSnapshot = snapshots.SupplierLotCodeSnapshot ?? dto.SupplierLotCodeSnapshot;
        dto.ExpiryDateUtc = snapshots.ExpiryDateUtc ?? dto.ExpiryDateUtc;
        dto.SerialNumberSnapshot = snapshots.SerialNumberSnapshot ?? dto.SerialNumberSnapshot;
        dto.HandlingUnitCodeSnapshot = snapshots.HandlingUnitCodeSnapshot ?? dto.HandlingUnitCodeSnapshot;
    }

    private static async Task EnsureLocationAsync(IAppDbContext db, Guid businessId, Guid warehouseId, Guid? locationId, IStringLocalizer<ValidationResource> localizer, CancellationToken ct)
    {
        if (!locationId.HasValue) return;
        var location = await db.Set<WarehouseLocation>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == locationId.Value && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (location is null ||
            location.BusinessId != businessId ||
            location.WarehouseId != warehouseId ||
            location.Status != WarehouseLocationStatus.Active)
        {
            throw new InvalidOperationException(localizer["WarehouseLocationNotFound"]);
        }
    }

    public static async Task RecordEvidenceOrSaveAsync(IAppDbContext db, BusinessEventService? events, IClock clock, StockCountSession session, string action, AuditTrailAction auditAction, CancellationToken ct)
    {
        if (events is null)
        {
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            return;
        }

        var now = clock.UtcNow;
        var payload = $$"""
            {"stockCountSessionId":"{{session.Id}}","businessId":"{{session.BusinessId}}","warehouseId":"{{session.WarehouseId}}","locationId":"{{session.LocationId}}","status":"{{session.Status}}","type":"{{session.CountType}}","lineCount":{{session.Lines.Count}}}
            """;
        var eventResult = await events.AddEventAsync(new AddBusinessEventCommand(
                session.BusinessId,
                "StockCountSession",
                session.Id,
                $"inventory.stock_count.{action}",
                $"inventory.stock_count.{action}:{session.Id}:{session.Status}",
                now,
                null,
                BusinessEventSource.User,
                BusinessEventSeverity.Info,
                FoundationVisibility.Internal,
                $"Stock count {action}",
                null,
                null,
                null,
                payload),
            ct).ConfigureAwait(false);
        if (!eventResult.Succeeded) throw new InvalidOperationException(eventResult.Error);

        var auditResult = await events.AddAuditTrailAsync(new AddAuditTrailCommand(
                session.BusinessId,
                "StockCountSession",
                session.Id,
                auditAction,
                now,
                null,
                eventResult.Value,
                $"Stock count {action}",
                null,
                payload),
            ct).ConfigureAwait(false);
        if (!auditResult.Succeeded) throw new InvalidOperationException(auditResult.Error);
    }
}
