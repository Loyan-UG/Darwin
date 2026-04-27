using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Darwin.Domain.Entities.Pricing;
using Darwin.Domain.Enums;

namespace Darwin.Application.Pricing
{
    internal sealed class CouponEligibilityContext
    {
        public long SubtotalNetMinor { get; set; }
        public string Currency { get; set; } = Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCurrencyDefault;
        public IReadOnlyCollection<Guid> ProductIds { get; init; } = Array.Empty<Guid>();
        public IReadOnlyCollection<Guid> CategoryIds { get; init; } = Array.Empty<Guid>();
    }

    internal sealed class CouponEligibilityResult
    {
        public bool IsValid { get; init; }
        public string Message { get; init; } = string.Empty;
        public long DiscountMinor { get; init; }
    }

    internal static class CouponEligibility
    {
        public static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

        public static CouponEligibilityResult Evaluate(Promotion promotion, CouponEligibilityContext context)
        {
            if (!string.Equals(promotion.Currency, context.Currency, StringComparison.OrdinalIgnoreCase))
            {
                return Invalid("Coupon currency mismatch.");
            }

            if (promotion.MinSubtotalNetMinor.HasValue && context.SubtotalNetMinor < promotion.MinSubtotalNetMinor.Value)
            {
                return Invalid("Subtotal does not meet minimum requirement.");
            }

            var conditionsResult = EvaluateConditions(promotion.ConditionsJson, context);
            if (!conditionsResult.IsValid)
            {
                return conditionsResult;
            }

            var discount = CalculateDiscount(promotion, context.SubtotalNetMinor);
            return discount > 0
                ? new CouponEligibilityResult { IsValid = true, DiscountMinor = discount, Message = "Coupon applied." }
                : Invalid("No discount applicable.");
        }

        private static CouponEligibilityResult EvaluateConditions(string? conditionsJson, CouponEligibilityContext context)
        {
            if (string.IsNullOrWhiteSpace(conditionsJson))
            {
                return new CouponEligibilityResult { IsValid = true };
            }

            ConditionsModel? conditions;
            try
            {
                conditions = JsonSerializer.Deserialize<ConditionsModel>(
                    conditionsJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                return Invalid("Coupon conditions not met.");
            }

            if (conditions is null)
            {
                return new CouponEligibilityResult { IsValid = true };
            }

            var productIds = context.ProductIds.ToHashSet();
            var categoryIds = context.CategoryIds.ToHashSet();

            if (conditions.excludeProducts?.Any(productIds.Contains) == true)
            {
                return Invalid("Coupon conditions not met (excluded products).");
            }

            if (conditions.excludeCategories?.Any(categoryIds.Contains) == true)
            {
                return Invalid("Coupon conditions not met (excluded categories).");
            }

            if (conditions.includeProducts?.Any() == true && !conditions.includeProducts.Any(productIds.Contains))
            {
                return Invalid("Coupon conditions not met (products).");
            }

            if (conditions.includeCategories?.Any() == true && !conditions.includeCategories.Any(categoryIds.Contains))
            {
                return Invalid("Coupon conditions not met (categories).");
            }

            return new CouponEligibilityResult { IsValid = true };
        }

        private static long CalculateDiscount(Promotion promotion, long subtotalNetMinor)
        {
            var discount = promotion.Type switch
            {
                PromotionType.Amount => Math.Min(promotion.AmountMinor ?? 0, subtotalNetMinor),
                PromotionType.Percentage => (long)Math.Round(subtotalNetMinor * ((promotion.Percent ?? 0m) / 100m), MidpointRounding.AwayFromZero),
                _ => 0
            };

            return Math.Clamp(discount, 0, subtotalNetMinor);
        }

        private static CouponEligibilityResult Invalid(string message) =>
            new() { IsValid = false, Message = message };

        private sealed class ConditionsModel
        {
            public Guid[]? includeProducts { get; set; }
            public Guid[]? includeCategories { get; set; }
            public Guid[]? excludeProducts { get; set; }
            public Guid[]? excludeCategories { get; set; }
        }
    }
}
