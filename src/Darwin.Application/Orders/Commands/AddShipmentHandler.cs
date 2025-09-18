using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Orders.DTOs;
using Darwin.Application.Orders.Validators;
using Darwin.Domain.Entities.Orders;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Orders.Commands
{
    /// <summary>
    /// Adds a shipment to an order. In phase 1, no inventory allocation is performed here.
    /// </summary>
    public sealed class AddShipmentHandler
    {
        private readonly IAppDbContext _db;
        private readonly ShipmentCreateValidator _validator = new();

        public AddShipmentHandler(IAppDbContext db) => _db = db;

        public async Task HandleAsync(ShipmentCreateDto dto, CancellationToken ct = default)
        {
            var v = _validator.Validate(dto);
            if (!v.IsValid) throw new ValidationException(v.Errors);

            var order = await _db.Set<Order>().Include(o => o.Lines)
                .FirstOrDefaultAsync(o => o.Id == dto.OrderId, ct);
            if (order is null) throw new InvalidOperationException("Order not found.");

            // Validate lines belong to this order
            var lineIds = order.Lines.Select(l => l.Id).ToHashSet();
            foreach (var sl in dto.Lines)
                if (!lineIds.Contains(sl.OrderLineId))
                    throw new ValidationException("Invalid shipment line: order line does not belong to the order.");

            var shipment = new Shipment
            {
                OrderId = order.Id,
                Carrier = dto.Carrier.Trim(),
                Service = dto.Service.Trim(),
                TrackingNumber = string.IsNullOrWhiteSpace(dto.TrackingNumber) ? null : dto.TrackingNumber.Trim(),
                TotalWeight = dto.TotalWeight,
                Status = Darwin.Domain.Enums.ShipmentStatus.Pending,
                Lines = dto.Lines.Select(l => new ShipmentLine
                {
                    OrderLineId = l.OrderLineId,
                    Quantity = l.Quantity
                }).ToList()
            };

            _db.Set<Shipment>().Add(shipment);
            await _db.SaveChangesAsync(ct);
        }
    }
}
