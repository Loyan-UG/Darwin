using System;
using Darwin.Contracts.Common;

namespace Darwin.Contracts.Businesses
{
    /// <summary>
    /// Request model for business discovery/list endpoints.
    /// This contract is shared between WebApi and mobile clients and must remain stable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// IMPORTANT:
    /// - Keep this request DB-agnostic. No spatial/EF-specific types are allowed.
    /// - New fields should be added in a backward-compatible way whenever possible.
    /// </para>
    /// <para>
    /// The Application layer currently supports both text-based search and proximity-based discovery.
    /// These fields provide enough information for WebApi to map to Application DTOs without guessing.
    /// </para>
    /// </remarks>
    public sealed class BusinessListRequest : PagedRequest
    {
        /// <summary>
        /// Optional free-text search query (business name, category keywords, etc.).
        /// </summary>
        public string? Query { get; init; }

        /// <summary>
        /// Optional country code filter (e.g. "DE").
        /// Useful for restricting discovery queries without forcing the client to embed country into the address string.
        /// </summary>
        public string? CountryCode { get; init; }

        /// <summary>
        /// Optional address text used for server-side "starts with / contains" filtering.
        /// This is intentionally a plain string to remain provider-agnostic.
        /// </summary>
        public string? AddressQuery { get; init; }

        /// <summary>
        /// Optional city filter for discovery/search UX.
        /// </summary>
        public string? City { get; init; }

        /// <summary>
        /// Optional category filter key.
        /// This should match the key/identifier returned by the category-kinds endpoint.
        /// </summary>
        /// <remarks>
        /// WebApi will map this value to the corresponding domain enum value for Application queries.
        /// </remarks>
        public string? CategoryKindKey { get; init; }

        /// <summary>
        /// Optional origin coordinate for proximity discovery (e.g. current device location).
        /// </summary>
        public GeoCoordinateModel? Near { get; init; }


        /// <summary>
        /// Optional radius in meters when Near is set (default 3000).
        /// </summary>
        public int? RadiusMeters { get; init; }

    }
}
