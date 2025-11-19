using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.Contracts.Common
{
    /// <summary>
    /// Standard paged response.
    /// </summary>
    public class PagedResponse<T>
    {
        /// <summary>Total number of items available.</summary>
        public long Total { get; init; }

        /// <summary>Items for the current page.</summary>
        public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

        /// <summary>Echoed paging request for client reconciliation.</summary>
        public PagedRequest Request { get; init; } = new();
    }
}
