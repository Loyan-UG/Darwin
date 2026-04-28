using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
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
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public ApplyDhlShipmentLabelOperationHandler(
            IAppDbContext db,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _localizer = localizer;
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

            shipment.ProviderShipmentReference = string.IsNullOrWhiteSpace(shipment.ProviderShipmentReference)
                ? DhlShipmentPhaseOneMetadata.BuildProviderShipmentReference(shipment)
                : shipment.ProviderShipmentReference.Trim();

            shipment.TrackingNumber = string.IsNullOrWhiteSpace(shipment.TrackingNumber)
                ? DhlShipmentPhaseOneMetadata.BuildTrackingNumber(settings, shipment)
                : shipment.TrackingNumber.Trim();

            shipment.LabelUrl ??= DhlShipmentPhaseOneMetadata.BuildLabelUrl(settings.DhlApiBaseUrl!, shipment.ProviderShipmentReference);
            shipment.LastCarrierEventKey = "shipment.label_created";

            if (shipment.Status == ShipmentStatus.Pending)
            {
                shipment.Status = ShipmentStatus.Packed;
            }

            await ShipmentCarrierEventRecorder.AddIfMissingAsync(
                _db,
                shipment,
                "shipment.label_created",
                DateTime.UtcNow,
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
