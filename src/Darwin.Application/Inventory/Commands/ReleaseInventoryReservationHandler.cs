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
    public sealed class ReleaseInventoryReservationHandler
    {
        private readonly IAppDbContext _db;
        private readonly InventoryReleaseReservationValidator _validator = new();
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public ReleaseInventoryReservationHandler(
            IAppDbContext db,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _localizer = localizer;
        }

        public async Task HandleAsync(InventoryReleaseReservationDto dto, CancellationToken ct = default)
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

            if (stockLevel is null || stockLevel.ReservedQuantity < dto.Quantity)
                throw new DbUpdateConcurrencyException(_localizer["InventoryReleaseFailedDueToConcurrentChangeOrInsufficientReserved"]);

            stockLevel.ReservedQuantity -= dto.Quantity;
            stockLevel.AvailableQuantity += dto.Quantity;

            // Ledger: release does not change on-hand; zero-delta for traceability.
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
