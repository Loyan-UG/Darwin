using System;
using System.Collections.Generic;

namespace Darwin.Contracts.Businesses
{
    /// <summary>
    /// Response contract for the category kinds endpoint.
    /// </summary>
    public sealed class BusinessCategoryKindsResponse
    {
        /// <summary>
        /// Ordered list of available category kinds.
        /// </summary>
        public IReadOnlyList<BusinessCategoryKindItem> Items { get; set; } = Array.Empty<BusinessCategoryKindItem>();
    }
}
