namespace Darwin.Contracts.Common;

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

/// <summary>
/// Standard paged response.
/// </summary>
public class PagedResponse<T>
{
    /// <summary>Total number of items available.</summary>
    public long Total { get; init; }

    /// <summary>Items for the current page.</summary>
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
}
