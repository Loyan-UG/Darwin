using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Orders.Commands;

internal static class ShipmentCarrierEventRecorder
{
    public static async Task AddIfMissingAsync(
        IAppDbContext db,
        Shipment shipment,
        string carrierEventKey,
        DateTime occurredAtUtc,
        string? providerStatus,
        string? exceptionCode = null,
        string? exceptionMessage = null,
        CancellationToken ct = default)
    {
        if (shipment.Id == Guid.Empty || string.IsNullOrWhiteSpace(carrierEventKey) || string.IsNullOrWhiteSpace(shipment.ProviderShipmentReference))
        {
            return;
        }

        var normalizedEventKey = carrierEventKey.Trim();
        var normalizedProviderStatus = string.IsNullOrWhiteSpace(providerStatus) ? null : providerStatus.Trim();
        var normalizedExceptionCode = string.IsNullOrWhiteSpace(exceptionCode) ? null : exceptionCode.Trim();
        var normalizedExceptionMessage = string.IsNullOrWhiteSpace(exceptionMessage) ? null : exceptionMessage.Trim();
        var normalizedReference = shipment.ProviderShipmentReference.Trim();

        var existing = await db.Set<ShipmentCarrierEvent>()
            .FirstOrDefaultAsync(
                x => x.ShipmentId == shipment.Id &&
                     !x.IsDeleted &&
                     x.ProviderShipmentReference == normalizedReference &&
                     x.CarrierEventKey == normalizedEventKey &&
                     x.OccurredAtUtc == occurredAtUtc &&
                     x.ProviderStatus == normalizedProviderStatus,
                ct)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            if (string.IsNullOrWhiteSpace(existing.ExceptionCode) && !string.IsNullOrWhiteSpace(normalizedExceptionCode))
            {
                existing.ExceptionCode = normalizedExceptionCode;
            }

            if (string.IsNullOrWhiteSpace(existing.ExceptionMessage) && !string.IsNullOrWhiteSpace(normalizedExceptionMessage))
            {
                existing.ExceptionMessage = normalizedExceptionMessage;
            }

            return;
        }

        db.Set<ShipmentCarrierEvent>().Add(new ShipmentCarrierEvent
        {
            ShipmentId = shipment.Id,
            Carrier = shipment.Carrier.Trim(),
            ProviderShipmentReference = normalizedReference,
            CarrierEventKey = normalizedEventKey,
            ProviderStatus = normalizedProviderStatus,
            ExceptionCode = normalizedExceptionCode,
            ExceptionMessage = normalizedExceptionMessage,
            TrackingNumber = string.IsNullOrWhiteSpace(shipment.TrackingNumber) ? null : shipment.TrackingNumber.Trim(),
            LabelUrl = string.IsNullOrWhiteSpace(shipment.LabelUrl) ? null : shipment.LabelUrl.Trim(),
            Service = string.IsNullOrWhiteSpace(shipment.Service) ? null : shipment.Service.Trim(),
            OccurredAtUtc = occurredAtUtc
        });
    }
}
