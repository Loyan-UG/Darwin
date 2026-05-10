using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Shipping;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Orders.DTOs;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Settings;
using Darwin.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Orders.Commands;

/// <summary>
/// Applies a queued DHL return-shipment creation operation to a return shipment row.
/// </summary>
public sealed class ApplyDhlReturnShipmentCreateOperationHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly IStringLocalizer<ValidationResource> _localizer;
    private readonly IDhlShipmentProviderClient _dhlClient;
    private readonly IShipmentLabelStorage _labelStorage;

    public ApplyDhlReturnShipmentCreateOperationHandler(
        IAppDbContext db,
        IDhlShipmentProviderClient dhlClient,
        IShipmentLabelStorage labelStorage,
        IStringLocalizer<ValidationResource>? localizer = null,
        IClock? clock = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _dhlClient = dhlClient ?? throw new ArgumentNullException(nameof(dhlClient));
        _labelStorage = labelStorage ?? throw new ArgumentNullException(nameof(labelStorage));
        _clock = clock ?? DefaultHandlerDependencies.DefaultClock;
        _localizer = localizer ?? DefaultHandlerDependencies.DefaultLocalizer;
    }

    public async Task<ShipmentDetailDto> HandleAsync(Guid shipmentId, CancellationToken ct = default)
    {
        var shipment = await _db.Set<Shipment>()
            .FirstOrDefaultAsync(x => x.Id == shipmentId && !x.IsDeleted, ct)
            .ConfigureAwait(false);

        if (shipment is null)
        {
            throw new InvalidOperationException(_localizer["ShipmentNotFoundForLabelGeneration"]);
        }

        if (!DhlShipmentPhaseOneMetadata.IsDhlCarrier(shipment.Carrier))
        {
            throw new ValidationException(_localizer["ShipmentCarrierMustBeDhlForLabelGeneration"]);
        }

        if (!string.Equals(shipment.LastCarrierEventKey, "return.provider_create_queued", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(shipment.LastCarrierEventKey, "return.provider_create_failed", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException(_localizer["DhlReturnShipmentOperationRequiresReturnShipment"]);
        }

        var settings = await _db.Set<SiteSetting>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (settings is null || !settings.DhlEnabled)
        {
            throw new InvalidOperationException(_localizer["DhlLabelGenerationNotEnabled"]);
        }

        if (!DhlShipmentPhaseOneMetadata.HasLabelGenerationReadiness(settings))
        {
            throw new InvalidOperationException(_localizer["DhlLabelGenerationNotConfigured"]);
        }

        var order = await _db.Set<Order>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == shipment.OrderId && !x.IsDeleted, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException(_localizer["OrderNotFound"]);

        var returnSender = DhlShipmentPhaseOneMetadata.ParseShippingAddress(order.ShippingAddressJson, _localizer);
        var providerResult = await _dhlClient.CreateReturnShipmentAsync(settings, order, shipment, returnSender, ct).ConfigureAwait(false);

        shipment.ProviderShipmentReference = providerResult.ProviderShipmentReference.Trim();
        shipment.TrackingNumber = string.IsNullOrWhiteSpace(providerResult.TrackingNumber)
            ? shipment.ProviderShipmentReference
            : providerResult.TrackingNumber.Trim();

        if (providerResult.LabelPdfBytes is { Length: > 0 })
        {
            shipment.LabelUrl = await _labelStorage
                .SaveLabelAsync(shipment.Id, "DHL", providerResult.LabelPdfBytes, "application/pdf", ct)
                .ConfigureAwait(false);
        }
        else if (!string.IsNullOrWhiteSpace(providerResult.ProviderLabelUrl))
        {
            shipment.LabelUrl = providerResult.ProviderLabelUrl.Trim();
        }

        if (string.IsNullOrWhiteSpace(shipment.LabelUrl))
        {
            throw new InvalidOperationException(_localizer["DhlLabelGenerationResponseInvalid"]);
        }

        shipment.Status = ShipmentStatus.Packed;
        shipment.LastCarrierEventKey = "return.label_created";

        var nowUtc = _clock.UtcNow;
        await ShipmentCarrierEventRecorder.AddIfMissingAsync(
            _db,
            shipment,
            "return.label_created",
            nowUtc,
            "ReturnLabelCreated",
            ct: ct).ConfigureAwait(false);

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        return new ShipmentDetailDto
        {
            Id = shipment.Id,
            Carrier = shipment.Carrier,
            Service = shipment.Service,
            ProviderShipmentReference = shipment.ProviderShipmentReference,
            TrackingNumber = shipment.TrackingNumber,
            TrackingUrl = Queries.ShipmentTrackingPresentation.ResolveTrackingUrl(shipment.Carrier, shipment.TrackingNumber),
            LabelUrl = shipment.LabelUrl,
            TotalWeight = shipment.TotalWeight,
            Status = shipment.Status,
            ShippedAtUtc = shipment.ShippedAtUtc,
            DeliveredAtUtc = shipment.DeliveredAtUtc,
            LastCarrierEventKey = shipment.LastCarrierEventKey
        };
    }
}
