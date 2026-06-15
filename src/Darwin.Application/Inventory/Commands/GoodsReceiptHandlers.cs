using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Foundation;
using Darwin.Application.Inventory.DTOs;
using Darwin.Application.Inventory.Validators;
using Darwin.Domain.Entities.Inventory;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Inventory.Commands;

public sealed class GoodsReceiptWorkflowPolicy
{
    public Result CanReceive(GoodsReceipt receipt)
        => receipt.Status == GoodsReceiptStatus.Draft
            ? Result.Ok()
            : Result.Fail("GoodsReceiptLifecycleUnsupportedAction");

    public Result CanInspect(GoodsReceipt receipt)
        => receipt.Status == GoodsReceiptStatus.Received
            ? Result.Ok()
            : Result.Fail("GoodsReceiptLifecycleUnsupportedAction");

    public Result CanPost(GoodsReceipt receipt)
        => receipt.Status == GoodsReceiptStatus.Inspected || receipt.Status == GoodsReceiptStatus.Posted
            ? Result.Ok()
            : Result.Fail("GoodsReceiptLifecycleUnsupportedAction");

    public Result CanCancel(GoodsReceipt receipt)
        => receipt.Status is GoodsReceiptStatus.Draft or GoodsReceiptStatus.Received or GoodsReceiptStatus.Inspected
            ? Result.Ok()
            : Result.Fail("GoodsReceiptLifecycleUnsupportedAction");
}

public sealed class CreateGoodsReceiptFromPurchaseOrderHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<GoodsReceiptCreateDto> _validator;
    private readonly IStringLocalizer<ValidationResource> _localizer;

    public CreateGoodsReceiptFromPurchaseOrderHandler(
        IAppDbContext db,
        IValidator<GoodsReceiptCreateDto> validator,
        IStringLocalizer<ValidationResource> localizer)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    public async Task<Result<Guid>> HandleAsync(GoodsReceiptCreateDto dto, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);

        var warehouse = await _db.Set<Warehouse>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == dto.WarehouseId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (warehouse is null)
        {
            return Result<Guid>.Fail(_localizer["NoWarehouseIsConfigured"]);
        }

        var order = await _db.Set<PurchaseOrder>()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == dto.PurchaseOrderId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (order is null)
        {
            return Result<Guid>.Fail(_localizer["PurchaseOrderNotFound"]);
        }

        if (order.BusinessId != warehouse.BusinessId || order.Status != PurchaseOrderStatus.Issued)
        {
            return Result<Guid>.Fail(_localizer["PurchaseOrderLifecycleUnsupportedAction"]);
        }

        var lines = order.Lines
            .Where(x => !x.IsDeleted && x.Quantity > x.ReceivedQuantity + x.CancelledQuantity)
            .OrderBy(x => x.CreatedAtUtc)
            .Select((line, index) => new GoodsReceiptLine
            {
                PurchaseOrderLineId = line.Id,
                ProductVariantId = line.ProductVariantId,
                SupplierSku = InventoryManagementHandlerSupport.NormalizeOptional(line.SupplierSku),
                Description = InventoryManagementHandlerSupport.NormalizeOptional(line.Description),
                OrderedQuantity = line.Quantity,
                PreviouslyReceivedQuantity = line.ReceivedQuantity,
                ReceivedQuantity = line.Quantity - line.ReceivedQuantity - line.CancelledQuantity,
                AcceptedQuantity = 0,
                RejectedQuantity = 0,
                DamagedQuantity = 0,
                UnitCostMinor = line.UnitCostMinor,
                TotalCostMinor = line.TotalCostMinor,
                SortOrder = index
            })
            .ToList();

        if (lines.Count == 0)
        {
            return Result<Guid>.Fail(_localizer["PurchaseOrderLifecycleUnsupportedAction"]);
        }

        var receipt = new GoodsReceipt
        {
            BusinessId = order.BusinessId,
            SupplierId = order.SupplierId,
            PurchaseOrderId = order.Id,
            WarehouseId = warehouse.Id,
            Status = GoodsReceiptStatus.Draft,
            InternalNotes = InventoryManagementHandlerSupport.NormalizeOptional(dto.InternalNotes),
            Lines = lines
        };

        _db.Set<GoodsReceipt>().Add(receipt);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result<Guid>.Ok(receipt.Id);
    }
}

public sealed class UpdateGoodsReceiptLifecycleHandler
{
    public const string ReceiveAction = "Receive";
    public const string InspectAction = "Inspect";
    public const string PostAction = "Post";
    public const string CancelAction = "Cancel";
    public const string PostedReason = "GoodsReceiptPosted";

    private readonly IAppDbContext _db;
    private readonly IStringLocalizer<ValidationResource> _localizer;
    private readonly IClock _clock;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly GoodsReceiptWorkflowPolicy _workflow;
    private readonly BusinessEventService? _businessEventService;

    public UpdateGoodsReceiptLifecycleHandler(
        IAppDbContext db,
        IStringLocalizer<ValidationResource> localizer,
        IClock clock,
        NumberSequenceService numberSequenceService,
        GoodsReceiptWorkflowPolicy workflow,
        BusinessEventService? businessEventService = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _numberSequenceService = numberSequenceService ?? throw new ArgumentNullException(nameof(numberSequenceService));
        _workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
        _businessEventService = businessEventService;
    }

    public UpdateGoodsReceiptLifecycleHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        : this(
            db,
            localizer,
            InventoryManagementHandlerSupport.DefaultClock,
            new NumberSequenceService(db, InventoryManagementHandlerSupport.DefaultClock),
            new GoodsReceiptWorkflowPolicy())
    {
    }

    public async Task<Result> HandleAsync(GoodsReceiptLifecycleActionDto dto, CancellationToken ct = default)
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

        var receipt = await _db.Set<GoodsReceipt>()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (receipt is null)
        {
            return Result.Fail(_localizer["GoodsReceiptNotFound"]);
        }

        var currentVersion = receipt.RowVersion ?? Array.Empty<byte>();
        if (!currentVersion.SequenceEqual(rowVersion))
        {
            return Result.Fail(_localizer["ItemConcurrencyConflict"]);
        }

        var action = (dto.Action ?? string.Empty).Trim();
        Result result;
        if (string.Equals(action, ReceiveAction, StringComparison.OrdinalIgnoreCase))
        {
            result = await ReceiveAsync(receipt, dto.Lines, ct).ConfigureAwait(false);
        }
        else if (string.Equals(action, InspectAction, StringComparison.OrdinalIgnoreCase))
        {
            result = await InspectAsync(receipt, dto.Lines, ct).ConfigureAwait(false);
        }
        else if (string.Equals(action, PostAction, StringComparison.OrdinalIgnoreCase))
        {
            result = await PostAsync(receipt, ct).ConfigureAwait(false);
        }
        else if (string.Equals(action, CancelAction, StringComparison.OrdinalIgnoreCase))
        {
            result = Cancel(receipt);
        }
        else
        {
            result = Result.Fail(_localizer["GoodsReceiptLifecycleUnsupportedAction"]);
        }

        if (!result.Succeeded)
        {
            return result;
        }

        try
        {
            await RecordGoodsReceiptEvidenceOrSaveAsync(receipt, action, ct).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Fail(_localizer["ItemConcurrencyConflict"]);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(ex.Message);
        }

        return Result.Ok();
    }

    private async Task<Result> ReceiveAsync(GoodsReceipt receipt, IReadOnlyList<GoodsReceiptLineDto> lines, CancellationToken ct)
    {
        var canReceive = _workflow.CanReceive(receipt);
        if (!canReceive.Succeeded)
        {
            return Result.Fail(_localizer[canReceive.Error ?? "GoodsReceiptLifecycleUnsupportedAction"]);
        }

        if (lines.Count == 0)
        {
            return Result.Fail(_localizer["GoodsReceiptLinesRequired"]);
        }

        foreach (var line in receipt.Lines.Where(x => !x.IsDeleted))
        {
            var input = lines.FirstOrDefault(x => x.Id == line.Id || x.PurchaseOrderLineId == line.PurchaseOrderLineId);
            var remaining = Math.Max(0, line.OrderedQuantity - line.PreviouslyReceivedQuantity);
            var receivedQuantity = input?.ReceivedQuantity ?? line.ReceivedQuantity;
            if (receivedQuantity <= 0 || receivedQuantity > remaining)
            {
                return Result.Fail(_localizer["GoodsReceiptInvalidQuantity"]);
            }

            line.ReceivedQuantity = receivedQuantity;
        }

        if (string.IsNullOrWhiteSpace(receipt.GoodsReceiptNumber))
        {
            var sequence = await _numberSequenceService.ReserveNextAsync(
                    new NumberSequenceRequest(receipt.BusinessId, NumberSequenceDocumentType.GoodsReceipt, NumberSequenceService.GlobalScopeKey),
                    ct)
                .ConfigureAwait(false);
            if (sequence.Succeeded && !string.IsNullOrWhiteSpace(sequence.Value))
            {
                receipt.GoodsReceiptNumber = sequence.Value;
            }
        }

        receipt.Status = GoodsReceiptStatus.Received;
        receipt.ReceivedAtUtc ??= _clock.UtcNow;
        return Result.Ok();
    }

    private Task<Result> InspectAsync(GoodsReceipt receipt, IReadOnlyList<GoodsReceiptLineDto> lines, CancellationToken ct)
    {
        _ = ct;
        var canInspect = _workflow.CanInspect(receipt);
        if (!canInspect.Succeeded)
        {
            return Task.FromResult(Result.Fail(_localizer[canInspect.Error ?? "GoodsReceiptLifecycleUnsupportedAction"]));
        }

        if (lines.Count == 0)
        {
            return Task.FromResult(Result.Fail(_localizer["GoodsReceiptLinesRequired"]));
        }

        foreach (var line in receipt.Lines.Where(x => !x.IsDeleted))
        {
            var input = lines.FirstOrDefault(x => x.Id == line.Id || x.PurchaseOrderLineId == line.PurchaseOrderLineId);
            if (input is null)
            {
                return Task.FromResult(Result.Fail(_localizer["GoodsReceiptLinesRequired"]));
            }

            if (input.AcceptedQuantity < 0 || input.RejectedQuantity < 0 || input.DamagedQuantity < 0)
            {
                return Task.FromResult(Result.Fail(_localizer["GoodsReceiptInvalidQuantity"]));
            }

            var inspectedTotal = input.AcceptedQuantity + input.RejectedQuantity + input.DamagedQuantity;
            if (inspectedTotal != line.ReceivedQuantity)
            {
                return Task.FromResult(Result.Fail(_localizer["GoodsReceiptInvalidQuantity"]));
            }

            line.AcceptedQuantity = input.AcceptedQuantity;
            line.RejectedQuantity = input.RejectedQuantity;
            line.DamagedQuantity = input.DamagedQuantity;
        }

        receipt.Status = GoodsReceiptStatus.Inspected;
        receipt.InspectedAtUtc ??= _clock.UtcNow;
        return Task.FromResult(Result.Ok());
    }

    private async Task<Result> PostAsync(GoodsReceipt receipt, CancellationToken ct)
    {
        var canPost = _workflow.CanPost(receipt);
        if (!canPost.Succeeded)
        {
            return Result.Fail(_localizer[canPost.Error ?? "GoodsReceiptLifecycleUnsupportedAction"]);
        }

        if (receipt.Status == GoodsReceiptStatus.Posted)
        {
            return Result.Ok();
        }

        var alreadyPosted = await _db.Set<InventoryTransaction>()
            .AsNoTracking()
            .AnyAsync(x => x.ReferenceId == receipt.Id && x.Reason == PostedReason && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (alreadyPosted)
        {
            receipt.Status = GoodsReceiptStatus.Posted;
            receipt.PostedAtUtc ??= _clock.UtcNow;
            return Result.Ok();
        }

        var purchaseOrder = await _db.Set<PurchaseOrder>()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == receipt.PurchaseOrderId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (purchaseOrder is null)
        {
            return Result.Fail(_localizer["PurchaseOrderNotFound"]);
        }

        var variantIdsToRefresh = new HashSet<Guid>();
        foreach (var line in receipt.Lines.Where(x => !x.IsDeleted && x.AcceptedQuantity > 0))
        {
            var purchaseLine = purchaseOrder.Lines.FirstOrDefault(x => x.Id == line.PurchaseOrderLineId && !x.IsDeleted);
            if (purchaseLine is null)
            {
                return Result.Fail(_localizer["PurchaseOrderNotFound"]);
            }

            var remaining = Math.Max(0, purchaseLine.Quantity - purchaseLine.ReceivedQuantity - purchaseLine.CancelledQuantity);
            if (line.AcceptedQuantity > remaining)
            {
                return Result.Fail(_localizer["GoodsReceiptInvalidQuantity"]);
            }

            var stockLevel = await InventoryStockHelper.GetOrCreateStockLevelAsync(_db, receipt.WarehouseId, line.ProductVariantId, ct).ConfigureAwait(false);
            stockLevel.AvailableQuantity += line.AcceptedQuantity;
            purchaseLine.ReceivedQuantity += line.AcceptedQuantity;
            _db.Set<InventoryTransaction>().Add(new InventoryTransaction
            {
                WarehouseId = receipt.WarehouseId,
                ProductVariantId = line.ProductVariantId,
                QuantityDelta = line.AcceptedQuantity,
                Reason = PostedReason,
                ReferenceId = receipt.Id
            });
            variantIdsToRefresh.Add(line.ProductVariantId);
        }

        if (purchaseOrder.Lines.Where(x => !x.IsDeleted).All(x => x.ReceivedQuantity + x.CancelledQuantity >= x.Quantity))
        {
            purchaseOrder.Status = PurchaseOrderStatus.Received;
            purchaseOrder.ReceivedAtUtc ??= _clock.UtcNow;
        }

        receipt.Status = GoodsReceiptStatus.Posted;
        receipt.PostedAtUtc ??= _clock.UtcNow;
        foreach (var variantId in variantIdsToRefresh)
        {
            await InventoryStockHelper.RefreshLegacyVariantStockAsync(_db, variantId, _localizer, ct).ConfigureAwait(false);
        }

        return Result.Ok();
    }

    private Result Cancel(GoodsReceipt receipt)
    {
        var canCancel = _workflow.CanCancel(receipt);
        if (!canCancel.Succeeded)
        {
            return Result.Fail(_localizer[canCancel.Error ?? "GoodsReceiptLifecycleUnsupportedAction"]);
        }

        receipt.Status = GoodsReceiptStatus.Cancelled;
        receipt.CancelledAtUtc ??= _clock.UtcNow;
        return Result.Ok();
    }

    private async Task RecordGoodsReceiptEvidenceOrSaveAsync(GoodsReceipt receipt, string action, CancellationToken ct)
    {
        if (_businessEventService is null)
        {
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return;
        }

        var now = _clock.UtcNow;
        var safeAction = string.IsNullOrWhiteSpace(action) ? receipt.Status.ToString().ToLowerInvariant() : action.Trim().ToLowerInvariant();
        var payload = $$"""
            {"goodsReceiptId":"{{receipt.Id}}","purchaseOrderId":"{{receipt.PurchaseOrderId}}","businessId":"{{receipt.BusinessId}}","supplierId":"{{receipt.SupplierId}}","warehouseId":"{{receipt.WarehouseId}}","status":"{{receipt.Status}}","receivedQuantity":{{receipt.Lines.Where(x => !x.IsDeleted).Sum(x => x.ReceivedQuantity)}},"acceptedQuantity":{{receipt.Lines.Where(x => !x.IsDeleted).Sum(x => x.AcceptedQuantity)}}}
            """;

        var eventResult = await _businessEventService.AddEventAsync(
                new AddBusinessEventCommand(
                    receipt.BusinessId,
                    "GoodsReceipt",
                    receipt.Id,
                    $"purchasing.goods_receipt.{safeAction}",
                    $"purchasing.goods_receipt.{safeAction}:{receipt.Id}",
                    now,
                    null,
                    BusinessEventSource.User,
                    BusinessEventSeverity.Info,
                    FoundationVisibility.Internal,
                    $"Goods receipt {receipt.Status}",
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

        var auditResult = await _businessEventService.AddAuditTrailAsync(
                new AddAuditTrailCommand(
                    receipt.BusinessId,
                    "GoodsReceipt",
                    receipt.Id,
                    receipt.Status == GoodsReceiptStatus.Draft ? AuditTrailAction.Created : AuditTrailAction.StatusChanged,
                    now,
                    null,
                    eventResult.Value,
                    $"Goods receipt {receipt.Status}",
                    null,
                    payload),
                ct)
            .ConfigureAwait(false);
        if (!auditResult.Succeeded)
        {
            throw new InvalidOperationException(auditResult.Error);
        }
    }
}
