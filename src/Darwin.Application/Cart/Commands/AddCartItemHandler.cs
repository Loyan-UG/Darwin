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
    /// Handler to add an item to an existing cart. If the item already exists, quantity is incremented.
    /// </summary>
    public sealed class AddCartItemHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<AddCartItemDto> _validator;

        public AddCartItemHandler(IAppDbContext db, IValidator<AddCartItemDto> validator)
        {
            _db = db;
            _validator = validator;
        }

        public async Task<Guid> HandleAsync(AddCartItemDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var cart = await _db.Set<Darwin.Domain.Entities.CartCheckout.Cart>()
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == dto.CartId, ct)
                ?? throw new InvalidOperationException("Cart not found");

            // Check for existing item
            var existing = cart.Items.FirstOrDefault(i => i.VariantId == dto.VariantId && !i.IsDeleted);
            if (existing != null)
            {
                existing.Quantity += dto.Quantity;
                existing.UnitPriceNetMinor = dto.UnitPriceNetMinor;
                existing.VatRate = dto.VatRate;
            }
            else
            {
                cart.Items.Add(new CartItem
                {
                    Id = Guid.NewGuid(),
                    CartId = cart.Id,
                    VariantId = dto.VariantId,
                    Quantity = dto.Quantity,
                    UnitPriceNetMinor = dto.UnitPriceNetMinor,
                    VatRate = dto.VatRate
                });
            }

            await _db.SaveChangesAsync(ct);
            return existing?.Id ?? cart.Items.Last().Id;
        }
    }
}
