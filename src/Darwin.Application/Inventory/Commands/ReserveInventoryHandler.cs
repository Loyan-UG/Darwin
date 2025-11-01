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

            // Load a snapshot for validation (available = on-hand - reserved)
            var snap = await _db.Set<ProductVariant>()
                .AsNoTracking()
                .Where(vr => vr.Id == dto.VariantId)
                .Select(vr => new { vr.Id, vr.StockOnHand, vr.StockReserved })
                .FirstOrDefaultAsync(ct);

            if (snap is null) throw new InvalidOperationException("Variant not found.");

            var available = snap.StockOnHand - snap.StockReserved;
            if (available < dto.Quantity)
                throw new FluentValidation.ValidationException("Insufficient available stock for reservation.");

            // Atomic increment guarded by availability condition to avoid race
            var affected = await _db.Set<ProductVariant>()
                .Where(vr => vr.Id == dto.VariantId
                             && (vr.StockOnHand - vr.StockReserved) >= dto.Quantity)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(vr => vr.StockReserved, vr => vr.StockReserved + dto.Quantity),
                    ct);

            if (affected == 0)
                throw new DbUpdateConcurrencyException("Reservation failed due to concurrent change. Retry the operation.");

            // Ledger: reservation itself does not change on-hand; write a zero-delta row for traceability (optional).
            _db.Set<InventoryTransaction>().Add(new InventoryTransaction
            {
                VariantId = dto.VariantId,
                QuantityDelta = 0,
                Reason = dto.Reason,
                ReferenceId = dto.ReferenceId
            });

            await _db.SaveChangesAsync(ct);
        }
    }
}
