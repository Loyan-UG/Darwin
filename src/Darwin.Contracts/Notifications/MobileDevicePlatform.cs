namespace Darwin.Contracts.Notifications;

/// <summary>
/// Client-facing mobile platform discriminator used for push token registration.
/// Keep values stable for backward compatibility across app versions.
/// </summary>
public enum MobileDevicePlatform : short
{
    Unknown = 0,
    Android = 1,
    iOS = 2
}
