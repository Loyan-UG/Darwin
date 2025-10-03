using System;
using Darwin.Domain.Common;
using Darwin.Domain.Enums;


namespace Darwin.Domain.Entities.Pricing
{
    /// <summary>
    /// Promotion/coupon definition with optional code and simple conditions. Phase 1 keeps conditions minimal.
    /// </summary>
    public sealed class Promotion : BaseEntity
    {
        /// <summary>Administrative name for the promotion.</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>Optional coupon code a customer enters at checkout (unique among active promotions).</summary>
        public string? Code { get; set; }
        /// <summary>Reward type: percentage or fixed amount in minor units.</summary>
        public PromotionType Type { get; set; } = PromotionType.Percentage;
        /// <summary>For Type=Amount, minor units in currency. For Type=Percentage, use Percent value.</summary>
        public long? AmountMinor { get; set; }
        /// <summary>For Type=Percentage, 0..100.00 stored as decimal.</summary>
        public decimal? Percent { get; set; }
        /// <summary>ISO currency code for amount-based rewards (phase 1: EUR).</summary>
        public string Currency { get; set; } = "EUR";
        /// <summary>Optional minimal order subtotal (net) required to apply the promotion (minor units).</summary>
        public long? MinSubtotalNetMinor { get; set; }
        /// <summary>Optional JSON of simple conditions (e.g., categories/products inclusion/exclusion).</summary>
        public string? ConditionsJson { get; set; }
        /// <summary>Active window for the promotion.</summary>
        public DateTime? StartsAtUtc { get; set; }
        public DateTime? EndsAtUtc { get; set; }
        /// <summary>Global redemption cap across all customers.</summary>
        public int? MaxRedemptions { get; set; }
        /// <summary>Per-customer redemption limit.</summary>
        public int? PerCustomerLimit { get; set; }
        /// <summary>Whether the promotion is currently enabled.</summary>
        public bool IsActive { get; set; } = true;
    }
}