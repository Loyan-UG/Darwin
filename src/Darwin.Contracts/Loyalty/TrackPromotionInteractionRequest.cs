using System;

namespace Darwin.Contracts.Loyalty;

/// <summary>
/// Request payload used by mobile clients to track promotion interactions.
/// </summary>
public sealed class TrackPromotionInteractionRequest
{
    public Guid BusinessId { get; init; }
    public string BusinessName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string CtaKind { get; init; } = string.Empty;
    public PromotionInteractionEventType EventType { get; init; } = PromotionInteractionEventType.Impression;
    public DateTime? OccurredAtUtc { get; init; }
}
