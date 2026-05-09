using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Inventory.DTOs;
using Darwin.Application.Inventory.Validators;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Inventory.Commands
{
    public sealed class ReserveInventoryHandler
    {
        private readonly IAppDbContext _db;
        private readonly InventoryReserveValidator _validator = new();
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public ReserveInventoryHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _localizer = localizer;
        }

        public async Task HandleAsync(InventoryReserveDto dto, CancellationToken ct = default)
        {
            var v = _validator.Validate(dto);
            if (!v.IsValid) throw new FluentValidation.ValidationException(v.Errors);

            var warehouseId = await Darwin.Application.Inventory.InventoryStockHelper.ResolveWarehouseIdAsync(_db, dto.VariantId, dto.WarehouseId, _localizer, ct);

            if (dto.ReferenceId.HasValue)
            {
                var exists = await _db.Set<InventoryTransaction>()
                    .AsNoTracking()
                    .AnyAsync(t => t.ReferenceId == dto.ReferenceId
                                   && t.Reason == dto.Reason
                                   && t.ProductVariantId == dto.VariantId
                                   && t.WarehouseId == warehouseId, ct);
                if (exists) return;
            }

            var stockLevel = await _db.Set<StockLevel>()
                .FirstOrDefaultAsync(
                    x => x.WarehouseId == warehouseId && x.ProductVariantId == dto.VariantId,
                    ct);

            if (stockLevel is null) throw new InvalidOperationException(_localizer["StockLevelNotFound"]);

            if (stockLevel.AvailableQuantity < dto.Quantity)
                throw new FluentValidation.ValidationException(_localizer["InsufficientAvailableStockForReservation"]);

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

            await Darwin.Application.Inventory.InventoryStockHelper.RefreshLegacyVariantStockAsync(_db, dto.VariantId, _localizer, ct);
            await _db.SaveChangesAsync(ct);
        }
    }
}
