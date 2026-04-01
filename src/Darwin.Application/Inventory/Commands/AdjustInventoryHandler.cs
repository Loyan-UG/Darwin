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
    /// <summary>
    /// Adjusts on-hand stock for a variant (positive receipt or negative write-off) and appends a ledger transaction.
    /// </summary>
    public sealed class AdjustInventoryHandler
    {
        private readonly IAppDbContext _db;
        private readonly InventoryAdjustValidator _validator = new();
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public AdjustInventoryHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _localizer = localizer;
        }

        public async Task HandleAsync(InventoryAdjustDto dto, CancellationToken ct = default)
        {
            var v = _validator.Validate(dto);
            if (!v.IsValid) throw new FluentValidation.ValidationException(v.Errors);

            var variant = await _db.Set<ProductVariant>().FirstOrDefaultAsync(vr => vr.Id == dto.VariantId, ct);
            if (variant is null) throw new InvalidOperationException(_localizer["VariantNotFound"]);

            var warehouseId = await Darwin.Application.Inventory.InventoryStockHelper.ResolveWarehouseIdAsync(_db, dto.VariantId, dto.WarehouseId, _localizer, ct);
            var stockLevel = await Darwin.Application.Inventory.InventoryStockHelper.GetOrCreateStockLevelAsync(_db, warehouseId, dto.VariantId, ct);

            if (dto.QuantityDelta < 0 && stockLevel.AvailableQuantity < Math.Abs(dto.QuantityDelta))
            {
                throw new InvalidOperationException(_localizer["InsufficientAvailableStock"]);
            }

            stockLevel.AvailableQuantity = checked(stockLevel.AvailableQuantity + dto.QuantityDelta);

            // append ledger
            _db.Set<InventoryTransaction>().Add(new InventoryTransaction
            {
                WarehouseId = warehouseId,
                ProductVariantId = dto.VariantId,
                QuantityDelta = dto.QuantityDelta,
                Reason = dto.Reason,
                ReferenceId = dto.ReferenceId
            });

            await Darwin.Application.Inventory.InventoryStockHelper.RefreshLegacyVariantStockAsync(_db, dto.VariantId, _localizer, ct);
            await _db.SaveChangesAsync(ct);
        }
    }
}
