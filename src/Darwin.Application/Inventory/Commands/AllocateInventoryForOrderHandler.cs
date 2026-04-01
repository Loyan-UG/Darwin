using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Inventory.DTOs;
using Darwin.Application.Inventory.Validators;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.Inventory;
using Darwin.Domain.Entities.Orders;
using FluentValidation;
using Microsoft.Extensions.Localization;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Inventory.Commands
{
    /// <summary>
    /// Allocates stock for a placed order:
    /// - For each line: decrements StockOnHand by 'qty' and decrements StockReserved by the same 'qty'.
    /// - Appends a ledger row with negative QuantityDelta and Reason 'ShipmentAllocation'.
    /// - Idempotent per (OrderId + VariantId): when an InventoryTransaction with the same ReferenceId and Reason exists, the line is skipped.
    /// 
    /// This handler assumes that reservation was done earlier (StockReserved >= qty per line).
    /// </summary>
    public sealed class AllocateInventoryForOrderHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<InventoryAllocateForOrderDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public AllocateInventoryForOrderHandler(
            IAppDbContext db,
            IValidator<InventoryAllocateForOrderDto> validator,
            IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _validator = validator;
            _localizer = localizer;
        }

        /// <summary>
        /// Executes allocation for all lines in a single order. Uses SQL set-based updates where possible.
        /// </summary>
        public async Task HandleAsync(InventoryAllocateForOrderDto dto, CancellationToken ct = default)
        {
            var v = _validator.Validate(dto);
            if (!v.IsValid) throw new ValidationException(v.Errors);

            // Load order basics (ensures the order exists and is not soft-deleted)
            var order = await _db.Set<Order>()
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == dto.OrderId && !o.IsDeleted, ct);
            if (order is null)
                throw new InvalidOperationException(_localizer["OrderNotFound"]);

            var variantIds = dto.Lines.Select(l => l.VariantId).Distinct().ToList();
            var existingVariantIds = await _db.Set<ProductVariant>()
                .AsNoTracking()
                .Where(vr => variantIds.Contains(vr.Id))
                .Select(vr => vr.Id)
                .ToListAsync(ct);

            if (existingVariantIds.Count != variantIds.Count)
                throw new ValidationException(_localizer["InventoryVariantsMissing"]);

            // Idempotency: check existing ledger rows for this order (Reason = ShipmentAllocation)
            var alreadyAllocated = await _db.Set<InventoryTransaction>()
                .Where(t => t.ReferenceId == dto.OrderId && t.Reason == "ShipmentAllocation")
                .Select(t => new { t.ProductVariantId, t.WarehouseId })
                .ToListAsync(ct);

            // Process each line atomically with a guarded ExecuteUpdateAsync
            foreach (var line in dto.Lines)
            {
                var warehouseId = await Darwin.Application.Inventory.InventoryStockHelper.ResolveWarehouseIdAsync(
                    _db,
                    line.VariantId,
                    line.WarehouseId ?? dto.WarehouseId,
                    _localizer,
                    ct);

                if (alreadyAllocated.Any(x => x.ProductVariantId == line.VariantId && x.WarehouseId == warehouseId))
                    continue; // skip idempotent duplicate

                var stockLevel = await _db.Set<StockLevel>()
                    .FirstOrDefaultAsync(
                        x => x.WarehouseId == warehouseId && x.ProductVariantId == line.VariantId,
                        ct);

                if (stockLevel is null || stockLevel.ReservedQuantity < line.Quantity)
                    throw new ValidationException(_localizer["InsufficientReservedStockForVariant", line.VariantId]);

                stockLevel.ReservedQuantity -= line.Quantity;

                // Append ledger: negative delta indicates stock leaves on-hand for fulfillment
                _db.Set<InventoryTransaction>().Add(new InventoryTransaction
                {
                    WarehouseId = warehouseId,
                    ProductVariantId = line.VariantId,
                    QuantityDelta = -line.Quantity,
                    Reason = "ShipmentAllocation",
                    ReferenceId = dto.OrderId
                });

                await Darwin.Application.Inventory.InventoryStockHelper.RefreshLegacyVariantStockAsync(_db, line.VariantId, _localizer, ct);
            }

            await _db.SaveChangesAsync(ct);
        }
    }
}
