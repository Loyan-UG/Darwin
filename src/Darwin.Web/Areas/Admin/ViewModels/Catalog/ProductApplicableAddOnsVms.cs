using System;
using System.Collections.Generic;

namespace Darwin.Web.Areas.Admin.ViewModels.Catalog
{
    /// <summary>
    /// View model for previewing the resolved add-on groups that apply to a product (and optionally a specific variant).
    /// This model is read-only; selection/posting is handled by product/variant edit flows or the storefront cart.
    /// </summary>
    public sealed class ProductApplicableAddOnsVm
    {
        /// <summary>Target product identifier.</summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Optional variant identifier when inspecting a specific variant.
        /// This is reserved for future variant-level attachments/overrides.
        /// </summary>
        public Guid? VariantId { get; set; }

        /// <summary>Resolved groups to display in the Admin UI.</summary>
        public List<ApplicableAddOnGroupVm> Groups { get; set; } = new();
    }

    /// <summary>
    /// Resolved add-on group with selection constraints and a flattened list of options and values.
    /// </summary>
    public sealed class ApplicableAddOnGroupVm
    {
        /// <summary>Group identifier.</summary>
        public Guid Id { get; set; }

        /// <summary>Human-readable name shown in the UI.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>ISO 4217 currency code (e.g., "EUR").</summary>
        public string Currency { get; set; } = "EUR";

        /// <summary>Selection mode representation (e.g., "Single", "Multiple"). Bind to the enum in the view.</summary>
        public string SelectionMode { get; set; } = "Single";

        /// <summary>Minimum required selections.</summary>
        public int MinSelections { get; set; }

        /// <summary>Maximum allowed selections; null means unbounded.</summary>
        public int? MaxSelections { get; set; }

        /// <summary>Only active groups are shown.</summary>
        public bool IsActive { get; set; }

        /// <summary>Options belonging to this group.</summary>
        public List<ApplicableAddOnOptionVm> Options { get; set; } = new();
    }

    /// <summary>
    /// Resolved add-on option containing ordered values.
    /// </summary>
    public sealed class ApplicableAddOnOptionVm
    {
        /// <summary>Option identifier.</summary>
        public Guid Id { get; set; }

        /// <summary>Display label (e.g., "Color").</summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>Ordering inside the group.</summary>
        public int SortOrder { get; set; }

        /// <summary>Active values ordered by SortOrder.</summary>
        public List<ApplicableAddOnOptionValueVm> Values { get; set; } = new();
    }

    /// <summary>
    /// Resolved selectable value with price delta and optional hint.
    /// </summary>
    public sealed class ApplicableAddOnOptionValueVm
    {
        /// <summary>Value identifier.</summary>
        public Guid Id { get; set; }

        /// <summary>Human-readable label (e.g., "Black").</summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>Price delta in minor units (NET). May be negative.</summary>
        public long PriceDeltaMinor { get; set; }

        /// <summary>Optional hint (e.g., color hex, short note).</summary>
        public string? Hint { get; set; }

        /// <summary>Ordering within the option.</summary>
        public int SortOrder { get; set; }
    }
}
