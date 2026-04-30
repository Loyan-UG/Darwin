using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Pricing;
using Darwin.Application.Pricing.DTOs;
using Darwin.Application.Pricing.Validators;
using Darwin.Domain.Entities.Pricing;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Pricing.Queries
{
    /// <summary>
    /// Validates a coupon code against current basket context (subtotal, currency, optional user),
    /// checks active window and redemption limits, evaluates simple conditions, and computes discount.
    /// </summary>
    public sealed class ValidateCouponHandler
    {
        private readonly IAppDbContext _db;
        private readonly IClock _clock;
        private readonly ValidateCouponInputValidator _validator = new();

        public ValidateCouponHandler(IAppDbContext db, IClock clock)
        {
            _db = db;
            _clock = clock;
        }

        public async Task<ValidateCouponResultDto> HandleAsync(ValidateCouponInputDto input, CancellationToken ct = default)
        {
            var v = _validator.Validate(input);
            if (!v.IsValid) throw new ValidationException(v.Errors);

            var now = _clock.UtcNow;
            var normalizedCode = CouponEligibility.NormalizeCode(input.Code);
            var promo = await _db.Set<Promotion>().AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    !p.IsDeleted &&
                    p.IsActive &&
                    p.Code == normalizedCode &&
                    (!p.StartsAtUtc.HasValue || p.StartsAtUtc <= now) &&
                    (!p.EndsAtUtc.HasValue || now <= p.EndsAtUtc), ct);

            if (promo == null)
                return new ValidateCouponResultDto { IsValid = false, Message = "Invalid or inactive coupon." };

            // Check redemption caps
            if (promo.MaxRedemptions.HasValue)
            {
                var totalRedemptions = await _db.Set<PromotionRedemption>().AsNoTracking()
                    .CountAsync(r => !r.IsDeleted && r.PromotionId == promo.Id, ct);
                if (totalRedemptions >= promo.MaxRedemptions.Value)
                    return new ValidateCouponResultDto { IsValid = false, Message = "Redemption limit reached." };
            }

            if (promo.PerCustomerLimit.HasValue && input.UserId.HasValue)
            {
                var perUser = await _db.Set<PromotionRedemption>().AsNoTracking()
                    .CountAsync(r => !r.IsDeleted && r.PromotionId == promo.Id && r.UserId == input.UserId, ct);
                if (perUser >= promo.PerCustomerLimit.Value)
                    return new ValidateCouponResultDto { IsValid = false, Message = "Per-customer limit reached." };
            }

            var eligibility = CouponEligibility.Evaluate(
                promo,
                new CouponEligibilityContext
                {
                    SubtotalNetMinor = input.SubtotalNetMinor,
                    Currency = input.Currency,
                    ProductIds = input.ProductIds ?? new List<Guid>(),
                    CategoryIds = input.CategoryIds ?? new List<Guid>()
                });

            return new ValidateCouponResultDto
            {
                IsValid = eligibility.IsValid,
                DiscountMinor = eligibility.DiscountMinor,
                Currency = promo.Currency,
                PromotionId = promo.Id,
                Message = eligibility.Message
            };
        }
    }
}
