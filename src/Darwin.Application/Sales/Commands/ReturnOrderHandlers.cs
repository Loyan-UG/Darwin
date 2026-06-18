using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Foundation;
using Darwin.Application.Inventory.Commands;
using Darwin.Application.Inventory.DTOs;
using Darwin.Application.Sales.DTOs;
using Darwin.Application.Sales.Services;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Sales;
using Darwin.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Sales.Commands;

public sealed class CreateReturnOrderHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<ReturnOrderCreateDto> _validator;
    private readonly IStringLocalizer<ValidationResource> _localizer;
    private readonly IClock _clock;
    private readonly ReturnOrderLifecycleEventService _events;

    public CreateReturnOrderHandler(
        IAppDbContext db,
        IValidator<ReturnOrderCreateDto> validator,
        IStringLocalizer<ValidationResource> localizer,
        IClock clock,
        ReturnOrderLifecycleEventService events)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public async Task<Guid> HandleAsync(ReturnOrderCreateDto dto, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);

        var order = await _db.Set<Order>()
            .AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == dto.OrderId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (order is null)
        {
            throw new ValidationException(_localizer["OrderNotFound"]);
        }

        Shipment? shipment = null;
        if (NormalizeGuid(dto.ShipmentId).HasValue)
        {
            shipment = await _db.Set<Shipment>()
                .AsNoTracking()
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == dto.ShipmentId && x.OrderId == order.Id && !x.IsDeleted, ct)
                .ConfigureAwait(false);
            if (shipment is null)
            {
                throw new ValidationException(_localizer["ShipmentNotFound"]);
            }
        }

        var orderLines = order.Lines.Where(x => !x.IsDeleted).ToDictionary(x => x.Id);
        var shipmentLines = shipment?.Lines.Where(x => !x.IsDeleted).ToDictionary(x => x.Id) ?? new Dictionary<Guid, ShipmentLine>();
        var returnOrder = new ReturnOrder
        {
            Id = Guid.NewGuid(),
            BusinessId = order.BusinessId,
            CustomerId = order.CustomerId,
            OrderId = order.Id,
            ShipmentId = shipment?.Id ?? NormalizeGuid(dto.ShipmentId),
            InvoiceId = NormalizeGuid(dto.InvoiceId),
            Status = ReturnOrderStatus.Requested,
            RequestedByUserId = NormalizeGuid(dto.ActorUserId),
            Currency = NormalizeCurrency(order.Currency),
            CustomerSnapshotJson = NormalizeJson(dto.CustomerSnapshotJson),
            ShippingAddressJson = NormalizeJson(string.IsNullOrWhiteSpace(dto.ShippingAddressJson) ? order.ShippingAddressJson : dto.ShippingAddressJson),
            InternalNotes = NormalizeOptional(dto.InternalNotes),
            MetadataJson = "{}"
        };

        var sort = 0;
        foreach (var lineDto in dto.Lines)
        {
            if (!orderLines.TryGetValue(lineDto.OrderLineId, out var orderLine))
            {
                throw new ValidationException(_localizer["InvalidReturnOrderLine"]);
            }

            ShipmentLine? shipmentLine = null;
            if (NormalizeGuid(lineDto.ShipmentLineId).HasValue)
            {
                if (!shipmentLines.TryGetValue(lineDto.ShipmentLineId!.Value, out shipmentLine) || shipmentLine.OrderLineId != orderLine.Id)
                {
                    throw new ValidationException(_localizer["InvalidShipmentLineOrderMismatch"]);
                }
            }

            var maxQuantity = shipmentLine?.Quantity ?? orderLine.Quantity;
            if (lineDto.RequestedQuantity <= 0 || lineDto.RequestedQuantity > maxQuantity)
            {
                throw new ValidationException(_localizer["ReturnOrderInvalidRequestedQuantity"]);
            }

            var totals = CalculateTotals(orderLine, lineDto.RequestedQuantity);
            returnOrder.Lines.Add(new ReturnOrderLine
            {
                Id = Guid.NewGuid(),
                OrderLineId = orderLine.Id,
                ShipmentLineId = shipmentLine?.Id,
                ProductVariantId = orderLine.VariantId,
                Name = orderLine.Name.Trim(),
                Sku = NormalizeOptional(orderLine.Sku),
                Description = null,
                RequestedQuantity = lineDto.RequestedQuantity,
                UnitPriceNetMinor = orderLine.UnitPriceNetMinor,
                UnitPriceGrossMinor = orderLine.UnitPriceGrossMinor,
                TaxRate = orderLine.VatRate,
                RequestedNetMinor = totals.Net,
                RequestedTaxMinor = totals.Tax,
                RequestedGrossMinor = totals.Gross,
                SortOrder = sort++
            });
        }

        RecalculateHeader(returnOrder);
        _db.Set<ReturnOrder>().Add(returnOrder);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        var eventResult = await _events.RecordCreatedAsync(returnOrder, _clock.UtcNow, ct).ConfigureAwait(false);
        if (!eventResult.Succeeded)
        {
            throw new ValidationException(eventResult.Error);
        }

        return returnOrder.Id;
    }

    private static (long Net, long Tax, long Gross) CalculateTotals(OrderLine orderLine, int quantity)
    {
        var net = quantity * orderLine.UnitPriceNetMinor;
        var gross = quantity * orderLine.UnitPriceGrossMinor;
        return (net, Math.Max(0, gross - net), gross);
    }

    internal static void RecalculateHeader(ReturnOrder order)
    {
        order.RequestedQuantity = order.Lines.Where(x => !x.IsDeleted).Sum(x => x.RequestedQuantity);
        order.ApprovedQuantity = order.Lines.Where(x => !x.IsDeleted).Sum(x => x.ApprovedQuantity);
        order.ReceivedQuantity = order.Lines.Where(x => !x.IsDeleted).Sum(x => x.ReceivedQuantity);
        order.AcceptedQuantity = order.Lines.Where(x => !x.IsDeleted).Sum(x => x.AcceptedQuantity);
        order.RejectedQuantity = order.Lines.Where(x => !x.IsDeleted).Sum(x => x.RejectedQuantity);
        order.ScrappedQuantity = order.Lines.Where(x => !x.IsDeleted).Sum(x => x.ScrappedQuantity);
        order.RestockQuantity = order.Lines.Where(x => !x.IsDeleted).Sum(x => x.RestockQuantity);
        order.RequestedGrossMinor = order.Lines.Where(x => !x.IsDeleted).Sum(x => x.RequestedGrossMinor);
        order.ApprovedGrossMinor = order.Lines.Where(x => !x.IsDeleted).Sum(x => x.ApprovedGrossMinor);
        order.AcceptedGrossMinor = order.Lines.Where(x => !x.IsDeleted).Sum(x => x.AcceptedGrossMinor);
        order.RefundEligibleGrossMinor = order.AcceptedGrossMinor;
    }

    internal static void ApplyApprovedTotals(ReturnOrderLine line, int quantity)
    {
        var net = quantity * line.UnitPriceNetMinor;
        var gross = quantity * line.UnitPriceGrossMinor;
        line.ApprovedQuantity = quantity;
        line.ApprovedNetMinor = net;
        line.ApprovedGrossMinor = gross;
        line.ApprovedTaxMinor = Math.Max(0, gross - net);
    }

    internal static void ApplyAcceptedTotals(ReturnOrderLine line, int quantity)
    {
        var net = quantity * line.UnitPriceNetMinor;
        var gross = quantity * line.UnitPriceGrossMinor;
        line.AcceptedQuantity = quantity;
        line.AcceptedNetMinor = net;
        line.AcceptedGrossMinor = gross;
        line.AcceptedTaxMinor = Math.Max(0, gross - net);
    }

    internal static Guid? NormalizeGuid(Guid? value) => value.HasValue && value.Value != Guid.Empty ? value.Value : null;
    internal static string NormalizeCurrency(string? value) => string.IsNullOrWhiteSpace(value) ? "EUR" : value.Trim().ToUpperInvariant();
    internal static string NormalizeJson(string? value) => string.IsNullOrWhiteSpace(value) ? "{}" : value.Trim();
    internal static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class UpdateReturnOrderLifecycleHandler
{
    private readonly IAppDbContext _db;
    private readonly IStringLocalizer<ValidationResource> _localizer;
    private readonly IClock _clock;
    private readonly NumberSequenceService _numberSequence;
    private readonly ReturnOrderWorkflowPolicy _policy;
    private readonly ReturnOrderLifecycleEventService _events;
    private readonly ProcessReturnReceiptHandler _returnReceipt;
    private readonly IValidator<ReturnOrderLifecycleDto> _lifecycleValidator;
    private readonly IValidator<ReturnOrderApproveDto> _approveValidator;
    private readonly IValidator<ReturnOrderQueueShipmentDto> _queueShipmentValidator;
    private readonly IValidator<ReturnOrderReceiveDto> _receiveValidator;
    private readonly IValidator<ReturnOrderInspectDto> _inspectValidator;
    private readonly IValidator<ReturnOrderLinkRefundDto> _linkRefundValidator;

    public UpdateReturnOrderLifecycleHandler(
        IAppDbContext db,
        IStringLocalizer<ValidationResource> localizer,
        IClock clock,
        NumberSequenceService numberSequence,
        ReturnOrderWorkflowPolicy policy,
        ReturnOrderLifecycleEventService events,
        ProcessReturnReceiptHandler returnReceipt,
        IValidator<ReturnOrderLifecycleDto> lifecycleValidator,
        IValidator<ReturnOrderApproveDto> approveValidator,
        IValidator<ReturnOrderQueueShipmentDto> queueShipmentValidator,
        IValidator<ReturnOrderReceiveDto> receiveValidator,
        IValidator<ReturnOrderInspectDto> inspectValidator,
        IValidator<ReturnOrderLinkRefundDto> linkRefundValidator)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _numberSequence = numberSequence ?? throw new ArgumentNullException(nameof(numberSequence));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _returnReceipt = returnReceipt ?? throw new ArgumentNullException(nameof(returnReceipt));
        _lifecycleValidator = lifecycleValidator ?? throw new ArgumentNullException(nameof(lifecycleValidator));
        _approveValidator = approveValidator ?? throw new ArgumentNullException(nameof(approveValidator));
        _queueShipmentValidator = queueShipmentValidator ?? throw new ArgumentNullException(nameof(queueShipmentValidator));
        _receiveValidator = receiveValidator ?? throw new ArgumentNullException(nameof(receiveValidator));
        _inspectValidator = inspectValidator ?? throw new ArgumentNullException(nameof(inspectValidator));
        _linkRefundValidator = linkRefundValidator ?? throw new ArgumentNullException(nameof(linkRefundValidator));
    }

    public async Task ApproveAsync(ReturnOrderApproveDto dto, CancellationToken ct = default)
    {
        await _approveValidator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        await TransitionAsync(dto, ReturnOrderStatus.Approved, async order =>
        {
            if (string.IsNullOrWhiteSpace(order.ReturnOrderNumber))
            {
                order.ReturnOrderNumber = await ReserveReturnOrderNumberAsync(order.BusinessId, ct).ConfigureAwait(false);
            }

            var quantities = dto.Lines.ToDictionary(x => x.LineId, x => x.Quantity);
            foreach (var line in order.Lines.Where(x => !x.IsDeleted))
            {
                var quantity = quantities.Count == 0 ? line.RequestedQuantity : quantities.GetValueOrDefault(line.Id);
                if (quantity < 0 || quantity > line.RequestedQuantity)
                {
                    throw new ValidationException(_localizer["ReturnOrderInvalidApprovedQuantity"]);
                }

                CreateReturnOrderHandler.ApplyApprovedTotals(line, quantity);
            }

            if (order.Lines.Where(x => !x.IsDeleted).Sum(x => x.ApprovedQuantity) <= 0)
            {
                throw new ValidationException(_localizer["ReturnOrderApprovalRequiresQuantity"]);
            }

            order.ApprovedAtUtc = _clock.UtcNow;
            order.ApprovedByUserId = CreateReturnOrderHandler.NormalizeGuid(dto.ActorUserId);
            CreateReturnOrderHandler.RecalculateHeader(order);
        }, ct).ConfigureAwait(false);
    }

    public Task RejectAsync(ReturnOrderLifecycleDto dto, CancellationToken ct = default)
        => TransitionValidatedAsync(dto, ReturnOrderStatus.Rejected, order =>
        {
            order.RejectedAtUtc = _clock.UtcNow;
            order.RejectedByUserId = CreateReturnOrderHandler.NormalizeGuid(dto.ActorUserId);
        }, ct);

    public async Task QueueReturnShipmentAsync(ReturnOrderQueueShipmentDto dto, CancellationToken ct = default)
    {
        await _queueShipmentValidator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        await TransitionAsync(dto, ReturnOrderStatus.ReturnShipmentQueued, order =>
        {
            var shipmentId = CreateReturnOrderHandler.NormalizeGuid(dto.ShipmentId);
            if (shipmentId.HasValue)
            {
                order.ShipmentId = shipmentId;
            }

            order.ReturnShipmentQueuedAtUtc = _clock.UtcNow;
            order.ReturnShipmentQueuedByUserId = CreateReturnOrderHandler.NormalizeGuid(dto.ActorUserId);
        }, ct).ConfigureAwait(false);
    }

    public async Task ReceiveAsync(ReturnOrderReceiveDto dto, CancellationToken ct = default)
    {
        await _receiveValidator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        await TransitionAsync(dto, ReturnOrderStatus.Received, order =>
        {
            var quantities = dto.Lines.ToDictionary(x => x.LineId, x => x.Quantity);
            foreach (var line in order.Lines.Where(x => !x.IsDeleted))
            {
                var quantity = quantities.Count == 0 ? line.ApprovedQuantity : quantities.GetValueOrDefault(line.Id);
                if (quantity < 0 || quantity > line.ApprovedQuantity)
                {
                    throw new ValidationException(_localizer["ReturnOrderInvalidReceivedQuantity"]);
                }

                line.ReceivedQuantity = quantity;
            }

            if (order.Lines.Where(x => !x.IsDeleted).Sum(x => x.ReceivedQuantity) <= 0)
            {
                throw new ValidationException(_localizer["ReturnOrderReceiveRequiresQuantity"]);
            }

            order.ReceivedAtUtc = _clock.UtcNow;
            order.ReceivedByUserId = CreateReturnOrderHandler.NormalizeGuid(dto.ActorUserId);
            CreateReturnOrderHandler.RecalculateHeader(order);
        }, ct).ConfigureAwait(false);
    }

    public async Task InspectAsync(ReturnOrderInspectDto dto, CancellationToken ct = default)
    {
        await _inspectValidator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        var restockRequests = new List<InventoryReturnReceiptDto>();
        await TransitionAsync(dto, ReturnOrderStatus.Inspected, order =>
        {
            var lines = dto.Lines.ToDictionary(x => x.LineId);
            foreach (var line in order.Lines.Where(x => !x.IsDeleted))
            {
                if (!lines.TryGetValue(line.Id, out var input))
                {
                    throw new ValidationException(_localizer["ReturnOrderInspectionRequiresAllLines"]);
                }

                var inspectedTotal = input.AcceptedQuantity + input.RejectedQuantity + input.ScrappedQuantity;
                if (inspectedTotal > line.ReceivedQuantity)
                {
                    throw new ValidationException(_localizer["ReturnOrderInvalidInspectionQuantity"]);
                }

                if (input.RestockQuantity > input.AcceptedQuantity)
                {
                    throw new ValidationException(_localizer["ReturnOrderInvalidRestockQuantity"]);
                }

                line.RejectedQuantity = input.RejectedQuantity;
                line.ScrappedQuantity = input.ScrappedQuantity;
                line.RestockQuantity = input.RestockQuantity;
                line.RestockWarehouseId = CreateReturnOrderHandler.NormalizeGuid(input.RestockWarehouseId);
                CreateReturnOrderHandler.ApplyAcceptedTotals(line, input.AcceptedQuantity);
                line.Disposition = ResolveDisposition(line);

                if (line.RestockQuantity > 0)
                {
                    if (!line.ProductVariantId.HasValue)
                    {
                        throw new ValidationException(_localizer["ReturnOrderRestockRequiresVariant"]);
                    }

                    restockRequests.Add(new InventoryReturnReceiptDto
                    {
                        VariantId = line.ProductVariantId.Value,
                        WarehouseId = line.RestockWarehouseId,
                        Quantity = line.RestockQuantity,
                        ReferenceId = order.Id,
                        Reason = "ReturnOrderRestock"
                    });
                }
            }

            if (order.Lines.Where(x => !x.IsDeleted).Sum(x => x.AcceptedQuantity + x.RejectedQuantity + x.ScrappedQuantity) <= 0)
            {
                throw new ValidationException(_localizer["ReturnOrderInspectionRequiresQuantity"]);
            }

            order.InspectedAtUtc = _clock.UtcNow;
            order.InspectedByUserId = CreateReturnOrderHandler.NormalizeGuid(dto.ActorUserId);
            CreateReturnOrderHandler.RecalculateHeader(order);
        }, ct).ConfigureAwait(false);

        foreach (var request in restockRequests)
        {
            await _returnReceipt.HandleAsync(request, ct).ConfigureAwait(false);
        }
    }

    public Task MarkRefundReadyAsync(ReturnOrderLifecycleDto dto, CancellationToken ct = default)
        => TransitionValidatedAsync(dto, ReturnOrderStatus.RefundReady, order =>
        {
            if (order.AcceptedQuantity <= 0 || order.RefundEligibleGrossMinor <= 0)
            {
                throw new ValidationException(_localizer["ReturnOrderRefundReadyRequiresAcceptedQuantity"]);
            }

            order.RefundReadyAtUtc = _clock.UtcNow;
            order.RefundReadyByUserId = CreateReturnOrderHandler.NormalizeGuid(dto.ActorUserId);
        }, ct);

    public async Task LinkRefundAsync(ReturnOrderLinkRefundDto dto, CancellationToken ct = default)
    {
        await _linkRefundValidator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        var order = await LoadAsync(dto.Id, ct).ConfigureAwait(false);
        SalesQuoteGuard.EnsureRowVersion(order.RowVersion, dto.RowVersion, _localizer);
        if (order.Status != ReturnOrderStatus.RefundReady && order.Status != ReturnOrderStatus.Refunded)
        {
            throw new ValidationException(_localizer["ReturnOrderRefundLinkRequiresRefundReady"]);
        }

        var refund = await _db.Set<Refund>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == dto.RefundId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (refund is null ||
            refund.Status != RefundStatus.Completed ||
            refund.OrderId != order.OrderId ||
            refund.AmountMinor <= 0 ||
            !string.Equals(CreateReturnOrderHandler.NormalizeCurrency(refund.Currency), CreateReturnOrderHandler.NormalizeCurrency(order.Currency), StringComparison.Ordinal))
        {
            throw new ValidationException(_localizer["ReturnOrderInvalidRefundLink"]);
        }

        var exists = await _db.Set<ReturnOrderRefundLink>()
            .AnyAsync(x => x.ReturnOrderId == order.Id && x.RefundId == refund.Id && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        var currentLinkedAmount = order.RefundLinks.Where(x => !x.IsDeleted).Sum(x => x.AmountMinor);
        var linkedAmount = currentLinkedAmount;
        if (!exists)
        {
            linkedAmount = currentLinkedAmount + refund.AmountMinor;
            if (linkedAmount > order.RefundEligibleGrossMinor)
            {
                throw new ValidationException(_localizer["ReturnOrderRefundLinkExceedsEligibility"]);
            }

            var link = new ReturnOrderRefundLink
            {
                Id = Guid.NewGuid(),
                ReturnOrderId = order.Id,
                RefundId = refund.Id,
                AmountMinor = refund.AmountMinor,
                Currency = CreateReturnOrderHandler.NormalizeCurrency(refund.Currency),
                Notes = CreateReturnOrderHandler.NormalizeOptional(dto.Notes),
                RowVersion = Array.Empty<byte>()
            };
            _db.Set<ReturnOrderRefundLink>().Add(link);
        }

        if (linkedAmount == order.RefundEligibleGrossMinor)
        {
            var from = order.Status;
            if (_policy.CanTransition(from, ReturnOrderStatus.Refunded))
            {
                order.Status = ReturnOrderStatus.Refunded;
                order.RefundedAtUtc = _clock.UtcNow;
                order.RefundedByUserId = CreateReturnOrderHandler.NormalizeGuid(dto.ActorUserId);
            }
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        if (order.Status == ReturnOrderStatus.Refunded)
        {
            var eventResult = await _events.RecordStatusChangedAsync(order, ReturnOrderStatus.RefundReady, ReturnOrderStatus.Refunded, _clock.UtcNow, ct).ConfigureAwait(false);
            if (!eventResult.Succeeded)
            {
                throw new ValidationException(eventResult.Error);
            }
        }
    }

    public Task CloseAsync(ReturnOrderLifecycleDto dto, CancellationToken ct = default)
        => TransitionValidatedAsync(dto, ReturnOrderStatus.Closed, order =>
        {
            order.ClosedAtUtc = _clock.UtcNow;
            order.ClosedByUserId = CreateReturnOrderHandler.NormalizeGuid(dto.ActorUserId);
        }, ct);

    public Task CancelAsync(ReturnOrderLifecycleDto dto, CancellationToken ct = default)
        => TransitionValidatedAsync(dto, ReturnOrderStatus.Cancelled, order =>
        {
            order.CancelledAtUtc = _clock.UtcNow;
            order.CancelledByUserId = CreateReturnOrderHandler.NormalizeGuid(dto.ActorUserId);
        }, ct);

    private async Task TransitionValidatedAsync(ReturnOrderLifecycleDto dto, ReturnOrderStatus target, Action<ReturnOrder> mutate, CancellationToken ct)
    {
        await _lifecycleValidator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        await TransitionAsync(dto, target, order =>
        {
            mutate(order);
            return Task.CompletedTask;
        }, ct).ConfigureAwait(false);
    }

    private async Task TransitionAsync(ReturnOrderLifecycleDto dto, ReturnOrderStatus target, Action<ReturnOrder> mutate, CancellationToken ct)
        => await TransitionAsync(dto, target, order =>
        {
            mutate(order);
            return Task.CompletedTask;
        }, ct).ConfigureAwait(false);

    private async Task TransitionAsync(ReturnOrderLifecycleDto dto, ReturnOrderStatus target, Func<ReturnOrder, Task> mutate, CancellationToken ct)
    {
        var order = await LoadAsync(dto.Id, ct).ConfigureAwait(false);
        SalesQuoteGuard.EnsureRowVersion(order.RowVersion, dto.RowVersion, _localizer);
        var from = order.Status;
        if (!_policy.CanTransition(from, target))
        {
            throw new ValidationException(_localizer["ReturnOrderInvalidLifecycleTransition"]);
        }

        order.Status = target;
        await mutate(order).ConfigureAwait(false);
        CreateReturnOrderHandler.RecalculateHeader(order);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        var eventResult = await _events.RecordStatusChangedAsync(order, from, target, _clock.UtcNow, ct).ConfigureAwait(false);
        if (!eventResult.Succeeded)
        {
            throw new ValidationException(eventResult.Error);
        }
    }

    private async Task<ReturnOrder> LoadAsync(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty)
        {
            throw new ValidationException(_localizer["ReturnOrderNotFound"]);
        }

        var order = await _db.Set<ReturnOrder>()
            .Include(x => x.Lines)
            .Include(x => x.RefundLinks)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        return order ?? throw new ValidationException(_localizer["ReturnOrderNotFound"]);
    }

    private async Task<string> ReserveReturnOrderNumberAsync(Guid? businessId, CancellationToken ct)
    {
        var result = await _numberSequence
            .ReserveNextAsync(new NumberSequenceRequest(businessId, NumberSequenceDocumentType.ReturnOrder, NumberSequenceService.GlobalScopeKey), ct)
            .ConfigureAwait(false);
        if (!result.Succeeded && businessId.HasValue)
        {
            result = await _numberSequence
                .ReserveNextAsync(new NumberSequenceRequest(null, NumberSequenceDocumentType.ReturnOrder, NumberSequenceService.GlobalScopeKey), ct)
                .ConfigureAwait(false);
        }

        if (!result.Succeeded)
        {
            throw new ValidationException(_localizer["ReturnOrderNumberSequenceRequired"]);
        }

        return result.Value!;
    }

    private static ReturnInspectionDisposition ResolveDisposition(ReturnOrderLine line)
    {
        var flags = 0;
        if (line.AcceptedQuantity > 0) flags++;
        if (line.RejectedQuantity > 0) flags++;
        if (line.ScrappedQuantity > 0) flags++;
        if (line.RestockQuantity > 0) flags++;
        if (flags > 1) return ReturnInspectionDisposition.Mixed;
        if (line.RestockQuantity > 0) return ReturnInspectionDisposition.Restock;
        if (line.ScrappedQuantity > 0) return ReturnInspectionDisposition.Scrap;
        if (line.RejectedQuantity > 0) return ReturnInspectionDisposition.Rejected;
        if (line.AcceptedQuantity > 0) return ReturnInspectionDisposition.AcceptedForRefund;
        return ReturnInspectionDisposition.NotInspected;
    }
}
