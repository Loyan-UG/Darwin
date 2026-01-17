using System;

namespace Darwin.Domain.Enums
{
    /// <summary>
    /// Allowed channels for a marketing campaign. Flags enable multi-channel campaigns.
    /// </summary>
    [Flags]
    public enum CampaignChannels : short
    {
        None = 0,
        InApp = 1,
        Push = 2,
        Email = 4,
        Sms = 8,
        WhatsApp = 16
    }

    /// <summary>
    /// Concrete delivery channel used by a single campaign delivery attempt.
    /// </summary>
    public enum CampaignDeliveryChannel : short
    {
        InApp = 1,
        Push = 2,
        Email = 3,
        Sms = 4,
        WhatsApp = 5
    }

    /// <summary>
    /// Lifecycle state of a campaign delivery attempt.
    /// </summary>
    public enum CampaignDeliveryStatus : short
    {
        Pending = 0,
        InProgress = 1,
        Succeeded = 2,
        Failed = 3,
        Cancelled = 4
    }
}
