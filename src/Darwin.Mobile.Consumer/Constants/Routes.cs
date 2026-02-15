namespace Darwin.Mobile.Consumer.Constants;

/// <summary>
/// Contains route constants used for navigation throughout the Consumer app.
/// These keys must remain stable across releases to avoid breaking deep links and navigation history.
/// </summary>
public static class Routes
{
    /// <summary>
    /// Represents the endpoint path for user login in the consumer application.
    /// </summary>
    /// <remarks>Use this constant to specify the login route when making authentication-related API calls
    /// within the application. Centralizing the route as a constant helps ensure consistency and simplifies maintenance
    /// if the endpoint changes.</remarks>
    public const string Login = "consumer/main/login";

    /// <summary>
    /// Route for the Discover tab (default landing page).
    /// </summary>
    public const string Discover = "consumer/main/discover";

    /// <summary>
    /// Route for the QR tab.
    /// </summary>
    public const string Qr = "consumer/main/qr";

    /// <summary>
    /// Route for the Rewards tab.
    /// </summary>
    public const string Rewards = "consumer/main/rewards";

    /// <summary>
    /// Route for the Profile tab.
    /// </summary>
    public const string Profile = "consumer/main/profile";

    /// <summary>
    /// Route prefix for viewing details of a business. A business ID should be appended.
    /// Example: $"consumer/discover/business/{businessId}"
    /// </summary>
    public const string BusinessDetail = "consumer/discover/business";


    //public const string Login = "Login";
    //public const string Qr = "Qr";
    //public const string Discover = "Discover";
    //public const string Rewards = "Rewards";
    //public const string Profile = "Profile";
}
