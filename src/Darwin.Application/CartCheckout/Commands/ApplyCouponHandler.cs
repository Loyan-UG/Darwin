using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.CartCheckout.DTOs;
using Darwin.Application.Pricing;
using Darwin.Domain.Entities.CartCheckout;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.CartCheckout.Commands
{
    /// <summary>
    /// Applies or clears a coupon code on a cart after checking the same eligibility gates used by pricing.
    /// </summary>
    public sealed class ApplyCouponHandler
    {
        private readonly IAppDbContext _db;
        private readonly IClock _clock;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public ApplyCouponHandler(IAppDbContext db, IStringLocalizer<ValidationResource>? localizer = null, IClock? clock = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _clock = clock ?? DefaultHandlerDependencies.DefaultClock;
            _localizer = localizer ?? DefaultHandlerDependencies.DefaultLocalizer;
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

            var normalizedCode = CouponEligibility.NormalizeCode(dto.CouponCode);
            if (!IsCouponCodeFormatValid(normalizedCode))
            {
                throw new InvalidOperationException(_localizer["CouponIsInvalidOrInactive"]);
            }

            var lines = await _db.Set<CartItem>()
                .AsNoTracking()
                .Where(i => i.CartId == cart.Id && !i.IsDeleted && i.Quantity > 0)
                .ToListAsync(ct);

            if (lines.Count == 0)
            {
                throw new InvalidOperationException(_localizer["CouponIsInvalidOrInactive"]);
            }

            var subtotalNetMinor = lines.Sum(i => i.UnitPriceNetMinor * i.Quantity);
            var variantIds = lines.Select(i => i.VariantId).Distinct().ToList();
            var productRows = await _db.Set<ProductVariant>()
                .AsNoTracking()
                .Where(v => variantIds.Contains(v.Id) && !v.IsDeleted)
                .Join(
                    _db.Set<Product>().AsNoTracking().Where(p => !p.IsDeleted),
                    variant => variant.ProductId,
                    product => product.Id,
                    (variant, product) => new
                    {
                        ProductId = product.Id,
                        product.PrimaryCategoryId
                    })
                .ToListAsync(ct);

            if (productRows.Count != variantIds.Count)
            {
                throw new InvalidOperationException(_localizer["CartVariantsNoLongerAvailable"]);
            }

            var now = _clock.UtcNow;
            var promo = await _db.Set<Promotion>()
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    !p.IsDeleted &&
                    p.IsActive &&
                    p.Code != null &&
                    p.Code == normalizedCode &&
                    (p.StartsAtUtc == null || p.StartsAtUtc <= now) &&
                    (p.EndsAtUtc == null || p.EndsAtUtc >= now),
                    ct);

            if (promo == null)
                throw new InvalidOperationException(_localizer["CouponIsInvalidOrInactive"]);

            if (promo.MaxRedemptions.HasValue)
            {
                var totalRedemptions = await _db.Set<PromotionRedemption>()
                    .AsNoTracking()
                    .CountAsync(r => !r.IsDeleted && r.PromotionId == promo.Id, ct);
                if (totalRedemptions >= promo.MaxRedemptions.Value)
                {
                    throw new InvalidOperationException(_localizer["CouponIsInvalidOrInactive"]);
                }
            }

            if (promo.PerCustomerLimit.HasValue && cart.UserId.HasValue)
            {
                var customerRedemptions = await _db.Set<PromotionRedemption>()
                    .AsNoTracking()
                    .CountAsync(r => !r.IsDeleted && r.PromotionId == promo.Id && r.UserId == cart.UserId, ct);
                if (customerRedemptions >= promo.PerCustomerLimit.Value)
                {
                    throw new InvalidOperationException(_localizer["CouponIsInvalidOrInactive"]);
                }
            }

            var eligibility = CouponEligibility.Evaluate(
                promo,
                new CouponEligibilityContext
                {
                    SubtotalNetMinor = subtotalNetMinor,
                    Currency = cart.Currency,
                    ProductIds = productRows.Select(p => p.ProductId).Distinct().ToList(),
                    CategoryIds = productRows
                        .Where(p => p.PrimaryCategoryId.HasValue)
                        .Select(p => p.PrimaryCategoryId!.Value)
                        .Distinct()
                        .ToList()
                });

            if (!eligibility.IsValid)
            {
                throw new InvalidOperationException(_localizer["CouponIsInvalidOrInactive"]);
            }

            cart.CouponCode = promo.Code!.Trim();
            await _db.SaveChangesAsync(ct);
        }

        private static bool IsCouponCodeFormatValid(string code) =>
            code.Length <= 64 && code.All(ch => char.IsLetterOrDigit(ch) || ch == '_' || ch == '-');
    }
}

