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
    public static string RewardsBusinessPickerLabel => ResourceManager.GetString(nameof(RewardsBusinessPickerLabel), Culture) ?? "Business";
    public static string RewardsAvailableRewardsLabel => ResourceManager.GetString(nameof(RewardsAvailableRewardsLabel), Culture) ?? "Available rewards";
    public static string RewardsHistoryLabel => ResourceManager.GetString(nameof(RewardsHistoryLabel), Culture) ?? "Rewards history";
    public static string RewardsNoHistoryMessage => ResourceManager.GetString(nameof(RewardsNoHistoryMessage), Culture) ?? "No reward history available yet.";
    public static string RewardsNoAccountsFound => ResourceManager.GetString(nameof(RewardsNoAccountsFound), Culture) ?? "No loyalty accounts found yet.";
    public static string RewardsLoadAccountsFailed => ResourceManager.GetString(nameof(RewardsLoadAccountsFailed), Culture) ?? "Unable to load your loyalty accounts.";
    public static string RewardsLoadAccountSummaryFailed => ResourceManager.GetString(nameof(RewardsLoadAccountSummaryFailed), Culture) ?? "Unable to load account summary.";
    public static string RewardsLoadRewardsFailed => ResourceManager.GetString(nameof(RewardsLoadRewardsFailed), Culture) ?? "Unable to load available rewards.";
    public static string RewardsLoadHistoryFailed => ResourceManager.GetString(nameof(RewardsLoadHistoryFailed), Culture) ?? "Unable to load reward history.";
    public static string RewardsJoinedBusinessesLabel => ResourceManager.GetString(nameof(RewardsJoinedBusinessesLabel), Culture) ?? "Joined businesses";
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

    public static string RegisterTitle => ResourceManager.GetString(nameof(RegisterTitle), Culture) ?? "Create account";
    public static string RegisterDescription => ResourceManager.GetString(nameof(RegisterDescription), Culture) ?? "Create your consumer account to start collecting points and rewards.";
    public static string RegisterButton => ResourceManager.GetString(nameof(RegisterButton), Culture) ?? "Register";
    public static string RegisterFailed => ResourceManager.GetString(nameof(RegisterFailed), Culture) ?? "Unable to create your account right now. Please try again.";
    public static string RegisterSuccess => ResourceManager.GetString(nameof(RegisterSuccess), Culture) ?? "Your account has been created successfully.";
    public static string RegisterSuccessConfirmationSent => ResourceManager.GetString(nameof(RegisterSuccessConfirmationSent), Culture) ?? "Your account has been created. Please check your email for confirmation.";
    public static string RegisterEmailAlreadyUsed => ResourceManager.GetString(nameof(RegisterEmailAlreadyUsed), Culture) ?? "An account with this email already exists.";
    public static string FirstNameRequired => ResourceManager.GetString(nameof(FirstNameRequired), Culture) ?? "First name is required.";
    public static string LastNameRequired => ResourceManager.GetString(nameof(LastNameRequired), Culture) ?? "Last name is required.";

    public static string ForgotPasswordTitle => ResourceManager.GetString(nameof(ForgotPasswordTitle), Culture) ?? "Forgot password";
    public static string ForgotPasswordDescription => ResourceManager.GetString(nameof(ForgotPasswordDescription), Culture) ?? "Enter your email and we will send you password reset instructions if an account exists.";
    public static string ForgotPasswordButton => ResourceManager.GetString(nameof(ForgotPasswordButton), Culture) ?? "Forgot password?";
    public static string ForgotPasswordSendButton => ResourceManager.GetString(nameof(ForgotPasswordSendButton), Culture) ?? "Send reset instructions";
    public static string ForgotPasswordSuccess => ResourceManager.GetString(nameof(ForgotPasswordSuccess), Culture) ?? "If the email exists, password reset instructions have been sent.";
    public static string ForgotPasswordFailed => ResourceManager.GetString(nameof(ForgotPasswordFailed), Culture) ?? "Unable to request password reset right now. Please try again.";

    public static string RegisterAutoLoginFailed => ResourceManager.GetString(nameof(RegisterAutoLoginFailed), Culture) ?? "Your account was created, but automatic sign-in failed. Please login manually.";

    public static string DiscoverMyBusinessesTab => ResourceManager.GetString(nameof(DiscoverMyBusinessesTab), Culture) ?? "My Businesses";
    public static string DiscoverExploreTab => ResourceManager.GetString(nameof(DiscoverExploreTab), Culture) ?? "Explore";
    public static string DiscoverMyBusinessesHeadline => ResourceManager.GetString(nameof(DiscoverMyBusinessesHeadline), Culture) ?? "Your joined businesses";
    public static string DiscoverExploreHeadline => ResourceManager.GetString(nameof(DiscoverExploreHeadline), Culture) ?? "Explore businesses";
    public static string DiscoverSearchPlaceholder => ResourceManager.GetString(nameof(DiscoverSearchPlaceholder), Culture) ?? "Search by business name or address";
    public static string DiscoverMyBusinessesEmpty => ResourceManager.GetString(nameof(DiscoverMyBusinessesEmpty), Culture) ?? "You have not joined any business yet. Use Explore to join one.";
    public static string DiscoverExploreEmpty => ResourceManager.GetString(nameof(DiscoverExploreEmpty), Culture) ?? "No businesses found for the current search.";
    public static string DiscoverOpenQrButton => ResourceManager.GetString(nameof(DiscoverOpenQrButton), Culture) ?? "Open QR";
    public static string DiscoverOpenRewardsButton => ResourceManager.GetString(nameof(DiscoverOpenRewardsButton), Culture) ?? "Open Rewards";
    public static string DiscoverAlreadyJoinedLabel => ResourceManager.GetString(nameof(DiscoverAlreadyJoinedLabel), Culture) ?? "Already joined";
    public static string DiscoverLoadJoinedFailed => ResourceManager.GetString(nameof(DiscoverLoadJoinedFailed), Culture) ?? "Unable to load your joined businesses.";
    public static string DiscoverLoadExploreFailed => ResourceManager.GetString(nameof(DiscoverLoadExploreFailed), Culture) ?? "Unable to load explore businesses.";

    
    public static string DiscoverCategoryFilterLabel => ResourceManager.GetString(nameof(DiscoverCategoryFilterLabel), Culture) ?? "Category";
    public static string DiscoverSearchButton => ResourceManager.GetString(nameof(DiscoverSearchButton), Culture) ?? "Search";
    public static string DiscoverClearFiltersButton => ResourceManager.GetString(nameof(DiscoverClearFiltersButton), Culture) ?? "Clear";
    public static string DiscoverNearbyOnlyLabel => ResourceManager.GetString(nameof(DiscoverNearbyOnlyLabel), Culture) ?? "Nearby only";
    public static string DiscoverLocationUnavailable => ResourceManager.GetString(nameof(DiscoverLocationUnavailable), Culture) ?? "Location is unavailable. Showing broad results instead.";
    public static string DiscoverNearbyRadiusLabel => ResourceManager.GetString(nameof(DiscoverNearbyRadiusLabel), Culture) ?? "Radius";
    public static string FeedTitle => ResourceManager.GetString(nameof(FeedTitle), Culture) ?? "Feed";
    public static string FeedEmptyMessage => ResourceManager.GetString(nameof(FeedEmptyMessage), Culture) ?? "No feed items yet.";
    public static string FeedLoadFailed => ResourceManager.GetString(nameof(FeedLoadFailed), Culture) ?? "Unable to load feed right now.";
    public static string FeedLoadMoreButton => ResourceManager.GetString(nameof(FeedLoadMoreButton), Culture) ?? "Load more";
    public static string FeedNoAccountsMessage => ResourceManager.GetString(nameof(FeedNoAccountsMessage), Culture) ?? "Join a business first to see your feed.";
    public static string BusinessDetailsNotFound => ResourceManager.GetString(nameof(BusinessDetailsNotFound), Culture) ?? "Business not found.";
    public static string BusinessDetailsLoadFailed => ResourceManager.GetString(nameof(BusinessDetailsLoadFailed), Culture) ?? "Unable to load business details.";
    public static string BusinessEngagementTitle => ResourceManager.GetString(nameof(BusinessEngagementTitle), Culture) ?? "Engagement";
    public static string BusinessEngagementLoadFailed => ResourceManager.GetString(nameof(BusinessEngagementLoadFailed), Culture) ?? "Unable to load engagement details.";
    public static string BusinessLikeButton => ResourceManager.GetString(nameof(BusinessLikeButton), Culture) ?? "Like / Unlike";
    public static string BusinessFavoriteButton => ResourceManager.GetString(nameof(BusinessFavoriteButton), Culture) ?? "Favorite / Unfavorite";
    public static string BusinessLikeToggleFailed => ResourceManager.GetString(nameof(BusinessLikeToggleFailed), Culture) ?? "Unable to update like state.";
    public static string BusinessFavoriteToggleFailed => ResourceManager.GetString(nameof(BusinessFavoriteToggleFailed), Culture) ?? "Unable to update favorite state.";
    public static string BusinessMyReviewTitle => ResourceManager.GetString(nameof(BusinessMyReviewTitle), Culture) ?? "My review";
    public static string BusinessMyReviewRatingLabel => ResourceManager.GetString(nameof(BusinessMyReviewRatingLabel), Culture) ?? "Rating";
    public static string BusinessMyReviewCommentPlaceholder => ResourceManager.GetString(nameof(BusinessMyReviewCommentPlaceholder), Culture) ?? "Write your feedback (optional).";
    public static string BusinessSaveReviewButton => ResourceManager.GetString(nameof(BusinessSaveReviewButton), Culture) ?? "Save review";
    public static string BusinessReviewSaveFailed => ResourceManager.GetString(nameof(BusinessReviewSaveFailed), Culture) ?? "Unable to save your review.";
    public static string BusinessRecentReviewsTitle => ResourceManager.GetString(nameof(BusinessRecentReviewsTitle), Culture) ?? "Recent reviews";
    public static string BusinessJoinFailed => ResourceManager.GetString(nameof(BusinessJoinFailed), Culture) ?? "Unable to join the loyalty program.";
    public static string BusinessScanSessionPrepareFailed => ResourceManager.GetString(nameof(BusinessScanSessionPrepareFailed), Culture) ?? "Unable to create scan session.";
}
