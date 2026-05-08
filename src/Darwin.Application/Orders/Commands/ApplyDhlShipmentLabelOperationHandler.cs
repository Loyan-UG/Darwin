using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

namespace Darwin.Application.Orders.Commands
{
    /// <summary>
    /// Applies a queued DHL label-generation operation to an existing shipment.
    /// </summary>
    public sealed class ApplyDhlShipmentLabelOperationHandler
    {
        private readonly IAppDbContext _db;
        private readonly IClock _clock;
        private readonly IStringLocalizer<ValidationResource> _localizer;
        private readonly IDhlShipmentProviderClient _dhlClient;
        private readonly IShipmentLabelStorage _labelStorage;

        public ApplyDhlShipmentLabelOperationHandler(
            IAppDbContext db,
            IDhlShipmentProviderClient dhlClient,
            IShipmentLabelStorage labelStorage,
            IStringLocalizer<ValidationResource>? localizer = null,
            IClock? clock = null)
        {
            _db = db;
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

            if (string.IsNullOrWhiteSpace(shipment.ProviderShipmentReference))
            {
                throw new InvalidOperationException(_localizer["DhlLabelGenerationRequiresProviderShipmentReference"]);
            }

            shipment.ProviderShipmentReference = shipment.ProviderShipmentReference.Trim();
            var labelResult = await _dhlClient.GetLabelAsync(settings, shipment, ct).ConfigureAwait(false);
            if (labelResult.LabelPdfBytes is { Length: > 0 })
            {
                shipment.LabelUrl = await _labelStorage
                    .SaveLabelAsync(shipment.Id, "DHL", labelResult.LabelPdfBytes, "application/pdf", ct)
                    .ConfigureAwait(false);
            }
            else if (!string.IsNullOrWhiteSpace(labelResult.ProviderLabelUrl))
            {
                shipment.LabelUrl = labelResult.ProviderLabelUrl.Trim();
            }
            else
            {
                throw new InvalidOperationException(_localizer["DhlLabelGenerationResponseInvalid"]);
            }

            shipment.LastCarrierEventKey = "shipment.label_created";

            if (shipment.Status == ShipmentStatus.Pending)
            {
                shipment.Status = ShipmentStatus.Packed;
            }

            var nowUtc = _clock.UtcNow;
            await ShipmentCarrierEventRecorder.AddIfMissingAsync(
                _db,
                shipment,
                "shipment.label_created",
                nowUtc,
                "LabelCreated",
                ct: ct).ConfigureAwait(false);

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);

            return new ShipmentDetailDto
            {
                Id = shipment.Id,
                Carrier = shipment.Carrier ?? string.Empty,
                Service = shipment.Service ?? string.Empty,
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
}

