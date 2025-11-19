namespace Darwin.Contracts.Common
{

    /// <summary>
    /// Standard paging request for list endpoints.
    /// </summary>
    public class PagedRequest
    {
        /// <summary>1-based page index. Defaults to 1.</summary>
        public int Page { get; init; } = 1;

        /// <summary>Page size. Reasonable max enforced by API (e.g., 100).</summary>
        public int PageSize { get; init; } = 20;

        /// <summary>Optional free-text search.</summary>
        public string? Search { get; init; }
    }


}