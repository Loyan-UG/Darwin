using System;
using System.Collections.Generic;
using Darwin.Domain.Enums;

namespace Darwin.Application.Businesses.DTOs
{
    /// <summary>
    /// Represents a single business category option for discovery/filter UIs.
    /// This is a thin projection over <see cref="BusinessCategoryKind"/> with a stable integer value and a UI label.
    /// Localization is intentionally deferred to API/UI layers (or resource-based mapping) to avoid hard-coupling
    /// Application to presentation concerns.
    /// </summary>
    public sealed class BusinessCategoryKindItemDto
    {
        /// <summary>
        /// Enum value of the category.
        /// </summary>
        public BusinessCategoryKind Kind { get; set; }

        /// <summary>
        /// Numeric representation of <see cref="Kind"/> (useful for clients that store enum values).
        /// </summary>
        public short Value { get; set; }

        /// <summary>
        /// Human-friendly English label for the category.
        /// This is a fallback label; UI may replace it using localization/resources.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response model for listing all available <see cref="BusinessCategoryKind"/> values.
    /// </summary>
    public sealed class BusinessCategoryKindsResponseDto
    {
        /// <summary>
        /// All known business categories. Never null.
        /// </summary>
        public IReadOnlyList<BusinessCategoryKindItemDto> Items { get; init; } = Array.Empty<BusinessCategoryKindItemDto>();
    }
}
