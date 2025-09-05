using System;
using Darwin.Domain.Common;


namespace Darwin.Domain.Entities.Pricing
{
    /// <summary>
    /// Tax category mapping used to resolve VAT rates (e.g., Standard 19%, Reduced 7%) for variants.
    /// </summary>
    public sealed class TaxCategory : BaseEntity
    {
        /// <summary>Human-friendly name (e.g., "Standard", "Reduced").</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>VAT rate as decimal fraction (e.g., 0.19 for 19%).</summary>
        public decimal VatRate { get; set; }
        /// <summary>Optional effective-from timestamp to support rate changes over time.</summary>
        public DateTime? EffectiveFromUtc { get; set; }
        /// <summary>Optional notes or legal reference.</summary>
        public string? Notes { get; set; }
    }
}