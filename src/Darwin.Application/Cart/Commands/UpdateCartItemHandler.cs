using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Cart.DTOs;
using Darwin.Domain.Entities.CartCheckout;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Cart.Commands
{
    /// <summary>
    /// Handler to update quantity or price of an existing cart item. Uses concurrency check via RowVersion.
    /// </summary>
    public sealed class UpdateCartItemHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<UpdateCartItemDto> _validator;

        public UpdateCartItemHandler(IAppDbContext db, IValidator<UpdateCartItemDto> validator)
        {
            _db = db;
            _validator = validator;
        }

        public async Task HandleAsync(UpdateCartItemDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var item = await _db.Set<CartItem>()
                .FirstOrDefaultAsync(i => i.Id == dto.ItemId && i.CartId == dto.CartId && !i.IsDeleted, ct)
                ?? throw new InvalidOperationException("Cart item not found");

            if (!item.RowVersion.SequenceEqual(dto.RowVersion))
                throw new DbUpdateConcurrencyException("Concurrency conflict");

            item.Quantity = dto.Quantity;
            item.UnitPriceNetMinor = dto.UnitPriceNetMinor;
            item.VatRate = dto.VatRate;

            await _db.SaveChangesAsync(ct);
        }
    }
}
