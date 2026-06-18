using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Foundation;
using Darwin.Application.Inventory;
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

public sealed class CreateGoodsReceiptInlineIdentityHandler
{
    public const string LotIdentityType = "Lot";
    public const string SerialIdentityType = "Serial";
    public const string HandlingUnitIdentityType = "HandlingUnit";

    private readonly IAppDbContext _db;
    private readonly IStringLocalizer<ValidationResource> _localizer;
    private readonly IClock _clock;
    private readonly BusinessEventService? _businessEventService;

    public CreateGoodsReceiptInlineIdentityHandler(
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

    public CreateGoodsReceiptInlineIdentityHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        : this(db, localizer, InventoryManagementHandlerSupport.DefaultClock)
    {
    }

    public async Task<Result<Guid>> HandleAsync(GoodsReceiptInlineIdentityCreateDto dto, CancellationToken ct = default)
    {
        if (dto.GoodsReceiptId == Guid.Empty || dto.GoodsReceiptLineId == Guid.Empty)
        {
            return Result<Guid>.Fail(_localizer["GoodsReceiptNotFound"]);
        }

        if (dto.RowVersion is null || dto.RowVersion.Length == 0)
        {
            return Result<Guid>.Fail(_localizer["RowVersionRequired"]);
        }

        if (!string.IsNullOrWhiteSpace(dto.MetadataJson) && FoundationInputNormalizer.LooksSensitive(dto.MetadataJson))
        {
            return Result<Guid>.Fail(_localizer["GoodsReceiptIdentitySensitiveMetadataRejected"]);
        }

        var receipt = await _db.Set<GoodsReceipt>()
            .Include(x => x.Lines)
                .ThenInclude(x => x.Identities)
            .FirstOrDefaultAsync(x => x.Id == dto.GoodsReceiptId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (receipt is null)
        {
            return Result<Guid>.Fail(_localizer["GoodsReceiptNotFound"]);
        }

        if (!receipt.RowVersion.SequenceEqual(dto.RowVersion))
        {
            return Result<Guid>.Fail(_localizer["ItemConcurrencyConflict"]);
        }

        if (receipt.Status != GoodsReceiptStatus.Received)
        {
            return Result<Guid>.Fail(_localizer["GoodsReceiptLifecycleUnsupportedAction"]);
        }

        var line = receipt.Lines.FirstOrDefault(x => x.Id == dto.GoodsReceiptLineId && !x.IsDeleted);
        if (line is null)
        {
            return Result<Guid>.Fail(_localizer["GoodsReceiptLinesRequired"]);
        }

        var identityType = (dto.IdentityType ?? string.Empty).Trim();
        var normalizedMetadata = InventoryManagementHandlerSupport.NormalizeMetadataJson(dto.MetadataJson);
        var quantity = dto.Quantity <= 0 ? 1 : dto.Quantity;

        if (!string.Equals(identityType, LotIdentityType, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(identityType, SerialIdentityType, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(identityType, HandlingUnitIdentityType, StringComparison.OrdinalIgnoreCase))
        {
            return Result<Guid>.Fail(_localizer["GoodsReceiptIdentityTypeRequired"]);
        }

        try
        {
            var identity = string.Equals(identityType, LotIdentityType, StringComparison.OrdinalIgnoreCase)
                ? await CreateLotIdentityAsync(receipt, line, dto, quantity, normalizedMetadata, ct).ConfigureAwait(false)
                : string.Equals(identityType, SerialIdentityType, StringComparison.OrdinalIgnoreCase)
                    ? await CreateSerialIdentityAsync(receipt, line, dto, normalizedMetadata, ct).ConfigureAwait(false)
                    : await CreateHandlingUnitIdentityAsync(receipt, line, dto, quantity, normalizedMetadata, ct).ConfigureAwait(false);

            line.Identities.Add(identity);
            await RecordInlineIdentityEvidenceOrSaveAsync(receipt, line, identity, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(identity.Id);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<Guid>.Fail(_localizer["ItemConcurrencyConflict"]);
        }
        catch (InvalidOperationException ex)
        {
            return Result<Guid>.Fail(ex.Message);
        }
    }

    private async Task<GoodsReceiptLineIdentity> CreateLotIdentityAsync(GoodsReceipt receipt, GoodsReceiptLine line, GoodsReceiptInlineIdentityCreateDto dto, int quantity, string? metadataJson, CancellationToken ct)
    {
        var lotCode = InventoryManagementHandlerSupport.NormalizeRequiredCode(dto.LotCode ?? string.Empty);
        if (string.IsNullOrWhiteSpace(lotCode))
        {
            throw new InvalidOperationException(_localizer["InventoryLotNotFound"]);
        }

        var exists = await _db.Set<InventoryLot>()
            .AnyAsync(x => x.BusinessId == receipt.BusinessId && x.ProductVariantId == line.ProductVariantId && x.LotCode == lotCode && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (exists)
        {
            throw new InvalidOperationException(_localizer["InventoryLotAlreadyExists"]);
        }

        var lot = new InventoryLot
        {
            BusinessId = receipt.BusinessId,
            ProductVariantId = line.ProductVariantId,
            LotCode = lotCode,
            SupplierLotCode = InventoryManagementHandlerSupport.NormalizeOptional(dto.SupplierLotCode),
            ExpiryDateUtc = dto.ExpiryDateUtc,
            Status = InventoryLotStatus.Active,
            MetadataJson = metadataJson
        };
        _db.Set<InventoryLot>().Add(lot);

        return new GoodsReceiptLineIdentity
        {
            GoodsReceiptLineId = line.Id,
            ProductVariantId = line.ProductVariantId,
            InventoryLotId = lot.Id,
            Quantity = quantity,
            LotCodeSnapshot = lot.LotCode,
            SupplierLotCodeSnapshot = lot.SupplierLotCode,
            ExpiryDateUtc = lot.ExpiryDateUtc,
            SortOrder = line.Identities.Count + 1,
            MetadataJson = metadataJson
        };
    }

    private async Task<GoodsReceiptLineIdentity> CreateSerialIdentityAsync(GoodsReceipt receipt, GoodsReceiptLine line, GoodsReceiptInlineIdentityCreateDto dto, string? metadataJson, CancellationToken ct)
    {
        var serialNumber = InventoryManagementHandlerSupport.NormalizeRequiredCode(dto.SerialNumber ?? string.Empty);
        if (string.IsNullOrWhiteSpace(serialNumber))
        {
            throw new InvalidOperationException(_localizer["InventorySerialUnitNotFound"]);
        }

        InventoryLot? lot = null;
        if (dto.InventoryLotId.HasValue)
        {
            lot = await _db.Set<InventoryLot>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == dto.InventoryLotId.Value && !x.IsDeleted, ct)
                .ConfigureAwait(false);
            if (lot is null || lot.BusinessId != receipt.BusinessId || lot.ProductVariantId != line.ProductVariantId)
            {
                throw new InvalidOperationException(_localizer["InventoryLotNotFound"]);
            }
        }

        var exists = await _db.Set<InventorySerialUnit>()
            .AnyAsync(x => x.BusinessId == receipt.BusinessId && x.ProductVariantId == line.ProductVariantId && x.SerialNumber == serialNumber && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (exists)
        {
            throw new InvalidOperationException(_localizer["InventorySerialUnitAlreadyExists"]);
        }

        var serial = new InventorySerialUnit
        {
            BusinessId = receipt.BusinessId,
            ProductVariantId = line.ProductVariantId,
            InventoryLotId = dto.InventoryLotId,
            SerialNumber = serialNumber,
            ExpiryDateUtc = dto.ExpiryDateUtc ?? lot?.ExpiryDateUtc,
            Status = InventorySerialUnitStatus.Received,
            MetadataJson = metadataJson
        };
        _db.Set<InventorySerialUnit>().Add(serial);

        return new GoodsReceiptLineIdentity
        {
            GoodsReceiptLineId = line.Id,
            ProductVariantId = line.ProductVariantId,
            InventoryLotId = dto.InventoryLotId,
            InventorySerialUnitId = serial.Id,
            Quantity = 1,
            LotCodeSnapshot = lot?.LotCode,
            SupplierLotCodeSnapshot = lot?.SupplierLotCode,
            SerialNumberSnapshot = serial.SerialNumber,
            ExpiryDateUtc = serial.ExpiryDateUtc,
            SortOrder = line.Identities.Count + 1,
            MetadataJson = metadataJson
        };
    }

    private async Task<GoodsReceiptLineIdentity> CreateHandlingUnitIdentityAsync(GoodsReceipt receipt, GoodsReceiptLine line, GoodsReceiptInlineIdentityCreateDto dto, int quantity, string? metadataJson, CancellationToken ct)
    {
        var code = InventoryManagementHandlerSupport.NormalizeRequiredCode(dto.HandlingUnitCode ?? string.Empty);
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException(_localizer["HandlingUnitNotFound"]);
        }

        var exists = await _db.Set<HandlingUnit>()
            .AnyAsync(x => x.BusinessId == receipt.BusinessId && x.Code == code && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (exists)
        {
            throw new InvalidOperationException(_localizer["HandlingUnitAlreadyExists"]);
        }

        var handlingUnit = new HandlingUnit
        {
            BusinessId = receipt.BusinessId,
            WarehouseId = receipt.WarehouseId,
            Code = code,
            DisplayName = InventoryManagementHandlerSupport.NormalizeOptional(dto.HandlingUnitDisplayName) ?? code,
            HandlingUnitType = HandlingUnitType.Pallet,
            Status = HandlingUnitStatus.Open,
            MetadataJson = metadataJson
        };
        _db.Set<HandlingUnit>().Add(handlingUnit);

        return new GoodsReceiptLineIdentity
        {
            GoodsReceiptLineId = line.Id,
            ProductVariantId = line.ProductVariantId,
            HandlingUnitId = handlingUnit.Id,
            Quantity = quantity,
            HandlingUnitCodeSnapshot = handlingUnit.Code,
            SortOrder = line.Identities.Count + 1,
            MetadataJson = metadataJson
        };
    }

    private async Task RecordInlineIdentityEvidenceOrSaveAsync(GoodsReceipt receipt, GoodsReceiptLine line, GoodsReceiptLineIdentity identity, CancellationToken ct)
    {
        if (_businessEventService is null)
        {
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return;
        }

        var payload = $$"""
            {"goodsReceiptId":"{{receipt.Id}}","goodsReceiptLineId":"{{line.Id}}","businessId":"{{receipt.BusinessId}}","warehouseId":"{{receipt.WarehouseId}}","productVariantId":"{{line.ProductVariantId}}","identityId":"{{identity.Id}}","quantity":{{identity.Quantity}}}
            """;
        var eventResult = await _businessEventService.AddEventAsync(
                new AddBusinessEventCommand(
                    receipt.BusinessId,
                    "GoodsReceiptLineIdentity",
                    identity.Id,
                    "inventory.goods_receipt.identity_created",
                    $"inventory.goods_receipt.identity_created:{identity.Id}",
                    _clock.UtcNow,
                    null,
                    BusinessEventSource.User,
                    BusinessEventSeverity.Info,
                    FoundationVisibility.Internal,
                    "Goods receipt identity created",
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
                new AddAuditTrailCommand(receipt.BusinessId, "GoodsReceiptLineIdentity", identity.Id, AuditTrailAction.Created, _clock.UtcNow, null, eventResult.Value, "Goods receipt identity created", null, payload),
                ct)
            .ConfigureAwait(false);
        if (!auditResult.Succeeded)
        {
            throw new InvalidOperationException(auditResult.Error);
        }
    }
}

public sealed class UpdateGoodsReceiptLifecycleHandler
{
    public const string ReceiveAction = "Receive";
    public const string InspectAction = "Inspect";
    public const string PostAction = "Post";
    public const string CancelAction = "Cancel";
    public const string PostedReason = InventoryMovementReferencePolicy.GoodsReceiptPosted;

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
                .ThenInclude(x => x.Identities)
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

    private async Task<Result> InspectAsync(GoodsReceipt receipt, IReadOnlyList<GoodsReceiptLineDto> lines, CancellationToken ct)
    {
        var canInspect = _workflow.CanInspect(receipt);
        if (!canInspect.Succeeded)
        {
            return Result.Fail(_localizer[canInspect.Error ?? "GoodsReceiptLifecycleUnsupportedAction"]);
        }

        if (lines.Count == 0)
        {
            return Result.Fail(_localizer["GoodsReceiptLinesRequired"]);
        }

        foreach (var line in receipt.Lines.Where(x => !x.IsDeleted))
        {
            var input = lines.FirstOrDefault(x => x.Id == line.Id || x.PurchaseOrderLineId == line.PurchaseOrderLineId);
            if (input is null)
            {
                return Result.Fail(_localizer["GoodsReceiptLinesRequired"]);
            }

            if (input.AcceptedQuantity < 0 || input.RejectedQuantity < 0 || input.DamagedQuantity < 0)
            {
                return Result.Fail(_localizer["GoodsReceiptInvalidQuantity"]);
            }

            var inspectedTotal = input.AcceptedQuantity + input.RejectedQuantity + input.DamagedQuantity;
            if (inspectedTotal != line.ReceivedQuantity)
            {
                return Result.Fail(_localizer["GoodsReceiptInvalidQuantity"]);
            }

            line.AcceptedQuantity = input.AcceptedQuantity;
            line.RejectedQuantity = input.RejectedQuantity;
            line.DamagedQuantity = input.DamagedQuantity;
            var identityResult = await ReplaceLineIdentitiesAsync(receipt.BusinessId, line, input.Identities, ct).ConfigureAwait(false);
            if (!identityResult.Succeeded)
            {
                return identityResult;
            }
        }

        receipt.Status = GoodsReceiptStatus.Inspected;
        receipt.InspectedAtUtc ??= _clock.UtcNow;
        return Result.Ok();
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

        var purchaseOrder = await _db.Set<PurchaseOrder>()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == receipt.PurchaseOrderId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (purchaseOrder is null)
        {
            return Result.Fail(_localizer["PurchaseOrderNotFound"]);
        }

        var identityValidation = await ValidateReceiptIdentityEvidenceAsync(receipt, ct).ConfigureAwait(false);
        if (!identityValidation.Succeeded)
        {
            return identityValidation;
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

            var alreadyPosted = await InventoryMovementReferencePolicy.ExistsAsync(
                    _db,
                    receipt.Id,
                    PostedReason,
                    receipt.WarehouseId,
                    line.ProductVariantId,
                    ct)
                .ConfigureAwait(false);
            if (alreadyPosted)
            {
                variantIdsToRefresh.Add(line.ProductVariantId);
                continue;
            }

            var stockLevel = await InventoryStockHelper.GetOrCreateStockLevelAsync(_db, receipt.WarehouseId, line.ProductVariantId, ct).ConfigureAwait(false);
            stockLevel.AvailableQuantity += line.AcceptedQuantity;
            purchaseLine.ReceivedQuantity += line.AcceptedQuantity;
            InventoryMovementReferencePolicy.AddLedgerRow(_db, receipt.WarehouseId, line.ProductVariantId, line.AcceptedQuantity, PostedReason, receipt.Id);
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

    private async Task<Result> ReplaceLineIdentitiesAsync(Guid businessId, GoodsReceiptLine line, IReadOnlyList<GoodsReceiptLineIdentityDto> inputs, CancellationToken ct)
    {
        var activeInputs = inputs
            .Where(x => !IsBlankIdentityInput(x))
            .ToList();

        foreach (var input in activeInputs)
        {
            if (!string.IsNullOrWhiteSpace(input.MetadataJson) && FoundationInputNormalizer.LooksSensitive(input.MetadataJson))
            {
                return Result.Fail(_localizer["GoodsReceiptIdentitySensitiveMetadataRejected"]);
            }

            if (input.Quantity <= 0)
            {
                return Result.Fail(_localizer["GoodsReceiptInvalidIdentityQuantity"]);
            }
        }

        if (line.Identities.Count > 0)
        {
            _db.Set<GoodsReceiptLineIdentity>().RemoveRange(line.Identities);
            line.Identities.Clear();
        }

        var index = 0;
        foreach (var input in activeInputs)
        {
            if (input.ProductVariantId != Guid.Empty && input.ProductVariantId != line.ProductVariantId)
            {
                return Result.Fail(_localizer["GoodsReceiptIdentityProductMismatch"]);
            }

            var identity = new GoodsReceiptLineIdentity
            {
                GoodsReceiptLineId = line.Id,
                ProductVariantId = line.ProductVariantId,
                InventoryLotId = input.InventoryLotId,
                InventorySerialUnitId = input.InventorySerialUnitId,
                HandlingUnitId = input.HandlingUnitId,
                Quantity = input.Quantity,
                SortOrder = input.SortOrder == 0 ? ++index : input.SortOrder,
                MetadataJson = InventoryManagementHandlerSupport.NormalizeMetadataJson(input.MetadataJson)
            };

            var snapshotResult = await PopulateIdentitySnapshotsAsync(businessId, line.ProductVariantId, identity, ct).ConfigureAwait(false);
            if (!snapshotResult.Succeeded)
            {
                return snapshotResult;
            }

            line.Identities.Add(identity);
        }

        return Result.Ok();
    }

    private static bool IsBlankIdentityInput(GoodsReceiptLineIdentityDto input)
        => input.InventoryLotId is null
           && input.InventorySerialUnitId is null
           && input.HandlingUnitId is null
           && input.Quantity <= 0
           && string.IsNullOrWhiteSpace(input.MetadataJson);

    private async Task<Result> PopulateIdentitySnapshotsAsync(Guid businessId, Guid productVariantId, GoodsReceiptLineIdentity identity, CancellationToken ct)
    {
        if (identity.InventoryLotId.HasValue)
        {
            var lot = await _db.Set<InventoryLot>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == identity.InventoryLotId.Value && !x.IsDeleted, ct)
                .ConfigureAwait(false);
            if (lot is null || lot.BusinessId != businessId || lot.ProductVariantId != productVariantId)
            {
                return Result.Fail(_localizer["InventoryLotNotFound"]);
            }

            identity.LotCodeSnapshot = lot.LotCode;
            identity.SupplierLotCodeSnapshot = lot.SupplierLotCode;
            identity.ExpiryDateUtc = lot.ExpiryDateUtc;
        }

        if (identity.InventorySerialUnitId.HasValue)
        {
            var serial = await _db.Set<InventorySerialUnit>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == identity.InventorySerialUnitId.Value && !x.IsDeleted, ct)
                .ConfigureAwait(false);
            if (serial is null || serial.BusinessId != businessId || serial.ProductVariantId != productVariantId)
            {
                return Result.Fail(_localizer["InventorySerialUnitNotFound"]);
            }

            if (identity.InventoryLotId.HasValue && serial.InventoryLotId != identity.InventoryLotId)
            {
                return Result.Fail(_localizer["InventorySerialUnitNotFound"]);
            }

            if (identity.Quantity != 1)
            {
                return Result.Fail(_localizer["GoodsReceiptSerialQuantityMustBeOne"]);
            }

            identity.SerialNumberSnapshot = serial.SerialNumber;
            identity.ExpiryDateUtc ??= serial.ExpiryDateUtc;
        }

        if (identity.HandlingUnitId.HasValue)
        {
            var handlingUnit = await _db.Set<HandlingUnit>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == identity.HandlingUnitId.Value && !x.IsDeleted, ct)
                .ConfigureAwait(false);
            if (handlingUnit is null || handlingUnit.BusinessId != businessId)
            {
                return Result.Fail(_localizer["HandlingUnitNotFound"]);
            }

            identity.HandlingUnitCodeSnapshot = handlingUnit.Code;
        }

        return Result.Ok();
    }

    private async Task<Result> ValidateReceiptIdentityEvidenceAsync(GoodsReceipt receipt, CancellationToken ct)
    {
        var policies = await _db.Set<ProductTrackingPolicy>()
            .AsNoTracking()
            .Where(x => x.BusinessId == receipt.BusinessId && x.Status == ProductTrackingPolicyStatus.Active && !x.IsDeleted)
            .ToDictionaryAsync(x => x.ProductVariantId, ct)
            .ConfigureAwait(false);

        foreach (var line in receipt.Lines.Where(x => !x.IsDeleted && x.AcceptedQuantity > 0))
        {
            policies.TryGetValue(line.ProductVariantId, out var policy);
            if (policy is null || policy.TrackingMode == ProductTrackingMode.Untracked)
            {
                continue;
            }

            var identities = line.Identities.Where(x => !x.IsDeleted).ToList();
            if (identities.Count == 0)
            {
                return Result.Fail(_localizer["GoodsReceiptIdentityRequired"]);
            }

            foreach (var identity in identities)
            {
                if (identity.ProductVariantId != line.ProductVariantId || identity.Quantity <= 0)
                {
                    return Result.Fail(_localizer["GoodsReceiptInvalidIdentityQuantity"]);
                }
            }

            if (policy.TrackingMode is ProductTrackingMode.LotTracked or ProductTrackingMode.LotAndExpiryTracked)
            {
                var lotQuantity = identities.Where(x => x.InventoryLotId.HasValue).Sum(x => x.Quantity);
                if (lotQuantity != line.AcceptedQuantity)
                {
                    return Result.Fail(_localizer["GoodsReceiptLotIdentityRequired"]);
                }
            }

            if (policy.TrackingMode is ProductTrackingMode.SerialTracked or ProductTrackingMode.SerialAndExpiryTracked)
            {
                var serials = identities.Where(x => x.InventorySerialUnitId.HasValue).ToList();
                if (serials.Count != line.AcceptedQuantity || serials.Any(x => x.Quantity != 1))
                {
                    return Result.Fail(_localizer["GoodsReceiptSerialIdentityRequired"]);
                }
            }

            if (policy.RequiresExpiryDate && identities.Any(x => !x.ExpiryDateUtc.HasValue))
            {
                return Result.Fail(_localizer["GoodsReceiptExpiryIdentityRequired"]);
            }

            if (policy.RequiresSupplierLot && identities.Any(x => string.IsNullOrWhiteSpace(x.SupplierLotCodeSnapshot)))
            {
                return Result.Fail(_localizer["GoodsReceiptSupplierLotIdentityRequired"]);
            }

            if (policy.RequiresHandlingUnit)
            {
                var handlingUnitQuantity = identities.Where(x => x.HandlingUnitId.HasValue).Sum(x => x.Quantity);
                if (handlingUnitQuantity != line.AcceptedQuantity)
                {
                    return Result.Fail(_localizer["GoodsReceiptHandlingUnitIdentityRequired"]);
                }
            }
        }

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
