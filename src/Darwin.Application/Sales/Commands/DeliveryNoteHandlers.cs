using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Foundation;
using Darwin.Application.Sales.DTOs;
using Darwin.Application.Sales.Services;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Sales;
using Darwin.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Sales.Commands;

public sealed class CreateDeliveryNoteFromShipmentHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<DeliveryNoteCreateFromShipmentDto> _validator;
    private readonly IStringLocalizer<ValidationResource> _localizer;
    private readonly IClock _clock;
    private readonly DeliveryNoteLifecycleEventService _events;

    public CreateDeliveryNoteFromShipmentHandler(
        IAppDbContext db,
        IValidator<DeliveryNoteCreateFromShipmentDto> validator,
        IStringLocalizer<ValidationResource> localizer,
        IClock clock,
        DeliveryNoteLifecycleEventService events)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public async Task<Guid> HandleAsync(DeliveryNoteCreateFromShipmentDto dto, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);

        var existingId = await _db.Set<DeliveryNote>()
            .AsNoTracking()
            .Where(x => x.ShipmentId == dto.ShipmentId && !x.IsDeleted)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        if (existingId != Guid.Empty)
        {
            throw new ValidationException(_localizer["DeliveryNoteAlreadyExistsForShipment"]);
        }

        var shipment = await _db.Set<Shipment>()
            .AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == dto.ShipmentId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (shipment is null)
        {
            throw new ValidationException(_localizer["ShipmentNotFound"]);
        }

        var shipmentLines = shipment.Lines.Where(x => !x.IsDeleted).OrderBy(x => x.CreatedAtUtc).ToList();
        if (shipmentLines.Count == 0)
        {
            throw new ValidationException(_localizer["DeliveryNoteRequiresShipmentLines"]);
        }

        var order = await _db.Set<Order>()
            .AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == shipment.OrderId && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (order is null)
        {
            throw new ValidationException(_localizer["OrderNotFound"]);
        }

        var orderLines = order.Lines.Where(x => !x.IsDeleted).ToDictionary(x => x.Id);
        var note = new DeliveryNote
        {
            Id = Guid.NewGuid(),
            BusinessId = order.BusinessId,
            CustomerId = order.CustomerId,
            OrderId = order.Id,
            ShipmentId = shipment.Id,
            Status = DeliveryNoteStatus.Draft,
            Currency = order.Currency,
            Carrier = NormalizeOptional(shipment.Carrier),
            Service = NormalizeOptional(shipment.Service),
            TrackingNumber = NormalizeOptional(shipment.TrackingNumber),
            ProviderShipmentReference = NormalizeOptional(shipment.ProviderShipmentReference),
            ShippingAddressJson = NormalizeJson(order.ShippingAddressJson),
            InternalNotes = NormalizeOptional(dto.InternalNotes),
            MetadataJson = "{}"
        };

        var sort = 0;
        foreach (var shipmentLine in shipmentLines)
        {
            if (!orderLines.TryGetValue(shipmentLine.OrderLineId, out var orderLine))
            {
                throw new ValidationException(_localizer["InvalidShipmentLineOrderMismatch"]);
            }

            if (shipmentLine.Quantity <= 0 || shipmentLine.Quantity > orderLine.Quantity)
            {
                throw new ValidationException(_localizer["DeliveryNoteInvalidShipmentQuantity"]);
            }

            var totalNet = shipmentLine.Quantity * orderLine.UnitPriceNetMinor;
            var totalGross = shipmentLine.Quantity * orderLine.UnitPriceGrossMinor;
            note.Lines.Add(new DeliveryNoteLine
            {
                Id = Guid.NewGuid(),
                OrderLineId = orderLine.Id,
                ProductVariantId = orderLine.VariantId,
                Name = orderLine.Name.Trim(),
                Sku = NormalizeOptional(orderLine.Sku),
                Quantity = shipmentLine.Quantity,
                UnitPriceNetMinor = orderLine.UnitPriceNetMinor,
                UnitPriceGrossMinor = orderLine.UnitPriceGrossMinor,
                TaxRate = orderLine.VatRate,
                TotalNetMinor = totalNet,
                TotalGrossMinor = totalGross,
                TotalTaxMinor = Math.Max(0, totalGross - totalNet),
                SortOrder = sort++
            });
        }

        note.TotalQuantity = note.Lines.Sum(x => x.Quantity);
        note.TotalNetMinor = note.Lines.Sum(x => x.TotalNetMinor);
        note.TotalTaxMinor = note.Lines.Sum(x => x.TotalTaxMinor);
        note.TotalGrossMinor = note.Lines.Sum(x => x.TotalGrossMinor);
        _db.Set<DeliveryNote>().Add(note);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        var eventResult = await _events.RecordCreatedAsync(note, _clock.UtcNow, ct).ConfigureAwait(false);
        if (!eventResult.Succeeded)
        {
            throw new ValidationException(eventResult.Error);
        }

        return note.Id;
    }

    private static string NormalizeJson(string? value) => string.IsNullOrWhiteSpace(value) ? "{}" : value.Trim();
    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class UpdateDeliveryNoteLifecycleHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<DeliveryNoteLifecycleDto> _validator;
    private readonly IStringLocalizer<ValidationResource> _localizer;
    private readonly IClock _clock;
    private readonly NumberSequenceService _numberSequence;
    private readonly DeliveryNoteWorkflowPolicy _policy;
    private readonly DeliveryNoteLifecycleEventService _events;

    public UpdateDeliveryNoteLifecycleHandler(
        IAppDbContext db,
        IValidator<DeliveryNoteLifecycleDto> validator,
        IStringLocalizer<ValidationResource> localizer,
        IClock clock,
        NumberSequenceService numberSequence,
        DeliveryNoteWorkflowPolicy policy,
        DeliveryNoteLifecycleEventService events)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _numberSequence = numberSequence ?? throw new ArgumentNullException(nameof(numberSequence));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public Task PrepareAsync(DeliveryNoteLifecycleDto dto, CancellationToken ct = default)
        => TransitionAsync(dto, DeliveryNoteStatus.Prepared, note =>
        {
            note.PreparedAtUtc = _clock.UtcNow;
            note.PreparedByUserId = NormalizeGuid(dto.ActorUserId);
        }, ct);

    public Task IssueAsync(DeliveryNoteLifecycleDto dto, CancellationToken ct = default)
        => TransitionAsync(dto, DeliveryNoteStatus.Issued, async note =>
        {
            if (string.IsNullOrWhiteSpace(note.DeliveryNoteNumber))
            {
                note.DeliveryNoteNumber = await ReserveDeliveryNoteNumberAsync(note.BusinessId, ct).ConfigureAwait(false);
            }

            note.IssuedAtUtc = _clock.UtcNow;
            note.IssuedByUserId = NormalizeGuid(dto.ActorUserId);
        }, ct);

    public Task MarkShippedAsync(DeliveryNoteLifecycleDto dto, CancellationToken ct = default)
        => TransitionAsync(dto, DeliveryNoteStatus.Shipped, note =>
        {
            note.ShippedAtUtc = _clock.UtcNow;
            note.ShippedByUserId = NormalizeGuid(dto.ActorUserId);
        }, ct);

    public Task MarkDeliveredAsync(DeliveryNoteLifecycleDto dto, CancellationToken ct = default)
        => TransitionAsync(dto, DeliveryNoteStatus.Delivered, note =>
        {
            note.DeliveredAtUtc = _clock.UtcNow;
            note.DeliveredByUserId = NormalizeGuid(dto.ActorUserId);
        }, ct);

    public Task CancelAsync(DeliveryNoteLifecycleDto dto, CancellationToken ct = default)
        => TransitionAsync(dto, DeliveryNoteStatus.Cancelled, note =>
        {
            note.CancelledAtUtc = _clock.UtcNow;
            note.CancelledByUserId = NormalizeGuid(dto.ActorUserId);
        }, ct);

    private async Task TransitionAsync(DeliveryNoteLifecycleDto dto, DeliveryNoteStatus target, Action<DeliveryNote> mutate, CancellationToken ct)
        => await TransitionAsync(dto, target, note =>
        {
            mutate(note);
            return Task.CompletedTask;
        }, ct).ConfigureAwait(false);

    private async Task TransitionAsync(DeliveryNoteLifecycleDto dto, DeliveryNoteStatus target, Func<DeliveryNote, Task> mutate, CancellationToken ct)
    {
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);
        var note = await _db.Set<DeliveryNote>()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted, ct)
            .ConfigureAwait(false);
        if (note is null)
        {
            throw new ValidationException(_localizer["DeliveryNoteNotFound"]);
        }

        SalesQuoteGuard.EnsureRowVersion(note.RowVersion, dto.RowVersion, _localizer);
        var from = note.Status;
        if (!_policy.CanTransition(from, target))
        {
            throw new ValidationException(_localizer["DeliveryNoteInvalidLifecycleTransition"]);
        }

        note.Status = target;
        await mutate(note).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        var eventResult = await _events.RecordStatusChangedAsync(note, from, target, _clock.UtcNow, ct).ConfigureAwait(false);
        if (!eventResult.Succeeded)
        {
            throw new ValidationException(eventResult.Error);
        }
    }

    private async Task<string> ReserveDeliveryNoteNumberAsync(Guid? businessId, CancellationToken ct)
    {
        var result = await _numberSequence
            .ReserveNextAsync(new NumberSequenceRequest(businessId, NumberSequenceDocumentType.DeliveryNote, NumberSequenceService.GlobalScopeKey), ct)
            .ConfigureAwait(false);
        if (!result.Succeeded && businessId.HasValue)
        {
            result = await _numberSequence
                .ReserveNextAsync(new NumberSequenceRequest(null, NumberSequenceDocumentType.DeliveryNote, NumberSequenceService.GlobalScopeKey), ct)
                .ConfigureAwait(false);
        }

        if (!result.Succeeded)
        {
            throw new ValidationException(_localizer["DeliveryNoteNumberSequenceRequired"]);
        }

        return result.Value!;
    }

    private static Guid? NormalizeGuid(Guid? value) => value.HasValue && value.Value != Guid.Empty ? value.Value : null;
}
