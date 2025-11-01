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
    public sealed class ReleaseInventoryReservationHandler
    {
        private readonly IAppDbContext _db;
        private readonly InventoryReleaseReservationValidator _validator = new();

        public ReleaseInventoryReservationHandler(IAppDbContext db) => _db = db;

        public async Task HandleAsync(InventoryReleaseReservationDto dto, CancellationToken ct = default)
        {
            var v = _validator.Validate(dto);
            if (!v.IsValid) throw new FluentValidation.ValidationException(v.Errors);

            // Atomic decrement guarded to avoid going negative
            var affected = await _db.Set<ProductVariant>()
                .Where(vr => vr.Id == dto.VariantId && vr.StockReserved >= dto.Quantity)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(vr => vr.StockReserved, vr => vr.StockReserved - dto.Quantity),
                    ct);

            if (affected == 0)
                throw new DbUpdateConcurrencyException("Release failed due to concurrent change or insufficient reserved.");

            // Ledger: release does not change on-hand; zero-delta for traceability.
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
