using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Pricing.DTOs;
using Darwin.Application.Pricing.Validators;
using Darwin.Domain.Entities.Pricing;
using Darwin.Domain.Enums;
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
        private readonly ValidateCouponInputValidator _validator = new();

        public ValidateCouponHandler(IAppDbContext db) => _db = db;

        public async Task<ValidateCouponResultDto> HandleAsync(ValidateCouponInputDto input, CancellationToken ct = default)
        {
            var v = _validator.Validate(input);
            if (!v.IsValid) throw new ValidationException(v.Errors);

            var now = DateTime.UtcNow;
            var promo = await _db.Set<Promotion>().AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.IsActive &&
                    p.Code != null &&
                    p.Code.ToLower() == input.Code.ToLower() &&
                    (!p.StartsAtUtc.HasValue || p.StartsAtUtc <= now) &&
                    (!p.EndsAtUtc.HasValue || now <= p.EndsAtUtc), ct);

            if (promo == null)
                return new ValidateCouponResultDto { IsValid = false, Message = "Invalid or inactive coupon." };

            if (!string.Equals(promo.Currency, input.Currency, StringComparison.OrdinalIgnoreCase))
                return new ValidateCouponResultDto { IsValid = false, Message = "Coupon currency mismatch." };

            if (promo.MinSubtotalNetMinor.HasValue && input.SubtotalNetMinor < promo.MinSubtotalNetMinor.Value)
                return new ValidateCouponResultDto { IsValid = false, Message = "Subtotal does not meet minimum requirement." };

            // Check redemption caps
            if (promo.MaxRedemptions.HasValue)
            {
                var totalRedemptions = await _db.Set<PromotionRedemption>().AsNoTracking()
                    .CountAsync(r => r.PromotionId == promo.Id, ct);
                if (totalRedemptions >= promo.MaxRedemptions.Value)
                    return new ValidateCouponResultDto { IsValid = false, Message = "Redemption limit reached." };
            }

            if (promo.PerCustomerLimit.HasValue && input.UserId.HasValue)
            {
                var perUser = await _db.Set<PromotionRedemption>().AsNoTracking()
                    .CountAsync(r => r.PromotionId == promo.Id && r.UserId == input.UserId, ct);
                if (perUser >= promo.PerCustomerLimit.Value)
                    return new ValidateCouponResultDto { IsValid = false, Message = "Per-customer limit reached." };
            }

            // Evaluate simple conditions (optional)
            if (!string.IsNullOrWhiteSpace(promo.ConditionsJson) && (input.ProductIds != null || input.CategoryIds != null))
            {
                try
                {
                    var doc = JsonSerializer.Deserialize<ConditionsModel>(promo.ConditionsJson);
                    if (doc != null)
                    {
                        // includeProducts
                        if (doc.includeProducts?.Any() == true && input.ProductIds?.Any(id => doc.includeProducts.Contains(id)) != true)
                            return new ValidateCouponResultDto { IsValid = false, Message = "Coupon conditions not met (products)." };

                        // includeCategories (TODO: needs product-category mapping; assume ok for phase 1 if not provided)
                    }
                }
                catch
                {
                    // Invalid conditions json => treat as not matched
                    return new ValidateCouponResultDto { IsValid = false, Message = "Coupon conditions not met." };
                }
            }

            // Compute discount
            long discount = 0;
            if (promo.Type == PromotionType.Amount)
            {
                discount = Math.Min(promo.AmountMinor ?? 0, input.SubtotalNetMinor);
            }
            else if (promo.Type == PromotionType.Percentage)
            {
                var pct = (promo.Percent ?? 0m) / 100m;
                discount = (long)Math.Round(input.SubtotalNetMinor * (double)pct, MidpointRounding.AwayFromZero);
            }

            return new ValidateCouponResultDto
            {
                IsValid = discount > 0,
                DiscountMinor = discount,
                Currency = promo.Currency,
                PromotionId = promo.Id,
                Message = discount > 0 ? "Coupon applied." : "No discount applicable."
            };
        }

        private sealed class ConditionsModel
        {
            public Guid[]? includeProducts { get; set; }
            public Guid[]? includeCategories { get; set; }
            public Guid[]? excludeProducts { get; set; }
        }
    }
}
