using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CartCheckout.DTOs;
using Darwin.Application.CartCheckout.Validators;
using Darwin.Domain.Entities.CartCheckout;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CartCheckout.Commands
{
    /// <summary>
    /// Applies or clears a coupon code on a cart. Validation against Promotions can be added in a later phase.
    /// </summary>
    public sealed class ApplyCouponHandler
    {
        private readonly IAppDbContext _db;
        private readonly CartApplyCouponValidator _validator = new();

        public ApplyCouponHandler(IAppDbContext db) => _db = db;

        public async Task HandleAsync(CartApplyCouponDto dto, CancellationToken ct = default)
        {
            var val = _validator.Validate(dto);
            if (!val.IsValid) throw new FluentValidation.ValidationException(val.Errors);

            var cart = await _db.Set<Cart>().FirstOrDefaultAsync(c => c.Id == dto.CartId, ct);
            if (cart is null) throw new InvalidOperationException("Cart not found.");

            cart.CouponCode = string.IsNullOrWhiteSpace(dto.CouponCode) ? null : dto.CouponCode.Trim();
            await _db.SaveChangesAsync(ct);
        }
    }
}
