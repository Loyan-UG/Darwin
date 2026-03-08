namespace Darwin.Contracts.Loyalty;

/// <summary>
/// Event kind emitted by clients for promotions analytics.
/// </summary>
public enum PromotionInteractionEventType
{
    Impression = 1,
    Open = 2,
    Claim = 3
}
