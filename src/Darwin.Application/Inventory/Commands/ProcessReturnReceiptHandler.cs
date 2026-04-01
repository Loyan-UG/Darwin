using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Inventory.DTOs;
using Darwin.Application.Inventory.Validators;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.Inventory;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Inventory.Commands
{
    /// <summary>
    /// Processes a customer return receipt:
    /// - Increases StockOnHand by the returned quantity (no change to StockReserved).
    /// - Appends a ledger row with positive QuantityDelta and Reason 'ReturnReceipt' (or provided reason).
    /// - Idempotent per (ReferenceId + Reason + VariantId) when ReferenceId is supplied: existing ledger row causes skip.
    /// </summary>
    public sealed class ProcessReturnReceiptHandler
    {
        private readonly IAppDbContext _db;
        private readonly InventoryReturnReceiptValidator _validator = new();
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public ProcessReturnReceiptHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _localizer = localizer;
        }

        public async Task HandleAsync(InventoryReturnReceiptDto dto, CancellationToken ct = default)
        {
            var v = _validator.Validate(dto);
            if (!v.IsValid) throw new ValidationException(v.Errors);

            var warehouseId = await Darwin.Application.Inventory.InventoryStockHelper.ResolveWarehouseIdAsync(_db, dto.VariantId, dto.WarehouseId, _localizer, ct);

            // Idempotency: if a ReferenceId is provided and a matching ledger row exists, skip
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

            var variantExists = await _db.Set<ProductVariant>()
                .AsNoTracking()
                .AnyAsync(vr => vr.Id == dto.VariantId, ct);

            if (!variantExists)
                throw new InvalidOperationException(_localizer["VariantNotFound"]);

            var stockLevel = await Darwin.Application.Inventory.InventoryStockHelper.GetOrCreateStockLevelAsync(_db, warehouseId, dto.VariantId, ct);
            stockLevel.AvailableQuantity += dto.Quantity;

            _db.Set<InventoryTransaction>().Add(new InventoryTransaction
            {
                WarehouseId = warehouseId,
                ProductVariantId = dto.VariantId,
                QuantityDelta = dto.Quantity,
                Reason = dto.Reason,
                ReferenceId = dto.ReferenceId
            });

            await Darwin.Application.Inventory.InventoryStockHelper.RefreshLegacyVariantStockAsync(_db, dto.VariantId, _localizer, ct);
            await _db.SaveChangesAsync(ct);
        }
    }
}
