namespace Darwin.Mobile.Business.Constants;

/// <summary>
/// Centralized shell route names for the Business app.
/// 
/// Design notes:
/// - Phase 1 routes are fully active.
/// - Production shell navigation exposes only implemented business operations.
/// - Dedicated settings leaf routes are explicitly named to keep navigation predictable.
/// </summary>
public static class Routes
{
    // Auth + flow routes
    public const string Login = "Login";
    public const string InvitationAcceptance = "InvitationAcceptance";
    public const string Session = "Session";

    // Phase 1 active tabs
    public const string Home = "Home";
    public const string Scanner = "Scanner";
    public const string Settings = "Settings";

    // Settings leaf routes
    public const string SettingsProfile = "SettingsProfile";
    public const string SettingsChangePassword = "SettingsChangePassword";
    public const string SettingsStaffAccessBadge = "SettingsStaffAccessBadge";
    public const string SettingsSubscription = "SettingsSubscription";
    public const string SettingsLegalHub = "SettingsLegalHub";
    public const string SettingsAccountDeletion = "SettingsAccountDeletion";

    // Active business operations tabs
    public const string Dashboard = "Dashboard";
    public const string Rewards = "Rewards";
}
