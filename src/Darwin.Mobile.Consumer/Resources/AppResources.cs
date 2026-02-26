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
    public static string ServerUnreachableMessage =>
    ResourceManager.GetString(nameof(ServerUnreachableMessage), Culture)
    ?? "Unable to connect to server. Please check your internet connection and server URL, then try again.";


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
    public static string LogoutButtonText => ResourceManager.GetString(nameof(LogoutButtonText), Culture) ?? "Logout";

    public static string ProfileSectionTitle => ResourceManager.GetString(nameof(ProfileSectionTitle), Culture) ?? "My profile";
    public static string FirstNameLabel => ResourceManager.GetString(nameof(FirstNameLabel), Culture) ?? "First name";
    public static string LastNameLabel => ResourceManager.GetString(nameof(LastNameLabel), Culture) ?? "Last name";
    public static string PhoneLabel => ResourceManager.GetString(nameof(PhoneLabel), Culture) ?? "Phone";
    public static string LocaleLabel => ResourceManager.GetString(nameof(LocaleLabel), Culture) ?? "Locale";
    public static string TimezoneLabel => ResourceManager.GetString(nameof(TimezoneLabel), Culture) ?? "Timezone";
    public static string CurrencyLabel => ResourceManager.GetString(nameof(CurrencyLabel), Culture) ?? "Currency";
    public static string SaveProfileButton => ResourceManager.GetString(nameof(SaveProfileButton), Culture) ?? "Save profile";
    public static string ChangePasswordTitle => ResourceManager.GetString(nameof(ChangePasswordTitle), Culture) ?? "Change password";
    public static string CurrentPasswordLabel => ResourceManager.GetString(nameof(CurrentPasswordLabel), Culture) ?? "Current password";
    public static string NewPasswordLabel => ResourceManager.GetString(nameof(NewPasswordLabel), Culture) ?? "New password";
    public static string ConfirmNewPasswordLabel => ResourceManager.GetString(nameof(ConfirmNewPasswordLabel), Culture) ?? "Confirm new password";
    public static string ChangePasswordButton => ResourceManager.GetString(nameof(ChangePasswordButton), Culture) ?? "Update password";
    public static string ProfileLoadFailed => ResourceManager.GetString(nameof(ProfileLoadFailed), Culture) ?? "Unable to load profile data.";
    public static string ProfileSaveFailed => ResourceManager.GetString(nameof(ProfileSaveFailed), Culture) ?? "Unable to save profile changes.";
    public static string ProfileSaveSuccess => ResourceManager.GetString(nameof(ProfileSaveSuccess), Culture) ?? "Profile updated successfully.";
    public static string ProfileRequiredNames => ResourceManager.GetString(nameof(ProfileRequiredNames), Culture) ?? "First name and last name are required.";
    public static string ProfileRequiredFields => ResourceManager.GetString(nameof(ProfileRequiredFields), Culture) ?? "Please fill all required profile fields before saving.";
    public static string PasswordMismatch => ResourceManager.GetString(nameof(PasswordMismatch), Culture) ?? "New password and confirmation do not match.";
    public static string PasswordMinLength => ResourceManager.GetString(nameof(PasswordMinLength), Culture) ?? "New password must be at least 8 characters long.";
    public static string PasswordChangeFailed => ResourceManager.GetString(nameof(PasswordChangeFailed), Culture) ?? "Unable to change password. Please check your current password.";
    public static string PasswordChangeSuccess => ResourceManager.GetString(nameof(PasswordChangeSuccess), Culture) ?? "Password changed successfully.";

    public static string SessionExpiredReLogin => ResourceManager.GetString(nameof(SessionExpiredReLogin), Culture) ?? "Your session has expired. Please log in again.";
    public static string ProfileNotLoadedYet => ResourceManager.GetString(nameof(ProfileNotLoadedYet), Culture) ?? "Profile is not loaded yet. Please refresh and try again.";

    public static string SettingsTitle => ResourceManager.GetString(nameof(SettingsTitle), Culture) ?? "Settings";
    public static string SettingsSubtitle => ResourceManager.GetString(nameof(SettingsSubtitle), Culture) ?? "Manage your account preferences.";
    public static string SettingsProfileButton => ResourceManager.GetString(nameof(SettingsProfileButton), Culture) ?? "Profile";
    public static string SettingsChangePasswordButton => ResourceManager.GetString(nameof(SettingsChangePasswordButton), Culture) ?? "Change password";

}
