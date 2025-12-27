using Darwin.Contracts.Common;

namespace Darwin.Contracts.Businesses
{
    /// <summary>
    /// Request contract for map-based business discovery (pins).
    /// The client sends the currently visible viewport bounds and optional filters.
    /// </summary>
    public sealed class BusinessMapDiscoveryRequest
    {
        /// <summary>
        /// Current viewport bounds (required).
        /// </summary>
        public GeoBoundsModel? Bounds { get; set; }

        /// <summary>
        /// 1-based page index. Defaults to 1 when not provided.
        /// </summary>
        public int? Page { get; set; }

        /// <summary>
        /// Page size. Defaults to a safe upper bound when not provided.
        /// </summary>
        public int? PageSize { get; set; }

        /// <summary>
        /// Optional category token. Must match a known BusinessCategoryKind enum name.
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// Optional search query string.
        /// </summary>
        public string? Query { get; set; }

        /// <summary>
        /// Optional ISO country code filter (e.g. "DE").
        /// </summary>
        public string? CountryCode { get; set; }
    }
}
