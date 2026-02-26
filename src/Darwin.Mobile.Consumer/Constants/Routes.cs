namespace Darwin.Mobile.Consumer.Constants;

/// <summary>
/// Centralized Shell route names used by the Consumer mobile app.
///
/// Important conventions:
/// - Keep route names as single, stable segments (no embedded '/') to avoid Shell URI parsing ambiguity.
/// - Use absolute navigation (for example: "//" + Routes.Qr) when switching tabs/root sections.
/// - Keep these identifiers backward-compatible once released to avoid breaking deep links/bookmarks.
/// </summary>
public static class Routes
{
    /// <summary>
    /// Login route used by the unauthenticated flow.
    /// </summary>
    public const string Login = "login";

    /// <summary>
    /// Discover tab route (default authenticated landing tab).
    /// </summary>
    public const string Discover = "discover";

    /// <summary>
    /// QR tab route.
    /// </summary>
    public const string Qr = "qr";

    /// <summary>
    /// Rewards tab route.
    /// </summary>
    public const string Rewards = "rewards";

    /// <summary>
    /// Central settings hub.
    /// </summary>
    public const string Settings = "settings";

    /// <summary>
    /// Dedicated profile edit screen route.
    /// </summary>
    public const string ProfileEdit = "profile-edit";

    /// <summary>
    /// Dedicated password change screen route.
    /// </summary>
    public const string ChangePassword = "change-password";

    /// <summary>
    /// Route prefix for the business detail page.
    /// The page may receive additional route/query parameters (for example a business identifier).
    /// </summary>
    public const string BusinessDetail = "business-detail";
}
