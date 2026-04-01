using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CartCheckout.DTOs;
using Darwin.Domain.Entities.CartCheckout;
using Darwin.Domain.Entities.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.CartCheckout.Commands
{
    /// <summary>
    /// Applies or clears a coupon code on a cart. For now, it validates that the promotion with the given
    /// code exists, is active, and is within time window. Discount calculation is still performed during
    /// pricing (e.g., at summary/checkout). This handler only stores the chosen code on the cart.
    /// </summary>
    public sealed class ApplyCouponHandler
    {
        private readonly IAppDbContext _db;
        private readonly IStringLocalizer<ValidationResource> _localizer;
        public ApplyCouponHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _localizer = localizer;
        }

        public async Task HandleAsync(CartApplyCouponDto dto, CancellationToken ct = default)
        {
            var cart = await _db.Set<Cart>().FirstOrDefaultAsync(c => c.Id == dto.CartId && !c.IsDeleted, ct)
                       ?? throw new InvalidOperationException(_localizer["CartNotFound"]);

            // Clear
            if (string.IsNullOrWhiteSpace(dto.CouponCode))
            {
                cart.CouponCode = null;
                await _db.SaveChangesAsync(ct);
                return;
            }

            // Validate promo existence and window
            var now = DateTime.UtcNow;
            var promo = await _db.Set<Promotion>()
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    !p.IsDeleted &&
                    p.IsActive &&
                    p.Code == dto.CouponCode &&
                    (p.StartsAtUtc == null || p.StartsAtUtc <= now) &&
                    (p.EndsAtUtc == null || p.EndsAtUtc >= now),
                    ct);

            if (promo == null)
                throw new InvalidOperationException(_localizer["CouponIsInvalidOrInactive"]);

            // (Optional) You can add more checks here: max redemptions, per-customer limits, etc.
            cart.CouponCode = dto.CouponCode!.Trim();
            await _db.SaveChangesAsync(ct);
        }
    }
}
