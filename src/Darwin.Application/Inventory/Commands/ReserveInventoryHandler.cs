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
    /// Reserves quantity for a variant (e.g., on cart add or order placement) and writes a transaction.
    /// </summary>
    public sealed class ReserveInventoryHandler
    {
        private readonly IAppDbContext _db;
        private readonly InventoryReserveValidator _validator = new();

        public ReserveInventoryHandler(IAppDbContext db) => _db = db;

        public async Task HandleAsync(InventoryReserveDto dto, CancellationToken ct = default)
        {
            var v = _validator.Validate(dto);
            if (!v.IsValid) throw new FluentValidation.ValidationException(v.Errors);

            var variant = await _db.Set<ProductVariant>().FirstOrDefaultAsync(vr => vr.Id == dto.VariantId, ct);
            if (variant is null) throw new InvalidOperationException("Variant not found.");

            // business rule: available = OnHand - Reserved
            var available = variant.StockOnHand - variant.StockReserved;
            if (dto.Quantity > available)
                throw new FluentValidation.ValidationException("Insufficient available stock to reserve.");

            variant.StockReserved = checked(variant.StockReserved + dto.Quantity);

            _db.Set<InventoryTransaction>().Add(new InventoryTransaction
            {
                VariantId = dto.VariantId,
                QuantityDelta = 0, // reservation does not change on-hand
                Reason = dto.Reason,
                ReferenceId = dto.ReferenceId
            });

            await _db.SaveChangesAsync(ct);
        }
    }
}
