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

    // Session
    public static string SessionTitle => ResourceManager.GetString(nameof(SessionTitle), Culture) ?? "Session";
    public static string CustomerLabel => ResourceManager.GetString(nameof(CustomerLabel), Culture) ?? "Customer";

    // Common
    public static string ErrorLabel => ResourceManager.GetString(nameof(ErrorLabel), Culture) ?? "Error";
}