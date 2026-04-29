using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CartCheckout;
using Darwin.Application.CartCheckout.DTOs;
using Darwin.Domain.Entities.CartCheckout;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CartCheckout.Commands
{
    /// <summary>
    /// Removes (soft-deletes) a cart line identified by (CartId, VariantId, SelectedAddOnValueIdsJson).
    /// </summary>
    public sealed class RemoveCartItemHandler
    {
        private readonly IAppDbContext _db;
        public RemoveCartItemHandler(IAppDbContext db) => _db = db;

        public async Task HandleAsync(CartRemoveItemDto dto, CancellationToken ct = default)
        {
            var selectedAddOnValueIdsJson = CartAddOnSelectionJson.NormalizeJsonOrNull(dto.SelectedAddOnValueIdsJson);
            var originalSelectedAddOnValueIdsJson = string.IsNullOrWhiteSpace(dto.SelectedAddOnValueIdsJson)
                ? null
                : dto.SelectedAddOnValueIdsJson.Trim();

            var line = await _db.Set<CartItem>()
                .FirstOrDefaultAsync(i =>
                    !i.IsDeleted &&
                    i.CartId == dto.CartId &&
                    i.VariantId == dto.VariantId &&
                    (selectedAddOnValueIdsJson == null ||
                     i.SelectedAddOnValueIdsJson == selectedAddOnValueIdsJson ||
                     i.SelectedAddOnValueIdsJson == originalSelectedAddOnValueIdsJson),
                    ct);

            if (line == null) return; // idempotent
            line.IsDeleted = true;
            await _db.SaveChangesAsync(ct);
        }
    }
}
