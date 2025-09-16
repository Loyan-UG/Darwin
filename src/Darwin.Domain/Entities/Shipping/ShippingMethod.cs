using System.Collections.Generic;
using Darwin.Domain.Common;

namespace Darwin.Domain.Entities.Shipping
{
    /// <summary>
    /// Represents a shipping method (e.g., "DHL Standard") with a set of tiered rates.
    /// The optional Currency allows defining prices in the carrier's base currency for multi-currency setups.
    /// </summary>
    public sealed class ShippingMethod : BaseEntity
    {
        /// <summary>Display name shown to admins (and potentially to customers).</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Carrier code/name used for operational routing (e.g., "DHL").</summary>
        public string Carrier { get; set; } = string.Empty;

        /// <summary>Service level (e.g., "Standard", "Express").</summary>
        public string Service { get; set; } = string.Empty;

        /// <summary>Comma-separated ISO country codes this method serves; null/empty means global.</summary>
        public string? CountriesCsv { get; set; }

        /// <summary>Whether the method is available for selection in checkout.</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Optional ISO 4217 currency for this method's rates (e.g., "EUR").
        /// If null, the site's default currency is assumed at runtime.
        /// </summary>
        public string? Currency { get; set; }

        /// <summary>Tiered rates ordered by SortOrder.</summary>
        public List<ShippingRate> Rates { get; set; } = new();
    }

    /// <summary>
    /// A single tiered rate evaluated by shipment mass and order subtotal thresholds.
    /// Persist mass in SI base (grams). Display conversion per SiteSetting.
    /// </summary>
    public sealed class ShippingRate : BaseEntity
    {
        /// <summary>Owning shipping method id.</summary>
        public System.Guid ShippingMethodId { get; set; }

        /// <summary>Upper bound of shipment mass (grams). Null => no upper bound.</summary>
        public int? MaxShipmentMass { get; set; }

        /// <summary>Upper bound of order subtotal (net, minor units). Null => no bound.</summary>
        public long? MaxSubtotalNetMinor { get; set; }

        /// <summary>Shipping price in minor units (integer).</summary>
        public long PriceMinor { get; set; }

        /// <summary>Evaluation order for tiers; lower comes first.</summary>
        public int SortOrder { get; set; }
    }
}
