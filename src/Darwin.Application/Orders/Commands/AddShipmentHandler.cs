using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Orders.DTOs;
using Darwin.Domain.Entities.Integration;
using Darwin.Application.Orders.Validators;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Settings;
using FluentValidation;
using Microsoft.Extensions.Localization;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Orders.Commands
{
    /// <summary>
    /// Adds a shipment to an order. In phase 1, no inventory allocation is performed here.
    /// </summary>
    public sealed class AddShipmentHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<ShipmentCreateDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public AddShipmentHandler(
            IAppDbContext db,
            IValidator<ShipmentCreateDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _validator = validator;
            _localizer = localizer;
        }

        public async Task HandleAsync(ShipmentCreateDto dto, CancellationToken ct = default)
        {
            var v = _validator.Validate(dto);
            if (!v.IsValid) throw new ValidationException(v.Errors);

            var order = await _db.Set<Order>().Include(o => o.Lines)
                .FirstOrDefaultAsync(o => o.Id == dto.OrderId && !o.IsDeleted, ct);
            if (order is null) throw new InvalidOperationException(_localizer["OrderNotFound"]);

            // Validate lines belong to this order
            var lineIds = order.Lines.Where(l => !l.IsDeleted).Select(l => l.Id).ToHashSet();
            foreach (var sl in dto.Lines)
                if (!lineIds.Contains(sl.OrderLineId))
                    throw new ValidationException(_localizer["InvalidShipmentLineOrderMismatch"]);

            var shipment = new Shipment
            {
                OrderId = order.Id,
                Carrier = dto.Carrier.Trim(),
                Service = dto.Service.Trim(),
                ProviderShipmentReference = string.IsNullOrWhiteSpace(dto.ProviderShipmentReference) ? null : dto.ProviderShipmentReference.Trim(),
                TrackingNumber = string.IsNullOrWhiteSpace(dto.TrackingNumber) ? null : dto.TrackingNumber.Trim(),
                LabelUrl = string.IsNullOrWhiteSpace(dto.LabelUrl) ? null : dto.LabelUrl.Trim(),
                TotalWeight = dto.TotalWeight,
                Status = Darwin.Domain.Enums.ShipmentStatus.Pending,
                LastCarrierEventKey = string.IsNullOrWhiteSpace(dto.LastCarrierEventKey) ? null : dto.LastCarrierEventKey.Trim(),
                Lines = dto.Lines.Select(l => new ShipmentLine
                {
                    OrderLineId = l.OrderLineId,
                    Quantity = l.Quantity
                }).ToList()
            };

            if (DhlShipmentPhaseOneMetadata.IsDhlCarrier(shipment.Carrier))
            {
                var settings = await _db.Set<SiteSetting>()
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted)
                    .OrderBy(x => x.Id)
                    .FirstOrDefaultAsync(ct)
                    .ConfigureAwait(false);

                if (settings is not null &&
                    settings.DhlEnabled &&
                    DhlShipmentPhaseOneMetadata.HasLabelGenerationReadiness(settings) &&
                    string.IsNullOrWhiteSpace(shipment.ProviderShipmentReference) &&
                    string.IsNullOrWhiteSpace(shipment.TrackingNumber) &&
                    string.IsNullOrWhiteSpace(shipment.LabelUrl))
                {
                    shipment.LastCarrierEventKey = string.IsNullOrWhiteSpace(shipment.LastCarrierEventKey)
                        ? "shipment.provider_create_queued"
                        : shipment.LastCarrierEventKey.Trim();
                }
            }

            _db.Set<Shipment>().Add(shipment);
            await _db.SaveChangesAsync(ct);

            if (DhlShipmentPhaseOneMetadata.IsDhlCarrier(shipment.Carrier) &&
                string.Equals(shipment.LastCarrierEventKey, "shipment.provider_create_queued", StringComparison.OrdinalIgnoreCase))
            {
                _db.Set<ShipmentProviderOperation>().Add(new ShipmentProviderOperation
                {
                    ShipmentId = shipment.Id,
                    Provider = "DHL",
                    OperationType = "CreateShipment",
                    Status = "Pending"
                });

                await _db.SaveChangesAsync(ct);
            }
        }
    }
}
