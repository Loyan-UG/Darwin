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
    public static string SettingsStaffAccessBadgeButton => ResourceManager.GetString(nameof(SettingsStaffAccessBadgeButton), Culture) ?? "Staff access badge";
    public static string SettingsSubscriptionButton => ResourceManager.GetString(nameof(SettingsSubscriptionButton), Culture) ?? "Subscription";

    public static string SubscriptionTitle => ResourceManager.GetString(nameof(SubscriptionTitle), Culture) ?? "Subscription";
    public static string SubscriptionDescription => ResourceManager.GetString(nameof(SubscriptionDescription), Culture) ?? "Manage your billing plan and payment methods via secure portal access.";
    public static string SubscriptionStatusSectionTitle => ResourceManager.GetString(nameof(SubscriptionStatusSectionTitle), Culture) ?? "Current plan";
    public static string SubscriptionRefreshStatusButton => ResourceManager.GetString(nameof(SubscriptionRefreshStatusButton), Culture) ?? "Refresh subscription status";
    public static string SubscriptionStatusUnavailable => ResourceManager.GetString(nameof(SubscriptionStatusUnavailable), Culture) ?? "Subscription status is currently unavailable.";
    public static string SubscriptionNoActivePlan => ResourceManager.GetString(nameof(SubscriptionNoActivePlan), Culture) ?? "No active subscription found for this business.";
    public static string SubscriptionUnknownPlan => ResourceManager.GetString(nameof(SubscriptionUnknownPlan), Culture) ?? "Unknown plan";
    public static string SubscriptionUnknownProvider => ResourceManager.GetString(nameof(SubscriptionUnknownProvider), Culture) ?? "Unknown provider";
    public static string SubscriptionUnknownStatus => ResourceManager.GetString(nameof(SubscriptionUnknownStatus), Culture) ?? "Unknown status";
    public static string SubscriptionDateUnknown => ResourceManager.GetString(nameof(SubscriptionDateUnknown), Culture) ?? "N/A";
    public static string SubscriptionStatusSummaryFormat => ResourceManager.GetString(nameof(SubscriptionStatusSummaryFormat), Culture) ?? "Plan: {0} · Status: {1} · Provider: {2} · Price: {3}";
    public static string SubscriptionStatusDatesFormat => ResourceManager.GetString(nameof(SubscriptionStatusDatesFormat), Culture) ?? "Period end: {0} · Trial end: {1}";
    public static string SubscriptionSetCancelAtPeriodEndButton => ResourceManager.GetString(nameof(SubscriptionSetCancelAtPeriodEndButton), Culture) ?? "Schedule cancellation at period end";
    public static string SubscriptionUndoCancelAtPeriodEndButton => ResourceManager.GetString(nameof(SubscriptionUndoCancelAtPeriodEndButton), Culture) ?? "Keep subscription active";
    public static string SubscriptionCancelAtPeriodEndScheduled => ResourceManager.GetString(nameof(SubscriptionCancelAtPeriodEndScheduled), Culture) ?? "Cancellation was scheduled for the end of current period.";
    public static string SubscriptionCancelAtPeriodEndCleared => ResourceManager.GetString(nameof(SubscriptionCancelAtPeriodEndCleared), Culture) ?? "Cancellation schedule was removed.";
    public static string SubscriptionCancelAtPeriodEndUpdateFailed => ResourceManager.GetString(nameof(SubscriptionCancelAtPeriodEndUpdateFailed), Culture) ?? "Unable to update cancellation preference. Please refresh and try again.";
    public static string SubscriptionPlansSectionTitle => ResourceManager.GetString(nameof(SubscriptionPlansSectionTitle), Culture) ?? "Available plans";
    public static string SubscriptionPlansUnavailable => ResourceManager.GetString(nameof(SubscriptionPlansUnavailable), Culture) ?? "No billing plans are currently available.";
    public static string SubscriptionNoAlternativePlans => ResourceManager.GetString(nameof(SubscriptionNoAlternativePlans), Culture) ?? "No alternative plans are currently available for upgrade.";
    public static string SubscriptionPlanLineFormat => ResourceManager.GetString(nameof(SubscriptionPlanLineFormat), Culture) ?? "{0}: {1} {2} every {3} {4}";
    public static string SubscriptionCheckoutNoPlanSelected => ResourceManager.GetString(nameof(SubscriptionCheckoutNoPlanSelected), Culture) ?? "Select a plan to continue checkout.";
    public static string SubscriptionCheckoutSelectedPlanFormat => ResourceManager.GetString(nameof(SubscriptionCheckoutSelectedPlanFormat), Culture) ?? "Checkout target: {0} ({1})";
    public static string SubscriptionStartCheckoutButton => ResourceManager.GetString(nameof(SubscriptionStartCheckoutButton), Culture) ?? "Continue to checkout";
    public static string SubscriptionCheckoutPlanPickerLabel => ResourceManager.GetString(nameof(SubscriptionCheckoutPlanPickerLabel), Culture) ?? "Checkout plan";
    public static string SubscriptionCheckoutPlanPickerTitle => ResourceManager.GetString(nameof(SubscriptionCheckoutPlanPickerTitle), Culture) ?? "Select a plan";
    public static string SubscriptionCheckoutUrlInvalid => ResourceManager.GetString(nameof(SubscriptionCheckoutUrlInvalid), Culture) ?? "Checkout URL is invalid. Please refresh and try again.";
    public static string SubscriptionCheckoutStartFailed => ResourceManager.GetString(nameof(SubscriptionCheckoutStartFailed), Culture) ?? "Unable to start checkout right now. Please try again.";
    public static string SubscriptionPortalSectionTitle => ResourceManager.GetString(nameof(SubscriptionPortalSectionTitle), Culture) ?? "Billing portal";
    public static string SubscriptionPortalUrlLabel => ResourceManager.GetString(nameof(SubscriptionPortalUrlLabel), Culture) ?? "Portal URL";
    public static string SubscriptionPortalReadyHint => ResourceManager.GetString(nameof(SubscriptionPortalReadyHint), Culture) ?? "Billing portal is configured for this environment. Open it to manage plan and payment details.";
    public static string SubscriptionPortalMissingHint => ResourceManager.GetString(nameof(SubscriptionPortalMissingHint), Culture) ?? "Billing portal is not configured for this environment yet. Contact your admin team.";
    public static string SubscriptionPortalCopiedHint => ResourceManager.GetString(nameof(SubscriptionPortalCopiedHint), Culture) ?? "Billing portal URL copied to clipboard.";
    public static string SubscriptionPortalOpenFailed => ResourceManager.GetString(nameof(SubscriptionPortalOpenFailed), Culture) ?? "Unable to open billing portal right now. Please try again shortly.";
    public static string SubscriptionPortalCopyFailed => ResourceManager.GetString(nameof(SubscriptionPortalCopyFailed), Culture) ?? "Unable to copy billing portal URL right now. Please try again.";
    public static string SubscriptionPortalValidationMissingUrl => ResourceManager.GetString(nameof(SubscriptionPortalValidationMissingUrl), Culture) ?? "Billing portal URL is not configured.";
    public static string SubscriptionPortalValidationInvalidUrl => ResourceManager.GetString(nameof(SubscriptionPortalValidationInvalidUrl), Culture) ?? "Billing portal URL is invalid. Configure an absolute URL.";
    public static string SubscriptionPortalValidationRequiresHttps => ResourceManager.GetString(nameof(SubscriptionPortalValidationRequiresHttps), Culture) ?? "Billing portal URL must use HTTPS.";
    public static string SubscriptionPortalValidationHostNotAllowedFormat => ResourceManager.GetString(nameof(SubscriptionPortalValidationHostNotAllowedFormat), Culture) ?? "Billing portal host \"{0}\" is not allowed for this environment.";
    public static string SubscriptionPortalValidationReadyFormat => ResourceManager.GetString(nameof(SubscriptionPortalValidationReadyFormat), Culture) ?? "Configured host: {0}";
    public static string SubscriptionOpenPortalButton => ResourceManager.GetString(nameof(SubscriptionOpenPortalButton), Culture) ?? "Open billing portal";
    public static string SubscriptionCopyPortalUrlButton => ResourceManager.GetString(nameof(SubscriptionCopyPortalUrlButton), Culture) ?? "Copy portal URL";

    public static string StaffAccessBadgeTitle => ResourceManager.GetString(nameof(StaffAccessBadgeTitle), Culture) ?? "Staff access badge";
    public static string StaffAccessBadgeDescription => ResourceManager.GetString(nameof(StaffAccessBadgeDescription), Culture) ?? "Use this short-lived QR for internal staff checkpoints and controlled business operations.";
    public static string StaffAccessBadgeSummaryFormat => ResourceManager.GetString(nameof(StaffAccessBadgeSummaryFormat), Culture) ?? "{0} · {1}";
    public static string StaffAccessBadgeRoleFormat => ResourceManager.GetString(nameof(StaffAccessBadgeRoleFormat), Culture) ?? "Role: {0}";
    public static string StaffAccessBadgeExpiresInFormat => ResourceManager.GetString(nameof(StaffAccessBadgeExpiresInFormat), Culture) ?? "Expires in {0}";
    public static string StaffAccessBadgeExpired => ResourceManager.GetString(nameof(StaffAccessBadgeExpired), Culture) ?? "Badge expired. Refreshing…";
    public static string StaffAccessBadgeRefreshButton => ResourceManager.GetString(nameof(StaffAccessBadgeRefreshButton), Culture) ?? "Refresh badge";
    public static string StaffAccessBadgeLoadFailed => ResourceManager.GetString(nameof(StaffAccessBadgeLoadFailed), Culture) ?? "Unable to prepare staff badge right now.";
    public static string StaffAccessBadgeUnknownRole => ResourceManager.GetString(nameof(StaffAccessBadgeUnknownRole), Culture) ?? "Unknown";

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
    public static string ProfileConcurrencyConflict => ResourceManager.GetString(nameof(ProfileConcurrencyConflict), Culture) ?? "Your profile was updated elsewhere. Please refresh and try again.";

    public static string ChangePasswordTitle => ResourceManager.GetString(nameof(ChangePasswordTitle), Culture) ?? "Change password";
    public static string CurrentPasswordLabel => ResourceManager.GetString(nameof(CurrentPasswordLabel), Culture) ?? "Current password";
    public static string NewPasswordLabel => ResourceManager.GetString(nameof(NewPasswordLabel), Culture) ?? "New password";
    public static string ConfirmNewPasswordLabel => ResourceManager.GetString(nameof(ConfirmNewPasswordLabel), Culture) ?? "Confirm new password";
    public static string ChangePasswordButton => ResourceManager.GetString(nameof(ChangePasswordButton), Culture) ?? "Update password";
    public static string PasswordMismatch => ResourceManager.GetString(nameof(PasswordMismatch), Culture) ?? "New password and confirmation do not match.";
    public static string PasswordMinLength => ResourceManager.GetString(nameof(PasswordMinLength), Culture) ?? "New password must be at least 8 characters long.";
    public static string PasswordChangeFailed => ResourceManager.GetString(nameof(PasswordChangeFailed), Culture) ?? "Unable to change password. Please check your current password.";
    public static string PasswordChangeSuccess => ResourceManager.GetString(nameof(PasswordChangeSuccess), Culture) ?? "Password changed successfully.";


    // Business permissions
    public static string BusinessPermissionDeniedRewardEdit => ResourceManager.GetString(nameof(BusinessPermissionDeniedRewardEdit), Culture) ?? "Your account does not have permission to edit reward tiers.";
    public static string BusinessPermissionDeniedRedemption => ResourceManager.GetString(nameof(BusinessPermissionDeniedRedemption), Culture) ?? "Your account does not have permission to confirm redemption.";
    public static string BusinessPermissionDeniedAccrual => ResourceManager.GetString(nameof(BusinessPermissionDeniedAccrual), Culture) ?? "Your account does not have permission to confirm accrual.";
    public static string BusinessPermissionsUnavailableWarning => ResourceManager.GetString(nameof(BusinessPermissionsUnavailableWarning), Culture) ?? "Operator permissions could not be loaded. Please refresh and try again.";
    public static string BusinessNoScannerPermissionWarning => ResourceManager.GetString(nameof(BusinessNoScannerPermissionWarning), Culture) ?? "Your account does not currently have scanner processing permissions.";
    public static string ScannerSingleOwnerHint => ResourceManager.GetString(nameof(ScannerSingleOwnerHint), Culture) ?? "After scanning, the session screen will load details and handle confirmation in one place.";


    // Dashboard + reporting
    public static string DashboardTitle => ResourceManager.GetString(nameof(DashboardTitle), Culture) ?? "Dashboard";
    public static string DashboardSubtitle => ResourceManager.GetString(nameof(DashboardSubtitle), Culture) ?? "Operational snapshot for recent scanner activity.";
    public static string DashboardLookbackLabel => ResourceManager.GetString(nameof(DashboardLookbackLabel), Culture) ?? "Window";
    public static string DashboardRefreshButton => ResourceManager.GetString(nameof(DashboardRefreshButton), Culture) ?? "Refresh";
    public static string DashboardExportCsvButton => ResourceManager.GetString(nameof(DashboardExportCsvButton), Culture) ?? "Export CSV";
    public static string DashboardExportCsvShareTitle => ResourceManager.GetString(nameof(DashboardExportCsvShareTitle), Culture) ?? "Share dashboard CSV";
    public static string DashboardExportCsvFailed => ResourceManager.GetString(nameof(DashboardExportCsvFailed), Culture) ?? "Unable to export dashboard CSV.";
    public static string DashboardExportPdfButton => ResourceManager.GetString(nameof(DashboardExportPdfButton), Culture) ?? "Export PDF";
    public static string DashboardExportPdfShareTitle => ResourceManager.GetString(nameof(DashboardExportPdfShareTitle), Culture) ?? "Share dashboard PDF";
    public static string DashboardExportPdfFailed => ResourceManager.GetString(nameof(DashboardExportPdfFailed), Culture) ?? "Unable to export dashboard PDF.";
    public static string DashboardLoadFailed => ResourceManager.GetString(nameof(DashboardLoadFailed), Culture) ?? "Unable to load dashboard report.";
    public static string DashboardTopCustomersTitle => ResourceManager.GetString(nameof(DashboardTopCustomersTitle), Culture) ?? "Top customers";
    public static string DashboardTopCustomersEmpty => ResourceManager.GetString(nameof(DashboardTopCustomersEmpty), Culture) ?? "No customer interactions were recorded in the selected window.";
    public static string DashboardRecentActivityTitle => ResourceManager.GetString(nameof(DashboardRecentActivityTitle), Culture) ?? "Recent activity";
    public static string DashboardRecentActivityEmpty => ResourceManager.GetString(nameof(DashboardRecentActivityEmpty), Culture) ?? "No recent activity was recorded yet.";
    public static string DashboardCampaignTargetingFixMetricsFormat => ResourceManager.GetString(nameof(DashboardCampaignTargetingFixMetricsFormat), Culture) ?? "Campaign quick-fix — applied: {0} · no-change: {1} · resets: {2}";


    // Rewards editor
    public static string RewardsTitle => ResourceManager.GetString(nameof(RewardsTitle), Culture) ?? "Rewards";
    public static string RewardsSubtitle => ResourceManager.GetString(nameof(RewardsSubtitle), Culture) ?? "Create and maintain reward tiers for your loyalty program.";
    public static string RewardsRefreshButton => ResourceManager.GetString(nameof(RewardsRefreshButton), Culture) ?? "Refresh";
    public static string RewardsNewTierButton => ResourceManager.GetString(nameof(RewardsNewTierButton), Culture) ?? "New tier";
    public static string RewardsCreateButton => ResourceManager.GetString(nameof(RewardsCreateButton), Culture) ?? "Create tier";
    public static string RewardsUpdateButton => ResourceManager.GetString(nameof(RewardsUpdateButton), Culture) ?? "Update tier";
    public static string RewardsDeleteButton => ResourceManager.GetString(nameof(RewardsDeleteButton), Culture) ?? "Delete tier";
    public static string RewardsCurrentTiersLabel => ResourceManager.GetString(nameof(RewardsCurrentTiersLabel), Culture) ?? "Current tiers";
    public static string RewardsPointsPlaceholder => ResourceManager.GetString(nameof(RewardsPointsPlaceholder), Culture) ?? "Points required";
    public static string RewardsTypePickerTitle => ResourceManager.GetString(nameof(RewardsTypePickerTitle), Culture) ?? "Reward type";
    public static string RewardsValuePlaceholder => ResourceManager.GetString(nameof(RewardsValuePlaceholder), Culture) ?? "Reward value (optional)";
    public static string RewardsDescriptionPlaceholder => ResourceManager.GetString(nameof(RewardsDescriptionPlaceholder), Culture) ?? "Description (optional)";
    public static string RewardsAllowSelfRedemptionLabel => ResourceManager.GetString(nameof(RewardsAllowSelfRedemptionLabel), Culture) ?? "Allow self redemption";
    public static string RewardsLoadFailed => ResourceManager.GetString(nameof(RewardsLoadFailed), Culture) ?? "Unable to load reward configuration.";
    public static string RewardsSaveFailed => ResourceManager.GetString(nameof(RewardsSaveFailed), Culture) ?? "Unable to save reward tier.";
    public static string RewardsDeleteFailed => ResourceManager.GetString(nameof(RewardsDeleteFailed), Culture) ?? "Unable to delete reward tier.";
    public static string RewardsValidationFailed => ResourceManager.GetString(nameof(RewardsValidationFailed), Culture) ?? "Please review reward tier fields and try again.";
    public static string RewardsPointsValidation => ResourceManager.GetString(nameof(RewardsPointsValidation), Culture) ?? "Points required must be a number greater than zero.";
    public static string RewardsTypeValidation => ResourceManager.GetString(nameof(RewardsTypeValidation), Culture) ?? "Please select a valid reward type.";
    public static string RewardsValueValidation => ResourceManager.GetString(nameof(RewardsValueValidation), Culture) ?? "Reward value must be a valid number.";
    public static string RewardsCampaignsLabel => ResourceManager.GetString(nameof(RewardsCampaignsLabel), Culture) ?? "Campaigns";
    public static string RewardsCampaignActivateButton => ResourceManager.GetString(nameof(RewardsCampaignActivateButton), Culture) ?? "Activate campaign";
    public static string RewardsCampaignDeactivateButton => ResourceManager.GetString(nameof(RewardsCampaignDeactivateButton), Culture) ?? "Deactivate campaign";
    public static string RewardsCampaignToggleFailed => ResourceManager.GetString(nameof(RewardsCampaignToggleFailed), Culture) ?? "Unable to change campaign activation state.";
    public static string RewardsCampaignNamePlaceholder => ResourceManager.GetString(nameof(RewardsCampaignNamePlaceholder), Culture) ?? "Campaign internal name";
    public static string RewardsCampaignTitlePlaceholder => ResourceManager.GetString(nameof(RewardsCampaignTitlePlaceholder), Culture) ?? "Campaign title";
    public static string RewardsCampaignBodyPlaceholder => ResourceManager.GetString(nameof(RewardsCampaignBodyPlaceholder), Culture) ?? "Campaign body (optional)";
    public static string RewardsCampaignNewButton => ResourceManager.GetString(nameof(RewardsCampaignNewButton), Culture) ?? "New campaign";
    public static string RewardsCampaignCreateButton => ResourceManager.GetString(nameof(RewardsCampaignCreateButton), Culture) ?? "Create campaign";
    public static string RewardsCampaignUpdateButton => ResourceManager.GetString(nameof(RewardsCampaignUpdateButton), Culture) ?? "Update campaign";
    public static string RewardsCampaignSaveFailed => ResourceManager.GetString(nameof(RewardsCampaignSaveFailed), Culture) ?? "Unable to save campaign.";
    public static string RewardsCampaignValidationFailed => ResourceManager.GetString(nameof(RewardsCampaignValidationFailed), Culture) ?? "Campaign name and title are required.";
    public static string RewardsCampaignNameDuplicateValidationFailed => ResourceManager.GetString(nameof(RewardsCampaignNameDuplicateValidationFailed), Culture) ?? "A campaign with this internal name already exists.";
    public static string RewardsCampaignStartsAtPlaceholder => ResourceManager.GetString(nameof(RewardsCampaignStartsAtPlaceholder), Culture) ?? "Start UTC (yyyy-MM-dd HH:mm, optional)";
    public static string RewardsCampaignEndsAtPlaceholder => ResourceManager.GetString(nameof(RewardsCampaignEndsAtPlaceholder), Culture) ?? "End UTC (yyyy-MM-dd HH:mm, optional)";
    public static string RewardsCampaignDateValidationFailed => ResourceManager.GetString(nameof(RewardsCampaignDateValidationFailed), Culture) ?? "Campaign date format is invalid. Use yyyy-MM-dd HH:mm.";
    public static string RewardsCampaignDateRangeValidationFailed => ResourceManager.GetString(nameof(RewardsCampaignDateRangeValidationFailed), Culture) ?? "Campaign start date must be earlier than end date.";
    public static string RewardsCampaignChannelPickerTitle => ResourceManager.GetString(nameof(RewardsCampaignChannelPickerTitle), Culture) ?? "Delivery channels";
    public static string RewardsCampaignChannelInAppOnly => ResourceManager.GetString(nameof(RewardsCampaignChannelInAppOnly), Culture) ?? "In-app only";
    public static string RewardsCampaignChannelInAppAndPush => ResourceManager.GetString(nameof(RewardsCampaignChannelInAppAndPush), Culture) ?? "In-app + Push";
    public static string RewardsCampaignChannelValidationFailed => ResourceManager.GetString(nameof(RewardsCampaignChannelValidationFailed), Culture) ?? "Please select a valid campaign delivery channel.";
    public static string RewardsCampaignTargetingJsonPlaceholder => ResourceManager.GetString(nameof(RewardsCampaignTargetingJsonPlaceholder), Culture) ?? "Targeting JSON (optional, object)";
    public static string RewardsCampaignTargetingPresetTitle => ResourceManager.GetString(nameof(RewardsCampaignTargetingPresetTitle), Culture) ?? "Quick audience presets";
    public static string RewardsCampaignTargetingHintDefault => ResourceManager.GetString(nameof(RewardsCampaignTargetingHintDefault), Culture) ?? "Audience: joined members (default).";
    public static string RewardsCampaignTargetingHintJoinedMembers => ResourceManager.GetString(nameof(RewardsCampaignTargetingHintJoinedMembers), Culture) ?? "Joined members preset: keep {\"audienceKind\":\"JoinedMembers\"}.";
    public static string RewardsCampaignTargetingHintTierSegment => ResourceManager.GetString(nameof(RewardsCampaignTargetingHintTierSegment), Culture) ?? "Tier preset: provide tier value, for example {\"audienceKind\":\"TierSegment\",\"tier\":\"Gold\"}.";
    public static string RewardsCampaignTargetingHintPointsThreshold => ResourceManager.GetString(nameof(RewardsCampaignTargetingHintPointsThreshold), Culture) ?? "Points preset: provide minimumPoints, for example {\"audienceKind\":\"PointsThreshold\",\"minimumPoints\":100}.";
    public static string RewardsCampaignTargetingHintDateWindow => ResourceManager.GetString(nameof(RewardsCampaignTargetingHintDateWindow), Culture) ?? "Date-window preset: provide eligibleFromUtc and eligibleToUtc in ISO-8601 UTC format.";
    public static string RewardsCampaignTargetingHintInvalid => ResourceManager.GetString(nameof(RewardsCampaignTargetingHintInvalid), Culture) ?? "Targeting JSON is invalid. Use presets or a valid JSON object.";
    public static string RewardsCampaignTargetingSchemaTierMissing => ResourceManager.GetString(nameof(RewardsCampaignTargetingSchemaTierMissing), Culture) ?? "Tier audience requires a non-empty \"tier\" value.";
    public static string RewardsCampaignTargetingSchemaMinimumPointsMissing => ResourceManager.GetString(nameof(RewardsCampaignTargetingSchemaMinimumPointsMissing), Culture) ?? "Points audience requires a non-negative \"minimumPoints\" value.";
    public static string RewardsCampaignTargetingSchemaDateWindowMissing => ResourceManager.GetString(nameof(RewardsCampaignTargetingSchemaDateWindowMissing), Culture) ?? "Date-window audience requires valid \"eligibleFromUtc\" and \"eligibleToUtc\" UTC values.";
    public static string RewardsCampaignTargetingSchemaDateWindowRangeInvalid => ResourceManager.GetString(nameof(RewardsCampaignTargetingSchemaDateWindowRangeInvalid), Culture) ?? "Date-window audience requires eligibleFromUtc to be earlier than or equal to eligibleToUtc.";
    public static string RewardsCampaignTargetingApplyFixButton => ResourceManager.GetString(nameof(RewardsCampaignTargetingApplyFixButton), Culture) ?? "Apply quick fix";
    public static string RewardsCampaignTargetingFixAppliedMessage => ResourceManager.GetString(nameof(RewardsCampaignTargetingFixAppliedMessage), Culture) ?? "Quick fix applied to targeting JSON.";
    public static string RewardsCampaignTargetingFixNoChangesMessage => ResourceManager.GetString(nameof(RewardsCampaignTargetingFixNoChangesMessage), Culture) ?? "No schema fix was needed for targeting JSON.";
    public static string RewardsCampaignTargetingFixMetricsFormat => ResourceManager.GetString(nameof(RewardsCampaignTargetingFixMetricsFormat), Culture) ?? "Quick-fix stats — applied: {0}, no-change: {1}";
    public static string RewardsCampaignTargetingFixMetricsResetButton => ResourceManager.GetString(nameof(RewardsCampaignTargetingFixMetricsResetButton), Culture) ?? "Reset quick-fix stats";
    public static string RewardsCampaignTargetingFixMetricsResetMessage => ResourceManager.GetString(nameof(RewardsCampaignTargetingFixMetricsResetMessage), Culture) ?? "Quick-fix stats were reset.";
    public static string RewardsCampaignTargetingFixMetricsWindowFormat => ResourceManager.GetString(nameof(RewardsCampaignTargetingFixMetricsWindowFormat), Culture) ?? "Window start: {0} · Last reset: {1}";
    public static string RewardsCampaignTargetingFixMetricsWindowUnknown => ResourceManager.GetString(nameof(RewardsCampaignTargetingFixMetricsWindowUnknown), Culture) ?? "—";
    public static string RewardsCampaignPayloadJsonPlaceholder => ResourceManager.GetString(nameof(RewardsCampaignPayloadJsonPlaceholder), Culture) ?? "Payload JSON (optional, object)";
    public static string RewardsCampaignTargetingValidationFailed => ResourceManager.GetString(nameof(RewardsCampaignTargetingValidationFailed), Culture) ?? "Targeting JSON must be a valid JSON object.";
    public static string RewardsCampaignPayloadValidationFailed => ResourceManager.GetString(nameof(RewardsCampaignPayloadValidationFailed), Culture) ?? "Payload JSON must be a valid JSON object.";
    public static string RewardsCampaignSearchPlaceholder => ResourceManager.GetString(nameof(RewardsCampaignSearchPlaceholder), Culture) ?? "Search campaigns by name, title, or body";
    public static string RewardsCampaignClearSearchButton => ResourceManager.GetString(nameof(RewardsCampaignClearSearchButton), Culture) ?? "Clear search";
    public static string RewardsCampaignStateFilterPickerTitle => ResourceManager.GetString(nameof(RewardsCampaignStateFilterPickerTitle), Culture) ?? "State filter";
    public static string RewardsCampaignStateFilterAll => ResourceManager.GetString(nameof(RewardsCampaignStateFilterAll), Culture) ?? "All states";
    public static string RewardsCampaignStateFilterDraft => ResourceManager.GetString(nameof(RewardsCampaignStateFilterDraft), Culture) ?? "Draft";
    public static string RewardsCampaignStateFilterScheduled => ResourceManager.GetString(nameof(RewardsCampaignStateFilterScheduled), Culture) ?? "Scheduled";
    public static string RewardsCampaignStateFilterActive => ResourceManager.GetString(nameof(RewardsCampaignStateFilterActive), Culture) ?? "Active";
    public static string RewardsCampaignStateFilterExpired => ResourceManager.GetString(nameof(RewardsCampaignStateFilterExpired), Culture) ?? "Expired";
    public static string RewardsCampaignAudienceFilterPickerTitle => ResourceManager.GetString(nameof(RewardsCampaignAudienceFilterPickerTitle), Culture) ?? "Audience filter";
    public static string RewardsCampaignAudienceFilterAll => ResourceManager.GetString(nameof(RewardsCampaignAudienceFilterAll), Culture) ?? "All audiences";
    public static string RewardsCampaignSortPickerTitle => ResourceManager.GetString(nameof(RewardsCampaignSortPickerTitle), Culture) ?? "Sort by";
    public static string RewardsCampaignSortStartDateDesc => ResourceManager.GetString(nameof(RewardsCampaignSortStartDateDesc), Culture) ?? "Start date (newest first)";
    public static string RewardsCampaignSortStartDateAsc => ResourceManager.GetString(nameof(RewardsCampaignSortStartDateAsc), Culture) ?? "Start date (oldest first)";
    public static string RewardsCampaignSortTitleAsc => ResourceManager.GetString(nameof(RewardsCampaignSortTitleAsc), Culture) ?? "Title (A-Z)";
    public static string RewardsCampaignSortTitleDesc => ResourceManager.GetString(nameof(RewardsCampaignSortTitleDesc), Culture) ?? "Title (Z-A)";
    public static string RewardsCampaignsEmptyFiltered => ResourceManager.GetString(nameof(RewardsCampaignsEmptyFiltered), Culture) ?? "No campaigns match the current search/filter.";
    public static string RewardsCampaignClearFiltersButton => ResourceManager.GetString(nameof(RewardsCampaignClearFiltersButton), Culture) ?? "Clear filters";
    public static string RewardsCampaignFilterSummaryFormat => ResourceManager.GetString(nameof(RewardsCampaignFilterSummaryFormat), Culture) ?? "Showing {0} of {1} campaigns";
    public static string RewardsCampaignAudienceSummaryDefault => ResourceManager.GetString(nameof(RewardsCampaignAudienceSummaryDefault), Culture) ?? "Audience: all joined members";
    public static string RewardsCampaignAudienceSummaryFormat => ResourceManager.GetString(nameof(RewardsCampaignAudienceSummaryFormat), Culture) ?? "Audience: {0}";
    public static string RewardsCampaignAudienceSummaryWithEligibilityFormat => ResourceManager.GetString(nameof(RewardsCampaignAudienceSummaryWithEligibilityFormat), Culture) ?? "Audience: {0} • {1}";
    public static string RewardsCampaignAudienceJoinedMembers => ResourceManager.GetString(nameof(RewardsCampaignAudienceJoinedMembers), Culture) ?? "joined members";
    public static string RewardsCampaignAudienceTierSegment => ResourceManager.GetString(nameof(RewardsCampaignAudienceTierSegment), Culture) ?? "tier segment";
    public static string RewardsCampaignAudiencePointsThreshold => ResourceManager.GetString(nameof(RewardsCampaignAudiencePointsThreshold), Culture) ?? "points threshold";
    public static string RewardsCampaignAudienceDateWindow => ResourceManager.GetString(nameof(RewardsCampaignAudienceDateWindow), Culture) ?? "date window";
    public static string RewardsCampaignEligibilityTierFormat => ResourceManager.GetString(nameof(RewardsCampaignEligibilityTierFormat), Culture) ?? "Tier: {0}";
    public static string RewardsCampaignEligibilityRangeFormat => ResourceManager.GetString(nameof(RewardsCampaignEligibilityRangeFormat), Culture) ?? "Points {0}-{1}";
    public static string RewardsCampaignEligibilityMinFormat => ResourceManager.GetString(nameof(RewardsCampaignEligibilityMinFormat), Culture) ?? "Points ≥ {0}";
    public static string RewardsCampaignEligibilityMaxFormat => ResourceManager.GetString(nameof(RewardsCampaignEligibilityMaxFormat), Culture) ?? "Points ≤ {0}";
    public static string RewardsCampaignChannelSummaryFormat => ResourceManager.GetString(nameof(RewardsCampaignChannelSummaryFormat), Culture) ?? "Channels: {0}";
    public static string RewardsCampaignChannelUnknown => ResourceManager.GetString(nameof(RewardsCampaignChannelUnknown), Culture) ?? "unknown";
    public static string RewardsCampaignStateMetricsFormat => ResourceManager.GetString(nameof(RewardsCampaignStateMetricsFormat), Culture) ?? "Draft: {0} · Scheduled: {1} · Active: {2} · Expired: {3}";
    public static string RewardsCampaignAudienceMetricsFormat => ResourceManager.GetString(nameof(RewardsCampaignAudienceMetricsFormat), Culture) ?? "Joined: {0} · Tier: {1} · Points: {2} · Date window: {3}";
    public static string RewardsCampaignChannelMetricsFormat => ResourceManager.GetString(nameof(RewardsCampaignChannelMetricsFormat), Culture) ?? "In-app only: {0} · In-app + Push: {1} · Other: {2}";
    public static string RewardsCampaignStateMetricChipFormat => ResourceManager.GetString(nameof(RewardsCampaignStateMetricChipFormat), Culture) ?? "{0} ({1})";
    public static string RewardsCampaignDiagnosticsSnapshotAtFormat => ResourceManager.GetString(nameof(RewardsCampaignDiagnosticsSnapshotAtFormat), Culture) ?? "Diagnostics snapshot at: {0}";
    public static string RewardsCampaignCopyDiagnosticsButton => ResourceManager.GetString(nameof(RewardsCampaignCopyDiagnosticsButton), Culture) ?? "Copy campaign diagnostics";
    public static string RewardsCampaignClearDiagnosticsStatusButton => ResourceManager.GetString(nameof(RewardsCampaignClearDiagnosticsStatusButton), Culture) ?? "Clear diagnostics status";
    public static string RewardsCampaignDiagnosticsAppliedFiltersFormat => ResourceManager.GetString(nameof(RewardsCampaignDiagnosticsAppliedFiltersFormat), Culture) ?? "Applied filters — State: {0} · Audience: {1} · Search: {2} · Sort: {3}";
    public static string RewardsCampaignDiagnosticsSearchEmpty => ResourceManager.GetString(nameof(RewardsCampaignDiagnosticsSearchEmpty), Culture) ?? "none";
    public static string RewardsCampaignDiagnosticsVisibleCampaignsFormat => ResourceManager.GetString(nameof(RewardsCampaignDiagnosticsVisibleCampaignsFormat), Culture) ?? "Visible campaigns: {0}";
    public static string RewardsCampaignDiagnosticsVisibleCampaignsWithRemainingFormat => ResourceManager.GetString(nameof(RewardsCampaignDiagnosticsVisibleCampaignsWithRemainingFormat), Culture) ?? "Visible campaigns: {0} (+{1} more)";
    public static string RewardsCampaignDiagnosticsVisibleCampaignsEmpty => ResourceManager.GetString(nameof(RewardsCampaignDiagnosticsVisibleCampaignsEmpty), Culture) ?? "Visible campaigns: none";
    public static string RewardsCampaignDiagnosticsCopied => ResourceManager.GetString(nameof(RewardsCampaignDiagnosticsCopied), Culture) ?? "Campaign diagnostics copied to clipboard.";
    public static string RewardsCampaignDiagnosticsCopyFailed => ResourceManager.GetString(nameof(RewardsCampaignDiagnosticsCopyFailed), Culture) ?? "Unable to copy campaign diagnostics right now.";


}
