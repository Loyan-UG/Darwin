using System;

namespace Darwin.Contracts.Loyalty
{
    /// <summary>
    /// Request payload for toggling campaign activation state.
    /// </summary>
    public sealed class SetCampaignActivationRequest
    {
        public Guid Id { get; init; }
        public bool IsActive { get; init; }
        public byte[] RowVersion { get; init; } = Array.Empty<byte>();
    }
}
