namespace Darwin.Mobile.Business.Constants;

/// <summary>
/// Centralized shell route names for the Business app.
/// 
/// Design notes:
/// - Phase 1 routes are fully active.
/// - Future-phase routes already exist so navigation structure is stable from day one.
/// - Dedicated settings leaf routes are explicitly named to keep navigation predictable.
/// </summary>
public static class Routes
{
    // Auth + flow routes
    public const string Login = "Login";
    public const string Session = "Session";

    // Phase 1 active tabs
    public const string Home = "Home";
    public const string Scanner = "Scanner";
    public const string Settings = "Settings";

    // Settings leaf routes
    public const string SettingsProfile = "SettingsProfile";
    public const string SettingsChangePassword = "SettingsChangePassword";

    // Future tabs (currently Coming Soon placeholders)
    public const string Dashboard = "Dashboard";
    public const string Rewards = "Rewards";
    public const string Team = "Team";

    // Internal routes for current Coming Soon placeholders.
    public const string ComingSoonDashboard = "ComingSoonDashboard";
    public const string ComingSoonRewards = "ComingSoonRewards";
    public const string ComingSoonTeam = "ComingSoonTeam";
}
