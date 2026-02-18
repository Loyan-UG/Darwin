namespace Darwin.Mobile.Business.Constants;

/// <summary>
/// Centralized shell route names for the Business app.
/// 
/// Design notes:
/// - Phase 1 routes are fully active.
/// - Future-phase routes already exist so navigation structure is stable from day one.
/// - "ComingSoon*" routes currently map to placeholder pages.
/// </summary>
public static class Routes
{
    // Auth + flow routes
    public const string Login = "Login";
    public const string Session = "Session";

    // Phase 1 active tabs
    public const string Home = "Home";
    public const string Scanner = "Scanner";

    // Future tabs (currently Coming Soon placeholders)
    public const string Dashboard = "Dashboard";
    public const string Rewards = "Rewards";
    public const string Team = "Team";
    public const string Settings = "Settings";

    // Internal routes for the current Coming Soon placeholders.
    // We keep separate route names to avoid ambiguous shell behavior later.
    public const string ComingSoonDashboard = "ComingSoonDashboard";
    public const string ComingSoonRewards = "ComingSoonRewards";
    public const string ComingSoonTeam = "ComingSoonTeam";
    public const string ComingSoonSettings = "ComingSoonSettings";
}
