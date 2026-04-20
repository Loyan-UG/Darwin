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
    public static string HomeGreeting => ResourceManager.GetString(nameof(HomeGreeting), Culture) ?? "Welcome to Darwin";
    public static string StartButton => ResourceManager.GetString(nameof(StartButton), Culture) ?? "Start";
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
    public static string EmailPlaceholder =>
        ResourceManager.GetString(nameof(EmailPlaceholder), Culture) ?? "user@example.com";
    public static string PasswordLabel =>
        ResourceManager.GetString(nameof(PasswordLabel), Culture) ?? "Password";
    public static string LoginButton =>
        ResourceManager.GetString(nameof(LoginButton), Culture) ?? "Sign in";
    public static string LoginEmailPlaceholder =>
        ResourceManager.GetString(nameof(LoginEmailPlaceholder), Culture) ?? "user@example.com";
    public static string LoginPasswordPlaceholder =>
        ResourceManager.GetString(nameof(LoginPasswordPlaceholder), Culture) ?? "••••••";
    public static string InvalidCredentials =>
        ResourceManager.GetString(nameof(InvalidCredentials), Culture) ?? "Invalid email or password.";
    public static string LoginReadinessBusy =>
        ResourceManager.GetString(nameof(LoginReadinessBusy), Culture) ?? "Signing you in and restoring your customer session...";
    public static string LoginReadinessEmail =>
        ResourceManager.GetString(nameof(LoginReadinessEmail), Culture) ?? "Enter your email address to continue.";
    public static string LoginReadinessPassword =>
        ResourceManager.GetString(nameof(LoginReadinessPassword), Culture) ?? "Enter your password to continue.";
    public static string LoginReadinessReady =>
        ResourceManager.GetString(nameof(LoginReadinessReady), Culture) ?? "Your sign-in details are ready. You can continue now.";
    public static string LoginEmailConfirmationRequired =>
        ResourceManager.GetString(nameof(LoginEmailConfirmationRequired), Culture) ?? "Please confirm your email address before signing in.";
    public static string LoginAccountLocked =>
        ResourceManager.GetString(nameof(LoginAccountLocked), Culture) ?? "Your account is currently locked. Please contact support.";
    public static string RequestActivationEmailButton =>
        ResourceManager.GetString(nameof(RequestActivationEmailButton), Culture) ?? "Send activation email";
    public static string ActivationEmailSent =>
        ResourceManager.GetString(nameof(ActivationEmailSent), Culture) ?? "Confirmation email sent. Please check your inbox.";
    public static string ActivationEmailRequestFailed =>
        ResourceManager.GetString(nameof(ActivationEmailRequestFailed), Culture) ?? "Unable to send a confirmation email right now. Please try again.";
    public static string ActivationTitle =>
        ResourceManager.GetString(nameof(ActivationTitle), Culture) ?? "Email activation";
    public static string ActivationDescription =>
        ResourceManager.GetString(nameof(ActivationDescription), Culture) ?? "Request another confirmation email or complete account activation with your email and token.";
    public static string ActivationTokenLabel =>
        ResourceManager.GetString(nameof(ActivationTokenLabel), Culture) ?? "Activation token";
    public static string ActivationTokenPlaceholder =>
        ResourceManager.GetString(nameof(ActivationTokenPlaceholder), Culture) ?? "Paste token from your email";
    public static string ActivationRequestButton =>
        ResourceManager.GetString(nameof(ActivationRequestButton), Culture) ?? "Send activation email";
    public static string ActivationConfirmButton =>
        ResourceManager.GetString(nameof(ActivationConfirmButton), Culture) ?? "Confirm email";
    public static string ActivationConfirmSuccess =>
        ResourceManager.GetString(nameof(ActivationConfirmSuccess), Culture) ?? "Email confirmed. You can sign in now.";
    public static string ActivationConfirmFailed =>
        ResourceManager.GetString(nameof(ActivationConfirmFailed), Culture) ?? "Email confirmation could not be completed. Please check the token and try again.";
    public static string ActivationEmailTokenRequired =>
        ResourceManager.GetString(nameof(ActivationEmailTokenRequired), Culture) ?? "Email and activation token are required.";
    public static string ActivationOpenFlowButton =>
        ResourceManager.GetString(nameof(ActivationOpenFlowButton), Culture) ?? "Open activation flow";
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
    public static string RewardsNextRewardSectionTitle => ResourceManager.GetString(nameof(RewardsNextRewardSectionTitle), Culture) ?? "Next reward progress";
    public static string RewardsAvailableRewardsCountFormat => ResourceManager.GetString(nameof(RewardsAvailableRewardsCountFormat), Culture) ?? "Configured rewards: {0}";
    public static string RewardsCurrentPointsTitle => ResourceManager.GetString(nameof(RewardsCurrentPointsTitle), Culture) ?? "Current points";
    public static string RewardsJoinedBusinessesTitle => ResourceManager.GetString(nameof(RewardsJoinedBusinessesTitle), Culture) ?? "Joined businesses";
    public static string RewardsTotalPointsTitle => ResourceManager.GetString(nameof(RewardsTotalPointsTitle), Culture) ?? "Total points";
    public static string RewardsConfiguredRewardsTitle => ResourceManager.GetString(nameof(RewardsConfiguredRewardsTitle), Culture) ?? "Configured rewards";
    public static string RewardsRedeemableRewardsTitle => ResourceManager.GetString(nameof(RewardsRedeemableRewardsTitle), Culture) ?? "Redeemable rewards";
    public static string RewardsRedeemableRewardsCountFormat => ResourceManager.GetString(nameof(RewardsRedeemableRewardsCountFormat), Culture) ?? "Redeemable rewards: {0}";
    public static string RewardsNextRewardNameFormat => ResourceManager.GetString(nameof(RewardsNextRewardNameFormat), Culture) ?? "Next reward: {0}";
    public static string RewardsNextRewardRemainingFormat => ResourceManager.GetString(nameof(RewardsNextRewardRemainingFormat), Culture) ?? "Remaining points: {0}";
    public static string RewardsNextRewardThresholdFormat => ResourceManager.GetString(nameof(RewardsNextRewardThresholdFormat), Culture) ?? "Threshold: {0} pts";
    public static string RewardsNextRewardProgressFormat => ResourceManager.GetString(nameof(RewardsNextRewardProgressFormat), Culture) ?? "Progress: {0}%";
    public static string RewardsAllRewardsUnlockedMessage => ResourceManager.GetString(nameof(RewardsAllRewardsUnlockedMessage), Culture) ?? "All currently configured rewards are already unlocked for this business.";
    public static string RewardsExpiryTrackingDisabled => ResourceManager.GetString(nameof(RewardsExpiryTrackingDisabled), Culture) ?? "Point expiry tracking is not enabled for this loyalty program yet.";
    public static string RewardsExpiryTrackingEnabled => ResourceManager.GetString(nameof(RewardsExpiryTrackingEnabled), Culture) ?? "Point expiry tracking is enabled for this loyalty program.";
    public static string RewardsPointsExpiringSoonFormat => ResourceManager.GetString(nameof(RewardsPointsExpiringSoonFormat), Culture) ?? "Points expiring soon: {0}";
    public static string RewardsNextPointsExpiryFormat => ResourceManager.GetString(nameof(RewardsNextPointsExpiryFormat), Culture) ?? "Next expiry: {0:yyyy-MM-dd HH:mm}";
    public static string BusinessCategoryFormat => ResourceManager.GetString(nameof(BusinessCategoryFormat), Culture) ?? "Category: {0}";

    public static string RewardsOverviewTitle => ResourceManager.GetString(nameof(RewardsOverviewTitle), Culture) ?? "Multi-business overview";
    public static string RewardsOverviewJoinedBusinessesFormat => ResourceManager.GetString(nameof(RewardsOverviewJoinedBusinessesFormat), Culture) ?? "Joined businesses: {0}";
    public static string RewardsOverviewTotalPointsFormat => ResourceManager.GetString(nameof(RewardsOverviewTotalPointsFormat), Culture) ?? "Total points across businesses: {0}";
    public static string RewardsOverviewTopBusinessFormat => ResourceManager.GetString(nameof(RewardsOverviewTopBusinessFormat), Culture) ?? "Top business by points: {0}";
    public static string RewardsOpenSelectedBusinessQrButton => ResourceManager.GetString(nameof(RewardsOpenSelectedBusinessQrButton), Culture) ?? "Open selected business QR";
    public static string BusinessActiveFormat => ResourceManager.GetString(nameof(BusinessActiveFormat), Culture) ?? "Active: {0}";

    public static string BusinessDetailsTitle => ResourceManager.GetString(nameof(BusinessDetailsTitle), Culture) ?? "Business";
    public static string BusinessDetailsPlaceholder => ResourceManager.GetString(nameof(BusinessDetailsPlaceholder), Culture) ?? "Additional details coming soon.";
    public static string JoinProgramButton => ResourceManager.GetString(nameof(JoinProgramButton), Culture) ?? "Join Loyalty Program";
    public static string LogoutButtonText => ResourceManager.GetString(nameof(LogoutButtonText), Culture) ?? "Logout";

    public static string ProfileSectionTitle => ResourceManager.GetString(nameof(ProfileSectionTitle), Culture) ?? "My profile";
    public static string FirstNameLabel => ResourceManager.GetString(nameof(FirstNameLabel), Culture) ?? "First name";
    public static string FirstNamePlaceholder => ResourceManager.GetString(nameof(FirstNamePlaceholder), Culture) ?? "Enter your first name";
    public static string LastNameLabel => ResourceManager.GetString(nameof(LastNameLabel), Culture) ?? "Last name";
    public static string LastNamePlaceholder => ResourceManager.GetString(nameof(LastNamePlaceholder), Culture) ?? "Enter your last name";
    public static string PhoneLabel => ResourceManager.GetString(nameof(PhoneLabel), Culture) ?? "Phone";
    public static string LocaleLabel => ResourceManager.GetString(nameof(LocaleLabel), Culture) ?? "Locale";
    public static string TimezoneLabel => ResourceManager.GetString(nameof(TimezoneLabel), Culture) ?? "Timezone";
    public static string CurrencyLabel => ResourceManager.GetString(nameof(CurrencyLabel), Culture) ?? "Currency";
    public static string SaveProfileButton => ResourceManager.GetString(nameof(SaveProfileButton), Culture) ?? "Save profile";
    public static string ProfilePushRegistrationSectionTitle => ResourceManager.GetString(nameof(ProfilePushRegistrationSectionTitle), Culture) ?? "Push registration";
    public static string ProfilePushRegistrationSyncButton => ResourceManager.GetString(nameof(ProfilePushRegistrationSyncButton), Culture) ?? "Sync push registration";
    public static string ProfilePushRegistrationStatusIdle => ResourceManager.GetString(nameof(ProfilePushRegistrationStatusIdle), Culture) ?? "Push registration has not been synced yet.";
    public static string ProfilePushRegistrationStatusSuccess => ResourceManager.GetString(nameof(ProfilePushRegistrationStatusSuccess), Culture) ?? "Push registration synced successfully.";
    public static string ProfilePushRegistrationStatusFailed => ResourceManager.GetString(nameof(ProfilePushRegistrationStatusFailed), Culture) ?? "Push registration sync failed.";
    public static string ProfilePushRegistrationLastSyncFormat => ResourceManager.GetString(nameof(ProfilePushRegistrationLastSyncFormat), Culture) ?? "Last sync: {0}";
    public static string ProfilePushOpenSettingsButton => ResourceManager.GetString(nameof(ProfilePushOpenSettingsButton), Culture) ?? "Open notification settings";
    public static string ProfilePushOpenSettingsHint => ResourceManager.GetString(nameof(ProfilePushOpenSettingsHint), Culture) ?? "If notifications are blocked, open system settings and allow notifications for Loyan.";
    public static string ProfilePushOpenSettingsFailed => ResourceManager.GetString(nameof(ProfilePushOpenSettingsFailed), Culture) ?? "Could not open notification settings. Please open app settings manually.";
    public static string ProfilePushPermissionEnabled => ResourceManager.GetString(nameof(ProfilePushPermissionEnabled), Culture) ?? "Notifications permission: enabled";
    public static string ProfilePushPermissionDisabled => ResourceManager.GetString(nameof(ProfilePushPermissionDisabled), Culture) ?? "Notifications permission: disabled";
    public static string ProfilePushPermissionUnknown => ResourceManager.GetString(nameof(ProfilePushPermissionUnknown), Culture) ?? "Notifications permission: unknown";
    public static string ProfilePushTokenAvailabilityReady => ResourceManager.GetString(nameof(ProfilePushTokenAvailabilityReady), Culture) ?? "Push token status: ready";
    public static string ProfilePushTokenAvailabilityMissing => ResourceManager.GetString(nameof(ProfilePushTokenAvailabilityMissing), Culture) ?? "Push token status: missing";
    public static string ProfilePushTokenAvailabilityUnknown => ResourceManager.GetString(nameof(ProfilePushTokenAvailabilityUnknown), Culture) ?? "Push token status: unknown";
    public static string ProfileAddressesSectionTitle => ResourceManager.GetString(nameof(ProfileAddressesSectionTitle), Culture) ?? "Saved addresses";
    public static string ProfileAddressCountFormat => ResourceManager.GetString(nameof(ProfileAddressCountFormat), Culture) ?? "Saved addresses: {0}";
    public static string ProfileSavedAddressesTitle => ResourceManager.GetString(nameof(ProfileSavedAddressesTitle), Culture) ?? "Saved addresses";
    public static string ProfileDefaultBillingAddressFormat => ResourceManager.GetString(nameof(ProfileDefaultBillingAddressFormat), Culture) ?? "Default billing: {0}";
    public static string ProfileDefaultShippingAddressFormat => ResourceManager.GetString(nameof(ProfileDefaultShippingAddressFormat), Culture) ?? "Default shipping: {0}";
    public static string ProfileManageAddressesButton => ResourceManager.GetString(nameof(ProfileManageAddressesButton), Culture) ?? "Manage addresses";
    public static string ProfileManagePreferencesButton => ResourceManager.GetString(nameof(ProfileManagePreferencesButton), Culture) ?? "Manage preferences";
    public static string ProfileCustomerContextSectionTitle => ResourceManager.GetString(nameof(ProfileCustomerContextSectionTitle), Culture) ?? "Customer context";
    public static string ProfileCustomerContextEmpty => ResourceManager.GetString(nameof(ProfileCustomerContextEmpty), Culture) ?? "No linked CRM customer context is available yet.";
    public static string ProfileCustomerDisplayNameFormat => ResourceManager.GetString(nameof(ProfileCustomerDisplayNameFormat), Culture) ?? "Customer: {0}";
    public static string ProfileCustomerCompanyFormat => ResourceManager.GetString(nameof(ProfileCustomerCompanyFormat), Culture) ?? "Company: {0}";
    public static string ProfileCustomerSegmentsFormat => ResourceManager.GetString(nameof(ProfileCustomerSegmentsFormat), Culture) ?? "Segments: {0}";
    public static string ProfileCustomerLastInteractionFormat => ResourceManager.GetString(nameof(ProfileCustomerLastInteractionFormat), Culture) ?? "Last interaction: {0:yyyy-MM-dd HH:mm}";
    public static string ProfileCustomerNoInteractions => ResourceManager.GetString(nameof(ProfileCustomerNoInteractions), Culture) ?? "No CRM interactions recorded yet.";
    public static string ProfileViewCustomerContextButton => ResourceManager.GetString(nameof(ProfileViewCustomerContextButton), Culture) ?? "Open customer details";
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
    public static string SettingsOrdersAndInvoicesButton => ResourceManager.GetString(nameof(SettingsOrdersAndInvoicesButton), Culture) ?? "Orders & invoices";
    public static string SettingsPreferencesButton => ResourceManager.GetString(nameof(SettingsPreferencesButton), Culture) ?? "Privacy & preferences";
    public static string SettingsChangePasswordButton => ResourceManager.GetString(nameof(SettingsChangePasswordButton), Culture) ?? "Change password";
    public static string MemberPreferencesTitle => ResourceManager.GetString(nameof(MemberPreferencesTitle), Culture) ?? "Privacy & preferences";
    public static string MemberPreferencesSubtitle => ResourceManager.GetString(nameof(MemberPreferencesSubtitle), Culture) ?? "Manage your backend-backed communication and analytics preferences.";
    public static string MemberPreferencesRefreshButton => ResourceManager.GetString(nameof(MemberPreferencesRefreshButton), Culture) ?? "Refresh preferences";
    public static string MemberPreferencesMarketingConsentLabel => ResourceManager.GetString(nameof(MemberPreferencesMarketingConsentLabel), Culture) ?? "Allow marketing communication";
    public static string MemberPreferencesEmailLabel => ResourceManager.GetString(nameof(MemberPreferencesEmailLabel), Culture) ?? "Allow promotional email";
    public static string MemberPreferencesSmsLabel => ResourceManager.GetString(nameof(MemberPreferencesSmsLabel), Culture) ?? "Allow promotional SMS";
    public static string MemberPreferencesWhatsAppLabel => ResourceManager.GetString(nameof(MemberPreferencesWhatsAppLabel), Culture) ?? "Allow promotional WhatsApp";
    public static string MemberPreferencesPromotionalPushLabel => ResourceManager.GetString(nameof(MemberPreferencesPromotionalPushLabel), Culture) ?? "Allow promotional push notifications";
    public static string MemberPreferencesAnalyticsLabel => ResourceManager.GetString(nameof(MemberPreferencesAnalyticsLabel), Culture) ?? "Allow optional analytics tracking";
    public static string MemberPreferencesTermsAcceptedFormat => ResourceManager.GetString(nameof(MemberPreferencesTermsAcceptedFormat), Culture) ?? "Terms accepted at: {0:yyyy-MM-dd HH:mm}";
    public static string MemberPreferencesLoadFailed => ResourceManager.GetString(nameof(MemberPreferencesLoadFailed), Culture) ?? "Unable to load privacy preferences.";
    public static string MemberPreferencesSaveFailed => ResourceManager.GetString(nameof(MemberPreferencesSaveFailed), Culture) ?? "Unable to save privacy preferences right now.";
    public static string MemberPreferencesSaved => ResourceManager.GetString(nameof(MemberPreferencesSaved), Culture) ?? "Privacy preferences updated successfully.";
    public static string MemberPreferencesSaveButton => ResourceManager.GetString(nameof(MemberPreferencesSaveButton), Culture) ?? "Save preferences";
    public static string MemberCustomerContextTitle => ResourceManager.GetString(nameof(MemberCustomerContextTitle), Culture) ?? "Customer details";
    public static string MemberCustomerContextSubtitle => ResourceManager.GetString(nameof(MemberCustomerContextSubtitle), Culture) ?? "Review your linked CRM profile, segments, consent history, and recent interactions.";
    public static string MemberCustomerContextRefreshButton => ResourceManager.GetString(nameof(MemberCustomerContextRefreshButton), Culture) ?? "Refresh customer details";
    public static string MemberCustomerContextEmpty => ResourceManager.GetString(nameof(MemberCustomerContextEmpty), Culture) ?? "No linked CRM customer context is available for this account yet.";
    public static string MemberCustomerContextSummarySectionTitle => ResourceManager.GetString(nameof(MemberCustomerContextSummarySectionTitle), Culture) ?? "Customer summary";
    public static string MemberCustomerContextSegmentsSectionTitle => ResourceManager.GetString(nameof(MemberCustomerContextSegmentsSectionTitle), Culture) ?? "Segments";
    public static string MemberCustomerContextConsentsSectionTitle => ResourceManager.GetString(nameof(MemberCustomerContextConsentsSectionTitle), Culture) ?? "Consent history";
    public static string MemberCustomerContextInteractionsSectionTitle => ResourceManager.GetString(nameof(MemberCustomerContextInteractionsSectionTitle), Culture) ?? "Recent interactions";
    public static string MemberCustomerContextEmailFormat => ResourceManager.GetString(nameof(MemberCustomerContextEmailFormat), Culture) ?? "Email: {0}";
    public static string MemberCustomerContextPhoneFormat => ResourceManager.GetString(nameof(MemberCustomerContextPhoneFormat), Culture) ?? "Phone: {0}";
    public static string MemberCustomerContextCompanyFormat => ResourceManager.GetString(nameof(MemberCustomerContextCompanyFormat), Culture) ?? "Company: {0}";
    public static string MemberCustomerContextNotesFormat => ResourceManager.GetString(nameof(MemberCustomerContextNotesFormat), Culture) ?? "Notes: {0}";
    public static string MemberCustomerContextCreatedAtFormat => ResourceManager.GetString(nameof(MemberCustomerContextCreatedAtFormat), Culture) ?? "Customer since: {0:yyyy-MM-dd HH:mm}";
    public static string MemberCustomerContextLastInteractionFormat => ResourceManager.GetString(nameof(MemberCustomerContextLastInteractionFormat), Culture) ?? "Last interaction: {0:yyyy-MM-dd HH:mm}";
    public static string MemberCustomerContextInteractionCountFormat => ResourceManager.GetString(nameof(MemberCustomerContextInteractionCountFormat), Culture) ?? "Interactions recorded: {0}";
    public static string MemberCustomerContextNoSegments => ResourceManager.GetString(nameof(MemberCustomerContextNoSegments), Culture) ?? "No CRM segments are currently linked.";
    public static string MemberCustomerContextNoConsents => ResourceManager.GetString(nameof(MemberCustomerContextNoConsents), Culture) ?? "No consent history is available yet.";
    public static string MemberCustomerContextNoInteractions => ResourceManager.GetString(nameof(MemberCustomerContextNoInteractions), Culture) ?? "No CRM interactions are available yet.";
    public static string MemberCustomerContextLoadFailed => ResourceManager.GetString(nameof(MemberCustomerContextLoadFailed), Culture) ?? "Unable to load customer details right now.";
    public static string MemberCustomerContextConsentStatusGranted => ResourceManager.GetString(nameof(MemberCustomerContextConsentStatusGranted), Culture) ?? "Granted";
    public static string MemberCustomerContextConsentStatusRevoked => ResourceManager.GetString(nameof(MemberCustomerContextConsentStatusRevoked), Culture) ?? "Revoked";
    public static string MemberCustomerContextConsentGrantedAtFormat => ResourceManager.GetString(nameof(MemberCustomerContextConsentGrantedAtFormat), Culture) ?? "Granted at: {0:yyyy-MM-dd HH:mm}";
    public static string MemberCustomerContextConsentRevokedAtFormat => ResourceManager.GetString(nameof(MemberCustomerContextConsentRevokedAtFormat), Culture) ?? "Revoked at: {0:yyyy-MM-dd HH:mm}";
    public static string MemberCustomerContextInteractionTypeChannelFormat => ResourceManager.GetString(nameof(MemberCustomerContextInteractionTypeChannelFormat), Culture) ?? "{0} via {1}";
    public static string MemberCustomerContextInteractionSubjectFormat => ResourceManager.GetString(nameof(MemberCustomerContextInteractionSubjectFormat), Culture) ?? "Subject: {0}";
    public static string MemberCustomerContextInteractionCreatedAtFormat => ResourceManager.GetString(nameof(MemberCustomerContextInteractionCreatedAtFormat), Culture) ?? "Created at: {0:yyyy-MM-dd HH:mm}";
    public static string MemberCommerceTitle => ResourceManager.GetString(nameof(MemberCommerceTitle), Culture) ?? "Orders & invoices";
    public static string MemberCommerceSubtitle => ResourceManager.GetString(nameof(MemberCommerceSubtitle), Culture) ?? "Review recent member orders and invoices, retry payments, and copy text documents.";
    public static string MemberCommerceRefreshButton => ResourceManager.GetString(nameof(MemberCommerceRefreshButton), Culture) ?? "Refresh history";
    public static string MemberCommerceOrdersSectionTitle => ResourceManager.GetString(nameof(MemberCommerceOrdersSectionTitle), Culture) ?? "Orders";
    public static string MemberCommerceOrdersEmpty => ResourceManager.GetString(nameof(MemberCommerceOrdersEmpty), Culture) ?? "No member orders are available yet.";
    public static string MemberCommerceInvoicesSectionTitle => ResourceManager.GetString(nameof(MemberCommerceInvoicesSectionTitle), Culture) ?? "Invoices";
    public static string MemberCommerceInvoicesEmpty => ResourceManager.GetString(nameof(MemberCommerceInvoicesEmpty), Culture) ?? "No member invoices are available yet.";
    public static string MemberCommerceViewOrderButton => ResourceManager.GetString(nameof(MemberCommerceViewOrderButton), Culture) ?? "View order";
    public static string MemberCommerceViewInvoiceButton => ResourceManager.GetString(nameof(MemberCommerceViewInvoiceButton), Culture) ?? "View invoice";
    public static string MemberCommerceRetryPaymentButton => ResourceManager.GetString(nameof(MemberCommerceRetryPaymentButton), Culture) ?? "Retry payment";
    public static string MemberCommerceCopyDocumentButton => ResourceManager.GetString(nameof(MemberCommerceCopyDocumentButton), Culture) ?? "Copy document";
    public static string MemberCommerceOrderStatusFormat => ResourceManager.GetString(nameof(MemberCommerceOrderStatusFormat), Culture) ?? "Status: {0}";
    public static string MemberCommerceOrderCreatedFormat => ResourceManager.GetString(nameof(MemberCommerceOrderCreatedFormat), Culture) ?? "Created: {0:yyyy-MM-dd HH:mm}";
    public static string MemberCommerceOrderTotalFormat => ResourceManager.GetString(nameof(MemberCommerceOrderTotalFormat), Culture) ?? "Total: {0}";
    public static string MemberCommerceOrderShippingMethodFormat => ResourceManager.GetString(nameof(MemberCommerceOrderShippingMethodFormat), Culture) ?? "Shipping: {0}";
    public static string MemberCommerceOrderPaymentsCountFormat => ResourceManager.GetString(nameof(MemberCommerceOrderPaymentsCountFormat), Culture) ?? "Payments: {0}";
    public static string MemberCommerceOrderInvoicesCountFormat => ResourceManager.GetString(nameof(MemberCommerceOrderInvoicesCountFormat), Culture) ?? "Invoices: {0}";
    public static string MemberCommerceInvoiceStatusFormat => ResourceManager.GetString(nameof(MemberCommerceInvoiceStatusFormat), Culture) ?? "Status: {0}";
    public static string MemberCommerceInvoiceDueDateFormat => ResourceManager.GetString(nameof(MemberCommerceInvoiceDueDateFormat), Culture) ?? "Due: {0:yyyy-MM-dd HH:mm}";
    public static string MemberCommerceInvoiceTotalFormat => ResourceManager.GetString(nameof(MemberCommerceInvoiceTotalFormat), Culture) ?? "Total: {0}";
    public static string MemberCommerceInvoiceBalanceFormat => ResourceManager.GetString(nameof(MemberCommerceInvoiceBalanceFormat), Culture) ?? "Balance: {0}";
    public static string MemberCommerceInvoicePaymentSummaryFormat => ResourceManager.GetString(nameof(MemberCommerceInvoicePaymentSummaryFormat), Culture) ?? "Payment: {0}";
    public static string MemberCommerceCheckoutLaunchedFormat => ResourceManager.GetString(nameof(MemberCommerceCheckoutLaunchedFormat), Culture) ?? "Hosted checkout launched for {0}.";
    public static string MemberCommerceDocumentCopiedFormat => ResourceManager.GetString(nameof(MemberCommerceDocumentCopiedFormat), Culture) ?? "Document copied for {0}.";
    public static string MemberCommerceLoadFailed => ResourceManager.GetString(nameof(MemberCommerceLoadFailed), Culture) ?? "Unable to load order and invoice history.";
    public static string MemberCommerceOrderDetailLoadFailed => ResourceManager.GetString(nameof(MemberCommerceOrderDetailLoadFailed), Culture) ?? "Unable to load order detail.";
    public static string MemberCommerceInvoiceDetailLoadFailed => ResourceManager.GetString(nameof(MemberCommerceInvoiceDetailLoadFailed), Culture) ?? "Unable to load invoice detail.";
    public static string MemberCommercePaymentIntentFailed => ResourceManager.GetString(nameof(MemberCommercePaymentIntentFailed), Culture) ?? "Unable to start a payment retry right now.";
    public static string MemberCommerceDocumentDownloadFailed => ResourceManager.GetString(nameof(MemberCommerceDocumentDownloadFailed), Culture) ?? "Unable to download the document right now.";
    public static string MemberAddressesTitle => ResourceManager.GetString(nameof(MemberAddressesTitle), Culture) ?? "Address book";
    public static string MemberAddressesRefreshButton => ResourceManager.GetString(nameof(MemberAddressesRefreshButton), Culture) ?? "Refresh addresses";
    public static string MemberAddressesCreateButton => ResourceManager.GetString(nameof(MemberAddressesCreateButton), Culture) ?? "New address";
    public static string MemberAddressesSavedSectionTitle => ResourceManager.GetString(nameof(MemberAddressesSavedSectionTitle), Culture) ?? "Saved addresses";
    public static string MemberAddressesEmpty => ResourceManager.GetString(nameof(MemberAddressesEmpty), Culture) ?? "No addresses are saved yet.";
    public static string MemberAddressesDefaultBillingBadge => ResourceManager.GetString(nameof(MemberAddressesDefaultBillingBadge), Culture) ?? "Default billing";
    public static string MemberAddressesDefaultShippingBadge => ResourceManager.GetString(nameof(MemberAddressesDefaultShippingBadge), Culture) ?? "Default shipping";
    public static string MemberAddressesEditButton => ResourceManager.GetString(nameof(MemberAddressesEditButton), Culture) ?? "Edit";
    public static string MemberAddressesDeleteButton => ResourceManager.GetString(nameof(MemberAddressesDeleteButton), Culture) ?? "Delete";
    public static string MemberAddressesSetBillingButton => ResourceManager.GetString(nameof(MemberAddressesSetBillingButton), Culture) ?? "Set billing";
    public static string MemberAddressesSetShippingButton => ResourceManager.GetString(nameof(MemberAddressesSetShippingButton), Culture) ?? "Set shipping";
    public static string MemberAddressesEditorTitleCreate => ResourceManager.GetString(nameof(MemberAddressesEditorTitleCreate), Culture) ?? "Create address";
    public static string MemberAddressesEditorTitleEdit => ResourceManager.GetString(nameof(MemberAddressesEditorTitleEdit), Culture) ?? "Edit address";
    public static string MemberAddressesFullNameLabel => ResourceManager.GetString(nameof(MemberAddressesFullNameLabel), Culture) ?? "Full name";
    public static string MemberAddressesCompanyLabel => ResourceManager.GetString(nameof(MemberAddressesCompanyLabel), Culture) ?? "Company";
    public static string MemberAddressesStreet1Label => ResourceManager.GetString(nameof(MemberAddressesStreet1Label), Culture) ?? "Street line 1";
    public static string MemberAddressesStreet2Label => ResourceManager.GetString(nameof(MemberAddressesStreet2Label), Culture) ?? "Street line 2";
    public static string MemberAddressesPostalCodeLabel => ResourceManager.GetString(nameof(MemberAddressesPostalCodeLabel), Culture) ?? "Postal code";
    public static string MemberAddressesCityLabel => ResourceManager.GetString(nameof(MemberAddressesCityLabel), Culture) ?? "City";
    public static string MemberAddressesStateLabel => ResourceManager.GetString(nameof(MemberAddressesStateLabel), Culture) ?? "State / region";
    public static string MemberAddressesCountryCodeLabel => ResourceManager.GetString(nameof(MemberAddressesCountryCodeLabel), Culture) ?? "Country code";
    public static string MemberAddressesPhoneLabel => ResourceManager.GetString(nameof(MemberAddressesPhoneLabel), Culture) ?? "Phone";
    public static string MemberAddressesDefaultBillingSwitchLabel => ResourceManager.GetString(nameof(MemberAddressesDefaultBillingSwitchLabel), Culture) ?? "Use as default billing address";
    public static string MemberAddressesDefaultShippingSwitchLabel => ResourceManager.GetString(nameof(MemberAddressesDefaultShippingSwitchLabel), Culture) ?? "Use as default shipping address";
    public static string MemberAddressesSaveButton => ResourceManager.GetString(nameof(MemberAddressesSaveButton), Culture) ?? "Save address";
    public static string MemberAddressesCancelButton => ResourceManager.GetString(nameof(MemberAddressesCancelButton), Culture) ?? "Cancel";
    public static string MemberAddressesLoadFailed => ResourceManager.GetString(nameof(MemberAddressesLoadFailed), Culture) ?? "Unable to load addresses.";
    public static string MemberAddressesValidationFailed => ResourceManager.GetString(nameof(MemberAddressesValidationFailed), Culture) ?? "Please complete the required address fields before saving.";
    public static string MemberAddressesSaveFailed => ResourceManager.GetString(nameof(MemberAddressesSaveFailed), Culture) ?? "Unable to save the address right now.";
    public static string MemberAddressesDeleteFailed => ResourceManager.GetString(nameof(MemberAddressesDeleteFailed), Culture) ?? "Unable to delete the address right now.";
    public static string MemberAddressesDefaultFailed => ResourceManager.GetString(nameof(MemberAddressesDefaultFailed), Culture) ?? "Unable to update the default address right now.";
    public static string MemberAddressesCreated => ResourceManager.GetString(nameof(MemberAddressesCreated), Culture) ?? "Address created successfully.";
    public static string MemberAddressesUpdated => ResourceManager.GetString(nameof(MemberAddressesUpdated), Culture) ?? "Address updated successfully.";
    public static string MemberAddressesDeleted => ResourceManager.GetString(nameof(MemberAddressesDeleted), Culture) ?? "Address deleted successfully.";
    public static string MemberAddressesDefaultUpdated => ResourceManager.GetString(nameof(MemberAddressesDefaultUpdated), Culture) ?? "Default address updated successfully.";

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
    public static string ForgotPasswordReadinessBusy => ResourceManager.GetString(nameof(ForgotPasswordReadinessBusy), Culture) ?? "Preparing and sending your password reset instructions...";
    public static string ForgotPasswordReadinessEmail => ResourceManager.GetString(nameof(ForgotPasswordReadinessEmail), Culture) ?? "Enter your email address to receive reset instructions.";
    public static string ForgotPasswordReadinessReady => ResourceManager.GetString(nameof(ForgotPasswordReadinessReady), Culture) ?? "Your email is ready. Send the reset instructions when you are ready.";
    public static string ForgotPasswordButton => ResourceManager.GetString(nameof(ForgotPasswordButton), Culture) ?? "Forgot password?";
    public static string ForgotPasswordSendButton => ResourceManager.GetString(nameof(ForgotPasswordSendButton), Culture) ?? "Send reset instructions";
    public static string ForgotPasswordSuccess => ResourceManager.GetString(nameof(ForgotPasswordSuccess), Culture) ?? "If the email exists, password reset instructions have been sent.";
    public static string ForgotPasswordFailed => ResourceManager.GetString(nameof(ForgotPasswordFailed), Culture) ?? "Unable to request password reset right now. Please try again.";


    public static string ResetPasswordTitle => ResourceManager.GetString(nameof(ResetPasswordTitle), Culture) ?? "Reset password";
    public static string ResetPasswordDescription => ResourceManager.GetString(nameof(ResetPasswordDescription), Culture) ?? "Enter your email, reset token, and new password to complete the reset.";
    public static string ResetPasswordTokenLabel => ResourceManager.GetString(nameof(ResetPasswordTokenLabel), Culture) ?? "Reset token";
    public static string ResetPasswordTokenPlaceholder => ResourceManager.GetString(nameof(ResetPasswordTokenPlaceholder), Culture) ?? "Paste token from your email";
    public static string ResetPasswordTokenRequired => ResourceManager.GetString(nameof(ResetPasswordTokenRequired), Culture) ?? "Reset token is required.";
    public static string ResetPasswordActionButton => ResourceManager.GetString(nameof(ResetPasswordActionButton), Culture) ?? "Reset password";
    public static string ResetPasswordNavigateButton => ResourceManager.GetString(nameof(ResetPasswordNavigateButton), Culture) ?? "Already have a token? Reset password";
    public static string ResetPasswordSuccess => ResourceManager.GetString(nameof(ResetPasswordSuccess), Culture) ?? "Your password has been reset successfully. Please log in with your new password.";
    public static string ResetPasswordFailed => ResourceManager.GetString(nameof(ResetPasswordFailed), Culture) ?? "Unable to reset password. Please verify email/token and try again.";
    public static string ProfileConcurrencyConflict => ResourceManager.GetString(nameof(ProfileConcurrencyConflict), Culture) ?? "Your profile was updated elsewhere. Please refresh and try again.";

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
    public static string DiscoverLoadingMessage => ResourceManager.GetString(nameof(DiscoverLoadingMessage), Culture) ?? "Loading your businesses and nearby discovery results...";

    public static string DiscoverJoinedBusinessesCountFormat => ResourceManager.GetString(nameof(DiscoverJoinedBusinessesCountFormat), Culture) ?? "Joined businesses: {0}";
    public static string DiscoverJoinedBusinessesTitle => ResourceManager.GetString(nameof(DiscoverJoinedBusinessesTitle), Culture) ?? "Joined businesses";
    public static string DiscoverTotalPointsFormat => ResourceManager.GetString(nameof(DiscoverTotalPointsFormat), Culture) ?? "Total points: {0}";
    public static string DiscoverTotalPointsTitle => ResourceManager.GetString(nameof(DiscoverTotalPointsTitle), Culture) ?? "Total points";
    public static string DiscoverTopBalanceBusinessFormat => ResourceManager.GetString(nameof(DiscoverTopBalanceBusinessFormat), Culture) ?? "Top balance at: {0}";
    public static string DiscoverTopBusinessTitle => ResourceManager.GetString(nameof(DiscoverTopBusinessTitle), Culture) ?? "Top business";
    
    public static string DiscoverCategoryFilterLabel => ResourceManager.GetString(nameof(DiscoverCategoryFilterLabel), Culture) ?? "Category";
    public static string DiscoverSearchButton => ResourceManager.GetString(nameof(DiscoverSearchButton), Culture) ?? "Search";
    public static string DiscoverClearFiltersButton => ResourceManager.GetString(nameof(DiscoverClearFiltersButton), Culture) ?? "Clear";
    public static string DiscoverNearbyOnlyLabel => ResourceManager.GetString(nameof(DiscoverNearbyOnlyLabel), Culture) ?? "Nearby only";
    public static string DiscoverLocationUnavailable => ResourceManager.GetString(nameof(DiscoverLocationUnavailable), Culture) ?? "Location is unavailable. Showing broad results instead.";
    public static string DiscoverNearbyRadiusLabel => ResourceManager.GetString(nameof(DiscoverNearbyRadiusLabel), Culture) ?? "Radius";
    public static string DiscoverNearbyRadiusMetersFormat => ResourceManager.GetString(nameof(DiscoverNearbyRadiusMetersFormat), Culture) ?? "{0} m";
    public static string FeedTitle => ResourceManager.GetString(nameof(FeedTitle), Culture) ?? "Feed";
    public static string FeedEmptyMessage => ResourceManager.GetString(nameof(FeedEmptyMessage), Culture) ?? "No feed items yet.";
    public static string FeedLoadFailed => ResourceManager.GetString(nameof(FeedLoadFailed), Culture) ?? "Unable to load feed right now.";
    public static string FeedLoadMoreButton => ResourceManager.GetString(nameof(FeedLoadMoreButton), Culture) ?? "Load more";
    public static string FeedLoadingMessage => ResourceManager.GetString(nameof(FeedLoadingMessage), Culture) ?? "Preparing your loyalty timeline and promotions...";
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
    public static string BusinessCityFormat => ResourceManager.GetString(nameof(BusinessCityFormat), Culture) ?? "City: {0}";
    public static string BusinessWebsiteFormat => ResourceManager.GetString(nameof(BusinessWebsiteFormat), Culture) ?? "Website: {0}";
    public static string BusinessPhoneFormat => ResourceManager.GetString(nameof(BusinessPhoneFormat), Culture) ?? "Phone: {0}";
    public static string BusinessEmailFormat => ResourceManager.GetString(nameof(BusinessEmailFormat), Culture) ?? "Email: {0}";
    public static string BusinessInformationTitle => ResourceManager.GetString(nameof(BusinessInformationTitle), Culture) ?? "Business Information";
    public static string BusinessLoyaltyProgramTitle => ResourceManager.GetString(nameof(BusinessLoyaltyProgramTitle), Culture) ?? "Loyalty Program";
    public static string BusinessRewardTiersTitle => ResourceManager.GetString(nameof(BusinessRewardTiersTitle), Culture) ?? "Reward Tiers";
    public static string BusinessPointsFormat => ResourceManager.GetString(nameof(BusinessPointsFormat), Culture) ?? "Points: {0}";
    public static string BusinessRewardTypeFormat => ResourceManager.GetString(nameof(BusinessRewardTypeFormat), Culture) ?? "Type: {0}";
    public static string BusinessRewardValueFormat => ResourceManager.GetString(nameof(BusinessRewardValueFormat), Culture) ?? "Value: {0}";
    public static string BusinessSelfRedemptionFormat => ResourceManager.GetString(nameof(BusinessSelfRedemptionFormat), Culture) ?? "Self Redemption: {0}";
    public static string BusinessLikesCountFormat => ResourceManager.GetString(nameof(BusinessLikesCountFormat), Culture) ?? "Likes: {0}";
    public static string BusinessFavoritesCountFormat => ResourceManager.GetString(nameof(BusinessFavoritesCountFormat), Culture) ?? "Favorites: {0}";
    public static string BusinessRatingFormat => ResourceManager.GetString(nameof(BusinessRatingFormat), Culture) ?? "Rating: {0}";
    public static string BusinessReviewsCountFormat => ResourceManager.GetString(nameof(BusinessReviewsCountFormat), Culture) ?? "Reviews: {0}";
    public static string BusinessCurrentRatingFormat => ResourceManager.GetString(nameof(BusinessCurrentRatingFormat), Culture) ?? "Current rating: {0}/5";
    public static string BusinessReviewRatingFormat => ResourceManager.GetString(nameof(BusinessReviewRatingFormat), Culture) ?? "Rating: {0}/5";
    public static string FeedOpenQrButton => ResourceManager.GetString(nameof(FeedOpenQrButton), Culture) ?? "Open QR";
    public static string FeedOpenRewardsButton => ResourceManager.GetString(nameof(FeedOpenRewardsButton), Culture) ?? "Open Rewards";
    public static string FeedSelectedBusinessPointsFormat => ResourceManager.GetString(nameof(FeedSelectedBusinessPointsFormat), Culture) ?? "Points: {0}";
    public static string FeedPromotionsTitle => ResourceManager.GetString(nameof(FeedPromotionsTitle), Culture) ?? "Promotions for you";
    public static string FeedBusinessPickerLabel => ResourceManager.GetString(nameof(FeedBusinessPickerLabel), Culture) ?? "Business";
    public static string FeedRefreshButton => ResourceManager.GetString(nameof(FeedRefreshButton), Culture) ?? "Refresh feed";
    public static string FeedOpenPromotionButton => ResourceManager.GetString(nameof(FeedOpenPromotionButton), Culture) ?? "Open";
    public static string FeedPromotionScopeSelectedBusinessButton => ResourceManager.GetString(nameof(FeedPromotionScopeSelectedBusinessButton), Culture) ?? "Selected business promotions";
    public static string FeedPromotionScopeAllBusinessesButton => ResourceManager.GetString(nameof(FeedPromotionScopeAllBusinessesButton), Culture) ?? "All businesses promotions";
    public static string FeedPromotionScopeSelectedBusiness => ResourceManager.GetString(nameof(FeedPromotionScopeSelectedBusiness), Culture) ?? "Promotion scope: selected business";
    public static string FeedPromotionScopeAllBusinesses => ResourceManager.GetString(nameof(FeedPromotionScopeAllBusinesses), Culture) ?? "Promotion scope: all joined businesses";
    public static string FeedPromotionPolicyDiagnosticsSummaryFormat => ResourceManager.GetString(nameof(FeedPromotionPolicyDiagnosticsSummaryFormat), Culture) ?? "Policy: suppression {0} min · cap {1} | Diagnostics: initial {2}, suppressed {3}, dedup {4}, capped {5}, final {6}";
    public static string FeedPromotionDiagnosticsTitle => ResourceManager.GetString(nameof(FeedPromotionDiagnosticsTitle), Culture) ?? "Promotion diagnostics";
    public static string FeedPromotionDiagnosticsSnapshotAtFormat => ResourceManager.GetString(nameof(FeedPromotionDiagnosticsSnapshotAtFormat), Culture) ?? "Diagnostics snapshot at: {0}";
    public static string FeedPromotionDiagnosticsFreshnessFreshFormat => ResourceManager.GetString(nameof(FeedPromotionDiagnosticsFreshnessFreshFormat), Culture) ?? "Snapshot freshness: {0} minute(s) old";
    public static string FeedPromotionDiagnosticsFreshnessStaleFormat => ResourceManager.GetString(nameof(FeedPromotionDiagnosticsFreshnessStaleFormat), Culture) ?? "Snapshot freshness warning: {0} minute(s) old";
    public static string FeedPromotionDiagnosticsVisiblePromotionsFormat => ResourceManager.GetString(nameof(FeedPromotionDiagnosticsVisiblePromotionsFormat), Culture) ?? "Visible promotions: {0}";
    public static string FeedPromotionDiagnosticsVisiblePromotionsWithRemainingFormat => ResourceManager.GetString(nameof(FeedPromotionDiagnosticsVisiblePromotionsWithRemainingFormat), Culture) ?? "Visible promotions: {0} (+{1} more)";
    public static string FeedPromotionDiagnosticsVisiblePromotionsEmpty => ResourceManager.GetString(nameof(FeedPromotionDiagnosticsVisiblePromotionsEmpty), Culture) ?? "Visible promotions: none";
    public static string FeedCopyPromotionDiagnosticsButton => ResourceManager.GetString(nameof(FeedCopyPromotionDiagnosticsButton), Culture) ?? "Copy promotion diagnostics";
    public static string FeedPromotionDiagnosticsCopied => ResourceManager.GetString(nameof(FeedPromotionDiagnosticsCopied), Culture) ?? "Promotion diagnostics copied.";
    public static string FeedPromotionDiagnosticsCopyFailed => ResourceManager.GetString(nameof(FeedPromotionDiagnosticsCopyFailed), Culture) ?? "Unable to copy promotion diagnostics right now.";
    public static string FeedPromotionDiagnosticsClearStatusButton => ResourceManager.GetString(nameof(FeedPromotionDiagnosticsClearStatusButton), Culture) ?? "Clear diagnostics status";
    public static string FeedPointsDeltaFormat => ResourceManager.GetString(nameof(FeedPointsDeltaFormat), Culture) ?? "{0:+#;-#;0} pts";
    public static string FeedPointsSpentFormat => ResourceManager.GetString(nameof(FeedPointsSpentFormat), Culture) ?? "Spent: {0} pts";

    public static string QrBusinessFormat => ResourceManager.GetString(nameof(QrBusinessFormat), Culture) ?? "Business: {0}";
    public static string QrExpiresAtFormat => ResourceManager.GetString(nameof(QrExpiresAtFormat), Culture) ?? "Expires at: {0:HH:mm:ss}";
    public static string QrAccrualHelpText => ResourceManager.GetString(nameof(QrAccrualHelpText), Culture) ?? "Use this when the business should add new points to your account.";
    public static string QrRedemptionHelpText => ResourceManager.GetString(nameof(QrRedemptionHelpText), Culture) ?? "Use this when you want to redeem points or claim an available reward.";
    public static string QrDiscoverGuidanceMessage => ResourceManager.GetString(nameof(QrDiscoverGuidanceMessage), Culture) ?? "To generate a QR code, first go to Discover, open a business, and join its loyalty program.";
    public static string QrRefreshGuidanceMessage => ResourceManager.GetString(nameof(QrRefreshGuidanceMessage), Culture) ?? "Accrual creates a QR for earning points. Redemption creates a QR for spending points or rewards.";
    public static string QrJoinedStatusMessage => ResourceManager.GetString(nameof(QrJoinedStatusMessage), Culture) ?? "You have successfully joined this loyalty program. Show this QR code to the business scanner.";
    public static string QrNoBusinessSelectedMessage => ResourceManager.GetString(nameof(QrNoBusinessSelectedMessage), Culture) ?? "No business is selected yet. Please open a business in Discover and join it first.";
    public static string QrAutoRefreshDueNow => ResourceManager.GetString(nameof(QrAutoRefreshDueNow), Culture) ?? "Auto refresh is due now.";
    public static string QrAutoRefreshInFormat => ResourceManager.GetString(nameof(QrAutoRefreshInFormat), Culture) ?? "Auto refresh in {0}";
    public static string ScannerCameraAccessRequiredTitle => ResourceManager.GetString(nameof(ScannerCameraAccessRequiredTitle), Culture) ?? "Camera access required";
    public static string ScannerCameraAccessRequiredMessage => ResourceManager.GetString(nameof(ScannerCameraAccessRequiredMessage), Culture) ?? "Loyan needs camera access to scan QR codes for loyalty sessions. Please allow camera access or continue with manual token entry.";
    public static string ScannerCameraAccessAllowButton => ResourceManager.GetString(nameof(ScannerCameraAccessAllowButton), Culture) ?? "Allow";
    public static string ScannerPermissionDeniedTitle => ResourceManager.GetString(nameof(ScannerPermissionDeniedTitle), Culture) ?? "Camera permission denied";
    public static string ScannerPermissionDeniedMessage => ResourceManager.GetString(nameof(ScannerPermissionDeniedMessage), Culture) ?? "Camera access has been denied. You can enable it in app settings to scan QR codes, or continue with manual token entry.";
    public static string ScannerOpenSettingsButton => ResourceManager.GetString(nameof(ScannerOpenSettingsButton), Culture) ?? "Open settings";
    public static string ScannerManualTokenTitle => ResourceManager.GetString(nameof(ScannerManualTokenTitle), Culture) ?? "Manual scan token";
    public static string ScannerManualTokenMessage => ResourceManager.GetString(nameof(ScannerManualTokenMessage), Culture) ?? "No camera is available. Paste the ScanSessionToken or cancel.";
    public static string ScannerManualTokenAccept => ResourceManager.GetString(nameof(ScannerManualTokenAccept), Culture) ?? "OK";
    public static string ScannerManualTokenCancel => ResourceManager.GetString(nameof(ScannerManualTokenCancel), Culture) ?? "Cancel";
    public static string ScannerManualTokenPlaceholder => ResourceManager.GetString(nameof(ScannerManualTokenPlaceholder), Culture) ?? "Paste token here";

    public static string AuthLegalSectionTitle => ResourceManager.GetString(nameof(AuthLegalSectionTitle), Culture) ?? "Legal & Privacy";
    public static string AuthLegalSectionSubtitle => ResourceManager.GetString(nameof(AuthLegalSectionSubtitle), Culture) ?? "Review the legal pages before creating an account or signing in.";
    public static string LegalHubTitle => ResourceManager.GetString(nameof(LegalHubTitle), Culture) ?? "Legal & Privacy";
    public static string LegalHubSubtitle => ResourceManager.GetString(nameof(LegalHubSubtitle), Culture) ?? "Open the current legal and privacy pages maintained on loyan.de.";
    public static string LegalImpressumButton => ResourceManager.GetString(nameof(LegalImpressumButton), Culture) ?? "Legal notice";
    public static string LegalPrivacyNoticeButton => ResourceManager.GetString(nameof(LegalPrivacyNoticeButton), Culture) ?? "Privacy notice";
    public static string LegalTermsButton => ResourceManager.GetString(nameof(LegalTermsButton), Culture) ?? "Terms of use";
    public static string LegalAccountDeletionButton => ResourceManager.GetString(nameof(LegalAccountDeletionButton), Culture) ?? "Delete account";
    public static string LegalOpenFailed => ResourceManager.GetString(nameof(LegalOpenFailed), Culture) ?? "The legal page could not be opened right now. Please try again shortly.";
    public static string SettingsLegalHubButton => ResourceManager.GetString(nameof(SettingsLegalHubButton), Culture) ?? "Legal & Privacy";
    public static string SettingsDeleteAccountButton => ResourceManager.GetString(nameof(SettingsDeleteAccountButton), Culture) ?? "Delete account";
    public static string RegisterAcknowledgementsTitle => ResourceManager.GetString(nameof(RegisterAcknowledgementsTitle), Culture) ?? "Required acknowledgements";
    public static string RegisterReadinessBusy => ResourceManager.GetString(nameof(RegisterReadinessBusy), Culture) ?? "Creating your account and preparing your first session...";
    public static string RegisterReadinessProfileDetails => ResourceManager.GetString(nameof(RegisterReadinessProfileDetails), Culture) ?? "Add your first and last name to continue.";
    public static string RegisterReadinessEmail => ResourceManager.GetString(nameof(RegisterReadinessEmail), Culture) ?? "Add your email address to continue.";
    public static string RegisterReadinessPassword => ResourceManager.GetString(nameof(RegisterReadinessPassword), Culture) ?? "Enter and confirm your password to continue.";
    public static string RegisterReadinessPasswordLength => ResourceManager.GetString(nameof(RegisterReadinessPasswordLength), Culture) ?? "Your password must contain at least 8 characters.";
    public static string RegisterReadinessPasswordMismatch => ResourceManager.GetString(nameof(RegisterReadinessPasswordMismatch), Culture) ?? "Your password confirmation must match exactly.";
    public static string RegisterReadinessAcknowledgements => ResourceManager.GetString(nameof(RegisterReadinessAcknowledgements), Culture) ?? "Confirm the required legal acknowledgements to enable account creation.";
    public static string RegisterReadinessReady => ResourceManager.GetString(nameof(RegisterReadinessReady), Culture) ?? "Your account details are ready. You can create your account now.";
    public static string RegisterTermsAcknowledgementLabel => ResourceManager.GetString(nameof(RegisterTermsAcknowledgementLabel), Culture) ?? "I accept the consumer terms before creating my account.";
    public static string RegisterPrivacyAcknowledgementLabel => ResourceManager.GetString(nameof(RegisterPrivacyAcknowledgementLabel), Culture) ?? "I confirm that I have reviewed the privacy notice before creating my account.";
    public static string RegisterTermsAcceptanceRequired => ResourceManager.GetString(nameof(RegisterTermsAcceptanceRequired), Culture) ?? "Please accept the terms before creating your account.";
    public static string RegisterPrivacyAcknowledgementRequired => ResourceManager.GetString(nameof(RegisterPrivacyAcknowledgementRequired), Culture) ?? "Please acknowledge the privacy notice before creating your account.";
    public static string RegisterEmailConfirmationSent => ResourceManager.GetString(nameof(RegisterEmailConfirmationSent), Culture) ?? "Your account was created. Please confirm your email before signing in.";
    public static string RegisterReturnToLoginButton => ResourceManager.GetString(nameof(RegisterReturnToLoginButton), Culture) ?? "Back to sign in";
    public static string AccountDeletionTitle => ResourceManager.GetString(nameof(AccountDeletionTitle), Culture) ?? "Delete account";
    public static string AccountDeletionWarningTitle => ResourceManager.GetString(nameof(AccountDeletionWarningTitle), Culture) ?? "Before you continue";
    public static string AccountDeletionWarningBody => ResourceManager.GetString(nameof(AccountDeletionWarningBody), Culture) ?? "This action permanently deactivates your account and anonymizes personal data where it is safe to do so.";
    public static string AccountDeletionRetentionHint => ResourceManager.GetString(nameof(AccountDeletionRetentionHint), Culture) ?? "Some transaction and history records may remain for legal or technical reasons, but direct personal data will be removed or replaced.";
    public static string AccountDeletionLogoutHint => ResourceManager.GetString(nameof(AccountDeletionLogoutHint), Culture) ?? "After completion you will be signed out and future login with this account will no longer be possible.";
    public static string AccountDeletionConfirmationLabel => ResourceManager.GetString(nameof(AccountDeletionConfirmationLabel), Culture) ?? "I understand that this request is irreversible and want to permanently deactivate and anonymize my account.";
    public static string AccountDeletionContinueButton => ResourceManager.GetString(nameof(AccountDeletionContinueButton), Culture) ?? "Send deletion request";
    public static string AccountDeletionOpenFailed => ResourceManager.GetString(nameof(AccountDeletionOpenFailed), Culture) ?? "The account deletion request could not be submitted right now. Please try again shortly.";
    public static string AccountDeletionConfirmationRequired => ResourceManager.GetString(nameof(AccountDeletionConfirmationRequired), Culture) ?? "Please confirm that you understand the irreversible consequences before continuing.";
    public static string AccountDeletionRequestFailed => ResourceManager.GetString(nameof(AccountDeletionRequestFailed), Culture) ?? "Your account deletion request could not be completed right now. Please try again shortly.";
    public static string PermissionDisclosureContinueButton => ResourceManager.GetString(nameof(PermissionDisclosureContinueButton), Culture) ?? "Continue";
    public static string PermissionDisclosureCancelButton => ResourceManager.GetString(nameof(PermissionDisclosureCancelButton), Culture) ?? "Not now";
    public static string PermissionDisclosurePrivacyButton => ResourceManager.GetString(nameof(PermissionDisclosurePrivacyButton), Culture) ?? "Open privacy notice";
    public static string LocationDisclosureTitle => ResourceManager.GetString(nameof(LocationDisclosureTitle), Culture) ?? "Location access";
    public static string LocationDisclosurePermissionName => ResourceManager.GetString(nameof(LocationDisclosurePermissionName), Culture) ?? "Location permission";
    public static string LocationDisclosurePurpose => ResourceManager.GetString(nameof(LocationDisclosurePurpose), Culture) ?? "Darwin uses your location to show nearby businesses and improve local discovery results.";
    public static string LocationDisclosureRequirement => ResourceManager.GetString(nameof(LocationDisclosureRequirement), Culture) ?? "This feature is optional. You can continue using the app without nearby results if you do not allow location access.";
    public static string NotificationDisclosureTitle => ResourceManager.GetString(nameof(NotificationDisclosureTitle), Culture) ?? "Notifications";
    public static string NotificationDisclosurePermissionName => ResourceManager.GetString(nameof(NotificationDisclosurePermissionName), Culture) ?? "Notification permission";
    public static string NotificationDisclosurePurpose => ResourceManager.GetString(nameof(NotificationDisclosurePurpose), Culture) ?? "Darwin requests notification access so the app can deliver loyalty and account-related push messages when you enable them.";
    public static string NotificationDisclosureRequirement => ResourceManager.GetString(nameof(NotificationDisclosureRequirement), Culture) ?? "Notifications are optional. If you do not allow them, loyalty updates will remain available inside the app only.";
    public static string ProfilePushPermissionNotGranted => ResourceManager.GetString(nameof(ProfilePushPermissionNotGranted), Culture) ?? "Notification permission was not granted. Push registration was not updated.";
    public static string ProfilePushPermissionRequestFailed => ResourceManager.GetString(nameof(ProfilePushPermissionRequestFailed), Culture) ?? "Notification permission could not be requested right now. Please try again.";

}
