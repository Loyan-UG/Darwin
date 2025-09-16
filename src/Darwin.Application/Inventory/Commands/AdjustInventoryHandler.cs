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
    /// <summary>
    /// Adjusts on-hand stock for a variant (positive receipt or negative write-off) and appends a ledger transaction.
    /// </summary>
    public sealed class AdjustInventoryHandler
    {
        private readonly IAppDbContext _db;
        private readonly InventoryAdjustValidator _validator = new();

        public AdjustInventoryHandler(IAppDbContext db) => _db = db;

        public async Task HandleAsync(InventoryAdjustDto dto, CancellationToken ct = default)
        {
            var v = _validator.Validate(dto);
            if (!v.IsValid) throw new FluentValidation.ValidationException(v.Errors);

            var variant = await _db.Set<ProductVariant>().FirstOrDefaultAsync(vr => vr.Id == dto.VariantId, ct);
            if (variant is null) throw new InvalidOperationException("Variant not found.");

            // apply delta
            variant.StockOnHand = checked(variant.StockOnHand + dto.QuantityDelta);

            // append ledger
            _db.Set<InventoryTransaction>().Add(new InventoryTransaction
            {
                VariantId = dto.VariantId,
                QuantityDelta = dto.QuantityDelta,
                Reason = dto.Reason,
                ReferenceId = dto.ReferenceId
            });

            await _db.SaveChangesAsync(ct);
        }
    }
}
