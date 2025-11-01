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
        private readonly InventoryAllocateForOrderValidator _validator = new();

        public AllocateInventoryForOrderHandler(IAppDbContext db) => _db = db;

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
                throw new InvalidOperationException("Order not found.");

            // Fetch variants impacted (track minimal state)
            var variantIds = dto.Lines.Select(l => l.VariantId).Distinct().ToList();
            var variants = await _db.Set<ProductVariant>()
                .Where(vr => variantIds.Contains(vr.Id))
                .Select(vr => new { vr.Id, vr.StockOnHand, vr.StockReserved })
                .ToListAsync(ct);

            if (variants.Count != variantIds.Count)
                throw new ValidationException("One or more variants do not exist.");

            // Idempotency: check existing ledger rows for this order (Reason = ShipmentAllocation)
            var alreadyAllocated = await _db.Set<InventoryTransaction>()
                .Where(t => t.ReferenceId == dto.OrderId && t.Reason == "ShipmentAllocation")
                .Select(t => t.VariantId)
                .ToListAsync(ct);

            // Process each line atomically with a guarded ExecuteUpdateAsync
            foreach (var line in dto.Lines)
            {
                if (alreadyAllocated.Contains(line.VariantId))
                    continue; // skip idempotent duplicate

                // First, ensure current snapshot has enough reserved to move out
                var snap = variants.First(x => x.Id == line.VariantId);
                if (snap.StockReserved < line.Quantity)
                    throw new ValidationException($"Insufficient reserved stock for variant {line.VariantId}.");

                // Apply: StockOnHand -= qty, StockReserved -= qty (guard with WHERE to avoid race)
                var affected = await _db.Set<ProductVariant>()
                    .Where(vr => vr.Id == line.VariantId
                                 && vr.StockReserved >= line.Quantity)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(vr => vr.StockOnHand, vr => vr.StockOnHand - line.Quantity)
                        .SetProperty(vr => vr.StockReserved, vr => vr.StockReserved - line.Quantity),
                        ct);

                if (affected == 0)
                    throw new DbUpdateConcurrencyException("Allocation failed due to concurrent change. Retry the operation.");

                // Append ledger: negative delta indicates stock leaves on-hand for fulfillment
                _db.Set<InventoryTransaction>().Add(new InventoryTransaction
                {
                    VariantId = line.VariantId,
                    QuantityDelta = -line.Quantity,
                    Reason = "ShipmentAllocation",
                    ReferenceId = dto.OrderId
                });
            }

            await _db.SaveChangesAsync(ct);
        }
    }
}
