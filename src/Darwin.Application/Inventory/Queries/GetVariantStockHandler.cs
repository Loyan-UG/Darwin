using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Inventory.Queries
{
    /// <summary>
    /// Returns current stock figures for a product variant: OnHand, Reserved, and Available.
    /// Assumes fields exist on ProductVariant aggregate.
    /// </summary>
    public sealed class GetVariantStockHandler
    {
        private readonly IAppDbContext _db;
        public GetVariantStockHandler(IAppDbContext db) => _db = db;

        public async Task<(int OnHand, int Reserved, int Available)?> HandleAsync(Guid variantId, CancellationToken ct = default)
        {
            var row = await _db.Set<Darwin.Domain.Entities.Catalog.ProductVariant>()
                .AsNoTracking()
                .Where(v => v.Id == variantId)
                .Select(v => new
                {
                    OnHand = v.StockOnHand,
                    Reserved = v.StockReserved
                })
                .FirstOrDefaultAsync(ct);

            if (row == null) return null;

            var available = row.OnHand - row.Reserved;
            return (row.OnHand, row.Reserved, available);
        }
    }
}
