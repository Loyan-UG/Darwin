using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Inventory.DTOs;
using Darwin.Application.Inventory.Validators;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.Inventory;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Inventory.Commands
{
    public sealed class ReserveInventoryHandler
    {
        private readonly IAppDbContext _db;
        private readonly InventoryReserveValidator _validator = new();

        public ReserveInventoryHandler(IAppDbContext db) => _db = db;

        public async Task HandleAsync(InventoryReserveDto dto, CancellationToken ct = default)
        {
            var v = _validator.Validate(dto);
            if (!v.IsValid) throw new FluentValidation.ValidationException(v.Errors);

            var warehouseId = await Darwin.Application.Inventory.InventoryStockHelper.ResolveWarehouseIdAsync(_db, dto.VariantId, dto.WarehouseId, ct);
            var stockLevel = await _db.Set<StockLevel>()
                .FirstOrDefaultAsync(
                    x => x.WarehouseId == warehouseId && x.ProductVariantId == dto.VariantId,
                    ct);

            if (stockLevel is null) throw new InvalidOperationException("Stock level not found.");

            if (stockLevel.AvailableQuantity < dto.Quantity)
                throw new FluentValidation.ValidationException("Insufficient available stock for reservation.");

            stockLevel.AvailableQuantity -= dto.Quantity;
            stockLevel.ReservedQuantity += dto.Quantity;

            // Ledger: reservation itself does not change on-hand; write a zero-delta row for traceability (optional).
            _db.Set<InventoryTransaction>().Add(new InventoryTransaction
            {
                WarehouseId = warehouseId,
                ProductVariantId = dto.VariantId,
                QuantityDelta = 0,
                Reason = dto.Reason,
                ReferenceId = dto.ReferenceId
            });

            await Darwin.Application.Inventory.InventoryStockHelper.RefreshLegacyVariantStockAsync(_db, dto.VariantId, ct);
            await _db.SaveChangesAsync(ct);
        }
    }
}
