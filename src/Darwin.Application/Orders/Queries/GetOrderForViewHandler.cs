using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Orders.DTOs;
using Darwin.Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Orders.Queries
{
    /// <summary>
    /// Returns a full order detail for Admin view (lines, payments, shipments).
    /// </summary>
    public sealed class GetOrderForViewHandler
    {
        private readonly IAppDbContext _db;
        public GetOrderForViewHandler(IAppDbContext db) => _db = db;

        public async Task<OrderDetailDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            var o = await _db.Set<Order>()
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new OrderDetailDto
                {
                    Id = x.Id,
                    OrderNumber = x.OrderNumber,
                    UserId = x.UserId,
                    Currency = x.Currency,
                    PricesIncludeTax = x.PricesIncludeTax,
                    SubtotalNetMinor = x.SubtotalNetMinor,
                    TaxTotalMinor = x.TaxTotalMinor,
                    ShippingTotalMinor = x.ShippingTotalMinor,
                    DiscountTotalMinor = x.DiscountTotalMinor,
                    GrandTotalGrossMinor = x.GrandTotalGrossMinor,
                    Status = x.Status,
                    BillingAddressJson = x.BillingAddressJson,
                    ShippingAddressJson = x.ShippingAddressJson,
                    RowVersion = x.RowVersion,
                    Lines = x.Lines.Select(l => new OrderLineDetailDto
                    {
                        Id = l.Id,
                        VariantId = l.VariantId,
                        Name = l.Name,
                        Sku = l.Sku,
                        Quantity = l.Quantity,
                        UnitPriceNetMinor = l.UnitPriceNetMinor,
                        VatRate = l.VatRate,
                        UnitPriceGrossMinor = l.UnitPriceGrossMinor,
                        LineTaxMinor = l.LineTaxMinor,
                        LineGrossMinor = l.LineGrossMinor
                    }).ToList(),
                    Payments = x.Payments.Select(p => new PaymentDetailDto
                    {
                        Id = p.Id,
                        Provider = p.Provider,
                        ProviderReference = p.ProviderReference,
                        AmountMinor = p.AmountMinor,
                        Currency = p.Currency,
                        Status = p.Status,
                        CapturedAtUtc = p.CapturedAtUtc,
                        FailureReason = p.FailureReason
                    }).ToList(),
                    Shipments = x.Shipments.Select(s => new ShipmentDetailDto
                    {
                        Id = s.Id,
                        Carrier = s.Carrier,
                        Service = s.Service,
                        TrackingNumber = s.TrackingNumber,
                        TotalWeight = s.TotalWeight,
                        Status = s.Status,
                        ShippedAtUtc = s.ShippedAtUtc,
                        DeliveredAtUtc = s.DeliveredAtUtc
                    }).ToList()
                })
                .FirstOrDefaultAsync(ct);

            return o;
        }
    }
}
