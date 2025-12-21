using System;

namespace Darwin.Contracts.Businesses
{
    /// <summary>
    /// Represents a single business category kind option for UI filter lists.
    /// The API intentionally returns both the numeric value and a stable key so
    /// client apps can store/filter without hardcoding server enums.
    /// </summary>
    public sealed class BusinessCategoryKindItem
    {
        /// <summary>
        /// Numeric value of the category kind (aligned with server enum values).
        /// </summary>
        public int Value { get; set; }

        /// <summary>
        /// Stable key name (typically the enum name).
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Human readable label (English fallback; localization is a UI concern).
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;
    }
}
