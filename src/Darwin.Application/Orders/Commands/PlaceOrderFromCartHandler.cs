using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.CartCheckout;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Orders.Commands
{
    /// <summary>
    /// Creates an Order snapshot from a Cart. Copies add-on selections for each line
    /// and populates Name/SKU from catalog using the provided culture.
    /// </summary>
    public sealed class PlaceOrderFromCartHandler
    {
        private readonly IAppDbContext _db;
        public PlaceOrderFromCartHandler(IAppDbContext db) => _db = db;

        public async Task<Order> HandleAsync(System.Guid cartId, System.Guid? userId, string culture = "de-DE", System.Threading.CancellationToken ct = default)
        {
            var cart = await _db.Set<Cart>()
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == cartId && !c.IsDeleted, ct)
                ?? throw new System.InvalidOperationException("Cart not found.");

            if (cart.Items.Count == 0)
                throw new System.InvalidOperationException("Cart is empty.");

            var order = new Order
            {
                UserId = userId,
                Currency = cart.Currency,
                PricesIncludeTax = false
            };

            long subtotalNet = 0;
            long taxTotal = 0;

            // Pre-load variant + product + translation for all lines
            var variantIds = cart.Items.Select(i => i.VariantId).Distinct().ToList();
            var variants = await _db.Set<ProductVariant>()
                .Where(v => variantIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id, ct);

            var productIds = variants.Values.Select(v => v.ProductId).Distinct().ToList();
            var translations = await _db.Set<ProductTranslation>()
                .Where(t => productIds.Contains(t.ProductId) && (t.Culture == culture))
                .ToDictionaryAsync(t => t.ProductId, ct);

            foreach (var ci in cart.Items)
            {
                var v = variants[ci.VariantId];
                var name = translations.TryGetValue(v.ProductId, out var t) ? t.Name : string.Empty;

                var lineNet = ci.UnitPriceNetMinor * ci.Quantity;
                var lineTax = (long)System.Math.Round(lineNet * (double)ci.VatRate, System.MidpointRounding.AwayFromZero);
                var lineGross = lineNet + lineTax;

                order.Lines.Add(new OrderLine
                {
                    VariantId = ci.VariantId,
                    Name = name,
                    Sku = v.Sku,
                    Quantity = ci.Quantity,
                    UnitPriceNetMinor = ci.UnitPriceNetMinor,
                    VatRate = ci.VatRate,
                    UnitPriceGrossMinor = ci.UnitPriceNetMinor + (long)System.Math.Round(ci.UnitPriceNetMinor * (double)ci.VatRate),
                    LineTaxMinor = lineTax,
                    LineGrossMinor = lineGross,
                    SelectedAddOnValueIdsJson = ci.SelectedAddOnValueIdsJson ?? "[]"
                });

                subtotalNet += lineNet;
                taxTotal += lineTax;
            }

            order.SubtotalNetMinor = subtotalNet;
            order.TaxTotalMinor = taxTotal;
            order.ShippingTotalMinor = 0; // rated later
            order.DiscountTotalMinor = 0; // applied later
            order.GrandTotalGrossMinor = subtotalNet + taxTotal;

            _db.Set<Order>().Add(order);
            await _db.SaveChangesAsync(ct);
            return order;
        }
    }
}
