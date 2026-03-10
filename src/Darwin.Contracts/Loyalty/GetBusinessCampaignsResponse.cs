using System.Collections.Generic;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Paged response for business campaign management listing.
    /// </summary>
    public sealed class GetBusinessCampaignsResponse
    {
        public List<BusinessCampaignItem> Items { get; init; } = new();
        public int Total { get; init; }
    }
}
