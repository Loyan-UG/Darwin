using System;
using System.Collections.Generic;
using Darwin.Domain.Common;

namespace Darwin.Domain.Entities.Catalog
{
    /// <summary>
    /// Logical container for a set of add-on options/values that can be attached to many products,
    /// categories, or brands. Examples: "Sunglass Lenses", "Gift Wrapping", "Extended Warranty".
    /// A group defines selection rules (single/multi and min/max) and the pricing currency used for deltas.
    /// </summary>
    public sealed class AddOnGroup : BaseEntity
    {
        /// <summary>Administrative name of the group shown in Admin (not public-facing).</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>ISO 4217 currency of price deltas (phase 1 typically "EUR").</summary>
        public string Currency { get; set; } = "EUR";

        /// <summary>When true, this group is considered global (applies to all products) unless overridden by assignments.</summary>
        public bool IsGlobal { get; set; } = false;

        /// <summary>Selection mode: single choice (radio) or multiple (checkbox).</summary>
        public Enums.AddOnSelectionMode SelectionMode { get; set; } = Enums.AddOnSelectionMode.Single;

        /// <summary>Minimum number of values required to be selected (0 means optional).</summary>
        public int MinSelections { get; set; } = 0;

        /// <summary>Maximum number of values allowed to be selected (null = no cap).</summary>
        public int? MaxSelections { get; set; }

        /// <summary>Whether this group is available on the storefront.</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Options/questions contained in this group.</summary>
        public List<AddOnOption> Options { get; set; } = new();
    }

    

    /// <summary>
    /// A user-facing question or pick-list inside an add-on group (e.g., "Lens Type", "Gift Wrap").
    /// </summary>
    public sealed class AddOnOption : BaseEntity
    {
        public Guid AddOnGroupId { get; set; }

        /// <summary>Display label shown to end-users.</summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>Ordering within the group.</summary>
        public int SortOrder { get; set; } = 0;

        /// <summary>Possible values to choose from.</summary>
        public List<AddOnOptionValue> Values { get; set; } = new();
    }

    /// <summary>
    /// A selectable value with an associated price delta (may be positive or negative).
    /// </summary>
    public sealed class AddOnOptionValue : BaseEntity
    {
        public Guid AddOnOptionId { get; set; }

        /// <summary>Human-readable label (e.g., "Black Sunglass Lens").</summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>Price delta (minor units, net). Can be negative for discounts.</summary>
        public long PriceDeltaMinor { get; set; } = 0;

        /// <summary>Optional hint (e.g., color hex, short note).</summary>
        public string? Hint { get; set; }

        /// <summary>Ordering under the option.</summary>
        public int SortOrder { get; set; } = 0;

        /// <summary>Whether this value is currently selectable.</summary>
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Assignment: attaches an add-on group to a product.
    /// </summary>
    public sealed class AddOnGroupProduct : BaseEntity
    {
        public Guid AddOnGroupId { get; set; }
        public Guid ProductId { get; set; }
    }

    /// <summary>
    /// Assignment: attaches an add-on group to a category (applies to all products under it).
    /// </summary>
    public sealed class AddOnGroupCategory : BaseEntity
    {
        public Guid AddOnGroupId { get; set; }
        public Guid CategoryId { get; set; }
    }

    /// <summary>
    /// Assignment: attaches an add-on group to a brand (applies to all its products).
    /// </summary>
    public sealed class AddOnGroupBrand : BaseEntity
    {
        public Guid AddOnGroupId { get; set; }
        public Guid BrandId { get; set; }
    }

    /// <summary>
    /// Assignment: attaches an add-on group to a specific product variant.
    /// Precedence for applicable add-ons becomes:
    /// Variant → Product → Category → Brand → Global
    /// </summary>
    public sealed class AddOnGroupVariant : BaseEntity
    {
        /// <summary>Identifier of the add-on group.</summary>
        public Guid AddOnGroupId { get; set; }

        /// <summary>Identifier of the product variant.</summary>
        public Guid VariantId { get; set; }
    }
}
