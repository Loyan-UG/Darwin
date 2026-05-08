using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Orders.DTOs;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Orders.Commands;

public sealed class ApplyShipmentCarrierEventHandler
{
    private readonly IAppDbContext _db;
    private readonly IValidator<ApplyShipmentCarrierEventDto> _validator;
    private readonly IStringLocalizer<ValidationResource> _localizer;

    public ApplyShipmentCarrierEventHandler(
        IAppDbContext db,
        IValidator<ApplyShipmentCarrierEventDto> validator,
        IStringLocalizer<ValidationResource> localizer)
    {
        _db = db;
        _validator = validator;
        _localizer = localizer;
    }

    public async Task<ShipmentDetailDto> HandleAsync(ApplyShipmentCarrierEventDto dto, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(dto, ct).ConfigureAwait(false);

        var normalizedCarrier = dto.Carrier.Trim();
        var normalizedReference = dto.ProviderShipmentReference.Trim();

        var shipment = await _db.Set<Shipment>()
            .FirstOrDefaultAsync(
                x => x.ProviderShipmentReference == normalizedReference &&
                     !x.IsDeleted &&
                     x.Carrier == normalizedCarrier,
                ct)
            .ConfigureAwait(false);

        if (shipment is null)
        {
            throw new ValidationException(_localizer["ShipmentNotFoundForCarrierCallback"]);
        }

        var normalizedEventKey = dto.CarrierEventKey.Trim();
        var normalizedProviderStatus = string.IsNullOrWhiteSpace(dto.ProviderStatus) ? null : dto.ProviderStatus.Trim();
        var normalizedExceptionCode = string.IsNullOrWhiteSpace(dto.ExceptionCode) ? null : dto.ExceptionCode.Trim();
        var normalizedExceptionMessage = string.IsNullOrWhiteSpace(dto.ExceptionMessage) ? null : dto.ExceptionMessage.Trim();

        shipment.Carrier = normalizedCarrier;
        shipment.ProviderShipmentReference = normalizedReference;
        shipment.LastCarrierEventKey = normalizedEventKey;

        if (!string.IsNullOrWhiteSpace(dto.Service))
        {
            shipment.Service = dto.Service.Trim();
        }

        if (!string.IsNullOrWhiteSpace(dto.TrackingNumber))
        {
            shipment.TrackingNumber = dto.TrackingNumber.Trim();
        }

        if (!string.IsNullOrWhiteSpace(dto.LabelUrl))
        {
            shipment.LabelUrl = dto.LabelUrl.Trim();
        }

        var resolvedStatus = ResolveStatus(dto.ProviderStatus, dto.CarrierEventKey);
        if (resolvedStatus.HasValue && GetStage(resolvedStatus.Value) >= GetStage(shipment.Status))
        {
            shipment.Status = resolvedStatus.Value;
        }

        switch (shipment.Status)
        {
            case ShipmentStatus.Shipped:
                shipment.ShippedAtUtc ??= dto.OccurredAtUtc;
                break;
            case ShipmentStatus.Delivered:
                shipment.ShippedAtUtc ??= dto.OccurredAtUtc;
                shipment.DeliveredAtUtc ??= dto.OccurredAtUtc;
                break;
            case ShipmentStatus.Returned:
                shipment.ShippedAtUtc ??= dto.OccurredAtUtc;
                shipment.DeliveredAtUtc = null;
                break;
        }

        await ShipmentCarrierEventRecorder.AddIfMissingAsync(
            _db,
            shipment,
            normalizedEventKey,
            dto.OccurredAtUtc,
            normalizedProviderStatus,
            normalizedExceptionCode,
            normalizedExceptionMessage,
            ct).ConfigureAwait(false);

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

    private static ShipmentStatus? ResolveStatus(string? providerStatus, string carrierEventKey)
    {
        var normalized = $"{providerStatus ?? string.Empty} {carrierEventKey}".Trim().ToLowerInvariant();

        if (normalized.Contains("deliver"))
        {
            return ShipmentStatus.Delivered;
        }

        if (normalized.Contains("return"))
        {
            return ShipmentStatus.Returned;
        }

        if (normalized.Contains("out_for_delivery") ||
            normalized.Contains("in_transit") ||
            normalized.Contains("transit") ||
            normalized.Contains("shipment.picked_up") ||
            normalized.Contains("picked_up") ||
            normalized.Contains("handoff") ||
            normalized.Contains("shipped"))
        {
            return ShipmentStatus.Shipped;
        }

        if (normalized.Contains("label_created") ||
            normalized.Contains("manifested") ||
            normalized.Contains("packed") ||
            normalized.Contains("ready_for_pickup"))
        {
            return ShipmentStatus.Packed;
        }

        return null;
    }

    private static int GetStage(ShipmentStatus status)
        => status switch
        {
            ShipmentStatus.Pending => 0,
            ShipmentStatus.Packed => 1,
            ShipmentStatus.Shipped => 2,
            ShipmentStatus.Delivered => 3,
            ShipmentStatus.Returned => 4,
            _ => 0
        };
}
