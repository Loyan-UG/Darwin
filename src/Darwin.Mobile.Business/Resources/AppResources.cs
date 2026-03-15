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
    public static string RewardsCampaignPayloadJsonPlaceholder => ResourceManager.GetString(nameof(RewardsCampaignPayloadJsonPlaceholder), Culture) ?? "Payload JSON (optional, object)";
    public static string RewardsCampaignTargetingValidationFailed => ResourceManager.GetString(nameof(RewardsCampaignTargetingValidationFailed), Culture) ?? "Targeting JSON must be a valid JSON object.";
    public static string RewardsCampaignPayloadValidationFailed => ResourceManager.GetString(nameof(RewardsCampaignPayloadValidationFailed), Culture) ?? "Payload JSON must be a valid JSON object.";
    public static string RewardsCampaignSearchPlaceholder => ResourceManager.GetString(nameof(RewardsCampaignSearchPlaceholder), Culture) ?? "Search campaigns by name or title";
    public static string RewardsCampaignClearSearchButton => ResourceManager.GetString(nameof(RewardsCampaignClearSearchButton), Culture) ?? "Clear search";
    public static string RewardsCampaignStateFilterPickerTitle => ResourceManager.GetString(nameof(RewardsCampaignStateFilterPickerTitle), Culture) ?? "State filter";
    public static string RewardsCampaignStateFilterAll => ResourceManager.GetString(nameof(RewardsCampaignStateFilterAll), Culture) ?? "All states";
    public static string RewardsCampaignStateFilterDraft => ResourceManager.GetString(nameof(RewardsCampaignStateFilterDraft), Culture) ?? "Draft";
    public static string RewardsCampaignStateFilterScheduled => ResourceManager.GetString(nameof(RewardsCampaignStateFilterScheduled), Culture) ?? "Scheduled";
    public static string RewardsCampaignStateFilterActive => ResourceManager.GetString(nameof(RewardsCampaignStateFilterActive), Culture) ?? "Active";
    public static string RewardsCampaignStateFilterExpired => ResourceManager.GetString(nameof(RewardsCampaignStateFilterExpired), Culture) ?? "Expired";
    public static string RewardsCampaignSortPickerTitle => ResourceManager.GetString(nameof(RewardsCampaignSortPickerTitle), Culture) ?? "Sort by";
    public static string RewardsCampaignSortStartDateDesc => ResourceManager.GetString(nameof(RewardsCampaignSortStartDateDesc), Culture) ?? "Start date (newest first)";
    public static string RewardsCampaignSortStartDateAsc => ResourceManager.GetString(nameof(RewardsCampaignSortStartDateAsc), Culture) ?? "Start date (oldest first)";
    public static string RewardsCampaignSortTitleAsc => ResourceManager.GetString(nameof(RewardsCampaignSortTitleAsc), Culture) ?? "Title (A-Z)";
    public static string RewardsCampaignSortTitleDesc => ResourceManager.GetString(nameof(RewardsCampaignSortTitleDesc), Culture) ?? "Title (Z-A)";
    public static string RewardsCampaignsEmptyFiltered => ResourceManager.GetString(nameof(RewardsCampaignsEmptyFiltered), Culture) ?? "No campaigns match the current search/filter.";
    public static string RewardsCampaignClearFiltersButton => ResourceManager.GetString(nameof(RewardsCampaignClearFiltersButton), Culture) ?? "Clear filters";
    public static string RewardsCampaignFilterSummaryFormat => ResourceManager.GetString(nameof(RewardsCampaignFilterSummaryFormat), Culture) ?? "Showing {0} of {1} campaigns";
    public static string RewardsCampaignStateMetricsFormat => ResourceManager.GetString(nameof(RewardsCampaignStateMetricsFormat), Culture) ?? "Draft: {0} · Scheduled: {1} · Active: {2} · Expired: {3}";
    public static string RewardsCampaignStateMetricChipFormat => ResourceManager.GetString(nameof(RewardsCampaignStateMetricChipFormat), Culture) ?? "{0} ({1})";


}
