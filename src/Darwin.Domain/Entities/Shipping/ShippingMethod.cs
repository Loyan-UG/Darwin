using System.Collections.Generic;
using Darwin.Domain.Common;


namespace Darwin.Domain.Entities.Shipping
{
    /// <summary>
    /// Shipping method definition (e.g., DHL Standard) with simple rating rules.
    /// </summary>
    public sealed class ShippingMethod : BaseEntity
    {
        /// <summary>Display name for checkout.</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>Carrier code used by fulfillment.</summary>
        public string Carrier { get; set; } = string.Empty;
        /// <summary>Service level such as Standard or Express.</summary>
        public string Service { get; set; } = string.Empty;
        /// <summary>ISO country codes (comma-separated) served by this method; null means global.</summary>
        public string? CountriesCsv { get; set; }
        /// <summary>Whether the method is available for selection.</summary>
        public bool IsActive { get; set; } = true;
        /// <summary>Rate rules ordered by weight/price thresholds.</summary>
        public List<ShippingRate> Rates { get; set; } = new();
    }


    /// <summary>
    /// Simple tiered shipping rate rule by weight or price thresholds.
    /// </summary>
    public sealed class ShippingRate : BaseEntity
    {
        public System.Guid ShippingMethodId { get; set; }
        /// <summary>Upper bound of weight (grams) for this tier; null means no bound.</summary>
        public int? MaxWeight { get; set; }
        /// <summary>Upper bound of order subtotal (net, minor units) for this tier; null means no bound.</summary>
        public long? MaxSubtotalNetMinor { get; set; }
        /// <summary>Shipping charge in minor units.</summary>
        public long PriceMinor { get; set; }
        /// <summary>Sort order for evaluation; lower first.</summary>
        public int SortOrder { get; set; }
    }
}