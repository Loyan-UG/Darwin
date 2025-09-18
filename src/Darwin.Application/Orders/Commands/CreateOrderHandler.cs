using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Orders.DTOs;
using Darwin.Application.Orders.Validators;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Orders.Commands
{
    /// <summary>
    /// Creates an order from provided lines and totals. Totals are computed server-side to ensure consistency.
    /// </summary>
    public sealed class CreateOrderHandler
    {
        private readonly IAppDbContext _db;
        private readonly OrderCreateValidator _validator = new();

        public CreateOrderHandler(IAppDbContext db) => _db = db;

        public async Task<Guid> HandleAsync(OrderCreateDto dto, CancellationToken ct = default)
        {
            var v = _validator.Validate(dto);
            if (!v.IsValid) throw new ValidationException(v.Errors);

            // Compute line totals (line-level tax calc)
            long subtotalNet = 0;
            long taxTotal = 0;
            var lines = dto.Lines.Select(l =>
            {
                var unitGross = l.UnitPriceNetMinor + (long)Math.Round(l.UnitPriceNetMinor * (double)l.VatRate, MidpointRounding.AwayFromZero);
                var lineNet = l.UnitPriceNetMinor * l.Quantity;
                var lineGross = unitGross * l.Quantity;
                var lineTax = lineGross - lineNet;

                subtotalNet += lineNet;
                taxTotal += lineTax;

                return new OrderLine
                {
                    VariantId = l.VariantId,
                    Name = l.Name.Trim(),
                    Sku = l.Sku.Trim(),
                    Quantity = l.Quantity,
                    UnitPriceNetMinor = l.UnitPriceNetMinor,
                    VatRate = l.VatRate,
                    UnitPriceGrossMinor = unitGross,
                    LineTaxMinor = lineTax,
                    LineGrossMinor = lineGross
                };
            }).ToList();

            var grand = subtotalNet + taxTotal + dto.ShippingTotalMinor - dto.DiscountTotalMinor;
            if (grand < 0) grand = 0;

            var order = new Order
            {
                OrderNumber = await NextOrderNumberAsync(ct),
                UserId = dto.UserId,
                Currency = dto.Currency,
                PricesIncludeTax = dto.PricesIncludeTax,
                SubtotalNetMinor = subtotalNet,
                TaxTotalMinor = taxTotal,
                ShippingTotalMinor = dto.ShippingTotalMinor,
                DiscountTotalMinor = dto.DiscountTotalMinor,
                GrandTotalGrossMinor = grand,
                Status = OrderStatus.Created,
                BillingAddressJson = dto.BillingAddressJson,
                ShippingAddressJson = dto.ShippingAddressJson,
                Lines = lines
            };

            _db.Set<Order>().Add(order);
            await _db.SaveChangesAsync(ct);
            return order.Id;
        }

        /// <summary>
        /// Generates a sequential, human-friendly order number (simple approach for phase 1).
        /// </summary>
        private async Task<string> NextOrderNumberAsync(CancellationToken ct)
        {
            // Simple approach: use count+1. Replace with a dedicated sequence table in the future to avoid race conditions.
            var lastCount = await _db.Set<Order>().AsNoTracking().CountAsync(ct);
            return $"D-{DateTime.UtcNow:yyyyMMdd}-{lastCount + 1:D5}";
        }
    }
}
