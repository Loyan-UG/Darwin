namespace Darwin.Infrastructure.Notifications;

/// <summary>
/// Selects the active transactional email transport.
/// </summary>
public sealed class EmailDeliveryOptions
{
    public string Provider { get; set; } = EmailProviderNames.Smtp;
}
