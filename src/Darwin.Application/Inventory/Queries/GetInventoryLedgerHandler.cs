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
            Guid? variantId, int page, int pageSize, CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;

            var q = _db.Set<InventoryTransaction>().AsNoTracking().AsQueryable();
            if (variantId.HasValue)
                q = q.Where(t => t.VariantId == variantId.Value);

            var total = await q.CountAsync(ct);

            var items = await q.OrderByDescending(t => t.CreatedAtUtc)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(t => new InventoryTransactionRowDto
                {
                    Id = t.Id,
                    VariantId = t.VariantId,
                    QuantityDelta = t.QuantityDelta,
                    Reason = t.Reason,
                    ReferenceId = t.ReferenceId,
                    CreatedAtUtc = t.CreatedAtUtc
                })
                .ToListAsync(ct);

            return (items, total);
        }
    }
}
