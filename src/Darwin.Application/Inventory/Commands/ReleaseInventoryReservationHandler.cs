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
    /// Releases reserved quantity (e.g., cart removal, timeout, or order cancellation) and writes a transaction.
    /// </summary>
    public sealed class ReleaseInventoryReservationHandler
    {
        private readonly IAppDbContext _db;
        private readonly InventoryReleaseReservationValidator _validator = new();

        public ReleaseInventoryReservationHandler(IAppDbContext db) => _db = db;

        public async Task HandleAsync(InventoryReleaseReservationDto dto, CancellationToken ct = default)
        {
            var v = _validator.Validate(dto);
            if (!v.IsValid) throw new FluentValidation.ValidationException(v.Errors);

            var variant = await _db.Set<Darwin.Domain.Entities.Catalog.ProductVariant>()
                .FirstOrDefaultAsync(vr => vr.Id == dto.VariantId, ct);

            if (variant is null) throw new InvalidOperationException("Variant not found.");
            if (dto.Quantity > variant.StockReserved)
                throw new FluentValidation.ValidationException("Release quantity exceeds reserved quantity.");

            variant.StockReserved = checked(variant.StockReserved - dto.Quantity);

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
