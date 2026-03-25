using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Inventory.DTOs;
using Darwin.Domain.Entities.Inventory;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Inventory.Queries
{
    /// <summary>
    /// Returns paged inventory ledger (transactions) for a specific variant (or whole store if variantId is null).
    /// </summary>
    public sealed class GetInventoryLedgerHandler
    {
        private readonly IAppDbContext _db;
        public GetInventoryLedgerHandler(IAppDbContext db) => _db = db;

        public async Task<(List<InventoryTransactionRowDto> Items, int Total)> HandleAsync(
            Guid? variantId, int page, int pageSize, Guid? warehouseId = null, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;

            var q =
                from transaction in _db.Set<InventoryTransaction>().AsNoTracking()
                join warehouse in _db.Set<Warehouse>().AsNoTracking() on transaction.WarehouseId equals warehouse.Id
                select new { transaction, warehouse };
            if (variantId.HasValue)
                q = q.Where(x => x.transaction.ProductVariantId == variantId.Value);
            if (warehouseId.HasValue)
                q = q.Where(x => x.transaction.WarehouseId == warehouseId.Value);

            var total = await q.CountAsync(ct);

            var items = await q.OrderByDescending(x => x.transaction.CreatedAtUtc)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new InventoryTransactionRowDto
                {
                    Id = x.transaction.Id,
                    WarehouseId = x.transaction.WarehouseId,
                    WarehouseName = x.warehouse.Name,
                    VariantId = x.transaction.ProductVariantId,
                    QuantityDelta = x.transaction.QuantityDelta,
                    Reason = x.transaction.Reason,
                    ReferenceId = x.transaction.ReferenceId,
                    CreatedAtUtc = x.transaction.CreatedAtUtc
                })
                .ToListAsync(ct);

            return (items, total);
        }
    }
}
