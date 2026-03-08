using System;
using Darwin.Contracts.Loyalty;

namespace Darwin.Application.Loyalty.DTOs;

/// <summary>
/// DTO used by WebApi to record promotion interaction analytics.
/// </summary>
public sealed class TrackPromotionInteractionDto
{
    public Guid BusinessId { get; init; }
    public string BusinessName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string CtaKind { get; init; } = string.Empty;
    public PromotionInteractionEventType EventType { get; init; } = PromotionInteractionEventType.Impression;
    public DateTime? OccurredAtUtc { get; init; }
}
