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

        public ProcessReturnReceiptHandler(IAppDbContext db) => _db = db;

        public async Task HandleAsync(InventoryReturnReceiptDto dto, CancellationToken ct = default)
        {
            var v = _validator.Validate(dto);
            if (!v.IsValid) throw new ValidationException(v.Errors);

            // Idempotency: if a ReferenceId is provided and a matching ledger row exists, skip
            if (dto.ReferenceId.HasValue)
            {
                var exists = await _db.Set<InventoryTransaction>()
                    .AsNoTracking()
                    .AnyAsync(t => t.ReferenceId == dto.ReferenceId
                                   && t.Reason == dto.Reason
                                   && t.VariantId == dto.VariantId, ct);
                if (exists) return;
            }

            // Atomic increment of on-hand using ExecuteUpdateAsync
            var affected = await _db.Set<ProductVariant>()
                .Where(vr => vr.Id == dto.VariantId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(vr => vr.StockOnHand, vr => vr.StockOnHand + dto.Quantity),
                    ct);

            if (affected == 0)
                throw new InvalidOperationException("Variant not found.");

            _db.Set<InventoryTransaction>().Add(new InventoryTransaction
            {
                VariantId = dto.VariantId,
                QuantityDelta = dto.Quantity,
                Reason = dto.Reason,
                ReferenceId = dto.ReferenceId
            });

            await _db.SaveChangesAsync(ct);
        }
    }
}
