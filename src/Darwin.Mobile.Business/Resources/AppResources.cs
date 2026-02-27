using System.Globalization;
using System.Resources;

namespace Darwin.Mobile.Business.Resources;

/// <summary>
/// Strongly-typed localization accessor for Business mobile app.
/// </summary>
public static class AppResources
{
    /// <summary>
    /// Optional culture override.
    /// </summary>
    public static CultureInfo? Culture { get; set; }

    private static readonly ResourceManager ResourceManager =
        new ResourceManager("Darwin.Mobile.Business.Resources.Strings", typeof(AppResources).Assembly);

    // Navigation / generic
    public static string HomeTitle => ResourceManager.GetString(nameof(HomeTitle), Culture) ?? "Home";
    public static string StartButton => ResourceManager.GetString(nameof(StartButton), Culture) ?? "Start";
    public static string ComingSoonTitle => ResourceManager.GetString(nameof(ComingSoonTitle), Culture) ?? "Coming soon";
    public static string LogoutButtonText => ResourceManager.GetString(nameof(LogoutButtonText), Culture) ?? "Logout";

    // Scanner / actions
    public static string ScanTitle => ResourceManager.GetString(nameof(ScanTitle), Culture) ?? "Scan";
    public static string LastTokenLabel => ResourceManager.GetString(nameof(LastTokenLabel), Culture) ?? "Last token";
    public static string PointsLabel => ResourceManager.GetString(nameof(PointsLabel), Culture) ?? "Points";
    public static string PointsPlaceholder => ResourceManager.GetString(nameof(PointsPlaceholder), Culture) ?? "Enter points";

    public static string AccrueButton => ResourceManager.GetString(nameof(AccrueButton), Culture) ?? "Confirm Accrual";
    public static string RedeemButton => ResourceManager.GetString(nameof(RedeemButton), Culture) ?? "Confirm Redemption";
    public static string ConfirmAccrualButton => ResourceManager.GetString(nameof(ConfirmAccrualButton), Culture) ?? "Confirm Accrual";
    public static string ConfirmRedemptionButton => ResourceManager.GetString(nameof(ConfirmRedemptionButton), Culture) ?? "Confirm Redemption";

    public static string AccrualHelpText => ResourceManager.GetString(nameof(AccrualHelpText), Culture)
                                            ?? "Adds points to the customer account for this visit.";
    public static string RedemptionHelpText => ResourceManager.GetString(nameof(RedemptionHelpText), Culture)
                                               ?? "Confirms reward redemption and deducts required points.";

    // Scanner feedback
    public static string NoQrDetected => ResourceManager.GetString(nameof(NoQrDetected), Culture)
                                        ?? "No QR code detected. Please try again.";
    public static string NoActiveSession => ResourceManager.GetString(nameof(NoActiveSession), Culture)
                                          ?? "No active scan session.";
    public static string AccrualNotAllowed => ResourceManager.GetString(nameof(AccrualNotAllowed), Culture)
                                            ?? "Accrual is not allowed for this session.";
    public static string RedemptionNotAllowed => ResourceManager.GetString(nameof(RedemptionNotAllowed), Culture)
                                               ?? "Redemption is not allowed for this session.";
    public static string PointsMustBeGreaterThanZero => ResourceManager.GetString(nameof(PointsMustBeGreaterThanZero), Culture)
                                                      ?? "Points must be greater than zero.";
    public static string FailedToProcessScan => ResourceManager.GetString(nameof(FailedToProcessScan), Culture)
                                              ?? "Failed to process scan.";
    public static string FailedToConfirmAccrual => ResourceManager.GetString(nameof(FailedToConfirmAccrual), Culture)
                                                 ?? "Failed to confirm accrual.";
    public static string FailedToConfirmRedemption => ResourceManager.GetString(nameof(FailedToConfirmRedemption), Culture)
                                                    ?? "Failed to confirm redemption.";

    // Login
    public static string LoginTitle => ResourceManager.GetString(nameof(LoginTitle), Culture) ?? "Login";
    public static string EmailLabel => ResourceManager.GetString(nameof(EmailLabel), Culture) ?? "Email";
    public static string PasswordLabel => ResourceManager.GetString(nameof(PasswordLabel), Culture) ?? "Password";
    public static string LoginButton => ResourceManager.GetString(nameof(LoginButton), Culture) ?? "Sign in";
    public static string InvalidCredentials => ResourceManager.GetString(nameof(InvalidCredentials), Culture) ?? "Invalid email or password.";
    public static string EmailRequired => ResourceManager.GetString(nameof(EmailRequired), Culture) ?? "Email is required.";
    public static string PasswordRequired => ResourceManager.GetString(nameof(PasswordRequired), Culture) ?? "Password is required.";

    public static string ServerUnreachableMessage =>
    ResourceManager.GetString(nameof(ServerUnreachableMessage), Culture)
    ?? "Unable to reach server. Please check your internet connection and try again.";

    public static string NoBusinessMembershipMessage =>
        ResourceManager.GetString(nameof(NoBusinessMembershipMessage), Culture)
        ?? "Your username and password are correct, but your account is not assigned to any business yet. Please contact support.";


    // Session
    public static string SessionTitle => ResourceManager.GetString(nameof(SessionTitle), Culture) ?? "Session";
    public static string CustomerLabel => ResourceManager.GetString(nameof(CustomerLabel), Culture) ?? "Customer";

    // Common
    public static string ErrorLabel => ResourceManager.GetString(nameof(ErrorLabel), Culture) ?? "Error";


    public static string SettingsTitle => ResourceManager.GetString(nameof(SettingsTitle), Culture) ?? "Settings";
    public static string SettingsSubtitle => ResourceManager.GetString(nameof(SettingsSubtitle), Culture) ?? "Manage your account preferences.";
    public static string SettingsProfileButton => ResourceManager.GetString(nameof(SettingsProfileButton), Culture) ?? "Profile";
    public static string SettingsChangePasswordButton => ResourceManager.GetString(nameof(SettingsChangePasswordButton), Culture) ?? "Change password";

    public static string ProfileTitle => ResourceManager.GetString(nameof(ProfileTitle), Culture) ?? "Profile";
    public static string ProfileSectionTitle => ResourceManager.GetString(nameof(ProfileSectionTitle), Culture) ?? "My profile";
    public static string FirstNameLabel => ResourceManager.GetString(nameof(FirstNameLabel), Culture) ?? "First name";
    public static string LastNameLabel => ResourceManager.GetString(nameof(LastNameLabel), Culture) ?? "Last name";
    public static string PhoneLabel => ResourceManager.GetString(nameof(PhoneLabel), Culture) ?? "Phone";
    public static string LocaleLabel => ResourceManager.GetString(nameof(LocaleLabel), Culture) ?? "Locale";
    public static string TimezoneLabel => ResourceManager.GetString(nameof(TimezoneLabel), Culture) ?? "Timezone";
    public static string CurrencyLabel => ResourceManager.GetString(nameof(CurrencyLabel), Culture) ?? "Currency";
    public static string SaveProfileButton => ResourceManager.GetString(nameof(SaveProfileButton), Culture) ?? "Save profile";
    public static string ProfileLoadFailed => ResourceManager.GetString(nameof(ProfileLoadFailed), Culture) ?? "Unable to load profile data.";
    public static string ProfileSaveFailed => ResourceManager.GetString(nameof(ProfileSaveFailed), Culture) ?? "Unable to save profile changes.";
    public static string ProfileSaveSuccess => ResourceManager.GetString(nameof(ProfileSaveSuccess), Culture) ?? "Profile updated successfully.";
    public static string ProfileRequiredFields => ResourceManager.GetString(nameof(ProfileRequiredFields), Culture) ?? "Please fill all required profile fields before saving.";
    public static string ProfileNotLoadedYet => ResourceManager.GetString(nameof(ProfileNotLoadedYet), Culture) ?? "Profile is not loaded yet. Please refresh and try again.";

    public static string ChangePasswordTitle => ResourceManager.GetString(nameof(ChangePasswordTitle), Culture) ?? "Change password";
    public static string CurrentPasswordLabel => ResourceManager.GetString(nameof(CurrentPasswordLabel), Culture) ?? "Current password";
    public static string NewPasswordLabel => ResourceManager.GetString(nameof(NewPasswordLabel), Culture) ?? "New password";
    public static string ConfirmNewPasswordLabel => ResourceManager.GetString(nameof(ConfirmNewPasswordLabel), Culture) ?? "Confirm new password";
    public static string ChangePasswordButton => ResourceManager.GetString(nameof(ChangePasswordButton), Culture) ?? "Update password";
    public static string PasswordMismatch => ResourceManager.GetString(nameof(PasswordMismatch), Culture) ?? "New password and confirmation do not match.";
    public static string PasswordMinLength => ResourceManager.GetString(nameof(PasswordMinLength), Culture) ?? "New password must be at least 8 characters long.";
    public static string PasswordChangeFailed => ResourceManager.GetString(nameof(PasswordChangeFailed), Culture) ?? "Unable to change password. Please check your current password.";
    public static string PasswordChangeSuccess => ResourceManager.GetString(nameof(PasswordChangeSuccess), Culture) ?? "Password changed successfully.";


}