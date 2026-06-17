using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Foundation;
using Darwin.Application.Inventory.DTOs;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.Foundation;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Inventory;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Inventory.Commands;

public sealed class WarehouseTaskWorkflowPolicy
{
    public Result CanTransition(WarehouseTaskStatus current, WarehouseTaskStatus target)
    {
        if (current == target) return Result.Ok();

        var allowed = current switch
        {
            WarehouseTaskStatus.Draft => target is WarehouseTaskStatus.Ready or WarehouseTaskStatus.Cancelled,
            WarehouseTaskStatus.Ready => target is WarehouseTaskStatus.Assigned or WarehouseTaskStatus.InProgress or WarehouseTaskStatus.Cancelled,
            WarehouseTaskStatus.Assigned => target is WarehouseTaskStatus.InProgress or WarehouseTaskStatus.Cancelled,
            WarehouseTaskStatus.InProgress => target is WarehouseTaskStatus.Completed or WarehouseTaskStatus.Cancelled,
            WarehouseTaskStatus.Completed => false,
            WarehouseTaskStatus.Cancelled => false,
            _ => false
        };

        return allowed ? Result.Ok() : Result.Fail("WarehouseTaskLifecycleUnsupportedAction");
    }
}

public sealed class CreateWarehouseTaskHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<WarehouseTaskCreateDto> _validator;
    private readonly IStringLocalizer<ValidationResource> _localizer;
    private readonly IClock _clock;
    private readonly NumberSequenceService _numbers;
    private readonly BusinessEventService? _events;

    public CreateWarehouseTaskHandler(
        IAppDbContext db,
        IValidator<WarehouseTaskCreateDto> validator,
        IStringLocalizer<ValidationResource> localizer,
        IClock clock,
        NumberSequenceService numbers,
        BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _numbers = numbers ?? throw new ArgumentNullException(nameof(numbers));
        _events = events;
    }

    public async Task<Guid> HandleAsync(WarehouseTaskCreateDto dto, CancellationToken ct = default)
    {
        Normalize(dto);
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        EnsureSafe(dto);
        if (dto.Status is not (WarehouseTaskStatus.Draft or WarehouseTaskStatus.Ready))
        {
            throw new ValidationException(_localizer["WarehouseTaskCreateStatusInvalid"]);
        }

        await WarehouseTaskHandlerSupport.EnsureWarehouseTaskLinksAsync(_db, dto, _localizer, ct).ConfigureAwait(false);

        var task = new WarehouseTask
        {
            BusinessId = dto.BusinessId,
            WarehouseId = dto.WarehouseId,
            FromLocationId = dto.FromLocationId,
            ToLocationId = dto.ToLocationId,
            AssignedToUserId = dto.AssignedToUserId,
            TaskNumber = await ReserveTaskNumberAsync(dto.BusinessId, ct).ConfigureAwait(false),
            Title = dto.Title.Trim(),
            TaskType = dto.TaskType,
            Status = dto.Status,
            Priority = dto.Priority,
            SourceType = dto.SourceType,
            SourceEntityId = dto.SourceEntityId,
            DueAtUtc = dto.DueAtUtc,
            ReadyAtUtc = dto.Status == WarehouseTaskStatus.Ready ? _clock.UtcNow : null,
            AssignedAtUtc = dto.AssignedToUserId.HasValue || dto.Status == WarehouseTaskStatus.Assigned ? _clock.UtcNow : null,
            StartedAtUtc = dto.Status == WarehouseTaskStatus.InProgress ? _clock.UtcNow : null,
            CompletedAtUtc = dto.Status == WarehouseTaskStatus.Completed ? _clock.UtcNow : null,
            CancelledAtUtc = dto.Status == WarehouseTaskStatus.Cancelled ? _clock.UtcNow : null,
            InternalNotes = InventoryManagementHandlerSupport.NormalizeOptional(dto.InternalNotes),
            MetadataJson = InventoryManagementHandlerSupport.NormalizeMetadataJson(dto.MetadataJson),
            Lines = dto.Lines.Select((line, index) => new WarehouseTaskLine
            {
                ProductVariantId = line.ProductVariantId,
                FromLocationId = line.FromLocationId ?? dto.FromLocationId,
                ToLocationId = line.ToLocationId ?? dto.ToLocationId,
                SkuSnapshot = InventoryManagementHandlerSupport.NormalizeOptional(line.SkuSnapshot),
                Description = line.Description.Trim(),
                RequestedQuantity = line.RequestedQuantity,
                CompletedQuantity = line.CompletedQuantity,
                ShortQuantity = line.ShortQuantity,
                ShortReason = InventoryManagementHandlerSupport.NormalizeOptional(line.ShortReason),
                SortOrder = line.SortOrder <= 0 ? index + 1 : line.SortOrder,
                SourceLineType = InventoryManagementHandlerSupport.NormalizeOptional(line.SourceLineType),
                SourceLineId = line.SourceLineId,
                MetadataJson = InventoryManagementHandlerSupport.NormalizeMetadataJson(line.MetadataJson)
            }).ToList()
        };

        _db.Set<WarehouseTask>().Add(task);
        await WarehouseTaskHandlerSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, task, "created", AuditTrailAction.Created, ct).ConfigureAwait(false);
        return task.Id;
    }

    private async Task<string?> ReserveTaskNumberAsync(Guid businessId, CancellationToken ct)
    {
        var number = await _numbers
            .ReserveNextAsync(new NumberSequenceRequest(businessId, NumberSequenceDocumentType.WarehouseTask, NumberSequenceService.GlobalScopeKey), ct)
            .ConfigureAwait(false);
        return number.Succeeded ? number.Value : null;
    }

    internal static void Normalize(WarehouseTaskCreateDto dto)
    {
        dto.Title = dto.Title?.Trim() ?? string.Empty;
        dto.InternalNotes = InventoryManagementHandlerSupport.NormalizeOptional(dto.InternalNotes);
        dto.MetadataJson = InventoryManagementHandlerSupport.NormalizeMetadataJson(dto.MetadataJson);
        dto.Lines ??= new List<WarehouseTaskLineDto>();
        foreach (var line in dto.Lines)
        {
            line.SkuSnapshot = InventoryManagementHandlerSupport.NormalizeOptional(line.SkuSnapshot);
            line.Description = line.Description?.Trim() ?? string.Empty;
            line.ShortReason = InventoryManagementHandlerSupport.NormalizeOptional(line.ShortReason);
            line.SourceLineType = InventoryManagementHandlerSupport.NormalizeOptional(line.SourceLineType);
            line.MetadataJson = InventoryManagementHandlerSupport.NormalizeMetadataJson(line.MetadataJson);
        }
    }

    internal static void EnsureSafe(WarehouseTaskCreateDto dto)
    {
        if (FoundationInputNormalizer.LooksSensitive(dto.Title) ||
            FoundationInputNormalizer.LooksSensitive(dto.InternalNotes) ||
            FoundationInputNormalizer.LooksSensitive(dto.MetadataJson) ||
            dto.Lines.Any(line =>
                FoundationInputNormalizer.LooksSensitive(line.SkuSnapshot) ||
                FoundationInputNormalizer.LooksSensitive(line.Description) ||
                FoundationInputNormalizer.LooksSensitive(line.ShortReason) ||
                FoundationInputNormalizer.LooksSensitive(line.SourceLineType) ||
                FoundationInputNormalizer.LooksSensitive(line.MetadataJson)))
        {
            throw new ArgumentException("WarehouseTaskSensitiveMetadataRejected");
        }

        foreach (var line in dto.Lines)
        {
            if (dto.TaskType != WarehouseTaskType.Picking &&
                (line.ShortQuantity > 0 || !string.IsNullOrWhiteSpace(line.ShortReason)))
            {
                throw new ValidationException("WarehouseTaskShortageOnlyAllowedForPicking");
            }

            if (line.ShortQuantity < 0 ||
                line.CompletedQuantity < 0 ||
                line.CompletedQuantity + line.ShortQuantity > line.RequestedQuantity)
            {
                throw new ValidationException("WarehouseTaskLineQuantityInvalid");
            }
        }
    }
}

public sealed class UpdateWarehouseTaskHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<WarehouseTaskEditDto> _validator;
    private readonly IStringLocalizer<ValidationResource> _localizer;
    private readonly IClock _clock;
    private readonly BusinessEventService? _events;

    public UpdateWarehouseTaskHandler(
        IAppDbContext db,
        IValidator<WarehouseTaskEditDto> validator,
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

    public async Task HandleAsync(WarehouseTaskEditDto dto, CancellationToken ct = default)
    {
        CreateWarehouseTaskHandler.Normalize(dto);
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        CreateWarehouseTaskHandler.EnsureSafe(dto);

        var task = await _db.Set<WarehouseTask>()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (task is null) throw new InvalidOperationException(_localizer["WarehouseTaskNotFound"]);
        WarehouseTaskHandlerSupport.EnsureRowVersion(task.RowVersion, dto.RowVersion, _localizer);
        if (task.Status is WarehouseTaskStatus.Completed or WarehouseTaskStatus.Cancelled)
        {
            throw new InvalidOperationException(_localizer["WarehouseTaskClosedEditRejected"]);
        }

        await WarehouseTaskHandlerSupport.EnsureWarehouseTaskLinksAsync(_db, dto, _localizer, ct).ConfigureAwait(false);

        task.BusinessId = dto.BusinessId;
        task.WarehouseId = dto.WarehouseId;
        task.FromLocationId = dto.FromLocationId;
        task.ToLocationId = dto.ToLocationId;
        task.AssignedToUserId = dto.AssignedToUserId;
        task.Title = dto.Title.Trim();
        task.TaskType = dto.TaskType;
        task.Priority = dto.Priority;
        task.SourceType = dto.SourceType;
        task.SourceEntityId = dto.SourceEntityId;
        task.DueAtUtc = dto.DueAtUtc;
        task.InternalNotes = InventoryManagementHandlerSupport.NormalizeOptional(dto.InternalNotes);
        task.MetadataJson = InventoryManagementHandlerSupport.NormalizeMetadataJson(dto.MetadataJson);

        _db.Set<WarehouseTaskLine>().RemoveRange(task.Lines);
        task.Lines = dto.Lines.Select((line, index) => new WarehouseTaskLine
        {
            WarehouseTaskId = task.Id,
            ProductVariantId = line.ProductVariantId,
            FromLocationId = line.FromLocationId ?? dto.FromLocationId,
            ToLocationId = line.ToLocationId ?? dto.ToLocationId,
            SkuSnapshot = InventoryManagementHandlerSupport.NormalizeOptional(line.SkuSnapshot),
            Description = line.Description.Trim(),
            RequestedQuantity = line.RequestedQuantity,
            CompletedQuantity = line.CompletedQuantity,
            ShortQuantity = line.ShortQuantity,
            ShortReason = InventoryManagementHandlerSupport.NormalizeOptional(line.ShortReason),
            SortOrder = line.SortOrder <= 0 ? index + 1 : line.SortOrder,
            SourceLineType = InventoryManagementHandlerSupport.NormalizeOptional(line.SourceLineType),
            SourceLineId = line.SourceLineId,
            MetadataJson = InventoryManagementHandlerSupport.NormalizeMetadataJson(line.MetadataJson)
        }).ToList();

        await WarehouseTaskHandlerSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, task, "updated", AuditTrailAction.Updated, ct).ConfigureAwait(false);
    }
}

public sealed class CreateWarehouseReceivingTaskFromGoodsReceiptHandler
{
    private readonly IAppDbContext _db;
    private readonly CreateWarehouseTaskHandler _createTask;
    private readonly IStringLocalizer<ValidationResource> _localizer;

    public CreateWarehouseReceivingTaskFromGoodsReceiptHandler(
        IAppDbContext db,
        CreateWarehouseTaskHandler createTask,
        IStringLocalizer<ValidationResource> localizer)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _createTask = createTask ?? throw new ArgumentNullException(nameof(createTask));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    public async Task<Result<Guid>> HandleAsync(CreateWarehouseReceivingTaskFromGoodsReceiptDto dto, CancellationToken ct = default)
    {
        if (dto.GoodsReceiptId == Guid.Empty) return Result<Guid>.Fail(_localizer["GoodsReceiptNotFound"]);
        if (FoundationInputNormalizer.LooksSensitive(dto.InternalNotes)) return Result<Guid>.Fail("WarehouseTaskSensitiveMetadataRejected");

        var receipt = await _db.Set<GoodsReceipt>()
            .AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == dto.GoodsReceiptId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (receipt is null) return Result<Guid>.Fail(_localizer["GoodsReceiptNotFound"]);
        if (receipt.Status is GoodsReceiptStatus.Cancelled or GoodsReceiptStatus.Posted)
        {
            return Result<Guid>.Fail(_localizer["GoodsReceiptLifecycleUnsupportedAction"]);
        }

        var duplicate = await HasActiveTaskAsync(receipt.Id, WarehouseTaskType.Receiving, ct).ConfigureAwait(false);
        if (duplicate.HasValue) return Result<Guid>.Ok(duplicate.Value);

        var lines = receipt.Lines
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .Select((line, index) => new WarehouseTaskLineDto
            {
                ProductVariantId = line.ProductVariantId,
                SkuSnapshot = line.SupplierSku,
                Description = line.Description ?? "Goods receipt line",
                RequestedQuantity = Math.Max(line.ReceivedQuantity, line.OrderedQuantity - line.PreviouslyReceivedQuantity),
                SortOrder = index + 1,
                SourceLineType = "GoodsReceiptLine",
                SourceLineId = line.Id
            })
            .Where(x => x.RequestedQuantity > 0)
            .ToList();
        if (lines.Count == 0) return Result<Guid>.Fail(_localizer["GoodsReceiptLinesRequired"]);

        var id = await _createTask.HandleAsync(new WarehouseTaskCreateDto
        {
            BusinessId = receipt.BusinessId,
            WarehouseId = receipt.WarehouseId,
            AssignedToUserId = dto.AssignedToUserId,
            Title = $"Receiving for goods receipt {receipt.GoodsReceiptNumber ?? receipt.Id.ToString("N")}",
            TaskType = WarehouseTaskType.Receiving,
            Status = WarehouseTaskStatus.Ready,
            Priority = dto.Priority,
            SourceType = WarehouseTaskSourceType.GoodsReceipt,
            SourceEntityId = receipt.Id,
            DueAtUtc = dto.DueAtUtc,
            InternalNotes = dto.InternalNotes,
            Lines = lines
        }, ct).ConfigureAwait(false);
        return Result<Guid>.Ok(id);
    }

    private async Task<Guid?> HasActiveTaskAsync(Guid receiptId, WarehouseTaskType taskType, CancellationToken ct)
    {
        return await _db.Set<WarehouseTask>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted &&
                        x.SourceType == WarehouseTaskSourceType.GoodsReceipt &&
                        x.SourceEntityId == receiptId &&
                        x.TaskType == taskType &&
                        x.Status != WarehouseTaskStatus.Cancelled)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }
}

public sealed class CreateWarehousePutawayTaskFromGoodsReceiptHandler
{
    private readonly IAppDbContext _db;
    private readonly CreateWarehouseTaskHandler _createTask;
    private readonly IStringLocalizer<ValidationResource> _localizer;

    public CreateWarehousePutawayTaskFromGoodsReceiptHandler(
        IAppDbContext db,
        CreateWarehouseTaskHandler createTask,
        IStringLocalizer<ValidationResource> localizer)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _createTask = createTask ?? throw new ArgumentNullException(nameof(createTask));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    public async Task<Result<Guid>> HandleAsync(CreateWarehousePutawayTaskFromGoodsReceiptDto dto, CancellationToken ct = default)
    {
        if (dto.GoodsReceiptId == Guid.Empty) return Result<Guid>.Fail(_localizer["GoodsReceiptNotFound"]);
        if (dto.ToLocationId == Guid.Empty) return Result<Guid>.Fail(_localizer["WarehouseLocationNotFound"]);
        if (FoundationInputNormalizer.LooksSensitive(dto.InternalNotes)) return Result<Guid>.Fail("WarehouseTaskSensitiveMetadataRejected");

        var receipt = await _db.Set<GoodsReceipt>()
            .AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == dto.GoodsReceiptId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (receipt is null) return Result<Guid>.Fail(_localizer["GoodsReceiptNotFound"]);
        if (receipt.Status != GoodsReceiptStatus.Posted)
        {
            return Result<Guid>.Fail(_localizer["GoodsReceiptLifecycleUnsupportedAction"]);
        }

        var destination = await _db.Set<WarehouseLocation>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == dto.ToLocationId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (destination is null ||
            destination.BusinessId != receipt.BusinessId ||
            destination.WarehouseId != receipt.WarehouseId ||
            destination.Status != WarehouseLocationStatus.Active)
        {
            return Result<Guid>.Fail(_localizer["WarehouseLocationNotFound"]);
        }

        var duplicate = await HasActiveTaskAsync(receipt.Id, WarehouseTaskType.Putaway, ct).ConfigureAwait(false);
        if (duplicate.HasValue) return Result<Guid>.Ok(duplicate.Value);

        var lines = receipt.Lines
            .Where(x => !x.IsDeleted && x.AcceptedQuantity > 0)
            .OrderBy(x => x.SortOrder)
            .Select((line, index) => new WarehouseTaskLineDto
            {
                ProductVariantId = line.ProductVariantId,
                ToLocationId = destination.Id,
                SkuSnapshot = line.SupplierSku,
                Description = line.Description ?? "Accepted goods receipt line",
                RequestedQuantity = line.AcceptedQuantity,
                SortOrder = index + 1,
                SourceLineType = "GoodsReceiptLine",
                SourceLineId = line.Id
            })
            .ToList();
        if (lines.Count == 0) return Result<Guid>.Fail(_localizer["GoodsReceiptInvalidQuantity"]);

        var id = await _createTask.HandleAsync(new WarehouseTaskCreateDto
        {
            BusinessId = receipt.BusinessId,
            WarehouseId = receipt.WarehouseId,
            ToLocationId = destination.Id,
            AssignedToUserId = dto.AssignedToUserId,
            Title = $"Putaway for goods receipt {receipt.GoodsReceiptNumber ?? receipt.Id.ToString("N")}",
            TaskType = WarehouseTaskType.Putaway,
            Status = WarehouseTaskStatus.Ready,
            Priority = dto.Priority,
            SourceType = WarehouseTaskSourceType.GoodsReceipt,
            SourceEntityId = receipt.Id,
            DueAtUtc = dto.DueAtUtc,
            InternalNotes = dto.InternalNotes,
            Lines = lines
        }, ct).ConfigureAwait(false);
        return Result<Guid>.Ok(id);
    }

    private async Task<Guid?> HasActiveTaskAsync(Guid receiptId, WarehouseTaskType taskType, CancellationToken ct)
    {
        return await _db.Set<WarehouseTask>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted &&
                        x.SourceType == WarehouseTaskSourceType.GoodsReceipt &&
                        x.SourceEntityId == receiptId &&
                        x.TaskType == taskType &&
                        x.Status != WarehouseTaskStatus.Cancelled)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }
}

public sealed class CreateWarehousePickingTaskFromOrderHandler
{
    private readonly IAppDbContext _db;
    private readonly CreateWarehouseTaskHandler _createTask;
    private readonly IStringLocalizer<ValidationResource> _localizer;

    public CreateWarehousePickingTaskFromOrderHandler(
        IAppDbContext db,
        CreateWarehouseTaskHandler createTask,
        IStringLocalizer<ValidationResource> localizer)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _createTask = createTask ?? throw new ArgumentNullException(nameof(createTask));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    public async Task<Result<Guid>> HandleAsync(CreateWarehousePickingTaskFromOrderDto dto, CancellationToken ct = default)
    {
        if (dto.OrderId == Guid.Empty || dto.BusinessId == Guid.Empty || dto.WarehouseId == Guid.Empty)
        {
            return Result<Guid>.Fail(_localizer["InvalidUpdateRequest"]);
        }

        if (FoundationInputNormalizer.LooksSensitive(dto.InternalNotes))
        {
            return Result<Guid>.Fail("WarehouseTaskSensitiveMetadataRejected");
        }

        var order = await _db.Set<Order>()
            .AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == dto.OrderId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (order is null) return Result<Guid>.Fail(_localizer["OrderNotFound"]);
        if (order.BusinessId != dto.BusinessId)
        {
            return Result<Guid>.Fail(_localizer["OrderNotFound"]);
        }

        if (order.Status is OrderStatus.Cancelled or OrderStatus.Refunded or OrderStatus.Completed)
        {
            return Result<Guid>.Fail(_localizer["WarehouseTaskPickingOrderStatusInvalid"]);
        }

        var warehouse = await _db.Set<Warehouse>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == dto.WarehouseId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (warehouse is null || warehouse.BusinessId != dto.BusinessId)
        {
            return Result<Guid>.Fail(_localizer["WarehouseNotFound"]);
        }

        var duplicate = await _db.Set<WarehouseTask>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted &&
                        x.SourceType == WarehouseTaskSourceType.Order &&
                        x.SourceEntityId == order.Id &&
                        x.TaskType == WarehouseTaskType.Picking &&
                        x.WarehouseId == dto.WarehouseId &&
                        x.Status != WarehouseTaskStatus.Cancelled)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        if (duplicate.HasValue) return Result<Guid>.Ok(duplicate.Value);

        await WarehouseTaskHandlerSupport.EnsureLocationForTaskAsync(_db, dto.BusinessId, dto.WarehouseId, dto.FromLocationId, _localizer, ct).ConfigureAwait(false);

        var allocatedRows = await _db.Set<InventoryTransaction>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted &&
                        x.ReferenceId == order.Id &&
                        x.Reason == InventoryMovementReferencePolicy.ShipmentAllocation &&
                        x.WarehouseId == dto.WarehouseId &&
                        x.QuantityDelta < 0)
            .GroupBy(x => x.ProductVariantId)
            .Select(g => new { ProductVariantId = g.Key, Quantity = -g.Sum(x => x.QuantityDelta) })
            .ToDictionaryAsync(x => x.ProductVariantId, x => x.Quantity, ct)
            .ConfigureAwait(false);
        if (allocatedRows.Count == 0)
        {
            return Result<Guid>.Fail(_localizer["WarehouseTaskPickingAllocationMissing"]);
        }

        var runningByVariant = new Dictionary<Guid, int>();
        var lines = new List<WarehouseTaskLineDto>();
        foreach (var line in order.Lines.Where(x => !x.IsDeleted && x.Quantity > 0 && x.VariantId.HasValue).OrderBy(x => x.CreatedAtUtc))
        {
            if (line.WarehouseId.HasValue && line.WarehouseId.Value != dto.WarehouseId)
            {
                continue;
            }

            if (line.VariantId is not Guid variantId)
            {
                continue;
            }

            if (!allocatedRows.TryGetValue(variantId, out var allocatedQuantity))
            {
                continue;
            }

            var alreadyUsed = runningByVariant.GetValueOrDefault(variantId);
            var remainingAllocated = allocatedQuantity - alreadyUsed;
            var requestedQuantity = Math.Min(line.Quantity, remainingAllocated);
            if (requestedQuantity <= 0) continue;

            runningByVariant[variantId] = alreadyUsed + requestedQuantity;
            lines.Add(new WarehouseTaskLineDto
            {
                ProductVariantId = variantId,
                FromLocationId = dto.FromLocationId,
                SkuSnapshot = line.Sku,
                Description = line.Name,
                RequestedQuantity = requestedQuantity,
                SortOrder = lines.Count + 1,
                SourceLineType = "OrderLine",
                SourceLineId = line.Id
            });
        }

        if (lines.Count == 0)
        {
            return Result<Guid>.Fail(_localizer["WarehouseTaskPickingAllocationMissing"]);
        }

        var id = await _createTask.HandleAsync(new WarehouseTaskCreateDto
        {
            BusinessId = dto.BusinessId,
            WarehouseId = dto.WarehouseId,
            FromLocationId = dto.FromLocationId,
            AssignedToUserId = dto.AssignedToUserId,
            Title = $"Picking for order {order.OrderNumber}",
            TaskType = WarehouseTaskType.Picking,
            Status = WarehouseTaskStatus.Ready,
            Priority = dto.Priority,
            SourceType = WarehouseTaskSourceType.Order,
            SourceEntityId = order.Id,
            DueAtUtc = dto.DueAtUtc,
            InternalNotes = dto.InternalNotes,
            Lines = lines
        }, ct).ConfigureAwait(false);
        return Result<Guid>.Ok(id);
    }
}

public sealed class UpdateWarehouseTaskLifecycleHandler
{
    private readonly IAppDbContext _db;
    private readonly IStringLocalizer<ValidationResource> _localizer;
    private readonly IClock _clock;
    private readonly WarehouseTaskWorkflowPolicy _workflow;
    private readonly BusinessEventService? _events;

    public UpdateWarehouseTaskLifecycleHandler(
        IAppDbContext db,
        IStringLocalizer<ValidationResource> localizer,
        IClock clock,
        WarehouseTaskWorkflowPolicy workflow,
        BusinessEventService? events = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
        _events = events;
    }

    public async Task<Result> HandleAsync(WarehouseTaskLifecycleActionDto dto, CancellationToken ct = default)
    {
        if (dto.Id == Guid.Empty) return Result.Fail(_localizer["InvalidUpdateRequest"]);
        if ((dto.RowVersion ?? Array.Empty<byte>()).Length == 0) return Result.Fail(_localizer["RowVersionRequired"]);
        if (FoundationInputNormalizer.LooksSensitive(dto.Notes)) return Result.Fail("WarehouseTaskSensitiveMetadataRejected");

        var task = await _db.Set<WarehouseTask>()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (task is null) return Result.Fail(_localizer["WarehouseTaskNotFound"]);
        if (!(task.RowVersion ?? Array.Empty<byte>()).SequenceEqual(dto.RowVersion ?? Array.Empty<byte>()))
        {
            return Result.Fail(_localizer["ItemConcurrencyConflict"]);
        }

        var workflow = _workflow.CanTransition(task.Status, dto.TargetStatus);
        if (!workflow.Succeeded) return Result.Fail(_localizer[workflow.Error ?? "WarehouseTaskLifecycleUnsupportedAction"]);

        if (dto.AssignedToUserId.HasValue)
        {
            var userExists = await _db.Set<User>()
                .AsNoTracking()
                .AnyAsync(x => x.Id == dto.AssignedToUserId.Value && !x.IsDeleted && x.IsActive, ct)
                .ConfigureAwait(false);
            if (!userExists) return Result.Fail(_localizer["WarehouseTaskAssigneeInvalid"]);
            task.AssignedToUserId = dto.AssignedToUserId;
            task.AssignedAtUtc ??= _clock.UtcNow;
        }

        if (dto.TargetStatus == WarehouseTaskStatus.Assigned && !task.AssignedToUserId.HasValue)
        {
            return Result.Fail(_localizer["WarehouseTaskAssigneeRequired"]);
        }

        if (dto.TargetStatus == WarehouseTaskStatus.Completed)
        {
            var taskValidation = await ValidateCompletionOwnerRulesAsync(task, ct).ConfigureAwait(false);
            if (!taskValidation.Succeeded) return taskValidation;

            foreach (var line in task.Lines)
            {
                if (line.ShortQuantity > 0)
                {
                    line.CompletedQuantity = Math.Max(0, line.RequestedQuantity - line.ShortQuantity);
                }
                else
                {
                    line.CompletedQuantity = line.RequestedQuantity;
                }
            }
        }

        ApplyStatus(task, dto.TargetStatus);
        if (!string.IsNullOrWhiteSpace(dto.Notes))
        {
            task.InternalNotes = InventoryManagementHandlerSupport.NormalizeOptional(
                string.IsNullOrWhiteSpace(task.InternalNotes) ? dto.Notes : $"{task.InternalNotes}\n{dto.Notes}");
        }

        await WarehouseTaskHandlerSupport.RecordEvidenceOrSaveAsync(_db, _events, _clock, task, dto.TargetStatus.ToString().ToLowerInvariant(), AuditTrailAction.StatusChanged, ct).ConfigureAwait(false);
        return Result.Ok();
    }

    private async Task<Result> ValidateCompletionOwnerRulesAsync(WarehouseTask task, CancellationToken ct)
    {
        if (task.TaskType == WarehouseTaskType.Putaway && task.SourceType == WarehouseTaskSourceType.GoodsReceipt)
        {
            if (!task.SourceEntityId.HasValue) return Result.Fail(_localizer["GoodsReceiptNotFound"]);

            var receipt = await _db.Set<GoodsReceipt>()
                .AsNoTracking()
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == task.SourceEntityId.Value && !x.IsDeleted, ct)
                .ConfigureAwait(false);
            if (receipt is null || receipt.Status != GoodsReceiptStatus.Posted)
            {
                return Result.Fail(_localizer["GoodsReceiptLifecycleUnsupportedAction"]);
            }

            var sourceLineIds = task.Lines
                .Where(x => !x.IsDeleted)
                .Select(x => x.SourceLineId)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToHashSet();
            var acceptedByLine = receipt.Lines
                .Where(x => !x.IsDeleted && sourceLineIds.Contains(x.Id))
                .ToDictionary(x => x.Id, x => x.AcceptedQuantity);
            foreach (var line in task.Lines.Where(x => !x.IsDeleted))
            {
                if (!line.ToLocationId.HasValue ||
                    !line.SourceLineId.HasValue ||
                    !acceptedByLine.TryGetValue(line.SourceLineId.Value, out var acceptedQuantity) ||
                    acceptedQuantity <= 0 ||
                    line.RequestedQuantity != acceptedQuantity)
                {
                    return Result.Fail(_localizer["WarehouseTaskPutawayInvalid"]);
                }
            }
        }
        else if (task.TaskType == WarehouseTaskType.Picking && task.SourceType == WarehouseTaskSourceType.Order)
        {
            var pickValidation = await ValidatePickingCompletionAsync(task, ct).ConfigureAwait(false);
            if (!pickValidation.Succeeded) return pickValidation;
        }

        return Result.Ok();
    }

    private async Task<Result> ValidatePickingCompletionAsync(WarehouseTask task, CancellationToken ct)
    {
        if (!task.SourceEntityId.HasValue) return Result.Fail(_localizer["OrderNotFound"]);
        var order = await _db.Set<Order>()
            .AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == task.SourceEntityId.Value && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (order is null || order.BusinessId != task.BusinessId)
        {
            return Result.Fail(_localizer["OrderNotFound"]);
        }

        if (order.Status is OrderStatus.Cancelled or OrderStatus.Refunded or OrderStatus.Completed)
        {
            return Result.Fail(_localizer["WarehouseTaskPickingOrderStatusInvalid"]);
        }

        var allocatedRows = await _db.Set<InventoryTransaction>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted &&
                        x.ReferenceId == order.Id &&
                        x.Reason == InventoryMovementReferencePolicy.ShipmentAllocation &&
                        x.WarehouseId == task.WarehouseId &&
                        x.QuantityDelta < 0)
            .GroupBy(x => x.ProductVariantId)
            .Select(g => new { ProductVariantId = g.Key, Quantity = -g.Sum(x => x.QuantityDelta) })
            .ToDictionaryAsync(x => x.ProductVariantId, x => x.Quantity, ct)
            .ConfigureAwait(false);
        var requestedByVariant = new Dictionary<Guid, int>();
        var orderLineIds = order.Lines.Where(x => !x.IsDeleted).Select(x => x.Id).ToHashSet();
        foreach (var line in task.Lines.Where(x => !x.IsDeleted))
        {
            if (!line.ProductVariantId.HasValue ||
                !line.SourceLineId.HasValue ||
                !orderLineIds.Contains(line.SourceLineId.Value) ||
                line.RequestedQuantity <= 0 ||
                line.ShortQuantity < 0 ||
                line.CompletedQuantity < 0 ||
                line.CompletedQuantity + line.ShortQuantity > line.RequestedQuantity)
            {
                return Result.Fail(_localizer["WarehouseTaskPickingInvalid"]);
            }

            if (line.FromLocationId.HasValue)
            {
                await WarehouseTaskHandlerSupport.EnsureLocationForTaskAsync(_db, task.BusinessId, task.WarehouseId, line.FromLocationId, _localizer, ct).ConfigureAwait(false);
            }

            var variantId = line.ProductVariantId.GetValueOrDefault();
            requestedByVariant[variantId] = requestedByVariant.GetValueOrDefault(variantId) + line.RequestedQuantity;
        }

        foreach (var (variantId, requestedQuantity) in requestedByVariant)
        {
            if (!allocatedRows.TryGetValue(variantId, out var allocatedQuantity) || requestedQuantity > allocatedQuantity)
            {
                return Result.Fail(_localizer["WarehouseTaskPickingAllocationMissing"]);
            }
        }

        return Result.Ok();
    }

    private void ApplyStatus(WarehouseTask task, WarehouseTaskStatus target)
    {
        task.Status = target;
        var now = _clock.UtcNow;
        switch (target)
        {
            case WarehouseTaskStatus.Ready:
                task.ReadyAtUtc ??= now;
                break;
            case WarehouseTaskStatus.Assigned:
                task.AssignedAtUtc ??= now;
                break;
            case WarehouseTaskStatus.InProgress:
                task.StartedAtUtc ??= now;
                break;
            case WarehouseTaskStatus.Completed:
                task.CompletedAtUtc ??= now;
                break;
            case WarehouseTaskStatus.Cancelled:
                task.CancelledAtUtc ??= now;
                break;
        }
    }
}

internal static class WarehouseTaskHandlerSupport
{
    public static void EnsureRowVersion(byte[]? current, byte[]? provided, IStringLocalizer<ValidationResource> localizer)
    {
        var rowVersion = provided ?? Array.Empty<byte>();
        if (rowVersion.Length == 0 || !(current ?? Array.Empty<byte>()).SequenceEqual(rowVersion))
        {
            throw new DbUpdateConcurrencyException(localizer["ConcurrencyConflictDetected"]);
        }
    }

    public static async Task EnsureWarehouseTaskLinksAsync(IAppDbContext db, WarehouseTaskCreateDto dto, IStringLocalizer<ValidationResource> localizer, CancellationToken ct)
    {
        var warehouse = await db.Set<Warehouse>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == dto.WarehouseId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (warehouse is null || warehouse.BusinessId != dto.BusinessId) throw new InvalidOperationException(localizer["WarehouseNotFound"]);

        await EnsureLocationAsync(db, dto.BusinessId, dto.WarehouseId, dto.FromLocationId, localizer, ct).ConfigureAwait(false);
        await EnsureLocationAsync(db, dto.BusinessId, dto.WarehouseId, dto.ToLocationId, localizer, ct).ConfigureAwait(false);
        if (dto.AssignedToUserId.HasValue)
        {
            var userExists = await db.Set<User>().AsNoTracking().AnyAsync(x => x.Id == dto.AssignedToUserId.Value && !x.IsDeleted && x.IsActive, ct).ConfigureAwait(false);
            if (!userExists) throw new InvalidOperationException(localizer["WarehouseTaskAssigneeInvalid"]);
        }

        foreach (var line in dto.Lines)
        {
            await EnsureLocationAsync(db, dto.BusinessId, dto.WarehouseId, line.FromLocationId, localizer, ct).ConfigureAwait(false);
            await EnsureLocationAsync(db, dto.BusinessId, dto.WarehouseId, line.ToLocationId, localizer, ct).ConfigureAwait(false);
            if (line.ProductVariantId.HasValue)
            {
                var variantExists = await db.Set<ProductVariant>().AsNoTracking().AnyAsync(x => x.Id == line.ProductVariantId.Value && !x.IsDeleted, ct).ConfigureAwait(false);
                if (!variantExists) throw new InvalidOperationException(localizer["VariantNotFound"]);
            }
        }
    }

    public static Task EnsureLocationForTaskAsync(IAppDbContext db, Guid businessId, Guid warehouseId, Guid? locationId, IStringLocalizer<ValidationResource> localizer, CancellationToken ct)
        => EnsureLocationAsync(db, businessId, warehouseId, locationId, localizer, ct);

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

    public static async Task RecordEvidenceOrSaveAsync(IAppDbContext db, BusinessEventService? events, IClock clock, WarehouseTask task, string action, AuditTrailAction auditAction, CancellationToken ct)
    {
        if (events is null)
        {
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            return;
        }

        var now = clock.UtcNow;
        var payload = $$"""
            {"warehouseTaskId":"{{task.Id}}","businessId":"{{task.BusinessId}}","warehouseId":"{{task.WarehouseId}}","status":"{{task.Status}}","type":"{{task.TaskType}}","priority":"{{task.Priority}}","sourceType":"{{task.SourceType}}","sourceEntityId":"{{task.SourceEntityId}}","lineCount":{{task.Lines.Count}}}
            """;
        var eventResult = await events.AddEventAsync(new AddBusinessEventCommand(
                task.BusinessId,
                "WarehouseTask",
                task.Id,
                $"inventory.warehouse_task.{action}",
                $"inventory.warehouse_task.{action}:{task.Id}:{task.Status}",
                now,
                null,
                BusinessEventSource.User,
                BusinessEventSeverity.Info,
                FoundationVisibility.Internal,
                $"Warehouse task {action}",
                null,
                null,
                null,
                payload),
            ct).ConfigureAwait(false);
        if (!eventResult.Succeeded) throw new InvalidOperationException(eventResult.Error);

        var auditResult = await events.AddAuditTrailAsync(new AddAuditTrailCommand(
                task.BusinessId,
                "WarehouseTask",
                task.Id,
                auditAction,
                now,
                null,
                eventResult.Value,
                $"Warehouse task {action}",
                null,
                payload),
            ct).ConfigureAwait(false);
        if (!auditResult.Succeeded) throw new InvalidOperationException(auditResult.Error);
    }
}
