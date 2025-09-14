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
    /// Handler to remove a cart item by marking it as deleted. Uses concurrency token.
    /// </summary>
    public sealed class RemoveCartItemHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<RemoveCartItemDto> _validator;

        public RemoveCartItemHandler(IAppDbContext db, IValidator<RemoveCartItemDto> validator)
        {
            _db = db;
            _validator = validator;
        }

        public async Task HandleAsync(RemoveCartItemDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var item = await _db.Set<CartItem>()
                .FirstOrDefaultAsync(i => i.Id == dto.ItemId && i.CartId == dto.CartId && !i.IsDeleted, ct)
                ?? throw new InvalidOperationException("Cart item not found");

            if (!item.RowVersion.SequenceEqual(dto.RowVersion))
                throw new DbUpdateConcurrencyException("Concurrency conflict");

            // Soft delete
            item.IsDeleted = true;
            await _db.SaveChangesAsync(ct);
        }
    }
}
