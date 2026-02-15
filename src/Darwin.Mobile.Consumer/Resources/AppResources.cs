using System.Globalization;
using System.Resources;

namespace Darwin.Mobile.Consumer.Resources;

/// <summary>
/// Provides strongly typed access to localized application strings.
/// </summary>
public static class AppResources
{
    /// <summary>
    /// Gets or sets the culture used for resource lookup.
    /// </summary>
    public static CultureInfo? Culture { get; set; }

    private static readonly ResourceManager ResourceManager =
        new ResourceManager("Darwin.Mobile.Consumer.Resources.Strings", typeof(AppResources).Assembly);

    public static string HomeTitle => ResourceManager.GetString(nameof(HomeTitle), Culture) ?? "Home";
    public static string StartButton => ResourceManager.GetString(nameof(StartButton), Culture) ?? "Start";
    public static string ComingSoonTitle => ResourceManager.GetString(nameof(ComingSoonTitle), Culture) ?? "Coming soon";

    // keys for scanner functionality
    public static string ScanTitle => ResourceManager.GetString(nameof(ScanTitle), Culture) ?? "Scan";
    public static string LastTokenLabel => ResourceManager.GetString(nameof(LastTokenLabel), Culture) ?? "Last token";
    public static string PointsLabel => ResourceManager.GetString(nameof(PointsLabel), Culture) ?? "Points";
    public static string AccrueButton => ResourceManager.GetString(nameof(AccrueButton), Culture) ?? "Confirm Accrual";
    public static string RedeemButton => ResourceManager.GetString(nameof(RedeemButton), Culture) ?? "Confirm Redemption";

    // keys for login functionality
    public static string LoginTitle =>
    ResourceManager.GetString(nameof(LoginTitle), Culture) ?? "Login";
    public static string EmailLabel =>
        ResourceManager.GetString(nameof(EmailLabel), Culture) ?? "Email";
    public static string PasswordLabel =>
        ResourceManager.GetString(nameof(PasswordLabel), Culture) ?? "Password";
    public static string LoginButton =>
        ResourceManager.GetString(nameof(LoginButton), Culture) ?? "Sign in";
    public static string InvalidCredentials =>
        ResourceManager.GetString(nameof(InvalidCredentials), Culture) ?? "Invalid email or password.";
    public static string EmailRequired =>
        ResourceManager.GetString(nameof(EmailRequired), Culture) ?? "Email is required.";
    public static string PasswordRequired =>
        ResourceManager.GetString(nameof(PasswordRequired), Culture) ?? "Password is required.";

    public static string SessionTitle => ResourceManager.GetString(nameof(SessionTitle), Culture) ?? "Session";
    public static string CustomerLabel => ResourceManager.GetString(nameof(CustomerLabel), Culture) ?? "Customer";
    public static string PointsPlaceholder => ResourceManager.GetString(nameof(PointsPlaceholder), Culture) ?? "Enter points";
    public static string ConfirmAccrualButton => ResourceManager.GetString(nameof(ConfirmAccrualButton), Culture) ?? "Confirm Accrual";
    public static string ConfirmRedemptionButton => ResourceManager.GetString(nameof(ConfirmRedemptionButton), Culture) ?? "Confirm Redemption";

    public static string QrTitle => ResourceManager.GetString(nameof(QrTitle), Culture) ?? "QR";
    public static string DiscoverTitle => ResourceManager.GetString(nameof(DiscoverTitle), Culture) ?? "Discover";
    public static string RewardsTitle => ResourceManager.GetString(nameof(RewardsTitle), Culture) ?? "Rewards";
    public static string ProfileTitle => ResourceManager.GetString(nameof(ProfileTitle), Culture) ?? "Profile";
    public static string RefreshAccrualButton => ResourceManager.GetString(nameof(RefreshAccrualButton), Culture) ?? "Refresh Accrual";
    public static string RefreshRedemptionButton => ResourceManager.GetString(nameof(RefreshRedemptionButton), Culture) ?? "Refresh Redemption";
    public static string AccountSummaryLabel => ResourceManager.GetString(nameof(AccountSummaryLabel), Culture) ?? "Account summary";


    public static string PointsBalanceFormat => ResourceManager.GetString(nameof(PointsBalanceFormat), Culture) ?? "Points: {0}";
    public static string RewardCostFormat => ResourceManager.GetString(nameof(RewardCostFormat), Culture) ?? "Cost: {0} pts";
    public static string BusinessCategoryFormat => ResourceManager.GetString(nameof(BusinessCategoryFormat), Culture) ?? "Category: {0}";

    public static string BusinessDetailsTitle => ResourceManager.GetString(nameof(BusinessDetailsTitle), Culture) ?? "Business";
    public static string BusinessDetailsPlaceholder => ResourceManager.GetString(nameof(BusinessDetailsPlaceholder), Culture) ?? "Additional details coming soon.";
    public static string JoinProgramButton => ResourceManager.GetString(nameof(JoinProgramButton), Culture) ?? "Join Loyalty Program";

}
