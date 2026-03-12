using System;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Response payload returned after successful campaign create/update mutations.
    /// </summary>
    public sealed class BusinessCampaignMutationResponse
    {
        public Guid CampaignId { get; init; }
    }
}
