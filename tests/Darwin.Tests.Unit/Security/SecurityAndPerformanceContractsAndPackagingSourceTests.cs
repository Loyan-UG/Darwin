using FluentAssertions;

namespace Darwin.Tests.Unit.Security;

public sealed class SecurityAndPerformanceContractsAndPackagingSourceTests : SecurityAndPerformanceSourceTestBase
{
    [Fact]
    public void MobileBusinessSessionFeedback_Should_KeepResourceBackedNonAdminContractsWired()
    {
        var appResourcesSource = ReadMobileBusinessFile(Path.Combine("Resources", "AppResources.cs"));
        var sessionViewModelSource = ReadMobileBusinessFile(Path.Combine("ViewModels", "SessionViewModel.cs"));
        var stringsSource = ReadMobileBusinessFile(Path.Combine("Resources", "Strings.resx"));
        var germanStringsSource = ReadMobileBusinessFile(Path.Combine("Resources", "Strings.de.resx"));

        appResourcesSource.Should().Contain("public static string InvalidSessionToken =>");
        appResourcesSource.Should().Contain("public static string SessionLoadFailed =>");
        appResourcesSource.Should().Contain("public static string SessionLoadSuccess =>");
        appResourcesSource.Should().Contain("public static string AccrualConfirmedSuccess =>");
        appResourcesSource.Should().Contain("public static string RedemptionConfirmedSuccess =>");

        sessionViewModelSource.Should().Contain("SetWarning(AppResources.InvalidSessionToken);");
        sessionViewModelSource.Should().Contain("SetError(result.Error ?? AppResources.SessionLoadFailed);");
        sessionViewModelSource.Should().Contain("SetSuccess(AppResources.SessionLoadSuccess);");
        sessionViewModelSource.Should().Contain("SetWarning(AppResources.AccrualNotAllowed);");
        sessionViewModelSource.Should().Contain("SetWarning(AppResources.PointsMustBeGreaterThanZero);");
        sessionViewModelSource.Should().Contain("SetError(result.Error ?? AppResources.FailedToConfirmAccrual);");
        sessionViewModelSource.Should().Contain("SetSuccess(AppResources.AccrualConfirmedSuccess);");
        sessionViewModelSource.Should().Contain("SetWarning(AppResources.RedemptionNotAllowed);");
        sessionViewModelSource.Should().Contain("SetError(result.Error ?? AppResources.FailedToConfirmRedemption);");
        sessionViewModelSource.Should().Contain("SetSuccess(AppResources.RedemptionConfirmedSuccess);");

        stringsSource.Should().Contain("<data name=\"InvalidSessionToken\"");
        stringsSource.Should().Contain("<data name=\"SessionLoadFailed\"");
        stringsSource.Should().Contain("<data name=\"SessionLoadSuccess\"");
        stringsSource.Should().Contain("<data name=\"AccrualConfirmedSuccess\"");
        stringsSource.Should().Contain("<data name=\"RedemptionConfirmedSuccess\"");

        germanStringsSource.Should().Contain("<data name=\"InvalidSessionToken\"");
        germanStringsSource.Should().Contain("<data name=\"SessionLoadFailed\"");
        germanStringsSource.Should().Contain("<data name=\"SessionLoadSuccess\"");
        germanStringsSource.Should().Contain("<data name=\"AccrualConfirmedSuccess\"");
        germanStringsSource.Should().Contain("<data name=\"RedemptionConfirmedSuccess\"");
    }


    [Fact]
    public void MobileBusinessDashboardActivityChrome_Should_KeepLocalizedUnknownActivityFallbackWired()
    {
        var trackerSource = ReadMobileBusinessFile(Path.Combine("Services", "Reporting", "BusinessActivityTracker.cs"));
        var appResourcesSource = ReadMobileBusinessFile(Path.Combine("Resources", "AppResources.cs"));
        var stringsSource = ReadMobileBusinessFile(Path.Combine("Resources", "Strings.resx"));
        var germanStringsSource = ReadMobileBusinessFile(Path.Combine("Resources", "Strings.de.resx"));

        trackerSource.Should().Contain("BusinessActivityKind.CampaignTargetingFixMetricsReset => AppResources.DashboardActivityKindCampaignTargetingFixMetricsReset,");
        trackerSource.Should().Contain("_ => AppResources.DashboardActivityKindUnknown");

        appResourcesSource.Should().Contain("public static string DashboardActivityKindUnknown =>");

        stringsSource.Should().Contain("<data name=\"DashboardActivityKindUnknown\"");
        germanStringsSource.Should().Contain("<data name=\"DashboardActivityKindUnknown\"");
    }


    [Fact]
    public void MobileBusinessRewardsChrome_Should_KeepLocalizedRewardTypeContractsWired()
    {
        var viewModelSource = ReadMobileBusinessFile(Path.Combine("ViewModels", "RewardsViewModel.cs"));
        var modelsSource = ReadMobileBusinessFile(Path.Combine("ViewModels", "RewardsViewModel.Models.cs"));
        var viewSource = ReadMobileBusinessFile(Path.Combine("Views", "RewardsPage.xaml"));
        var appSource = ReadMobileBusinessFile("App.xaml");
        var converterSource = ReadMobileBusinessFile(Path.Combine("Converters", "LocalizedBooleanConverter.cs"));
        var appResourcesSource = ReadMobileBusinessFile(Path.Combine("Resources", "AppResources.cs"));
        var stringsSource = ReadMobileBusinessFile(Path.Combine("Resources", "Strings.resx"));
        var germanStringsSource = ReadMobileBusinessFile(Path.Combine("Resources", "Strings.de.resx"));

        viewModelSource.Should().Contain("RewardTypeOptions = new ObservableCollection<RewardTypeOption>");
        viewModelSource.Should().Contain("new RewardTypeOption(RewardTypeFreeItem, AppResources.RewardsRewardTypeFreeItem)");
        viewModelSource.Should().Contain("new RewardTypeOption(RewardTypePercentDiscount, AppResources.RewardsRewardTypePercentDiscount)");
        viewModelSource.Should().Contain("new RewardTypeOption(RewardTypeAmountDiscount, AppResources.RewardsRewardTypeAmountDiscount)");
        viewModelSource.Should().Contain("public RewardTypeOption? SelectedRewardTypeOption");
        viewModelSource.Should().Contain("_selectedRewardType = value?.Value ?? RewardTypeFreeItem;");
        viewModelSource.Should().Contain("RewardTypeOptions.Any(option => string.Equals(option.Value, SelectedRewardType, StringComparison.OrdinalIgnoreCase))");

        modelsSource.Should().Contain("public string LocalizedRewardType { get; init; } = string.Empty;");
        modelsSource.Should().Contain("LocalizedRewardType = RewardTypeOption.ResolveLabel(item.RewardType),");
        modelsSource.Should().Contain("\"FreeItem\" => AppResources.RewardsRewardTypeFreeItem,");
        modelsSource.Should().Contain("\"PercentDiscount\" => AppResources.RewardsRewardTypePercentDiscount,");
        modelsSource.Should().Contain("\"AmountDiscount\" => AppResources.RewardsRewardTypeAmountDiscount,");
        modelsSource.Should().Contain("_ => AppResources.RewardsRewardTypeUnknown");
        modelsSource.Should().Contain("public string LocalizedCampaignState => ResolveCampaignStateLabel(CampaignState);");
        modelsSource.Should().Contain("PromotionCampaignState.Draft => AppResources.RewardsCampaignStateFilterDraft,");
        modelsSource.Should().Contain("PromotionCampaignState.Scheduled => AppResources.RewardsCampaignStateFilterScheduled,");
        modelsSource.Should().Contain("PromotionCampaignState.Active => AppResources.RewardsCampaignStateFilterActive,");
        modelsSource.Should().Contain("PromotionCampaignState.Expired => AppResources.RewardsCampaignStateFilterExpired,");
        modelsSource.Should().Contain("_ => AppResources.RewardsCampaignStateUnknown");
        modelsSource.Should().Contain("return string.IsNullOrWhiteSpace(audienceKind)");
        modelsSource.Should().Contain("? AppResources.RewardsCampaignAudienceJoinedMembers");
        modelsSource.Should().Contain(": AppResources.RewardsCampaignAudienceUnknown;");
        modelsSource.Should().Contain("return AppResources.RewardsCampaignAudienceSummaryUnknown;");

        appSource.Should().Contain("xmlns:businessConv=\"clr-namespace:Darwin.Mobile.Business.Converters\"");
        appSource.Should().Contain("<businessConv:LocalizedBooleanConverter x:Key=\"LocalizedBooleanConverter\" />");
        viewSource.Should().Contain("ItemDisplayBinding=\"{Binding Label}\"");
        viewSource.Should().Contain("SelectedItem=\"{Binding SelectedRewardTypeOption}\"");
        viewSource.Should().Contain("ItemsSource=\"{Binding CampaignChannelOptions}\" ItemDisplayBinding=\"{Binding Label}\" SelectedItem=\"{Binding SelectedCampaignChannel}\"");
        viewSource.Should().Contain("ItemsSource=\"{Binding CampaignStateFilterOptions}\" ItemDisplayBinding=\"{Binding Label}\" SelectedItem=\"{Binding SelectedCampaignStateFilter}\"");
        viewSource.Should().Contain("ItemsSource=\"{Binding CampaignAudienceFilterOptions}\" ItemDisplayBinding=\"{Binding Label}\" SelectedItem=\"{Binding SelectedCampaignAudienceFilter}\"");
        viewSource.Should().Contain("ItemsSource=\"{Binding CampaignSortOptions}\" ItemDisplayBinding=\"{Binding Label}\" SelectedItem=\"{Binding SelectedCampaignSortOption}\"");
        viewSource.Should().Contain("Text=\"{Binding LocalizedRewardType}\"");
        viewSource.Should().Contain("Text=\"{Binding LocalizedCampaignState, StringFormat='{x:Static res:AppResources.RewardsCampaignStateFormat}'}\"");
        viewSource.Should().Contain("Text=\"{Binding AllowSelfRedemption, Converter={StaticResource LocalizedBooleanConverter}, StringFormat='{x:Static res:AppResources.RewardsTierSelfRedemptionFormat}'}\"");
        viewModelSource.Should().Contain("PromotionAudienceKind.JoinedMembers => AppResources.RewardsCampaignTargetingHintJoinedMembers,");
        viewModelSource.Should().Contain(": null;");
        viewModelSource.Should().Contain("_ => AppResources.RewardsCampaignTargetingHintUnknown");
        modelsSource.Should().Contain("public sealed class CampaignChannelOption");
        modelsSource.Should().Contain("public sealed class CampaignStateFilterOption");
        modelsSource.Should().Contain("public sealed class CampaignAudienceFilterOption");
        modelsSource.Should().Contain("public const string UnknownAudienceKindKey = \"__unknown\";");
        modelsSource.Should().Contain("public sealed class CampaignSortOption");
        modelsSource.Should().Contain("public string Label { get; }");
        modelsSource.Should().Contain("public string DisplayName => Label;");
        modelsSource.Should().Contain("? CampaignAudienceFilterOption.UnknownAudienceKindKey");
        modelsSource.Should().Contain("return CampaignAudienceFilterOption.UnknownAudienceKindKey;");
        converterSource.Should().Contain("public sealed class LocalizedBooleanConverter : Microsoft.Maui.Controls.IValueConverter");
        converterSource.Should().Contain("true => AppResources.CommonYes,");
        converterSource.Should().Contain("false => AppResources.CommonNo,");
        viewModelSource.Should().Contain("new CampaignAudienceFilterOption(CampaignAudienceFilterOption.UnknownAudienceKindKey, AppResources.RewardsCampaignAudienceUnknown)");
        viewModelSource.Should().Contain("public int UnknownAudienceCampaignCount => CountCampaignsByAudienceKind(CampaignAudienceFilterOption.UnknownAudienceKindKey);");
        viewModelSource.Should().Contain("AppResources.RewardsCampaignAudienceUnknown,");
        viewModelSource.Should().Contain("UnknownAudienceCampaignCount.ToString(CultureInfo.InvariantCulture)");
        viewModelSource.Should().Contain("OnPropertyChanged(nameof(UnknownAudienceCampaignCount));");
        viewModelSource.Should().Contain("OnPropertyChanged(nameof(UnknownAudienceCampaignMetricText));");

        appResourcesSource.Should().Contain("public static string CommonYes =>");
        appResourcesSource.Should().Contain("public static string CommonNo =>");
        appResourcesSource.Should().Contain("public static string RewardsRewardTypeFreeItem =>");
        appResourcesSource.Should().Contain("public static string RewardsRewardTypePercentDiscount =>");
        appResourcesSource.Should().Contain("public static string RewardsRewardTypeAmountDiscount =>");
        appResourcesSource.Should().Contain("public static string RewardsRewardTypeUnknown =>");
        appResourcesSource.Should().Contain("public static string RewardsCampaignStateUnknown =>");
        appResourcesSource.Should().Contain("public static string RewardsCampaignAudienceUnknown =>");
        appResourcesSource.Should().Contain("public static string RewardsCampaignTargetingHintUnknown =>");
        appResourcesSource.Should().Contain("public static string RewardsCampaignAudienceSummaryUnknown =>");

        stringsSource.Should().Contain("<data name=\"CommonYes\"");
        stringsSource.Should().Contain("<data name=\"CommonNo\"");
        stringsSource.Should().Contain("<data name=\"RewardsRewardTypeFreeItem\"");
        stringsSource.Should().Contain("<data name=\"RewardsRewardTypePercentDiscount\"");
        stringsSource.Should().Contain("<data name=\"RewardsRewardTypeAmountDiscount\"");
        stringsSource.Should().Contain("<data name=\"RewardsRewardTypeUnknown\"");
        stringsSource.Should().Contain("<data name=\"RewardsCampaignStateUnknown\"");
        stringsSource.Should().Contain("<data name=\"RewardsCampaignAudienceUnknown\"");
        stringsSource.Should().Contain("<data name=\"RewardsCampaignTargetingHintUnknown\"");
        stringsSource.Should().Contain("<data name=\"RewardsCampaignAudienceSummaryUnknown\"");

        germanStringsSource.Should().Contain("<data name=\"CommonYes\"");
        germanStringsSource.Should().Contain("<data name=\"CommonNo\"");
        germanStringsSource.Should().Contain("<data name=\"RewardsRewardTypeFreeItem\"");
        germanStringsSource.Should().Contain("<data name=\"RewardsRewardTypePercentDiscount\"");
        germanStringsSource.Should().Contain("<data name=\"RewardsRewardTypeAmountDiscount\"");
        germanStringsSource.Should().Contain("<data name=\"RewardsRewardTypeUnknown\"");
        germanStringsSource.Should().Contain("<data name=\"RewardsCampaignStateUnknown\"");
        germanStringsSource.Should().Contain("<data name=\"RewardsCampaignAudienceUnknown\"");
        germanStringsSource.Should().Contain("<data name=\"RewardsCampaignTargetingHintUnknown\"");
        germanStringsSource.Should().Contain("<data name=\"RewardsCampaignAudienceSummaryUnknown\"");
    }

    [Fact]
    public void MobileBusinessOperatorRoleChrome_Should_KeepLocalizedAuthorizationRoleContractsWired()
    {
        var homeViewSource = ReadMobileBusinessFile(Path.Combine("Views", "HomePage.xaml"));
        var homeViewModelSource = ReadMobileBusinessFile(Path.Combine("ViewModels", "HomeViewModel.cs"));
        var authorizationServiceSource = ReadMobileBusinessFile(Path.Combine("Services", "Identity", "BusinessAuthorizationService.cs"));
        var staffAccessBadgeSource = ReadMobileBusinessFile(Path.Combine("ViewModels", "StaffAccessBadgeViewModel.cs"));
        var appResourcesSource = ReadMobileBusinessFile(Path.Combine("Resources", "AppResources.cs"));
        var stringsSource = ReadMobileBusinessFile(Path.Combine("Resources", "Strings.resx"));
        var germanStringsSource = ReadMobileBusinessFile(Path.Combine("Resources", "Strings.de.resx"));

        homeViewSource.Should().Contain("Text=\"{x:Static res:AppResources.HomeOperatorRoleLabel}\"");
        homeViewSource.Should().Contain("Text=\"{Binding OperatorRole}\"");

        homeViewModelSource.Should().Contain("private string _operatorRole = AppResources.HomeUnavailableValue;");
        homeViewModelSource.Should().Contain("OperatorRole = authSnapshotResult.Succeeded && authSnapshotResult.Value is not null");
        homeViewModelSource.Should().Contain("? authSnapshotResult.Value.RoleDisplayName");
        homeViewModelSource.Should().Contain(": AppResources.HomeUnavailableValue;");

        authorizationServiceSource.Should().Contain("public string RoleDisplayName { get; init; } = AppResources.AuthorizationRoleUnknown;");
        authorizationServiceSource.Should().Contain("? AppResources.AuthorizationRoleAdministrator");
        authorizationServiceSource.Should().Contain("? AppResources.AuthorizationRoleBusinessOperator");
        authorizationServiceSource.Should().Contain("? AppResources.AuthorizationRoleBusinessOperatorLegacy");
        authorizationServiceSource.Should().Contain(": AppResources.AuthorizationRoleRestricted");

        staffAccessBadgeSource.Should().Contain("var role = authorizationResult.Succeeded && authorizationResult.Value is not null");
        staffAccessBadgeSource.Should().Contain("? authorizationResult.Value.RoleDisplayName");
        staffAccessBadgeSource.Should().Contain(": AppResources.StaffAccessBadgeUnknownRole;");
        staffAccessBadgeSource.Should().Contain("OperatorRoleDisplay = string.Format(AppResources.StaffAccessBadgeRoleFormat, role);");

        appResourcesSource.Should().Contain("public static string HomeOperatorRoleLabel =>");
        appResourcesSource.Should().Contain("public static string AuthorizationRoleUnknown =>");
        appResourcesSource.Should().Contain("public static string AuthorizationRoleAdministrator =>");
        appResourcesSource.Should().Contain("public static string AuthorizationRoleBusinessOperator =>");
        appResourcesSource.Should().Contain("public static string AuthorizationRoleBusinessOperatorLegacy =>");
        appResourcesSource.Should().Contain("public static string AuthorizationRoleRestricted =>");
        appResourcesSource.Should().Contain("public static string StaffAccessBadgeRoleFormat =>");

        stringsSource.Should().Contain("<data name=\"HomeOperatorRoleLabel\"");
        stringsSource.Should().Contain("<data name=\"AuthorizationRoleUnknown\"");
        stringsSource.Should().Contain("<data name=\"AuthorizationRoleAdministrator\"");
        stringsSource.Should().Contain("<data name=\"AuthorizationRoleBusinessOperator\"");
        stringsSource.Should().Contain("<data name=\"AuthorizationRoleBusinessOperatorLegacy\"");
        stringsSource.Should().Contain("<data name=\"AuthorizationRoleRestricted\"");
        stringsSource.Should().Contain("<data name=\"StaffAccessBadgeRoleFormat\"");

        germanStringsSource.Should().Contain("<data name=\"HomeOperatorRoleLabel\"");
        germanStringsSource.Should().Contain("<data name=\"AuthorizationRoleUnknown\"");
        germanStringsSource.Should().Contain("<data name=\"AuthorizationRoleAdministrator\"");
        germanStringsSource.Should().Contain("<data name=\"AuthorizationRoleBusinessOperator\"");
        germanStringsSource.Should().Contain("<data name=\"AuthorizationRoleBusinessOperatorLegacy\"");
        germanStringsSource.Should().Contain("<data name=\"AuthorizationRoleRestricted\"");
        germanStringsSource.Should().Contain("<data name=\"StaffAccessBadgeRoleFormat\"");
    }


    [Fact]
    public void MobileConsumerCommerceChrome_Should_KeepLocalizedOrderAndInvoiceStatusContractsWired()
    {
        var viewModelSource = ReadMobileConsumerFile(Path.Combine("ViewModels", "MemberCommerceViewModel.cs"));
        var appResourcesSource = ReadMobileConsumerFile(Path.Combine("Resources", "AppResources.cs"));
        var stringsSource = ReadMobileConsumerFile(Path.Combine("Resources", "Strings.resx"));
        var germanStringsSource = ReadMobileConsumerFile(Path.Combine("Resources", "Strings.de.resx"));

        viewModelSource.Should().Contain("StatusText = string.Format(AppResources.MemberCommerceOrderStatusFormat, LocalizeOrderStatus(order.Status))");
        viewModelSource.Should().Contain("StatusText = string.Format(AppResources.MemberCommerceInvoiceStatusFormat, LocalizeInvoiceStatus(invoice.Status))");
        viewModelSource.Should().Contain("\"Created\" => AppResources.MemberCommerceStatusCreated,");
        viewModelSource.Should().Contain("\"Confirmed\" => AppResources.MemberCommerceStatusConfirmed,");
        viewModelSource.Should().Contain("\"Paid\" => AppResources.MemberCommerceStatusPaid,");
        viewModelSource.Should().Contain("\"PartiallyShipped\" => AppResources.MemberCommerceStatusPartiallyShipped,");
        viewModelSource.Should().Contain("\"Shipped\" => AppResources.MemberCommerceStatusShipped,");
        viewModelSource.Should().Contain("\"Delivered\" => AppResources.MemberCommerceStatusDelivered,");
        viewModelSource.Should().Contain("\"Cancelled\" => AppResources.MemberCommerceStatusCancelled,");
        viewModelSource.Should().Contain("\"Refunded\" => AppResources.MemberCommerceStatusRefunded,");
        viewModelSource.Should().Contain("\"PartiallyRefunded\" => AppResources.MemberCommerceStatusPartiallyRefunded,");
        viewModelSource.Should().Contain("\"Completed\" => AppResources.MemberCommerceStatusCompleted,");
        viewModelSource.Should().Contain("\"Draft\" => AppResources.MemberCommerceStatusDraft,");
        viewModelSource.Should().Contain("\"Open\" => AppResources.MemberCommerceStatusOpen,");
        viewModelSource.Should().Contain("_ => AppResources.MemberCommerceStatusUnknown");
        viewModelSource.Should().Contain("OpenOrderShipmentTrackingCommand = new AsyncCommand<MemberCommerceShipmentSummaryViewModel>(OpenOrderShipmentTrackingAsync, CanOpenOrderShipmentTracking);");
        viewModelSource.Should().Contain("ShipmentSummaries = order.Shipments.Select(MapShipmentSummary).ToArray(),");
        viewModelSource.Should().Contain("await Browser.Default.OpenAsync(shipment.TrackingUrl, BrowserLaunchMode.SystemPreferred).ConfigureAwait(false);");
        viewModelSource.Should().Contain("TrackingUrl = string.IsNullOrWhiteSpace(shipment.TrackingUrl) ? null : shipment.TrackingUrl,");
        viewModelSource.Should().Contain("public sealed class MemberCommerceShipmentSummaryViewModel");

        appResourcesSource.Should().Contain("public static string MemberCommerceStatusCreated =>");
        appResourcesSource.Should().Contain("public static string MemberCommerceStatusConfirmed =>");
        appResourcesSource.Should().Contain("public static string MemberCommerceStatusPaid =>");
        appResourcesSource.Should().Contain("public static string MemberCommerceStatusPartiallyShipped =>");
        appResourcesSource.Should().Contain("public static string MemberCommerceStatusShipped =>");
        appResourcesSource.Should().Contain("public static string MemberCommerceStatusDelivered =>");
        appResourcesSource.Should().Contain("public static string MemberCommerceStatusCancelled =>");
        appResourcesSource.Should().Contain("public static string MemberCommerceStatusRefunded =>");
        appResourcesSource.Should().Contain("public static string MemberCommerceStatusPartiallyRefunded =>");
        appResourcesSource.Should().Contain("public static string MemberCommerceStatusCompleted =>");
        appResourcesSource.Should().Contain("public static string MemberCommerceStatusDraft =>");
        appResourcesSource.Should().Contain("public static string MemberCommerceStatusOpen =>");
        appResourcesSource.Should().Contain("public static string MemberCommerceStatusUnknown =>");
        appResourcesSource.Should().Contain("public static string MemberCommerceOpenTrackingButton =>");
        appResourcesSource.Should().Contain("public static string MemberCommerceTrackingOpenFailed =>");

        stringsSource.Should().Contain("<data name=\"MemberCommerceStatusCreated\"");
        stringsSource.Should().Contain("<data name=\"MemberCommerceStatusConfirmed\"");
        stringsSource.Should().Contain("<data name=\"MemberCommerceStatusPaid\"");
        stringsSource.Should().Contain("<data name=\"MemberCommerceStatusPartiallyShipped\"");
        stringsSource.Should().Contain("<data name=\"MemberCommerceStatusShipped\"");
        stringsSource.Should().Contain("<data name=\"MemberCommerceStatusDelivered\"");
        stringsSource.Should().Contain("<data name=\"MemberCommerceStatusCancelled\"");
        stringsSource.Should().Contain("<data name=\"MemberCommerceStatusRefunded\"");
        stringsSource.Should().Contain("<data name=\"MemberCommerceStatusPartiallyRefunded\"");
        stringsSource.Should().Contain("<data name=\"MemberCommerceStatusCompleted\"");
        stringsSource.Should().Contain("<data name=\"MemberCommerceStatusDraft\"");
        stringsSource.Should().Contain("<data name=\"MemberCommerceStatusOpen\"");
        stringsSource.Should().Contain("<data name=\"MemberCommerceStatusUnknown\"");
        stringsSource.Should().Contain("<data name=\"MemberCommerceOpenTrackingButton\"");
        stringsSource.Should().Contain("<data name=\"MemberCommerceTrackingOpenFailed\"");

        germanStringsSource.Should().Contain("<data name=\"MemberCommerceStatusCreated\"");
        germanStringsSource.Should().Contain("<data name=\"MemberCommerceStatusConfirmed\"");
        germanStringsSource.Should().Contain("<data name=\"MemberCommerceStatusPaid\"");
        germanStringsSource.Should().Contain("<data name=\"MemberCommerceStatusPartiallyShipped\"");
        germanStringsSource.Should().Contain("<data name=\"MemberCommerceStatusShipped\"");
        germanStringsSource.Should().Contain("<data name=\"MemberCommerceStatusDelivered\"");
        germanStringsSource.Should().Contain("<data name=\"MemberCommerceStatusCancelled\"");
        germanStringsSource.Should().Contain("<data name=\"MemberCommerceStatusRefunded\"");
        germanStringsSource.Should().Contain("<data name=\"MemberCommerceStatusPartiallyRefunded\"");
        germanStringsSource.Should().Contain("<data name=\"MemberCommerceStatusCompleted\"");
        germanStringsSource.Should().Contain("<data name=\"MemberCommerceStatusDraft\"");
        germanStringsSource.Should().Contain("<data name=\"MemberCommerceStatusOpen\"");
        germanStringsSource.Should().Contain("<data name=\"MemberCommerceStatusUnknown\"");
        germanStringsSource.Should().Contain("<data name=\"MemberCommerceOpenTrackingButton\"");
        germanStringsSource.Should().Contain("<data name=\"MemberCommerceTrackingOpenFailed\"");
    }


    [Fact]
    public void MobileProfileAndScanPlaceholders_Should_KeepResourceBackedNonAdminContractsWired()
    {
        var businessProfileViewSource = ReadMobileBusinessFile(Path.Combine("Views", "ProfilePage.xaml"));
        var businessQrScanViewSource = ReadMobileBusinessFile(Path.Combine("Views", "QrScanPage.xaml"));
        var businessAppResourcesSource = ReadMobileBusinessFile(Path.Combine("Resources", "AppResources.cs"));
        var businessStringsSource = ReadMobileBusinessFile(Path.Combine("Resources", "Strings.resx"));
        var businessGermanStringsSource = ReadMobileBusinessFile(Path.Combine("Resources", "Strings.de.resx"));

        var consumerProfileViewSource = ReadMobileConsumerFile(Path.Combine("Views", "ProfilePage.xaml"));
        var consumerAppResourcesSource = ReadMobileConsumerFile(Path.Combine("Resources", "AppResources.cs"));
        var consumerStringsSource = ReadMobileConsumerFile(Path.Combine("Resources", "Strings.resx"));
        var consumerGermanStringsSource = ReadMobileConsumerFile(Path.Combine("Resources", "Strings.de.resx"));

        businessProfileViewSource.Should().Contain("Placeholder=\"{x:Static res:AppResources.ProfilePhonePlaceholder}\"");
        businessProfileViewSource.Should().Contain("Placeholder=\"{x:Static res:AppResources.ProfileLocalePlaceholder}\"");
        businessProfileViewSource.Should().Contain("Placeholder=\"{x:Static res:AppResources.ProfileTimezonePlaceholder}\"");
        businessProfileViewSource.Should().Contain("Placeholder=\"{x:Static res:AppResources.ProfileCurrencyPlaceholder}\"");
        businessQrScanViewSource.Should().Contain("Title=\"{x:Static res:AppResources.ScanTitle}\"");
        businessQrScanViewSource.Should().Contain("Text=\"{x:Static res:AppResources.ScannerManualTokenCancel}\"");

        businessAppResourcesSource.Should().Contain("public static string ProfilePhonePlaceholder =>");
        businessAppResourcesSource.Should().Contain("public static string ProfileLocalePlaceholder =>");
        businessAppResourcesSource.Should().Contain("public static string ProfileTimezonePlaceholder =>");
        businessAppResourcesSource.Should().Contain("public static string ProfileCurrencyPlaceholder =>");

        consumerProfileViewSource.Should().Contain("Placeholder=\"{x:Static res:AppResources.ProfilePhonePlaceholder}\"");
        consumerProfileViewSource.Should().Contain("Placeholder=\"{x:Static res:AppResources.ProfileLocalePlaceholder}\"");
        consumerProfileViewSource.Should().Contain("Placeholder=\"{x:Static res:AppResources.ProfileCurrencyPlaceholder}\"");
        consumerProfileViewSource.Should().Contain("Placeholder=\"{x:Static res:AppResources.ProfileTimezonePlaceholder}\"");
        consumerProfileViewSource.Should().Contain("ItemsSource=\"{Binding PhoneVerificationChannelOptions}\"");
        consumerProfileViewSource.Should().Contain("ItemDisplayBinding=\"{Binding DisplayName}\"");
        consumerProfileViewSource.Should().Contain("SelectedItem=\"{Binding SelectedPhoneVerificationChannel}\"");

        consumerAppResourcesSource.Should().Contain("public static string ProfilePhonePlaceholder =>");
        consumerAppResourcesSource.Should().Contain("public static string ProfileLocalePlaceholder =>");
        consumerAppResourcesSource.Should().Contain("public static string ProfileCurrencyPlaceholder =>");
        consumerAppResourcesSource.Should().Contain("public static string ProfileTimezonePlaceholder =>");
        consumerAppResourcesSource.Should().Contain("public static string ProfilePhoneVerificationSmsOption =>");
        consumerAppResourcesSource.Should().Contain("public static string ProfilePhoneVerificationWhatsAppOption =>");

        businessStringsSource.Should().Contain("<data name=\"ProfilePhonePlaceholder\"");
        businessStringsSource.Should().Contain("<data name=\"ProfileLocalePlaceholder\"");
        businessStringsSource.Should().Contain("<data name=\"ProfileTimezonePlaceholder\"");
        businessStringsSource.Should().Contain("<data name=\"ProfileCurrencyPlaceholder\"");
        businessGermanStringsSource.Should().Contain("<data name=\"ProfilePhonePlaceholder\"");
        businessGermanStringsSource.Should().Contain("<data name=\"ProfileLocalePlaceholder\"");
        businessGermanStringsSource.Should().Contain("<data name=\"ProfileTimezonePlaceholder\"");
        businessGermanStringsSource.Should().Contain("<data name=\"ProfileCurrencyPlaceholder\"");

        consumerStringsSource.Should().Contain("<data name=\"ProfilePhonePlaceholder\"");
        consumerStringsSource.Should().Contain("<data name=\"ProfileLocalePlaceholder\"");
        consumerStringsSource.Should().Contain("<data name=\"ProfileCurrencyPlaceholder\"");
        consumerStringsSource.Should().Contain("<data name=\"ProfileTimezonePlaceholder\"");
        consumerStringsSource.Should().Contain("<data name=\"ProfilePhoneVerificationSmsOption\"");
        consumerStringsSource.Should().Contain("<data name=\"ProfilePhoneVerificationWhatsAppOption\"");
        consumerGermanStringsSource.Should().Contain("<data name=\"ProfilePhonePlaceholder\"");
        consumerGermanStringsSource.Should().Contain("<data name=\"ProfileLocalePlaceholder\"");
        consumerGermanStringsSource.Should().Contain("<data name=\"ProfileCurrencyPlaceholder\"");
        consumerGermanStringsSource.Should().Contain("<data name=\"ProfileTimezonePlaceholder\"");
        consumerGermanStringsSource.Should().Contain("<data name=\"ProfilePhoneVerificationSmsOption\"");
        consumerGermanStringsSource.Should().Contain("<data name=\"ProfilePhoneVerificationWhatsAppOption\"");
    }


    [Fact]
    public void MobileConsumerRewardsHistoryChrome_Should_KeepLocalizedTransactionTypeContractsWired()
    {
        var appSource = ReadMobileConsumerFile("App.xaml");
        var rewardsPageSource = ReadMobileConsumerFile(Path.Combine("Views", "RewardsPage.xaml"));
        var converterSource = ReadMobileConsumerFile(Path.Combine("Converters", "PointsTransactionTypeConverter.cs"));
        var timelineKindConverterSource = ReadMobileConsumerFile(Path.Combine("Converters", "LoyaltyTimelineKindConverter.cs"));
        var feedPageSource = ReadMobileConsumerFile(Path.Combine("Views", "FeedPage.xaml"));
        var businessDetailPageSource = ReadMobileConsumerFile(Path.Combine("Views", "BusinessDetailPage.xaml"));
        var businessRewardConverterSource = ReadMobileConsumerFile(Path.Combine("Converters", "BusinessRewardTypeConverter.cs"));
        var localizedBooleanConverterSource = ReadMobileConsumerFile(Path.Combine("Converters", "LocalizedBooleanConverter.cs"));
        var appResourcesSource = ReadMobileConsumerFile(Path.Combine("Resources", "AppResources.cs"));
        var stringsSource = ReadMobileConsumerFile(Path.Combine("Resources", "Strings.resx"));
        var germanStringsSource = ReadMobileConsumerFile(Path.Combine("Resources", "Strings.de.resx"));

        appSource.Should().Contain("xmlns:consumerConv=\"clr-namespace:Darwin.Mobile.Consumer.Converters\"");
        appSource.Should().Contain("<consumerConv:BusinessRewardTypeConverter x:Key=\"BusinessRewardTypeConverter\" />");
        appSource.Should().Contain("<consumerConv:LoyaltyTimelineKindConverter x:Key=\"LoyaltyTimelineKindConverter\" />");
        appSource.Should().Contain("<consumerConv:LocalizedBooleanConverter x:Key=\"LocalizedBooleanConverter\" />");
        appSource.Should().Contain("<consumerConv:PointsTransactionTypeConverter x:Key=\"PointsTransactionTypeConverter\" />");
        rewardsPageSource.Should().Contain("Text=\"{Binding Type, Converter={StaticResource PointsTransactionTypeConverter}}\"");
        feedPageSource.Should().Contain("Text=\"{Binding Kind, Converter={StaticResource LoyaltyTimelineKindConverter}}\"");
        businessDetailPageSource.Should().Contain("Text=\"{Binding Business.LoyaltyProgramPublic.IsActive, Converter={StaticResource LocalizedBooleanConverter}, StringFormat={x:Static res:AppResources.BusinessActiveFormat}}\"");
        businessDetailPageSource.Should().Contain("Text=\"{Binding RewardType, Converter={StaticResource BusinessRewardTypeConverter}, StringFormat={x:Static res:AppResources.BusinessRewardTypeFormat}}\"");
        businessDetailPageSource.Should().Contain("Text=\"{Binding AllowSelfRedemption, Converter={StaticResource LocalizedBooleanConverter}, StringFormat={x:Static res:AppResources.BusinessSelfRedemptionFormat}}\"");
        businessRewardConverterSource.Should().Contain("public sealed class BusinessRewardTypeConverter : Microsoft.Maui.Controls.IValueConverter");
        businessRewardConverterSource.Should().Contain("\"FreeItem\" => AppResources.BusinessRewardTypeFreeItem,");
        businessRewardConverterSource.Should().Contain("\"PercentDiscount\" => AppResources.BusinessRewardTypePercentDiscount,");
        businessRewardConverterSource.Should().Contain("\"AmountDiscount\" => AppResources.BusinessRewardTypeAmountDiscount,");
        businessRewardConverterSource.Should().Contain("_ => AppResources.BusinessRewardTypeUnknown");
        timelineKindConverterSource.Should().Contain("public sealed class LoyaltyTimelineKindConverter : Microsoft.Maui.Controls.IValueConverter");
        timelineKindConverterSource.Should().Contain("LoyaltyTimelineEntryKind.PointsTransaction => AppResources.FeedTimelineKindPointsTransaction,");
        timelineKindConverterSource.Should().Contain("LoyaltyTimelineEntryKind.RewardRedemption => AppResources.FeedTimelineKindRewardRedemption,");
        timelineKindConverterSource.Should().Contain("_ => AppResources.FeedTimelineKindUnknown");
        localizedBooleanConverterSource.Should().Contain("public sealed class LocalizedBooleanConverter : Microsoft.Maui.Controls.IValueConverter");
        localizedBooleanConverterSource.Should().Contain("true => AppResources.CommonYes,");
        localizedBooleanConverterSource.Should().Contain("false => AppResources.CommonNo,");
        converterSource.Should().Contain("public sealed class PointsTransactionTypeConverter : IValueConverter");
        converterSource.Should().Contain("\"Accrual\" => AppResources.RewardsTransactionTypeAccrual,");
        converterSource.Should().Contain("\"Redemption\" => AppResources.RewardsTransactionTypeRedemption,");
        converterSource.Should().Contain("\"Adjustment\" => AppResources.RewardsTransactionTypeAdjustment,");
        converterSource.Should().Contain("_ => AppResources.RewardsTransactionTypeUnknown");
        appResourcesSource.Should().Contain("public static string CommonYes =>");
        appResourcesSource.Should().Contain("public static string CommonNo =>");
        appResourcesSource.Should().Contain("public static string BusinessRewardTypeFreeItem =>");
        appResourcesSource.Should().Contain("public static string BusinessRewardTypePercentDiscount =>");
        appResourcesSource.Should().Contain("public static string BusinessRewardTypeAmountDiscount =>");
        appResourcesSource.Should().Contain("public static string BusinessRewardTypeUnknown =>");
        appResourcesSource.Should().Contain("public static string FeedTimelineKindPointsTransaction =>");
        appResourcesSource.Should().Contain("public static string FeedTimelineKindRewardRedemption =>");
        appResourcesSource.Should().Contain("public static string FeedTimelineKindUnknown =>");
        appResourcesSource.Should().Contain("public static string RewardsTransactionTypeAccrual =>");
        appResourcesSource.Should().Contain("public static string RewardsTransactionTypeRedemption =>");
        appResourcesSource.Should().Contain("public static string RewardsTransactionTypeAdjustment =>");
        appResourcesSource.Should().Contain("public static string RewardsTransactionTypeUnknown =>");
        stringsSource.Should().Contain("<data name=\"CommonYes\"");
        stringsSource.Should().Contain("<data name=\"CommonNo\"");
        stringsSource.Should().Contain("<data name=\"BusinessRewardTypeFreeItem\"");
        stringsSource.Should().Contain("<data name=\"BusinessRewardTypePercentDiscount\"");
        stringsSource.Should().Contain("<data name=\"BusinessRewardTypeAmountDiscount\"");
        stringsSource.Should().Contain("<data name=\"BusinessRewardTypeUnknown\"");
        stringsSource.Should().Contain("<data name=\"FeedTimelineKindPointsTransaction\"");
        stringsSource.Should().Contain("<data name=\"FeedTimelineKindRewardRedemption\"");
        stringsSource.Should().Contain("<data name=\"FeedTimelineKindUnknown\"");
        stringsSource.Should().Contain("<data name=\"RewardsTransactionTypeAccrual\"");
        stringsSource.Should().Contain("<data name=\"RewardsTransactionTypeRedemption\"");
        stringsSource.Should().Contain("<data name=\"RewardsTransactionTypeAdjustment\"");
        stringsSource.Should().Contain("<data name=\"RewardsTransactionTypeUnknown\"");
        germanStringsSource.Should().Contain("<data name=\"CommonYes\"");
        germanStringsSource.Should().Contain("<data name=\"CommonNo\"");
        germanStringsSource.Should().Contain("<data name=\"BusinessRewardTypeFreeItem\"");
        germanStringsSource.Should().Contain("<data name=\"BusinessRewardTypePercentDiscount\"");
        germanStringsSource.Should().Contain("<data name=\"BusinessRewardTypeAmountDiscount\"");
        germanStringsSource.Should().Contain("<data name=\"BusinessRewardTypeUnknown\"");
        germanStringsSource.Should().Contain("<data name=\"FeedTimelineKindPointsTransaction\"");
        germanStringsSource.Should().Contain("<data name=\"FeedTimelineKindRewardRedemption\"");
        germanStringsSource.Should().Contain("<data name=\"FeedTimelineKindUnknown\"");
        germanStringsSource.Should().Contain("<data name=\"RewardsTransactionTypeAccrual\"");
        germanStringsSource.Should().Contain("<data name=\"RewardsTransactionTypeRedemption\"");
        germanStringsSource.Should().Contain("<data name=\"RewardsTransactionTypeAdjustment\"");
        germanStringsSource.Should().Contain("<data name=\"RewardsTransactionTypeUnknown\"");
    }


    [Fact]
    public void MobileConsumerCustomerContextChrome_Should_KeepLocalizedConsentAndInteractionTokenContractsWired()
    {
        var viewModelSource = ReadMobileConsumerFile(Path.Combine("ViewModels", "MemberCustomerContextViewModel.cs"));
        var appResourcesSource = ReadMobileConsumerFile(Path.Combine("Resources", "AppResources.cs"));
        var stringsSource = ReadMobileConsumerFile(Path.Combine("Resources", "Strings.resx"));
        var germanStringsSource = ReadMobileConsumerFile(Path.Combine("Resources", "Strings.de.resx"));

        viewModelSource.Should().Contain("Type = LocalizeConsentType(consent.Type),");
        viewModelSource.Should().Contain("LocalizeInteractionType(interaction.Type)");
        viewModelSource.Should().Contain("LocalizeInteractionChannel(interaction.Channel)");
        viewModelSource.Should().Contain("\"MarketingEmail\" => AppResources.MemberCustomerContextConsentTypeMarketingEmail,");
        viewModelSource.Should().Contain("\"Sms\" => AppResources.ProfilePhoneVerificationSmsOption,");
        viewModelSource.Should().Contain("\"TermsOfService\" => AppResources.MemberCustomerContextConsentTypeTermsOfService,");
        viewModelSource.Should().Contain("_ => AppResources.MemberCustomerContextConsentTypeUnknown");
        viewModelSource.Should().Contain("\"Call\" => AppResources.MemberCustomerContextInteractionTypeCall,");
        viewModelSource.Should().Contain("\"Meeting\" => AppResources.MemberCustomerContextInteractionTypeMeeting,");
        viewModelSource.Should().Contain("\"Order\" => AppResources.MemberCustomerContextInteractionTypeOrder,");
        viewModelSource.Should().Contain("\"Support\" => AppResources.MemberCustomerContextInteractionTypeSupport,");
        viewModelSource.Should().Contain("_ => AppResources.MemberCustomerContextInteractionTypeUnknown");
        viewModelSource.Should().Contain("\"Phone\" => AppResources.PhoneLabel,");
        viewModelSource.Should().Contain("\"Chat\" => AppResources.MemberCustomerContextInteractionChannelChat,");
        viewModelSource.Should().Contain("\"InPerson\" => AppResources.MemberCustomerContextInteractionChannelInPerson,");
        viewModelSource.Should().Contain("_ => AppResources.MemberCustomerContextInteractionChannelUnknown");

        appResourcesSource.Should().Contain("public static string MemberCustomerContextConsentTypeMarketingEmail =>");
        appResourcesSource.Should().Contain("public static string MemberCustomerContextConsentTypeTermsOfService =>");
        appResourcesSource.Should().Contain("public static string MemberCustomerContextConsentTypeUnknown =>");
        appResourcesSource.Should().Contain("public static string MemberCustomerContextInteractionTypeCall =>");
        appResourcesSource.Should().Contain("public static string MemberCustomerContextInteractionTypeMeeting =>");
        appResourcesSource.Should().Contain("public static string MemberCustomerContextInteractionTypeOrder =>");
        appResourcesSource.Should().Contain("public static string MemberCustomerContextInteractionTypeSupport =>");
        appResourcesSource.Should().Contain("public static string MemberCustomerContextInteractionTypeUnknown =>");
        appResourcesSource.Should().Contain("public static string MemberCustomerContextInteractionChannelChat =>");
        appResourcesSource.Should().Contain("public static string MemberCustomerContextInteractionChannelInPerson =>");
        appResourcesSource.Should().Contain("public static string MemberCustomerContextInteractionChannelUnknown =>");

        stringsSource.Should().Contain("<data name=\"MemberCustomerContextConsentTypeMarketingEmail\"");
        stringsSource.Should().Contain("<data name=\"MemberCustomerContextConsentTypeTermsOfService\"");
        stringsSource.Should().Contain("<data name=\"MemberCustomerContextConsentTypeUnknown\"");
        stringsSource.Should().Contain("<data name=\"MemberCustomerContextInteractionTypeCall\"");
        stringsSource.Should().Contain("<data name=\"MemberCustomerContextInteractionTypeMeeting\"");
        stringsSource.Should().Contain("<data name=\"MemberCustomerContextInteractionTypeOrder\"");
        stringsSource.Should().Contain("<data name=\"MemberCustomerContextInteractionTypeSupport\"");
        stringsSource.Should().Contain("<data name=\"MemberCustomerContextInteractionTypeUnknown\"");
        stringsSource.Should().Contain("<data name=\"MemberCustomerContextInteractionChannelChat\"");
        stringsSource.Should().Contain("<data name=\"MemberCustomerContextInteractionChannelInPerson\"");
        stringsSource.Should().Contain("<data name=\"MemberCustomerContextInteractionChannelUnknown\"");

        germanStringsSource.Should().Contain("<data name=\"MemberCustomerContextConsentTypeMarketingEmail\"");
        germanStringsSource.Should().Contain("<data name=\"MemberCustomerContextConsentTypeTermsOfService\"");
        germanStringsSource.Should().Contain("<data name=\"MemberCustomerContextConsentTypeUnknown\"");
        germanStringsSource.Should().Contain("<data name=\"MemberCustomerContextInteractionTypeCall\"");
        germanStringsSource.Should().Contain("<data name=\"MemberCustomerContextInteractionTypeMeeting\"");
        germanStringsSource.Should().Contain("<data name=\"MemberCustomerContextInteractionTypeOrder\"");
        germanStringsSource.Should().Contain("<data name=\"MemberCustomerContextInteractionTypeSupport\"");
        germanStringsSource.Should().Contain("<data name=\"MemberCustomerContextInteractionTypeUnknown\"");
        germanStringsSource.Should().Contain("<data name=\"MemberCustomerContextInteractionChannelChat\"");
        germanStringsSource.Should().Contain("<data name=\"MemberCustomerContextInteractionChannelInPerson\"");
        germanStringsSource.Should().Contain("<data name=\"MemberCustomerContextInteractionChannelUnknown\"");
    }


    [Fact]
    public void MobileBusinessInvitationPreviewChrome_Should_KeepLocalizedRoleAndStatusDisplayContractsWired()
    {
        var invitationViewModelSource = ReadMobileBusinessFile(Path.Combine("ViewModels", "AcceptInvitationViewModel.cs"));
        var invitationViewSource = ReadMobileBusinessFile(Path.Combine("Views", "AcceptInvitationPage.xaml"));
        var appResourcesSource = ReadMobileBusinessFile(Path.Combine("Resources", "AppResources.cs"));
        var stringsSource = ReadMobileBusinessFile(Path.Combine("Resources", "Strings.resx"));
        var germanStringsSource = ReadMobileBusinessFile(Path.Combine("Resources", "Strings.de.resx"));

        invitationViewModelSource.Should().Contain("public string InvitationRoleDisplay => LocalizeInvitationRole(_preview?.Role);");
        invitationViewModelSource.Should().Contain("public string InvitationStatusDisplay => LocalizeInvitationStatus(_preview?.Status);");
        invitationViewModelSource.Should().Contain("OnPropertyChanged(nameof(InvitationRoleDisplay));");
        invitationViewModelSource.Should().Contain("OnPropertyChanged(nameof(InvitationStatusDisplay));");
        invitationViewModelSource.Should().Contain("\"Owner\" => AppResources.InvitationRoleOwner,");
        invitationViewModelSource.Should().Contain("\"Manager\" => AppResources.InvitationRoleManager,");
        invitationViewModelSource.Should().Contain("\"Staff\" => AppResources.InvitationRoleStaff,");
        invitationViewModelSource.Should().Contain("_ => AppResources.InvitationRoleUnknown");
        invitationViewModelSource.Should().Contain("\"Pending\" => AppResources.InvitationStatusPending,");
        invitationViewModelSource.Should().Contain("\"Accepted\" => AppResources.InvitationStatusAccepted,");
        invitationViewModelSource.Should().Contain("\"Expired\" => AppResources.InvitationStatusExpired,");
        invitationViewModelSource.Should().Contain("\"Revoked\" => AppResources.InvitationStatusRevoked,");
        invitationViewModelSource.Should().Contain("_ => AppResources.InvitationStatusUnknown");
        invitationViewModelSource.Should().Contain("string.Equals(InvitationStatus, \"Pending\", StringComparison.OrdinalIgnoreCase)");

        invitationViewSource.Should().Contain("Text=\"{Binding InvitationRoleDisplay}\"");
        invitationViewSource.Should().Contain("Text=\"{Binding InvitationStatusDisplay}\"");

        appResourcesSource.Should().Contain("public static string InvitationRoleOwner =>");
        appResourcesSource.Should().Contain("public static string InvitationRoleManager =>");
        appResourcesSource.Should().Contain("public static string InvitationRoleStaff =>");
        appResourcesSource.Should().Contain("public static string InvitationRoleUnknown =>");
        appResourcesSource.Should().Contain("public static string InvitationStatusPending =>");
        appResourcesSource.Should().Contain("public static string InvitationStatusAccepted =>");
        appResourcesSource.Should().Contain("public static string InvitationStatusExpired =>");
        appResourcesSource.Should().Contain("public static string InvitationStatusRevoked =>");
        appResourcesSource.Should().Contain("public static string InvitationStatusUnknown =>");

        stringsSource.Should().Contain("<data name=\"InvitationRoleOwner\"");
        stringsSource.Should().Contain("<data name=\"InvitationRoleManager\"");
        stringsSource.Should().Contain("<data name=\"InvitationRoleStaff\"");
        stringsSource.Should().Contain("<data name=\"InvitationRoleUnknown\"");
        stringsSource.Should().Contain("<data name=\"InvitationStatusPending\"");
        stringsSource.Should().Contain("<data name=\"InvitationStatusAccepted\"");
        stringsSource.Should().Contain("<data name=\"InvitationStatusExpired\"");
        stringsSource.Should().Contain("<data name=\"InvitationStatusUnknown\"");
        stringsSource.Should().Contain("<data name=\"InvitationStatusRevoked\"");

        germanStringsSource.Should().Contain("<data name=\"InvitationRoleOwner\"");
        germanStringsSource.Should().Contain("<data name=\"InvitationRoleManager\"");
        germanStringsSource.Should().Contain("<data name=\"InvitationRoleStaff\"");
        germanStringsSource.Should().Contain("<data name=\"InvitationStatusPending\"");
        germanStringsSource.Should().Contain("<data name=\"InvitationStatusAccepted\"");
        germanStringsSource.Should().Contain("<data name=\"InvitationStatusExpired\"");
        germanStringsSource.Should().Contain("<data name=\"InvitationStatusRevoked\"");
    }


    [Fact]
    public void BusinessesMetaAndProfileAddressesControllers_Should_KeepBoundaryContracts()
    {
        var businessesMetaSource = ReadWebApiFile(Path.Combine("Controllers", "Businesses", "BusinessesMetaController.cs"));
        var profileAddressesSource = ReadWebApiFile(Path.Combine("Controllers", "Profile", "ProfileAddressesController.cs"));

        businessesMetaSource.Should().Contain("[AllowAnonymous]");
        businessesMetaSource.Should().Contain("[Route(\"api/v1/public/businesses\")]");
        businessesMetaSource.Should().Contain("public async Task<IActionResult> GetCategoryKinds(");
        businessesMetaSource.Should().Contain("[HttpGet(\"category-kinds\")]");
        businessesMetaSource.Should().Contain("[HttpGet(\"/api/v1/businesses/category-kinds\")]");

        profileAddressesSource.Should().Contain("[Authorize]");
        profileAddressesSource.Should().Contain("[Route(\"api/v1/member/profile\")]");
        profileAddressesSource.Should().Contain("public async Task<IActionResult> GetAddressesAsync(");
        profileAddressesSource.Should().Contain("[HttpGet(\"addresses\")]");
        profileAddressesSource.Should().Contain("[HttpGet(\"/api/v1/profile/me/addresses\")]");
        profileAddressesSource.Should().Contain("public async Task<IActionResult> GetLinkedCustomerAsync(");
        profileAddressesSource.Should().Contain("[HttpGet(\"customer\")]");
        profileAddressesSource.Should().Contain("[HttpGet(\"/api/v1/profile/me/customer\")]");
        profileAddressesSource.Should().Contain("public async Task<IActionResult> GetLinkedCustomerContextAsync(");
        profileAddressesSource.Should().Contain("[HttpGet(\"customer/context\")]");
        profileAddressesSource.Should().Contain("[HttpGet(\"/api/v1/profile/me/customer/context\")]");
    }


    [Fact]
    public void BusinessLocationsAndSubscriptionInvoiceWorkspaces_Should_KeepFilteredOpsContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));
        var locationsViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Locations.cshtml"));
        var subscriptionInvoicesViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "SubscriptionInvoices.cshtml"));

        controllerSource.Should().Contain("Playbooks = BuildBusinessLocationPlaybooks(businessId)");
        controllerSource.Should().Contain("private List<BusinessLocationPlaybookVm> BuildBusinessLocationPlaybooks(Guid businessId)");
        controllerSource.Should().Contain("new SelectListItem(T(\"CommonAll\"), BusinessLocationQueueFilter.All.ToString()");
        controllerSource.Should().Contain("new SelectListItem(T(\"BusinessLocationsPrimaryLocationLabel\"), BusinessLocationQueueFilter.Primary.ToString()");
        controllerSource.Should().Contain("new SelectListItem(T(\"MissingAddress\"), BusinessLocationQueueFilter.MissingAddress.ToString()");
        controllerSource.Should().Contain("new SelectListItem(T(\"BusinessLocationsMissingCoordinatesLabel\"), BusinessLocationQueueFilter.MissingCoordinates.ToString()");
        controllerSource.Should().Contain("QueueLabel = T(\"BusinessLocationsPrimaryLocationLabel\")");
        controllerSource.Should().Contain("WhyItMatters = T(\"BusinessLocationsPlaybookPrimaryWhyItMatters\")");
        controllerSource.Should().Contain("OperatorAction = T(\"BusinessLocationsPlaybookPrimaryAction\")");
        controllerSource.Should().Contain("QueueLabel = T(\"MissingAddress\")");
        controllerSource.Should().Contain("WhyItMatters = T(\"BusinessLocationsPlaybookMissingAddressWhyItMatters\")");
        controllerSource.Should().Contain("OperatorAction = T(\"BusinessLocationsPlaybookMissingAddressAction\")");
        controllerSource.Should().Contain("QueueLabel = T(\"BusinessLocationsMissingCoordinatesLabel\")");
        controllerSource.Should().Contain("WhyItMatters = T(\"BusinessLocationsPlaybookMissingCoordinatesWhyItMatters\")");
        controllerSource.Should().Contain("OperatorAction = T(\"BusinessLocationsPlaybookMissingCoordinatesAction\")");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Locations\", \"Businesses\", new { businessId, filter = BusinessLocationQueueFilter.Primary }) ?? string.Empty");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Locations\", \"Businesses\", new { businessId, filter = BusinessLocationQueueFilter.MissingAddress }) ?? string.Empty");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Locations\", \"Businesses\", new { businessId, filter = BusinessLocationQueueFilter.MissingCoordinates }) ?? string.Empty");

        locationsViewSource.Should().Contain("BusinessLocationsPlaybooksTitle");
        locationsViewSource.Should().Contain("@foreach (var playbook in Model.Playbooks)");
        locationsViewSource.Should().Contain("hx-target=\"#business-locations-workspace-shell\"");
        locationsViewSource.Should().Contain(">@playbook.QueueLabel</a>");
        locationsViewSource.Should().Contain(">@playbook.WhyItMatters</a>");
        locationsViewSource.Should().Contain(">@playbook.OperatorAction</a>");
        locationsViewSource.Should().Contain("href=\"@playbook.QueueActionUrl\"");
        locationsViewSource.Should().Contain("BusinessLocationQueueFilter.Primary");
        locationsViewSource.Should().Contain("BusinessLocationQueueFilter.MissingAddress");
        locationsViewSource.Should().Contain("BusinessLocationQueueFilter.MissingCoordinates");
        locationsViewSource.Should().Contain("string InvitationWorkspaceLabel() => T.T(\"Invitations\")");
        locationsViewSource.Should().Contain("hx-push-url=\"true\">@InvitationWorkspaceLabel()</a>");
        locationsViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"Invitations\")</a>");
        locationsViewSource.Should().Contain("@T.T(\"BusinessLocationsEmptyState\")");
        locationsViewSource.Should().Contain("@CreateLocationUrl(Model.Business.Id, Model.Page, Model.PageSize, Model.Query, Model.Filter)");
        locationsViewSource.Should().Contain("@BusinessSetupUrl(Model.Business.Id)");
        locationsViewSource.Should().Contain("@BusinessMerchantReadinessUrl(Model.Business.Id)");
        locationsViewSource.Should().Contain("@EditLocationUrl(item.Id, Model.Page, Model.PageSize, Model.Query, Model.Filter)");
          locationsViewSource.Should().Contain("@T.T(\"CommonEdit\")");
          locationsViewSource.Should().Contain("@T.T(\"Setup\")");
          locationsViewSource.Should().Contain("@T.T(\"MerchantReadinessTitle\")");

        subscriptionInvoicesViewSource.Should().Contain("BusinessSubscriptionBillingPlaybooksTitle");
        subscriptionInvoicesViewSource.Should().Contain("BusinessSubscriptionInvoiceQueueFilter.Open");
        subscriptionInvoicesViewSource.Should().Contain("BusinessSubscriptionInvoiceQueueFilter.Paid");
        subscriptionInvoicesViewSource.Should().Contain("BusinessSubscriptionInvoiceQueueFilter.Draft");
        subscriptionInvoicesViewSource.Should().Contain("BusinessSubscriptionInvoiceQueueFilter.Uncollectible");
          subscriptionInvoicesViewSource.Should().Contain("BusinessSubscriptionInvoiceQueueFilter.Stripe");
          subscriptionInvoicesViewSource.Should().Contain("BusinessSubscriptionInvoiceQueueFilter.Overdue");
          subscriptionInvoicesViewSource.Should().Contain("BusinessSubscriptionInvoiceQueueFilter.PdfMissing");
          subscriptionInvoicesViewSource.Should().Contain("BusinessSubscriptionInvoiceQueueFilter.HostedLinkMissing");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceRootId() => \"business-subscription-invoices-workspace-shell\";");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceRootTarget() => $\"#{InvoiceRootId()}\";");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceRootSwap() => \"outerHTML\";");
        subscriptionInvoicesViewSource.Should().Contain("string InvoicePushUrlValue() => \"true\";");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceExternalTarget() => \"_blank\";");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceExternalRel() => \"noopener noreferrer\";");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceToolbarIconGlyphClass(string iconName) => $\"fa-solid {iconName}\";");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceAmountText(string currency, long totalMinor) => $\"{currency} {(totalMinor / 100M).ToString(\"0.00\")}\";");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceTimestampText(DateTime? timestampUtc) => timestampUtc?.ToLocalTime().ToString(CultureInfo.CurrentCulture) ?? \"-\";");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceIssuedTimelineText(DateTime? timestampUtc) => string.Format(T.T(\"CommonIssuedAt\"), InvoiceTimestampText(timestampUtc));");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceDueTimelineText(DateTime? timestampUtc) => string.Format(T.T(\"CommonDueAt\"), InvoiceTimestampText(timestampUtc));");
        subscriptionInvoicesViewSource.Should().Contain("string InvoicePaidTimelineText(DateTime? timestampUtc) => string.Format(T.T(\"CommonPaidAt\"), InvoiceTimestampText(timestampUtc));");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceSummaryTotalCountText() => InvoiceSummaryCountText(Model.Summary.TotalCount);");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceSummaryOpenCountText() => InvoiceSummaryCountText(Model.Summary.OpenCount);");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceSummaryPaidCountText() => InvoiceSummaryCountText(Model.Summary.PaidCount);");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceSummaryDraftCountText() => InvoiceSummaryCountText(Model.Summary.DraftCount);");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceSummaryUncollectibleCountText() => InvoiceSummaryCountText(Model.Summary.UncollectibleCount);");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceSummaryStripeCountText() => InvoiceSummaryCountText(Model.Summary.StripeCount);");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceSummaryOverdueCountText() => InvoiceSummaryCountText(Model.Summary.OverdueCount);");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceSummaryPdfMissingCountText() => InvoiceSummaryCountText(Model.Summary.PdfMissingCount);");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceRowStatusText(Darwin.WebAdmin.ViewModels.Businesses.BusinessSubscriptionInvoiceListItemVm item) => InvoiceStatusText(item.Status);");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceRowStatusBadgeClass(Darwin.WebAdmin.ViewModels.Businesses.BusinessSubscriptionInvoiceListItemVm item) => $\"{InvoiceBadgeClass()} {InvoiceBadgeToneClass(item.Status)}\";");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceRowAmountText(Darwin.WebAdmin.ViewModels.Businesses.BusinessSubscriptionInvoiceListItemVm item) => InvoiceAmountText(item.Currency, item.TotalMinor);");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceRowIssuedTimelineText(Darwin.WebAdmin.ViewModels.Businesses.BusinessSubscriptionInvoiceListItemVm item) => InvoiceIssuedTimelineText(item.IssuedAtUtc);");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceRowDueTimelineText(Darwin.WebAdmin.ViewModels.Businesses.BusinessSubscriptionInvoiceListItemVm item) => InvoiceDueTimelineText(item.DueAtUtc);");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceRowPaidTimelineText(Darwin.WebAdmin.ViewModels.Businesses.BusinessSubscriptionInvoiceListItemVm item) => InvoicePaidTimelineText(item.PaidAtUtc);");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceSubscriptionActionText() => T.T(\"BusinessSubscriptionShort\");");
        subscriptionInvoicesViewSource.Should().Contain("string InvoicePaymentsActionText() => T.T(\"CommonPayments\");");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceRefundsActionText() => T.T(\"RefundQueue\");");
        subscriptionInvoicesViewSource.Should().Contain("string InvoicePaymentSettingsActionText() => T.T(\"PaymentSettings\");");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceSupportQueueActionText() => T.T(\"BusinessSupportQueueTitle\")");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceMerchantReadinessActionText() => T.T(\"MerchantReadinessTitle\")");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceHostedActionText() => T.T(\"CommonHosted\");");
        subscriptionInvoicesViewSource.Should().Contain("string InvoicePdfActionText() => T.T(\"CommonPdf\");");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceRowPlanNameText(string? planName) => string.IsNullOrWhiteSpace(planName) ? \"-\" : planName;");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceRowProviderText(string? provider) => string.IsNullOrWhiteSpace(provider) ? \"-\" : provider;");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceRowPlanCodeText(string? planCode) => string.IsNullOrWhiteSpace(planCode) ? \"-\" : planCode;");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceRowProviderInvoiceIdText(string? providerInvoiceId) => string.IsNullOrWhiteSpace(providerInvoiceId) ? \"-\" : providerInvoiceId;");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceRowFailureReasonText(string? failureReason) => string.IsNullOrWhiteSpace(failureReason) ? \"-\" : failureReason;");
        subscriptionInvoicesViewSource.Should().Contain("bool InvoiceRowHasFailureReason(Darwin.WebAdmin.ViewModels.Businesses.BusinessSubscriptionInvoiceListItemVm item) => !string.IsNullOrWhiteSpace(item.FailureReason);");
        subscriptionInvoicesViewSource.Should().Contain("bool InvoiceRowHasStripeSignal(Darwin.WebAdmin.ViewModels.Businesses.BusinessSubscriptionInvoiceListItemVm item) => item.IsStripe;");
        subscriptionInvoicesViewSource.Should().Contain("bool InvoiceRowHasOverdueSignal(Darwin.WebAdmin.ViewModels.Businesses.BusinessSubscriptionInvoiceListItemVm item) => item.IsOverdue;");
        subscriptionInvoicesViewSource.Should().Contain("bool InvoiceRowHasPdfMissingSignal(Darwin.WebAdmin.ViewModels.Businesses.BusinessSubscriptionInvoiceListItemVm item) => !item.HasPdfUrl;");
        subscriptionInvoicesViewSource.Should().Contain("bool InvoiceRowHasHostedAction(Darwin.WebAdmin.ViewModels.Businesses.BusinessSubscriptionInvoiceListItemVm item) => item.HasHostedInvoiceUrl;");
        subscriptionInvoicesViewSource.Should().Contain("bool InvoiceRowHasPdfAction(Darwin.WebAdmin.ViewModels.Businesses.BusinessSubscriptionInvoiceListItemVm item) => item.HasPdfUrl;");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceRowHostedActionHref(string? hostedInvoiceUrl) => string.IsNullOrWhiteSpace(hostedInvoiceUrl) ? string.Empty : hostedInvoiceUrl;");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceRowPdfActionHref(string? pdfUrl) => string.IsNullOrWhiteSpace(pdfUrl) ? string.Empty : pdfUrl;");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceProviderColumnHeaderText() => T.T(\"CommonProvider\");");
        subscriptionInvoicesViewSource.Should().Contain("string InvoicePlanColumnHeaderText() => T.T(\"CommonPlan\");");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceStatusColumnHeaderText() => T.T(\"CommonStatus\");");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceAmountColumnHeaderText() => T.T(\"CommonAmount\");");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceTimelineColumnHeaderText() => T.T(\"CommonTimeline\");");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceActionsColumnHeaderText() => T.T(\"CommonActions\");");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceFilterPlaceholderText() => T.T(\"BusinessSubscriptionInvoicesSearchPlaceholder\");");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceFilterSubmitText() => T.T(\"CommonFilter\");");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceFilterResetText() => T.T(\"CommonReset\");");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceQueueFilterText(BusinessSubscriptionInvoiceQueueFilter filter) => filter switch");
        subscriptionInvoicesViewSource.Should().Contain("string InvoicePlaybookActionText(BusinessSubscriptionInvoiceQueueFilter filter) => filter switch");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceQueueButtonClass(BusinessSubscriptionInvoiceQueueFilter filter) => filter switch");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceQueueChipClass(BusinessSubscriptionInvoiceQueueFilter activeFilter, BusinessSubscriptionInvoiceQueueFilter filter) => filter switch");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceSignalFilterText(BusinessSubscriptionInvoiceQueueFilter filter) => filter switch");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceToolbarSecondaryButtonClass() => \"btn btn-outline-secondary\";");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceToolbarDangerButtonClass() => \"btn btn-outline-danger\";");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceToolbarActionsRailClass() => \"d-flex gap-2 flex-wrap\";");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceQueueChipBaseClass() => \"btn btn-sm\";");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceRowButtonClass() => \"btn btn-outline-secondary\";");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceEmptyStateButtonClass() => \"btn btn-sm btn-outline-secondary\";");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceFilterSubmitButtonClass() => \"btn btn-outline-secondary\";");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceRowActionsClass() => \"btn-group btn-group-sm\";");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceTableWrapperClass() => \"table-responsive\";");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceTableClass() => \"table table-sm table-striped align-middle\";");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceTableHeaderClass() => \"table-light\";");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceEmptyStateButtonsRailClass() => \"d-flex gap-2 flex-wrap justify-content-center mt-3\";");
        subscriptionInvoicesViewSource.Should().Contain("string InvoicePlaybookButtonsRailClass() => \"d-flex gap-2 flex-wrap\";");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceQueueFilterRailClass() => \"d-flex gap-2 flex-wrap mb-3\";");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceFilterFieldsRailClass() => \"d-flex gap-2\";");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceFilterToolbarClass() => \"d-flex justify-content-between align-items-center mb-3\";");
        subscriptionInvoicesViewSource.Should().Contain("id=\"@InvoiceRootId()\"");
        subscriptionInvoicesViewSource.Should().Contain("hx-target=\"@InvoiceRootTarget()\"");
        subscriptionInvoicesViewSource.Should().Contain("hx-swap=\"@InvoiceRootSwap()\"");
        subscriptionInvoicesViewSource.Should().Contain("hx-push-url=\"@InvoicePushUrlValue()\"");
        subscriptionInvoicesViewSource.Should().Contain("string InvoicePlaybookSectionTitleText() => T.T(\"BusinessSubscriptionBillingPlaybooksTitle\");");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceEmptyStateMessageText() => T.T(\"BusinessSubscriptionInvoicesEmptyState\");");
        subscriptionInvoicesViewSource.Should().Contain("@InvoiceEmptyStateMessageText()");
        subscriptionInvoicesViewSource.Should().Contain("Guid InvoicePageBusinessIdValue() => Model.Business.Id;");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceSubscriptionActionHref() => Url.Action(\"Subscription\", \"Businesses\", new { businessId = InvoicePageBusinessIdValue() }) ?? string.Empty;");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceQueueActionHref() => Url.Action(\"SubscriptionInvoices\", \"Businesses\", new { businessId = InvoicePageBusinessIdValue() }) ?? string.Empty;");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceQueueActionHrefFor(BusinessSubscriptionInvoiceQueueFilter filter) => Url.Action(\"SubscriptionInvoices\", \"Businesses\", new { businessId = InvoicePageBusinessIdValue(), filter }) ?? string.Empty;");
        subscriptionInvoicesViewSource.Should().Contain("hx-get=\"@InvoiceSubscriptionActionHref()\"");
        subscriptionInvoicesViewSource.Should().Contain("hx-get=\"@InvoiceQueueActionHref()\"");
        subscriptionInvoicesViewSource.Should().Contain("hx-get=\"@InvoiceQueueActionHrefFor(BusinessSubscriptionInvoiceQueueFilter.Open)\"");
        subscriptionInvoicesViewSource.Should().Contain("string InvoicePaymentsActionHref() => Url.Action(\"Payments\", \"Billing\", new { businessId = InvoicePageBusinessIdValue() }) ?? string.Empty;");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceRowPaymentsActionHref(string? providerInvoiceId) => Url.Action(\"Payments\", \"Billing\", new { businessId = InvoicePageBusinessIdValue(), q = providerInvoiceId }) ?? string.Empty;");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceRowPaymentsHref(Darwin.WebAdmin.ViewModels.Businesses.BusinessSubscriptionInvoiceListItemVm item) => InvoiceRowPaymentsActionHref(item.ProviderInvoiceId);");
        subscriptionInvoicesViewSource.Should().Contain("hx-get=\"@InvoicePaymentsActionHref()\"");
        subscriptionInvoicesViewSource.Should().Contain("hx-get=\"@InvoiceRowPaymentsHref(item)\"");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceSupportQueueActionHref() => Url.Action(\"SupportQueue\", \"Businesses\", new { businessId = Model.Business.Id }) ?? string.Empty;");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceMerchantReadinessActionHref() => Url.Action(\"MerchantReadiness\", \"Businesses\", new { businessId = Model.Business.Id }) ?? string.Empty;");
        subscriptionInvoicesViewSource.Should().Contain("hx-get=\"@InvoiceSupportQueueActionHref()\"");
        subscriptionInvoicesViewSource.Should().Contain("hx-get=\"@InvoiceMerchantReadinessActionHref()\"");
        subscriptionInvoicesViewSource.Should().Contain("@InvoiceSubscriptionActionText()");
        subscriptionInvoicesViewSource.Should().Contain("System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> InvoiceFilterQueueItems() => Model.FilterItems;");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceFilterQueryValue() => Model.Query ?? string.Empty;");
        subscriptionInvoicesViewSource.Should().Contain("BusinessSubscriptionInvoiceQueueFilter InvoiceSelectedQueueFilter() => Model.Filter;");
        subscriptionInvoicesViewSource.Should().Contain("bool InvoiceHasRows() => Model.Items.Count > 0;");
        subscriptionInvoicesViewSource.Should().Contain("System.Collections.Generic.IReadOnlyList<Darwin.WebAdmin.ViewModels.Businesses.BusinessSubscriptionInvoiceListItemVm> InvoiceRowItems() => Model.Items;");
        subscriptionInvoicesViewSource.Should().Contain("@InvoicePaymentsActionText()");
        subscriptionInvoicesViewSource.Should().Contain("@InvoiceSupportQueueActionText()");
        subscriptionInvoicesViewSource.Should().Contain("@InvoiceMerchantReadinessActionText()");
        subscriptionInvoicesViewSource.Should().Contain("@InvoiceSummaryTotalCountText()");
        subscriptionInvoicesViewSource.Should().Contain("@InvoiceSummaryOpenCountText()");
        subscriptionInvoicesViewSource.Should().Contain("@InvoiceSummaryPaidCountText()");
        subscriptionInvoicesViewSource.Should().Contain("@InvoiceSummaryDraftCountText()");
        subscriptionInvoicesViewSource.Should().Contain("@InvoiceSummaryUncollectibleCountText()");
        subscriptionInvoicesViewSource.Should().Contain("@InvoiceSummaryStripeCountText()");
        subscriptionInvoicesViewSource.Should().Contain("@InvoiceSummaryOverdueCountText()");
        subscriptionInvoicesViewSource.Should().Contain("@InvoiceSummaryPdfMissingCountText()");
        subscriptionInvoicesViewSource.Should().Contain("@InvoiceRowStatusText(item)");
        subscriptionInvoicesViewSource.Should().Contain("@InvoiceRowStatusBadgeClass(item)");
        subscriptionInvoicesViewSource.Should().Contain("@InvoiceRowAmountText(item)");
        subscriptionInvoicesViewSource.Should().Contain("@InvoiceRowIssuedTimelineText(item)");
        subscriptionInvoicesViewSource.Should().Contain("@InvoiceRowDueTimelineText(item)");
        subscriptionInvoicesViewSource.Should().Contain("@InvoiceRowPaidTimelineText(item)");
        subscriptionInvoicesViewSource.Should().Contain("@InvoiceFilterQueueItems()");
        subscriptionInvoicesViewSource.Should().Contain("@InvoiceFilterQueryValue()");
        subscriptionInvoicesViewSource.Should().Contain("@InvoiceQueueChipClass(InvoiceSelectedQueueFilter(), BusinessSubscriptionInvoiceQueueFilter.All)");
        subscriptionInvoicesViewSource.Should().Contain("@if (!InvoiceHasRows())");
        subscriptionInvoicesViewSource.Should().Contain("foreach (var item in InvoiceRowItems())");
      }


    [Fact]
    public void BusinessSubscriptionWorkspace_Should_KeepPlaybookOpsContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));
        var subscriptionViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Subscription.cshtml"));

        controllerSource.Should().Contain("Playbooks = BuildSubscriptionPlaybooks(business.Id, subscription, managementWebsiteConfigured)");
        controllerSource.Should().Contain("private List<BusinessSubscriptionPlaybookVm> BuildSubscriptionPlaybooks(Guid businessId, BusinessSubscriptionSnapshotVm subscription, bool managementWebsiteConfigured)");
        controllerSource.Should().Contain("new SelectListItem(T(\"BusinessSubscriptionAllInvoicesLabel\"), BusinessSubscriptionInvoiceQueueFilter.All.ToString()");
        controllerSource.Should().Contain("new SelectListItem(T(\"CommonOpen\"), BusinessSubscriptionInvoiceQueueFilter.Open.ToString()");
        controllerSource.Should().Contain("new SelectListItem(T(\"CommonPaid\"), BusinessSubscriptionInvoiceQueueFilter.Paid.ToString()");
        controllerSource.Should().Contain("new SelectListItem(T(\"CommonDraft\"), BusinessSubscriptionInvoiceQueueFilter.Draft.ToString()");
        controllerSource.Should().Contain("new SelectListItem(T(\"CommonUncollectible\"), BusinessSubscriptionInvoiceQueueFilter.Uncollectible.ToString()");
        controllerSource.Should().Contain("new SelectListItem(T(\"BusinessSubscriptionHostedLinkMissing\"), BusinessSubscriptionInvoiceQueueFilter.HostedLinkMissing.ToString()");
        controllerSource.Should().Contain("new SelectListItem(T(\"CommonStripe\"), BusinessSubscriptionInvoiceQueueFilter.Stripe.ToString()");
        controllerSource.Should().Contain("new SelectListItem(T(\"CommonOverdue\"), BusinessSubscriptionInvoiceQueueFilter.Overdue.ToString()");
        controllerSource.Should().Contain("new SelectListItem(T(\"BusinessSubscriptionReviewPdfMissing\"), BusinessSubscriptionInvoiceQueueFilter.PdfMissing.ToString()");
        controllerSource.Should().Contain("QueueLabel = T(\"BusinessManagementWebsite\")");
        controllerSource.Should().Contain("WhyItMatters = T(\"BusinessSubscriptionPlaybookManagementWebsiteWhyItMatters\")");
        controllerSource.Should().Contain("? T(\"BusinessSubscriptionPlaybookManagementWebsiteActionConfigured\")");
        controllerSource.Should().Contain(": T(\"BusinessSubscriptionPlaybookManagementWebsiteActionMissing\")");
        controllerSource.Should().Contain("QueueActionLabel = T(\"CommonSetup\")");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Edit\", \"SiteSettings\", new { fragment = \"site-settings-business-app\" }) ?? string.Empty");
        controllerSource.Should().Contain("QueueLabel = T(\"BusinessSubscriptionCancellationPolicy\")");
        controllerSource.Should().Contain("WhyItMatters = T(\"BusinessSubscriptionPlaybookCancellationWhyItMatters\")");
        controllerSource.Should().Contain("? T(\"BusinessSubscriptionPlaybookCancellationActionActive\")");
        controllerSource.Should().Contain(": T(\"BusinessSubscriptionPlaybookCancellationActionInactive\")");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"SubscriptionInvoices\", \"Businesses\", new { businessId, filter = BusinessSubscriptionInvoiceQueueFilter.Open }) ?? string.Empty");
        controllerSource.Should().Contain("FollowUpLabel = T(\"CommonPayments\")");
        controllerSource.Should().Contain("FollowUpUrl = Url.Action(\"Payments\", \"Billing\", new { businessId }) ?? string.Empty");
        controllerSource.Should().Contain("QueueLabel = T(\"BusinessSubscriptionNoActivePlan\")");
        controllerSource.Should().Contain("WhyItMatters = T(\"BusinessSubscriptionPlaybookNoActivePlanWhyItMatters\")");
        controllerSource.Should().Contain("OperatorAction = T(\"BusinessSubscriptionPlaybookNoActivePlanAction\")");
        controllerSource.Should().Contain("FollowUpLabel = T(\"BusinessSupportQueueTitle\")");
        controllerSource.Should().Contain("FollowUpUrl = Url.Action(\"SupportQueue\", \"Businesses\", new { businessId }) ?? string.Empty");
        controllerSource.Should().Contain("Status = T(\"Unavailable\")");
        controllerSource.Should().Contain("CheckoutReadinessLabel = validation.Succeeded ? T(\"BusinessSubscriptionCheckoutReady\") : (validation.Error ?? T(\"NotReady\"))");
        controllerSource.Should().Contain("? T(\"BusinessSubscriptionManageCurrentPlan\")");
        controllerSource.Should().Contain("subscription.HasSubscription ? T(\"BusinessSubscriptionUpgradeToPlan\") : T(\"BusinessSubscriptionStartWithPlan\")");
        controllerSource.Should().Contain("? T(\"BusinessSubscriptionCurrentPlanBadge\")");
        controllerSource.Should().Contain("? T(\"BusinessSubscriptionOpenBillingWebsite\")");
        controllerSource.Should().Contain("? T(\"BusinessSubscriptionResolvePrerequisites\")");
        controllerSource.Should().Contain(": T(\"BusinessSubscriptionConfigureWebsite\")");

        subscriptionViewSource.Should().Contain("BusinessSubscriptionBillingPlaybooksTitle");
        subscriptionViewSource.Should().Contain("@using System.Globalization");
        subscriptionViewSource.Should().Contain("var subscriptionCulture = CultureInfo.CurrentCulture;");
        subscriptionViewSource.Should().Contain("string SubscriptionDateTimeDisplayText(DateTime? value) => value?.ToString(\"g\", subscriptionCulture) ?? \"-\";");
        subscriptionViewSource.Should().Contain("string SubscriptionMoneyDisplayText(string currency, long amountMinor) => string.Format(subscriptionCulture, \"{0} {1:N2}\", currency, amountMinor / 100M);");
        subscriptionViewSource.Should().Contain("string SubscriptionMetricCardCountText(int value) => value.ToString(\"N0\", subscriptionCulture);");
        subscriptionViewSource.Should().Contain("@foreach (var playbook in Model.Playbooks)");
        subscriptionViewSource.Should().Contain("playbook.QueueActionUrl");
        subscriptionViewSource.Should().Contain("playbook.FollowUpUrl");
subscriptionViewSource.Should().Contain("string SubscriptionWorkspaceFrameId() => \"business-subscription-workspace-shell\";");
subscriptionViewSource.Should().Contain("string SubscriptionWorkspaceFrameTarget() => $\"#{SubscriptionWorkspaceFrameId()}\";");
subscriptionViewSource.Should().Contain("string SubscriptionWorkspaceFramePushUrlValue() => \"true\";");
subscriptionViewSource.Should().Contain("string SubscriptionWorkspaceFrameSwapValue() => \"outerHTML\";");
        subscriptionViewSource.Should().Contain("string SubscriptionEditActionHref() => Url.Action(\"Edit\", \"Businesses\", new { id = Model.Business.Id }) ?? string.Empty;");
        subscriptionViewSource.Should().Contain("string SubscriptionSetupActionHref() => Url.Action(\"Setup\", \"Businesses\", new { id = Model.Business.Id }) ?? string.Empty;");
        subscriptionViewSource.Should().Contain("string SubscriptionConfigureWebsiteActionFragment() => \"site-settings-business-app\";");
        subscriptionViewSource.Should().Contain("string SubscriptionConfigureWebsiteActionHref() => Url.Action(\"Edit\", \"SiteSettings\", new { fragment = SubscriptionConfigureWebsiteActionFragment() }) ?? string.Empty;");
        subscriptionViewSource.Should().Contain("string SubscriptionRenewalTogglePostActionHref() => Url.Action(\"SetSubscriptionCancelAtPeriodEnd\", \"Businesses\") ?? string.Empty;");
        subscriptionViewSource.Should().Contain("string SubscriptionInvoiceQueueActionHref() => Url.Action(\"SubscriptionInvoices\", \"Businesses\", new { businessId = Model.Business.Id }) ?? string.Empty;");
        subscriptionViewSource.Should().Contain("string SubscriptionInvoiceQueueFilterActionHref(Darwin.Application.Billing.BusinessSubscriptionInvoiceQueueFilter filter) => Url.Action(\"SubscriptionInvoices\", \"Businesses\", new { businessId = Model.Business.Id, filter }) ?? string.Empty;");
        subscriptionViewSource.Should().Contain("string SubscriptionPaymentsActionHref() => Url.Action(\"Payments\", \"Billing\", new { businessId = Model.Business.Id }) ?? string.Empty;");
        subscriptionViewSource.Should().Contain("string SubscriptionPaymentsSearchActionHref(string? providerInvoiceId) => Url.Action(\"Payments\", \"Billing\", new { businessId = Model.Business.Id, q = providerInvoiceId }) ?? string.Empty;");
        subscriptionViewSource.Should().Contain("string SubscriptionMerchantReadinessActionHref() => Url.Action(\"MerchantReadiness\", \"Businesses\", new { businessId = Model.Business.Id }) ?? string.Empty;");
        subscriptionViewSource.Should().Contain("string SubscriptionSupportQueueActionHref() => Url.Action(\"SupportQueue\", \"Businesses\", new { businessId = Model.Business.Id }) ?? string.Empty;");
subscriptionViewSource.Should().Contain("hx-target=\"@SubscriptionWorkspaceFrameTarget()\"");
subscriptionViewSource.Should().Contain("hx-push-url=\"@SubscriptionWorkspaceFramePushUrlValue()\"");
subscriptionViewSource.Should().Contain("hx-swap=\"@SubscriptionWorkspaceFrameSwapValue()\"");
subscriptionViewSource.Should().Contain("@SubscriptionBillingPlaybooksTableRowQueueActionHref(playbook)");
subscriptionViewSource.Should().Contain("@SubscriptionBillingPlaybooksTableRowFollowUpActionHref(playbook)");
subscriptionViewSource.Should().Contain("@SubscriptionBillingPlaybooksTableRowWhyDisplayText(playbook)");
subscriptionViewSource.Should().Contain("@SubscriptionBillingPlaybooksTableRowQueueDisplayText(playbook)");
subscriptionViewSource.Should().Contain("@SubscriptionBillingPlaybooksTableRowOperatorDisplayText(playbook)");
subscriptionViewSource.Should().Contain("@SubscriptionBillingPlaybooksTableRowQueueActionText(playbook)");
subscriptionViewSource.Should().Contain("@SubscriptionBillingPlaybooksTableRowFollowUpActionText(playbook)");
        subscriptionViewSource.Should().Contain("@SubscriptionActiveSubscriptionEmptyStateText()");
        subscriptionViewSource.Should().Contain("hx-get=\"@SubscriptionSetupActionHref()\"");
        subscriptionViewSource.Should().Contain("hx-get=\"@SubscriptionConfigureWebsiteActionHref()\"");
        subscriptionViewSource.Should().Contain("hx-get=\"@SubscriptionInvoiceQueueActionHref()\"");
        subscriptionViewSource.Should().Contain("hx-get=\"@SubscriptionInvoiceQueueFilterActionHref(Darwin.Application.Billing.BusinessSubscriptionInvoiceQueueFilter.Open)\"");
        subscriptionViewSource.Should().Contain("hx-get=\"@SubscriptionPaymentsActionHref()\"");
        subscriptionViewSource.Should().Contain("hx-get=\"@SubscriptionPaymentsSearchActionHref(invoice.ProviderInvoiceId)\"");
        subscriptionViewSource.Should().Contain("hx-get=\"@SubscriptionMerchantReadinessActionHref()\"");
        subscriptionViewSource.Should().Contain("hx-get=\"@SubscriptionSupportQueueActionHref()\"");
        subscriptionViewSource.Should().Contain("hx-post=\"@SubscriptionRenewalTogglePostActionHref()\"");
        subscriptionViewSource.Should().Contain("@if (SubscriptionHasActiveSubscription())");
        subscriptionViewSource.Should().Contain("@if (SubscriptionCanManageSubscription())");
        subscriptionViewSource.Should().Contain("@if (!SubscriptionHasPlanCatalogRows())");
        subscriptionViewSource.Should().Contain("@if (!SubscriptionHasRecentInvoiceListRows())");
        subscriptionViewSource.Should().Contain("@if (SubscriptionBillingPlaybooksTableRowCanOpenQueueAction(playbook.QueueActionUrl))");
        subscriptionViewSource.Should().Contain("@if (SubscriptionBillingPlaybooksTableRowCanOpenFollowUpAction(playbook.FollowUpUrl))");
subscriptionViewSource.Should().Contain("@SubscriptionBillingPlaybooksTableRowQueueActionHref(playbook)");
subscriptionViewSource.Should().Contain("@SubscriptionBillingPlaybooksTableRowFollowUpActionHref(playbook)");
subscriptionViewSource.Should().Contain("@SubscriptionRecentInvoicesTableEmptyStateText()");
subscriptionViewSource.Should().Contain("@SubscriptionInvoiceQueueActionText()");
        subscriptionViewSource.Should().Contain("hx-get=\"@SubscriptionPaymentsActionHref()\"");
subscriptionViewSource.Should().Contain("@SubscriptionAvailablePlansTableEmptyStateText()");
        subscriptionViewSource.Should().Contain("@SubscriptionResolvePrerequisitesHintText()");
subscriptionViewSource.Should().Contain("@SubscriptionSupportQueueActionText()");

        var mobileBusinessSubscriptionVmSource = ReadMobileBusinessFile(Path.Combine("ViewModels", "SubscriptionViewModel.cs"));
        mobileBusinessSubscriptionVmSource.Should().Contain("ResolveSubscriptionStatusDisplayName(status.Status)");
        mobileBusinessSubscriptionVmSource.Should().Contain("\"Trialing\" => AppResources.SubscriptionStatusTrialing");
        mobileBusinessSubscriptionVmSource.Should().Contain("\"Active\" => AppResources.SubscriptionStatusActive");
        mobileBusinessSubscriptionVmSource.Should().Contain("\"PastDue\" => AppResources.SubscriptionStatusPastDue");
        mobileBusinessSubscriptionVmSource.Should().Contain("\"Canceled\" => AppResources.SubscriptionStatusCanceled");
        mobileBusinessSubscriptionVmSource.Should().Contain("\"Unpaid\" => AppResources.SubscriptionStatusUnpaid");
        mobileBusinessSubscriptionVmSource.Should().Contain("\"Incomplete\" => AppResources.SubscriptionStatusIncomplete");
        mobileBusinessSubscriptionVmSource.Should().Contain("\"IncompleteExpired\" => AppResources.SubscriptionStatusIncompleteExpired");
        mobileBusinessSubscriptionVmSource.Should().Contain("\"Paused\" => AppResources.SubscriptionStatusPaused");
    }


    [Fact]
    public void UserAddressFragments_Should_KeepSectionAndModalContractsWired()
    {
        var sectionSource = ReadWebAdminFile(Path.Combine("Views", "Users", "_AddressesSection.cshtml"));
        var modalSource = ReadWebAdminFile(Path.Combine("Views", "Users", "_AddressEditModal.cshtml"));

        sectionSource.Should().Contain("var items = Model?.Items ?? new List<Darwin.WebAdmin.ViewModels.Identity.UserAddressListItemVm>()");
        sectionSource.Should().Contain("var userId = Model?.UserId ?? Guid.Empty;");
        sectionSource.Should().Contain("id=\"addresses-af\"");
        sectionSource.Should().Contain("@T.T(\"UserAddressesTitle\")");
        sectionSource.Should().Contain("data-bs-target=\"#addressEditModal\"");
        sectionSource.Should().Contain("data-action=\"@Url.Action(\"CreateAddress\", \"Users\")\"");
        sectionSource.Should().Contain("@T.T(\"AddAddress\")");
        sectionSource.Should().Contain("@T.T(\"BillingBadgeShort\")");
        sectionSource.Should().Contain("@T.T(\"ShippingBadgeShort\")");
        sectionSource.Should().Contain("hx-post=\"@Url.Action(\"SetDefaultAddress\", \"Users\")\"");
        sectionSource.Should().Contain("hx-vals='{\"id\":\"@a.Id\",\"userId\":\"@userId\",\"kind\":\"Billing\"}'");
        sectionSource.Should().Contain("hx-vals='{\"id\":\"@a.Id\",\"userId\":\"@userId\",\"kind\":\"Shipping\"}'");
        sectionSource.Should().Contain("data-refresh-alerts");
        sectionSource.Should().Contain("data-action=\"@Url.Action(\"EditAddress\", \"Users\")\"");
        sectionSource.Should().Contain("data-rowversion=\"@(a.RowVersion is null ? \"\" : Convert.ToBase64String(a.RowVersion))\"");
        sectionSource.Should().Contain("data-action=\"@Url.Action(\"DeleteAddress\", \"Users\")\"");
        sectionSource.Should().Contain("data-hx-target=\"#addresses-section\"");
        sectionSource.Should().Contain("@T.T(\"NoAddresses\")");

        modalSource.Should().Contain("id=\"addressEditModal\"");
        modalSource.Should().Contain("id=\"addressEditForm\"");
        modalSource.Should().Contain("hx-target=\"#addresses-section\"");
        modalSource.Should().Contain("data-refresh-alerts");
        modalSource.Should().Contain("data-hide-address-modal");
        modalSource.Should().Contain("id=\"addrId\"");
        modalSource.Should().Contain("id=\"addrRowVersion\"");
        modalSource.Should().Contain("id=\"addrUserId\" name=\"UserId\"");
        modalSource.Should().Contain("id=\"addrFullName\" name=\"FullName\"");
        modalSource.Should().Contain("id=\"addrCompany\" name=\"Company\"");
        modalSource.Should().Contain("id=\"addrStreet1\" name=\"Street1\"");
        modalSource.Should().Contain("id=\"addrStreet2\" name=\"Street2\"");
        modalSource.Should().Contain("id=\"addrPostalCode\" name=\"PostalCode\"");
        modalSource.Should().Contain("id=\"addrCity\" name=\"City\"");
        modalSource.Should().Contain("id=\"addrState\" name=\"State\"");
        modalSource.Should().Contain("id=\"addrCountryCode\" name=\"CountryCode\"");
        modalSource.Should().Contain("maxlength=\"2\"");
        modalSource.Should().Contain("id=\"addrPhone\" name=\"PhoneE164\"");
        modalSource.Should().Contain("name=\"IsDefaultBilling\" value=\"false\"");
        modalSource.Should().Contain("id=\"addrDefaultBilling\"");
        modalSource.Should().Contain("name=\"IsDefaultShipping\" value=\"false\"");
        modalSource.Should().Contain("id=\"addrDefaultShipping\"");
        modalSource.Should().Contain("@T.T(\"AddressFullNameHelp\")");
        modalSource.Should().Contain("@T.T(\"AddressCountryHelp\")");
        modalSource.Should().Contain("@T.T(\"DefaultBilling\")");
        modalSource.Should().Contain("@T.T(\"DefaultShipping\")");
    }


    [Fact]
    public void UserProfileFieldsPartial_Should_KeepValidationAndSettingsBackedPreferenceContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Users", "_UserProfileFields.cshtml"));

        source.Should().Contain("@model Darwin.WebAdmin.ViewModels.Identity.UserEditorVm");
        source.Should().Contain("<label asp-for=\"FirstName\" class=\"form-label\"></label>");
        source.Should().Contain("<input asp-for=\"FirstName\" class=\"form-control\" />");
        source.Should().Contain("<span asp-validation-for=\"FirstName\" class=\"text-danger\"></span>");
        source.Should().Contain("<label asp-for=\"LastName\" class=\"form-label\"></label>");
        source.Should().Contain("<input asp-for=\"LastName\" class=\"form-control\" />");
        source.Should().Contain("<span asp-validation-for=\"LastName\" class=\"text-danger\"></span>");
        source.Should().Contain("<field-help for=\"Locale\" text=\"@T.T(\"UserLocaleHelp\")\"></field-help>");
        source.Should().Contain("<setting-select asp-for=\"Locale\" setting=\"SupportedCulturesCsv\"></setting-select>");
        source.Should().Contain("<span asp-validation-for=\"Locale\" class=\"text-danger\"></span>");
        source.Should().Contain("<field-help for=\"Currency\" text=\"@T.T(\"UserCurrencyHelp\")\"></field-help>");
        source.Should().Contain("<setting-select asp-for=\"Currency\" setting=\"SupportedCurrenciesCsv\"></setting-select>");
        source.Should().Contain("<span asp-validation-for=\"Currency\" class=\"text-danger\"></span>");
        source.Should().Contain("<field-help for=\"Timezone\" text=\"@T.T(\"UserTimezoneHelp\")\"></field-help>");
        source.Should().Contain("<setting-select asp-for=\"Timezone\" setting=\"SupportedTimezonesCsv\"></setting-select>");
        source.Should().Contain("<span asp-validation-for=\"Timezone\" class=\"text-danger\"></span>");
        source.Should().Contain("<label asp-for=\"PhoneE164\" class=\"form-label\"></label>");
        source.Should().Contain("<input asp-for=\"PhoneE164\" class=\"form-control\" />");
        source.Should().Contain("<span asp-validation-for=\"PhoneE164\" class=\"text-danger\"></span>");
    }


    [Fact]
    public void AccountViews_Should_KeepLoginTwoFactorAndRegisterContractsWired()
    {
        var loginSource = ReadWebAdminFile(Path.Combine("Views", "Account", "Login.cshtml"));
        var twoFactorSource = ReadWebAdminFile(Path.Combine("Views", "Account", "LoginTwoFactor.cshtml"));
        var registerSource = ReadWebAdminFile(Path.Combine("Views", "Account", "Register.cshtml"));

        loginSource.Should().Contain("ViewData[\"Title\"] = T.T(\"Login\")");
        loginSource.Should().Contain("var returnUrl = (string?)ViewData[\"ReturnUrl\"] ?? \"\"");
        loginSource.Should().Contain("asp-action=\"LoginPost\" asp-controller=\"Account\"");
        loginSource.Should().Contain("@Html.AntiForgeryToken()");
        loginSource.Should().Contain("name=\"returnUrl\" value=\"@returnUrl\"");
        loginSource.Should().Contain("id=\"email\" name=\"email\" type=\"email\"");
        loginSource.Should().Contain("id=\"password\" name=\"password\" type=\"password\"");
        loginSource.Should().Contain("id=\"rememberMe\" name=\"rememberMe\" type=\"checkbox\"");
        loginSource.Should().Contain("@T.T(\"RememberMe\")");
        loginSource.Should().Contain("id=\"passkey-form\" method=\"post\" action=\"@Url.Action(\"WebAuthnFinishLogin\", \"Account\")\"");
        loginSource.Should().Contain("name=\"challengeTokenId\" id=\"challengeTokenId\"");
        loginSource.Should().Contain("name=\"clientResponseJson\" id=\"clientResponseJson\"");
        loginSource.Should().Contain("name=\"userId\" id=\"userId\"");
        loginSource.Should().Contain("name=\"rememberMe\" value=\"true\"");
        loginSource.Should().Contain("id=\"passkey-btn\"");
        loginSource.Should().Contain("@T.T(\"LoginWithPasskey\")");
        loginSource.Should().Contain(@"data-passkey-preparing-label=""@T.T(""PasskeyPreparing"")""");
        loginSource.Should().NotContain("alert('@T.T(\"PasskeyPreparing\")');");
        loginSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");

        twoFactorSource.Should().Contain("ViewData[\"Title\"] = T.T(\"LoginTwoFactorTitle\")");
        twoFactorSource.Should().Contain("var returnUrl = (string?)ViewData[\"ReturnUrl\"] ?? \"\"");
        twoFactorSource.Should().Contain("var rememberMe = (bool)(ViewData[\"RememberMe\"] ?? false);");
        twoFactorSource.Should().Contain("var userId = (string?)ViewData[\"TwoFaUserId\"] ?? \"\"");
        twoFactorSource.Should().Contain("@T.T(\"LoginTwoFactorHeading\")");
        twoFactorSource.Should().Contain("asp-action=\"LoginTwoFactorPost\" asp-controller=\"Account\"");
        twoFactorSource.Should().Contain("@Html.AntiForgeryToken()");
        twoFactorSource.Should().Contain("name=\"userId\" value=\"@userId\"");
        twoFactorSource.Should().Contain("name=\"rememberMe\" value=\"@(rememberMe ? \"true\" : \"false\")\"");
        twoFactorSource.Should().Contain("name=\"returnUrl\" value=\"@returnUrl\"");
        twoFactorSource.Should().Contain("id=\"code\" name=\"code\" type=\"number\"");
        twoFactorSource.Should().Contain("inputmode=\"numeric\"");
        twoFactorSource.Should().Contain("@T.T(\"LoginTwoFactorConfirmAction\")");
        twoFactorSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");

        registerSource.Should().Contain("ViewData[\"Title\"] = T.T(\"Register\")");
        registerSource.Should().Contain("var defaultCurrency = (string?)ViewData[\"DefaultCurrency\"] ?? Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCurrencyDefault;");
        registerSource.Should().Contain("var defaultLocale = (string?)ViewData[\"DefaultLocale\"] ?? Darwin.WebAdmin.Localization.AdminCultureCatalog.DefaultCulture;");
        registerSource.Should().Contain("var defaultTimeZone = (string?)ViewData[\"DefaultTimeZone\"] ?? Darwin.Application.Settings.DTOs.SiteSettingDto.TimeZoneDefault;");
        registerSource.Should().Contain("var supportedCulturesCsv = (string?)ViewData[\"SupportedCulturesCsv\"] ?? Darwin.WebAdmin.Localization.AdminCultureCatalog.SupportedCulturesCsvDefault;");
        registerSource.Should().Contain("asp-action=\"RegisterPost\" method=\"post\"");
        registerSource.Should().Contain("@Html.AntiForgeryToken()");
        registerSource.Should().Contain("id=\"email\" name=\"email\" type=\"email\"");
        registerSource.Should().Contain("id=\"password\" name=\"password\" type=\"password\"");
        registerSource.Should().Contain("autocomplete=\"new-password\"");
        registerSource.Should().Contain("fieldset class=\"d-none\"");
        registerSource.Should().Contain("id=\"locale\" name=\"locale\"");
        registerSource.Should().Contain("AdminCultureCatalog.German");
        registerSource.Should().Contain("AdminCultureCatalog.English");
        registerSource.Should().Contain("id=\"currency\" name=\"currency\"");
        registerSource.Should().Contain("SiteSettingDto.DefaultCurrencyDefault");
        registerSource.Should().Contain("id=\"timezone\" name=\"timezone\"");
        registerSource.Should().Contain("SiteSettingDto.TimeZoneDefault");
        registerSource.Should().Contain("name=\"supportedCulturesCsv\" value=\"@supportedCulturesCsv\"");
        registerSource.Should().Contain("@T.T(\"CreateAccount\")");
        registerSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");
    }


    [Fact]
    public void AccountLoginView_Should_KeepCredentialAndPasskeySurfaceContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Account", "Login.cshtml"));

        source.Should().Contain("ViewData[\"Title\"] = T.T(\"Login\")");
        source.Should().Contain("<h1 class=\"mb-3\">@T.T(\"Login\")</h1>");
        source.Should().Contain("asp-action=\"LoginPost\" asp-controller=\"Account\" method=\"post\" class=\"mb-3\"");
        source.Should().Contain("@Html.AntiForgeryToken()");
        source.Should().Contain("<input type=\"hidden\" name=\"returnUrl\" value=\"@returnUrl\" />");
        source.Should().Contain("<label for=\"email\" class=\"form-label\">@T.T(\"Email\")</label>");
        source.Should().Contain("<input id=\"email\" name=\"email\" type=\"email\" class=\"form-control\" required />");
        source.Should().Contain("data-valmsg-for=\"email\"");
        source.Should().Contain("<label for=\"password\" class=\"form-label\">@T.T(\"Password\")</label>");
        source.Should().Contain("<input id=\"password\" name=\"password\" type=\"password\" class=\"form-control\" required />");
        source.Should().Contain("data-valmsg-for=\"password\"");
        source.Should().Contain("<input id=\"rememberMe\" name=\"rememberMe\" type=\"checkbox\" class=\"form-check-input\" />");
        source.Should().Contain("<label for=\"rememberMe\" class=\"form-check-label\">@T.T(\"RememberMe\")</label>");
        source.Should().Contain("<button class=\"btn btn-primary\" type=\"submit\">@T.T(\"Login\")</button>");
        source.Should().Contain("<fieldset class=\"d-none\">");
        source.Should().Contain("<form id=\"passkey-form\" method=\"post\" action=\"@Url.Action(\"WebAuthnFinishLogin\", \"Account\")\">");
        source.Should().Contain("name=\"challengeTokenId\" id=\"challengeTokenId\"");
        source.Should().Contain("name=\"clientResponseJson\" id=\"clientResponseJson\"");
        source.Should().Contain("name=\"userId\" id=\"userId\"");
        source.Should().Contain("name=\"rememberMe\" value=\"true\"");
        source.Should().Contain("name=\"returnUrl\" value=\"@returnUrl\"");
        source.Should().Contain(@"<button id=""passkey-btn"" class=""btn btn-secondary mt-2"" type=""button"" data-passkey-preparing-label=""@T.T(""PasskeyPreparing"")"">@T.T(""LoginWithPasskey"")</button>");
        source.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");
        source.Should().NotContain("document.getElementById('passkey-btn')?.addEventListener('click', () => {");
        source.Should().NotContain("alert('@T.T(\"PasskeyPreparing\")');");
    }


    [Fact]
    public void AccountRegisterView_Should_KeepCredentialAndHiddenPreferenceBaselineContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Account", "Register.cshtml"));

        source.Should().Contain("@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers");
        source.Should().Contain("ViewData[\"Title\"] = T.T(\"Register\")");
        source.Should().Contain("<h1 class=\"mb-3\">@T.T(\"Register\")</h1>");
        source.Should().Contain("asp-action=\"RegisterPost\" method=\"post\" class=\"needs-validation\" novalidate");
        source.Should().Contain("@Html.AntiForgeryToken()");
        source.Should().Contain("<div asp-validation-summary=\"ModelOnly\" class=\"text-danger\"></div>");
        source.Should().Contain("<label for=\"email\" class=\"form-label\">@T.T(\"Email\")</label>");
        source.Should().Contain("<input id=\"email\" name=\"email\" type=\"email\" class=\"form-control\" required />");
        source.Should().Contain("data-valmsg-for=\"email\"");
        source.Should().Contain("<label for=\"password\" class=\"form-label\">@T.T(\"Password\")</label>");
        source.Should().Contain("<input id=\"password\" name=\"password\" type=\"password\" class=\"form-control\" required autocomplete=\"new-password\" />");
        source.Should().Contain("data-valmsg-for=\"password\"");
        source.Should().Contain("var defaultCurrency = (string?)ViewData[\"DefaultCurrency\"] ?? Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCurrencyDefault;");
        source.Should().Contain("var defaultLocale = (string?)ViewData[\"DefaultLocale\"] ?? Darwin.WebAdmin.Localization.AdminCultureCatalog.DefaultCulture;");
        source.Should().Contain("var defaultTimeZone = (string?)ViewData[\"DefaultTimeZone\"] ?? Darwin.Application.Settings.DTOs.SiteSettingDto.TimeZoneDefault;");
        source.Should().Contain("var supportedCulturesCsv = (string?)ViewData[\"SupportedCulturesCsv\"] ?? Darwin.WebAdmin.Localization.AdminCultureCatalog.SupportedCulturesCsvDefault;");
        source.Should().Contain("<fieldset class=\"d-none\">");
        source.Should().Contain("<label for=\"locale\" class=\"form-label\">@T.T(\"Locale\")</label>");
        source.Should().Contain("@T.T(\"LocaleGermanGermany\")");
        source.Should().Contain("@T.T(\"LocaleEnglishUnitedStates\")");
        source.Should().Contain("<label for=\"currency\" class=\"form-label\">@T.T(\"Currency\")</label>");
        source.Should().Contain("@T.T(\"CurrencyEuro\")");
        source.Should().Contain("@T.T(\"CurrencyUsd\")");
        source.Should().Contain("<label for=\"timezone\" class=\"form-label\">@T.T(\"TimeZone\")</label>");
        source.Should().Contain("@T.T(\"TimeZoneEuropeBerlin\")");
        source.Should().Contain("@T.T(\"Utc\")");
        source.Should().Contain("<input type=\"hidden\" name=\"supportedCulturesCsv\" value=\"@supportedCulturesCsv\" />");
        source.Should().Contain("<button type=\"submit\" class=\"btn btn-primary\">@T.T(\"CreateAccount\")</button>");
        source.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");
    }


    [Fact]
    public void AccountLoginTwoFactorView_Should_KeepChallengeStateAndCodeEntryContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Account", "LoginTwoFactor.cshtml"));

        source.Should().Contain("ViewData[\"Title\"] = T.T(\"LoginTwoFactorTitle\")");
        source.Should().Contain("var returnUrl = (string?)ViewData[\"ReturnUrl\"] ?? \"\"");
        source.Should().Contain("var rememberMe = (bool)(ViewData[\"RememberMe\"] ?? false);");
        source.Should().Contain("var userId = (string?)ViewData[\"TwoFaUserId\"] ?? \"\"");
        source.Should().Contain("<h1 class=\"mb-3\">@T.T(\"LoginTwoFactorHeading\")</h1>");
        source.Should().Contain("asp-action=\"LoginTwoFactorPost\" asp-controller=\"Account\" method=\"post\"");
        source.Should().Contain("@Html.AntiForgeryToken()");
        source.Should().Contain("<input type=\"hidden\" name=\"userId\" value=\"@userId\" />");
        source.Should().Contain("<input type=\"hidden\" name=\"rememberMe\" value=\"@(rememberMe ? \"true\" : \"false\")\" />");
        source.Should().Contain("<input type=\"hidden\" name=\"returnUrl\" value=\"@returnUrl\" />");
        source.Should().Contain("<label for=\"code\" class=\"form-label\">@T.T(\"LoginTwoFactorCodeLabel\")</label>");
        source.Should().Contain("<input id=\"code\" name=\"code\" type=\"number\" class=\"form-control\" inputmode=\"numeric\" required />");
        source.Should().Contain("data-valmsg-for=\"code\"");
        source.Should().Contain("<button class=\"btn btn-primary\" type=\"submit\">@T.T(\"LoginTwoFactorConfirmAction\")</button>");
        source.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");
    }


    [Fact]
    public void SharedAlertsAndConfirmDeleteModal_Should_KeepFeedbackAndDestructiveActionContractsWired()
    {
        var alertsSource = ReadWebAdminFile(Path.Combine("Views", "Shared", "_Alerts.cshtml"));
        var confirmDeleteSource = ReadWebAdminFile(Path.Combine("Views", "Shared", "_ConfirmDeleteModal.cshtml"));
        var adminCoreSource = ReadWebAdminFile(Path.Combine("wwwroot", "js", "admin-core.js"));
        var dynamicLinesSource = ReadWebAdminFile(Path.Combine("wwwroot", "js", "dynamic-lines.js"));

        alertsSource.Should().Contain("@inject Darwin.WebAdmin.Localization.IAdminTextLocalizer T");
        alertsSource.Should().Contain("string? success = TempData[\"Success\"] as string;");
        alertsSource.Should().Contain("string? error = TempData[\"Error\"] as string;");
        alertsSource.Should().Contain("string? warning = TempData[\"Warning\"] as string;");
        alertsSource.Should().Contain("string? info = TempData[\"Info\"] as string;");
        alertsSource.Should().Contain("if (!string.IsNullOrWhiteSpace(success))");
        alertsSource.Should().Contain("alert alert-success alert-dismissible fade show");
        alertsSource.Should().Contain("if (!string.IsNullOrWhiteSpace(error))");
        alertsSource.Should().Contain("alert alert-danger alert-dismissible fade show");
        alertsSource.Should().Contain("if (!string.IsNullOrWhiteSpace(warning))");
        alertsSource.Should().Contain("alert alert-warning alert-dismissible fade show");
        alertsSource.Should().Contain("if (!string.IsNullOrWhiteSpace(info))");
        alertsSource.Should().Contain("alert alert-info alert-dismissible fade show");
        alertsSource.Should().Contain("aria-label=\"@T.T(\"Close\")\"");

        confirmDeleteSource.Should().Contain("@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers");
        confirmDeleteSource.Should().Contain("@inject Darwin.WebAdmin.Localization.IAdminTextLocalizer T");
        confirmDeleteSource.Should().Contain("id=\"confirmDeleteModal\"");
        confirmDeleteSource.Should().Contain("id=\"confirmDeleteTitle\"");
        confirmDeleteSource.Should().Contain("@T.T(\"ConfirmDeleteTitle\")");
        confirmDeleteSource.Should().Contain("@T.T(\"ConfirmDeleteWarning\")");
        confirmDeleteSource.Should().Contain("@T.T(\"ConfirmDeleteItemLabel\")");
        confirmDeleteSource.Should().Contain("id=\"confirmDeleteForm\" method=\"post\"");
        confirmDeleteSource.Should().Contain("@Html.AntiForgeryToken()");
        confirmDeleteSource.Should().Contain("name=\"id\" id=\"confirmDeleteId\"");
        confirmDeleteSource.Should().Contain("name=\"rowVersion\" id=\"confirmDeleteRowVersion\"");
        confirmDeleteSource.Should().Contain("name=\"userId\"");
        confirmDeleteSource.Should().Contain("@T.T(\"Cancel\")");
        confirmDeleteSource.Should().Contain("@T.T(\"Delete\")");
        confirmDeleteSource.Should().NotContain("<script>");

        adminCoreSource.Should().Contain("window.darwinAdmin.configureConfirmDeleteModal = function (event)");
        adminCoreSource.Should().Contain("const modalEl = event.target;");
        adminCoreSource.Should().Contain("const form = document.getElementById('confirmDeleteForm');");
        adminCoreSource.Should().Contain("const actionUrl = button.getAttribute('data-action');");
        adminCoreSource.Should().Contain("const hxTarget = button.getAttribute('data-hx-target');");
        adminCoreSource.Should().Contain("const hxSwap = button.getAttribute('data-hx-swap') || 'innerHTML';");
        adminCoreSource.Should().Contain("form.setAttribute('action', actionUrl);");
        adminCoreSource.Should().Contain("form.setAttribute('hx-post', actionUrl);");
        adminCoreSource.Should().Contain("form.setAttribute('hx-target', hxTarget);");
        adminCoreSource.Should().Contain("form.setAttribute('hx-swap', hxSwap);");
        adminCoreSource.Should().Contain("document.addEventListener('show.bs.modal', window.darwinAdmin.configureConfirmDeleteModal);");
        adminCoreSource.Should().Contain("event.detail.elt.id === 'confirmDeleteForm'");
        adminCoreSource.Should().Contain("window.darwinAdmin.hideModal('confirmDeleteModal');");
    }


    [Fact]
    public void SharedLayout_Should_KeepGlobalNavigationCultureLogoutAndHtmxContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Shared", "_Layout.cshtml"));
        var adminCoreSource = ReadWebAdminFile(Path.Combine("wwwroot", "js", "admin-core.js"));
        var dynamicLinesSource = ReadWebAdminFile(Path.Combine("wwwroot", "js", "dynamic-lines.js"));
        var shippingMethodsSource = ReadWebAdminFile(Path.Combine("wwwroot", "js", "shipping-methods.js"));
        var mediaSource = ReadWebAdminFile(Path.Combine("wwwroot", "js", "media.js"));

        source.Should().Contain("@using Microsoft.AspNetCore.Antiforgery");
        source.Should().Contain("@inject Darwin.WebAdmin.Infrastructure.PermissionRazorHelper Perms");
        source.Should().Contain("@inject IAntiforgery Antiforgery");
        source.Should().Contain("var canManageBusinesses = await Perms.HasAsync(\"ManageBusinessSupport\");");
        source.Should().Contain("var canManageIdentity = await Perms.HasAsync(\"FullAdminAccess\");");
        source.Should().Contain("var currentCulture = CultureInfo.CurrentUICulture?.Name ?? Darwin.WebAdmin.Localization.AdminCultureCatalog.DefaultCulture;");
        source.Should().Contain("var languageOptions = T.GetSupportedLanguageOptions();");
        source.Should().Contain("var returnUrl = $\"{Context?.Request?.Path}{Context?.Request?.QueryString}\";");
        source.Should().Contain("<title>@ViewData[\"Title\"] - @T.T(\"DarwinAdmin\")</title>");
        source.Should().Contain("<body class=\"bg-light\" data-alerts-url=\"@Url.Action(\"AlertsFragment\", \"Home\")\">");
        source.Should().Contain("<form asp-controller=\"Culture\" asp-action=\"SetCulture\" method=\"post\" class=\"d-inline-flex align-items-center gap-2\">");
        source.Should().Contain("<input type=\"hidden\" name=\"returnUrl\" value=\"@returnUrl\" />");
        source.Should().Contain("<label for=\"admin-culture-switcher\" class=\"small text-muted mb-0\">@T.T(\"Language\")</label>");
        source.Should().Contain("<select id=\"admin-culture-switcher\" name=\"culture\" class=\"form-select form-select-sm\" data-culture-switcher>");
        source.Should().Contain("selected=\"@(string.Equals(currentCulture, option.Culture, StringComparison.OrdinalIgnoreCase))\"");
        source.Should().Contain("<form method=\"post\" action=\"/account/logout\" class=\"d-inline\">");
        source.Should().Contain("@T.T(\"Logout\")");
        source.Should().Contain("@(!string.IsNullOrWhiteSpace(User?.Identity?.Name) ? User.Identity!.Name : T.T(\"CurrentUserFallback\"))");
        source.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        source.Should().Contain("asp-controller=\"Home\" asp-action=\"Index\"");
        source.Should().Contain("asp-controller=\"Products\" asp-action=\"Index\"");
        source.Should().Contain("asp-controller=\"Categories\" asp-action=\"Index\"");
        source.Should().Contain("asp-controller=\"Brands\" asp-action=\"Index\"");
        source.Should().Contain("asp-controller=\"AddOnGroups\" asp-action=\"Index\"");
        source.Should().Contain("asp-controller=\"Pages\" asp-action=\"Index\"");
        source.Should().Contain("asp-controller=\"Media\" asp-action=\"Index\"");
        source.Should().Contain("asp-controller=\"Orders\" asp-action=\"ShipmentsQueue\"");
        source.Should().Contain("asp-controller=\"Crm\" asp-action=\"Customers\"");
        source.Should().Contain("asp-controller=\"Loyalty\" asp-action=\"Programs\"");
        source.Should().Contain("asp-controller=\"Inventory\" asp-action=\"Warehouses\"");
        source.Should().Contain("asp-controller=\"Billing\" asp-action=\"Payments\"");
        source.Should().Contain("asp-controller=\"Billing\" asp-action=\"TaxCompliance\"");
        source.Should().Contain("asp-controller=\"Businesses\" asp-action=\"Index\"");
        source.Should().Contain("asp-controller=\"MobileOperations\" asp-action=\"Index\"");
        source.Should().Contain("asp-controller=\"BusinessCommunications\" asp-action=\"Index\"");
        source.Should().Contain("asp-controller=\"Users\" asp-action=\"Index\"");
        source.Should().Contain("asp-controller=\"Roles\" asp-action=\"Index\"");
        source.Should().Contain("asp-controller=\"Permissions\" asp-action=\"Index\"");
        source.Should().Contain("asp-controller=\"SiteSettings\" asp-action=\"Edit\"");
        source.Should().Contain("<script src=\"~/js/admin-core.js\" asp-append-version=\"true\"></script>");
        source.Should().Contain("<script src=\"~/js/dynamic-lines.js\" asp-append-version=\"true\"></script>");
        source.Should().Contain("<script src=\"~/js/shipping-methods.js\" asp-append-version=\"true\"></script>");
        source.Should().Contain("<script src=\"~/js/media.js\" asp-append-version=\"true\"></script>");
        source.Should().Contain("@RenderSection(\"Scripts\", required: false)");

        adminCoreSource.Should().Contain("window.darwinAdmin.initBootstrapUi = function (root)");
        adminCoreSource.Should().Contain("bootstrap.Popover.getOrCreateInstance(el);");
        adminCoreSource.Should().Contain("bootstrap.Tooltip.getOrCreateInstance(el);");
        adminCoreSource.Should().Contain("window.darwinAdmin.refreshAlerts = function (url)");
        adminCoreSource.Should().Contain("const targetUrl = url || document.body.dataset.alertsUrl;");
        adminCoreSource.Should().Contain("htmx.ajax('GET', targetUrl, '#alerts-container');");
        adminCoreSource.Should().Contain("window.darwinAdmin.hideModal = function (modalId)");
        adminCoreSource.Should().Contain("document.addEventListener('DOMContentLoaded', function ()");
        adminCoreSource.Should().Contain("document.addEventListener('change', function (event)");
        adminCoreSource.Should().Contain("const switcher = event.target.closest('[data-culture-switcher]');");
        adminCoreSource.Should().Contain("switcher.form.submit();");
        adminCoreSource.Should().Contain("document.body.addEventListener('htmx:configRequest', function (event)");
        adminCoreSource.Should().Contain("const tokenInput = document.querySelector('input[name=\"__RequestVerificationToken\"]');");
        adminCoreSource.Should().Contain("event.detail.headers.RequestVerificationToken = token;");
        adminCoreSource.Should().Contain("document.body.addEventListener('htmx:afterSwap', function (event)");
        adminCoreSource.Should().Contain("window.darwinAdmin.initBootstrapUi(event.target);");

        dynamicLinesSource.Should().Contain("event.target.closest('[data-dynamic-lines-add]')");
        dynamicLinesSource.Should().Contain("data-dynamic-lines-container");
        dynamicLinesSource.Should().Contain("data-dynamic-lines-template");
        dynamicLinesSource.Should().Contain("template.innerHTML.replaceAll('__index__', index.toString())");
        dynamicLinesSource.Should().Contain("event.target.closest('[data-dynamic-lines-remove]')");

        shippingMethodsSource.Should().Contain("window.darwinAdmin.initShippingMethodForm = function ()");
        shippingMethodsSource.Should().Contain("event.target.closest('[data-shipping-rate-add]')");
        shippingMethodsSource.Should().Contain("event.target.closest('[data-shipping-rate-remove]')");
        shippingMethodsSource.Should().Contain("window.darwinAdmin.removeShippingRateRow(removeButton);");
        shippingMethodsSource.Should().Contain("replace(/Rates\\[\\d+\\]/, 'Rates[' + index + ']')");

        mediaSource.Should().Contain("event.target.closest('[data-copy-media-url]')");
        mediaSource.Should().Contain("navigator.clipboard.writeText(url);");
        mediaSource.Should().Contain("data-copied-label");
    }


    [Fact]
    public void SharedAuthLayout_Should_KeepCultureSwitcherAndAuthShellContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Shared", "_AuthLayout.cshtml"));
        var adminCoreSource = ReadWebAdminFile(Path.Combine("wwwroot", "js", "admin-core.js"));

        source.Should().Contain("@using System.Globalization");
        source.Should().Contain("@inject Darwin.WebAdmin.Localization.IAdminTextLocalizer T");
        source.Should().Contain("var currentCulture = CultureInfo.CurrentUICulture?.Name ?? Darwin.WebAdmin.Localization.AdminCultureCatalog.DefaultCulture;");
        source.Should().Contain("var languageOptions = T.GetSupportedLanguageOptions();");
        source.Should().Contain("var requestPath = Context?.Request?.Path.Value ?? \"/account/login\";");
        source.Should().Contain("var requestedReturnUrl = Context?.Request?.Query[\"returnUrl\"].ToString();");
        source.Should().Contain("var returnUrl = Url.IsLocalUrl(requestedReturnUrl)");
        source.Should().Contain("? $\"{requestPath}?returnUrl={Uri.EscapeDataString(requestedReturnUrl)}\"");
        source.Should().Contain(": requestPath;");
        source.Should().Contain("<html lang=\"@currentCulture\">");
        source.Should().Contain("<title>@ViewData[\"Title\"] - @T.T(\"DarwinAdmin\")</title>");
        source.Should().Contain("asp-controller=\"Account\" asp-action=\"Login\"");
        source.Should().Contain("<img src=\"~/images/DarwinLogo.png\" alt=\"Darwin\"");
        source.Should().Contain("<form asp-controller=\"Culture\" asp-action=\"SetCulture\" method=\"post\" class=\"d-inline-flex align-items-center gap-2\">");
        source.Should().Contain("@Html.AntiForgeryToken()");
        source.Should().Contain("<input type=\"hidden\" name=\"returnUrl\" value=\"@returnUrl\" />");
        source.Should().Contain("<label for=\"auth-culture-switcher\" class=\"small text-muted mb-0\">@T.T(\"Language\")</label>");
        source.Should().Contain("<select id=\"auth-culture-switcher\" name=\"culture\" class=\"form-select form-select-sm\" data-culture-switcher>");
        source.Should().Contain("selected=\"@(string.Equals(currentCulture, option.Culture, StringComparison.OrdinalIgnoreCase))\"");
        source.Should().Contain("<div class=\"card shadow-sm border-0\">");
        source.Should().Contain("<div class=\"card-body p-4 p-lg-5\">");
        source.Should().Contain("@RenderBody()");
        source.Should().Contain("<script src=\"~/lib/htmx/htmx.min.js\" asp-append-version=\"true\"></script>");
        source.Should().Contain("<script src=\"~/js/admin-core.js\" asp-append-version=\"true\"></script>");
        source.Should().Contain("@RenderSection(\"Scripts\", required: false)");

        adminCoreSource.Should().Contain("document.addEventListener('change', function (event)");
        adminCoreSource.Should().Contain("const switcher = event.target.closest('[data-culture-switcher]');");
        adminCoreSource.Should().Contain("switcher.form.submit();");
    }


    [Fact]
    public void SharedTagHelpers_Should_KeepActiveNavPagerFieldHelpAndSettingSelectContractsWired()
    {
        var activeNavSource = ReadWebAdminFile(Path.Combine("TagHelpers", "ActiveNavLinkTagHelper.cs"));
        var pagerSource = ReadWebAdminFile(Path.Combine("TagHelpers", "PagerTagHelper.cs"));
        var fieldHelpSource = ReadWebAdminFile(Path.Combine("TagHelpers", "FieldHelpTagHelper.cs"));
        var settingSelectSource = ReadWebAdminFile(Path.Combine("TagHelpers", "SettingSelectTagHelper.cs"));

        activeNavSource.Should().Contain("[HtmlTargetElement(\"active-nav\", Attributes = \"asp-controller\", TagStructure = TagStructure.NormalOrSelfClosing)]");
        activeNavSource.Should().Contain("public sealed class ActiveNavLinkTagHelper : TagHelper");
        activeNavSource.Should().Contain("output.TagName = \"a\";");
        activeNavSource.Should().Contain("var actionName = string.IsNullOrEmpty(Action) ? \"Index\" : Action;");
        activeNavSource.Should().Contain("var href = urlHelper.Action(actionName, Controller, routeValues);");
        activeNavSource.Should().Contain("if (IsActiveRoute())");
        activeNavSource.Should().Contain("css = string.IsNullOrWhiteSpace(css) ? \"active\" : $\"{css} active\";");
        activeNavSource.Should().Contain("output.Attributes.RemoveAll(\"asp-area\");");
        activeNavSource.Should().Contain("output.Attributes.RemoveAll(\"asp-controller\");");
        activeNavSource.Should().Contain("output.Attributes.RemoveAll(\"asp-action\");");
        activeNavSource.Should().Contain("static string Normalize(string? s) => (s ?? string.Empty).Trim().ToLowerInvariant();");
        activeNavSource.Should().Contain("if (string.IsNullOrEmpty(Action) && curController == wantController)");

        pagerSource.Should().Contain("[HtmlTargetElement(\"pager\", TagStructure = TagStructure.NormalOrSelfClosing)]");
        pagerSource.Should().Contain("private static readonly int[] DefaultPageSizes = { 10, 20, 50, 100 };");
        pagerSource.Should().Contain("if (Total <= 0)");
        pagerSource.Should().Contain("output.SuppressOutput();");
        pagerSource.Should().Contain("var viewContext = ViewContext ?? throw new InvalidOperationException(\"PagerTagHelper requires a non-null ViewContext.\");");
        pagerSource.Should().Contain("var pageSize = PageSize <= 0 ? 20 : PageSize;");
        pagerSource.Should().Contain("var htmxEnabled = !string.IsNullOrWhiteSpace(HxTarget);");
        pagerSource.Should().Contain("var hxSwap = string.IsNullOrWhiteSpace(HxSwap) ? \"outerHTML\" : HxSwap!;");
        pagerSource.Should().Contain("var hxPushUrl = string.IsNullOrWhiteSpace(HxPushUrl) ? \"true\" : HxPushUrl!;");
        pagerSource.Should().Contain("string BuildWorkspaceUrl()");
        pagerSource.Should().Contain("sb.Append(\"<form method=\\\"get\\\" class=\\\"d-inline-block\\\"\");");
        pagerSource.Should().Contain("sb.Append($\" hx-get=\\\"{BuildWorkspaceUrl()}\\\"\");");
        pagerSource.Should().Contain("sb.AppendLine(\"    <span class=\\\"input-group-text\\\">Page size</span>\");");
        pagerSource.Should().Contain("foreach (var size in DefaultPageSizes)");
        pagerSource.Should().Contain("sel.addEventListener('change', function(){");
        pagerSource.Should().Contain("if (form.requestSubmit) form.requestSubmit(); else form.submit();");
        pagerSource.Should().Contain("PageItem(\"\u00AB First\", 1, page == 1, aria: \"First\");");
        pagerSource.Should().Contain("PageItem(\"Last \u00BB\", totalPagesSafe, page == totalPagesSafe, aria: \"Last\");");

        fieldHelpSource.Should().Contain("[HtmlTargetElement(\"field-help\", TagStructure = TagStructure.NormalOrSelfClosing)]");
        fieldHelpSource.Should().Contain("public sealed class FieldHelpTagHelper : TagHelper");
        fieldHelpSource.Should().Contain("output.TagName = \"button\";");
        fieldHelpSource.Should().Contain("output.Attributes.SetAttribute(\"data-bs-toggle\", \"popover\");");
        fieldHelpSource.Should().Contain("output.Attributes.SetAttribute(\"data-bs-trigger\", \"focus\");");
        fieldHelpSource.Should().Contain("output.Attributes.SetAttribute(\"data-bs-placement\", Placement);");
        fieldHelpSource.Should().Contain("output.Attributes.SetAttribute(\"data-bs-content\", Content);");
        fieldHelpSource.Should().Contain("output.Attributes.SetAttribute(\"data-bs-html\", \"true\");");
        fieldHelpSource.Should().Contain("output.Content.SetHtmlContent(\"<span aria-hidden=\\\"true\\\" style=\\\"font-weight:600;\\\">i</span><span class=\\\"visually-hidden\\\">Help</span>\");");

        settingSelectSource.Should().Contain("[HtmlTargetElement(\"setting-select\", Attributes = \"asp-for\")]");
        settingSelectSource.Should().Contain("public sealed class SettingSelectTagHelper : TagHelper");
        settingSelectSource.Should().Contain("output.TagName = \"select\";");
        settingSelectSource.Should().Contain("output.Attributes.SetAttribute(\"class\", \"form-select\");");
        settingSelectSource.Should().Contain("var name = For.Name;");
        settingSelectSource.Should().Contain("var currentValue = For.Model?.ToString() ?? string.Empty;");
        settingSelectSource.Should().Contain("[HtmlAttributeName(\"setting\")]");
        settingSelectSource.Should().Contain("[HtmlAttributeName(\"key\")]");
        settingSelectSource.Should().Contain("var siteSettings = await _siteSettingCache.GetAsync();");
        settingSelectSource.Should().Contain("private readonly IAdminTextLocalizer _textLocalizer;");
        settingSelectSource.Should().Contain("var options = BuildOptions(siteSettings, currentValue);");
        settingSelectSource.Should().Contain("var encodedValue = HtmlEncoder.Default.Encode(option);");
        settingSelectSource.Should().Contain("var encodedLabel = HtmlEncoder.Default.Encode(GetOptionLabel(option));");
        settingSelectSource.Should().Contain("var configuredName = string.IsNullOrWhiteSpace(Setting) ? Key : Setting;");
        settingSelectSource.Should().Contain("if (string.Equals(configuredName, \"SupportedCurrenciesCsv\", StringComparison.OrdinalIgnoreCase))");
        settingSelectSource.Should().Contain("if (string.Equals(configuredName, \"SupportedTimezonesCsv\", StringComparison.OrdinalIgnoreCase))");
        settingSelectSource.Should().Contain("return _textLocalizer.T(\"CurrencyEuro\");");
        settingSelectSource.Should().Contain("return _textLocalizer.T(\"TimeZoneEuropeBerlin\");");
        settingSelectSource.Should().Contain("return _textLocalizer.T(\"Utc\");");
        settingSelectSource.Should().Contain("var selected = string.Equals(option, currentValue, StringComparison.OrdinalIgnoreCase) ? \"selected\" : null;");
        settingSelectSource.Should().Contain("output.Content.SetHtmlContent(innerHtml);");
    }


    [Fact]
    public void SharedTagHelpers_Should_KeepFieldHelpAndSettingSelectFallbackContractsWired()
    {
        var fieldHelpSource = ReadWebAdminFile(Path.Combine("TagHelpers", "FieldHelpTagHelper.cs"));
        var settingSelectSource = ReadWebAdminFile(Path.Combine("TagHelpers", "SettingSelectTagHelper.cs"));

        fieldHelpSource.Should().Contain("public string? Title { get; set; }");
        fieldHelpSource.Should().Contain("public string? Content { get; set; }");
        fieldHelpSource.Should().Contain("public string Placement { get; set; } = \"right\";");
        fieldHelpSource.Should().Contain("output.Attributes.SetAttribute(\"type\", \"button\");");
        fieldHelpSource.Should().Contain("output.Attributes.SetAttribute(\"class\", \"btn btn-sm btn-outline-secondary ms-2 rounded-circle lh-1\");");
        fieldHelpSource.Should().Contain("output.Attributes.SetAttribute(\"style\", \"width:1.75rem;height:1.75rem;padding:0;\");");
        fieldHelpSource.Should().Contain("if (!string.IsNullOrWhiteSpace(Title))");
        fieldHelpSource.Should().Contain("output.Attributes.SetAttribute(\"title\", Title);");
        fieldHelpSource.Should().Contain("if (!string.IsNullOrWhiteSpace(Content))");

        settingSelectSource.Should().Contain("private readonly ISiteSettingCache _siteSettingCache;");
        settingSelectSource.Should().Contain("public SettingSelectTagHelper(ISiteSettingCache siteSettingCache, IAdminTextLocalizer textLocalizer)");
        settingSelectSource.Should().Contain("_siteSettingCache = siteSettingCache;");
        settingSelectSource.Should().Contain("_textLocalizer = textLocalizer;");
        settingSelectSource.Should().Contain("output.Attributes.SetAttribute(\"id\", name);");
        settingSelectSource.Should().Contain("output.Attributes.SetAttribute(\"name\", name);");
        settingSelectSource.Should().Contain("private string[] BuildOptions(SiteSettingDto siteSettings, string currentValue)");
        settingSelectSource.Should().Contain("private string GetOptionLabel(string option)");
        settingSelectSource.Should().Contain("private static string[] SplitCsvOrFallback(string rawValue, string currentValue)");
        settingSelectSource.Should().Contain("return new[] { rawValue.Trim() };");
        settingSelectSource.Should().Contain("return string.IsNullOrEmpty(currentValue) ? Array.Empty<string>() : new[] { currentValue };");
        settingSelectSource.Should().Contain("foreach (var option in options)");
        settingSelectSource.Should().Contain("innerHtml += $\"<option value=\\\"{encodedValue}\\\"{(selected != null ? \" selected\" : string.Empty)}>{encodedLabel}</option>\";");
    }


    [Fact]
    public void SharedViewImportsAndValidationPartial_Should_KeepGlobalTagHelpersAndValidationContractsWired()
    {
        var viewImportsSource = ReadWebAdminFile(Path.Combine("Views", "_ViewImports.cshtml"));
        var validationSource = ReadWebAdminFile(Path.Combine("Views", "Shared", "_ValidationScriptsPartial.cshtml"));
        var viewStartSource = ReadWebAdminFile(Path.Combine("Views", "_ViewStart.cshtml"));
        var accountViewStartSource = ReadWebAdminFile(Path.Combine("Views", "Account", "_ViewStart.cshtml"));

        viewImportsSource.Should().Contain("@using Microsoft.AspNetCore.Html");
        viewImportsSource.Should().Contain("@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers");
        viewImportsSource.Should().Contain("@addTagHelper *, ViewComponents");
        viewImportsSource.Should().Contain("@addTagHelper *, Darwin.WebAdmin");

        validationSource.Should().Contain("@* Client-side validation scripts (loaded on pages that include this partial) *@");
        validationSource.Should().Contain("<script src=\"~/lib/jquery-validation/jquery.validate.min.js\" asp-append-version=\"true\"></script>");
        validationSource.Should().Contain("<script src=\"~/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.min.js\" asp-append-version=\"true\"></script>");
        validationSource.Should().NotContain("https://");
        validationSource.Should().NotContain("crossorigin=\"anonymous\"");

        viewStartSource.Should().Contain("Layout = \"~/Views/Shared/_Layout.cshtml\";");
        accountViewStartSource.Should().Contain("Layout = \"~/Views/Shared/_AuthLayout.cshtml\";");
    }


    [Fact]
    public void FeatureViewImports_Should_KeepSharedLocalizerInjectionContractsWired()
    {
        var addOnGroupsSource = ReadWebAdminFile(Path.Combine("Views", "AddOnGroups", "_ViewImports.cshtml"));
        var brandsSource = ReadWebAdminFile(Path.Combine("Views", "Brands", "_ViewImports.cshtml"));
        var categoriesSource = ReadWebAdminFile(Path.Combine("Views", "Categories", "_ViewImports.cshtml"));
        var loyaltySource = ReadWebAdminFile(Path.Combine("Views", "Loyalty", "_ViewImports.cshtml"));
        var mobileOperationsSource = ReadWebAdminFile(Path.Combine("Views", "MobileOperations", "_ViewImports.cshtml"));
        var pagesSource = ReadWebAdminFile(Path.Combine("Views", "Pages", "_ViewImports.cshtml"));
        var productsSource = ReadWebAdminFile(Path.Combine("Views", "Products", "_ViewImports.cshtml"));
        var shippingMethodsSource = ReadWebAdminFile(Path.Combine("Views", "ShippingMethods", "_ViewImports.cshtml"));

        addOnGroupsSource.Should().Contain("@inject Darwin.WebAdmin.Localization.IAdminTextLocalizer T");
        brandsSource.Should().Contain("@inject Darwin.WebAdmin.Localization.IAdminTextLocalizer T");
        categoriesSource.Should().Contain("@inject Darwin.WebAdmin.Localization.IAdminTextLocalizer T");
        loyaltySource.Should().Contain("@inject Darwin.WebAdmin.Localization.IAdminTextLocalizer T");
        mobileOperationsSource.Should().Contain("@inject Darwin.WebAdmin.Localization.IAdminTextLocalizer T");
        pagesSource.Should().Contain("@inject Darwin.WebAdmin.Localization.IAdminTextLocalizer T");
        productsSource.Should().Contain("@inject Darwin.WebAdmin.Localization.IAdminTextLocalizer T");
        shippingMethodsSource.Should().Contain("@inject Darwin.WebAdmin.Localization.IAdminTextLocalizer T");
    }


    [Fact]
    public void AdminCultureCatalog_Should_KeepCanonicalCultureAndNormalizationContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Localization", "AdminCultureCatalog.cs"));

        source.Should().Contain("public const string German = SiteSettingDto.DefaultCultureDefault;");
        source.Should().Contain("public const string English = \"en-US\";");
        source.Should().Contain("public const string DefaultCulture = German;");
        source.Should().Contain("public const string SupportedCulturesCsvDefault = SiteSettingDto.SupportedCulturesCsvDefault;");
        source.Should().Contain("private static readonly IReadOnlyList<(string Culture, string Label)> _languageOptions =");
        source.Should().Contain("(German, \"Deutsch\")");
        source.Should().Contain("(English, \"English\")");
        source.Should().Contain("public static IReadOnlyList<(string Culture, string Label)> LanguageOptions => _languageOptions;");
        source.Should().Contain("public static string NormalizeUiCulture(string? culture)");
        source.Should().Contain("string.Equals(culture, \"de\", StringComparison.OrdinalIgnoreCase)");
        source.Should().Contain("string.Equals(culture, German, StringComparison.OrdinalIgnoreCase)");
        source.Should().Contain("string.Equals(culture, \"en\", StringComparison.OrdinalIgnoreCase)");
        source.Should().Contain("string.Equals(culture, English, StringComparison.OrdinalIgnoreCase)");
        source.Should().Contain("return DefaultCulture;");
        source.Should().Contain("public static List<string> NormalizeSupportedCultureNames(string? supportedCulturesCsv)");
        source.Should().Contain("var cultureNames = (supportedCulturesCsv ?? SupportedCulturesCsvDefault)");
        source.Should().Contain(".Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)");
        source.Should().Contain(".Select(NormalizeUiCulture)");
        source.Should().Contain(".Distinct(StringComparer.OrdinalIgnoreCase)");
        source.Should().Contain("cultureNames.Add(DefaultCulture);");
        source.Should().Contain("cultureNames.Add(English);");
        source.Should().Contain("cultureNames.Insert(0, DefaultCulture);");
        source.Should().Contain("if (!cultureNames.Contains(English, StringComparer.OrdinalIgnoreCase))");
    }


    [Fact]
    public void DashboardController_Should_BeRemovedAfterLegacyAdminRedirectCleanup()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Darwin.WebAdmin", "Controllers", "Admin", "Home", "DashboardController.cs"));

        File.Exists(path).Should().BeFalse("legacy /admin and /dashboard compatibility redirects are now retired from WebAdmin");
    }


    [Fact]
    public void ApplicationValidationAndLoyaltyFailureReasons_Should_KeepLocalizedAndCanonicalContractsWired()
    {
        var createBrandSource = ReadApplicationFile(Path.Combine("Catalog", "Commands", "CreateBrandHandler.cs"));
        var updateBrandSource = ReadApplicationFile(Path.Combine("Catalog", "Commands", "UpdateBrandHandler.cs"));
        var signInSource = ReadApplicationFile(Path.Combine("Identity", "Commands", "SignInHandler.cs"));
        var currentUserAddressesSource = ReadApplicationFile(Path.Combine("Identity", "Queries", "CurrentUserAddressQueries.cs"));
        var accrualSource = ReadApplicationFile(Path.Combine("Loyalty", "Commands", "ConfirmAccrualFromSessionHandler.cs"));
        var redemptionSource = ReadApplicationFile(Path.Combine("Loyalty", "Commands", "ConfirmRedemptionFromSessionHandler.cs"));
        var scanSessionResolverSource = ReadApplicationFile(Path.Combine("Loyalty", "Services", "ScanSessionTokenResolver.cs"));
        var validationResourceSource = ReadApplicationFile(Path.Combine("Resources", "ValidationResource.resx"));
        var validationResourceGermanSource = ReadApplicationFile(Path.Combine("Resources", "ValidationResource.de-DE.resx"));

        createBrandSource.Should().Contain("throw new FluentValidation.ValidationException(_localizer[\"BrandSlugMustBeUnique\"])");
        updateBrandSource.Should().Contain("throw new FluentValidation.ValidationException(_localizer[\"BrandSlugMustBeUnique\"])");

        signInSource.Should().Contain("private readonly IStringLocalizer<ValidationResource> _localizer;");
        signInSource.Should().Contain("IStringLocalizer<ValidationResource> localizer");
        signInSource.Should().Contain("FailureReason = _localizer[\"InvalidCredentials\"]");

        currentUserAddressesSource.Should().Contain("private readonly IStringLocalizer<ValidationResource> _localizer;");
        currentUserAddressesSource.Should().Contain("IStringLocalizer<ValidationResource> localizer");
        currentUserAddressesSource.Should().Contain("return Result<IReadOnlyList<AddressListItemDto>>.Fail(result.Error ?? _localizer[\"UserNotFound\"])");

        accrualSource.Should().Contain("session.FailureReason = \"TokenAlreadyConsumed\";");
        accrualSource.Should().Contain("session.FailureReason = \"AccountNotFound\";");
        accrualSource.Should().Contain("session.FailureReason = \"AccountNotActive\";");
        accrualSource.Should().Contain("session.FailureReason = \"Expired\";");

        redemptionSource.Should().Contain("session.FailureReason = \"TokenAlreadyConsumed\";");
        redemptionSource.Should().Contain("session.FailureReason = \"Expired\";");
        redemptionSource.Should().Contain("session.FailureReason = \"AccountNotFound\";");
        redemptionSource.Should().Contain("session.FailureReason = \"AccountNotActive\";");
        redemptionSource.Should().Contain("session.FailureReason = \"NoSelections\";");
        redemptionSource.Should().Contain("session.FailureReason = \"InvalidSelections\";");
        redemptionSource.Should().Contain("session.FailureReason = \"InsufficientPoints\";");

        scanSessionResolverSource.Should().Contain("session.Outcome = \"Expired\";");
        scanSessionResolverSource.Should().Contain("session.FailureReason = \"Expired\";");

        validationResourceSource.Should().Contain("<data name=\"BrandSlugMustBeUnique\"");
        validationResourceSource.Should().Contain("<data name=\"InvalidCredentials\"");
        validationResourceSource.Should().Contain("<data name=\"UserNotFound\"");
        validationResourceGermanSource.Should().Contain("<data name=\"BrandSlugMustBeUnique\"");
        validationResourceGermanSource.Should().Contain("<data name=\"InvalidCredentials\"");
        validationResourceGermanSource.Should().Contain("<data name=\"UserNotFound\"");
    }


    [Fact]
    public void StartupAndCultureController_Should_KeepLocalizationCookieAndRouteContractsWired()
    {
        var startupSource = ReadWebAdminFile(Path.Combine("Extensions", "Startup.cs"));
        var cultureSource = ReadWebAdminFile(Path.Combine("Controllers", "CultureController.cs"));

        startupSource.Should().Contain("public static async Task UseWebStartupAsync(this WebApplication app)");
        startupSource.Should().Contain("var localizationSettings = await LoadLocalizationSettingsAsync(app.Services);");
        startupSource.Should().Contain("var requestLocalizationOptions = new RequestLocalizationOptions");
        startupSource.Should().Contain("DefaultRequestCulture = new RequestCulture(localizationSettings.DefaultCulture)");
        startupSource.Should().Contain("SupportedCultures = localizationSettings.SupportedCultures");
        startupSource.Should().Contain("SupportedUICultures = localizationSettings.SupportedCultures");
        startupSource.Should().Contain("requestLocalizationOptions.RequestCultureProviders.Insert(0, new QueryStringRequestCultureProvider());");
        startupSource.Should().Contain("requestLocalizationOptions.RequestCultureProviders.Insert(1, new CookieRequestCultureProvider());");
        startupSource.Should().Contain("app.UseRequestLocalization(requestLocalizationOptions);");
        startupSource.Should().Contain("if (app.Environment.IsDevelopment())");
        startupSource.Should().Contain("app.UseDeveloperExceptionPage();");
        startupSource.Should().Contain("await app.Services.MigrateAndSeedAsync();");
        startupSource.Should().Contain("app.UseExceptionHandler(\"/Error\");");
        startupSource.Should().Contain("app.UseHsts();");
        startupSource.Should().Contain("app.UseHttpsRedirection();");
        startupSource.Should().Contain("app.UseStaticFiles();");
        startupSource.Should().Contain("app.UseRouting();");
        startupSource.Should().Contain("app.UseAuthentication();");
        startupSource.Should().Contain("app.UseAuthorization();");
        startupSource.Should().Contain("app.MapControllerRoute(");
        startupSource.Should().Contain("pattern: \"{controller=Home}/{action=Index}/{id?}\"");
        startupSource.Should().Contain("private static async Task<(CultureInfo[] SupportedCultures, string DefaultCulture)> LoadLocalizationSettingsAsync(IServiceProvider services)");
        startupSource.Should().Contain("var siteSettingCache = scope.ServiceProvider.GetRequiredService<ISiteSettingCache>();");
        startupSource.Should().Contain("var settings = await siteSettingCache.GetAsync().ConfigureAwait(false);");
        startupSource.Should().Contain("var cultureNames = AdminCultureCatalog.NormalizeSupportedCultureNames(settings.SupportedCulturesCsv);");
        startupSource.Should().Contain("var supportedCultures = cultureNames.Select(static x => new CultureInfo(x)).ToArray();");
        startupSource.Should().Contain("string.Equals(x.Name, AdminCultureCatalog.DefaultCulture, StringComparison.OrdinalIgnoreCase)");
        startupSource.Should().Contain("?? supportedCultures[0].Name;");

        cultureSource.Should().Contain("public sealed class CultureController : Controller");
        cultureSource.Should().Contain("[HttpPost]");
        cultureSource.Should().Contain("[ValidateAntiForgeryToken]");
        cultureSource.Should().Contain("public IActionResult SetCulture(string culture, string? returnUrl = null)");
        cultureSource.Should().Contain("var normalizedCulture = AdminCultureCatalog.NormalizeUiCulture(culture);");
        cultureSource.Should().Contain("CookieRequestCultureProvider.DefaultCookieName");
        cultureSource.Should().Contain("CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(normalizedCulture))");
        cultureSource.Should().Contain("Expires = DateTimeOffset.UtcNow.AddYears(1)");
        cultureSource.Should().Contain("IsEssential = true,");
        cultureSource.Should().Contain("HttpOnly = false,");
        cultureSource.Should().Contain("SameSite = SameSiteMode.Lax,");
        cultureSource.Should().Contain("Secure = Request.IsHttps");
        cultureSource.Should().Contain("if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))");
        cultureSource.Should().Contain("return LocalRedirect(returnUrl);");
        cultureSource.Should().Contain("return RedirectToAction(\"Index\", \"Home\");");
    }


    [Fact]
    public void LocalizationInfrastructure_Should_KeepOverrideCatalogAndAdminTextResolverContractsWired()
    {
        var adminTextLocalizerSource = ReadWebAdminFile(Path.Combine("Localization", "AdminTextLocalizer.cs"));
        var overrideCatalogSource = ReadWebAdminFile(Path.Combine("Localization", "AdminTextOverrideCatalog.cs"));

        adminTextLocalizerSource.Should().Contain("public interface IAdminTextLocalizer");
        adminTextLocalizerSource.Should().Contain("string T(string key);");
        adminTextLocalizerSource.Should().Contain("IReadOnlyList<(string Culture, string Label)> GetSupportedLanguageOptions();");
        adminTextLocalizerSource.Should().Contain("public sealed class AdminTextLocalizer : IAdminTextLocalizer");
        adminTextLocalizerSource.Should().Contain("var currentCulture = CultureInfo.CurrentUICulture?.Name ?? AdminCultureCatalog.DefaultCulture;");
        adminTextLocalizerSource.Should().Contain("var businessOverrides = GetCurrentBusinessOverrides();");
        adminTextLocalizerSource.Should().Contain("AdminTextOverrideCatalog.TryResolve(businessOverrides, currentCulture, key, out var businessOverrideValue)");
        adminTextLocalizerSource.Should().Contain("var platformOverrides = GetCurrentPlatformOverrides();");
        adminTextLocalizerSource.Should().Contain("AdminTextOverrideCatalog.TryResolve(platformOverrides, currentCulture, key, out var overrideValue)");
        adminTextLocalizerSource.Should().Contain("var localized = _localizer[key];");
        adminTextLocalizerSource.Should().Contain("return !localized.ResourceNotFound && !string.Equals(localized.Value, key, StringComparison.Ordinal)");
        adminTextLocalizerSource.Should().Contain("return AdminCultureCatalog.LanguageOptions;");
        adminTextLocalizerSource.Should().Contain("if (httpContext.Items.TryGetValue(typeof(AdminTextOverrideCatalog), out var cached) &&");
        adminTextLocalizerSource.Should().Contain("var settings = _siteSettingCache.GetAsync().GetAwaiter().GetResult();");
        adminTextLocalizerSource.Should().Contain("var parsedOverrides = AdminTextOverrideCatalog.Parse(settings.AdminTextOverridesJson);");
        adminTextLocalizerSource.Should().Contain("const string cacheKey = \"AdminTextLocalizer.BusinessOverrides\";");
        adminTextLocalizerSource.Should().Contain("var businessId = TryResolveCurrentBusinessId(httpContext);");
        adminTextLocalizerSource.Should().Contain("httpContext.Items[cacheKey] = AdminTextOverrideCatalog.Empty;");
        adminTextLocalizerSource.Should().Contain("_db.Set<Business>()");
        adminTextLocalizerSource.Should().Contain(".AsNoTracking()");
        adminTextLocalizerSource.Should().Contain(".Select(x => x.AdminTextOverridesJson)");
        adminTextLocalizerSource.Should().Contain("private static Guid? TryResolveCurrentBusinessId(HttpContext httpContext)");
        adminTextLocalizerSource.Should().Contain("httpContext.Request.RouteValues[\"businessId\"]");
        adminTextLocalizerSource.Should().Contain("httpContext.Request.Query[\"businessId\"].ToString()");
        adminTextLocalizerSource.Should().Contain("httpContext.Request.HasFormContentType");
        adminTextLocalizerSource.Should().Contain("httpContext.Request.Form[\"BusinessId\"].ToString()");
        adminTextLocalizerSource.Should().Contain("return Guid.TryParse(value, out id) && id != Guid.Empty;");

        overrideCatalogSource.Should().Contain("public static class AdminTextOverrideCatalog");
        overrideCatalogSource.Should().Contain("public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Parse(string? json)");
        overrideCatalogSource.Should().Contain("if (string.IsNullOrWhiteSpace(json))");
        overrideCatalogSource.Should().Contain("return Empty;");
        overrideCatalogSource.Should().Contain("JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);");
        overrideCatalogSource.Should().Contain("var normalized = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);");
        overrideCatalogSource.Should().Contain("var normalizedCulture = AdminCultureCatalog.NormalizeUiCulture(culture);");
        overrideCatalogSource.Should().Contain(".Where(static kvp => !string.IsNullOrWhiteSpace(kvp.Key) && !string.IsNullOrWhiteSpace(kvp.Value))");
        overrideCatalogSource.Should().Contain(".ToDictionary(kvp => kvp.Key.Trim(), kvp => kvp.Value.Trim(), StringComparer.OrdinalIgnoreCase);");
        overrideCatalogSource.Should().Contain("catch (JsonException)");
        overrideCatalogSource.Should().Contain("public static bool TryResolve(");
        overrideCatalogSource.Should().Contain("var normalizedCulture = AdminCultureCatalog.NormalizeUiCulture(culture);");
        overrideCatalogSource.Should().Contain("if (!entries.TryGetValue(key, out var resolvedValue) || string.IsNullOrWhiteSpace(resolvedValue))");
        overrideCatalogSource.Should().Contain("public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Empty =");
    }


    [Fact]
    public void LocalizationMetadataProviders_Should_KeepDisplayNameLocalizationContractsWired()
    {
        var sharedDisplayMetadataProviderSource = ReadWebAdminFile(Path.Combine("Localization", "SharedDisplayMetadataProvider.cs"));
        var configureDisplayMetadataLocalizationSource = ReadWebAdminFile(Path.Combine("Localization", "ConfigureDisplayMetadataLocalization.cs"));

        sharedDisplayMetadataProviderSource.Should().Contain("public sealed class SharedDisplayMetadataProvider : IDisplayMetadataProvider");
        sharedDisplayMetadataProviderSource.Should().Contain("private readonly IStringLocalizer<SharedResource> _localizer;");
        sharedDisplayMetadataProviderSource.Should().Contain("var displayAttribute = context.Attributes.OfType<DisplayAttribute>().FirstOrDefault();");
        sharedDisplayMetadataProviderSource.Should().Contain("if (displayAttribute is null || string.IsNullOrWhiteSpace(displayAttribute.Name))");
        sharedDisplayMetadataProviderSource.Should().Contain("var resourceKey = displayAttribute.Name!;");
        sharedDisplayMetadataProviderSource.Should().Contain("context.DisplayMetadata.DisplayName = () => _localizer[resourceKey];");

        configureDisplayMetadataLocalizationSource.Should().Contain("public sealed class ConfigureDisplayMetadataLocalization : IConfigureOptions<MvcOptions>");
        configureDisplayMetadataLocalizationSource.Should().Contain("private readonly IDisplayMetadataProvider _displayMetadataProvider;");
        configureDisplayMetadataLocalizationSource.Should().Contain("public void Configure(MvcOptions options)");
        configureDisplayMetadataLocalizationSource.Should().Contain("options.ModelMetadataDetailsProviders.Add(_displayMetadataProvider);");
    }


    [Fact]
    public void SharedSettingsAndPermissionHelpers_Should_KeepCacheMappingAndPermissionBypassContractsWired()
    {
        var siteSettingCacheSource = ReadWebAdminFile(Path.Combine("Services", "Settings", "SiteSettingCache.cs"));
        var permissionRazorHelperSource = ReadWebAdminFile(Path.Combine("Infrastructure", "PermissionRazorHelper.cs"));

        siteSettingCacheSource.Should().Contain("public sealed class SiteSettingCache : ISiteSettingCache");
        siteSettingCacheSource.Should().Contain("private const string CacheKey = \"SiteSettingCache:Current\";");
        siteSettingCacheSource.Should().Contain("if (_cache.TryGetValue(CacheKey, out SiteSettingDto? cached) && cached is not null)");
        siteSettingCacheSource.Should().Contain("var entity = await _db.Set<SiteSetting>()");
        siteSettingCacheSource.Should().Contain(".AsNoTracking()");
        siteSettingCacheSource.Should().Contain(".SingleAsync(ct)");
        siteSettingCacheSource.Should().Contain("var dto = Map(entity);");
        siteSettingCacheSource.Should().Contain("_cache.Set(CacheKey, dto, new MemoryCacheEntryOptions");
        siteSettingCacheSource.Should().Contain("SlidingExpiration = TimeSpan.FromMinutes(10),");
        siteSettingCacheSource.Should().Contain("Priority = CacheItemPriority.Normal");
        siteSettingCacheSource.Should().Contain("public void Invalidate()");
        siteSettingCacheSource.Should().Contain("_cache.Remove(CacheKey);");
        siteSettingCacheSource.Should().Contain("private static SiteSettingDto Map(SiteSetting s)");
        siteSettingCacheSource.Should().Contain("RowVersion = s.RowVersion ?? Array.Empty<byte>(),");
        siteSettingCacheSource.Should().Contain("DefaultCulture = string.IsNullOrWhiteSpace(s.DefaultCulture) ? AdminCultureCatalog.DefaultCulture : AdminCultureCatalog.NormalizeUiCulture(s.DefaultCulture),");
        siteSettingCacheSource.Should().Contain("SupportedCulturesCsv = string.IsNullOrWhiteSpace(s.SupportedCulturesCsv) ? AdminCultureCatalog.SupportedCulturesCsvDefault : string.Join(\",\", AdminCultureCatalog.NormalizeSupportedCultureNames(s.SupportedCulturesCsv)),");
        siteSettingCacheSource.Should().Contain("DefaultCountry = string.IsNullOrWhiteSpace(s.DefaultCountry) ? SiteSettingDto.DefaultCountryDefault : s.DefaultCountry,");
        siteSettingCacheSource.Should().Contain("DefaultCurrency = string.IsNullOrWhiteSpace(s.DefaultCurrency) ? SiteSettingDto.DefaultCurrencyDefault : s.DefaultCurrency,");
        siteSettingCacheSource.Should().Contain("TimeZone = string.IsNullOrWhiteSpace(s.TimeZone) ? SiteSettingDto.TimeZoneDefault : s.TimeZone,");
        siteSettingCacheSource.Should().Contain("JwtIssuer = string.IsNullOrWhiteSpace(s.JwtIssuer) ? \"Darwin\" : s.JwtIssuer,");
        siteSettingCacheSource.Should().Contain("JwtAudience = string.IsNullOrWhiteSpace(s.JwtAudience) ? \"Darwin.PublicApi\" : s.JwtAudience,");
        siteSettingCacheSource.Should().Contain("MeasurementSystem = string.IsNullOrWhiteSpace(s.MeasurementSystem) ? \"Metric\" : s.MeasurementSystem,");
        siteSettingCacheSource.Should().Contain("WebAuthnRelyingPartyId = string.IsNullOrWhiteSpace(s.WebAuthnRelyingPartyId) ? \"localhost\" : s.WebAuthnRelyingPartyId,");
        siteSettingCacheSource.Should().Contain("WebAuthnAllowedOriginsCsv = string.IsNullOrWhiteSpace(s.WebAuthnAllowedOriginsCsv) ? \"https://localhost:5001\" : s.WebAuthnAllowedOriginsCsv,");
        siteSettingCacheSource.Should().Contain("HomeSlug = string.IsNullOrWhiteSpace(s.HomeSlug) ? SiteSettingDto.HomeSlugDefault : s.HomeSlug");

        permissionRazorHelperSource.Should().Contain("public sealed class PermissionRazorHelper");
        permissionRazorHelperSource.Should().Contain("var http = _httpContextAccessor.HttpContext;");
        permissionRazorHelperSource.Should().Contain("var user = http?.User;");
        permissionRazorHelperSource.Should().Contain("if (user?.Identity is null || !user.Identity.IsAuthenticated)");
        permissionRazorHelperSource.Should().Contain("var idValue = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;");
        permissionRazorHelperSource.Should().Contain("if (!Guid.TryParse(idValue, out var userId))");
        permissionRazorHelperSource.Should().Contain("if (await _permissions.HasAsync(userId, \"FullAdminAccess\", ct)) return true;");
        permissionRazorHelperSource.Should().Contain("return await _permissions.HasAsync(userId, permissionKey, ct);");
    }


    [Fact]
    public void SharedReferenceAndSeoHelpers_Should_KeepLookupSelectionAndCanonicalUrlContractsWired()
    {
        var adminReferenceDataServiceSource = ReadWebAdminFile(Path.Combine("Services", "Admin", "AdminReferenceDataService.cs"));
        var canonicalUrlServiceSource = ReadWebAdminFile(Path.Combine("Services", "Seo", "CanonicalUrlService.cs"));
        var canonicalUrlInterfaceSource = ReadWebAdminFile(Path.Combine("Services", "Seo", "ICanonicalUrlService.cs"));

        adminReferenceDataServiceSource.Should().Contain("public sealed class AdminReferenceDataService");
        adminReferenceDataServiceSource.Should().Contain("public async Task<Guid?> ResolveBusinessIdAsync(Guid? requestedBusinessId, CancellationToken ct = default)");
        adminReferenceDataServiceSource.Should().Contain("var items = await _getBusinesses.HandleAsync(ct).ConfigureAwait(false);");
        adminReferenceDataServiceSource.Should().Contain("return ResolveSelectedId(items.Select(x => x.Id).ToList(), requestedBusinessId);");
        adminReferenceDataServiceSource.Should().Contain("public async Task<Guid?> ResolveWarehouseIdAsync(Guid? requestedWarehouseId, Guid? businessId = null, CancellationToken ct = default)");
        adminReferenceDataServiceSource.Should().Contain("if (businessId.HasValue)");
        adminReferenceDataServiceSource.Should().Contain("items = items.Where(x => x.BusinessId == businessId.Value).ToList();");
        adminReferenceDataServiceSource.Should().Contain("public async Task<List<SelectListItem>> GetBusinessOptionsAsync(Guid? selectedBusinessId, CancellationToken ct = default)");
        adminReferenceDataServiceSource.Should().Contain("x.SecondaryLabel is null ? x.Label : $\"{x.Label} ({x.SecondaryLabel})\"");
        adminReferenceDataServiceSource.Should().Contain("public async Task<List<SelectListItem>> GetUserOptionsAsync(Guid? selectedUserId, bool includeEmpty = true, CancellationToken ct = default)");
        adminReferenceDataServiceSource.Should().Contain("return BuildOptions(items, selectedUserId, includeEmpty, \"Unassigned\");");
        adminReferenceDataServiceSource.Should().Contain("public async Task<List<SelectListItem>> GetCustomerOptionsAsync(Guid? selectedCustomerId, bool includeEmpty = false, CancellationToken ct = default)");
        adminReferenceDataServiceSource.Should().Contain("return BuildOptions(items, selectedCustomerId, includeEmpty, \"Select customer\");");
        adminReferenceDataServiceSource.Should().Contain("public async Task<List<SelectListItem>> GetSupplierOptionsAsync(Guid businessId, Guid? selectedSupplierId, bool includeEmpty = false, CancellationToken ct = default)");
        adminReferenceDataServiceSource.Should().Contain("var items = await _getSuppliers.HandleAsync(businessId, ct).ConfigureAwait(false);");
        adminReferenceDataServiceSource.Should().Contain("private static Guid? ResolveSelectedId(IReadOnlyCollection<Guid> availableIds, Guid? requestedId)");
        adminReferenceDataServiceSource.Should().Contain("if (availableIds.Count == 0)");
        adminReferenceDataServiceSource.Should().Contain("if (requestedId.HasValue && availableIds.Contains(requestedId.Value))");
        adminReferenceDataServiceSource.Should().Contain("return availableIds.First();");
        adminReferenceDataServiceSource.Should().Contain("private static List<SelectListItem> BuildOptions(");
        adminReferenceDataServiceSource.Should().Contain("options.Add(new SelectListItem(emptyLabel, string.Empty, !selectedId.HasValue));");
        adminReferenceDataServiceSource.Should().Contain("options.AddRange(items.Select(x => new SelectListItem(");

        canonicalUrlInterfaceSource.Should().Contain("public interface ICanonicalUrlService");
        canonicalUrlInterfaceSource.Should().Contain("string Page(string culture, string slug);");
        canonicalUrlInterfaceSource.Should().Contain("string Category(string culture, string slug);");
        canonicalUrlInterfaceSource.Should().Contain("string Product(string culture, string slug);");
        canonicalUrlInterfaceSource.Should().Contain("string Absolute(string relative);");

        canonicalUrlServiceSource.Should().Contain("public sealed class CanonicalUrlService : ICanonicalUrlService");
        canonicalUrlServiceSource.Should().Contain("public string Page(string culture, string slug) => $\"/{culture}/page/{slug}\";");
        canonicalUrlServiceSource.Should().Contain("public string Category(string culture, string slug) => $\"/{culture}/c/{slug}\";");
        canonicalUrlServiceSource.Should().Contain("public string Product(string culture, string slug) => $\"/{culture}/p/{slug}\";");
        canonicalUrlServiceSource.Should().Contain("var req = _http.HttpContext?.Request;");
        canonicalUrlServiceSource.Should().Contain("if (req == null) return relative;");
        canonicalUrlServiceSource.Should().Contain("var uri = UriHelper.BuildAbsolute(req.Scheme, req.Host, req.PathBase, relative);");
        canonicalUrlServiceSource.Should().Contain("return uri;");
    }


    [Fact]
    public void AdminReferenceDataService_Should_KeepRemainingLookupOptionBuilderContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Services", "Admin", "AdminReferenceDataService.cs"));

        source.Should().Contain("private readonly GetCustomerSegmentLookupHandler _getCustomerSegments;");
        source.Should().Contain("private readonly GetProductVariantLookupHandler _getVariants;");
        source.Should().Contain("private readonly GetFinancialAccountLookupHandler _getAccounts;");
        source.Should().Contain("private readonly GetPaymentLookupHandler _getPayments;");
        source.Should().Contain("_getCustomerSegments = getCustomerSegments ?? throw new ArgumentNullException(nameof(getCustomerSegments));");
        source.Should().Contain("_getVariants = getVariants ?? throw new ArgumentNullException(nameof(getVariants));");
        source.Should().Contain("_getAccounts = getAccounts ?? throw new ArgumentNullException(nameof(getAccounts));");
        source.Should().Contain("_getPayments = getPayments ?? throw new ArgumentNullException(nameof(getPayments));");
        source.Should().Contain("public async Task<List<SelectListItem>> GetCustomerSegmentOptionsAsync(Guid? selectedSegmentId, bool includeEmpty = false, CancellationToken ct = default)");
        source.Should().Contain("return BuildOptions(items, selectedSegmentId, includeEmpty, \"Select segment\");");
        source.Should().Contain("public async Task<List<SelectListItem>> GetVariantOptionsAsync(Guid? selectedVariantId, CancellationToken ct = default)");
        source.Should().Contain("return BuildOptions(items, selectedVariantId, false, \"Select variant\");");
        source.Should().Contain("public async Task<List<SelectListItem>> GetFinancialAccountOptionsAsync(Guid businessId, Guid? selectedAccountId, bool includeEmpty = false, CancellationToken ct = default)");
        source.Should().Contain("var items = await _getAccounts.HandleAsync(businessId, ct).ConfigureAwait(false);");
        source.Should().Contain("return BuildOptions(items, selectedAccountId, includeEmpty, \"Select account\");");
        source.Should().Contain("public async Task<List<SelectListItem>> GetPaymentOptionsAsync(Guid? selectedPaymentId, bool includeEmpty = false, CancellationToken ct = default)");
        source.Should().Contain("var items = await _getPayments.HandleAsync(ct).ConfigureAwait(false);");
        source.Should().Contain("return BuildOptions(items, selectedPaymentId, includeEmpty, \"Select payment\");");
        source.Should().Contain("string.IsNullOrWhiteSpace(x.Location) ? x.Name : $\"{x.Name} ({x.Location})\"");
        source.Should().Contain("x.SecondaryLabel is null ? x.Label : $\"{x.Label} ({x.SecondaryLabel})\"");
    }


    [Fact]
    public void PermissionAuthorizationPrimitives_Should_KeepConstructorFallbackAndPolicyNameContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Auth", "PermissionAuthorization.cs"));

        source.Should().Contain("public PermissionRequirement(string permissionKey)");
        source.Should().Contain("PermissionKey = !string.IsNullOrWhiteSpace(permissionKey)");
        source.Should().Contain(": throw new ArgumentNullException(nameof(permissionKey));");
        source.Should().Contain("public string PermissionKey { get; }");
        source.Should().Contain("private readonly DefaultAuthorizationPolicyProvider _fallback;");
        source.Should().Contain("_fallback = new DefaultAuthorizationPolicyProvider(options);");
        source.Should().Contain("public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();");
        source.Should().Contain("public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();");
        source.Should().Contain("return _fallback.GetPolicyAsync(policyName);");
        source.Should().Contain("public HasPermissionAttribute(string permissionKey)");
        source.Should().Contain("Policy = $\"perm:{permissionKey}\";");
        source.Should().Contain("[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]");
    }


    [Fact]
    public void AdminBaseController_Should_KeepLocalizedTempDataMessageHelperContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "AdminBaseController.cs"));

        source.Should().Contain("[PermissionAuthorize(\"AccessAdminPanel\")]");
        source.Should().Contain("protected string T(string key)");
        source.Should().Contain("return HttpContext.RequestServices.GetRequiredService<IAdminTextLocalizer>().T(key);");
        source.Should().Contain("protected void SetSuccessMessage(string key)");
        source.Should().Contain("TempData[\"Success\"] = T(key);");
        source.Should().Contain("protected void SetErrorMessage(string key)");
        source.Should().Contain("TempData[\"Error\"] = T(key);");
        source.Should().Contain("protected void SetWarningMessage(string key)");
        source.Should().Contain("TempData[\"Warning\"] = T(key);");
    }


    [Fact]
    public void DisplayMetadataLocalizationPrimitives_Should_KeepProviderInjectionAndDisplayNameContractsWired()
    {
        var providerSource = ReadWebAdminFile(Path.Combine("Localization", "SharedDisplayMetadataProvider.cs"));
        var optionsSource = ReadWebAdminFile(Path.Combine("Localization", "ConfigureDisplayMetadataLocalization.cs"));

        providerSource.Should().Contain("public sealed class SharedDisplayMetadataProvider : IDisplayMetadataProvider");
        providerSource.Should().Contain("private readonly IStringLocalizer<SharedResource> _localizer;");
        providerSource.Should().Contain("public SharedDisplayMetadataProvider(IStringLocalizer<SharedResource> localizer)");
        providerSource.Should().Contain("_localizer = localizer;");
        providerSource.Should().Contain("public void CreateDisplayMetadata(DisplayMetadataProviderContext context)");
        providerSource.Should().Contain("var displayAttribute = context.Attributes.OfType<DisplayAttribute>().FirstOrDefault();");
        providerSource.Should().Contain("if (displayAttribute is null || string.IsNullOrWhiteSpace(displayAttribute.Name))");
        providerSource.Should().Contain("var resourceKey = displayAttribute.Name!;");
        providerSource.Should().Contain("context.DisplayMetadata.DisplayName = () => _localizer[resourceKey];");

        optionsSource.Should().Contain("public sealed class ConfigureDisplayMetadataLocalization : IConfigureOptions<MvcOptions>");
        optionsSource.Should().Contain("private readonly IDisplayMetadataProvider _displayMetadataProvider;");
        optionsSource.Should().Contain("public ConfigureDisplayMetadataLocalization(IDisplayMetadataProvider displayMetadataProvider)");
        optionsSource.Should().Contain("_displayMetadataProvider = displayMetadataProvider;");
        optionsSource.Should().Contain("public void Configure(MvcOptions options)");
        optionsSource.Should().Contain("options.ModelMetadataDetailsProviders.Add(_displayMetadataProvider);");
    }


    [Fact]
    public void SharedTagHelpers_Should_KeepActiveNavRouteMatchingAndPagerNavigationContractsWired()
    {
        var activeNavSource = ReadWebAdminFile(Path.Combine("TagHelpers", "ActiveNavLinkTagHelper.cs"));
        var pagerSource = ReadWebAdminFile(Path.Combine("TagHelpers", "PagerTagHelper.cs"));

        activeNavSource.Should().Contain("private bool IsActiveRoute()");
        activeNavSource.Should().Contain("var currentArea = (string?)ViewContext.RouteData.Values[\"area\"];");
        activeNavSource.Should().Contain("var currentController = (string?)ViewContext.RouteData.Values[\"controller\"];");
        activeNavSource.Should().Contain("var currentAction = (string?)ViewContext.RouteData.Values[\"action\"];");
        activeNavSource.Should().Contain("var wantArea = Normalize(Area);");
        activeNavSource.Should().Contain("var wantController = Normalize(Controller);");
        activeNavSource.Should().Contain("var wantAction = Normalize(Action ?? \"Index\");");
        activeNavSource.Should().Contain("if (curController == wantController && curAction == wantAction)");
        activeNavSource.Should().Contain("if (!string.IsNullOrEmpty(wantArea))");
        activeNavSource.Should().Contain("if (string.IsNullOrEmpty(Action) && curController == wantController)");
        activeNavSource.Should().Contain("return false;");

        pagerSource.Should().Contain("string BuildUrl(int targetPage, int? targetPageSize = null)");
        pagerSource.Should().Contain("[\"page\"] = targetPage,");
        pagerSource.Should().Contain("[\"pageSize\"] = targetPageSize ?? pageSize");
        pagerSource.Should().Contain("PageItem(\"\u00AB First\", 1, page == 1, aria: \"First\");");
        pagerSource.Should().Contain("PageItem(\"\u2039 Prev\", Math.Max(1, page - 1), page == 1, aria: \"Previous\");");
        pagerSource.Should().Contain("for (int i = start; i <= end; i++)");
        pagerSource.Should().Contain("PageItem(i.ToString(), i, disabled: false, active: i == page, aria: i == page ? \"Current\" : null);");
        pagerSource.Should().Contain("PageItem(\"Next \u203A\", Math.Min(totalPagesSafe, page + 1), page == totalPagesSafe, aria: \"Next\");");
        pagerSource.Should().Contain("PageItem(\"Last \u00BB\", totalPagesSafe, page == totalPagesSafe, aria: \"Last\");");
    }


    [Fact]
    public void WebAdminAuthorizationInfrastructure_Should_KeepPermissionAttributeAndCurrentUserContractsWired()
    {
        var permissionAuthorizeSource = ReadWebAdminFile(Path.Combine("Security", "PermissionAuthorizeAttribute.cs"));
        var currentUserServiceSource = ReadWebAdminFile(Path.Combine("Auth", "CurrentUserService.cs"));
        var adminBaseControllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "AdminBaseController.cs"));
        var dependencyInjectionSource = ReadWebAdminFile(Path.Combine("Extensions", "DependencyInjection.cs"));

        permissionAuthorizeSource.Should().Contain("public sealed class PermissionAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter");
        permissionAuthorizeSource.Should().Contain("private const string FullAdminAccess = \"FullAdminAccess\";");
        permissionAuthorizeSource.Should().Contain("if (string.IsNullOrWhiteSpace(permissionKey))");
        permissionAuthorizeSource.Should().Contain("throw new ArgumentException(\"Permission key must be provided.\", nameof(permissionKey));");
        permissionAuthorizeSource.Should().Contain("public async Task OnAuthorizationAsync(AuthorizationFilterContext context)");
        permissionAuthorizeSource.Should().Contain("if (context is null) throw new ArgumentNullException(nameof(context));");
        permissionAuthorizeSource.Should().Contain("foreach (var filter in context.Filters)");
        permissionAuthorizeSource.Should().Contain("if (filter is IAllowAnonymousFilter)");
        permissionAuthorizeSource.Should().Contain("if (user?.Identity is null || !user.Identity.IsAuthenticated)");
        permissionAuthorizeSource.Should().Contain("context.Result = new ChallengeResult();");
        permissionAuthorizeSource.Should().Contain("var idValue = user.FindFirstValue(ClaimTypes.NameIdentifier);");
        permissionAuthorizeSource.Should().Contain("if (!Guid.TryParse(idValue, out var userId))");
        permissionAuthorizeSource.Should().Contain("context.Result = new ForbidResult();");
        permissionAuthorizeSource.Should().Contain("var permissions = (IPermissionService?)http.RequestServices.GetService(typeof(IPermissionService));");
        permissionAuthorizeSource.Should().Contain("context.Result = new StatusCodeResult(500);");
        permissionAuthorizeSource.Should().Contain("if (await permissions.HasAsync(userId, FullAdminAccess, http.RequestAborted))");
        permissionAuthorizeSource.Should().Contain("var allowed = await permissions.HasAsync(userId, _requiredPermissionKey, http.RequestAborted);");

        currentUserServiceSource.Should().Contain("public sealed class CurrentUserService : ICurrentUserService");
        currentUserServiceSource.Should().Contain("var user = _http.HttpContext?.User;");
        currentUserServiceSource.Should().Contain("if (user?.Identity?.IsAuthenticated == true)");
        currentUserServiceSource.Should().Contain("var id = user.FindFirstValue(ClaimTypes.NameIdentifier)");
        currentUserServiceSource.Should().Contain("?? user.FindFirstValue(\"sub\")");
        currentUserServiceSource.Should().Contain("?? user.FindFirstValue(\"uid\");");
        currentUserServiceSource.Should().Contain("if (Guid.TryParse(id, out var guid))");
        currentUserServiceSource.Should().Contain("return WellKnownIds.AdministratorUserId;");

        adminBaseControllerSource.Should().Contain("[PermissionAuthorize(\"AccessAdminPanel\")]");
        adminBaseControllerSource.Should().Contain("return HttpContext.RequestServices.GetRequiredService<IAdminTextLocalizer>().T(key);");
        adminBaseControllerSource.Should().Contain("TempData[\"Success\"] = T(key);");
        adminBaseControllerSource.Should().Contain("TempData[\"Error\"] = T(key);");
        adminBaseControllerSource.Should().Contain("TempData[\"Warning\"] = T(key);");

        dependencyInjectionSource.Should().Contain("services.AddScoped<ICurrentUserService, CurrentUserService>();");
    }


    [Fact]
    public void WebAdminDynamicPermissionPolicyInfrastructure_Should_KeepRequirementProviderAndHandlerContractsDefined()
    {
        var source = ReadWebAdminFile(Path.Combine("Auth", "PermissionAuthorization.cs"));

        source.Should().Contain("public sealed class PermissionRequirement : IAuthorizationRequirement");
        source.Should().Contain("PermissionKey = !string.IsNullOrWhiteSpace(permissionKey)");
        source.Should().Contain("throw new ArgumentNullException(nameof(permissionKey));");
        source.Should().Contain("public string PermissionKey { get; }");

        source.Should().Contain("public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider");
        source.Should().Contain("_fallback = new DefaultAuthorizationPolicyProvider(options);");
        source.Should().Contain("public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();");
        source.Should().Contain("public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();");
        source.Should().Contain("if (!string.IsNullOrWhiteSpace(policyName) &&");
        source.Should().Contain("policyName.StartsWith(\"perm:\", StringComparison.OrdinalIgnoreCase))");
        source.Should().Contain("var key = policyName.Substring(\"perm:\".Length);");
        source.Should().Contain("var policy = new AuthorizationPolicyBuilder()");
        source.Should().Contain(".AddRequirements(new PermissionRequirement(key))");
        source.Should().Contain(".Build();");
        source.Should().Contain("return _fallback.GetPolicyAsync(policyName);");

        source.Should().Contain("public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>");
        source.Should().Contain("var sub = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;");
        source.Should().Contain("if (!Guid.TryParse(sub, out var userId))");
        source.Should().Contain("if (await _permissions.HasAsync(userId, \"FullAdminAccess\", CancellationToken.None))");
        source.Should().Contain("context.Succeed(requirement);");
        source.Should().Contain("if (await _permissions.HasAsync(userId, requirement.PermissionKey, CancellationToken.None))");

        source.Should().Contain("public sealed class HasPermissionAttribute : AuthorizeAttribute");
        source.Should().Contain("Policy = $\"perm:{permissionKey}\";");
    }


    [Fact]
    public void WebAdminPermissionKeyCatalog_Should_KeepCanonicalAdminPermissionNamesCentralized()
    {
        var source = ReadWebAdminFile(Path.Combine("Security", "PermissionKeys.cs"));

        source.Should().Contain("public static class PermissionKeys");
        source.Should().Contain("public const string FullAdminAccess = \"FullAdminAccess\";");
        source.Should().Contain("public const string AccessMemberArea = \"AccessMemberArea\";");
        source.Should().Contain("public const string ManageBusinessSupport = \"ManageBusinessSupport\";");
        source.Should().Contain("Must match Domain/seed values.");
    }


    [Fact]
    public void WebAdminSharedResourceBaseline_Should_KeepLocalizationMarkerAndBilingualCoreKeysWired()
    {
        var sharedResourceSource = ReadWebAdminFile("SharedResource.cs");
        var sharedResourceResxSource = ReadWebAdminFile(Path.Combine("Resources", "SharedResource.resx"));
        var germanSharedResourceResxSource = ReadWebAdminFile(Path.Combine("Resources", "SharedResource.de-DE.resx"));

        sharedResourceSource.Should().Contain("public sealed class SharedResource");
        sharedResourceSource.Should().Contain("Marker type for shared ASP.NET Core localization resources.");

        sharedResourceResxSource.Should().Contain("<data name=\"Language\" xml:space=\"preserve\">");
        sharedResourceResxSource.Should().Contain("<value>Language</value>");
        sharedResourceResxSource.Should().Contain("<data name=\"Save\" xml:space=\"preserve\">");
        sharedResourceResxSource.Should().Contain("<value>Save</value>");
        sharedResourceResxSource.Should().Contain("<data name=\"Back\" xml:space=\"preserve\">");
        sharedResourceResxSource.Should().Contain("<value>Back</value>");
        sharedResourceResxSource.Should().Contain("<data name=\"Cancel\" xml:space=\"preserve\">");
        sharedResourceResxSource.Should().Contain("<value>Cancel</value>");
        sharedResourceResxSource.Should().Contain("<data name=\"ChangePassword\" xml:space=\"preserve\">");
        sharedResourceResxSource.Should().Contain("<value>Change Password</value>");
        sharedResourceResxSource.Should().Contain("<data name=\"BillingBadgeShort\" xml:space=\"preserve\">");
        sharedResourceResxSource.Should().Contain("<value>B</value>");
        sharedResourceResxSource.Should().Contain("<data name=\"ShippingBadgeShort\" xml:space=\"preserve\">");
        sharedResourceResxSource.Should().Contain("<value>S</value>");

        germanSharedResourceResxSource.Should().Contain("<data name=\"Language\" xml:space=\"preserve\">");
        germanSharedResourceResxSource.Should().Contain("<value>Sprache</value>");
        germanSharedResourceResxSource.Should().Contain("<data name=\"Save\" xml:space=\"preserve\">");
        germanSharedResourceResxSource.Should().Contain("<value>Speichern</value>");
        germanSharedResourceResxSource.Should().Contain("<data name=\"Back\" xml:space=\"preserve\">");
        germanSharedResourceResxSource.Should().Contain("<value>Zurueck</value>");
        germanSharedResourceResxSource.Should().Contain("<data name=\"Cancel\" xml:space=\"preserve\">");
        germanSharedResourceResxSource.Should().Contain("<value>Abbrechen</value>");
        germanSharedResourceResxSource.Should().Contain("<data name=\"ChangePassword\" xml:space=\"preserve\">");
        germanSharedResourceResxSource.Should().Contain("<value>Passwort aendern</value>");
        germanSharedResourceResxSource.Should().Contain("<data name=\"BillingBadgeShort\" xml:space=\"preserve\">");
        germanSharedResourceResxSource.Should().Contain("<value>R</value>");
        germanSharedResourceResxSource.Should().Contain("<data name=\"ShippingBadgeShort\" xml:space=\"preserve\">");
        germanSharedResourceResxSource.Should().Contain("<value>V</value>");
    }


    [Fact]
    public void WebAdminRuntimeConfig_Should_KeepAppsettingsAndLaunchProfilesContractsWired()
    {
        var appSettingsSource = ReadWebAdminFile("appsettings.json");
        var developmentAppSettingsSource = ReadWebAdminFile("appsettings.Development.json");
        var launchSettingsSource = ReadWebAdminFile(Path.Combine("Properties", "launchSettings.json"));

        appSettingsSource.Should().Contain("\"AllowedHosts\": \"*\"");
        appSettingsSource.Should().Contain("\"Serilog\"");
        appSettingsSource.Should().Contain("\"Serilog.Sinks.Console\"");
        appSettingsSource.Should().Contain("\"Serilog.Sinks.File\"");
        appSettingsSource.Should().Contain("\"rollingInterval\": \"Day\"");
        appSettingsSource.Should().Contain("\"retainedFileCountLimit\": 14");
        appSettingsSource.Should().Contain("\"buffered\": true");
        appSettingsSource.Should().Contain("\"Application\": \"Darwin.WebAdmin\"");
        appSettingsSource.Should().Contain("\"DataProtection\"");
        appSettingsSource.Should().Contain("\"KeysPath\": \"D:\\\\Darwin\\\\_shared_keys\"");
        appSettingsSource.Should().Contain("\"Email\"");
        appSettingsSource.Should().Contain("\"Smtp\"");
        appSettingsSource.Should().Contain("\"Host\": \"smtp.office365.com\"");
        appSettingsSource.Should().Contain("\"EnableSsl\": true");
        appSettingsSource.Should().Contain("\"BusinessOnboarding\"");
        appSettingsSource.Should().Contain("\"InvitationMagicLink\"");
        appSettingsSource.Should().Contain("\"BaseUrl\": \"darwin-business://InvitationAcceptance\"");

        developmentAppSettingsSource.Should().Contain("\"ConnectionStrings\"");
        developmentAppSettingsSource.Should().Contain("\"DefaultConnection\"");
        developmentAppSettingsSource.Should().Contain("\"Authentication\"");
        developmentAppSettingsSource.Should().Contain("\"Google\"");
        developmentAppSettingsSource.Should().Contain("\"Microsoft\"");
        developmentAppSettingsSource.Should().Contain("\"Provider\": \"Mailgun\"");
        developmentAppSettingsSource.Should().Contain("\"Mailgun\"");
        developmentAppSettingsSource.Should().Contain("\"Graph\"");
        developmentAppSettingsSource.Should().Contain("\"Serilog\"");
        developmentAppSettingsSource.Should().Contain("\"Default\": \"Debug\"");
        developmentAppSettingsSource.Should().Contain("\"path\": \"logs/dev-log-.txt\"");
        developmentAppSettingsSource.Should().Contain("\"retainedFileCountLimit\": 7");
        developmentAppSettingsSource.Should().Contain("\"buffered\": false");
        developmentAppSettingsSource.Should().Contain("\"Application\": \"Darwin.WebAdmin (Dev)\"");
        developmentAppSettingsSource.Should().Contain("\"KeysPath\": \"E:\\\\_Projects\\\\Darwin\\\\_shared_keys\"");

        launchSettingsSource.Should().Contain("\"$schema\": \"https://json.schemastore.org/launchsettings.json\"");
        launchSettingsSource.Should().Contain("\"http\"");
        launchSettingsSource.Should().Contain("\"https\"");
        launchSettingsSource.Should().Contain("\"commandName\": \"Project\"");
        launchSettingsSource.Should().Contain("\"dotnetRunMessages\": true");
        launchSettingsSource.Should().Contain("\"launchBrowser\": true");
        launchSettingsSource.Should().Contain("\"applicationUrl\": \"http://localhost:5219\"");
        launchSettingsSource.Should().Contain("\"applicationUrl\": \"https://localhost:7089;http://localhost:5219\"");
        launchSettingsSource.Should().Contain("\"ASPNETCORE_ENVIRONMENT\": \"Development\"");
    }


    [Fact]
    public void WebAdminProjectFile_Should_KeepTargetRuntimePackageAndContentContractsWired()
    {
        var source = ReadWebAdminFile("Darwin.WebAdmin.csproj");

        source.Should().Contain("<Project Sdk=\"Microsoft.NET.Sdk.Web\">");
        source.Should().Contain("<TargetFramework>net10.0</TargetFramework>");
        source.Should().Contain("<Nullable>enable</Nullable>");
        source.Should().Contain("<ImplicitUsings>enable</ImplicitUsings>");
        source.Should().Contain("<Compile Remove=\"logs\\**\" />");
        source.Should().Contain("<Content Remove=\"logs\\**\" />");
        source.Should().Contain("<EmbeddedResource Remove=\"logs\\**\" />");
        source.Should().Contain("<None Remove=\"logs\\**\" />");
        source.Should().Contain("<PackageReference Include=\"AutoMapper\" Version=\"16.1.1\" />");
        source.Should().Contain("<PackageReference Include=\"FluentValidation\" Version=\"12.1.1\" />");
        source.Should().Contain("<PackageReference Include=\"HtmlSanitizer\" Version=\"9.0.892\" />");
        source.Should().Contain("<PackageReference Include=\"Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation\" Version=\"10.0.6\" />");
        source.Should().Contain("<PackageReference Include=\"Microsoft.EntityFrameworkCore.Design\" Version=\"10.0.6\">");
        source.Should().Contain("<PackageReference Include=\"Microsoft.EntityFrameworkCore.Tools\" Version=\"10.0.6\">");
        source.Should().Contain("<PackageReference Include=\"QRCoder\" Version=\"1.8.0\" />");
        source.Should().Contain("<PackageReference Include=\"Serilog\" Version=\"4.3.1\" />");
        source.Should().Contain("<PackageReference Include=\"Serilog.AspNetCore\" Version=\"10.0.0\" />");
        source.Should().Contain("<ProjectReference Include=\"..\\Darwin.Application\\Darwin.Application.csproj\" />");
        source.Should().Contain("<ProjectReference Include=\"..\\Darwin.Infrastructure\\Darwin.Infrastructure.csproj\" />");
        source.Should().Contain("<ProjectReference Include=\"..\\Darwin.Shared\\Darwin.Shared.csproj\" />");
        source.Should().Contain("<Folder Include=\"wwwroot\\js\\\" />");
        source.Should().Contain("<Folder Include=\"wwwroot\\lib\\\" />");
        source.Should().Contain("<Folder Include=\"wwwroot\\uploads\\\" />");
        source.Should().Contain("<None Include=\"wwwroot\\images\\DarwinJustLogo.svg\" />");
    }


    [Fact]
    public void WebAdminStaticShellAssets_Should_KeepManifestAndCssBaselineContractsWired()
    {
        var manifestSource = ReadWebAdminFile(Path.Combine("wwwroot", "site.webmanifest"));
        var adminCssSource = ReadWebAdminFile(Path.Combine("wwwroot", "css", "admin.css"));
        var mainCssSource = ReadWebAdminFile(Path.Combine("wwwroot", "css", "main.css"));

        manifestSource.Should().Contain("\"name\": \"Darwin\"");
        manifestSource.Should().Contain("\"short_name\": \"Darwin\"");
        manifestSource.Should().Contain("\"src\": \"/web-app-manifest-192x192.png\"");
        manifestSource.Should().Contain("\"sizes\": \"192x192\"");
        manifestSource.Should().Contain("\"src\": \"/web-app-manifest-512x512.png\"");
        manifestSource.Should().Contain("\"sizes\": \"512x512\"");
        manifestSource.Should().Contain("\"purpose\": \"maskable\"");
        manifestSource.Should().Contain("\"theme_color\": \"#ffffff\"");
        manifestSource.Should().Contain("\"background_color\": \"#ffffff\"");
        manifestSource.Should().Contain("\"display\": \"standalone\"");

        adminCssSource.Should().Contain("/* Active nav item highlighting for Admin sidebar */");
        adminCssSource.Should().Contain(".list-group-item.active");
        adminCssSource.Should().Contain("font-weight: 600;");
        adminCssSource.Should().Contain("border-left: 3px solid currentColor;");

        mainCssSource.Should().Contain("/* Let the Quill editor container size itself to content so the parent grows */");
        mainCssSource.Should().Contain(".ql-container");
        mainCssSource.Should().Contain("height: auto !important;");
        mainCssSource.Should().Contain("min-height: 230px;");
    }


    [Fact]
    public void LocalizationDefaults_Should_StayCentralizedAcross_Settings_And_AdminCultureCatalog()
    {
        var siteSettingDtoSource = ReadApplicationFile(Path.Combine("Settings", "DTOs", "SiteSettingDto.cs"));
        var domainDefaultsSource = ReadDomainFile(Path.Combine("Common", "DomainDefaults.cs"));
        var adminCultureCatalogSource = ReadWebAdminFile(Path.Combine("Localization", "AdminCultureCatalog.cs"));

        domainDefaultsSource.Should().Contain("public const string DefaultCulture = \"de-DE\";");
        domainDefaultsSource.Should().Contain("public const string DefaultTimezone = \"Europe/Berlin\";");
        domainDefaultsSource.Should().Contain("public const string DefaultCurrency = \"EUR\";");
        domainDefaultsSource.Should().Contain("public const string DefaultCountryCode = \"DE\";");
        domainDefaultsSource.Should().Contain("public const string SupportedCulturesCsv = \"de-DE,en-US\";");

        siteSettingDtoSource.Should().Contain("using Darwin.Domain.Common;");
        siteSettingDtoSource.Should().Contain("public const string DefaultCultureDefault = DomainDefaults.DefaultCulture;");
        siteSettingDtoSource.Should().Contain("public const string SupportedCulturesCsvDefault = DomainDefaults.SupportedCulturesCsv;");
        siteSettingDtoSource.Should().Contain("public const string DefaultCountryDefault = DomainDefaults.DefaultCountryCode;");
        siteSettingDtoSource.Should().Contain("public const string DefaultCurrencyDefault = DomainDefaults.DefaultCurrency;");
        siteSettingDtoSource.Should().Contain("public const string TimeZoneDefault = DomainDefaults.DefaultTimezone;");

        adminCultureCatalogSource.Should().Contain("using Darwin.Application.Settings.DTOs;");
        adminCultureCatalogSource.Should().Contain("public const string German = SiteSettingDto.DefaultCultureDefault;");
        adminCultureCatalogSource.Should().Contain("public const string SupportedCulturesCsvDefault = SiteSettingDto.SupportedCulturesCsvDefault;");
        adminCultureCatalogSource.Should().Contain("return DefaultCulture;");
    }


    [Fact]
    public void OrderDetailsAndActionShells_Should_KeepTabsGridsAndCreateContractsWired()
    {
        var detailsSource = ReadWebAdminFile(Path.Combine("Views", "Orders", "Details.cshtml"));
        var addPaymentSource = ReadWebAdminFile(Path.Combine("Views", "Orders", "AddPayment.cshtml"));
        var addShipmentSource = ReadWebAdminFile(Path.Combine("Views", "Orders", "AddShipment.cshtml"));
        var addRefundSource = ReadWebAdminFile(Path.Combine("Views", "Orders", "AddRefund.cshtml"));
        var createInvoiceSource = ReadWebAdminFile(Path.Combine("Views", "Orders", "CreateInvoice.cshtml"));
        var paymentShellSource = ReadWebAdminFile(Path.Combine("Views", "Orders", "_PaymentCreateShell.cshtml"));
        var shipmentShellSource = ReadWebAdminFile(Path.Combine("Views", "Orders", "_ShipmentCreateShell.cshtml"));
        var refundShellSource = ReadWebAdminFile(Path.Combine("Views", "Orders", "_RefundCreateShell.cshtml"));
        var invoiceShellSource = ReadWebAdminFile(Path.Combine("Views", "Orders", "_InvoiceCreateShell.cshtml"));

        detailsSource.Should().Contain("id=\"order-details-workspace-shell\"");
        detailsSource.Should().Contain("@T.T(\"BackToOrders\")");
        detailsSource.Should().Contain("hx-post=\"@Url.Action(\"ChangeStatus\", \"Orders\")\"");
        detailsSource.Should().Contain("name=\"RowVersion\" value=\"@Convert.ToBase64String(Model.RowVersion)\"");
        detailsSource.Should().Contain("name=\"WarehouseId\"");
        detailsSource.Should().Contain("name=\"NewStatus\"");
        detailsSource.Should().Contain("string AddPaymentUrl(Guid orderId) => Url.Action(\"AddPayment\", \"Orders\", new { orderId }) ?? string.Empty;");
        detailsSource.Should().Contain("string AddShipmentUrl(Guid orderId) => Url.Action(\"AddShipment\", \"Orders\", new { orderId }) ?? string.Empty;");
        detailsSource.Should().Contain("string AddRefundUrl(Guid orderId, Guid? paymentId = null) => Url.Action(\"AddRefund\", \"Orders\", new { orderId, paymentId }) ?? string.Empty;");
        detailsSource.Should().Contain("string CreateInvoiceUrl(Guid orderId) => Url.Action(\"CreateInvoice\", \"Orders\", new { orderId }) ?? string.Empty;");
        detailsSource.Should().Contain("hx-get=\"@AddPaymentUrl(Model.Id)\"");
        detailsSource.Should().Contain("hx-get=\"@AddShipmentUrl(Model.Id)\"");
        detailsSource.Should().Contain("hx-get=\"@AddRefundUrl(Model.Id)\"");
        detailsSource.Should().Contain("hx-get=\"@CreateInvoiceUrl(Model.Id)\"");
        detailsSource.Should().Contain("id=\"order-operation-shell\"");
        detailsSource.Should().Contain("@T.T(\"TaxPriceSnapshot\")");
        detailsSource.Should().Contain("@T.T(\"CurrentTaxPolicy\")");
        detailsSource.Should().Contain("@T.T(\"ReturnSupportBaseline\")");
        detailsSource.Should().Contain("@T.T(\"InventoryAllocation\")");
        detailsSource.Should().Contain("@T.T(\"InventoryAllocationSubtitle\")");
        detailsSource.Should().Contain("@T.T(\"AllocateNow\")");
        detailsSource.Should().Contain("data-bs-target=\"#payments\"");
        detailsSource.Should().Contain("data-bs-target=\"#shipments\"");
        detailsSource.Should().Contain("data-bs-target=\"#refunds\"");
        detailsSource.Should().Contain("data-bs-target=\"#invoices\"");
        detailsSource.Should().Contain("string OrderPaymentsUrl(Guid orderId) => Url.Action(\"Payments\", \"Orders\", new { orderId }) ?? string.Empty;");
        detailsSource.Should().Contain("string OrderShipmentsUrl(Guid orderId) => Url.Action(\"Shipments\", \"Orders\", new { orderId }) ?? string.Empty;");
        detailsSource.Should().Contain("string OrderRefundsUrl(Guid orderId) => Url.Action(\"Refunds\", \"Orders\", new { orderId }) ?? string.Empty;");
        detailsSource.Should().Contain("string OrderInvoicesUrl(Guid orderId) => Url.Action(\"Invoices\", \"Orders\", new { orderId }) ?? string.Empty;");
        detailsSource.Should().Contain("hx-get=\"@OrderPaymentsUrl(Model.Id)\"");
        detailsSource.Should().Contain("hx-get=\"@OrderShipmentsUrl(Model.Id)\"");
        detailsSource.Should().Contain("hx-get=\"@OrderRefundsUrl(Model.Id)\"");
        detailsSource.Should().Contain("hx-get=\"@OrderInvoicesUrl(Model.Id)\"");

        addPaymentSource.Should().Contain("<partial name=\"~/Views/Orders/_PaymentCreateShell.cshtml\" model=\"Model\" />");
        addPaymentSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");
        addShipmentSource.Should().Contain("<partial name=\"~/Views/Orders/_ShipmentCreateShell.cshtml\" model=\"Model\" />");
        addShipmentSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");
        addRefundSource.Should().Contain("<partial name=\"~/Views/Orders/_RefundCreateShell.cshtml\" model=\"Model\" />");
        addRefundSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");
        createInvoiceSource.Should().Contain("<partial name=\"~/Views/Orders/_InvoiceCreateShell.cshtml\" model=\"Model\" />");
        createInvoiceSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");

        paymentShellSource.Should().Contain("id=\"order-payment-shell\"");
        paymentShellSource.Should().Contain("hx-post=\"@Url.Action(\"AddPayment\", \"Orders\")\"");
        paymentShellSource.Should().Contain("<input type=\"hidden\" asp-for=\"OrderId\" />");
        paymentShellSource.Should().Contain("asp-for=\"Provider\"");
        paymentShellSource.Should().Contain("asp-for=\"ProviderReference\"");
        paymentShellSource.Should().Contain("asp-for=\"AmountMinor\"");
        paymentShellSource.Should().Contain("asp-for=\"Currency\"");
        paymentShellSource.Should().Contain("asp-for=\"Status\"");
        paymentShellSource.Should().Contain("asp-for=\"FailureReason\"");
        paymentShellSource.Should().Contain("string OrderDetailsUrl(Guid id) => Url.Action(\"Details\", \"Orders\", new { id }) ?? string.Empty;");
        paymentShellSource.Should().Contain("hx-get=\"@OrderDetailsUrl(Model.OrderId)\"");

        shipmentShellSource.Should().Contain("id=\"order-shipment-shell\"");
        shipmentShellSource.Should().Contain("hx-post=\"@Url.Action(\"AddShipment\", \"Orders\")\"");
        shipmentShellSource.Should().Contain("<input type=\"hidden\" asp-for=\"OrderId\" />");
        shipmentShellSource.Should().Contain("asp-for=\"Carrier\"");
        shipmentShellSource.Should().Contain("asp-for=\"Service\"");
        shipmentShellSource.Should().Contain("asp-for=\"ProviderShipmentReference\"");
        shipmentShellSource.Should().Contain("asp-for=\"TrackingNumber\"");
        shipmentShellSource.Should().Contain("asp-for=\"LabelUrl\"");
        shipmentShellSource.Should().Contain("asp-for=\"LastCarrierEventKey\"");
        shipmentShellSource.Should().Contain("asp-for=\"TotalWeight\"");
        shipmentShellSource.Should().Contain("@T.T(\"ShipmentLines\")");
        shipmentShellSource.Should().Contain("asp-for=\"Lines[i].OrderLineId\"");
        shipmentShellSource.Should().Contain("asp-for=\"Lines[i].Quantity\"");
        shipmentShellSource.Should().Contain("string OrderDetailsUrl(Guid id) => Url.Action(\"Details\", \"Orders\", new { id }) ?? string.Empty;");
        shipmentShellSource.Should().Contain("hx-get=\"@OrderDetailsUrl(Model.OrderId)\"");

        refundShellSource.Should().Contain("id=\"order-refund-shell\"");
        refundShellSource.Should().Contain("hx-post=\"@Url.Action(\"AddRefund\", \"Orders\")\"");
        refundShellSource.Should().Contain("<input type=\"hidden\" asp-for=\"OrderId\" />");
        refundShellSource.Should().Contain("asp-for=\"PaymentId\" asp-items=\"Model.PaymentOptions\"");
        refundShellSource.Should().Contain("asp-for=\"AmountMinor\"");
        refundShellSource.Should().Contain("asp-for=\"Currency\"");
        refundShellSource.Should().Contain("asp-for=\"Reason\"");
        refundShellSource.Should().Contain("string OrderDetailsUrl(Guid id) => Url.Action(\"Details\", \"Orders\", new { id }) ?? string.Empty;");
        refundShellSource.Should().Contain("hx-get=\"@OrderDetailsUrl(Model.OrderId)\"");

        invoiceShellSource.Should().Contain("id=\"order-invoice-shell\"");
        invoiceShellSource.Should().Contain("hx-post=\"@Url.Action(\"CreateInvoice\", \"Orders\")\"");
        invoiceShellSource.Should().Contain("<input type=\"hidden\" asp-for=\"OrderId\" />");
        invoiceShellSource.Should().Contain("asp-for=\"BusinessId\" asp-items=\"Model.BusinessOptions\"");
        invoiceShellSource.Should().Contain("asp-for=\"CustomerId\" asp-items=\"Model.CustomerOptions\"");
        invoiceShellSource.Should().Contain("asp-for=\"PaymentId\" asp-items=\"Model.PaymentOptions\"");
        invoiceShellSource.Should().Contain("asp-for=\"DueAtUtc\" type=\"datetime-local\"");
        invoiceShellSource.Should().Contain("string OrderDetailsUrl(Guid id) => Url.Action(\"Details\", \"Orders\", new { id }) ?? string.Empty;");
        invoiceShellSource.Should().Contain("hx-get=\"@OrderDetailsUrl(Model.OrderId)\"");
        invoiceShellSource.Should().Contain("@T.T(\"CreateInvoice\")");
    }


    [Fact]
    public void OrderDetailGridPartials_Should_KeepPaymentsShipmentsRefundsAndInvoicesContractsWired()
    {
        var paymentsSource = ReadWebAdminFile(Path.Combine("Views", "Orders", "_PaymentsGrid.cshtml"));
        var shipmentsSource = ReadWebAdminFile(Path.Combine("Views", "Orders", "_ShipmentsGrid.cshtml"));
        var refundsSource = ReadWebAdminFile(Path.Combine("Views", "Orders", "_RefundsGrid.cshtml"));
        var invoicesSource = ReadWebAdminFile(Path.Combine("Views", "Orders", "_InvoicesGrid.cshtml"));

        paymentsSource.Should().Contain("id=\"payments-grid-content\"");
        paymentsSource.Should().Contain("hx-boost=\"true\"");
        paymentsSource.Should().Contain("hx-target=\"this\"");
        paymentsSource.Should().Contain("hx-push-url=\"false\"");
        paymentsSource.Should().Contain("name=\"orderId\" value=\"@Model.OrderId\"");
        paymentsSource.Should().Contain("name=\"filter\" asp-items=\"Model.FilterItems\"");
        paymentsSource.Should().Contain("@T.T(\"Failed\")");
        paymentsSource.Should().Contain("@T.T(\"Refunded\")");
        paymentsSource.Should().Contain("@T.T(\"NoPayments\")");
        paymentsSource.Should().Contain("@T.T(\"Net\")");
        paymentsSource.Should().Contain("string EditPaymentUrl(Guid id) => Url.Action(\"EditPayment\", \"Billing\", new { id }) ?? string.Empty;");
        paymentsSource.Should().Contain("string AddRefundUrl(Guid orderId, Guid paymentId) => Url.Action(\"AddRefund\", \"Orders\", new { orderId, paymentId }) ?? string.Empty;");
        paymentsSource.Should().Contain("hx-get=\"@EditPaymentUrl(p.Id)\"");
        paymentsSource.Should().Contain("hx-get=\"@AddRefundUrl(Model.OrderId, p.Id)\"");
        paymentsSource.Should().Contain("asp-route-orderId=\"@Model.OrderId\"");
        paymentsSource.Should().Contain("asp-route-filter=\"@Model.Filter\"");
        paymentsSource.Should().Contain("hx-target=\"#payments-grid-content\"");

        shipmentsSource.Should().Contain("id=\"shipments-grid-content\"");
        shipmentsSource.Should().Contain("hx-boost=\"true\"");
        shipmentsSource.Should().Contain("name=\"orderId\" value=\"@Model.OrderId\"");
        shipmentsSource.Should().Contain("name=\"filter\" asp-items=\"Model.FilterItems\"");
        shipmentsSource.Should().Contain("@T.T(\"PendingPacked\")");
        shipmentsSource.Should().Contain("@T.T(\"ShippedDelivered\")");
        shipmentsSource.Should().Contain("@T.T(\"CarrierReview\")");
        shipmentsSource.Should().Contain("@T.T(\"ReturnFollowUp\")");
        shipmentsSource.Should().Contain("@T.T(\"AwaitingHandoff\")");
        shipmentsSource.Should().Contain("@T.T(\"TrackingOverdue\")");
        shipmentsSource.Should().Contain("@T.T(\"NoShipments\")");
        shipmentsSource.Should().Contain("@T.T(\"TrackingOverdueBadge\")");
        shipmentsSource.Should().Contain("@T.T(\"ReturnedParcel\")");
        shipmentsSource.Should().Contain("@T.T(\"UseRefundRestockFollowUp\")");
        shipmentsSource.Should().Contain("!string.IsNullOrWhiteSpace(s.TrackingUrl)");
        shipmentsSource.Should().Contain("<a href=\"@s.TrackingUrl\" target=\"_blank\" rel=\"noopener noreferrer\">@s.TrackingNumber</a>");
        shipmentsSource.Should().Contain("href=\"@s.TrackingUrl\"");
        shipmentsSource.Should().Contain("data-bs-target=\"#refunds\"");
        shipmentsSource.Should().Contain("string ShippingMethodsUrl(string filter) => Url.Action(\"Index\", \"ShippingMethods\", new { filter }) ?? string.Empty;");
        shipmentsSource.Should().Contain("string ShippingSettingsUrl() => Url.Action(\"Edit\", \"SiteSettings\", new { fragment = \"site-settings-shipping\" }) ?? string.Empty;");
        shipmentsSource.Should().Contain("string AddRefundUrl(Guid orderId, Guid? paymentId) => Url.Action(\"AddRefund\", \"Orders\", new { orderId, paymentId }) ?? string.Empty;");
        shipmentsSource.Should().Contain("hx-get=\"@ShippingMethodsUrl(\"Dhl\")\"");
        shipmentsSource.Should().Contain("hx-get=\"@ShippingSettingsUrl()\"");
        shipmentsSource.Should().Contain("hx-get=\"@AddRefundUrl(Model.OrderId, Model.DefaultRefundPaymentId)\"");
        shipmentsSource.Should().Contain("asp-route-orderId=\"@Model.OrderId\"");
        shipmentsSource.Should().Contain("asp-route-filter=\"@Model.Filter\"");
        shipmentsSource.Should().Contain("hx-target=\"#shipments-grid-content\"");

        refundsSource.Should().Contain("id=\"refunds-grid-content\"");
        refundsSource.Should().Contain("hx-boost=\"true\"");
        refundsSource.Should().Contain("name=\"orderId\" value=\"@Model.OrderId\"");
        refundsSource.Should().Contain("name=\"filter\" asp-items=\"Model.FilterItems\"");
        refundsSource.Should().Contain("@T.T(\"Pending\")");
        refundsSource.Should().Contain("@T.T(\"Completed\")");
        refundsSource.Should().Contain("@Model.ReturnedShipmentCount");
        refundsSource.Should().Contain("@T.T(\"ReturnedShipmentFollowUpOpen\")");
        refundsSource.Should().Contain("@T.T(\"ReturnedShipmentFollowUpNote\")");
        refundsSource.Should().Contain("data-bs-target=\"#shipments\"");
        refundsSource.Should().Contain("string AddRefundUrl(Guid orderId, Guid? paymentId) => Url.Action(\"AddRefund\", \"Orders\", new { orderId, paymentId }) ?? string.Empty;");
        refundsSource.Should().Contain("hx-get=\"@AddRefundUrl(Model.OrderId, Model.DefaultRefundPaymentId)\"");
        refundsSource.Should().Contain("@T.T(\"NoRefunds\")");
        refundsSource.Should().Contain("@T.T(\"PaymentStatus\")");
        refundsSource.Should().Contain("string EditPaymentUrl(Guid id) => Url.Action(\"EditPayment\", \"Billing\", new { id }) ?? string.Empty;");
        refundsSource.Should().Contain("hx-get=\"@EditPaymentUrl(item.PaymentId)\"");
        refundsSource.Should().Contain("asp-route-orderId=\"@Model.OrderId\"");
        refundsSource.Should().Contain("asp-route-filter=\"@Model.Filter\"");
        refundsSource.Should().Contain("hx-target=\"#refunds-grid-content\"");

        invoicesSource.Should().Contain("id=\"invoices-grid-content\"");
        invoicesSource.Should().Contain("hx-boost=\"true\"");
        invoicesSource.Should().Contain("name=\"orderId\" value=\"@Model.OrderId\"");
        invoicesSource.Should().Contain("name=\"filter\" asp-items=\"Model.FilterItems\"");
        invoicesSource.Should().Contain("@T.T(\"Outstanding\")");
        invoicesSource.Should().Contain("@T.T(\"Paid\")");
        invoicesSource.Should().Contain("@T.T(\"NoInvoices\")");
        invoicesSource.Should().Contain("@T.T(\"VatIdMissing\")");
        invoicesSource.Should().Contain("string EditCustomerUrl(Guid id) => Url.Action(\"EditCustomer\", \"Crm\", new { id }) ?? string.Empty;");
        invoicesSource.Should().Contain("string EditPaymentUrl(Guid id) => Url.Action(\"EditPayment\", \"Billing\", new { id }) ?? string.Empty;");
        invoicesSource.Should().Contain("string EditInvoiceUrl(Guid id) => Url.Action(\"EditInvoice\", \"Crm\", new { id }) ?? string.Empty;");
        invoicesSource.Should().Contain("hx-get=\"@EditCustomerUrl(item.CustomerId.Value)\"");
        invoicesSource.Should().Contain("hx-get=\"@EditPaymentUrl(item.PaymentId.Value)\"");
        invoicesSource.Should().Contain("hx-get=\"@EditInvoiceUrl(item.Id)\"");
        invoicesSource.Should().Contain("@T.T(\"Net\")");
        invoicesSource.Should().Contain("@T.T(\"Tax\")");
        invoicesSource.Should().Contain("@T.T(\"Gross\")");
        invoicesSource.Should().Contain("@T.T(\"Settled\")");
        invoicesSource.Should().Contain("@T.T(\"Balance\")");
        invoicesSource.Should().Contain("asp-route-orderId=\"@Model.OrderId\"");
        invoicesSource.Should().Contain("asp-route-filter=\"@Model.Filter\"");
        invoicesSource.Should().Contain("hx-target=\"#invoices-grid-content\"");
    }


    [Fact]
    public void ShippingMethodEditorShellAndForm_Should_KeepCreateEditRateTierAndScriptContractsWired()
    {
        var shellSource = ReadWebAdminFile(Path.Combine("Views", "ShippingMethods", "_ShippingMethodEditorShell.cshtml"));
        var formSource = ReadWebAdminFile(Path.Combine("Views", "ShippingMethods", "_ShippingMethodForm.cshtml"));

        shellSource.Should().Contain("id=\"shipping-method-editor-shell\"");
        shellSource.Should().Contain("var isCreate = (bool?)ViewData[\"IsCreate\"] ?? false;");
        shellSource.Should().Contain("@(isCreate ? T.T(\"CreateShippingMethod\") : T.T(\"EditShippingMethod\"))");
        shellSource.Should().Contain("@T.T(\"ShippingMethodEditorIntro\")");
        shellSource.Should().Contain("string ShippingMethodsGlobalIndexUrl() => Url.Action(\"Index\", \"ShippingMethods\") ?? string.Empty;");
        shellSource.Should().Contain("string CreateShippingMethodUrl() => Url.Action(\"Create\", \"ShippingMethods\") ?? string.Empty;");
        shellSource.Should().Contain("hx-get=\"@ShippingMethodsGlobalIndexUrl()\"");
        shellSource.Should().Contain("@T.T(\"Back\")");
        shellSource.Should().Contain("asp-action=\"@(isCreate ? \"Create\" : \"Edit\")\"");
        shellSource.Should().Contain("hx-post=\"@Url.Action(isCreate ? \"Create\" : \"Edit\", \"ShippingMethods\")\"");
        shellSource.Should().Contain("@Html.AntiForgeryToken()");
        shellSource.Should().Contain("<partial name=\"_ShippingMethodForm\" model=\"Model\" />");

        formSource.Should().Contain("<input type=\"hidden\" asp-for=\"Id\" />");
        formSource.Should().Contain("<input type=\"hidden\" asp-for=\"RowVersion\" />");
        formSource.Should().Contain("asp-validation-summary=\"ModelOnly\" class=\"text-danger mb-3\"");
        formSource.Should().Contain("@T.T(\"MethodDetails\")");
        formSource.Should().Contain("asp-for=\"Name\" class=\"form-control\"");
        formSource.Should().Contain("asp-for=\"Carrier\" class=\"form-control\"");
        formSource.Should().Contain("asp-for=\"Service\" class=\"form-control\"");
        formSource.Should().Contain("asp-for=\"CountriesCsv\" class=\"form-control\" placeholder=\"@T.T(\"ShippingMethodCountriesPlaceholder\")\"");
        formSource.Should().Contain("@T.T(\"LeaveCountriesEmpty\")");
        formSource.Should().Contain("asp-for=\"Currency\" class=\"form-control\" placeholder=\"@T.T(\"ShippingMethodCurrencyPlaceholder\")\" maxlength=\"3\"");
        formSource.Should().Contain("asp-for=\"IsActive\" class=\"form-check-input\"");
        formSource.Should().Contain("@T.T(\"RateTiers\")");
        formSource.Should().Contain("data-shipping-rate-add");
        formSource.Should().Contain("@T.T(\"AddRate\")");
        formSource.Should().Contain("id=\"shipping-rates-table\"");
        formSource.Should().Contain("id=\"shipping-rate-rows\"");
        formSource.Should().Contain("name=\"Rates[@i].Id\"");
        formSource.Should().Contain("name=\"Rates[@i].MaxShipmentMass\"");
        formSource.Should().Contain("name=\"Rates[@i].MaxSubtotalNetMinor\"");
        formSource.Should().Contain("name=\"Rates[@i].PriceMinor\"");
        formSource.Should().Contain("name=\"Rates[@i].SortOrder\"");
        formSource.Should().Contain("data-shipping-rate-remove");
        formSource.Should().Contain("@T.T(\"ShippingRateTiersHelp\")");
        formSource.Should().Contain("id=\"shipping-rate-row-template\"");
        formSource.Should().Contain("Rates[__index__].Id");
        formSource.Should().Contain("Rates[__index__].MaxShipmentMass");
        formSource.Should().Contain("Rates[__index__].MaxSubtotalNetMinor");
        formSource.Should().Contain("Rates[__index__].PriceMinor");
        formSource.Should().Contain("Rates[__index__].SortOrder");
        formSource.Should().NotContain("<script>");
        formSource.Should().Contain("@T.T(\"Save\")");
        formSource.Should().Contain("string ShippingMethodsGlobalIndexUrl() => Url.Action(\"Index\", \"ShippingMethods\") ?? string.Empty;");
        formSource.Should().Contain("hx-get=\"@ShippingMethodsGlobalIndexUrl()\"");
        formSource.Should().Contain("@T.T(\"Cancel\")");
    }


    [Fact]
    public void WarehousesWorkspace_Should_KeepShellSummaryQueueGridAndPagerContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Inventory", "Warehouses.cshtml"));

        source.Should().Contain("id=\"inventory-warehouses-workspace-shell\"");
        source.Should().Contain("@T.T(\"WarehousesTitle\")");
        source.Should().Contain("string WarehousesUrl(object? businessId = null, string? q = null, string? filter = null) => Url.Action(\"Warehouses\", \"Inventory\", new { businessId, q, filter }) ?? string.Empty;");
        source.Should().Contain("hx-get=\"@WarehousesUrl()\"");
        source.Should().Contain("name=\"businessId\" asp-items=\"Model.BusinessOptions\"");
        source.Should().Contain("name=\"q\" value=\"@Model.Query\"");
        source.Should().Contain("@T.T(\"SearchWarehousesPlaceholder\")");
        source.Should().Contain("name=\"filter\" asp-items=\"Model.FilterItems\"");
        source.Should().Contain("@T.T(\"Filter\")");
        source.Should().Contain("@T.T(\"Reset\")");
        source.Should().Contain("string CreateWarehouseUrl(object? businessId = null) => Url.Action(\"CreateWarehouse\", \"Inventory\", new { businessId }) ?? string.Empty;");
        source.Should().Contain("hx-get=\"@CreateWarehouseUrl(Model.BusinessId)\"");
        source.Should().Contain("@T.T(\"NewWarehouse\")");
        source.Should().Contain("asp-route-filter=\"Default\"");
        source.Should().Contain("asp-route-filter=\"NoStockLevels\"");
        source.Should().Contain("@T.T(\"ClearQueueFilters\")");
        source.Should().Contain("@Model.Summary.TotalCount");
        source.Should().Contain("@Model.Summary.DefaultCount");
        source.Should().Contain("@Model.Summary.NoStockLevelsCount");
        source.Should().Contain("@T.T(\"WarehouseOpsPlaybooks\")");
        source.Should().Contain("@T.T(\"Playbook\")");
        source.Should().Contain("@T.T(\"WhenItApplies\")");
        source.Should().Contain("@T.T(\"OperatorAction\")");
        source.Should().Contain("@playbook.Title");
        source.Should().Contain("@playbook.ScopeNote");
        source.Should().Contain("@playbook.OperatorAction");
        source.Should().Contain("@T.T(\"Name\")");
        source.Should().Contain("@T.T(\"Location\")");
        source.Should().Contain("@T.T(\"Default\")");
        source.Should().Contain("@T.T(\"StockLevels\")");
        source.Should().Contain("@T.T(\"Actions\")");
        source.Should().Contain("@T.T(\"NoWarehousesFound\")");
        source.Should().Contain("@T.T(\"Standard\")");
        source.Should().Contain("@T.T(\"Empty\")");
        source.Should().Contain("string StockLevelsUrl(object? businessId = null, object? warehouseId = null) => Url.Action(\"StockLevels\", \"Inventory\", new { businessId, warehouseId }) ?? string.Empty;");
        source.Should().Contain("string EditWarehouseUrl(Guid id) => Url.Action(\"EditWarehouse\", \"Inventory\", new { id }) ?? string.Empty;");
        source.Should().Contain("hx-get=\"@StockLevelsUrl(Model.BusinessId, item.Id)\"");
        source.Should().Contain("hx-get=\"@EditWarehouseUrl(item.Id)\"");
        source.Should().Contain("asp-controller=\"Inventory\"");
        source.Should().Contain("asp-action=\"Warehouses\"");
        source.Should().Contain("asp-route-businessId=\"@Model.BusinessId\"");
        source.Should().Contain("asp-route-q=\"@Model.Query\"");
        source.Should().Contain("asp-route-filter=\"@Model.Filter\"");
    }


    [Fact]
    public void WarehouseEditorShellAndForm_Should_KeepCreateEditAndBusinessBindingContractsWired()
    {
        var shellSource = ReadWebAdminFile(Path.Combine("Views", "Inventory", "_WarehouseEditorShell.cshtml"));
        var formSource = ReadWebAdminFile(Path.Combine("Views", "Inventory", "_WarehouseForm.cshtml"));

        shellSource.Should().Contain("id=\"inventory-warehouse-editor-shell\"");
        shellSource.Should().Contain("var isCreate = (bool?)ViewData[\"IsCreate\"] == true;");
        shellSource.Should().Contain("ViewData[\"Title\"] = isCreate ? T.T(\"CreateWarehouse\") : T.T(\"EditWarehouse\")");
        shellSource.Should().Contain("@(isCreate ? T.T(\"CreateWarehouse\") : T.T(\"EditWarehouse\"))");
        shellSource.Should().Contain("asp-action=\"@(isCreate ? \"CreateWarehouse\" : \"EditWarehouse\")\"");
        shellSource.Should().Contain("hx-post=\"@Url.Action(isCreate ? \"CreateWarehouse\" : \"EditWarehouse\", \"Inventory\")\"");
        shellSource.Should().Contain("@Html.AntiForgeryToken()");
        shellSource.Should().Contain("<input type=\"hidden\" asp-for=\"Id\" />");
        shellSource.Should().Contain("<input type=\"hidden\" asp-for=\"RowVersion\" />");
        shellSource.Should().Contain("<partial name=\"_WarehouseForm\" model=\"Model\" />");
        shellSource.Should().Contain("@T.T(\"Save\")");
        shellSource.Should().Contain("string WarehousesUrl(Guid? businessId) => Url.Action(\"Warehouses\", \"Inventory\", new { businessId }) ?? string.Empty;");
        shellSource.Should().Contain("hx-get=\"@WarehousesUrl(Model.BusinessId)\"");
        shellSource.Should().Contain("@T.T(\"Back\")");

        formSource.Should().Contain("asp-for=\"BusinessId\" asp-items=\"Model.BusinessOptions\" class=\"form-select\"");
        formSource.Should().Contain("asp-for=\"Name\" class=\"form-control\"");
        formSource.Should().Contain("asp-for=\"Location\" class=\"form-control\"");
        formSource.Should().Contain("asp-for=\"Description\" class=\"form-control\"");
        formSource.Should().Contain("asp-for=\"IsDefault\" class=\"form-check-input\"");
        formSource.Should().Contain("asp-for=\"IsDefault\" class=\"form-check-label\"");
    }


    [Fact]
    public void SuppliersWorkspace_Should_KeepShellSummaryQueueGridAndPagerContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Inventory", "Suppliers.cshtml"));

        source.Should().Contain("id=\"inventory-suppliers-workspace-shell\"");
        source.Should().Contain("@T.T(\"Suppliers\")");
        source.Should().Contain("string SuppliersUrl(object? businessId = null, string? q = null, string? filter = null) => Url.Action(\"Suppliers\", \"Inventory\", new { businessId, q, filter }) ?? string.Empty;");
        source.Should().Contain("hx-get=\"@SuppliersUrl()\"");
        source.Should().Contain("name=\"businessId\" asp-items=\"Model.BusinessOptions\"");
        source.Should().Contain("name=\"q\" value=\"@Model.Query\"");
        source.Should().Contain("@T.T(\"SearchSuppliersPlaceholder\")");
        source.Should().Contain("name=\"filter\" asp-items=\"Model.FilterItems\"");
        source.Should().Contain("@T.T(\"Filter\")");
        source.Should().Contain("@T.T(\"Reset\")");
        source.Should().Contain("string CreateSupplierUrl(object? businessId = null) => Url.Action(\"CreateSupplier\", \"Inventory\", new { businessId }) ?? string.Empty;");
        source.Should().Contain("hx-get=\"@CreateSupplierUrl(Model.BusinessId)\"");
        source.Should().Contain("@T.T(\"NewSupplier\")");
        source.Should().Contain("asp-route-filter=\"MissingAddress\"");
        source.Should().Contain("asp-route-filter=\"HasPurchaseOrders\"");
        source.Should().Contain("@T.T(\"ClearQueueFilters\")");
        source.Should().Contain("@Model.Summary.TotalCount");
        source.Should().Contain("@Model.Summary.MissingAddressCount");
        source.Should().Contain("@Model.Summary.HasPurchaseOrdersCount");
        source.Should().Contain("@T.T(\"SupplierOpsPlaybooks\")");
        source.Should().Contain("@T.T(\"Playbook\")");
        source.Should().Contain("@T.T(\"WhenItApplies\")");
        source.Should().Contain("@T.T(\"OperatorAction\")");
        source.Should().Contain("@playbook.Title");
        source.Should().Contain("@playbook.ScopeNote");
        source.Should().Contain("@playbook.OperatorAction");
        source.Should().Contain("@T.T(\"Name\")");
        source.Should().Contain("@T.T(\"Email\")");
        source.Should().Contain("@T.T(\"Phone\")");
        source.Should().Contain("@T.T(\"Address\")");
        source.Should().Contain("@T.T(\"PurchaseOrders\")");
        source.Should().Contain("@T.T(\"Actions\")");
        source.Should().Contain("@T.T(\"NoSuppliersFound\")");
        source.Should().Contain("@T.T(\"MissingAddressBadge\")");
        source.Should().Contain("@T.T(\"Active\")");
        source.Should().Contain("string PurchaseOrdersUrl(object? businessId = null, string? q = null) => Url.Action(\"PurchaseOrders\", \"Inventory\", new { businessId, q }) ?? string.Empty;");
        source.Should().Contain("hx-get=\"@PurchaseOrdersUrl(Model.BusinessId, item.Name)\"");
        source.Should().Contain("string EditSupplierUrl(Guid id) => Url.Action(\"EditSupplier\", \"Inventory\", new { id }) ?? string.Empty;");
        source.Should().Contain("hx-get=\"@EditSupplierUrl(item.Id)\"");
        source.Should().Contain("asp-controller=\"Inventory\"");
        source.Should().Contain("asp-action=\"Suppliers\"");
        source.Should().Contain("asp-route-businessId=\"@Model.BusinessId\"");
        source.Should().Contain("asp-route-q=\"@Model.Query\"");
        source.Should().Contain("asp-route-filter=\"@Model.Filter\"");
    }


    [Fact]
    public void SupplierEditorShellAndForm_Should_KeepCreateEditAndBusinessContactContractsWired()
    {
        var shellSource = ReadWebAdminFile(Path.Combine("Views", "Inventory", "_SupplierEditorShell.cshtml"));
        var formSource = ReadWebAdminFile(Path.Combine("Views", "Inventory", "_SupplierForm.cshtml"));

        shellSource.Should().Contain("id=\"inventory-supplier-editor-shell\"");
        shellSource.Should().Contain("var isCreate = (bool?)ViewData[\"IsCreate\"] == true;");
        shellSource.Should().Contain("ViewData[\"Title\"] = isCreate ? T.T(\"CreateSupplier\") : T.T(\"EditSupplier\")");
        shellSource.Should().Contain("@(isCreate ? T.T(\"CreateSupplier\") : T.T(\"EditSupplier\"))");
        shellSource.Should().Contain("asp-action=\"@(isCreate ? \"CreateSupplier\" : \"EditSupplier\")\"");
        shellSource.Should().Contain("hx-post=\"@Url.Action(isCreate ? \"CreateSupplier\" : \"EditSupplier\", \"Inventory\")\"");
        shellSource.Should().Contain("@Html.AntiForgeryToken()");
        shellSource.Should().Contain("<input type=\"hidden\" asp-for=\"Id\" />");
        shellSource.Should().Contain("<input type=\"hidden\" asp-for=\"RowVersion\" />");
        shellSource.Should().Contain("<partial name=\"_SupplierForm\" model=\"Model\" />");
        shellSource.Should().Contain("@T.T(\"Save\")");
        shellSource.Should().Contain("string SuppliersUrl(Guid? businessId) => Url.Action(\"Suppliers\", \"Inventory\", new { businessId }) ?? string.Empty;");
        shellSource.Should().Contain("hx-get=\"@SuppliersUrl(Model.BusinessId)\"");
        shellSource.Should().Contain("@T.T(\"Back\")");

        formSource.Should().Contain("asp-for=\"BusinessId\" asp-items=\"Model.BusinessOptions\" class=\"form-select\"");
        formSource.Should().Contain("asp-for=\"Name\" class=\"form-control\"");
        formSource.Should().Contain("asp-for=\"Email\" class=\"form-control\"");
        formSource.Should().Contain("asp-for=\"Phone\" class=\"form-control\"");
        formSource.Should().Contain("asp-for=\"Address\" class=\"form-control\"");
        formSource.Should().Contain("textarea asp-for=\"Notes\" class=\"form-control\" rows=\"4\"></textarea>");
    }


    [Fact]
    public void PurchaseOrderEditorShellAndForm_Should_KeepCreateEditAndLineTemplateContractsWired()
    {
        var shellSource = ReadWebAdminFile(Path.Combine("Views", "Inventory", "_PurchaseOrderEditorShell.cshtml"));
        var formSource = ReadWebAdminFile(Path.Combine("Views", "Inventory", "_PurchaseOrderForm.cshtml"));

        shellSource.Should().Contain("id=\"inventory-purchase-order-editor-shell\"");
        shellSource.Should().Contain("var isCreate = (bool?)ViewData[\"IsCreate\"] == true;");
        shellSource.Should().Contain("ViewData[\"Title\"] = isCreate ? T.T(\"CreatePurchaseOrder\") : T.T(\"EditPurchaseOrder\")");
        shellSource.Should().Contain("@(isCreate ? T.T(\"CreatePurchaseOrder\") : T.T(\"EditPurchaseOrder\"))");
        shellSource.Should().Contain("asp-action=\"@(isCreate ? \"CreatePurchaseOrder\" : \"EditPurchaseOrder\")\"");
        shellSource.Should().Contain("hx-post=\"@Url.Action(isCreate ? \"CreatePurchaseOrder\" : \"EditPurchaseOrder\", \"Inventory\")\"");
        shellSource.Should().Contain("@Html.AntiForgeryToken()");
        shellSource.Should().Contain("<input type=\"hidden\" asp-for=\"Id\" />");
        shellSource.Should().Contain("<input type=\"hidden\" asp-for=\"RowVersion\" />");
        shellSource.Should().Contain("<partial name=\"_PurchaseOrderForm\" model=\"Model\" />");
        shellSource.Should().Contain("@T.T(\"Save\")");
        shellSource.Should().Contain("asp-action=\"PurchaseOrders\" asp-route-businessId=\"@Model.BusinessId\" class=\"btn btn-outline-secondary\"");
        shellSource.Should().Contain("@T.T(\"Back\")");

        formSource.Should().Contain("asp-for=\"BusinessId\" asp-items=\"Model.BusinessOptions\" class=\"form-select\"");
        formSource.Should().Contain("asp-for=\"SupplierId\" asp-items=\"Model.SupplierOptions\" class=\"form-select\"");
        formSource.Should().Contain("asp-for=\"Status\" class=\"form-control\"");
        formSource.Should().Contain("asp-for=\"OrderNumber\" class=\"form-control\"");
        formSource.Should().Contain("asp-for=\"OrderedAtUtc\" type=\"datetime-local\" class=\"form-control\"");
        formSource.Should().Contain("@T.T(\"PurchaseOrderLines\")");
        formSource.Should().Contain("id=\"addPurchaseOrderLine\"");
        formSource.Should().Contain("@T.T(\"AddLine\")");
        formSource.Should().Contain("id=\"purchaseOrderLines\"");
        formSource.Should().Contain("asp-for=\"Lines[i].ProductVariantId\" asp-items=\"Model.VariantOptions\" class=\"form-select\"");
        formSource.Should().Contain("asp-for=\"Lines[i].Quantity\" class=\"form-control\"");
        formSource.Should().Contain("asp-for=\"Lines[i].UnitCostMinor\" class=\"form-control\"");
        formSource.Should().Contain("asp-for=\"Lines[i].TotalCostMinor\" class=\"form-control\"");
        formSource.Should().Contain("class=\"btn btn-sm btn-outline-danger remove-line\"");
        formSource.Should().Contain("id=\"purchaseOrderLineTemplate\"");
        formSource.Should().Contain("Lines[__index__].ProductVariantId");
        formSource.Should().Contain("Lines[__index__].Quantity");
        formSource.Should().Contain("Lines[__index__].UnitCostMinor");
        formSource.Should().Contain("Lines[__index__].TotalCostMinor");
        formSource.Should().Contain("@T.T(\"ProductVariant\")");
        formSource.Should().Contain("@T.T(\"Quantity\")");
        formSource.Should().Contain("@T.T(\"UnitCost\")");
        formSource.Should().Contain("@T.T(\"TotalCost\")");
        formSource.Should().Contain("@T.T(\"Remove\")");
    }


    [Fact]
    public void StockLevelsWorkspace_Should_KeepShellQueueGridAndActionRailContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Inventory", "StockLevels.cshtml"));

        source.Should().Contain("id=\"inventory-stock-levels-workspace-shell\"");
        source.Should().Contain("@T.T(\"StockLevelsTitle\")");
        source.Should().Contain("string StockLevelsUrl(object? businessId = null, object? warehouseId = null, string? q = null, string? filter = null) => Url.Action(\"StockLevels\", \"Inventory\", new { businessId, warehouseId, q, filter }) ?? string.Empty;");
        source.Should().Contain("hx-get=\"@StockLevelsUrl()\"");
        source.Should().Contain("name=\"businessId\" value=\"@Model.BusinessId\"");
        source.Should().Contain("name=\"warehouseId\" asp-items=\"Model.WarehouseOptions\"");
        source.Should().Contain("name=\"q\" value=\"@Model.Query\"");
        source.Should().Contain("@T.T(\"SearchStockLevelsPlaceholder\")");
        source.Should().Contain("name=\"filter\" asp-items=\"Model.FilterItems\"");
        source.Should().Contain("@T.T(\"Filter\")");
        source.Should().Contain("@T.T(\"Reset\")");
        source.Should().Contain("string CreateStockLevelUrl(object? businessId = null, object? warehouseId = null) => Url.Action(\"CreateStockLevel\", \"Inventory\", new { businessId, warehouseId }) ?? string.Empty;");
        source.Should().Contain("hx-get=\"@CreateStockLevelUrl(Model.BusinessId, Model.WarehouseId)\"");
        source.Should().Contain("@T.T(\"NewStockLevel\")");
        source.Should().Contain("asp-route-filter=\"LowStock\"");
        source.Should().Contain("asp-route-filter=\"Reserved\"");
        source.Should().Contain("asp-route-filter=\"InTransit\"");
        source.Should().Contain("@T.T(\"ClearQueueFilters\")");
        source.Should().Contain("@T.T(\"Sku\")");
        source.Should().Contain("@T.T(\"Available\")");
        source.Should().Contain("@T.T(\"Reserved\")");
        source.Should().Contain("@T.T(\"ReorderPoint\")");
        source.Should().Contain("@T.T(\"Actions\")");
        source.Should().Contain("@T.T(\"NoStockLevelsFound\")");
        source.Should().Contain("string AdjustStockUrl(Guid stockLevelId, object? businessId = null) => Url.Action(\"AdjustStock\", \"Inventory\", new { stockLevelId, businessId }) ?? string.Empty;");
        source.Should().Contain("hx-get=\"@AdjustStockUrl(item.Id, Model.BusinessId)\"");
        source.Should().Contain("@T.T(\"Adjust\")");
        source.Should().Contain("string ReserveStockUrl(Guid stockLevelId, object? businessId = null) => Url.Action(\"ReserveStock\", \"Inventory\", new { stockLevelId, businessId }) ?? string.Empty;");
        source.Should().Contain("hx-get=\"@ReserveStockUrl(item.Id, Model.BusinessId)\"");
        source.Should().Contain("@T.T(\"Reserve\")");
        source.Should().Contain("string ReleaseReservationUrl(Guid stockLevelId, object? businessId = null) => Url.Action(\"ReleaseReservation\", \"Inventory\", new { stockLevelId, businessId }) ?? string.Empty;");
        source.Should().Contain("hx-get=\"@ReleaseReservationUrl(item.Id, Model.BusinessId)\"");
        source.Should().Contain("@T.T(\"Release\")");
        source.Should().Contain("string ReturnReceiptUrl(Guid stockLevelId, object? businessId = null) => Url.Action(\"ReturnReceipt\", \"Inventory\", new { stockLevelId, businessId }) ?? string.Empty;");
        source.Should().Contain("hx-get=\"@ReturnReceiptUrl(item.Id, Model.BusinessId)\"");
        source.Should().Contain("@T.T(\"Return\")");
        source.Should().Contain("string VariantLedgerUrl(Guid variantId, Guid? warehouseId) => Url.Action(\"VariantLedger\", \"Inventory\", new { variantId, warehouseId }) ?? string.Empty;");
        source.Should().Contain("hx-get=\"@VariantLedgerUrl(item.ProductVariantId, item.WarehouseId)\"");
        source.Should().Contain("@T.T(\"Ledger\")");
        source.Should().Contain("string EditStockLevelUrl(Guid id) => Url.Action(\"EditStockLevel\", \"Inventory\", new { id }) ?? string.Empty;");
        source.Should().Contain("hx-get=\"@EditStockLevelUrl(item.Id)\"");
        source.Should().Contain("@T.T(\"Edit\")");
        source.Should().Contain("asp-controller=\"Inventory\"");
        source.Should().Contain("asp-action=\"StockLevels\"");
        source.Should().Contain("asp-route-businessId=\"@Model.BusinessId\"");
        source.Should().Contain("asp-route-warehouseId=\"@Model.WarehouseId\"");
        source.Should().Contain("asp-route-q=\"@Model.Query\"");
        source.Should().Contain("asp-route-filter=\"@Model.Filter\"");
    }


    [Fact]
    public void StockLevelEditorShellAndForm_Should_KeepCreateEditAndQuantityContractsWired()
    {
        var shellSource = ReadWebAdminFile(Path.Combine("Views", "Inventory", "_StockLevelEditorShell.cshtml"));
        var formSource = ReadWebAdminFile(Path.Combine("Views", "Inventory", "_StockLevelForm.cshtml"));

        shellSource.Should().Contain("id=\"inventory-stock-level-editor-shell\"");
        shellSource.Should().Contain("var isCreate = ViewData[\"IsCreate\"] as bool? == true;");
        shellSource.Should().Contain("ViewData[\"Title\"] = isCreate ? T.T(\"InventoryCreateStockLevelTitle\") : T.T(\"InventoryEditStockLevelTitle\")");
        shellSource.Should().Contain("@(isCreate ? T.T(\"InventoryCreateStockLevelTitle\") : T.T(\"InventoryEditStockLevelTitle\"))");
        shellSource.Should().Contain("@T.T(\"InventoryStockLevelEditorIntro\")");
        shellSource.Should().Contain("asp-action=\"@(isCreate ? \"CreateStockLevel\" : \"EditStockLevel\")\"");
        shellSource.Should().Contain("hx-post=\"@Url.Action(isCreate ? \"CreateStockLevel\" : \"EditStockLevel\", \"Inventory\")\"");
        shellSource.Should().Contain("@Html.AntiForgeryToken()");
        shellSource.Should().Contain("<input type=\"hidden\" asp-for=\"Id\" />");
        shellSource.Should().Contain("<input type=\"hidden\" asp-for=\"RowVersion\" />");
        shellSource.Should().Contain("<partial name=\"_StockLevelForm\" model=\"Model\" />");
        shellSource.Should().Contain("@(isCreate ? T.T(\"Create\") : T.T(\"Save\"))");
        shellSource.Should().Contain("string StockLevelsUrl(Guid? warehouseId) => Url.Action(\"StockLevels\", \"Inventory\", new { warehouseId }) ?? string.Empty;");
        shellSource.Should().Contain("hx-get=\"@StockLevelsUrl(Model.WarehouseId)\"");
        shellSource.Should().Contain("@T.T(\"Back\")");

        formSource.Should().Contain("asp-for=\"WarehouseId\" asp-items=\"Model.WarehouseOptions\" class=\"form-select\"");
        formSource.Should().Contain("asp-for=\"ProductVariantId\" asp-items=\"Model.VariantOptions\" class=\"form-select\"");
        formSource.Should().Contain("asp-for=\"AvailableQuantity\" class=\"form-control\"");
        formSource.Should().Contain("asp-for=\"ReservedQuantity\" class=\"form-control\"");
        formSource.Should().Contain("asp-for=\"ReorderPoint\" class=\"form-control\"");
        formSource.Should().Contain("asp-for=\"ReorderQuantity\" class=\"form-control\"");
        formSource.Should().Contain("asp-for=\"InTransitQuantity\" class=\"form-control\"");
    }


    [Fact]
    public void StockTransfersWorkspace_Should_KeepShellSummaryQueueGridAndPagerContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Inventory", "StockTransfers.cshtml"));

        source.Should().Contain("id=\"inventory-stock-transfers-workspace-shell\"");
        source.Should().Contain("@T.T(\"StockTransfersTitle\")");
        source.Should().Contain("string StockTransfersUrl(object? businessId = null, object? warehouseId = null, string? q = null, string? filter = null) => Url.Action(\"StockTransfers\", \"Inventory\", new { businessId, warehouseId, q, filter }) ?? string.Empty;");
        source.Should().Contain("hx-get=\"@StockTransfersUrl()\"");
        source.Should().Contain("name=\"businessId\" value=\"@ViewBag.BusinessId\"");
        source.Should().Contain("name=\"warehouseId\" asp-items=\"Model.WarehouseOptions\"");
        source.Should().Contain("name=\"q\" value=\"@Model.Query\"");
        source.Should().Contain("@T.T(\"SearchTransfersPlaceholder\")");
        source.Should().Contain("name=\"filter\" asp-items=\"Model.FilterItems\"");
        source.Should().Contain("@T.T(\"Filter\")");
        source.Should().Contain("@T.T(\"Reset\")");
        source.Should().Contain("string CreateStockTransferUrl(object? businessId = null, object? warehouseId = null) => Url.Action(\"CreateStockTransfer\", \"Inventory\", new { businessId, warehouseId }) ?? string.Empty;");
        source.Should().Contain("hx-get=\"@CreateStockTransferUrl(ViewBag.BusinessId, Model.WarehouseId)\"");
        source.Should().Contain("@T.T(\"NewTransfer\")");
        source.Should().Contain("asp-route-filter=\"Draft\"");
        source.Should().Contain("asp-route-filter=\"InTransit\"");
        source.Should().Contain("asp-route-filter=\"Completed\"");
        source.Should().Contain("@T.T(\"ClearQueueFilters\")");
        source.Should().Contain("@Model.Summary.TotalCount");
        source.Should().Contain("@Model.Summary.DraftCount");
        source.Should().Contain("@Model.Summary.InTransitCount");
        source.Should().Contain("@Model.Summary.CompletedCount");
        source.Should().Contain("@T.T(\"StockTransferOpsPlaybooks\")");
        source.Should().Contain("@T.T(\"Playbook\")");
        source.Should().Contain("@T.T(\"WhenItApplies\")");
        source.Should().Contain("@T.T(\"OperatorAction\")");
        source.Should().Contain("@playbook.Title");
        source.Should().Contain("@playbook.ScopeNote");
        source.Should().Contain("@playbook.OperatorAction");
        source.Should().Contain("@T.T(\"From\")");
        source.Should().Contain("@T.T(\"To\")");
        source.Should().Contain("@T.T(\"Status\")");
        source.Should().Contain("@T.T(\"Lines\")");
        source.Should().Contain("@T.T(\"Created\")");
        source.Should().Contain("@T.T(\"Actions\")");
        source.Should().Contain("@T.T(\"NoStockTransfersFound\")");
        source.Should().Contain("T.T(\"InTransit\")");
        source.Should().Contain("T.T(\"Completed\")");
        source.Should().Contain("string EditStockTransferUrl(Guid id) => Url.Action(\"EditStockTransfer\", \"Inventory\", new { id }) ?? string.Empty;");
        source.Should().Contain("hx-get=\"@EditStockTransferUrl(item.Id)\"");
        source.Should().Contain("asp-controller=\"Inventory\"");
        source.Should().Contain("asp-action=\"StockTransfers\"");
        source.Should().Contain("asp-route-businessId=\"@ViewBag.BusinessId\"");
        source.Should().Contain("asp-route-warehouseId=\"@Model.WarehouseId\"");
        source.Should().Contain("asp-route-q=\"@Model.Query\"");
        source.Should().Contain("asp-route-filter=\"@Model.Filter\"");
    }


    [Fact]
    public void StockTransferEditorShellAndForm_Should_KeepCreateEditAndLineTemplateContractsWired()
    {
        var shellSource = ReadWebAdminFile(Path.Combine("Views", "Inventory", "_StockTransferEditorShell.cshtml"));
        var formSource = ReadWebAdminFile(Path.Combine("Views", "Inventory", "_StockTransferForm.cshtml"));

        shellSource.Should().Contain("id=\"inventory-stock-transfer-editor-shell\"");
        shellSource.Should().Contain("var isCreate = (bool?)ViewData[\"IsCreate\"] == true;");
        shellSource.Should().Contain("ViewData[\"Title\"] = isCreate ? T.T(\"CreateStockTransfer\") : T.T(\"EditStockTransfer\")");
        shellSource.Should().Contain("@(isCreate ? T.T(\"CreateStockTransfer\") : T.T(\"EditStockTransfer\"))");
        shellSource.Should().Contain("asp-action=\"@(isCreate ? \"CreateStockTransfer\" : \"EditStockTransfer\")\"");
        shellSource.Should().Contain("hx-post=\"@Url.Action(isCreate ? \"CreateStockTransfer\" : \"EditStockTransfer\", \"Inventory\")\"");
        shellSource.Should().Contain("@Html.AntiForgeryToken()");
        shellSource.Should().Contain("<input type=\"hidden\" asp-for=\"Id\" />");
        shellSource.Should().Contain("<input type=\"hidden\" asp-for=\"RowVersion\" />");
        shellSource.Should().Contain("<partial name=\"_StockTransferForm\" model=\"Model\" />");
        shellSource.Should().Contain("@T.T(\"Save\")");
        shellSource.Should().Contain("string StockTransfersUrl(Guid? warehouseId) => Url.Action(\"StockTransfers\", \"Inventory\", new { warehouseId }) ?? string.Empty;");
        shellSource.Should().Contain("hx-get=\"@StockTransfersUrl(Model.FromWarehouseId)\"");
        shellSource.Should().Contain("@T.T(\"Back\")");

        formSource.Should().Contain("asp-for=\"FromWarehouseId\" asp-items=\"Model.WarehouseOptions\" class=\"form-select\"");
        formSource.Should().Contain("asp-for=\"ToWarehouseId\" asp-items=\"Model.WarehouseOptions\" class=\"form-select\"");
        formSource.Should().Contain("asp-for=\"Status\" class=\"form-control\"");
        formSource.Should().Contain("@T.T(\"TransferLines\")");
        formSource.Should().Contain("id=\"addTransferLine\"");
        formSource.Should().Contain("@T.T(\"AddLine\")");
        formSource.Should().Contain("id=\"transferLines\"");
        formSource.Should().Contain("asp-for=\"Lines[i].ProductVariantId\" asp-items=\"Model.VariantOptions\" class=\"form-select\"");
        formSource.Should().Contain("asp-for=\"Lines[i].Quantity\" class=\"form-control\"");
        formSource.Should().Contain("class=\"btn btn-sm btn-outline-danger remove-line\"");
        formSource.Should().Contain("id=\"transferLineTemplate\"");
        formSource.Should().Contain("Lines[__index__].ProductVariantId");
        formSource.Should().Contain("Lines[__index__].Quantity");
        formSource.Should().Contain("@T.T(\"ProductVariant\")");
        formSource.Should().Contain("@T.T(\"Quantity\")");
        formSource.Should().Contain("@T.T(\"Remove\")");
    }


    [Fact]
    public void VariantLedgerWorkspace_Should_KeepShellSummaryQueueGridAndPagerContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Inventory", "VariantLedger.cshtml"));

        source.Should().Contain("id=\"inventory-ledger-workspace-shell\"");
        source.Should().Contain("@T.T(\"InventoryLedger\")");
        source.Should().Contain("@Model.Summary.TotalCount");
        source.Should().Contain("@Model.Summary.InboundCount");
        source.Should().Contain("@Model.Summary.OutboundCount");
        source.Should().Contain("@Model.Summary.ReservationCount");
        source.Should().Contain("@T.T(\"LedgerOpsPlaybooks\")");
        source.Should().Contain("@T.T(\"Queue\")");
        source.Should().Contain("@T.T(\"Scope\")");
        source.Should().Contain("@T.T(\"OperatorAction\")");
        source.Should().Contain("@item.Title");
        source.Should().Contain("@item.ScopeNote");
        source.Should().Contain("@item.OperatorAction");
        source.Should().Contain("string VariantLedgerUrl(object? variantId = null, object? warehouseId = null, string? filter = null) => Url.Action(\"VariantLedger\", \"Inventory\", new { variantId, warehouseId, filter }) ?? string.Empty;");
        source.Should().Contain("string StockLevelsUrl(object? warehouseId = null) => Url.Action(\"StockLevels\", \"Inventory\", new { warehouseId }) ?? string.Empty;");
        source.Should().Contain("string StockTransfersUrl(object? warehouseId = null) => Url.Action(\"StockTransfers\", \"Inventory\", new { warehouseId }) ?? string.Empty;");
        source.Should().Contain("hx-get=\"@VariantLedgerUrl()\"");
        source.Should().Contain("hx-get=\"@VariantLedgerUrl(Model.VariantId, Model.WarehouseId, \"Inbound\")\"");
        source.Should().Contain("hx-get=\"@StockLevelsUrl(Model.WarehouseId)\"");
        source.Should().Contain("hx-get=\"@StockTransfersUrl(Model.WarehouseId)\"");
        source.Should().Contain("name=\"variantId\" value=\"@Model.VariantId\"");
        source.Should().Contain("name=\"warehouseId\" value=\"@Model.WarehouseId\"");
        source.Should().Contain("name=\"filter\" asp-items=\"Model.FilterItems\"");
        source.Should().Contain("@T.T(\"Filter\")");
        source.Should().Contain("@T.T(\"Reset\")");
        source.Should().Contain("asp-route-filter=\"Inbound\"");
        source.Should().Contain("asp-route-filter=\"Outbound\"");
        source.Should().Contain("asp-route-filter=\"Reservations\"");
        source.Should().Contain("@T.T(\"ClearQueueFilters\")");
        source.Should().Contain("@T.T(\"WhenUtc\")");
        source.Should().Contain("@T.T(\"Warehouse\")");
        source.Should().Contain("@T.T(\"QtyDelta\")");
        source.Should().Contain("@T.T(\"Reason\")");
        source.Should().Contain("@T.T(\"Reference\")");
        source.Should().Contain("@(r.ReferenceId?.ToString() ?? \"-\")");
        source.Should().Contain("@T.T(\"NoLedgerEntries\")");
        source.Should().Contain("asp-controller=\"Inventory\"");
        source.Should().Contain("asp-action=\"VariantLedger\"");
        source.Should().Contain("asp-route-variantId=\"@Model.VariantId\"");
        source.Should().Contain("asp-route-warehouseId=\"@Model.WarehouseId\"");
        source.Should().Contain("asp-route-filter=\"@Model.Filter\"");
        source.Should().Contain("asp-controller=\"Products\" asp-action=\"Index\" class=\"btn btn-outline-secondary\"");
        source.Should().Contain("@T.T(\"Back\")");
    }


    [Fact]
    public void BillingPlansWorkspaceAndEditorSurface_Should_KeepShellSummaryQueueGridAndFormContractsWired()
    {
        var workspaceSource = ReadWebAdminFile(Path.Combine("Views", "Billing", "Plans.cshtml"));
        var shellSource = ReadWebAdminFile(Path.Combine("Views", "Billing", "_BillingPlanEditorShell.cshtml"));
        var formSource = ReadWebAdminFile(Path.Combine("Views", "Billing", "_BillingPlanForm.cshtml"));

        workspaceSource.Should().Contain("id=\"billing-plans-workspace-shell\"");
        workspaceSource.Should().Contain("@T.T(\"BillingPlansTitle\")");
        workspaceSource.Should().Contain("@T.T(\"BillingPlansIntro\")");
        workspaceSource.Should().Contain("string CreatePlanUrl() => Url.Action(\"CreatePlan\", \"Billing\") ?? string.Empty;");
        workspaceSource.Should().Contain("hx-get=\"@CreatePlanUrl()\"");
        workspaceSource.Should().Contain("@T.T(\"BillingPlansCreatePlan\")");
        workspaceSource.Should().Contain("@Model.Summary.TotalCount");
        workspaceSource.Should().Contain("@Model.Summary.ActiveCount");
        workspaceSource.Should().Contain("@Model.Summary.InactiveCount");
        workspaceSource.Should().Contain("@Model.Summary.TrialCount");
        workspaceSource.Should().Contain("@Model.Summary.MissingFeaturesCount");
        workspaceSource.Should().Contain("@Model.Summary.InUseCount");
        workspaceSource.Should().Contain("@T.T(\"BillingPlansPlaybooksTitle\")");
        workspaceSource.Should().Contain("@T.T(\"BillingPlansQueueColumn\")");
        workspaceSource.Should().Contain("@T.T(\"BillingPlansScopeColumn\")");
        workspaceSource.Should().Contain("@T.T(\"OperatorAction\")");
        workspaceSource.Should().Contain("string GlobalBillingPlansUrl() => Url.Action(\"Plans\", \"Billing\") ?? string.Empty;");
        workspaceSource.Should().Contain("hx-get=\"@GlobalBillingPlansUrl()\"");
        workspaceSource.Should().Contain("string BillingPlansUrl(Darwin.Application.Billing.DTOs.BillingPlanQueueFilter? queue = null) => Url.Action(\"Plans\", \"Billing\", new { queue }) ?? string.Empty;");
        workspaceSource.Should().Contain("hx-get=\"@BillingPlansUrl(Darwin.Application.Billing.DTOs.BillingPlanQueueFilter.Inactive)\"");
        workspaceSource.Should().Contain("@T.T(\"BillingPlansSearchPlaceholder\")");
        workspaceSource.Should().Contain("name=\"pageSize\" value=\"@Model.PageSize\"");
        workspaceSource.Should().Contain("@T.T(\"CommonSearch\")");
        workspaceSource.Should().Contain("@T.T(\"CommonReset\")");
        workspaceSource.Should().Contain("@T.T(\"BillingPlansClearQueueFilters\")");
        workspaceSource.Should().Contain("@T.T(\"BillingPlansPriceColumn\")");
        workspaceSource.Should().Contain("@T.T(\"BillingPlansIntervalColumn\")");
        workspaceSource.Should().Contain("@T.T(\"BillingPlansUsageColumn\")");
        workspaceSource.Should().Contain("@T.T(\"BillingPlansUpdatedColumn\")");
        workspaceSource.Should().Contain("@T.T(\"BillingPlansEmptyState\")");
        workspaceSource.Should().Contain("string EditPlanUrl(Guid id) => Url.Action(\"EditPlan\", \"Billing\", new { id }) ?? string.Empty;");
        workspaceSource.Should().Contain("hx-get=\"@EditPlanUrl(item.Id)\"");
        workspaceSource.Should().Contain("@T.T(\"CommonEdit\")");
        workspaceSource.Should().Contain("asp-controller=\"Billing\"");
        workspaceSource.Should().Contain("asp-action=\"Plans\"");
        workspaceSource.Should().Contain("asp-route-q=\"@Model.Query\"");
        workspaceSource.Should().Contain("asp-route-queue=\"@Model.QueueFilter\"");

        shellSource.Should().Contain("BillingPlanEditorCreateTitle");
        shellSource.Should().Contain("BillingPlanEditorEditTitle");
        shellSource.Should().Contain("string GlobalBillingPlansUrl() => Url.Action(\"Plans\", \"Billing\") ?? string.Empty;");
        shellSource.Should().Contain("hx-get=\"@GlobalBillingPlansUrl()\"");
        shellSource.Should().Contain("@T.T(\"BillingPlanEditorBackToPlans\")");
        shellSource.Should().Contain("@T.T(\"BillingPlanEditorActiveSubscriptions\")");
        shellSource.Should().Contain("@T.T(\"CommonStatus\")");
        shellSource.Should().Contain("BillingPlanEditorNoTrial");
        shellSource.Should().Contain("<partial name=\"_BillingPlanForm\" model=\"Model\" />");

        formSource.Should().Contain("asp-action=\"@(isCreate ? \"CreatePlan\" : \"EditPlan\")\"");
        formSource.Should().Contain("hx-post=\"@Url.Action(isCreate ? \"CreatePlan\" : \"EditPlan\", \"Billing\")\"");
        formSource.Should().Contain("hx-target=\"#billing-plan-editor-shell\"");
        formSource.Should().Contain("@Html.AntiForgeryToken()");
        formSource.Should().Contain("input type=\"hidden\" asp-for=\"Id\"");
        formSource.Should().Contain("input type=\"hidden\" asp-for=\"RowVersion\" value=\"@Convert.ToBase64String(Model.RowVersion)\"");
        formSource.Should().Contain("asp-validation-summary=\"ModelOnly\" class=\"alert alert-danger\"");
        formSource.Should().Contain("asp-for=\"Code\" class=\"form-control\"");
        formSource.Should().Contain("asp-for=\"Name\" class=\"form-control\"");
        formSource.Should().Contain("asp-for=\"Currency\" class=\"form-control text-uppercase\"");
        formSource.Should().Contain("asp-for=\"Description\" rows=\"3\" class=\"form-control\"");
        formSource.Should().Contain("asp-for=\"PriceMinor\" class=\"form-control\"");
        formSource.Should().Contain("asp-for=\"Interval\" asp-items=\"Model.IntervalItems\" class=\"form-select\"");
        formSource.Should().Contain("asp-for=\"IntervalCount\" class=\"form-control\"");
        formSource.Should().Contain("asp-for=\"TrialDays\" class=\"form-control\"");
        formSource.Should().Contain("asp-for=\"FeaturesJson\" rows=\"6\" class=\"form-control font-monospace\"");
        formSource.Should().Contain("@T.T(\"BillingPlanFormFeaturesHelp\")");
        formSource.Should().Contain("asp-for=\"IsActive\" class=\"form-check-input\"");
        formSource.Should().Contain("@(isCreate ? T.T(\"BillingPlansCreatePlan\") : T.T(\"BillingPlanFormSaveChanges\"))");
        formSource.Should().Contain("string GlobalBillingPlansUrl() => Url.Action(\"Plans\", \"Billing\") ?? string.Empty;");
        formSource.Should().Contain("hx-get=\"@GlobalBillingPlansUrl()\"");
        formSource.Should().Contain("@T.T(\"Cancel\")");
    }


    [Fact]
    public void BillingPaymentsWorkspaceAndEditorSurface_Should_KeepShellReadinessGridAndFormContractsWired()
    {
        var workspaceSource = ReadWebAdminFile(Path.Combine("Views", "Billing", "Payments.cshtml"));
        var shellSource = ReadWebAdminFile(Path.Combine("Views", "Billing", "_PaymentEditorShell.cshtml"));
        var formSource = ReadWebAdminFile(Path.Combine("Views", "Billing", "_PaymentForm.cshtml"));

        workspaceSource.Should().Contain("id=\"billing-payments-workspace-shell\"");
        workspaceSource.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        workspaceSource.Should().Contain("@T.T(\"Payments\")");
        workspaceSource.Should().Contain("@T.T(\"StripeReadiness\")");
        workspaceSource.Should().Contain("string PaymentSettingsUrl() => Url.Action(\"Edit\", \"SiteSettings\", new { fragment = \"site-settings-payments\" }) ?? string.Empty;");
        workspaceSource.Should().Contain("hx-get=\"@PaymentSettingsUrl()\"");
        workspaceSource.Should().Contain("@T.T(\"OpenPaymentSettings\")");
        workspaceSource.Should().Contain("@T.T(\"TaxInvoicingReadiness\")");
        workspaceSource.Should().Contain("string GlobalTaxComplianceUrl() => Url.Action(\"TaxCompliance\", \"Billing\") ?? string.Empty;");
        workspaceSource.Should().Contain("hx-get=\"@GlobalTaxComplianceUrl()\"");
        workspaceSource.Should().Contain("@T.T(\"OpenTaxCompliance\")");
        workspaceSource.Should().Contain("string TaxSettingsUrl() => Url.Action(\"Edit\", \"SiteSettings\", new { fragment = \"site-settings-tax\" }) ?? string.Empty;");
        workspaceSource.Should().Contain("hx-get=\"@TaxSettingsUrl()\"");
        workspaceSource.Should().Contain("@T.T(\"OpenTaxSettings\")");
        workspaceSource.Should().Contain("@T.T(\"WebhookLifecycleVisibility\")");
        workspaceSource.Should().Contain("string GlobalBillingWebhooksUrl() => Url.Action(\"Webhooks\", \"Billing\") ?? string.Empty;");
        workspaceSource.Should().Contain("hx-get=\"@GlobalBillingWebhooksUrl()\"");
        workspaceSource.Should().Contain("@T.T(\"OpenWebhookQueue\")");
        workspaceSource.Should().Contain("@T.T(\"ReviewPaymentExceptions\")");
        workspaceSource.Should().Contain("@T.T(\"ReviewDisputeSignals\")");
        workspaceSource.Should().Contain("@Model.Summary.PendingCount");
        workspaceSource.Should().Contain("@Model.Summary.FailedCount");
        workspaceSource.Should().Contain("@Model.Summary.RefundedCount");
        workspaceSource.Should().Contain("@Model.Summary.UnlinkedCount");
        workspaceSource.Should().Contain("@Model.Summary.ProviderLinkedCount");
        workspaceSource.Should().Contain("@T.T(\"PaymentSupportPlaybooks\")");
        workspaceSource.Should().Contain("@T.T(\"Queue\")");
        workspaceSource.Should().Contain("@T.T(\"Scope\")");
        workspaceSource.Should().Contain("@T.T(\"OperatorAction\")");
        workspaceSource.Should().Contain("string GlobalPaymentsUrl() => Url.Action(\"Payments\", \"Billing\") ?? string.Empty;");
        workspaceSource.Should().Contain("hx-get=\"@GlobalPaymentsUrl()\"");
        workspaceSource.Should().Contain("name=\"businessId\" asp-items=\"Model.BusinessOptions\"");
        workspaceSource.Should().Contain("name=\"q\" value=\"@Model.Query\"");
        workspaceSource.Should().Contain("@T.T(\"SearchPaymentsPlaceholder\")");
        workspaceSource.Should().Contain("@T.T(\"AllQueues\")");
        workspaceSource.Should().Contain("@T.T(\"NeedsReconciliation\")");
        workspaceSource.Should().Contain("@T.T(\"DisputeFollowUp\")");
        workspaceSource.Should().Contain("@T.T(\"RecordPayment\")");
        workspaceSource.Should().Contain("@T.T(\"NoPaymentsFound\")");
        workspaceSource.Should().Contain("@T.T(\"Provider\")");
        workspaceSource.Should().Contain("@T.T(\"Timeline\")");
        workspaceSource.Should().Contain("string EditPaymentUrl(Guid id) => Url.Action(\"EditPayment\", \"Billing\", new { id }) ?? string.Empty;");
        workspaceSource.Should().Contain("hx-get=\"@EditPaymentUrl(item.Id)\"");
        workspaceSource.Should().Contain("@T.T(\"Edit\")");

        shellSource.Should().Contain("id=\"billing-payment-editor-shell\"");
        shellSource.Should().Contain("ViewData[\"Title\"] = isCreate ? T.T(\"CreatePayment\") : T.T(\"EditPayment\")");
        shellSource.Should().Contain("asp-action=\"@(isCreate ? \"CreatePayment\" : \"EditPayment\")\"");
        shellSource.Should().Contain("hx-post=\"@Url.Action(isCreate ? \"CreatePayment\" : \"EditPayment\", \"Billing\")\"");
        shellSource.Should().Contain("@Html.AntiForgeryToken()");
        shellSource.Should().Contain("input type=\"hidden\" asp-for=\"Id\"");
        shellSource.Should().Contain("input type=\"hidden\" name=\"RowVersion\" value=\"@(Model.RowVersion is null ? string.Empty : Convert.ToBase64String(Model.RowVersion))\"");
        shellSource.Should().Contain("<partial name=\"_PaymentForm\" model=\"Model\" />");
        shellSource.Should().Contain("@T.T(\"Save\")");
        shellSource.Should().Contain("string PaymentsUrl(Guid? businessId) => Url.Action(\"Payments\", \"Billing\", new { businessId }) ?? string.Empty;");
        shellSource.Should().Contain("hx-get=\"@PaymentsUrl(Model.BusinessId)\"");
        shellSource.Should().Contain("@T.T(\"BackToPayments\")");

        formSource.Should().Contain("@T.T(\"RelatedRecords\")");
        formSource.Should().Contain("string OrderDetailsUrl(Guid id) => Url.Action(\"Details\", \"Orders\", new { id }) ?? string.Empty;");
        formSource.Should().Contain("string CrmEditCustomerUrl(Guid id) => Url.Action(\"EditCustomer\", \"Crm\", new { id }) ?? string.Empty;");
        formSource.Should().Contain("string UserEditUrl(Guid id) => Url.Action(\"Edit\", \"Users\", new { id }) ?? string.Empty;");
        formSource.Should().Contain("hx-get=\"@OrderDetailsUrl(Model.OrderId.Value)\"");
        formSource.Should().Contain("hx-get=\"@CrmEditCustomerUrl(Model.CustomerId.Value)\"");
        formSource.Should().Contain("hx-get=\"@UserEditUrl(Model.UserId.Value)\"");
        formSource.Should().Contain("@T.T(\"Lifecycle\")");
        formSource.Should().Contain("@T.T(\"RecordedAmount\")");
        formSource.Should().Contain("@T.T(\"NetCollected\")");
        formSource.Should().Contain("@T.T(\"ReconciliationAndDisputeSnapshot\")");
        formSource.Should().Contain("string PaymentSettingsUrl() => Url.Action(\"PaymentSettings\", \"Settings\") ?? string.Empty;");
        formSource.Should().Contain("hx-get=\"@PaymentSettingsUrl()\"");
        formSource.Should().Contain("@T.T(\"NeedsReconciliationQueue\")");
        formSource.Should().Contain("@T.T(\"DisputeFollowUpQueue\")");
        formSource.Should().Contain("@T.T(\"PaymentExceptions\")");
        formSource.Should().Contain("@T.T(\"DisputeSignals\")");
        formSource.Should().Contain("@T.T(\"RecordedFailureReason\")");
        formSource.Should().Contain("@T.T(\"PaymentSupportPlaybooks\")");
        formSource.Should().Contain("@T.T(\"RefundTimeline\")");
        formSource.Should().Contain("asp-for=\"BusinessId\" asp-items=\"Model.BusinessOptions\" class=\"form-select\"");
        formSource.Should().Contain("asp-for=\"OrderId\" class=\"form-control\"");
        formSource.Should().Contain("asp-for=\"InvoiceId\" class=\"form-control\"");
        formSource.Should().Contain("asp-for=\"CustomerId\" asp-items=\"Model.CustomerOptions\" class=\"form-select\"");
        formSource.Should().Contain("asp-for=\"UserId\" asp-items=\"Model.UserOptions\" class=\"form-select\"");
        formSource.Should().Contain("asp-for=\"AmountMinor\" class=\"form-control\"");
        formSource.Should().Contain("asp-for=\"Currency\" class=\"form-control\"");
        formSource.Should().Contain("var paymentStatusOptions = Html.GetEnumSelectList<Darwin.Domain.Enums.PaymentStatus>().Select");
        formSource.Should().Contain("Text = T.T(option.Text)");
        formSource.Should().Contain("asp-for=\"Status\" asp-items=\"paymentStatusOptions\" class=\"form-select\"");
        formSource.Should().Contain("asp-for=\"Provider\" class=\"form-control\"");
        formSource.Should().Contain("asp-for=\"ProviderTransactionRef\" class=\"form-control\"");
        formSource.Should().Contain("asp-for=\"PaidAtUtc\" type=\"datetime-local\" class=\"form-control\"");
    }


    [Fact]
    public void BillingRefundsAndFinanceWorkspaces_Should_KeepShellSummaryQueueGridAndPagerContractsWired()
    {
        var refundsSource = ReadWebAdminFile(Path.Combine("Views", "Billing", "Refunds.cshtml"));
        var accountsSource = ReadWebAdminFile(Path.Combine("Views", "Billing", "FinancialAccounts.cshtml"));
        var expensesSource = ReadWebAdminFile(Path.Combine("Views", "Billing", "Expenses.cshtml"));
        var journalSource = ReadWebAdminFile(Path.Combine("Views", "Billing", "JournalEntries.cshtml"));

        refundsSource.Should().Contain("id=\"billing-refunds-workspace-shell\"");
        refundsSource.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        refundsSource.Should().Contain("@T.T(\"RefundQueueTitle\")");
        refundsSource.Should().Contain("@T.T(\"RefundQueueIntro\")");
        refundsSource.Should().Contain("string ScopedRefundsUrl(BillingRefundQueueFilter queue) => Url.Action(\"Refunds\", \"Billing\", new { businessId = Model.BusinessId, queue }) ?? string.Empty;");
        refundsSource.Should().Contain("hx-get=\"@ScopedRefundsUrl(BillingRefundQueueFilter.Pending)\"");
        refundsSource.Should().Contain("@T.T(\"BackToPayments\")");
        refundsSource.Should().Contain("@T.T(\"WebhookAnomalyVisibility\")");
        refundsSource.Should().Contain("@T.T(\"OpenWebhookQueue\")");
        refundsSource.Should().Contain("@T.T(\"ReviewPaymentExceptions\")");
        refundsSource.Should().Contain("@T.T(\"ReviewDisputeSignals\")");
        refundsSource.Should().Contain("@Model.Summary.PendingCount");
        refundsSource.Should().Contain("@Model.Summary.CompletedCount");
        refundsSource.Should().Contain("@Model.Summary.FailedCount");
        refundsSource.Should().Contain("@Model.Summary.StripeCount");
        refundsSource.Should().Contain("@Model.Summary.NeedsSupportCount");
        refundsSource.Should().Contain("@T.T(\"RefundSupportPlaybooks\")");
        refundsSource.Should().Contain("@T.T(\"SearchRefundsPlaceholder\")");
        refundsSource.Should().Contain("@T.T(\"AllQueues\")");
        refundsSource.Should().Contain("@T.T(\"OpenPayments\")");
        refundsSource.Should().Contain("@T.T(\"NoRefundsFound\")");
        refundsSource.Should().Contain("string CrmEditCustomerUrl(Guid id) => Url.Action(\"EditCustomer\", \"Crm\", new { id }) ?? string.Empty;");
        refundsSource.Should().Contain("hx-get=\"@CrmEditCustomerUrl(item.CustomerId.Value)\"");
        refundsSource.Should().Contain("asp-controller=\"Billing\"");
        refundsSource.Should().Contain("asp-action=\"Refunds\"");
        refundsSource.Should().Contain("asp-route-businessId=\"@Model.BusinessId\"");
        refundsSource.Should().Contain("asp-route-q=\"@Model.Query\"");
        refundsSource.Should().Contain("asp-route-queue=\"@Model.QueueFilter\"");

        accountsSource.Should().Contain("id=\"billing-financial-accounts-workspace-shell\"");
        accountsSource.Should().Contain("@T.T(\"FinancialAccountsTitle\")");
        accountsSource.Should().Contain("string GlobalFinancialAccountsUrl() => Url.Action(\"FinancialAccounts\", \"Billing\") ?? string.Empty;");
        accountsSource.Should().Contain("hx-get=\"@GlobalFinancialAccountsUrl()\"");
        accountsSource.Should().Contain("@T.T(\"SearchFinancialAccountsPlaceholder\")");
        accountsSource.Should().Contain("@T.T(\"CreateFinancialAccount\")");
        accountsSource.Should().Contain("@Model.Summary.TotalCount");
        accountsSource.Should().Contain("@Model.Summary.AssetCount");
        accountsSource.Should().Contain("@Model.Summary.RevenueCount");
        accountsSource.Should().Contain("@Model.Summary.ExpenseCount");
        accountsSource.Should().Contain("@Model.Summary.MissingCodeCount");
        accountsSource.Should().Contain("@T.T(\"FinancialAccountsPlaybooksTitle\")");
        accountsSource.Should().Contain("@T.T(\"FinancialAccountsEmptyState\")");
        accountsSource.Should().Contain("@T.T(\"FinancialAccountsCodeMissing\")");
        accountsSource.Should().Contain("string FinancialAccountsUrl(object? businessId = null, string? q = null, object? queue = null) => Url.Action(\"FinancialAccounts\", \"Billing\", new { businessId, q, queue }) ?? string.Empty;");
        accountsSource.Should().Contain("string CreateFinancialAccountUrl(object? businessId = null) => Url.Action(\"CreateFinancialAccount\", \"Billing\", new { businessId }) ?? string.Empty;");
        accountsSource.Should().Contain("string EditFinancialAccountUrl(Guid id) => Url.Action(\"EditFinancialAccount\", \"Billing\", new { id }) ?? string.Empty;");
        accountsSource.Should().Contain("hx-get=\"@EditFinancialAccountUrl(item.Id)\"");
        accountsSource.Should().Contain("string JournalEntriesUrl(object? businessId = null, string? q = null) => Url.Action(\"JournalEntries\", \"Billing\", new { businessId, q }) ?? string.Empty;");
        accountsSource.Should().Contain("hx-get=\"@JournalEntriesUrl(Model.BusinessId, string.IsNullOrWhiteSpace(item.Code) ? item.Name : item.Code)\"");
        accountsSource.Should().Contain("asp-controller=\"Billing\"");
        accountsSource.Should().Contain("asp-action=\"FinancialAccounts\"");
        accountsSource.Should().Contain("asp-route-businessId=\"@Model.BusinessId\"");
        accountsSource.Should().Contain("asp-route-q=\"@Model.Query\"");
        accountsSource.Should().Contain("asp-route-queue=\"@Model.QueueFilter\"");

        expensesSource.Should().Contain("id=\"billing-expenses-workspace-shell\"");
        expensesSource.Should().Contain("@T.T(\"ExpensesTitle\")");
        expensesSource.Should().Contain("string GlobalExpensesUrl() => Url.Action(\"Expenses\", \"Billing\") ?? string.Empty;");
        expensesSource.Should().Contain("hx-get=\"@GlobalExpensesUrl()\"");
        expensesSource.Should().Contain("@T.T(\"SearchExpensesPlaceholder\")");
        expensesSource.Should().Contain("@T.T(\"CreateExpense\")");
        expensesSource.Should().Contain("@Model.Summary.TotalCount");
        expensesSource.Should().Contain("@Model.Summary.SupplierLinkedCount");
        expensesSource.Should().Contain("@Model.Summary.RecentCount");
        expensesSource.Should().Contain("@Model.Summary.HighValueCount");
        expensesSource.Should().Contain("@T.T(\"ExpensesReviewPlaybooks\")");
        expensesSource.Should().Contain("@T.T(\"ExpensesEmptyState\")");
        expensesSource.Should().Contain("string ExpensesUrl(object? businessId = null) => Url.Action(\"Expenses\", \"Billing\", new { businessId }) ?? string.Empty;");
        expensesSource.Should().Contain("string CreateExpenseUrl(object? businessId = null) => Url.Action(\"CreateExpense\", \"Billing\", new { businessId }) ?? string.Empty;");
        expensesSource.Should().Contain("string EditExpenseUrl(Guid id) => Url.Action(\"EditExpense\", \"Billing\", new { id }) ?? string.Empty;");
        expensesSource.Should().Contain("hx-get=\"@EditExpenseUrl(item.Id)\"");
        expensesSource.Should().Contain("string EditSupplierUrl(Guid id) => Url.Action(\"EditSupplier\", \"Inventory\", new { id }) ?? string.Empty;");
        expensesSource.Should().Contain("hx-get=\"@EditSupplierUrl(item.SupplierId.Value)\"");
        expensesSource.Should().Contain("asp-controller=\"Billing\"");
        expensesSource.Should().Contain("asp-action=\"Expenses\"");
        expensesSource.Should().Contain("asp-route-businessId=\"@Model.BusinessId\"");
        expensesSource.Should().Contain("asp-route-q=\"@Model.Query\"");

        journalSource.Should().Contain("id=\"billing-journal-entries-workspace-shell\"");
        journalSource.Should().Contain("@T.T(\"JournalEntriesTitle\")");
        journalSource.Should().Contain("string GlobalJournalEntriesUrl() => Url.Action(\"JournalEntries\", \"Billing\") ?? string.Empty;");
        journalSource.Should().Contain("hx-get=\"@GlobalJournalEntriesUrl()\"");
        journalSource.Should().Contain("@T.T(\"SearchJournalEntriesPlaceholder\")");
        journalSource.Should().Contain("@T.T(\"CreateJournalEntry\")");
        journalSource.Should().Contain("@Model.Summary.TotalCount");
        journalSource.Should().Contain("@Model.Summary.RecentCount");
        journalSource.Should().Contain("@Model.Summary.MultiLineCount");
        journalSource.Should().Contain("@T.T(\"JournalReviewPlaybooks\")");
        journalSource.Should().Contain("@T.T(\"JournalEntriesEmptyState\")");
        journalSource.Should().Contain("string JournalEntriesUrl(object? businessId = null, object? queue = null) => Url.Action(\"JournalEntries\", \"Billing\", new { businessId, queue }) ?? string.Empty;");
        journalSource.Should().Contain("string CreateJournalEntryUrl(object? businessId = null) => Url.Action(\"CreateJournalEntry\", \"Billing\", new { businessId }) ?? string.Empty;");
        journalSource.Should().Contain("string EditJournalEntryUrl(Guid id) => Url.Action(\"EditJournalEntry\", \"Billing\", new { id }) ?? string.Empty;");
        journalSource.Should().Contain("hx-get=\"@EditJournalEntryUrl(item.Id)\"");
        journalSource.Should().Contain("asp-controller=\"Billing\"");
        journalSource.Should().Contain("asp-action=\"JournalEntries\"");
        journalSource.Should().Contain("asp-route-businessId=\"@Model.BusinessId\"");
        journalSource.Should().Contain("asp-route-q=\"@Model.Query\"");
        journalSource.Should().Contain("asp-route-queue=\"@Model.QueueFilter\"");
    }


    [Fact]
    public void BillingFinancialEditorsAndForms_Should_KeepCreateEditAndLineContractsWired()
    {
        var accountShellSource = ReadWebAdminFile(Path.Combine("Views", "Billing", "_FinancialAccountEditorShell.cshtml"));
        var accountFormSource = ReadWebAdminFile(Path.Combine("Views", "Billing", "_FinancialAccountForm.cshtml"));
        var expenseShellSource = ReadWebAdminFile(Path.Combine("Views", "Billing", "_ExpenseEditorShell.cshtml"));
        var expenseFormSource = ReadWebAdminFile(Path.Combine("Views", "Billing", "_ExpenseForm.cshtml"));
        var journalShellSource = ReadWebAdminFile(Path.Combine("Views", "Billing", "_JournalEntryEditorShell.cshtml"));
        var journalFormSource = ReadWebAdminFile(Path.Combine("Views", "Billing", "_JournalEntryForm.cshtml"));

        accountShellSource.Should().Contain("id=\"billing-financial-account-editor-shell\"");
        accountShellSource.Should().Contain("ViewData[\"Title\"] = isCreate ? T.T(\"CreateFinancialAccount\") : T.T(\"EditFinancialAccount\")");
        accountShellSource.Should().Contain("hx-post=\"@Url.Action(isCreate ? \"CreateFinancialAccount\" : \"EditFinancialAccount\", \"Billing\")\"");
        accountShellSource.Should().Contain("@Html.AntiForgeryToken()");
        accountShellSource.Should().Contain("input type=\"hidden\" asp-for=\"Id\"");
        accountShellSource.Should().Contain("input type=\"hidden\" name=\"RowVersion\" value=\"@(Model.RowVersion is null ? string.Empty : Convert.ToBase64String(Model.RowVersion))\"");
        accountShellSource.Should().Contain("<partial name=\"_FinancialAccountForm\" model=\"Model\" />");
        accountShellSource.Should().Contain("string FinancialAccountsUrl(Guid? businessId) => Url.Action(\"FinancialAccounts\", \"Billing\", new { businessId }) ?? string.Empty;");
        accountShellSource.Should().Contain("hx-get=\"@FinancialAccountsUrl(Model.BusinessId)\"");
        accountShellSource.Should().Contain("@T.T(\"Back\")");

        accountFormSource.Should().Contain("asp-for=\"BusinessId\" asp-items=\"Model.BusinessOptions\" class=\"form-select\"");
        accountFormSource.Should().Contain("asp-for=\"Code\" class=\"form-control\"");
        accountFormSource.Should().Contain("var accountTypeOptions = Html.GetEnumSelectList<Darwin.Domain.Enums.AccountType>().Select");
        accountFormSource.Should().Contain("Text = T.T(option.Text)");
        accountFormSource.Should().Contain("asp-for=\"Type\" asp-items=\"accountTypeOptions\" class=\"form-select\"");
        accountFormSource.Should().Contain("asp-for=\"Name\" class=\"form-control\"");
        accountFormSource.Should().Contain("asp-validation-for=\"Name\" class=\"text-danger\"");

        expenseShellSource.Should().Contain("id=\"billing-expense-editor-shell\"");
        expenseShellSource.Should().Contain("ViewData[\"Title\"] = isCreate ? T.T(\"CreateExpense\") : T.T(\"EditExpense\")");
        expenseShellSource.Should().Contain("hx-post=\"@Url.Action(isCreate ? \"CreateExpense\" : \"EditExpense\", \"Billing\")\"");
        expenseShellSource.Should().Contain("@Html.AntiForgeryToken()");
        expenseShellSource.Should().Contain("input type=\"hidden\" asp-for=\"Id\"");
        expenseShellSource.Should().Contain("input type=\"hidden\" name=\"RowVersion\" value=\"@(Model.RowVersion is null ? string.Empty : Convert.ToBase64String(Model.RowVersion))\"");
        expenseShellSource.Should().Contain("<partial name=\"_ExpenseForm\" model=\"Model\" />");
        expenseShellSource.Should().Contain("string ExpensesUrl(Guid? businessId) => Url.Action(\"Expenses\", \"Billing\", new { businessId }) ?? string.Empty;");
        expenseShellSource.Should().Contain("hx-get=\"@ExpensesUrl(Model.BusinessId)\"");
        expenseShellSource.Should().Contain("@T.T(\"Back\")");

        expenseFormSource.Should().Contain("asp-for=\"BusinessId\" asp-items=\"Model.BusinessOptions\" class=\"form-select\"");
        expenseFormSource.Should().Contain("asp-for=\"SupplierId\" asp-items=\"Model.SupplierOptions\" class=\"form-select\"");
        expenseFormSource.Should().Contain("asp-for=\"ExpenseDateUtc\" type=\"date\" class=\"form-control\"");
        expenseFormSource.Should().Contain("asp-for=\"Category\" class=\"form-control\"");
        expenseFormSource.Should().Contain("asp-for=\"AmountMinor\" class=\"form-control\"");
        expenseFormSource.Should().Contain("textarea asp-for=\"Description\" class=\"form-control\" rows=\"4\"></textarea>");

        journalShellSource.Should().Contain("id=\"billing-journal-entry-editor-shell\"");
        journalShellSource.Should().Contain("ViewData[\"Title\"] = isCreate ? T.T(\"CreateJournalEntry\") : T.T(\"EditJournalEntry\")");
        journalShellSource.Should().Contain("hx-post=\"@Url.Action(isCreate ? \"CreateJournalEntry\" : \"EditJournalEntry\", \"Billing\")\"");
        journalShellSource.Should().Contain("@Html.AntiForgeryToken()");
        journalShellSource.Should().Contain("input type=\"hidden\" asp-for=\"Id\"");
        journalShellSource.Should().Contain("input type=\"hidden\" name=\"RowVersion\" value=\"@(Model.RowVersion is null ? string.Empty : Convert.ToBase64String(Model.RowVersion))\"");
        journalShellSource.Should().Contain("<partial name=\"_JournalEntryForm\" model=\"Model\" />");
        journalShellSource.Should().Contain("string JournalEntriesUrl(Guid? businessId) => Url.Action(\"JournalEntries\", \"Billing\", new { businessId }) ?? string.Empty;");
        journalShellSource.Should().Contain("hx-get=\"@JournalEntriesUrl(Model.BusinessId)\"");
        journalShellSource.Should().Contain("@T.T(\"Back\")");

        journalFormSource.Should().Contain("asp-for=\"BusinessId\" asp-items=\"Model.BusinessOptions\" class=\"form-select\"");
        journalFormSource.Should().Contain("asp-for=\"EntryDateUtc\" type=\"date\" class=\"form-control\"");
        journalFormSource.Should().Contain("asp-for=\"Description\" class=\"form-control\"");
        journalFormSource.Should().Contain("@T.T(\"Lines\")");
        journalFormSource.Should().Contain("id=\"addJournalLine\" data-dynamic-lines-add data-dynamic-lines-container=\"#journalLines\" data-dynamic-lines-template=\"#journalLineTemplate\"");
        journalFormSource.Should().Contain("id=\"journalLines\"");
        journalFormSource.Should().Contain("asp-for=\"Lines[i].AccountId\" asp-items=\"Model.AccountOptions\" class=\"form-select\"");
        journalFormSource.Should().Contain("asp-for=\"Lines[i].DebitMinor\" class=\"form-control\"");
        journalFormSource.Should().Contain("asp-for=\"Lines[i].CreditMinor\" class=\"form-control\"");
        journalFormSource.Should().Contain("asp-for=\"Lines[i].Memo\" class=\"form-control\"");
        journalFormSource.Should().Contain("id=\"journalLineTemplate\"");
        journalFormSource.Should().Contain("name=\"Lines[__index__].AccountId\"");
        journalFormSource.Should().Contain("name=\"Lines[__index__].DebitMinor\" value=\"0\"");
        journalFormSource.Should().Contain("name=\"Lines[__index__].CreditMinor\" value=\"0\"");
        journalFormSource.Should().Contain("name=\"Lines[__index__].Memo\"");
        journalFormSource.Should().Contain("class=\"btn btn-sm btn-outline-danger remove-line\" data-dynamic-lines-remove>@T.T(\"Remove\")</button>");
    }


    [Fact]
    public void BillingTaxComplianceAndWebhooksWorkspaces_Should_KeepReadinessTriageAndPagerContractsWired()
    {
        var taxSource = ReadWebAdminFile(Path.Combine("Views", "Billing", "TaxCompliance.cshtml"));
        var webhooksSource = ReadWebAdminFile(Path.Combine("Views", "Billing", "Webhooks.cshtml"));

        taxSource.Should().Contain("id=\"billing-tax-compliance-workspace-shell\"");
        taxSource.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        taxSource.Should().Contain("@T.T(\"TaxComplianceTitle\")");
        taxSource.Should().Contain("@T.T(\"TaxComplianceIntro\")");
        taxSource.Should().Contain("@T.T(\"TaxInvoicingReadiness\")");
        taxSource.Should().Contain("string GlobalPaymentsUrl() => Url.Action(\"Payments\", \"Billing\") ?? string.Empty;");
        taxSource.Should().Contain("hx-get=\"@GlobalPaymentsUrl()\"");
        taxSource.Should().Contain("@T.T(\"Payments\")");
        taxSource.Should().Contain("string TaxSettingsUrl() => Url.Action(\"Edit\", \"SiteSettings\", new { fragment = \"site-settings-tax\" }) ?? string.Empty;");
        taxSource.Should().Contain("hx-get=\"@TaxSettingsUrl()\"");
        taxSource.Should().Contain("@T.T(\"OpenTaxSettings\")");
        taxSource.Should().Contain("@T.T(\"ArchiveReadiness\")");
        taxSource.Should().Contain("@T.T(\"EInvoiceBaseline\")");
        taxSource.Should().Contain("@T.T(\"CompleteIssuerData\")");
        taxSource.Should().Contain("@T.T(\"ReviewEInvoiceBaseline\")");
        taxSource.Should().Contain("@Model.Summary.BusinessCustomersMissingVatIdCount");
        taxSource.Should().Contain("@Model.Summary.BusinessInvoicesMissingVatIdCount");
        taxSource.Should().Contain("@Model.Summary.DraftInvoiceCount");
        taxSource.Should().Contain("@Model.Summary.DueSoonInvoiceCount");
        taxSource.Should().Contain("@Model.Summary.OverdueInvoiceCount");
        taxSource.Should().Contain("@T.T(\"TaxCompliancePlaybooksTitle\")");
        taxSource.Should().Contain("@T.T(\"TaxComplianceInvoiceReviewTitle\")");
        taxSource.Should().Contain("@T.T(\"TaxComplianceNoInvoiceFollowUp\")");
        taxSource.Should().Contain("@T.T(\"TaxComplianceOpenInvoiceAction\")");
        taxSource.Should().Contain("@T.T(\"TaxComplianceOpenCustomerAction\")");
        taxSource.Should().Contain("@T.T(\"TaxComplianceOpenOrderAction\")");
        taxSource.Should().Contain("@T.T(\"TaxComplianceCustomerReviewTitle\")");
        taxSource.Should().Contain("@T.T(\"TaxComplianceNoCustomerFollowUp\")");
        taxSource.Should().Contain("string GlobalInvoicesUrl() => Url.Action(\"Invoices\", \"Crm\") ?? string.Empty;");
        taxSource.Should().Contain("hx-get=\"@GlobalInvoicesUrl()\"");
        taxSource.Should().Contain("@T.T(\"ReviewInvoices\")");
        taxSource.Should().Contain("string CrmCustomersUrl(CustomerQueueFilter filter) => Url.Action(\"Customers\", \"Crm\", new { filter }) ?? string.Empty;");
        taxSource.Should().Contain("hx-get=\"@CrmCustomersUrl(CustomerQueueFilter.MissingVatId)\"");
        taxSource.Should().Contain("@T.T(\"FixVatId\")");

        webhooksSource.Should().Contain("id=\"billing-webhooks-workspace-shell\"");
        webhooksSource.Should().Contain("@T.T(\"WebhookDeliveries\")");
        webhooksSource.Should().Contain("@Model.Summary.ActiveSubscriptionCount");
        webhooksSource.Should().Contain("@Model.Summary.PendingDeliveryCount");
        webhooksSource.Should().Contain("@Model.Summary.FailedDeliveryCount");
        webhooksSource.Should().Contain("@Model.Summary.SucceededDeliveryCount");
        webhooksSource.Should().Contain("@Model.Summary.RetryPendingCount");
        webhooksSource.Should().Contain("@Model.Summary.PaymentExceptionCount");
        webhooksSource.Should().Contain("@Model.Summary.DisputeSignalCount");
        webhooksSource.Should().Contain("@T.T(\"WebhookSupportPlaybooks\")");
        webhooksSource.Should().Contain("@T.T(\"ActiveWebhookSubscriptions\")");
        webhooksSource.Should().Contain("@T.T(\"NoWebhookSubscriptionsFound\")");
        webhooksSource.Should().Contain("string GlobalBillingWebhooksUrl() => Url.Action(\"Webhooks\", \"Billing\") ?? string.Empty;");
        webhooksSource.Should().Contain("hx-get=\"@GlobalBillingWebhooksUrl()\"");
        webhooksSource.Should().Contain("string BillingWebhooksUrl(BillingWebhookDeliveryQueueFilter queue) => Url.Action(\"Webhooks\", \"Billing\", new { queue }) ?? string.Empty;");
        webhooksSource.Should().Contain("@T.T(\"SearchWebhookDeliveriesPlaceholder\")");
        webhooksSource.Should().Contain("@T.T(\"AllDeliveries\")");
        webhooksSource.Should().Contain("string GlobalPaymentsUrl() => Url.Action(\"Payments\", \"Billing\") ?? string.Empty;");
        webhooksSource.Should().Contain("hx-get=\"@GlobalPaymentsUrl()\"");
        webhooksSource.Should().Contain("@T.T(\"PaymentsLabel\")");
        webhooksSource.Should().Contain("string PaymentSettingsUrl() => Url.Action(\"Edit\", \"SiteSettings\", new { fragment = \"site-settings-payments\" }) ?? string.Empty;");
        webhooksSource.Should().Contain("hx-get=\"@PaymentSettingsUrl()\"");
        webhooksSource.Should().Contain("@T.T(\"PaymentSettings\")");
        webhooksSource.Should().Contain("@T.T(\"NoWebhookDeliveriesFound\")");
        webhooksSource.Should().Contain("@T.T(\"InactiveSubscription\")");
        webhooksSource.Should().Contain("@T.T(\"DisputeSignal\")");
        webhooksSource.Should().Contain("@T.T(\"PaymentException\")");
        webhooksSource.Should().Contain("@T.T(\"RetryPathUsed\")");
        webhooksSource.Should().Contain("@T.T(\"WebhookDisputeEscalationNote\")");
        webhooksSource.Should().Contain("@T.T(\"WebhookPaymentEvidenceNote\")");
        webhooksSource.Should().Contain("asp-controller=\"Billing\"");
        webhooksSource.Should().Contain("asp-action=\"Webhooks\"");
        webhooksSource.Should().Contain("asp-route-q=\"@Model.Query\"");
        webhooksSource.Should().Contain("asp-route-queue=\"@Model.QueueFilter\"");
        webhooksSource.Should().Contain("pager page=\"Model.Page\" page-size=\"Model.PageSize\" total=\"Model.Total\" asp-controller=\"Billing\" asp-action=\"Webhooks\"");
    }


    [Fact]
    public void BrandEditorShellAndForm_Should_KeepSharedCreateEditAndTranslationContractsWired()
    {
        var shellSource = ReadWebAdminFile(Path.Combine("Views", "Brands", "_BrandEditorShell.cshtml"));
        var formSource = ReadWebAdminFile(Path.Combine("Views", "Brands", "_BrandForm.cshtml"));

        shellSource.Should().Contain("id=\"brand-editor-shell\"");
        shellSource.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        shellSource.Should().Contain("var isCreate = (bool?)ViewData[\"IsCreate\"] == true;");
        shellSource.Should().Contain("ViewData[\"Title\"] = isCreate ? T.T(\"CreateBrand\") : T.T(\"EditBrand\")");
        shellSource.Should().Contain("@(isCreate ? T.T(\"CreateBrand\") : T.T(\"EditBrand\"))");
        shellSource.Should().Contain("@(isCreate ? T.T(\"BrandCreateIntro\") : T.T(\"BrandEditIntro\"))");
        shellSource.Should().Contain("@T.T(\"Translations\"): @Model.Translations.Count");
        shellSource.Should().Contain("@T.T(\"BrandIdentifier\"): @Model.Id");
        shellSource.Should().Contain("asp-action=\"@(isCreate ? \"Create\" : \"Edit\")\"");
        shellSource.Should().Contain("hx-post=\"@Url.Action(isCreate ? \"Create\" : \"Edit\", \"Brands\")\"");
        shellSource.Should().Contain("@Html.AntiForgeryToken()");
        shellSource.Should().Contain("<input type=\"hidden\" asp-for=\"Id\" />");
        shellSource.Should().Contain("<input type=\"hidden\" asp-for=\"RowVersion\" />");
        shellSource.Should().Contain("<partial name=\"_BrandForm\" model=\"Model\" />");

        formSource.Should().Contain("asp-validation-summary=\"All\" class=\"text-danger mb-3\"");
        formSource.Should().Contain("label asp-for=\"Slug\" class=\"form-label\"");
        formSource.Should().Contain("asp-for=\"Slug\" class=\"form-control\"");
        formSource.Should().Contain("@T.T(\"BrandSlugHelp\")");
        formSource.Should().Contain("label asp-for=\"LogoMediaId\" class=\"form-label\"");
        formSource.Should().Contain("asp-for=\"LogoMediaId\" class=\"form-control\"");
        formSource.Should().Contain("@T.T(\"BrandLogoHelp\")");
        formSource.Should().Contain("<div class=\"card-header\"><strong>@T.T(\"Translations\")</strong></div>");
        formSource.Should().Contain("@for (int i = 0; i < Model.Translations.Count; i++)");
        formSource.Should().Contain("label asp-for=\"Translations[i].Culture\" class=\"form-label\"");
        formSource.Should().Contain("asp-for=\"Translations[i].Culture\" class=\"form-control\"");
        formSource.Should().Contain("@T.T(\"BrandCultureHelp\")");
        formSource.Should().Contain("asp-validation-for=\"Translations[i].Culture\" class=\"text-danger\"");
        formSource.Should().Contain("label asp-for=\"Translations[i].Name\" class=\"form-label\"");
        formSource.Should().Contain("asp-for=\"Translations[i].Name\" class=\"form-control\"");
        formSource.Should().Contain("asp-validation-for=\"Translations[i].Name\" class=\"text-danger\"");
        formSource.Should().Contain("label asp-for=\"Translations[i].DescriptionHtml\" class=\"form-label\"");
        formSource.Should().Contain("textarea asp-for=\"Translations[i].DescriptionHtml\" class=\"form-control\" rows=\"3\"></textarea>");
        formSource.Should().Contain("@T.T(\"BrandDescriptionHelp\")");
        formSource.Should().Contain("asp-validation-for=\"Translations[i].DescriptionHtml\" class=\"text-danger\"");
        formSource.Should().Contain("type=\"submit\" class=\"btn btn-primary\"");
        formSource.Should().Contain("@T.T(\"Save\")");
        formSource.Should().Contain("string BrandsIndexUrl() => Url.Action(\"Index\", \"Brands\") ?? string.Empty;");
        formSource.Should().Contain("hx-get=\"@BrandsIndexUrl()\"");
        formSource.Should().Contain("@T.T(\"Back\")");
    }


    [Fact]
    public void CategoryEditorShells_Should_KeepCreateAndEditInlineFormContractsWired()
    {
        var createSource = ReadWebAdminFile(Path.Combine("Views", "Categories", "_CategoryCreateEditorShell.cshtml"));
        var editSource = ReadWebAdminFile(Path.Combine("Views", "Categories", "_CategoryEditEditorShell.cshtml"));

        createSource.Should().Contain("id=\"category-editor-shell\"");
        createSource.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        createSource.Should().Contain("@T.T(\"CreateCategory\")");
        createSource.Should().Contain("@T.T(\"CategoryCreateIntro\")");
        createSource.Should().Contain("@T.T(\"Translations\"): @Model.Translations.Count");
        createSource.Should().Contain("asp-action=\"Create\"");
        createSource.Should().Contain("hx-post=\"@Url.Action(\"Create\", \"Categories\")\"");
        createSource.Should().Contain("@Html.AntiForgeryToken()");
        createSource.Should().Contain("asp-validation-summary=\"All\"");
        createSource.Should().Contain("asp-for=\"ParentId\"");
        createSource.Should().Contain("new SelectList(ViewBag.Categories, \"Id\", \"Name\")");
        createSource.Should().Contain("@T.T(\"RootCategoryOption\")");
        createSource.Should().Contain("asp-for=\"SortOrder\"");
        createSource.Should().Contain("asp-for=\"IsActive\"");
        createSource.Should().Contain("name=\"Translations.Index\" value=\"@i\"");
        createSource.Should().Contain("asp-for=\"Translations[@i].Culture\"");
        createSource.Should().Contain("(IEnumerable<string>)ViewBag.Cultures");
        createSource.Should().Contain("asp-for=\"Translations[@i].Name\"");
        createSource.Should().Contain("asp-for=\"Translations[@i].Slug\"");
        createSource.Should().Contain("CategorySlugHelp");
        createSource.Should().Contain("asp-for=\"Translations[@i].Description\"");
        createSource.Should().Contain("CategoryDescriptionHelp");
        createSource.Should().Contain("string CategoriesIndexUrl() => Url.Action(\"Index\", \"Categories\") ?? string.Empty;");
        createSource.Should().Contain("hx-get=\"@CategoriesIndexUrl()\"");
        createSource.Should().Contain("@T.T(\"Back\")");
        createSource.Should().Contain("@T.T(\"Create\")");

        editSource.Should().Contain("id=\"category-editor-shell\"");
        editSource.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        editSource.Should().Contain("@T.T(\"EditCategory\")");
        editSource.Should().Contain("@T.T(\"CategoryEditIntro\")");
        editSource.Should().Contain("@T.T(\"Translations\"): @Model.Translations.Count");
        editSource.Should().Contain("@T.T(\"CategoryIdentifier\"): @Model.Id");
        editSource.Should().Contain("asp-action=\"Edit\"");
        editSource.Should().Contain("class=\"needs-validation\" novalidate");
        editSource.Should().Contain("hx-post=\"@Url.Action(\"Edit\", \"Categories\")\"");
        editSource.Should().Contain("@Html.AntiForgeryToken()");
        editSource.Should().Contain("<input type=\"hidden\" asp-for=\"Id\" />");
        editSource.Should().Contain("<input type=\"hidden\" asp-for=\"RowVersion\" />");
        editSource.Should().Contain("asp-validation-summary=\"ModelOnly\"");
        editSource.Should().Contain("@T.T(\"ParentCategory\")");
        editSource.Should().Contain("@T.T(\"NoParentOption\")");
        editSource.Should().Contain("ViewBag.Categories is IEnumerable<Darwin.Application.Catalog.Queries.LookupItem> cats");
        editSource.Should().Contain("asp-for=\"SortOrder\"");
        editSource.Should().Contain("asp-for=\"IsActive\"");
        editSource.Should().Contain("asp-for=\"Translations[@i].Culture\"");
        editSource.Should().Contain("asp-validation-for=\"Translations[@i].Culture\"");
        editSource.Should().Contain("asp-for=\"Translations[@i].Name\"");
        editSource.Should().Contain("asp-validation-for=\"Translations[@i].Name\"");
        editSource.Should().Contain("asp-for=\"Translations[@i].Slug\"");
        editSource.Should().Contain("asp-validation-for=\"Translations[@i].Slug\"");
        editSource.Should().Contain("textarea asp-for=\"Translations[@i].Description\"");
        editSource.Should().Contain("string CategoriesIndexUrl() => Url.Action(\"Index\", \"Categories\") ?? string.Empty;");
        editSource.Should().Contain("hx-get=\"@CategoriesIndexUrl()\"");
        editSource.Should().Contain("@T.T(\"Back\")");
        editSource.Should().Contain("@T.T(\"Save\")");
    }


    [Fact]
    public void PageEditorShells_Should_KeepCreateAndEditFormContractsWired()
    {
        var createSource = ReadWebAdminFile(Path.Combine("Views", "Pages", "_PageCreateEditorShell.cshtml"));
        var editSource = ReadWebAdminFile(Path.Combine("Views", "Pages", "_PageEditEditorShell.cshtml"));

        createSource.Should().Contain("id=\"page-editor-shell\"");
        createSource.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        createSource.Should().Contain("@T.T(\"PageCreateTitle\")");
        createSource.Should().Contain("@T.T(\"PageCreateIntro\")");
        createSource.Should().Contain("asp-action=\"Create\"");
        createSource.Should().Contain("hx-post=\"@Url.Action(\"Create\", \"Pages\")\"");
        createSource.Should().Contain("@Html.AntiForgeryToken()");
        createSource.Should().Contain("<partial name=\"_PageForm\" model=\"Model\" />");
        createSource.Should().Contain("data-page-editor-placeholder=\"@T.T(\"PageEditorPlaceholder\")\"");
        createSource.Should().Contain("data-page-image-upload-url=\"@Url.Action(\"UploadQuill\", \"Media\")\"");
        createSource.Should().NotContain("<partial name=\"_PageEditorScript\" />");

        editSource.Should().Contain("id=\"page-editor-shell\"");
        editSource.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        editSource.Should().Contain("@T.T(\"PageEditTitle\")");
        editSource.Should().Contain("@T.T(\"PageEditIntro\")");
        editSource.Should().Contain("asp-action=\"Edit\"");
        editSource.Should().Contain("hx-post=\"@Url.Action(\"Edit\", \"Pages\")\"");
        editSource.Should().Contain("@Html.AntiForgeryToken()");
        editSource.Should().Contain("<input type=\"hidden\" asp-for=\"Id\" />");
        editSource.Should().Contain("<input type=\"hidden\" asp-for=\"RowVersion\" />");
        editSource.Should().Contain("<partial name=\"_PageForm\" model=\"Model\" />");
        editSource.Should().Contain("data-page-editor-placeholder=\"@T.T(\"PageEditorPlaceholder\")\"");
        editSource.Should().Contain("data-page-image-upload-url=\"@Url.Action(\"UploadQuill\", \"Media\")\"");
        editSource.Should().NotContain("<partial name=\"_PageEditorScript\" />");
    }


    [Fact]
    public void PageForm_Should_KeepFieldTranslationAndSubmitContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Pages", "_PageForm.cshtml"));

        source.Should().Contain("<div asp-validation-summary=\"All\" class=\"text-danger mb-3\"></div>");
        source.Should().Contain("@T.T(\"Status\")");
        source.Should().Contain("asp-for=\"Status\" class=\"form-select\"");
        source.Should().Contain("Enum.GetValues(typeof(Darwin.Domain.Enums.PageStatus))");
        source.Should().Contain("selected=\"@(Model.Status.Equals(s))\"");
        source.Should().Contain("@T.T(\"PagePublishStartUtc\")");
        source.Should().Contain("asp-for=\"PublishStartUtc\" type=\"datetime-local\"");
        source.Should().Contain("@T.T(\"PagePublishEndUtc\")");
        source.Should().Contain("asp-for=\"PublishEndUtc\" type=\"datetime-local\"");
        source.Should().Contain("@T.T(\"PageTranslationsHeading\")");
        source.Should().Contain("@T.T(\"PageTranslationsHelp\")");
        source.Should().Contain("@for (int i = 0; i < Model.Translations.Count; i++)");
        source.Should().Contain("name=\"Translations.Index\" value=\"@i\"");
        source.Should().Contain("@T.T(\"Culture\")");
        source.Should().Contain("asp-for=\"Translations[@i].Culture\" class=\"form-select\"");
        source.Should().Contain("if (ViewBag.Cultures is IEnumerable<string> cultures)");
        source.Should().Contain("@T.T(\"Title\")");
        source.Should().Contain("asp-for=\"Translations[@i].Title\" class=\"form-control\"");
        source.Should().Contain("@T.T(\"Slug\")");
        source.Should().Contain("asp-for=\"Translations[@i].Slug\" class=\"form-control\"");
        source.Should().Contain("content=\"@T.T(\"PageSlugHelpContent\")\"");
        source.Should().Contain("@T.T(\"MetaTitle\")");
        source.Should().Contain("asp-for=\"Translations[@i].MetaTitle\" class=\"form-control\"");
        source.Should().Contain("@T.T(\"MetaDescription\")");
        source.Should().Contain("asp-for=\"Translations[@i].MetaDescription\" class=\"form-control\"");
        source.Should().Contain("content=\"@T.T(\"PageMetaDescriptionHelpContent\")\"");
        source.Should().Contain("@T.T(\"ContentHtml\")");
        source.Should().Contain("@T.T(\"PageContentHelpNote\")");
        source.Should().Contain("content=\"@T.T(\"PageContentHelpContent\")\"");
        source.Should().Contain("id=\"page-quill-editor-@i\" class=\"border rounded\" data-page-quill-editor=\"true\"");
        source.Should().Contain("<textarea asp-for=\"Translations[@i].ContentHtml\" class=\"d-none\"></textarea>");
        source.Should().Contain("string PagesIndexUrl() => Url.Action(\"Index\", \"Pages\") ?? string.Empty;");
        source.Should().Contain("hx-get=\"@PagesIndexUrl()\"");
        source.Should().Contain("hx-target=\"#page-editor-shell\"");
        source.Should().Contain("hx-push-url=\"true\">@T.T(\"Back\")</a>");
        source.Should().Contain("<button type=\"submit\" class=\"btn btn-primary\">@T.T(\"Save\")</button>");
    }


    [Fact]
    public void PageEditorScript_Should_KeepQuillUploadAndSubmitSyncContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("wwwroot", "js", "content-editors.js"));

        source.Should().Contain("window.darwinAdmin.initPageEditors = function (root)");
        source.Should().Contain("if (!window.Quill)");
        source.Should().Contain("console.error(options.notLoadedMessage);");
        source.Should().Contain("scope.querySelectorAll(selector).forEach(function (el)");
        source.Should().Contain("initEditors(scope, '[data-page-quill-editor=\"true\"]', {");
        source.Should().Contain("if (el.dataset.quillInitialized === 'true')");
        source.Should().Contain("const quillOptions = {");
        source.Should().Contain("const quill = new Quill(el, quillOptions);");
        source.Should().Contain("theme: 'snow',");
        source.Should().Contain("placeholder: options.placeholder,");
        source.Should().Contain("['link', 'image', 'video']");
        source.Should().Contain("image: function () {");
        source.Should().Contain("input.type = 'file';");
        source.Should().Contain("input.accept = 'image/*';");
        source.Should().Contain("const formData = new FormData();");
        source.Should().Contain("formData.append('file', file);");
        source.Should().Contain("fetch(uploadUrl, {");
        source.Should().Contain("method: 'POST',");
        source.Should().Contain("throw new Error(uploadFailedError);");
        source.Should().Contain("const json = await resp.json();");
        source.Should().Contain("quill.insertEmbed(range.index, 'image', json.url);");
        source.Should().Contain("alert(uploadFailedMessage);");
        source.Should().Contain("const hidden = el.parentElement.querySelector('textarea');");
        source.Should().Contain("quill.root.innerHTML = hidden.value;");
        source.Should().Contain("bindSubmit(el.closest('form'), selector, options.submitDataKey);");
        source.Should().Contain("form.addEventListener('submit', function () {");
        source.Should().Contain("form.querySelectorAll(selector).forEach(function (editorEl)");
        source.Should().Contain("if (editorHidden && editorEl.__quill)");
        source.Should().Contain("editorHidden.value = editorEl.__quill.root.innerHTML;");
        source.Should().Contain("form.dataset[dataKey] = 'true';");
        source.Should().Contain("el.__quill = quill;");
        source.Should().Contain("el.dataset.quillInitialized = 'true';");
        source.Should().Contain("document.addEventListener('DOMContentLoaded', function ()");
        source.Should().Contain("document.body.addEventListener('htmx:afterSwap', function (event)");
    }


    [Fact]
    public void PermissionEditorShells_Should_KeepCreateAndEditFormContractsWired()
    {
        var createSource = ReadWebAdminFile(Path.Combine("Views", "Permissions", "_PermissionCreateEditorShell.cshtml"));
        var editSource = ReadWebAdminFile(Path.Combine("Views", "Permissions", "_PermissionEditEditorShell.cshtml"));

        createSource.Should().Contain("id=\"permission-editor-shell\"");
        createSource.Should().Contain("@T.T(\"CreatePermission\")");
        createSource.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        createSource.Should().Contain("asp-action=\"Create\"");
        createSource.Should().Contain("hx-post=\"@Url.Action(\"Create\", \"Permissions\")\"");
        createSource.Should().Contain("@Html.AntiForgeryToken()");
        createSource.Should().Contain("asp-validation-summary=\"ModelOnly\" class=\"text-danger\"");
        createSource.Should().Contain("asp-for=\"Key\" class=\"form-control\"");
        createSource.Should().Contain("@T.T(\"PermissionKeyHelp\")");
        createSource.Should().Contain("asp-validation-for=\"Key\" class=\"text-danger\"");
        createSource.Should().Contain("asp-for=\"DisplayName\" class=\"form-control\"");
        createSource.Should().Contain("@T.T(\"AdminDisplayNameHelp\")");
        createSource.Should().Contain("asp-for=\"Description\" class=\"form-control\"");
        createSource.Should().Contain("string PermissionsIndexUrl() => Url.Action(\"Index\", \"Permissions\") ?? string.Empty;");
        createSource.Should().Contain("hx-get=\"@PermissionsIndexUrl()\"");
        createSource.Should().Contain("@T.T(\"Back\")");
        createSource.Should().Contain("@T.T(\"Create\")");

        editSource.Should().Contain("id=\"permission-editor-shell\"");
        editSource.Should().Contain("@T.T(\"EditPermission\")");
        editSource.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        editSource.Should().Contain("asp-action=\"Edit\"");
        editSource.Should().Contain("hx-post=\"@Url.Action(\"Edit\", \"Permissions\")\"");
        editSource.Should().Contain("@Html.AntiForgeryToken()");
        editSource.Should().Contain("<input type=\"hidden\" asp-for=\"Id\" />");
        editSource.Should().Contain("<input type=\"hidden\" asp-for=\"Key\" />");
        editSource.Should().Contain("<input type=\"hidden\" asp-for=\"RowVersion\" />");
        editSource.Should().Contain("<input type=\"hidden\" asp-for=\"IsSystem\" />");
        editSource.Should().Contain("value=\"@Model.Key\" readonly");
        editSource.Should().Contain("@T.T(\"ImmutableKeyHelp\")");
        editSource.Should().Contain("@T.T(\"System\")");
        editSource.Should().Contain("@T.T(\"No\")");
        editSource.Should().Contain("data-action=\"@Url.Action(\"Delete\", \"Permissions\")\"");
        editSource.Should().Contain("data-rowversion=\"@Convert.ToBase64String(Model.RowVersion)\"");
        editSource.Should().Contain("data-name=\"@Model.Key\"");
        editSource.Should().Contain("string PermissionsIndexUrl() => Url.Action(\"Index\", \"Permissions\") ?? string.Empty;");
        editSource.Should().Contain("hx-get=\"@PermissionsIndexUrl()\"");
        editSource.Should().Contain("@T.T(\"Back\")");
        editSource.Should().Contain("@T.T(\"Save\")");
        editSource.Should().Contain("<partial name=\"~/Views/Shared/_ConfirmDeleteModal.cshtml\" />");
    }


    [Fact]
    public void ProductEditorAndFormSurfaces_Should_KeepCreateEditFormAndQuillContractsWired()
    {
        var createSource = ReadWebAdminFile(Path.Combine("Views", "Products", "_ProductCreateEditorShell.cshtml"));
        var editSource = ReadWebAdminFile(Path.Combine("Views", "Products", "_ProductEditEditorShell.cshtml"));
        var formSource = ReadWebAdminFile(Path.Combine("Views", "Products", "_ProductForm.cshtml"));

        createSource.Should().Contain("id=\"product-editor-shell\"");
        createSource.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        createSource.Should().Contain("@T.T(\"CreateProduct\")");
        createSource.Should().Contain("@T.T(\"ProductCreateIntro\")");
        createSource.Should().Contain("@T.T(\"Translations\"): @Model.Translations.Count");
        createSource.Should().Contain("@T.T(\"Variants\"): @Model.Variants.Count");
        createSource.Should().Contain("asp-action=\"Create\"");
        createSource.Should().Contain("hx-post=\"@Url.Action(\"Create\", \"Products\")\"");
        createSource.Should().Contain("@Html.AntiForgeryToken()");
        createSource.Should().Contain("<partial name=\"_ProductForm\" model=\"Model\" />");
        createSource.Should().Contain("data-product-description-placeholder=\"@T.T(\"ProductDescriptionPlaceholder\")\"");
        createSource.Should().NotContain("window.darwinAdmin.initProductEditors");
        createSource.Should().NotContain("placeholder: '@T.T(\"ProductDescriptionPlaceholder\")'");

        editSource.Should().Contain("id=\"product-editor-shell\"");
        editSource.Should().Contain("@T.T(\"EditProduct\")");
        editSource.Should().Contain("@T.T(\"ProductEditIntro\")");
        editSource.Should().Contain("@T.T(\"ProductIdentifier\"): @Model.Id");
        editSource.Should().Contain("@T.T(\"ProductEditorTranslationsNote\")");
        editSource.Should().Contain("@T.T(\"ProductEditorVariantsNote\")");
        editSource.Should().Contain("@T.T(\"CatalogReadiness\")");
        editSource.Should().Contain("@T.T(Model.Variants.Count > 1 ? \"VariantRich\" : \"SingleVariant\")");
        editSource.Should().Contain("@T.T(\"ProductEditorReadinessNote\")");
        editSource.Should().Contain("asp-action=\"Edit\"");
        editSource.Should().Contain("hx-post=\"@Url.Action(\"Edit\", \"Products\")\"");
        editSource.Should().Contain("@Html.AntiForgeryToken()");
        editSource.Should().Contain("<input type=\"hidden\" asp-for=\"Id\" />");
        editSource.Should().Contain("<input type=\"hidden\" asp-for=\"RowVersion\" />");
        editSource.Should().Contain("<partial name=\"_ProductForm\" model=\"Model\" />");
        editSource.Should().Contain("data-product-description-placeholder=\"@T.T(\"ProductDescriptionPlaceholder\")\"");

        formSource.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        formSource.Should().Contain("<div asp-validation-summary=\"All\" class=\"text-danger mb-3\"></div>");
        formSource.Should().Contain("@T.T(\"Brand\")");
        formSource.Should().Contain("asp-for=\"BrandId\" class=\"form-select\"");
        formSource.Should().Contain("ViewBag.Brands");
        formSource.Should().Contain("@T.T(\"PrimaryCategory\")");
        formSource.Should().Contain("asp-for=\"PrimaryCategoryId\" class=\"form-select\"");
        formSource.Should().Contain("ViewBag.Categories");
        formSource.Should().Contain("@T.T(\"Kind\")");
        formSource.Should().Contain("Html.GetEnumSelectList<ProductKind>()");
        formSource.Should().Contain("@for (int i = 0; i < Model.Translations.Count; i++)");
        formSource.Should().Contain("name=\"Translations.Index\" value=\"@i\"");
        formSource.Should().Contain("asp-for=\"Translations[@i].Culture\" class=\"form-select\"");
        formSource.Should().Contain("(IEnumerable<string>)ViewBag.Cultures");
        formSource.Should().Contain("asp-for=\"Translations[@i].Name\" class=\"form-control\"");
        formSource.Should().Contain("asp-validation-for=\"Translations[@i].Name\" class=\"text-danger\"");
        formSource.Should().Contain("asp-for=\"Translations[@i].Slug\" class=\"form-control\"");
        formSource.Should().Contain("@T.T(\"ProductSlugHelp\")");
        formSource.Should().Contain("asp-for=\"Translations[@i].MetaDescription\" class=\"form-control\"");
        formSource.Should().Contain("@T.T(\"ProductMetaDescriptionHelp\")");
        formSource.Should().Contain("id=\"quill-editor-desc-@i\" class=\"border rounded\" data-quill-product-editor=\"true\"");
        formSource.Should().Contain("<textarea asp-for=\"Translations[@i].FullDescriptionHtml\" class=\"d-none\"></textarea>");
        formSource.Should().Contain("@for (int j = 0; j < Model.Variants.Count; j++)");
        formSource.Should().Contain("name=\"Variants.Index\" value=\"@j\"");
        formSource.Should().Contain("asp-for=\"Variants[@j].Sku\" class=\"form-control\"");
        formSource.Should().Contain("@T.T(\"ProductSkuHelp\")");
        formSource.Should().Contain("asp-for=\"Variants[@j].Currency\" class=\"form-select\"");
        formSource.Should().Contain("(IEnumerable<string>)ViewBag.Currencies");
        formSource.Should().Contain("asp-for=\"Variants[@j].TaxCategoryId\" class=\"form-select\"");
        formSource.Should().Contain("ViewBag.TaxCategories");
        formSource.Should().Contain("@T.T(\"MinorUnitsHelp\")");
        formSource.Should().Contain("string ProductsIndexUrl() => Url.Action(\"Index\", \"Products\") ?? string.Empty;");
        formSource.Should().Contain("hx-get=\"@ProductsIndexUrl()\"");
        formSource.Should().Contain("@T.T(\"Back\")");
        formSource.Should().Contain("@T.T(\"Save\")");
    }


    [Fact]
    public void BusinessStaffAccessBadgeWorkspace_Should_KeepHeaderPreviewAndContextContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Businesses", "StaffAccessBadge.cshtml"));

        source.Should().Contain("ViewData[\"Title\"] = T.T(\"BusinessStaffAccessBadgeTitle\")");
        source.Should().Contain("id=\"business-staff-access-badge-shell\"");
        source.Should().Contain("@T.T(\"BusinessStaffAccessBadgeTitle\")");
        source.Should().Contain("@StaffAccessBadgeUrl(Model.MembershipId)");
        source.Should().Contain("@T.T(\"RefreshBadgeAction\")");
        source.Should().Contain("@MobileOperationsUrl(Model.Business.Id, Model.UserEmail)");
        source.Should().Contain("@LoyaltyAccountsUrl(Model.UserEmail)");
        source.Should().Contain("@T.T(\"Loyalty\")");
        source.Should().Contain("@T.T(\"BusinessStaffAccessBadgePreviewTitle\")");
        source.Should().Contain("src=\"@Model.BadgeImageDataUrl\"");
        source.Should().Contain("alt=\"@T.T(\"BusinessStaffAccessBadgeQrAlt\")\"");
        source.Should().Contain("string LocalizeBusinessMemberRole(object? role) => role is null ? \"-\" : T.T(role.ToString() ?? string.Empty);");
        source.Should().Contain("@T.T(\"BusinessStaffAccessBadgeMemberRoleLabel\"): @LocalizeBusinessMemberRole(Model.Role)");
        source.Should().Contain("@T.T(\"BusinessStaffAccessBadgeContextTitle\")");
        source.Should().Contain("@T.T(\"BusinessStaffAccessBadgeIssuedAtLabel\")");
        source.Should().Contain("@Model.IssuedAtUtc.ToLocalTime().ToString(CultureInfo.CurrentCulture)");
        source.Should().Contain("@T.T(\"BusinessStaffAccessBadgeExpiresAtLabel\")");
        source.Should().Contain("@Model.ExpiresAtUtc.ToLocalTime().ToString(CultureInfo.CurrentCulture)");
        source.Should().Contain("@T.T(\"BusinessStaffAccessBadgeEmailStateLabel\")");
        source.Should().Contain("@(Model.EmailConfirmed ? T.T(\"BusinessStaffAccessBadgeConfirmed\") : T.T(\"BusinessStaffAccessBadgePendingActivation\"))");
        source.Should().Contain("@T.T(\"BusinessStaffAccessBadgeAccessStateLabel\")");
        source.Should().Contain("@(Model.IsActive ? T.T(\"BusinessStaffAccessBadgeMembershipActive\") : T.T(\"BusinessStaffAccessBadgeMembershipInactive\"))");
        source.Should().Contain("@T.T(\"BusinessStaffAccessBadgePayloadTitle\")");
        source.Should().Contain("rows=\"10\" readonly>@Model.BadgePayload</textarea>");
    }


    [Fact]
    public void BusinessSupportAuditRecommendations_Should_KeepLocalizedGuidanceContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("RecommendedAction = BuildSupportAuditRecommendedAction(x)");
        controllerSource.Should().Contain("private string BuildSupportAuditRecommendedAction(EmailDispatchAuditListItemDto item)");
        controllerSource.Should().Contain("? T(\"BusinessSupportAuditInvitationBusinessAction\")");
        controllerSource.Should().Contain(": T(\"BusinessSupportAuditInvitationGenericAction\")");
        controllerSource.Should().Contain("? T(\"BusinessSupportAuditActivationBusinessAction\")");
        controllerSource.Should().Contain(": T(\"BusinessSupportAuditActivationGenericAction\")");
        controllerSource.Should().Contain("return T(\"BusinessSupportAuditPasswordResetAction\")");
        controllerSource.Should().Contain("return T(\"BusinessSupportAuditGenericAction\")");
    }


    [Fact]
    public void BusinessSetupWorkspace_Should_KeepProfileAndLocalizationDefaultContractsWired()
    {
        var setupShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessSetupShell.cshtml"));

        setupShellSource.Should().Contain("@T.T(\"BusinessProfile\")");
        setupShellSource.Should().Contain("@BusinessCommunicationsDetailsUrl(Model.Id)");
        setupShellSource.Should().Contain("@TaxComplianceUrl(Model.Id)");
        setupShellSource.Should().Contain("@BusinessMerchantReadinessUrl(Model.Id)");
        setupShellSource.Should().Contain("@T.T(\"LocalizationOperationalDefaults\")");
        setupShellSource.Should().Contain("@T.T(\"BusinessSetupLocalizationOwnershipIntro\")");
        setupShellSource.Should().Contain("@SiteSettingsUrl(\"site-settings-localization\")");
        setupShellSource.Should().Contain("string GlobalCrmCustomersUrl() => Url.Action(\"Customers\", \"Crm\") ?? string.Empty;");
        setupShellSource.Should().Contain("@GlobalCrmCustomersUrl()");
        setupShellSource.Should().Contain("@BusinessSupportQueueUrl(Model.Id)");
        setupShellSource.Should().Contain("@T.T(\"CustomerLocaleReview\")");
        setupShellSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
    }


    [Fact]
    public void BusinessSetupWorkspace_Should_KeepBrandingFollowUpContractsWired()
    {
        var setupShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessSetupShell.cshtml"));

        setupShellSource.Should().Contain("@T.T(\"Branding\")");
        setupShellSource.Should().Contain("@T.T(\"BrandDisplayName\")");
        setupShellSource.Should().Contain("@T.T(\"BrandLogoUrl\")");
        setupShellSource.Should().Contain("@SiteSettingsUrl(\"site-settings-business-app\")");
        setupShellSource.Should().Contain("@BusinessCommunicationsDetailsUrl(Model.Id)");
        setupShellSource.Should().Contain("@BusinessMerchantReadinessUrl(Model.Id)");
        setupShellSource.Should().Contain("@T.T(\"BusinessApp\")");
        setupShellSource.Should().Contain("@T.T(\"BusinessCommunicationProfileTitle\")");
        setupShellSource.Should().Contain("@T.T(\"MerchantReadinessTitle\")");
    }


    [Fact]
    public void BusinessSetupWorkspace_Should_KeepOperationalSetupActionPlaybookContractsWired()
    {
        var setupShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessSetupShell.cshtml"));

        setupShellSource.Should().Contain("@T.T(\"OperationalSetupActions\")");
        setupShellSource.Should().Contain("@BusinessCreateMemberUrl(Model.Id)");
        setupShellSource.Should().Contain("@BusinessCreateLocationUrl(Model.Id)");
        setupShellSource.Should().Contain("@BusinessCreateInvitationUrl(Model.Id)");
        setupShellSource.Should().Contain("@BusinessOwnerOverrideAuditsUrl(Model.Id)");
        setupShellSource.Should().Contain("@BusinessMembersUrl(Model.Id, Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        setupShellSource.Should().Contain("@BusinessMembersUrl(Model.Id, Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        setupShellSource.Should().Contain("@BusinessInvitationsUrl(Model.Id, Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending)");
        setupShellSource.Should().Contain("@BusinessInvitationsUrl(Model.Id, Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Expired)");
        setupShellSource.Should().Contain("@BusinessMerchantReadinessUrl(Model.Id)");
        setupShellSource.Should().Contain("string MemberSupportFilterLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        setupShellSource.Should().Contain("@MemberSupportFilterLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        setupShellSource.Should().Contain("@MemberSupportFilterLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        setupShellSource.Should().NotContain("@BusinessMembersUrl(Model.Id, Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)\"\r\n                       hx-target=\"#business-setup-shell\"\r\n                       hx-swap=\"outerHTML\"\r\n                       hx-push-url=\"true\">@T.T(\"PendingActivation\")</a>");
        setupShellSource.Should().NotContain("@BusinessMembersUrl(Model.Id, Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)\"\r\n                       hx-target=\"#business-setup-shell\"\r\n                       hx-swap=\"outerHTML\"\r\n                       hx-push-url=\"true\">@T.T(\"UsersFilterLocked\")</a>");
        setupShellSource.Should().Contain("string InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter filter) => filter switch");
        setupShellSource.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending)");
        setupShellSource.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending)");
        setupShellSource.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Expired)");
        setupShellSource.Should().Contain("hx-push-url=\"true\">@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending)</a>");
        setupShellSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"Pending\")</a>");
        setupShellSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"Expired\")</a>");
        setupShellSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"Invitations\")</a>");
    }


    [Fact]
    public void BusinessSetupWorkspace_Should_KeepCommunicationDefaultsAndPlatformDependencyContractsWired()
    {
        var setupShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessSetupShell.cshtml"));

        setupShellSource.Should().Contain("@T.T(\"BusinessSetupPlatformDependenciesIntro\")");
        setupShellSource.Should().Contain("@T.T(\"BusinessSetupPlatformDependenciesRule\")");
        setupShellSource.Should().Contain("@BusinessMerchantReadinessUrl(Model.Id)");
        setupShellSource.Should().Contain("@BusinessSupportQueueUrl(Model.Id)");
        setupShellSource.Should().Contain("@SiteSettingsUrl(\"site-settings-communications-policy\")");
        setupShellSource.Should().Contain("@BusinessEditUrl(Model.Id)");
        setupShellSource.Should().Contain("@T.T(\"OpenGlobalSettings\")");
        setupShellSource.Should().Contain("@T.T(\"EditBusiness\")");
        setupShellSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
        setupShellSource.Should().Contain("@T.T(\"BusinessCommunicationDefaults\")");
        setupShellSource.Should().Contain("@T.T(\"BusinessCommunicationDefaultsHelp\")");
        setupShellSource.Should().Contain("@BusinessCommunicationsDetailsUrl(Model.Id)");
        setupShellSource.Should().Contain("@EmailAuditsUrl(Model.Id)");
        setupShellSource.Should().Contain("@ChannelAuditsUrl(Model.Id)");
        setupShellSource.Should().Contain("@T.T(\"BusinessSetupPaymentsShippingTitle\")");
        setupShellSource.Should().Contain("@SiteSettingsUrl(\"site-settings-payments\")");
        setupShellSource.Should().Contain("@SiteSettingsUrl(\"site-settings-shipping\")");
        setupShellSource.Should().Contain("@PaymentsUrl(Model.Id)");
        setupShellSource.Should().Contain("@T.T(\"OpenPayments\")");
        setupShellSource.Should().Contain("@T.T(\"BusinessSetupSecurityMobileAccessTitle\")");
        setupShellSource.Should().Contain("@SiteSettingsUrl(\"site-settings-security\")");
        setupShellSource.Should().Contain("@SiteSettingsUrl(\"site-settings-mobile\")");
        setupShellSource.Should().Contain("@MobileOperationsUrl(Model.Id)");
        setupShellSource.Should().Contain("@BusinessMembersUrl(Model.Id, Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        setupShellSource.Should().Contain("@T.T(\"MobileOperationsTitle\")");
        setupShellSource.Should().Contain("@MemberSupportFilterLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        setupShellSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"UsersFilterLocked\")</a>");
        setupShellSource.Should().Contain("@T.T(\"Communications\")");
        setupShellSource.Should().Contain("@SiteSettingsUrl(\"site-settings-smtp\")");
        setupShellSource.Should().Contain("@SiteSettingsUrl(\"site-settings-sms\")");
        setupShellSource.Should().Contain("@SiteSettingsUrl(\"site-settings-whatsapp\")");
        setupShellSource.Should().Contain("string CommunicationChannelLabel(string channel) => string.Equals(channel, \"WhatsApp\", StringComparison.OrdinalIgnoreCase)");
        setupShellSource.Should().Contain("? T.T(\"BusinessCommunicationWhatsAppShort\")");
        setupShellSource.Should().Contain(": T.T(\"SMS\");");
        setupShellSource.Should().Contain("@CommunicationChannelLabel(\"SMS\")");
        setupShellSource.Should().Contain("@CommunicationChannelLabel(\"WhatsApp\")");
        setupShellSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"SMS\")</a>");
        setupShellSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"WhatsApp\")</a>");
        setupShellSource.Should().Contain("@BusinessCommunicationsDetailsUrl(Model.Id)");
        setupShellSource.Should().Contain("@EmailAuditsUrl(Model.Id)");
        setupShellSource.Should().Contain("@ChannelAuditsUrl(Model.Id)");
    }


    [Fact]
    public void BusinessSetupWorkspace_Should_KeepFooterFollowUpContractsWired()
    {
        var setupShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessSetupShell.cshtml"));

        setupShellSource.Should().Contain("@T.T(\"BusinessSetupSaveAction\")");
        setupShellSource.Should().Contain("@BusinessEditUrl(Model.Id)");
        setupShellSource.Should().Contain("@T.T(\"BusinessMembersBackToBusinessAction\")");
        setupShellSource.Should().Contain("@BusinessMerchantReadinessUrl(Model.Id)");
        setupShellSource.Should().Contain("@BusinessSupportQueueUrl(Model.Id)");
        setupShellSource.Should().Contain("@T.T(\"MerchantReadinessTitle\")");
        setupShellSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
    }


    [Fact]
    public void BusinessLocationAndInvitationEditorShells_Should_KeepWorkspacePivotContractsWired()
    {
        var locationShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessLocationEditorShell.cshtml"));
        var invitationShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessInvitationEditorShell.cshtml"));

        locationShellSource.Should().Contain("@BusinessLocationsUrl(Model.BusinessId, Model.Page, Model.PageSize, Model.Query, Model.Filter)");
        locationShellSource.Should().Contain("@BusinessSetupUrl(Model.BusinessId)");
        locationShellSource.Should().Contain("@BusinessMerchantReadinessUrl(Model.BusinessId)");
        locationShellSource.Should().Contain("@T.T(\"BusinessLocationBackToLocations\")");
        locationShellSource.Should().Contain("@T.T(\"Setup\")");
        locationShellSource.Should().Contain("@T.T(\"MerchantReadinessTitle\")");
        locationShellSource.Should().Contain("mt-4");

        invitationShellSource.Should().Contain("@BusinessInvitationsUrl(Model.BusinessId, Model.Page, Model.PageSize, Model.Query, Model.Filter)");
        invitationShellSource.Should().Contain("@BusinessSupportQueueUrl(Model.BusinessId)");
        invitationShellSource.Should().Contain("@BusinessMerchantReadinessUrl(Model.BusinessId)");
        invitationShellSource.Should().Contain("@T.T(\"BusinessInvitationBackToInvitations\")");
        invitationShellSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
        invitationShellSource.Should().Contain("@T.T(\"MerchantReadinessTitle\")");
        invitationShellSource.Should().Contain("mt-4");
    }


    [Fact]
    public void BusinessLocationEditorShell_Should_KeepFormPostContractWired()
    {
        var locationShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessLocationEditorShell.cshtml"));

        locationShellSource.Should().Contain("<form asp-action=\"@(isCreate ? \"CreateLocation\" : \"EditLocation\")\"");
        locationShellSource.Should().Contain("hx-post=\"@Url.Action(isCreate ? \"CreateLocation\" : \"EditLocation\", \"Businesses\")\"");
        locationShellSource.Should().Contain("@Html.AntiForgeryToken()");
        locationShellSource.Should().Contain("<input type=\"hidden\" asp-for=\"BusinessId\" />");
        locationShellSource.Should().Contain("<input type=\"hidden\" asp-for=\"RowVersion\" />");
        locationShellSource.Should().Contain("<partial name=\"_BusinessLocationForm\" model=\"Model\" />");
    }


    [Fact]
    public void BusinessInvitationEditorShell_Should_KeepFormPostContractWired()
    {
        var invitationShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessInvitationEditorShell.cshtml"));

        invitationShellSource.Should().Contain("<form asp-action=\"CreateInvitation\"");
        invitationShellSource.Should().Contain("hx-post=\"@Url.Action(\"CreateInvitation\", \"Businesses\")\"");
        invitationShellSource.Should().Contain("@Html.AntiForgeryToken()");
        invitationShellSource.Should().Contain("<input type=\"hidden\" asp-for=\"BusinessId\" />");
        invitationShellSource.Should().Contain("<partial name=\"_BusinessInvitationForm\" model=\"Model\" />");
    }


    [Fact]
    public void BusinessLocationForm_Should_KeepFieldAndSubmitContractsWired()
    {
        var locationFormSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessLocationForm.cshtml"));

        locationFormSource.Should().Contain("@T.T(\"BusinessLocationName\")");
        locationFormSource.Should().Contain("@T.T(\"BusinessLocationPostalCode\")");
        locationFormSource.Should().Contain("@T.T(\"BusinessLocationAddressLine1\")");
        locationFormSource.Should().Contain("@T.T(\"BusinessLocationAddressLine2\")");
        locationFormSource.Should().Contain("@T.T(\"BusinessLocationCity\")");
        locationFormSource.Should().Contain("@T.T(\"BusinessLocationRegion\")");
        locationFormSource.Should().Contain("@T.T(\"BusinessLocationCountryCode\")");
        locationFormSource.Should().Contain("@T.T(\"BusinessLocationLatitude\")");
        locationFormSource.Should().Contain("@T.T(\"BusinessLocationLongitude\")");
        locationFormSource.Should().Contain("@T.T(\"BusinessLocationAltitudeMeters\")");
        locationFormSource.Should().Contain("@T.T(\"BusinessLocationOpeningHoursJson\")");
        locationFormSource.Should().Contain("@T.T(\"BusinessLocationInternalNote\")");
        locationFormSource.Should().Contain("@T.T(\"BusinessLocationPrimary\")");
        locationFormSource.Should().Contain("@T.T(\"Save\")");
    }


    [Fact]
    public void BusinessInvitationForm_Should_KeepFieldAndSubmitContractsWired()
    {
        var invitationFormSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessInvitationForm.cshtml"));

        invitationFormSource.Should().Contain("@T.T(\"BusinessInvitationEmail\")");
        invitationFormSource.Should().Contain("@T.T(\"BusinessInvitationRole\")");
        invitationFormSource.Should().Contain("@T.T(\"BusinessInvitationExpiresInDays\")");
        invitationFormSource.Should().Contain("@T.T(\"BusinessInvitationInternalNote\")");
        invitationFormSource.Should().Contain("@T.T(\"BusinessInvitationSend\")");
        invitationFormSource.Should().Contain("@T.T(\"Cancel\")");
    }


    [Fact]
    public void BusinessMemberEditorShell_Should_KeepFormPostContractWired()
    {
        var memberShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessMemberEditorShell.cshtml"));

        memberShellSource.Should().Contain("<form asp-action=\"@(isCreate ? \"CreateMember\" : \"EditMember\")\"");
        memberShellSource.Should().Contain("hx-post=\"@Url.Action(isCreate ? \"CreateMember\" : \"EditMember\", \"Businesses\")\"");
        memberShellSource.Should().Contain("@Html.AntiForgeryToken()");
        memberShellSource.Should().Contain("<input type=\"hidden\" asp-for=\"BusinessId\" />");
        memberShellSource.Should().Contain("<input type=\"hidden\" asp-for=\"UserId\" />");
        memberShellSource.Should().Contain("<input type=\"hidden\" name=\"RowVersion\" value=\"@(Model.RowVersion is null ? string.Empty : Convert.ToBase64String(Model.RowVersion))\" />");
        memberShellSource.Should().Contain("<partial name=\"_BusinessMemberForm\" model=\"Model\" />");
    }


    [Fact]
    public void BusinessMemberEditorShell_Should_KeepOwnerOverrideForceDeleteContractWired()
    {
        var memberShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessMemberEditorShell.cshtml"));

        memberShellSource.Should().Contain("<form asp-action=\"ForceDeleteMember\"");
        memberShellSource.Should().Contain("hx-post=\"@Url.Action(\"ForceDeleteMember\", \"Businesses\")\"");
        memberShellSource.Should().Contain("<input type=\"hidden\" name=\"rowVersion\" value=\"@Convert.ToBase64String(Model.RowVersion)\" />");
        memberShellSource.Should().Contain("@T.T(\"BusinessMemberOverrideReasonLabel\")");
        memberShellSource.Should().Contain("placeholder=\"@T.T(\"BusinessMemberOverrideReasonPlaceholder\")\"");
        memberShellSource.Should().Contain("@T.T(\"BusinessMemberForceRemoveLastOwnerAction\")");
    }


    [Fact]
    public void SharedIdentitySupportingViewModels_Should_KeepAddressRoleAndPermissionContractShapesWired()
    {
        var addressVmsSource = ReadWebAdminFile(Path.Combine("ViewModels", "Identity", "AddressVms.cs"));
        var permissionVmsSource = ReadWebAdminFile(Path.Combine("ViewModels", "Identity", "PermissionVms.cs"));
        var roleVmsSource = ReadWebAdminFile(Path.Combine("ViewModels", "Identity", "RoleVms.cs"));
        var rolePermissionVmsSource = ReadWebAdminFile(Path.Combine("ViewModels", "Identity", "RolePermissionVms.cs"));
        var userRoleVmsSource = ReadWebAdminFile(Path.Combine("ViewModels", "Identity", "UserRoleVms.cs"));

        addressVmsSource.Should().Contain("public sealed class UserAddressesSectionVm");
        addressVmsSource.Should().Contain("public Guid UserId { get; set; }");
        addressVmsSource.Should().Contain("public List<UserAddressListItemVm> Items { get; set; } = new();");
        addressVmsSource.Should().Contain("public sealed class UserAddressListItemVm");
        addressVmsSource.Should().Contain("public byte[] RowVersion { get; set; } = Array.Empty<byte>();");
        addressVmsSource.Should().Contain("public string CountryCode { get; set; } = SiteSettingDto.DefaultCountryDefault;");
        addressVmsSource.Should().Contain("public bool IsDefaultBilling { get; set; }");
        addressVmsSource.Should().Contain("public bool IsDefaultShipping { get; set; }");
        addressVmsSource.Should().Contain("public sealed class UserAddressCreateVm");
        addressVmsSource.Should().Contain("public sealed class UserAddressEditVm");

        permissionVmsSource.Should().Contain("public enum PermissionQueueFilter");
        permissionVmsSource.Should().Contain("DelegatedSupport = 3");
        permissionVmsSource.Should().Contain("public sealed class PermissionsListVm");
        permissionVmsSource.Should().Contain("public PermissionQueueFilter Filter { get; set; } = PermissionQueueFilter.All;");
        permissionVmsSource.Should().Contain("public PermissionOpsSummaryVm Summary { get; set; } = new();");
        permissionVmsSource.Should().Contain("public IEnumerable<SelectListItem> PageSizeItems { get; set; } = new List<SelectListItem>();");
        permissionVmsSource.Should().Contain("public sealed class PermissionEditVm");
        permissionVmsSource.Should().Contain("public byte[] RowVersion { get; set; } = Array.Empty<byte>();");
        permissionVmsSource.Should().Contain("public bool IsSystem { get; set; }");

        roleVmsSource.Should().Contain("public enum RoleQueueFilter");
        roleVmsSource.Should().Contain("DelegatedSupport = 3");
        roleVmsSource.Should().Contain("public sealed class RolesListItemVm");
        roleVmsSource.Should().Contain("public RoleQueueFilter Filter { get; set; } = RoleQueueFilter.All;");
        roleVmsSource.Should().Contain("public RoleOpsSummaryVm Summary { get; set; } = new();");
        roleVmsSource.Should().Contain("public IEnumerable<SelectListItem> FilterItems { get; set; } = new List<SelectListItem>();");
        roleVmsSource.Should().Contain("public sealed class RoleEditVm");
        roleVmsSource.Should().Contain("public byte[] RowVersion { get; set; } = Array.Empty<byte>();");
        roleVmsSource.Should().Contain("public bool IsSystem { get; set; }");

        rolePermissionVmsSource.Should().Contain("public sealed class RolePermissionsEditVm");
        rolePermissionVmsSource.Should().Contain("public Guid RoleId { get; set; }");
        rolePermissionVmsSource.Should().Contain("public byte[] RowVersion { get; set; } = Array.Empty<byte>();");
        rolePermissionVmsSource.Should().Contain("public List<Guid> SelectedPermissionIds { get; set; } = new();");
        rolePermissionVmsSource.Should().Contain("public List<PermissionItemVm> AllPermissions { get; set; } = new();");
        rolePermissionVmsSource.Should().Contain("public sealed class PermissionItemVm");
        rolePermissionVmsSource.Should().Contain("public string Key { get; set; } = string.Empty;");

        userRoleVmsSource.Should().Contain("public sealed class UserRolesEditVm");
        userRoleVmsSource.Should().Contain("public Guid UserId { get; set; }");
        userRoleVmsSource.Should().Contain("public byte[] RowVersion { get; set; } = Array.Empty<byte>();");
        userRoleVmsSource.Should().Contain("public List<Guid> SelectedRoleIds { get; set; } = new();");
        userRoleVmsSource.Should().Contain("public List<RoleItemVm> AllRoles { get; set; } = new();");
        userRoleVmsSource.Should().Contain("public UserQueueFilter Filter { get; set; } = UserQueueFilter.All;");
        userRoleVmsSource.Should().Contain("public int Page { get; set; } = 1;");
        userRoleVmsSource.Should().Contain("public int PageSize { get; set; } = 20;");
        userRoleVmsSource.Should().Contain("public sealed class RoleItemVm");
        userRoleVmsSource.Should().Contain("public byte[] RowVersion { get; set; } = Array.Empty<byte>();");
    }


    [Fact]
    public void SharedCatalogApplicableAddOnsViewModels_Should_KeepResolvedSelectionContractShapesWired()
    {
        var productApplicableAddOnsVmsSource = ReadWebAdminFile(Path.Combine("ViewModels", "Catalog", "ProductApplicableAddOnsVms.cs"));

        productApplicableAddOnsVmsSource.Should().Contain("public sealed class ProductApplicableAddOnsVm");
        productApplicableAddOnsVmsSource.Should().Contain("public Guid ProductId { get; set; }");
        productApplicableAddOnsVmsSource.Should().Contain("public Guid? VariantId { get; set; }");
        productApplicableAddOnsVmsSource.Should().Contain("public List<ApplicableAddOnGroupVm> Groups { get; set; } = new();");
        productApplicableAddOnsVmsSource.Should().Contain("public sealed class ApplicableAddOnGroupVm");
        productApplicableAddOnsVmsSource.Should().Contain("public string Currency { get; set; } = string.Empty;");
        productApplicableAddOnsVmsSource.Should().Contain("public AddOnSelectionMode SelectionMode { get; set; } = AddOnSelectionMode.Single;");
        productApplicableAddOnsVmsSource.Should().Contain("public int MinSelections { get; set; }");
        productApplicableAddOnsVmsSource.Should().Contain("public int? MaxSelections { get; set; }");
        productApplicableAddOnsVmsSource.Should().Contain("public List<ApplicableAddOnOptionVm> Options { get; set; } = new();");
        productApplicableAddOnsVmsSource.Should().Contain("public sealed class ApplicableAddOnOptionVm");
        productApplicableAddOnsVmsSource.Should().Contain("public List<ApplicableAddOnOptionValueVm> Values { get; set; } = new();");
        productApplicableAddOnsVmsSource.Should().Contain("public sealed class ApplicableAddOnOptionValueVm");
        productApplicableAddOnsVmsSource.Should().Contain("public long PriceDeltaMinor { get; set; }");
        productApplicableAddOnsVmsSource.Should().Contain("public string? Hint { get; set; }");
        productApplicableAddOnsVmsSource.Should().Contain("public int SortOrder { get; set; }");
    }


    [Fact]
    public void AddOnGroupAttachSurfaces_Should_KeepCategoryBrandAndVariantSelectionContractsWired()
    {
        var attachCategoriesSource = ReadWebAdminFile(Path.Combine("Views", "AddOnGroups", "AttachToCategories.cshtml"));
        var attachBrandsSource = ReadWebAdminFile(Path.Combine("Views", "AddOnGroups", "AttachToBrands.cshtml"));
        var attachVariantsSource = ReadWebAdminFile(Path.Combine("Views", "AddOnGroups", "AttachToVariants.cshtml"));

        attachCategoriesSource.Should().Contain("id=\"add-on-group-attach-categories-shell\"");
        attachCategoriesSource.Should().Contain("asp-action=\"AttachToCategories\"");
        attachCategoriesSource.Should().Contain("string AttachAddOnGroupToCategoriesSearchUrl() => Url.Action(\"AttachToCategories\", \"AddOnGroups\") ?? string.Empty;");
        attachCategoriesSource.Should().Contain("string AttachAddOnGroupToCategoriesUrl(Guid id) => Url.Action(\"AttachToCategories\", \"AddOnGroups\", new { id }) ?? string.Empty;");
        attachCategoriesSource.Should().Contain("hx-get=\"@AttachAddOnGroupToCategoriesSearchUrl()\"");
        attachCategoriesSource.Should().Contain("hx-get=\"@AttachAddOnGroupToCategoriesUrl(Model.AddOnGroupId)\"");
        attachCategoriesSource.Should().Contain("name=\"SelectedCategoryIds\"");
        attachCategoriesSource.Should().Contain("@T.T(\"AddOnNoCategories\")");
        attachCategoriesSource.Should().Contain("@T.T(\"AddOnSaveAttachments\")");
        attachCategoriesSource.Should().Contain("string AddOnGroupsIndexUrl() => Url.Action(\"Index\", \"AddOnGroups\") ?? string.Empty;");
        attachCategoriesSource.Should().Contain("hx-get=\"@AddOnGroupsIndexUrl()\"");
        attachCategoriesSource.Should().Contain("asp-route-id=\"@Model.AddOnGroupId\"");
        attachCategoriesSource.Should().Contain("hx-target=\"#add-on-group-attach-categories-shell\"");

        attachBrandsSource.Should().Contain("id=\"add-on-group-attach-brands-shell\"");
        attachBrandsSource.Should().Contain("asp-action=\"AttachToBrands\"");
        attachBrandsSource.Should().Contain("string AttachAddOnGroupToBrandsSearchUrl() => Url.Action(\"AttachToBrands\", \"AddOnGroups\") ?? string.Empty;");
        attachBrandsSource.Should().Contain("string AttachAddOnGroupToBrandsUrl(Guid id) => Url.Action(\"AttachToBrands\", \"AddOnGroups\", new { id }) ?? string.Empty;");
        attachBrandsSource.Should().Contain("hx-get=\"@AttachAddOnGroupToBrandsSearchUrl()\"");
        attachBrandsSource.Should().Contain("hx-get=\"@AttachAddOnGroupToBrandsUrl(Model.AddOnGroupId)\"");
        attachBrandsSource.Should().Contain("name=\"SelectedBrandIds\"");
        attachBrandsSource.Should().Contain("@T.T(\"AddOnNoBrands\")");
        attachBrandsSource.Should().Contain("@T.T(\"AddOnSaveAttachments\")");
        attachBrandsSource.Should().Contain("string AddOnGroupsIndexUrl() => Url.Action(\"Index\", \"AddOnGroups\") ?? string.Empty;");
        attachBrandsSource.Should().Contain("hx-get=\"@AddOnGroupsIndexUrl()\"");
        attachBrandsSource.Should().Contain("asp-route-id=\"@Model.AddOnGroupId\"");
        attachBrandsSource.Should().Contain("hx-target=\"#add-on-group-attach-brands-shell\"");

        attachVariantsSource.Should().Contain("id=\"add-on-group-attach-variants-shell\"");
        attachVariantsSource.Should().Contain("string AttachAddOnGroupToVariantsUrl() => Url.Action(\"AttachToVariants\", \"AddOnGroups\") ?? string.Empty;");
        attachVariantsSource.Should().Contain("hx-get=\"@AttachAddOnGroupToVariantsUrl()\"");
        attachVariantsSource.Should().Contain("string AddOnGroupsIndexUrl() => Url.Action(\"Index\", \"AddOnGroups\") ?? string.Empty;");
        attachVariantsSource.Should().Contain("hx-get=\"@AddOnGroupsIndexUrl()\"");
        attachVariantsSource.Should().Contain("name=\"q\" value=\"@Model.Query\"");
        attachVariantsSource.Should().Contain("id=\"selected-container\"");
        attachVariantsSource.Should().Contain("name=\"SelectedVariantIds\"");
        attachVariantsSource.Should().Contain("asp-for=\"RowVersion\"");
        attachVariantsSource.Should().Contain("asp-route-q=\"@Model.Query\"");
        attachCategoriesSource.Should().Contain("data-addon-selection-scope");
        attachCategoriesSource.Should().Contain("data-addon-toggle-all");
        attachCategoriesSource.Should().Contain("data-addon-row-check");
        attachBrandsSource.Should().Contain("data-addon-selection-scope");
        attachBrandsSource.Should().Contain("data-addon-toggle-all");
        attachBrandsSource.Should().Contain("data-addon-row-check");
        attachVariantsSource.Should().Contain("data-addon-variant-selection");
    }


    [Fact]
    public void AddOnGroupOptionsEditor_Should_KeepNestedOptionAndValueTemplateContractsWired()
    {
        var optionsEditorSource = ReadWebAdminFile(Path.Combine("Views", "AddOnGroups", "_OptionsEditor.cshtml"));
        var addOnGroupsJsSource = ReadWebAdminFile(Path.Combine("wwwroot", "js", "add-on-groups.js"));

        optionsEditorSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Catalog.AddOnGroupEditorVm");
        optionsEditorSource.Should().Contain("id=\"options-editor\" data-options-prefix=\"Options\"");
        optionsEditorSource.Should().Contain("@T.T(\"AddOnOptionsValues\")");
        optionsEditorSource.Should().Contain("@T.T(\"AddOnOptionsEditorHelp\")");
        optionsEditorSource.Should().Contain("name=\"Options[@i].Label\"");
        optionsEditorSource.Should().Contain("name=\"Options[@i].Values[@j].Label\"");
        optionsEditorSource.Should().Contain("name=\"Options[@i].Values[@j].PriceDeltaMinor\"");
        optionsEditorSource.Should().Contain("name=\"Options[@i].Values[@j].IsActive\" value=\"false\"");
        optionsEditorSource.Should().Contain("data-remove-option");
        optionsEditorSource.Should().Contain("data-add-value");
        optionsEditorSource.Should().Contain("data-remove-value");
        optionsEditorSource.Should().Contain("id=\"addOnOptionTemplate\"");
        optionsEditorSource.Should().Contain("id=\"addOnValueTemplate\"");
        optionsEditorSource.Should().Contain("name=\"__prefix__[__i__].Values[__j__].PriceDeltaMinor\"");
        optionsEditorSource.Should().Contain("<div class=\"values-container\">__value__</div>");
        optionsEditorSource.Should().NotContain("<script>");
        addOnGroupsJsSource.Should().Contain("function initOptionsEditor(root)");
        addOnGroupsJsSource.Should().Contain("const prefix = editor.getAttribute('data-options-prefix') || 'Options';");
        addOnGroupsJsSource.Should().Contain("return renderTemplate(optionTemplate, prefix, optionIndex, 0, renderValue(optionIndex, 0));");
        addOnGroupsJsSource.Should().Contain("valuesContainer.insertAdjacentHTML('beforeend', renderValue(optionIndex, valueIndex));");
        addOnGroupsJsSource.Should().Contain("target.closest('.value-item')?.remove();");
    }


    [Fact]
    public void AddOnGroupEditorFormAndShells_Should_KeepCreateEditAndUppercaseCurrencyContractsWired()
    {
        var createShellSource = ReadWebAdminFile(Path.Combine("Views", "AddOnGroups", "_AddOnGroupCreateEditorShell.cshtml"));
        var editShellSource = ReadWebAdminFile(Path.Combine("Views", "AddOnGroups", "_AddOnGroupEditEditorShell.cshtml"));
        var formSource = ReadWebAdminFile(Path.Combine("Views", "AddOnGroups", "_AddOnGroupForm.cshtml"));
        var addOnGroupsJsSource = ReadWebAdminFile(Path.Combine("wwwroot", "js", "add-on-groups.js"));

        createShellSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Catalog.AddOnGroupCreateVm");
        createShellSource.Should().Contain("id=\"add-on-group-editor-shell\"");
        createShellSource.Should().Contain("asp-action=\"Create\"");
        createShellSource.Should().Contain("hx-post=\"@Url.Action(\"Create\", \"AddOnGroups\")\"");
        createShellSource.Should().Contain("<partial name=\"_AddOnGroupForm\" model=\"Model\" />");
        createShellSource.Should().NotContain("<script>");

        editShellSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Catalog.AddOnGroupEditVm");
        editShellSource.Should().Contain("asp-action=\"Edit\"");
        editShellSource.Should().Contain("hx-post=\"@Url.Action(\"Edit\", \"AddOnGroups\")\"");
        editShellSource.Should().Contain("<input type=\"hidden\" asp-for=\"Id\" />");
        editShellSource.Should().Contain("<input type=\"hidden\" asp-for=\"RowVersion\" />");
        editShellSource.Should().Contain("<partial name=\"_AddOnGroupForm\" model=\"Model\" />");
        editShellSource.Should().NotContain("<script>");

        formSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Catalog.AddOnGroupEditorVm");
        formSource.Should().Contain("asp-validation-summary=\"All\"");
        formSource.Should().Contain("asp-for=\"Name\"");
        formSource.Should().Contain("asp-for=\"Currency\"");
        formSource.Should().Contain("maxlength=\"3\"");
        formSource.Should().Contain("data-addon-currency-uppercase");
        formSource.Should().Contain("asp-for=\"SelectionMode\"");
        formSource.Should().Contain("var selectionModeOptions = Html.GetEnumSelectList<Darwin.Domain.Enums.AddOnSelectionMode>().Select");
        formSource.Should().Contain("Text = T.T(option.Text)");
        formSource.Should().Contain("asp-items=\"selectionModeOptions\"");
        formSource.Should().Contain("asp-for=\"MinSelections\"");
        formSource.Should().Contain("asp-for=\"MaxSelections\"");
        formSource.Should().Contain("asp-for=\"IsGlobal\"");
        formSource.Should().Contain("asp-for=\"IsActive\"");
        formSource.Should().Contain("<partial name=\"_OptionsEditor\" model=\"Model\" />");
        formSource.Should().Contain("hx-target=\"#add-on-groups-workspace-shell\"");
        formSource.Should().Contain("@T.T(\"Save\")");
        formSource.Should().Contain("@T.T(\"Back\")");

        addOnGroupsJsSource.Should().Contain("event.target.closest('[data-addon-toggle-all]')");
        addOnGroupsJsSource.Should().Contain("scope.querySelectorAll('[data-addon-row-check]')");
        addOnGroupsJsSource.Should().Contain("[data-addon-variant-selection]");
        addOnGroupsJsSource.Should().Contain("hidden.dataset.bindId =");
        addOnGroupsJsSource.Should().Contain("event.target.closest('[data-addon-currency-uppercase]')");
        addOnGroupsJsSource.Should().Contain("input.value = input.value.toUpperCase();");
    }


    [Fact]
    public void SharedServiceInterfaces_Should_KeepDocumentationAndMethodFloorContractsWired()
    {
        var siteSettingCacheInterfaceSource = ReadWebAdminFile(Path.Combine("Services", "Settings", "ISiteSettingCache.cs"));
        var canonicalUrlInterfaceSource = ReadWebAdminFile(Path.Combine("Services", "Seo", "ICanonicalUrlService.cs"));

        siteSettingCacheInterfaceSource.Should().Contain("public interface ISiteSettingCache");
        siteSettingCacheInterfaceSource.Should().Contain("Task<SiteSettingDto> GetAsync(CancellationToken ct = default);");
        siteSettingCacheInterfaceSource.Should().Contain("void Invalidate();");
        siteSettingCacheInterfaceSource.Should().Contain("Admin controllers (dropdown options), UI helpers (formatting, culture), SEO services (canonical/hreflang).");

        canonicalUrlInterfaceSource.Should().Contain("public interface ICanonicalUrlService");
        canonicalUrlInterfaceSource.Should().Contain("string Page(string culture, string slug);");
        canonicalUrlInterfaceSource.Should().Contain("string Category(string culture, string slug);");
        canonicalUrlInterfaceSource.Should().Contain("string Product(string culture, string slug);");
        canonicalUrlInterfaceSource.Should().Contain("string Absolute(string relative);");
    }


    [Fact]
    public void BusinessSubscriptionInvoicesWorkspace_Should_KeepSearchFilterAndQueueChipContractsWired()
    {
        var subscriptionInvoicesViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "SubscriptionInvoices.cshtml"));

        subscriptionInvoicesViewSource.Should().Contain("id=\"@InvoiceRootId()\"");
        subscriptionInvoicesViewSource.Should().Contain("action=\"@InvoiceQueueActionHref()\"");
        subscriptionInvoicesViewSource.Should().Contain("hx-get=\"@InvoiceQueueActionHref()\"");
        subscriptionInvoicesViewSource.Should().Contain("name=\"@InvoiceFilterBusinessFieldName()\" value=\"@InvoiceFilterBusinessIdValue()\"");
        subscriptionInvoicesViewSource.Should().Contain("name=\"@InvoiceFilterQueryName()\" value=\"@InvoiceFilterQueryValue()\"");
        subscriptionInvoicesViewSource.Should().Contain("placeholder=\"@InvoiceFilterPlaceholderText()\"");
        subscriptionInvoicesViewSource.Should().Contain("name=\"@InvoiceFilterQueueName()\" asp-items=\"@InvoiceFilterQueueItems()\"");
        subscriptionInvoicesViewSource.Should().Contain("@InvoiceFilterSubmitText()");
        subscriptionInvoicesViewSource.Should().Contain("@InvoiceFilterResetText()");
        subscriptionInvoicesViewSource.Should().Contain("@InvoiceQueueChipClass(InvoiceSelectedQueueFilter(), BusinessSubscriptionInvoiceQueueFilter.All)");
        subscriptionInvoicesViewSource.Should().Contain("@InvoiceQueueChipClass(InvoiceSelectedQueueFilter(), BusinessSubscriptionInvoiceQueueFilter.Open)");
        subscriptionInvoicesViewSource.Should().Contain("@InvoiceQueueChipClass(InvoiceSelectedQueueFilter(), BusinessSubscriptionInvoiceQueueFilter.Overdue)");
        subscriptionInvoicesViewSource.Should().Contain("@InvoiceQueueChipClass(InvoiceSelectedQueueFilter(), BusinessSubscriptionInvoiceQueueFilter.PdfMissing)");
        subscriptionInvoicesViewSource.Should().Contain("@InvoiceQueueChipClass(InvoiceSelectedQueueFilter(), BusinessSubscriptionInvoiceQueueFilter.Stripe)");
        subscriptionInvoicesViewSource.Should().Contain("@InvoiceQueueChipClass(InvoiceSelectedQueueFilter(), BusinessSubscriptionInvoiceQueueFilter.Uncollectible)");
        subscriptionInvoicesViewSource.Should().Contain("string InvoiceRefundsActionHref() => Url.Action(\"Refunds\", \"Billing\", new { businessId = InvoicePageBusinessIdValue() }) ?? string.Empty;");
        subscriptionInvoicesViewSource.Should().Contain("hx-get=\"@InvoiceRefundsActionHref()\"");
        subscriptionInvoicesViewSource.Should().Contain("@InvoiceRefundsActionText()");
        subscriptionInvoicesViewSource.Should().Contain("string InvoicePaymentSettingsActionFragment() => \"site-settings-payments\";");
        subscriptionInvoicesViewSource.Should().Contain("string InvoicePaymentSettingsActionHref() => Url.Action(\"Edit\", \"SiteSettings\", new { fragment = InvoicePaymentSettingsActionFragment() }) ?? string.Empty;");
        subscriptionInvoicesViewSource.Should().Contain("hx-get=\"@InvoicePaymentSettingsActionHref()\"");
        subscriptionInvoicesViewSource.Should().Contain("target=\"@InvoiceExternalTarget()\"");
        subscriptionInvoicesViewSource.Should().Contain("rel=\"@InvoiceExternalRel()\"");
        subscriptionInvoicesViewSource.Should().Contain("@InvoicePaymentSettingsActionText()");
    }


    [Fact]
    public void BusinessSubscriptionWorkspace_Should_KeepTopRailAndCurrentPlanActionContractsWired()
    {
        var subscriptionViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Subscription.cshtml"));

subscriptionViewSource.Should().Contain("id=\"@SubscriptionWorkspaceFrameId()\"");
        subscriptionViewSource.Should().Contain("@inject Darwin.WebAdmin.Infrastructure.PermissionRazorHelper Perms");
        subscriptionViewSource.Should().Contain("var isFullAdmin = await Perms.HasAsync(\"FullAdminAccess\");");
        subscriptionViewSource.Should().Contain("hx-get=\"@SubscriptionEditActionHref()\"");
subscriptionViewSource.Should().Contain("@SubscriptionEditActionText()");
        subscriptionViewSource.Should().Contain("hx-get=\"@SubscriptionSetupActionHref()\"");
subscriptionViewSource.Should().Contain("@SubscriptionSetupActionText()");
        subscriptionViewSource.Should().Contain("hx-get=\"@SubscriptionPaymentsActionHref()\"");
subscriptionViewSource.Should().Contain("@SubscriptionPaymentsActionText()");
        subscriptionViewSource.Should().Contain("hx-get=\"@SubscriptionInvoiceQueueActionHref()\"");
subscriptionViewSource.Should().Contain("@SubscriptionInvoiceQueueActionText()");
        subscriptionViewSource.Should().Contain("action=\"@SubscriptionRenewalTogglePostActionHref()\"");
        subscriptionViewSource.Should().Contain("hx-post=\"@SubscriptionRenewalTogglePostActionHref()\"");
        subscriptionViewSource.Should().Contain("name=\"businessId\" value=\"@SubscriptionRenewalFormBusinessIdValue()\"");
        subscriptionViewSource.Should().Contain("name=\"subscriptionId\" value=\"@SubscriptionRenewalFormSubscriptionIdValue()\"");
        subscriptionViewSource.Should().Contain("name=\"rowVersion\" value=\"@Convert.ToBase64String(Model.Subscription.RowVersion)\"");
        subscriptionViewSource.Should().Contain("name=\"cancelAtPeriodEnd\" value=\"@((!Model.Subscription.CancelAtPeriodEnd).ToString().ToLowerInvariant())\"");
        subscriptionViewSource.Should().Contain("string SubscriptionStatusDisplayText(string? status) => string.IsNullOrWhiteSpace(status) ? \"-\" : T.T(status);");
        subscriptionViewSource.Should().Contain("@SubscriptionStatusDisplayText(Model.Subscription.Status)");
subscriptionViewSource.Should().Contain("string SubscriptionRecentInvoicesTableRowPlanDisplayText(string? planName) => SubscriptionFallbackDisplayText(planName);");
subscriptionViewSource.Should().Contain("@SubscriptionRecentInvoicesTableRowPlanDisplayText(invoice.PlanName)");
        subscriptionViewSource.Should().Contain("BusinessSubscriptionRestoreRenewal");
        subscriptionViewSource.Should().Contain("BusinessSubscriptionCancelAtPeriodEndAction");
subscriptionViewSource.Should().Contain("@SubscriptionBillingWebsiteActionText()");
subscriptionViewSource.Should().Contain("@SubscriptionConfigureWebsiteActionText()");
        subscriptionViewSource.Should().Contain("@SubscriptionResolvePrerequisitesHintText()");
        subscriptionViewSource.Should().Contain("@SubscriptionCurrentPlanBadgeText()");
    }


    [Fact]
    public void BusinessWrapperViews_Should_KeepTitleAndPartialHandoffContractsWired()
    {
        var createViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Create.cshtml"));
        var editViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Edit.cshtml"));
        var setupViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Setup.cshtml"));

        createViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Businesses.BusinessEditVm");
        createViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"CreateBusiness\");");
        createViewSource.Should().Contain("<partial name=\"_BusinessEditorShell\" model=\"Model\" />");

        editViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Businesses.BusinessEditVm");
        editViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"EditBusiness\");");
        editViewSource.Should().Contain("<partial name=\"_BusinessEditorShell\" model=\"Model\" />");

        setupViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Businesses.BusinessEditVm");
        setupViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"BusinessSetupTitle\");");
        setupViewSource.Should().Contain("<partial name=\"_BusinessSetupShell\" model=\"Model\" />");
    }


    [Fact]
    public void BusinessMemberInvitationAndLocationWrapperViews_Should_KeepTitleAndShellHandoffContractsWired()
    {
        var createInvitationViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "CreateInvitation.cshtml"));
        var createLocationViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "CreateLocation.cshtml"));
        var createMemberViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "CreateMember.cshtml"));
        var editLocationViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "EditLocation.cshtml"));
        var editMemberViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "EditMember.cshtml"));

        createInvitationViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Businesses.BusinessInvitationCreateVm");
        createInvitationViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"CreateBusinessInvitation\");");
        createInvitationViewSource.Should().Contain("<partial name=\"_BusinessInvitationEditorShell\" model=\"Model\" />");

        createLocationViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Businesses.BusinessLocationEditVm");
        createLocationViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"CreateBusinessLocation\");");
        createLocationViewSource.Should().Contain("<partial name=\"_BusinessLocationEditorShell\" model=\"Model\" />");

        createMemberViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Businesses.BusinessMemberEditVm");
        createMemberViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"AssignBusinessMemberTitle\");");
        createMemberViewSource.Should().Contain("<partial name=\"_BusinessMemberEditorShell\" model=\"Model\" />");

        editLocationViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Businesses.BusinessLocationEditVm");
        editLocationViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"EditBusinessLocation\");");
        editLocationViewSource.Should().Contain("<partial name=\"_BusinessLocationEditorShell\" model=\"Model\" />");

        editMemberViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Businesses.BusinessMemberEditVm");
        editMemberViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"EditBusinessMemberTitle\");");
        editMemberViewSource.Should().Contain("<partial name=\"_BusinessMemberEditorShell\" model=\"Model\" />");
    }


    [Fact]
    public void BrandWrapperViews_Should_KeepCreateModeFlagAndShellHandoffContractsWired()
    {
        var createViewSource = ReadWebAdminFile(Path.Combine("Views", "Brands", "Create.cshtml"));
        var editViewSource = ReadWebAdminFile(Path.Combine("Views", "Brands", "Edit.cshtml"));

        createViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Catalog.BrandEditVm");
        createViewSource.Should().Contain("ViewData[\"IsCreate\"] = true;");
        createViewSource.Should().Contain("<partial name=\"_BrandEditorShell\" model=\"Model\" />");
        createViewSource.Should().Contain("@section Scripts {");
        createViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");

        editViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Catalog.BrandEditVm");
        editViewSource.Should().Contain("ViewData[\"IsCreate\"] = false;");
        editViewSource.Should().Contain("<partial name=\"_BrandEditorShell\" model=\"Model\" />");
        editViewSource.Should().Contain("@section Scripts {");
        editViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");
    }


    [Fact]
    public void PageWrapperViews_Should_KeepTitleShellAndValidationHandoffContractsWired()
    {
        var createViewSource = ReadWebAdminFile(Path.Combine("Views", "Pages", "Create.cshtml"));
        var editViewSource = ReadWebAdminFile(Path.Combine("Views", "Pages", "Edit.cshtml"));

        createViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.CMS.PageCreateVm");
        createViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"CreatePage\");");
        createViewSource.Should().Contain("<partial name=\"_PageCreateEditorShell\" model=\"Model\" />");
        createViewSource.Should().Contain("@section Scripts {");
        createViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");

        editViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.CMS.PageEditVm");
        editViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"EditPage\");");
        editViewSource.Should().Contain("<partial name=\"_PageEditEditorShell\" model=\"Model\" />");
        editViewSource.Should().Contain("@section Scripts {");
        editViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");
    }


    [Fact]
    public void CategoryWrapperViews_Should_KeepTitleShellAndValidationHandoffContractsWired()
    {
        var createViewSource = ReadWebAdminFile(Path.Combine("Views", "Categories", "Create.cshtml"));
        var editViewSource = ReadWebAdminFile(Path.Combine("Views", "Categories", "Edit.cshtml"));

        createViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Catalog.CategoryCreateVm");
        createViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"CreateCategory\");");
        createViewSource.Should().Contain("<partial name=\"_CategoryCreateEditorShell\" model=\"Model\" />");
        createViewSource.Should().Contain("@section Scripts {");
        createViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");

        editViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Catalog.CategoryEditVm");
        editViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"EditCategory\");");
        editViewSource.Should().Contain("<partial name=\"_CategoryEditEditorShell\" model=\"Model\" />");
        editViewSource.Should().Contain("@section Scripts {");
        editViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");
    }


    [Fact]
    public void ProductWrapperViews_Should_KeepTitleShellAndValidationHandoffContractsWired()
    {
        var createViewSource = ReadWebAdminFile(Path.Combine("Views", "Products", "Create.cshtml"));
        var editViewSource = ReadWebAdminFile(Path.Combine("Views", "Products", "Edit.cshtml"));

        createViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Catalog.ProductCreateVm");
        createViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"CreateProduct\");");
        createViewSource.Should().Contain("<partial name=\"_ProductCreateEditorShell\" model=\"Model\" />");
        createViewSource.Should().Contain("@section Scripts {");
        createViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");

        editViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Catalog.ProductEditVm");
        editViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"EditProduct\");");
        editViewSource.Should().Contain("<partial name=\"_ProductEditEditorShell\" model=\"Model\" />");
        editViewSource.Should().Contain("@section Scripts {");
        editViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");
    }


    [Fact]
    public void AddOnGroupWrapperViews_Should_KeepTitleShellAndValidationHandoffContractsWired()
    {
        var createViewSource = ReadWebAdminFile(Path.Combine("Views", "AddOnGroups", "Create.cshtml"));
        var editViewSource = ReadWebAdminFile(Path.Combine("Views", "AddOnGroups", "Edit.cshtml"));

        createViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Catalog.AddOnGroupCreateVm");
        createViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"CreateAddOnGroup\");");
        createViewSource.Should().Contain("<partial name=\"_AddOnGroupCreateEditorShell\" model=\"Model\" />");
        createViewSource.Should().Contain("@section Scripts {");
        createViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");

        editViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Catalog.AddOnGroupEditVm");
        editViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"EditAddOnGroup\");");
        editViewSource.Should().Contain("<partial name=\"_AddOnGroupEditEditorShell\" model=\"Model\" />");
        editViewSource.Should().Contain("@section Scripts {");
        editViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");
    }


    [Fact]
    public void ShippingMethodWrapperViews_Should_KeepTitleCreateModeAndShellHandoffContractsWired()
    {
        var createViewSource = ReadWebAdminFile(Path.Combine("Views", "ShippingMethods", "Create.cshtml"));
        var editViewSource = ReadWebAdminFile(Path.Combine("Views", "ShippingMethods", "Edit.cshtml"));

        createViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Shipping.ShippingMethodEditVm");
        createViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"CreateShippingMethod\");");
        createViewSource.Should().Contain("ViewData[\"IsCreate\"] = true;");
        createViewSource.Should().Contain("<partial name=\"_ShippingMethodEditorShell\" model=\"Model\" />");

        editViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Shipping.ShippingMethodEditVm");
        editViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"EditShippingMethod\");");
        editViewSource.Should().Contain("ViewData[\"IsCreate\"] = false;");
        editViewSource.Should().Contain("<partial name=\"_ShippingMethodEditorShell\" model=\"Model\" />");
    }


    [Fact]
    public void PermissionWrapperViews_Should_KeepTitleShellAndValidationHandoffContractsWired()
    {
        var createViewSource = ReadWebAdminFile(Path.Combine("Views", "Permissions", "Create.cshtml"));
        var editViewSource = ReadWebAdminFile(Path.Combine("Views", "Permissions", "Edit.cshtml"));

        createViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Identity.PermissionCreateVm");
        createViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"CreatePermission\");");
        createViewSource.Should().Contain("<partial name=\"_PermissionCreateEditorShell\" model=\"Model\" />");
        createViewSource.Should().Contain("@section Scripts {");
        createViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");

        editViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Identity.PermissionEditVm");
        editViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"EditPermission\");");
        editViewSource.Should().Contain("<partial name=\"_PermissionEditEditorShell\" model=\"Model\" />");
        editViewSource.Should().Contain("@section Scripts {");
        editViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");
    }


    [Fact]
    public void RoleWrapperViews_Should_KeepTitleShellAndValidationHandoffContractsWired()
    {
        var createViewSource = ReadWebAdminFile(Path.Combine("Views", "Roles", "Create.cshtml"));
        var editViewSource = ReadWebAdminFile(Path.Combine("Views", "Roles", "Edit.cshtml"));

        createViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Identity.RoleCreateVm");
        createViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"CreateRole\");");
        createViewSource.Should().Contain("<partial name=\"_RoleCreateEditorShell\" model=\"Model\" />");
        createViewSource.Should().Contain("@section Scripts {");
        createViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");

        editViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Identity.RoleEditVm");
        editViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"EditRole\");");
        editViewSource.Should().Contain("<partial name=\"_RoleEditEditorShell\" model=\"Model\" />");
        editViewSource.Should().Contain("@section Scripts {");
        editViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");
    }


    [Fact]
    public void UserWrapperViews_Should_KeepTitleShellAndValidationHandoffContractsWired()
    {
        var createViewSource = ReadWebAdminFile(Path.Combine("Views", "Users", "Create.cshtml"));
        var editViewSource = ReadWebAdminFile(Path.Combine("Views", "Users", "Edit.cshtml"));
        var rolesViewSource = ReadWebAdminFile(Path.Combine("Views", "Users", "Roles.cshtml"));
        var changeEmailViewSource = ReadWebAdminFile(Path.Combine("Views", "Users", "ChangeEmail.cshtml"));
        var changePasswordViewSource = ReadWebAdminFile(Path.Combine("Views", "Users", "ChangePassword.cshtml"));

        createViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Identity.UserCreateVm");
        createViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"CreateUser\");");
        createViewSource.Should().Contain("<partial name=\"_UserCreateEditorShell\" model=\"Model\" />");
        createViewSource.Should().Contain("@section Scripts {");
        createViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");

        editViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Identity.UserEditVm");
        editViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"EditUserTitle\");");
        editViewSource.Should().Contain("<partial name=\"_UserEditEditorShell\" model=\"Model\" />");
        editViewSource.Should().Contain("@section Scripts {");
        editViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");

        rolesViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Identity.UserRolesEditVm");
        rolesViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"EditUserRolesTitle\");");
        rolesViewSource.Should().Contain("<partial name=\"_UserRolesEditorShell\" model=\"Model\" />");
        rolesViewSource.Should().Contain("@section Scripts {");
        rolesViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");

        changeEmailViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Identity.UserChangeEmailVm");
        changeEmailViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"ChangeEmail\");");
        changeEmailViewSource.Should().Contain("<partial name=\"_UserChangeEmailEditorShell\" model=\"Model\" />");
        changeEmailViewSource.Should().Contain("@section Scripts {");
        changeEmailViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");

        changePasswordViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Identity.UserChangePasswordVm");
        changePasswordViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"ChangePassword\");");
        changePasswordViewSource.Should().Contain("<partial name=\"_UserChangePasswordEditorShell\" model=\"Model\" />");
        changePasswordViewSource.Should().Contain("@section Scripts {");
        changePasswordViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");
    }


    [Fact]
    public void BillingWrapperViews_Should_KeepTitleCreateModeShellAndJournalScriptContractsWired()
    {
        var createPlanViewSource = ReadWebAdminFile(Path.Combine("Views", "Billing", "CreatePlan.cshtml"));
        var editPlanViewSource = ReadWebAdminFile(Path.Combine("Views", "Billing", "EditPlan.cshtml"));
        var createPaymentViewSource = ReadWebAdminFile(Path.Combine("Views", "Billing", "CreatePayment.cshtml"));
        var editPaymentViewSource = ReadWebAdminFile(Path.Combine("Views", "Billing", "EditPayment.cshtml"));
        var createFinancialAccountViewSource = ReadWebAdminFile(Path.Combine("Views", "Billing", "CreateFinancialAccount.cshtml"));
        var editFinancialAccountViewSource = ReadWebAdminFile(Path.Combine("Views", "Billing", "EditFinancialAccount.cshtml"));
        var createExpenseViewSource = ReadWebAdminFile(Path.Combine("Views", "Billing", "CreateExpense.cshtml"));
        var editExpenseViewSource = ReadWebAdminFile(Path.Combine("Views", "Billing", "EditExpense.cshtml"));
        var createJournalEntryViewSource = ReadWebAdminFile(Path.Combine("Views", "Billing", "CreateJournalEntry.cshtml"));
        var editJournalEntryViewSource = ReadWebAdminFile(Path.Combine("Views", "Billing", "EditJournalEntry.cshtml"));

        createPlanViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Billing.BillingPlanEditVm");
        createPlanViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"CreateBillingPlan\");");
        createPlanViewSource.Should().Contain("ViewData[\"IsCreate\"] = true;");
        createPlanViewSource.Should().Contain("<partial name=\"_BillingPlanEditorShell\" model=\"Model\" />");

        editPlanViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Billing.BillingPlanEditVm");
        editPlanViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"EditBillingPlan\");");
        editPlanViewSource.Should().Contain("ViewData[\"IsCreate\"] = false;");
        editPlanViewSource.Should().Contain("<partial name=\"_BillingPlanEditorShell\" model=\"Model\" />");

        createPaymentViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Billing.PaymentEditVm");
        createPaymentViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"CreatePayment\");");
        createPaymentViewSource.Should().Contain("ViewData[\"IsCreate\"] = true;");
        createPaymentViewSource.Should().Contain("<partial name=\"~/Views/Billing/_PaymentEditorShell.cshtml\" model=\"Model\" />");
        createPaymentViewSource.Should().Contain("@section Scripts { <partial name=\"_ValidationScriptsPartial\" /> }");

        editPaymentViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Billing.PaymentEditVm");
        editPaymentViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"EditPayment\");");
        editPaymentViewSource.Should().Contain("ViewData[\"IsCreate\"] = false;");
        editPaymentViewSource.Should().Contain("<partial name=\"~/Views/Billing/_PaymentEditorShell.cshtml\" model=\"Model\" />");
        editPaymentViewSource.Should().Contain("@section Scripts { <partial name=\"_ValidationScriptsPartial\" /> }");

        createFinancialAccountViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Billing.FinancialAccountEditVm");
        createFinancialAccountViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"CreateFinancialAccount\");");
        createFinancialAccountViewSource.Should().Contain("ViewData[\"IsCreate\"] = true;");
        createFinancialAccountViewSource.Should().Contain("<partial name=\"~/Views/Billing/_FinancialAccountEditorShell.cshtml\" model=\"Model\" />");
        createFinancialAccountViewSource.Should().Contain("@section Scripts { <partial name=\"_ValidationScriptsPartial\" /> }");

        editFinancialAccountViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Billing.FinancialAccountEditVm");
        editFinancialAccountViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"EditFinancialAccount\");");
        editFinancialAccountViewSource.Should().Contain("ViewData[\"IsCreate\"] = false;");
        editFinancialAccountViewSource.Should().Contain("<partial name=\"~/Views/Billing/_FinancialAccountEditorShell.cshtml\" model=\"Model\" />");
        editFinancialAccountViewSource.Should().Contain("@section Scripts { <partial name=\"_ValidationScriptsPartial\" /> }");

        createExpenseViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Billing.ExpenseEditVm");
        createExpenseViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"CreateExpense\");");
        createExpenseViewSource.Should().Contain("ViewData[\"IsCreate\"] = true;");
        createExpenseViewSource.Should().Contain("<partial name=\"~/Views/Billing/_ExpenseEditorShell.cshtml\" model=\"Model\" />");
        createExpenseViewSource.Should().Contain("@section Scripts { <partial name=\"_ValidationScriptsPartial\" /> }");

        editExpenseViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Billing.ExpenseEditVm");
        editExpenseViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"EditExpense\");");
        editExpenseViewSource.Should().Contain("ViewData[\"IsCreate\"] = false;");
        editExpenseViewSource.Should().Contain("<partial name=\"~/Views/Billing/_ExpenseEditorShell.cshtml\" model=\"Model\" />");
        editExpenseViewSource.Should().Contain("@section Scripts { <partial name=\"_ValidationScriptsPartial\" /> }");

        createJournalEntryViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Billing.JournalEntryEditVm");
        createJournalEntryViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"CreateJournalEntry\");");
        createJournalEntryViewSource.Should().Contain("ViewData[\"IsCreate\"] = true;");
        createJournalEntryViewSource.Should().Contain("<partial name=\"~/Views/Billing/_JournalEntryEditorShell.cshtml\" model=\"Model\" />");
        createJournalEntryViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");
        createJournalEntryViewSource.Should().NotContain("<script>");

        editJournalEntryViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Billing.JournalEntryEditVm");
        editJournalEntryViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"EditJournalEntry\");");
        editJournalEntryViewSource.Should().Contain("ViewData[\"IsCreate\"] = false;");
        editJournalEntryViewSource.Should().Contain("<partial name=\"~/Views/Billing/_JournalEntryEditorShell.cshtml\" model=\"Model\" />");
        editJournalEntryViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");
        editJournalEntryViewSource.Should().NotContain("<script>");
    }


    [Fact]
    public void OrderMutationWrapperViews_Should_KeepTitleShellAndValidationHandoffContractsWired()
    {
        var addPaymentViewSource = ReadWebAdminFile(Path.Combine("Views", "Orders", "AddPayment.cshtml"));
        var addShipmentViewSource = ReadWebAdminFile(Path.Combine("Views", "Orders", "AddShipment.cshtml"));
        var addRefundViewSource = ReadWebAdminFile(Path.Combine("Views", "Orders", "AddRefund.cshtml"));
        var createInvoiceViewSource = ReadWebAdminFile(Path.Combine("Views", "Orders", "CreateInvoice.cshtml"));

        addPaymentViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Orders.PaymentCreateVm");
        addPaymentViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"AddPayment\");");
        addPaymentViewSource.Should().Contain("<partial name=\"~/Views/Orders/_PaymentCreateShell.cshtml\" model=\"Model\" />");
        addPaymentViewSource.Should().Contain("@section Scripts {");
        addPaymentViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");

        addShipmentViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Orders.ShipmentCreateVm");
        addShipmentViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"AddShipment\");");
        addShipmentViewSource.Should().Contain("<partial name=\"~/Views/Orders/_ShipmentCreateShell.cshtml\" model=\"Model\" />");
        addShipmentViewSource.Should().Contain("@section Scripts {");
        addShipmentViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");

        addRefundViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Orders.RefundCreateVm");
        addRefundViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"AddRefund\");");
        addRefundViewSource.Should().Contain("<partial name=\"~/Views/Orders/_RefundCreateShell.cshtml\" model=\"Model\" />");
        addRefundViewSource.Should().Contain("@section Scripts {");
        addRefundViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");

        createInvoiceViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Orders.OrderInvoiceCreateVm");
        createInvoiceViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"CreateInvoice\");");
        createInvoiceViewSource.Should().Contain("<partial name=\"~/Views/Orders/_InvoiceCreateShell.cshtml\" model=\"Model\" />");
        createInvoiceViewSource.Should().Contain("@section Scripts {");
        createInvoiceViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");
    }


    [Fact]
    public void SiteSettingsWrapperView_Should_KeepTitleShellAndValidationHandoffContractsWired()
    {
        var editViewSource = ReadWebAdminFile(Path.Combine("Views", "SiteSettings", "Edit.cshtml"));

        editViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Settings.SiteSettingVm");
        editViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"EditSiteSettings\");");
        editViewSource.Should().Contain("<partial name=\"_SiteSettingsEditorShell\" model=\"Model\" />");
        editViewSource.Should().Contain("@section Scripts {");
        editViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");
    }


    [Fact]
    public void AdminTextLocalizer_Should_KeepBusinessIdResolutionAndBusinessOverrideCacheContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Localization", "AdminTextLocalizer.cs"));

        source.Should().Contain("const string cacheKey = \"AdminTextLocalizer.BusinessOverrides\";");
        source.Should().Contain("if (httpContext.Items.TryGetValue(cacheKey, out var cached) &&");
        source.Should().Contain("httpContext.Items[cacheKey] = AdminTextOverrideCatalog.Empty;");
        source.Should().Contain("httpContext.Items[cacheKey] = businessOverrides;");
        source.Should().Contain("if (TryParseGuid(httpContext.Request.RouteValues[\"businessId\"]?.ToString(), out var routeBusinessId))");
        source.Should().Contain("var controller = httpContext.Request.RouteValues[\"controller\"]?.ToString();");
        source.Should().Contain("if (string.Equals(controller, \"Businesses\", StringComparison.OrdinalIgnoreCase) &&");
        source.Should().Contain("TryParseGuid(httpContext.Request.RouteValues[\"id\"]?.ToString(), out var routeId)");
        source.Should().Contain("if (TryParseGuid(httpContext.Request.Query[\"businessId\"].ToString(), out var queryBusinessId))");
        source.Should().Contain("TryParseGuid(httpContext.Request.Query[\"id\"].ToString(), out var queryId)");
        source.Should().Contain("if (httpContext.Request.HasFormContentType)");
        source.Should().Contain("if (TryParseGuid(httpContext.Request.Form[\"BusinessId\"].ToString(), out var formBusinessId))");
        source.Should().Contain("TryParseGuid(httpContext.Request.Form[\"Id\"].ToString(), out var formId)");
        source.Should().Contain("return Guid.TryParse(value, out id) && id != Guid.Empty;");
    }


    [Fact]
    public void AdminTextOverrideCatalog_Should_KeepNormalizationFilteringAndEmptyFallbackContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Localization", "AdminTextOverrideCatalog.cs"));

        source.Should().Contain("if (root is null || root.Count == 0)");
        source.Should().Contain("var normalized = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);");
        source.Should().Contain("foreach (var (culture, entries) in root)");
        source.Should().Contain("if (string.IsNullOrWhiteSpace(culture) || entries is null || entries.Count == 0)");
        source.Should().Contain("var normalizedCulture = AdminCultureCatalog.NormalizeUiCulture(culture);");
        source.Should().Contain(".Where(static kvp => !string.IsNullOrWhiteSpace(kvp.Key) && !string.IsNullOrWhiteSpace(kvp.Value))");
        source.Should().Contain(".ToDictionary(kvp => kvp.Key.Trim(), kvp => kvp.Value.Trim(), StringComparer.OrdinalIgnoreCase);");
        source.Should().Contain("if (values.Count > 0)");
        source.Should().Contain("normalized[normalizedCulture] = values;");
        source.Should().Contain("return normalized.Count == 0 ? Empty : normalized;");
        source.Should().Contain("catch (JsonException)");
        source.Should().Contain("if (overrides.Count == 0)");
        source.Should().Contain("if (!overrides.TryGetValue(normalizedCulture, out var entries))");
        source.Should().Contain("value = resolvedValue;");
        source.Should().Contain("new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);");
    }


    [Fact]
    public void SiteSettingCache_Should_KeepDefaultMappingAndNormalizationFallbackContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Services", "Settings", "SiteSettingCache.cs"));

        source.Should().Contain("DefaultCulture = string.IsNullOrWhiteSpace(s.DefaultCulture) ? AdminCultureCatalog.DefaultCulture : AdminCultureCatalog.NormalizeUiCulture(s.DefaultCulture),");
        source.Should().Contain("SupportedCulturesCsv = string.IsNullOrWhiteSpace(s.SupportedCulturesCsv) ? AdminCultureCatalog.SupportedCulturesCsvDefault : string.Join(\",\", AdminCultureCatalog.NormalizeSupportedCultureNames(s.SupportedCulturesCsv)),");
        source.Should().Contain("DefaultCountry = string.IsNullOrWhiteSpace(s.DefaultCountry) ? SiteSettingDto.DefaultCountryDefault : s.DefaultCountry,");
        source.Should().Contain("DefaultCurrency = string.IsNullOrWhiteSpace(s.DefaultCurrency) ? SiteSettingDto.DefaultCurrencyDefault : s.DefaultCurrency,");
        source.Should().Contain("TimeZone = string.IsNullOrWhiteSpace(s.TimeZone) ? SiteSettingDto.TimeZoneDefault : s.TimeZone,");
        source.Should().Contain("DateFormat = string.IsNullOrWhiteSpace(s.DateFormat) ? SiteSettingDto.DateFormatDefault : s.DateFormat,");
        source.Should().Contain("TimeFormat = string.IsNullOrWhiteSpace(s.TimeFormat) ? SiteSettingDto.TimeFormatDefault : s.TimeFormat,");
        source.Should().Contain("JwtIssuer = string.IsNullOrWhiteSpace(s.JwtIssuer) ? \"Darwin\" : s.JwtIssuer,");
        source.Should().Contain("JwtAudience = string.IsNullOrWhiteSpace(s.JwtAudience) ? \"Darwin.PublicApi\" : s.JwtAudience,");
        source.Should().Contain("MeasurementSystem = string.IsNullOrWhiteSpace(s.MeasurementSystem) ? \"Metric\" : s.MeasurementSystem,");
        source.Should().Contain("WebAuthnRelyingPartyId = string.IsNullOrWhiteSpace(s.WebAuthnRelyingPartyId) ? \"localhost\" : s.WebAuthnRelyingPartyId,");
        source.Should().Contain("WebAuthnRelyingPartyName = string.IsNullOrWhiteSpace(s.WebAuthnRelyingPartyName) ? \"Darwin\" : s.WebAuthnRelyingPartyName,");
        source.Should().Contain("WebAuthnAllowedOriginsCsv = string.IsNullOrWhiteSpace(s.WebAuthnAllowedOriginsCsv) ? \"https://localhost:5001\" : s.WebAuthnAllowedOriginsCsv,");
        source.Should().Contain("HomeSlug = string.IsNullOrWhiteSpace(s.HomeSlug) ? SiteSettingDto.HomeSlugDefault : s.HomeSlug");
    }


    [Fact]
    public void PermissionRazorHelper_Should_KeepAnonymousClaimAndFullAdminBypassContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Infrastructure", "PermissionRazorHelper.cs"));

        source.Should().Contain("var http = _httpContextAccessor.HttpContext;");
        source.Should().Contain("var user = http?.User;");
        source.Should().Contain("if (user?.Identity is null || !user.Identity.IsAuthenticated)");
        source.Should().Contain("return false;");
        source.Should().Contain("var idValue = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;");
        source.Should().Contain("if (!Guid.TryParse(idValue, out var userId))");
        source.Should().Contain("if (await _permissions.HasAsync(userId, \"FullAdminAccess\", ct)) return true;");
        source.Should().Contain("return await _permissions.HasAsync(userId, permissionKey, ct);");
    }


    [Fact]
    public void CanonicalUrlService_Should_KeepConstructorRelativePathAndAbsoluteFallbackContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Services", "Seo", "CanonicalUrlService.cs"));

        source.Should().Contain("private readonly IHttpContextAccessor _http;");
        source.Should().Contain("public CanonicalUrlService(IHttpContextAccessor http)");
        source.Should().Contain("_http = http;");
        source.Should().Contain("public string Page(string culture, string slug) => $\"/{culture}/page/{slug}\";");
        source.Should().Contain("public string Category(string culture, string slug) => $\"/{culture}/c/{slug}\";");
        source.Should().Contain("public string Product(string culture, string slug) => $\"/{culture}/p/{slug}\";");
        source.Should().Contain("var req = _http.HttpContext?.Request;");
        source.Should().Contain("if (req == null) return relative;");
        source.Should().Contain("var uri = UriHelper.BuildAbsolute(req.Scheme, req.Host, req.PathBase, relative);");
        source.Should().Contain("return uri;");
    }


    [Fact]
    public void PagerTagHelper_Should_KeepSuppressClampAndHtmxDefaultContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("TagHelpers", "PagerTagHelper.cs"));

        source.Should().Contain("if (Total <= 0)");
        source.Should().Contain("output.SuppressOutput();");
        source.Should().Contain("var pageSize = PageSize <= 0 ? 20 : PageSize;");
        source.Should().Contain("var totalPages = (int)((Total + pageSize - 1) / pageSize);");
        source.Should().Contain("var page = Math.Max(1, Math.Min(Page, Math.Max(1, totalPages)));");
        source.Should().Contain("var routeValuesInput = RouteValues ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);");
        source.Should().Contain("var htmxEnabled = !string.IsNullOrWhiteSpace(HxTarget);");
        source.Should().Contain("var hxSwap = string.IsNullOrWhiteSpace(HxSwap) ? \"outerHTML\" : HxSwap!;");
        source.Should().Contain("var hxPushUrl = string.IsNullOrWhiteSpace(HxPushUrl) ? \"true\" : HxPushUrl!;");
        source.Should().Contain("rv.Remove(\"page\");");
        source.Should().Contain("rv.Remove(\"pageSize\");");
        source.Should().Contain("const form = sel.closest('form');");
        source.Should().Contain("sb.AppendLine(\"      if(pageInput) pageInput.value = '1';\");");
        source.Should().Contain("if (form.requestSubmit) form.requestSubmit(); else form.submit();");
        source.Should().Contain("PageItem(\"\u00AB First\", 1, page == 1, aria: \"First\");");
        source.Should().Contain("PageItem(\"Last \u00BB\", totalPagesSafe, page == totalPagesSafe, aria: \"Last\");");
    }


    [Fact]
    public void ActiveNavLinkTagHelper_Should_KeepExactMatchControllerFallbackAndAttributeCleanupContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("TagHelpers", "ActiveNavLinkTagHelper.cs"));

        source.Should().Contain("output.TagName = \"a\";");
        source.Should().Contain("output.TagMode = TagMode.StartTagAndEndTag;");
        source.Should().Contain("var routeValues = new RouteValueDictionary();");
        source.Should().Contain("if (!string.IsNullOrEmpty(Area))");
        source.Should().Contain("routeValues[\"area\"] = Area;");
        source.Should().Contain("var actionName = string.IsNullOrEmpty(Action) ? \"Index\" : Action;");
        source.Should().Contain("var href = urlHelper.Action(actionName, Controller, routeValues);");
        source.Should().Contain("if (!string.IsNullOrEmpty(href))");
        source.Should().Contain("if (curController == wantController && curAction == wantAction)");
        source.Should().Contain("if (string.IsNullOrEmpty(Action) && curController == wantController)");
        source.Should().Contain("css = string.IsNullOrWhiteSpace(css) ? \"active\" : $\"{css} active\";");
        source.Should().Contain("output.Attributes.RemoveAll(\"asp-area\");");
        source.Should().Contain("output.Attributes.RemoveAll(\"asp-controller\");");
        source.Should().Contain("output.Attributes.RemoveAll(\"asp-action\");");
    }


    [Fact]
    public void SettingSelectTagHelper_Should_KeepReflectionSplitTimezoneAndCurrentValueFallbackContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("TagHelpers", "SettingSelectTagHelper.cs"));

        source.Should().Contain("var currentValue = For.Model?.ToString() ?? string.Empty;");
        source.Should().Contain("var siteSettings = await _siteSettingCache.GetAsync();");
        source.Should().Contain("var options = BuildOptions(siteSettings, currentValue);");
        source.Should().Contain("var configuredName = string.IsNullOrWhiteSpace(Setting) ? Key : Setting;");
        source.Should().Contain("var property = siteSettings.GetType().GetProperty(configuredName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);");
        source.Should().Contain("string? rawValue = property?.GetValue(siteSettings) as string;");
        source.Should().Contain("if (string.Equals(configuredName, \"SupportedLocalesCsv\", StringComparison.OrdinalIgnoreCase))");
        source.Should().Contain("if (string.Equals(configuredName, nameof(SiteSettingDto.SupportedCulturesCsv), StringComparison.OrdinalIgnoreCase))");
        source.Should().Contain("if (string.Equals(configuredName, \"SupportedCurrenciesCsv\", StringComparison.OrdinalIgnoreCase))");
        source.Should().Contain(".Select(x => x!.Trim().ToUpperInvariant())");
        source.Should().Contain("if (string.Equals(configuredName, \"SupportedTimezonesCsv\", StringComparison.OrdinalIgnoreCase))");
        source.Should().Contain(".Prepend(siteSettings.TimeZone ?? SiteSettingDto.TimeZoneDefault)");
        source.Should().Contain("if (!string.IsNullOrWhiteSpace(rawValue))");
        source.Should().Contain("return SplitCsvOrFallback(rawValue, currentValue);");
        source.Should().Contain("if (string.Equals(configuredName, nameof(SiteSettingDto.TimeZone), StringComparison.OrdinalIgnoreCase))");
        source.Should().Contain("return TimeZoneInfo.GetSystemTimeZones()");
        source.Should().Contain(".Prepend(\"UTC\")");
        source.Should().Contain("return string.IsNullOrEmpty(currentValue) ? Array.Empty<string>() : new[] { currentValue };");
        source.Should().Contain("var encodedValue = HtmlEncoder.Default.Encode(option);");
        source.Should().Contain("var encodedLabel = HtmlEncoder.Default.Encode(GetOptionLabel(option));");
        source.Should().Contain("var selected = string.Equals(option, currentValue, StringComparison.OrdinalIgnoreCase) ? \"selected\" : null;");
        source.Should().Contain("output.Content.SetHtmlContent(innerHtml);");
    }


    [Fact]
    public void FieldHelpTagHelper_Should_KeepButtonShellAndConditionalPopoverContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("TagHelpers", "FieldHelpTagHelper.cs"));

        source.Should().Contain("output.TagName = \"button\";");
        source.Should().Contain("output.TagMode = TagMode.StartTagAndEndTag;");
        source.Should().Contain("output.Attributes.SetAttribute(\"type\", \"button\");");
        source.Should().Contain("output.Attributes.SetAttribute(\"class\", \"btn btn-sm btn-outline-secondary ms-2 rounded-circle lh-1\");");
        source.Should().Contain("output.Attributes.SetAttribute(\"style\", \"width:1.75rem;height:1.75rem;padding:0;\");");
        source.Should().Contain("output.Attributes.SetAttribute(\"data-bs-toggle\", \"popover\");");
        source.Should().Contain("output.Attributes.SetAttribute(\"data-bs-trigger\", \"focus\");");
        source.Should().Contain("output.Attributes.SetAttribute(\"data-bs-placement\", Placement);");
        source.Should().Contain("if (!string.IsNullOrWhiteSpace(Title))");
        source.Should().Contain("output.Attributes.SetAttribute(\"title\", Title);");
        source.Should().Contain("if (!string.IsNullOrWhiteSpace(Content))");
        source.Should().Contain("output.Attributes.SetAttribute(\"data-bs-content\", Content);");
        source.Should().Contain("output.Attributes.SetAttribute(\"data-bs-html\", \"true\");");
        source.Should().Contain("output.Content.SetHtmlContent(\"<span aria-hidden=\\\"true\\\" style=\\\"font-weight:600;\\\">i</span><span class=\\\"visually-hidden\\\">Help</span>\");");
    }


    [Fact]
    public void CultureController_Should_KeepCookieOptionAndLocalRedirectContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "CultureController.cs"));

        source.Should().Contain("var normalizedCulture = AdminCultureCatalog.NormalizeUiCulture(culture);");
        source.Should().Contain("Response.Cookies.Append(");
        source.Should().Contain("CookieRequestCultureProvider.DefaultCookieName,");
        source.Should().Contain("CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(normalizedCulture)),");
        source.Should().Contain("Expires = DateTimeOffset.UtcNow.AddYears(1),");
        source.Should().Contain("IsEssential = true,");
        source.Should().Contain("HttpOnly = false,");
        source.Should().Contain("SameSite = SameSiteMode.Lax,");
        source.Should().Contain("Secure = Request.IsHttps");
        source.Should().Contain("if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))");
        source.Should().Contain("return LocalRedirect(returnUrl);");
        source.Should().Contain("return RedirectToAction(\"Index\", \"Home\");");
    }


    [Fact]
    public void WebAdminDependencyInjection_Should_KeepCookieAuthLocalizationAndSharedServiceRegistrationContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Extensions", "DependencyInjection.cs"));

        source.Should().Contain("services.AddHttpContextAccessor();");
        source.Should().Contain("services.AddScoped<PermissionRazorHelper>();");
        source.Should().Contain("services.AddLocalization(options => options.ResourcesPath = \"Resources\");");
        source.Should().Contain("services.AddScoped<IAdminTextLocalizer, AdminTextLocalizer>();");
        source.Should().Contain("services.AddSingleton<IDisplayMetadataProvider, SharedDisplayMetadataProvider>();");
        source.Should().Contain("services.AddSingleton<IConfigureOptions<MvcOptions>, ConfigureDisplayMetadataLocalization>();");
        source.Should().Contain(".AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)");
        source.Should().Contain(".AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>");
        source.Should().Contain("options.LoginPath = \"/account/login\";");
        source.Should().Contain("options.LogoutPath = \"/account/logout\";");
        source.Should().Contain("options.AccessDeniedPath = \"/account/login\";");
        source.Should().Contain("options.Cookie.Name = \"Darwin.Auth\";");
        source.Should().Contain("options.Cookie.HttpOnly = true;");
        source.Should().Contain("options.Cookie.SameSite = SameSiteMode.Lax;");
        source.Should().Contain("options.SlidingExpiration = true;");
        source.Should().Contain("options.ExpireTimeSpan = TimeSpan.FromDays(30);");
        source.Should().Contain("services.AddScoped<ICanonicalUrlService, CanonicalUrlService>();");
        source.Should().Contain("services.AddScoped<ICurrentUserService, CurrentUserService>();");
        source.Should().Contain("services.AddMemoryCache();");
        source.Should().Contain("services.AddScoped<ISiteSettingCache, SiteSettingCache>();");
    }


    [Fact]
    public void WebAdminStartup_Should_KeepLocalizationProviderOrderingAndPipelineContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Extensions", "Startup.cs"));

        source.Should().Contain("var requestLocalizationOptions = new RequestLocalizationOptions");
        source.Should().Contain("DefaultRequestCulture = new RequestCulture(localizationSettings.DefaultCulture),");
        source.Should().Contain("SupportedCultures = localizationSettings.SupportedCultures,");
        source.Should().Contain("SupportedUICultures = localizationSettings.SupportedCultures");
        source.Should().Contain("requestLocalizationOptions.RequestCultureProviders.Insert(0, new QueryStringRequestCultureProvider());");
        source.Should().Contain("requestLocalizationOptions.RequestCultureProviders.Insert(1, new CookieRequestCultureProvider());");
        source.Should().Contain("app.UseRequestLocalization(requestLocalizationOptions);");
        source.Should().Contain("if (app.Environment.IsDevelopment())");
        source.Should().Contain("await app.Services.MigrateAndSeedAsync();");
        source.Should().Contain("app.UseExceptionHandler(\"/Error\");");
        source.Should().Contain("app.UseHsts();");
        source.Should().Contain("app.UseHttpsRedirection();");
        source.Should().Contain("app.UseStaticFiles();");
        source.Should().Contain("app.UseRouting();");
        source.Should().Contain("app.UseAuthentication();");
        source.Should().Contain("app.UseAuthorization();");
        source.Should().Contain("app.MapControllerRoute(");
        source.Should().Contain("pattern: \"{controller=Home}/{action=Index}/{id?}\"");
    }


    [Fact]
    public void WebAdminProgram_Should_KeepSerilogCompositionAndStartupHandoffContractsWired()
    {
        var source = ReadWebAdminFile("Program.cs");

        source.Should().Contain("using Serilog;");
        source.Should().Contain("using Darwin.WebAdmin.Extensions;");
        source.Should().Contain("using Darwin.Infrastructure.Extensions;");
        source.Should().Contain("var builder = WebApplication.CreateBuilder(args);");
        source.Should().Contain("Log.Logger = new LoggerConfiguration()");
        source.Should().Contain(".ReadFrom.Configuration(builder.Configuration)");
        source.Should().Contain(".Enrich.FromLogContext()");
        source.Should().Contain(".CreateLogger();");
        source.Should().Contain("builder.Host.UseSerilog();");
        source.Should().Contain("builder.Services.AddWebComposition(builder.Configuration);");
        source.Should().Contain("var app = builder.Build();");
        source.Should().Contain("app.UseSerilogRequestLogging();");
        source.Should().Contain("await app.UseWebStartupAsync();");
        source.Should().Contain("app.Run();");
    }


    [Fact]
    public void ConfigureDisplayMetadataLocalization_Should_KeepConstructorAndOptionsRegistrationContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Localization", "ConfigureDisplayMetadataLocalization.cs"));

        source.Should().Contain("public sealed class ConfigureDisplayMetadataLocalization : IConfigureOptions<MvcOptions>");
        source.Should().Contain("private readonly IDisplayMetadataProvider _displayMetadataProvider;");
        source.Should().Contain("public ConfigureDisplayMetadataLocalization(IDisplayMetadataProvider displayMetadataProvider)");
        source.Should().Contain("_displayMetadataProvider = displayMetadataProvider;");
        source.Should().Contain("public void Configure(MvcOptions options)");
        source.Should().Contain("options.ModelMetadataDetailsProviders.Add(_displayMetadataProvider);");
    }


    [Fact]
    public void SharedDisplayMetadataProvider_Should_KeepConstructorDisplayAttributeGuardAndDisplayNameResolutionContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Localization", "SharedDisplayMetadataProvider.cs"));

        source.Should().Contain("public sealed class SharedDisplayMetadataProvider : IDisplayMetadataProvider");
        source.Should().Contain("private readonly IStringLocalizer<SharedResource> _localizer;");
        source.Should().Contain("public SharedDisplayMetadataProvider(IStringLocalizer<SharedResource> localizer)");
        source.Should().Contain("_localizer = localizer;");
        source.Should().Contain("public void CreateDisplayMetadata(DisplayMetadataProviderContext context)");
        source.Should().Contain("var displayAttribute = context.Attributes.OfType<DisplayAttribute>().FirstOrDefault();");
        source.Should().Contain("if (displayAttribute is null || string.IsNullOrWhiteSpace(displayAttribute.Name))");
        source.Should().Contain("return;");
        source.Should().Contain("var resourceKey = displayAttribute.Name!;");
        source.Should().Contain("context.DisplayMetadata.DisplayName = () => _localizer[resourceKey];");
    }


    [Fact]
    public void SharedServiceInterfaces_Should_KeepSettingsCacheAndCanonicalUrlContractsWired()
    {
        var siteSettingCacheInterfaceSource = ReadWebAdminFile(Path.Combine("Services", "Settings", "ISiteSettingCache.cs"));
        var canonicalUrlInterfaceSource = ReadWebAdminFile(Path.Combine("Services", "Seo", "ICanonicalUrlService.cs"));

        siteSettingCacheInterfaceSource.Should().Contain("public interface ISiteSettingCache");
        siteSettingCacheInterfaceSource.Should().Contain("Gets the current site settings with caching semantics.");
        siteSettingCacheInterfaceSource.Should().Contain("Task<SiteSettingDto> GetAsync(CancellationToken ct = default);");
        siteSettingCacheInterfaceSource.Should().Contain("Invalidates the in-memory cache so that the next read hits the database.");
        siteSettingCacheInterfaceSource.Should().Contain("void Invalidate();");
        siteSettingCacheInterfaceSource.Should().Contain("Admin controllers (dropdown options), UI helpers (formatting, culture), SEO services (canonical/hreflang).");

        canonicalUrlInterfaceSource.Should().Contain("public interface ICanonicalUrlService");
        canonicalUrlInterfaceSource.Should().Contain("Builds canonical URLs for public pages based on culture and slugs.");
        canonicalUrlInterfaceSource.Should().Contain("string Page(string culture, string slug);");
        canonicalUrlInterfaceSource.Should().Contain("string Category(string culture, string slug);");
        canonicalUrlInterfaceSource.Should().Contain("string Product(string culture, string slug);");
        canonicalUrlInterfaceSource.Should().Contain("string Absolute(string relative);");
    }


    [Fact]
    public void IAdminTextLocalizer_Should_KeepTranslationAndLanguageOptionContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Localization", "AdminTextLocalizer.cs"));

        source.Should().Contain("public interface IAdminTextLocalizer");
        source.Should().Contain("string T(string key);");
        source.Should().Contain("IReadOnlyList<(string Culture, string Label)> GetSupportedLanguageOptions();");
    }


    [Fact]
    public void ILoginRateLimiter_Should_KeepMinimalAttemptThrottleContractsWired()
    {
        var source = ReadApplicationFile(Path.Combine("Abstractions", "Security", "ILoginRateLimiter.cs"));

        source.Should().Contain("public interface ILoginRateLimiter");
        source.Should().Contain("Minimal login attempt rate limiter.");
        source.Should().Contain("Task<bool> IsAllowedAsync(string key, int maxAttempts, int windowSeconds, CancellationToken ct = default);");
        source.Should().Contain("Task RecordAsync(string key, CancellationToken ct = default);");
    }


    [Fact]
    public void AuthSecurityAbstractions_Should_KeepSecurityStampAndTotpMethodFloorContractsWired()
    {
        var securityStampSource = ReadApplicationFile(Path.Combine("Abstractions", "Auth", "ISecurityStampService.cs"));
        var totpSource = ReadApplicationFile(Path.Combine("Abstractions", "Auth", "ITotpService.cs"));

        securityStampSource.Should().Contain("public interface ISecurityStampService");
        securityStampSource.Should().Contain("Generates and compares security stamps.");
        securityStampSource.Should().Contain("string NewStamp();");
        securityStampSource.Should().Contain("bool AreEqual(string? a, string? b);");

        totpSource.Should().Contain("public interface ITotpService");
        totpSource.Should().Contain("Abstraction for TOTP generation/verification (RFC 6238).");
        totpSource.Should().Contain("bool VerifyCode(string base32Secret, string code, int window = 1);");
        totpSource.Should().Contain("string GenerateCode(string base32Secret);");
    }


    [Fact]
    public void AuthCredentialAbstractions_Should_KeepPasswordHasherAndWebAuthnMethodFloorContractsWired()
    {
        var passwordHasherSource = ReadApplicationFile(Path.Combine("Abstractions", "Auth", "IUserPasswordHasher.cs"));
        var webAuthnSource = ReadApplicationFile(Path.Combine("Abstractions", "Auth", "IWebAuthnService.cs"));

        passwordHasherSource.Should().Contain("public interface IUserPasswordHasher");
        passwordHasherSource.Should().Contain("Abstraction for hashing and verifying user passwords.");
        passwordHasherSource.Should().Contain("string Hash(string password);");
        passwordHasherSource.Should().Contain("bool Verify(string hashedPassword, string password);");

        webAuthnSource.Should().Contain("public interface IWebAuthnService");
        webAuthnSource.Should().Contain("Abstraction over WebAuthn ceremonies.");
        webAuthnSource.Should().Contain("Task<(string OptionsJson, byte[] Challenge)> BeginRegistrationAsync(");
        webAuthnSource.Should().Contain("Task<(bool Ok, byte[] CredentialId, byte[] PublicKey, Guid? Aaguid, string CredType, string? AttestationFmt, uint SignCount, bool IsSynced, string? Error)>");
        webAuthnSource.Should().Contain("FinishRegistrationAsync(");
        webAuthnSource.Should().Contain("Task<(string OptionsJson, byte[] Challenge)> BeginLoginAsync(");
        webAuthnSource.Should().Contain("Task<(bool Ok, byte[] CredentialId, uint NewSignCount, string? Error)>");
        webAuthnSource.Should().Contain("FinishLoginAsync(");
    }


    [Fact]
    public void NotificationAbstractions_Should_KeepEmailSmsAndWhatsAppMethodFloorContractsWired()
    {
        var emailSenderSource = ReadApplicationFile(Path.Combine("Abstractions", "Notifications", "IEmailSender.cs"));
        var smsSenderSource = ReadApplicationFile(Path.Combine("Abstractions", "Notifications", "ISmsSender.cs"));
        var whatsAppSenderSource = ReadApplicationFile(Path.Combine("Abstractions", "Notifications", "IWhatsAppSender.cs"));

        emailSenderSource.Should().Contain("public interface IEmailSender");
        emailSenderSource.Should().Contain("Minimal email sender abstraction used for password reset and admin notifications.");
        emailSenderSource.Should().Contain("Task SendAsync(");
        emailSenderSource.Should().Contain("string toEmail,");
        emailSenderSource.Should().Contain("string subject,");
        emailSenderSource.Should().Contain("string htmlBody,");
        emailSenderSource.Should().Contain("EmailDispatchContext? context = null);");

        smsSenderSource.Should().Contain("public interface ISmsSender");
        smsSenderSource.Should().Contain("Sends SMS messages. Optional in early phases.");
        smsSenderSource.Should().Contain("Task SendAsync(");
        smsSenderSource.Should().Contain("string toPhoneE164,");
        smsSenderSource.Should().Contain("string text,");
        smsSenderSource.Should().Contain("ChannelDispatchContext? context = null);");

        whatsAppSenderSource.Should().Contain("public interface IWhatsAppSender");
        whatsAppSenderSource.Should().Contain("Sends WhatsApp text messages via the configured provider.");
        whatsAppSenderSource.Should().Contain("Task SendTextAsync(");
        whatsAppSenderSource.Should().Contain("string toPhoneE164,");
        whatsAppSenderSource.Should().Contain("string text,");
        whatsAppSenderSource.Should().Contain("ChannelDispatchContext? context = null);");
    }


    [Fact]
    public void AuxiliaryServiceAbstractions_Should_KeepReminderInvitationLinkAndWebAuthnSettingsContractsWired()
    {
        var reminderDispatcherSource = ReadApplicationFile(Path.Combine("Abstractions", "Notifications", "IInactiveReminderDispatcher.cs"));
        var invitationLinkBuilderSource = ReadApplicationFile(Path.Combine("Abstractions", "Services", "IBusinessInvitationLinkBuilder.cs"));
        var webAuthnSettingsProviderSource = ReadApplicationFile(Path.Combine("Abstractions", "Services", "IWebAuthnSettingsProvider.cs"));

        reminderDispatcherSource.Should().Contain("public interface IInactiveReminderDispatcher");
        reminderDispatcherSource.Should().Contain("Sends an inactive-user reminder using the configured outbound notification channel.");
        reminderDispatcherSource.Should().Contain("Task<Result> DispatchAsync(");
        reminderDispatcherSource.Should().Contain("Guid userId,");
        reminderDispatcherSource.Should().Contain("string destinationDeviceId,");
        reminderDispatcherSource.Should().Contain("string pushToken,");
        reminderDispatcherSource.Should().Contain("string platform,");
        reminderDispatcherSource.Should().Contain("int inactiveDays,");

        invitationLinkBuilderSource.Should().Contain("public interface IBusinessInvitationLinkBuilder");
        invitationLinkBuilderSource.Should().Contain("Builds optional acceptance links for business-invitation onboarding emails.");
        invitationLinkBuilderSource.Should().Contain("string? BuildAcceptanceLink(string token);");

        webAuthnSettingsProviderSource.Should().Contain("public interface IWebAuthnSettingsProvider");
        webAuthnSettingsProviderSource.Should().Contain("Provides WebAuthn/FIDO2 relying party settings (RpId and Origin) from site configuration.");
        webAuthnSettingsProviderSource.Should().Contain("Task<(string RpId, string Origin)> GetAsync(CancellationToken ct);");
    }


    [Fact]
    public void CoreApplicationAbstractions_Should_KeepDbContextAndClockContractsWired()
    {
        var dbContextSource = ReadApplicationFile(Path.Combine("Abstractions", "Persistence", "IAppDbContext.cs"));
        var clockSource = ReadApplicationFile(Path.Combine("Abstractions", "Services", "IClock.cs"));

        dbContextSource.Should().Contain("public interface IAppDbContext");
        dbContextSource.Should().Contain("Application-layer abstraction over the EF Core DbContext");
        dbContextSource.Should().Contain("DbSet<T> Set<T>() where T : class;");
        dbContextSource.Should().Contain("Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);");

        clockSource.Should().Contain("public interface IClock");
        clockSource.Should().Contain("Abstraction for current UTC time to improve testability.");
        clockSource.Should().Contain("DateTime UtcNow { get; }");
    }


    [Fact]
    public void IJwtTokenService_Should_KeepAccessRefreshAndRevocationMethodFloorContractsWired()
    {
        var source = ReadApplicationFile(Path.Combine("Abstractions", "Auth", "IJwtTokenService.cs"));

        source.Should().Contain("public interface IJwtTokenService");
        source.Should().Contain("Issues short-lived access tokens and opaque refresh tokens for a user.");
        source.Should().Contain("IssueTokensAsync(");
        source.Should().Contain("Guid? ValidateRefreshToken(string refreshToken, string? deviceId);");
        source.Should().Contain("void RevokeRefreshToken(string refreshToken, string? deviceId);");
        source.Should().Contain("int RevokeAllForUser(Guid userId);");
    }


    [Fact]
    public void SharedApplicationServiceContracts_Should_KeepPermissionHtmlSanitizerAndAddOnPricingMethodFloorWired()
    {
        var permissionServiceSource = ReadApplicationFile(Path.Combine("Identity", "Services", "IPermissionService.cs"));
        var htmlSanitizerSource = ReadApplicationFile(Path.Combine("Common", "Html", "HtmlSanitizerHelper.cs"));
        var addOnPricingServiceSource = ReadApplicationFile(Path.Combine("Catalog", "Services", "IAddOnPricingService.cs"));

        permissionServiceSource.Should().Contain("public interface IPermissionService");
        permissionServiceSource.Should().Contain("Centralized permission evaluation.");
        permissionServiceSource.Should().Contain("Task<bool> HasAsync(Guid userId, string permissionKey, CancellationToken ct = default);");
        permissionServiceSource.Should().Contain("Task<HashSet<string>> GetAllAsync(Guid userId, CancellationToken ct = default);");

        htmlSanitizerSource.Should().Contain("public interface IHtmlSanitizer");
        htmlSanitizerSource.Should().Contain("string Sanitize(string html);");

        addOnPricingServiceSource.Should().Contain("public interface IAddOnPricingService");
        addOnPricingServiceSource.Should().Contain("Abstraction for validating add-on selections and computing the total price delta");
        addOnPricingServiceSource.Should().Contain("Task ValidateSelectionsForVariantAsync(Guid variantId, IReadOnlyCollection<Guid> selectedValueIds, CancellationToken ct);");
        addOnPricingServiceSource.Should().Contain("Task<long> SumPriceDeltasAsync(IReadOnlyCollection<Guid> selectedValueIds, CancellationToken ct);");
    }


    [Fact]
    public void InfrastructureSecurityContracts_Should_KeepRelyingPartyAndSecretProtectorMethodFloorWired()
    {
        var relyingPartyProviderSource = ReadInfrastructureFile(Path.Combine("Auth", "WebAuthn", "RelyingPartyFromSiteSettingsProvider.cs"));
        var secretProtectorSource = ReadInfrastructureFile(Path.Combine("Security", "Secrets", "ISecretProtector.cs"));

        relyingPartyProviderSource.Should().Contain("public interface IRelyingPartyFromSiteSettingsProvider");
        relyingPartyProviderSource.Should().Contain("Reads WebAuthn relying party settings from SiteSettings row.");
        relyingPartyProviderSource.Should().Contain("Task<(string RpId, string RpName, string[] Origins, bool RequireUserVerification)> GetAsync(CancellationToken ct);");

        secretProtectorSource.Should().Contain("public interface ISecretProtector");
        secretProtectorSource.Should().Contain("Minimal abstraction over a string protector for at-rest encryption.");
        secretProtectorSource.Should().Contain("string Protect(string plain);");
        secretProtectorSource.Should().Contain("string Unprotect(string protectedData);");
    }


    [Fact]
    public void RelyingPartyFromSiteSettingsProvider_Should_KeepDbFallbackSplitTrimAndDefaultContractsWired()
    {
        var source = ReadInfrastructureFile(Path.Combine("Auth", "WebAuthn", "RelyingPartyFromSiteSettingsProvider.cs"));

        source.Should().Contain("internal sealed class RelyingPartyFromSiteSettingsProvider : IRelyingPartyFromSiteSettingsProvider");
        source.Should().Contain("private readonly IAppDbContext _db;");
        source.Should().Contain("public RelyingPartyFromSiteSettingsProvider(IAppDbContext db) => _db = db;");
        source.Should().Contain("var row = await _db.Set<Darwin.Domain.Entities.Settings.SiteSetting>()");
        source.Should().Contain(".AsNoTracking()");
        source.Should().Contain(".FirstOrDefaultAsync(ct) ?? new Darwin.Domain.Entities.Settings.SiteSetting();");
        source.Should().Contain("var csv = row.WebAuthnAllowedOriginsCsv ?? \"https://localhost:5001\";");
        source.Should().Contain("var origins = csv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)");
        source.Should().Contain(".Select(x => x.Trim())");
        source.Should().Contain(".Where(x => !string.IsNullOrWhiteSpace(x))");
        source.Should().Contain("var rpId = row.WebAuthnRelyingPartyId ?? \"localhost\";");
        source.Should().Contain("var rpName = row.WebAuthnRelyingPartyName ?? \"Darwin\";");
        source.Should().Contain("var require = row.WebAuthnRequireUserVerification;");
        source.Should().Contain("return (rpId, rpName, origins, require);");
    }


    [Fact]
    public void Fido2WebAuthnService_Should_KeepRegistrationLoginAndVerificationFallbackContractsWired()
    {
        var source = ReadInfrastructureFile(Path.Combine("Auth", "WebAuthn", "Fido2WebAuthnService.cs"));

        source.Should().Contain("public sealed class Fido2WebAuthnService : IWebAuthnService");
        source.Should().Contain("private readonly IAppDbContext _db;");
        source.Should().Contain("private readonly IRelyingPartyFromSiteSettingsProvider _rp;");
        source.Should().Contain("public Fido2WebAuthnService(IAppDbContext db, IRelyingPartyFromSiteSettingsProvider rp)");
        source.Should().Contain("var (rpId, rpName, origins, requireUserVerification) = await _rp.GetAsync(ct);");
        source.Should().Contain("var fido = new Fido2(new Fido2Configuration");
        source.Should().Contain("Origins = new HashSet<string>(origins)");
        source.Should().Contain("var exclude = await _db.Set<UserWebAuthnCredential>()");
        source.Should().Contain("Where(c => c.UserId == userId && !c.IsDeleted)");
        source.Should().Contain("var uv = requireUserVerification ? UserVerificationRequirement.Required : UserVerificationRequirement.Preferred;");
        source.Should().Contain("ResidentKey = ResidentKeyRequirement.Discouraged");
        source.Should().Contain("CredProps = true");
        source.Should().Contain("return (json, creationOptions.Challenge);");
        source.Should().Contain("JsonSerializer.Deserialize<AuthenticatorAttestationRawResponse>(clientResponseJson)");
        source.Should().Contain("JsonSerializer.Deserialize<CredentialCreateOptions>(optionsJson)");
        source.Should().Contain("return (false, Array.Empty<byte>(), Array.Empty<byte>(), null, \"public-key\", null, 0, false, \"Invalid JSON payload(s).\");");
        source.Should().Contain("AnyAsync(c => c.CredentialId == p.CredentialId && !c.IsDeleted, ct);");
        source.Should().Contain("IsCredentialIdUniqueToUserCallback = IsUniqueAsync");
        source.Should().Contain("return (true, credentialIdBytes, pubKey, aaguid, \"public-key\", attFmt, signCount, isSynced, null);");
        source.Should().Contain("catch (Fido2VerificationException ex)");
        source.Should().Contain("return (false, Array.Empty<byte>(), Array.Empty<byte>(), null, \"public-key\", null, 0, false, ex.Message);");
        source.Should().Contain("return (false, Array.Empty<byte>(), Array.Empty<byte>(), null, \"public-key\", null, 0, false, \"Unexpected error: \" + ex.Message);");
        source.Should().Contain("var descriptors = (allowedCredentialIds ?? Array.Empty<byte[]>())");
        source.Should().Contain("AllowedCredentials = descriptors");
        source.Should().Contain("return (json, assertionOptions.Challenge);");
        source.Should().Contain("JsonSerializer.Deserialize<AuthenticatorAssertionRawResponse>(clientResponseJson)");
        source.Should().Contain("JsonSerializer.Deserialize<AssertionOptions>(optionsJson)");
        source.Should().Contain("return (false, Array.Empty<byte>(), 0, \"Invalid JSON payload(s).\");");
        source.Should().Contain("var credentialId = WebEncoders.Base64UrlDecode(assertion.Id);");
        source.Should().Contain("FirstOrDefaultAsync(c => c.CredentialId == credentialId && !c.IsDeleted, ct);");
        source.Should().Contain("if (stored is null)");
        source.Should().Contain("return (false, Array.Empty<byte>(), 0, \"Credential not found.\");");
        source.Should().Contain("if (p.UserHandle is null || p.UserHandle.Length == 0) return true;");
        source.Should().Contain("if (userHandleBytes is null || userHandleBytes.Length == 0) return false;");
        source.Should().Contain("IsUserHandleOwnerOfCredentialIdCallback = IsOwner");
        source.Should().Contain("return (true, verify.CredentialId.ToArray(), verify.SignCount, null);");
        source.Should().Contain("return (false, Array.Empty<byte>(), 0, ex.Message);");
        source.Should().Contain("return (false, Array.Empty<byte>(), 0, \"Unexpected error: \" + ex.Message);");
    }


    [Fact]
    public void IdempotencyMiddleware_Should_KeepMutatingGuardCacheReplayAndFailureCleanupContractsWired()
    {
        var source = ReadWebApiFile(Path.Combine("Middleware", "IdempotencyMiddleware.cs"));

        source.Should().Contain("public sealed class IdempotencyMiddleware");
        source.Should().Contain("private const string HeaderKey = \"Idempotency-Key\";");
        source.Should().Contain("private const string CachePrefix = \"idempotency:\";");
        source.Should().Contain("private readonly TimeSpan _entryTtl = TimeSpan.FromMinutes(5);");
        source.Should().Contain("public IdempotencyMiddleware(RequestDelegate next, IMemoryCache cache, ILogger<IdempotencyMiddleware> logger)");
        source.Should().Contain("if (context is null) throw new ArgumentNullException(nameof(context));");
        source.Should().Contain("var method = context.Request.Method;");
        source.Should().Contain("method.Equals(HttpMethods.Post, StringComparison.OrdinalIgnoreCase)");
        source.Should().Contain("method.Equals(HttpMethods.Put, StringComparison.OrdinalIgnoreCase)");
        source.Should().Contain("method.Equals(HttpMethods.Patch, StringComparison.OrdinalIgnoreCase)");
        source.Should().Contain("method.Equals(HttpMethods.Delete, StringComparison.OrdinalIgnoreCase)");
        source.Should().Contain("await _next(context).ConfigureAwait(false);");
        source.Should().Contain("if (!context.Request.Headers.TryGetValue(HeaderKey, out var keyValues))");
        source.Should().Contain("var idempotencyKey = keyValues.ToString().Trim();");
        source.Should().Contain("if (string.IsNullOrEmpty(idempotencyKey))");
        source.Should().Contain("var cacheKey = CachePrefix + idempotencyKey;");
        source.Should().Contain("_cache.TryGetValue(cacheKey, out IdempotencyEntry? existingEntry)");
        source.Should().Contain("if (existingEntry is null)");
        source.Should().Contain("_cache.Remove(cacheKey);");
        source.Should().Contain("if (existingEntry.IsInProgress)");
        source.Should().Contain("context.Response.StatusCode = (int)HttpStatusCode.Conflict;");
        source.Should().Contain("await context.Response.WriteAsync(\"Request already in progress.\").ConfigureAwait(false);");
        source.Should().Contain("context.Response.StatusCode = existingEntry.StatusCode;");
        source.Should().Contain("context.Response.ContentType = existingEntry.ContentType ?? \"application/json\";");
        source.Should().Contain("await context.Response.Body.WriteAsync(existingEntry.Body, 0, existingEntry.Body.Length).ConfigureAwait(false);");
        source.Should().Contain("var inProgress = new IdempotencyEntry { IsInProgress = true };");
        source.Should().Contain("_cache.Set(cacheKey, inProgress, _entryTtl);");
        source.Should().Contain("var originalBodyStream = context.Response.Body;");
        source.Should().Contain("await using var memoryStream = new MemoryStream();");
        source.Should().Contain("context.Response.Body = memoryStream;");
        source.Should().Contain("context.Response.Body.Seek(0, SeekOrigin.Begin);");
        source.Should().Contain("var respBytes = new byte[memoryStream.Length];");
        source.Should().Contain("await context.Response.Body.ReadAsync(respBytes, 0, respBytes.Length).ConfigureAwait(false);");
        source.Should().Contain("var entry = new IdempotencyEntry");
        source.Should().Contain("StatusCode = context.Response.StatusCode,");
        source.Should().Contain("ContentType = context.Response.ContentType,");
        source.Should().Contain("Body = respBytes");
        source.Should().Contain("_cache.Set(cacheKey, entry, _entryTtl);");
        source.Should().Contain("await context.Response.Body.CopyToAsync(originalBodyStream).ConfigureAwait(false);");
        source.Should().Contain("catch (Exception ex)");
        source.Should().Contain("_logger.LogError(ex, \"Error while processing idempotent request with key {Key}\", idempotencyKey);");
        source.Should().Contain("_cache.Remove(cacheKey);");
        source.Should().Contain("throw;");
        source.Should().Contain("context.Response.Body = originalBodyStream;");
    }


    [Fact]
    public void ErrorHandlingMiddleware_Should_KeepProblemDetailsMappingAndWriteFailureContractsWired()
    {
        var source = ReadWebApiFile(Path.Combine("Middleware", "ErrorHandlingMiddleware.cs"));

        source.Should().Contain("public sealed class ErrorHandlingMiddleware");
        source.Should().Contain("private readonly RequestDelegate _next;");
        source.Should().Contain("private readonly ILogger<ErrorHandlingMiddleware> _logger;");
        source.Should().Contain("public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)");
        source.Should().Contain("_next = next ?? throw new ArgumentNullException(nameof(next));");
        source.Should().Contain("_logger = logger ?? throw new ArgumentNullException(nameof(logger));");
        source.Should().Contain("public async Task InvokeAsync(HttpContext context)");
        source.Should().Contain("if (context is null)");
        source.Should().Contain("throw new ArgumentNullException(nameof(context));");
        source.Should().Contain("await _next(context);");
        source.Should().Contain("catch (Exception ex)");
        source.Should().Contain("_logger.LogError(ex, \"Unhandled exception while processing request {Path}\", context.Request?.Path);");
        source.Should().Contain("await HandleExceptionAsync(context, ex);");
        source.Should().Contain("private async Task HandleExceptionAsync(HttpContext context, Exception ex)");
        source.Should().Contain("if (context.Response.HasStarted)");
        source.Should().Contain("_logger.LogWarning(ex, \"Cannot write error response because the response has already started for request {Path}\", context.Request?.Path);");
        source.Should().Contain("UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,");
        source.Should().Contain("_ => (int)HttpStatusCode.InternalServerError");
        source.Should().Contain("var title = status == (int)HttpStatusCode.Unauthorized");
        source.Should().Contain("? \"Unauthorized\"");
        source.Should().Contain(": \"API Error\";");
        source.Should().Contain("var problem = new ProblemDetails");
        source.Should().Contain("Status = status,");
        source.Should().Contain("Title = title,");
        source.Should().Contain("Detail = ex.Message,");
        source.Should().Contain("Instance = context.Request?.Path.Value");
        source.Should().Contain("context.Response.Clear();");
        source.Should().Contain("context.Response.StatusCode = status;");
        source.Should().Contain("context.Response.ContentType = \"application/json\";");
        source.Should().Contain("var json = JsonSerializer.Serialize(problem);");
        source.Should().Contain("await context.Response.WriteAsync(json);");
        source.Should().Contain("catch (Exception writeEx)");
        source.Should().Contain("_logger.LogError(writeEx, \"Failed to write error response for request {Path}\", context.Request?.Path);");
    }


    [Fact]
    public void StorefrontCheckoutUrlBuilder_Should_KeepStripeSpecificConfigValidationAndUrlShapingContractsWired()
    {
        var source = ReadWebApiFile(Path.Combine("Services", "StorefrontCheckoutUrlBuilder.cs"));

        source.Should().Contain("public sealed class StorefrontCheckoutUrlBuilder");
        source.Should().Contain("private readonly IConfiguration _configuration;");
        source.Should().Contain("private readonly IStringLocalizer<ValidationResource> _validationLocalizer;");
        source.Should().Contain("public StorefrontCheckoutUrlBuilder(");
        source.Should().Contain("IConfiguration configuration,");
        source.Should().Contain("IStringLocalizer<ValidationResource> validationLocalizer)");
        source.Should().Contain("_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));");
        source.Should().Contain("_validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));");
        source.Should().Contain("public string BuildFrontOfficeConfirmationUrl(Guid orderId, string? orderNumber, bool cancelled)");
        source.Should().Contain("var baseUrl = _configuration[\"StorefrontCheckout:FrontOfficeBaseUrl\"];");
        source.Should().Contain("if (string.IsNullOrWhiteSpace(baseUrl) || !Uri.TryCreate(baseUrl, UriKind.Absolute, out var frontOfficeBaseUri))");
        source.Should().Contain("throw new InvalidOperationException(_validationLocalizer[\"StorefrontFrontOfficeBaseUrlNotConfigured\"]);");
        source.Should().Contain("var queryBuilder = new QueryBuilder();");
        source.Should().Contain("if (!string.IsNullOrWhiteSpace(orderNumber))");
        source.Should().Contain("queryBuilder.Add(\"orderNumber\", orderNumber.Trim());");
        source.Should().Contain("if (cancelled)");
        source.Should().Contain("queryBuilder.Add(\"cancelled\", \"true\");");
        source.Should().Contain("Path = $\"/checkout/orders/{orderId:D}/confirmation\",");
        source.Should().Contain("Query = queryBuilder.ToQueryString().Value?.TrimStart('?')");
        source.Should().Contain("public string BuildStripeCheckoutUrl(StorefrontPaymentIntentResultDto result, string returnUrl, string cancelUrl)");
        source.Should().Contain("ArgumentNullException.ThrowIfNull(result);");
        source.Should().Contain("throw new InvalidOperationException(_validationLocalizer[\"StorefrontPaymentProviderNotSupported\"]);");
        source.Should().Contain("var stripeCheckoutBaseUrl = _configuration[\"StorefrontCheckout:StripeCheckoutBaseUrl\"];");
        source.Should().Contain("if (string.IsNullOrWhiteSpace(stripeCheckoutBaseUrl) || !Uri.TryCreate(stripeCheckoutBaseUrl, UriKind.Absolute, out var stripeCheckoutBaseUri))");
        source.Should().Contain("throw new InvalidOperationException(_validationLocalizer[\"StorefrontStripeCheckoutBaseUrlNotConfigured\"]);");
        source.Should().Contain("var queryBuilder = new QueryBuilder");
        source.Should().Contain("{ \"orderId\", result.OrderId.ToString(\"D\") },");
        source.Should().Contain("{ \"paymentId\", result.PaymentId.ToString(\"D\") },");
        source.Should().Contain("{ \"provider\", \"Stripe\" },");
        source.Should().Contain("{ \"checkoutSessionId\", result.ProviderCheckoutSessionReference ?? result.ProviderReference },");
        source.Should().Contain("{ \"returnUrl\", returnUrl },");
        source.Should().Contain("{ \"cancelUrl\", cancelUrl }");
        source.Should().Contain("queryBuilder.Add(\"paymentIntentId\", result.ProviderPaymentIntentReference);");
        source.Should().Contain("return new UriBuilder(stripeCheckoutBaseUri)");
        source.Should().Contain("}.Uri.AbsoluteUri;");
    }


    [Fact]
    public void StorefrontPaymentIntentFlow_Should_KeepStripeOnlyProviderNormalizationWired()
    {
        var handlerSource = ReadApplicationFile(Path.Combine("Orders", "Commands", "StorefrontCheckoutHandlers.cs"));
        var dtoSource = ReadApplicationFile(Path.Combine("Orders", "DTOs", "StorefrontCheckoutDtos.cs"));
        var validationResourceSource = ReadApplicationFile(Path.Combine("Resources", "ValidationResource.resx"));
        var germanValidationResourceSource = ReadApplicationFile(Path.Combine("Resources", "ValidationResource.de-DE.resx"));

        handlerSource.Should().Contain("var provider = NormalizeProvider(dto.Provider);");
        handlerSource.Should().Contain("private string NormalizeProvider(string? provider)");
        handlerSource.Should().Contain("var normalized = string.IsNullOrWhiteSpace(provider) ? \"Stripe\" : provider.Trim();");
        handlerSource.Should().Contain("throw new InvalidOperationException(_localizer[\"StorefrontPaymentProviderNotSupported\"]);");
        handlerSource.Should().Contain("return \"Stripe\";");

        dtoSource.Should().Contain("public string Provider { get; set; } = \"Stripe\";");

        validationResourceSource.Should().Contain("name=\"StorefrontPaymentProviderNotSupported\"");
        germanValidationResourceSource.Should().Contain("name=\"StorefrontPaymentProviderNotSupported\"");
    }


    [Fact]
    public void StorefrontShippingRating_Should_KeepDhlFirstCarrierFilteringWired()
    {
        var source = ReadApplicationFile(Path.Combine("Shipping", "Queries", "RateShipmentHandler.cs"));

        source.Should().Contain("public sealed class RateShipmentHandler");
        source.Should().Contain("var methods = await _db.Set<ShippingMethod>().AsNoTracking()");
        source.Should().Contain(".Include(m => m.Rates)");
        source.Should().Contain(".Where(m => m.IsActive)");
        source.Should().Contain(".Where(m => m.Carrier == \"DHL\")");
    }


    [Fact]
    public void DhlShipmentTrackingVisibility_Should_KeepAdminAndMemberContractsWired()
    {
        var shipmentEntitySource = ReadDomainFile(Path.Combine("Entities", "Orders", "PaymentShipment.cs"));
        var shipmentConfigSource = ReadInfrastructureFile(Path.Combine("Persistence", "Configurations", "Orders", "ShipmentConfiguration.cs"));
        var trackingPresentationSource = ReadApplicationFile(Path.Combine("Orders", "Queries", "ShipmentTrackingPresentation.cs"));
        var shipmentsPageSource = ReadApplicationFile(Path.Combine("Orders", "Queries", "GetShipmentsPageHandler.cs"));
        var orderShipmentsPageSource = ReadApplicationFile(Path.Combine("Orders", "Queries", "GetOrderShipmentsPageHandler.cs"));
        var orderDetailSource = ReadApplicationFile(Path.Combine("Orders", "Queries", "GetOrderForViewHandler.cs"));
        var memberOrderQueriesSource = ReadApplicationFile(Path.Combine("Orders", "Queries", "MemberOrderQueries.cs"));
        var orderDtosSource = ReadApplicationFile(Path.Combine("Orders", "DTOs", "OrderDtos.cs"));
        var memberOrderDtosSource = ReadApplicationFile(Path.Combine("Orders", "DTOs", "MemberOrderDtos.cs"));
        var memberOrdersControllerSource = ReadWebApiFile(Path.Combine("Controllers", "Member", "MemberOrdersController.cs"));

        trackingPresentationSource.Should().Contain("internal static class ShipmentTrackingPresentation");
        trackingPresentationSource.Should().Contain("private const string DhlTrackingBaseUrl = \"https://www.dhl.com/global-en/home/tracking/tracking-express.html\";");
        trackingPresentationSource.Should().Contain("item.TrackingUrl = ResolveTrackingUrl(item.Carrier, item.TrackingNumber);");
        trackingPresentationSource.Should().Contain("return $\"{DhlTrackingBaseUrl}?submit=1&tracking-id={Uri.EscapeDataString(normalizedTrackingNumber)}\";");
        shipmentEntitySource.Should().Contain("public string? ProviderShipmentReference { get; set; }");
        shipmentEntitySource.Should().Contain("public string? LabelUrl { get; set; }");
        shipmentEntitySource.Should().Contain("public string? LastCarrierEventKey { get; set; }");
        shipmentConfigSource.Should().Contain("builder.Property(x => x.ProviderShipmentReference).HasMaxLength(128);");
        shipmentConfigSource.Should().Contain("builder.Property(x => x.LabelUrl).HasMaxLength(2048);");
        shipmentConfigSource.Should().Contain("builder.Property(x => x.LastCarrierEventKey).HasMaxLength(128);");
        shipmentConfigSource.Should().Contain("builder.HasIndex(x => x.ProviderShipmentReference);");

        shipmentsPageSource.Should().Contain("ProviderShipmentReference = s.ProviderShipmentReference,");
        shipmentsPageSource.Should().Contain("TrackingUrl = ShipmentTrackingPresentation.ResolveTrackingUrl(s.Carrier, s.TrackingNumber),");
        shipmentsPageSource.Should().Contain("LabelUrl = s.LabelUrl,");
        shipmentsPageSource.Should().Contain("LastCarrierEventKey = s.LastCarrierEventKey,");
        shipmentsPageSource.Should().Contain("ShipmentTrackingPresentation.Enrich(items, nowUtc);");
        orderShipmentsPageSource.Should().Contain("ProviderShipmentReference = s.ProviderShipmentReference,");
        orderShipmentsPageSource.Should().Contain("TrackingUrl = ShipmentTrackingPresentation.ResolveTrackingUrl(s.Carrier, s.TrackingNumber),");
        orderShipmentsPageSource.Should().Contain("LabelUrl = s.LabelUrl,");
        orderShipmentsPageSource.Should().Contain("LastCarrierEventAtUtc = s.DeliveredAtUtc ?? s.ShippedAtUtc ?? s.CreatedAtUtc,");
        orderShipmentsPageSource.Should().Contain("LastCarrierEventKey = s.LastCarrierEventKey,");
        orderShipmentsPageSource.Should().Contain("DefaultRefundPaymentId = _db.Set<Payment>()");
        orderShipmentsPageSource.Should().Contain("ShipmentTrackingPresentation.Enrich(items, nowUtc);");
        orderDetailSource.Should().Contain("ProviderShipmentReference = s.ProviderShipmentReference,");
        orderDetailSource.Should().Contain("TrackingUrl = ShipmentTrackingPresentation.ResolveTrackingUrl(s.Carrier, s.TrackingNumber),");
        orderDetailSource.Should().Contain("LabelUrl = s.LabelUrl,");
        orderDetailSource.Should().Contain("LastCarrierEventKey = s.LastCarrierEventKey");
        memberOrderQueriesSource.Should().Contain("TrackingUrl = ShipmentTrackingPresentation.ResolveTrackingUrl(shipment.Carrier, shipment.TrackingNumber),");

        orderDtosSource.Should().Contain("public string? TrackingUrl { get; set; }");
        orderDtosSource.Should().Contain("public string? ProviderShipmentReference { get; set; }");
        orderDtosSource.Should().Contain("public string? LabelUrl { get; set; }");
        orderDtosSource.Should().Contain("public string? LastCarrierEventKey { get; set; }");
        memberOrderDtosSource.Should().Contain("public string? TrackingUrl { get; set; }");
        memberOrdersControllerSource.Should().Contain("TrackingUrl = shipment.TrackingUrl,");
        memberOrdersControllerSource.Should().Contain("TrackingUrl: {shipment.TrackingUrl ?? \"N/A\"}");
    }


    [Fact]
    public void InactiveReminderWorker_Should_KeepExecutionLoopDelayClampAndWarningThresholdContractsWired()
    {
        var serviceSource = ReadWorkerFile("InactiveReminderBackgroundService.cs");
        var optionsSource = ReadWorkerFile("InactiveReminderWorkerOptions.cs");

        serviceSource.Should().Contain("public sealed class InactiveReminderBackgroundService : BackgroundService");
        serviceSource.Should().Contain("private readonly IServiceScopeFactory _scopeFactory;");
        serviceSource.Should().Contain("private readonly IOptionsMonitor<InactiveReminderWorkerOptions> _optionsMonitor;");
        serviceSource.Should().Contain("private readonly ILogger<InactiveReminderBackgroundService> _logger;");
        serviceSource.Should().Contain("public InactiveReminderBackgroundService(");
        serviceSource.Should().Contain("_scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));");
        serviceSource.Should().Contain("_optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));");
        serviceSource.Should().Contain("_logger = logger ?? throw new ArgumentNullException(nameof(logger));");
        serviceSource.Should().Contain("protected override async Task ExecuteAsync(CancellationToken stoppingToken)");
        serviceSource.Should().Contain("_logger.LogInformation(\"Inactive reminder worker is disabled.\");");
        serviceSource.Should().Contain("_logger.LogInformation(\"Inactive reminder worker enabled.\");");
        serviceSource.Should().Contain("while (!stoppingToken.IsCancellationRequested)");
        serviceSource.Should().Contain("var options = _optionsMonitor.CurrentValue;");
        serviceSource.Should().Contain("if (!options.Enabled)");
        serviceSource.Should().Contain("await DelaySafeAsync(options.Interval, stoppingToken).ConfigureAwait(false);");
        serviceSource.Should().Contain("using var scope = _scopeFactory.CreateScope();");
        serviceSource.Should().Contain("GetRequiredService<ProcessInactiveReminderBatchHandler>()");
        serviceSource.Should().Contain("var result = await handler.HandleAsync(new ProcessInactiveReminderBatchDto");
        serviceSource.Should().Contain("InactiveThresholdDays = options.InactiveThresholdDays,");
        serviceSource.Should().Contain("CooldownHours = options.CooldownHours,");
        serviceSource.Should().Contain("MaxItems = options.MaxItemsPerRun");
        serviceSource.Should().Contain("if (!result.Succeeded || result.Value is null)");
        serviceSource.Should().Contain("_logger.LogWarning(\"Inactive reminder batch failed: {Error}\", result.Error ?? \"Unknown error\");");
        serviceSource.Should().Contain("var evaluated = Math.Max(1, result.Value.CandidatesEvaluated);");
        serviceSource.Should().Contain("var failedRatePercent = (result.Value.FailedCount * 100d) / evaluated;");
        serviceSource.Should().Contain("var cooldownSuppressionRatePercent = (result.Value.SuppressedByCooldownCount * 100d) / evaluated;");
        serviceSource.Should().Contain("FormatBreakdown(result.Value.FailureCodeCounts)");
        serviceSource.Should().Contain("FormatBreakdown(result.Value.SuppressionCodeCounts)");
        serviceSource.Should().Contain("if (failedRatePercent >= Math.Clamp(options.HighFailureRateWarningThresholdPercent, 0, 100))");
        serviceSource.Should().Contain("if (cooldownSuppressionRatePercent >= Math.Clamp(options.HighCooldownSuppressionWarningThresholdPercent, 0, 100))");
        serviceSource.Should().Contain("catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)");
        serviceSource.Should().Contain("catch (Exception ex)");
        serviceSource.Should().Contain("_logger.LogError(ex, \"Inactive reminder worker iteration failed unexpectedly.\");");
        serviceSource.Should().Contain("_logger.LogInformation(\"Inactive reminder worker stopped.\");");
        serviceSource.Should().Contain("private static Task DelaySafeAsync(TimeSpan requestedInterval, CancellationToken ct)");
        serviceSource.Should().Contain("requestedInterval < TimeSpan.FromMinutes(1)");
        serviceSource.Should().Contain("? TimeSpan.FromMinutes(1)");
        serviceSource.Should().Contain(": requestedInterval;");
        serviceSource.Should().Contain("return Task.Delay(interval, ct);");
        serviceSource.Should().Contain("private static string FormatBreakdown(IReadOnlyDictionary<string, int> counters)");
        serviceSource.Should().Contain("if (counters.Count == 0)");
        serviceSource.Should().Contain("return \"none\";");
        serviceSource.Should().Contain(".OrderByDescending(static kvp => kvp.Value)");
        serviceSource.Should().Contain(".ThenBy(static kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)");
        serviceSource.Should().Contain(".Select(static kvp => $\"{kvp.Key}={kvp.Value}\")");

        optionsSource.Should().Contain("public sealed class InactiveReminderWorkerOptions");
        optionsSource.Should().Contain("public bool Enabled { get; set; }");
        optionsSource.Should().Contain("public TimeSpan Interval { get; set; } = TimeSpan.FromHours(1);");
        optionsSource.Should().Contain("public int InactiveThresholdDays { get; set; } = 14;");
        optionsSource.Should().Contain("public int CooldownHours { get; set; } = 72;");
        optionsSource.Should().Contain("public int MaxItemsPerRun { get; set; } = 200;");
        optionsSource.Should().Contain("public int HighFailureRateWarningThresholdPercent { get; set; } = 30;");
        optionsSource.Should().Contain("public int HighCooldownSuppressionWarningThresholdPercent { get; set; } = 60;");
    }


    [Fact]
    public void MemoryLoginRateLimiter_Should_KeepSlidingWindowAndMinuteResetContractsWired()
    {
        var source = ReadInfrastructureFile(Path.Combine("Security", "LoginRateLimiter", "MemoryLoginRateLimiter.cs"));

        source.Should().Contain("public sealed class MemoryLoginRateLimiter : ILoginRateLimiter");
        source.Should().Contain("private readonly ConcurrentDictionary<string, (int count, DateTime windowStartUtc)> _entries = new();");
        source.Should().Contain("public Task<bool> IsAllowedAsync(string key, int maxAttempts, int windowSeconds, CancellationToken ct = default)");
        source.Should().Contain("var now = DateTime.UtcNow;");
        source.Should().Contain("var window = TimeSpan.FromSeconds(Math.Max(1, windowSeconds));");
        source.Should().Contain("var entry = _entries.GetOrAdd(key, _ => (0, now));");
        source.Should().Contain("if (now - entry.windowStartUtc > window)");
        source.Should().Contain("_entries[key] = (0, now);");
        source.Should().Contain("return Task.FromResult(true);");
        source.Should().Contain("return Task.FromResult(entry.count < maxAttempts);");
        source.Should().Contain("public Task RecordAsync(string key, CancellationToken ct = default)");
        source.Should().Contain("_entries.AddOrUpdate(key,");
        source.Should().Contain("_ => (1, now),");
        source.Should().Contain("var (count, start) = old;");
        source.Should().Contain("if (now - start > TimeSpan.FromMinutes(1))");
        source.Should().Contain("return (1, now);");
        source.Should().Contain("return (count + 1, start);");
        source.Should().Contain("return Task.CompletedTask;");
    }


    [Fact]
    public void SecurityFactorImplementations_Should_KeepSecurityStampAndTotpBranchContractsWired()
    {
        var securityStampSource = ReadInfrastructureFile(Path.Combine("Security", "SecurityStampService.cs"));
        var totpSource = ReadInfrastructureFile(Path.Combine("Security", "TotpService.cs"));

        securityStampSource.Should().Contain("public sealed class SecurityStampService : ISecurityStampService");
        securityStampSource.Should().Contain("Span<byte> bytes = stackalloc byte[32];");
        securityStampSource.Should().Contain("RandomNumberGenerator.Fill(bytes);");
        securityStampSource.Should().Contain("var sb = new StringBuilder(64);");
        securityStampSource.Should().Contain("foreach (var b in bytes) sb.Append(b.ToString(\"x2\"));");
        securityStampSource.Should().Contain("return sb.ToString();");
        securityStampSource.Should().Contain("public bool AreEqual(string? a, string? b)");
        securityStampSource.Should().Contain("var x = a ?? string.Empty;");
        securityStampSource.Should().Contain("var y = b ?? string.Empty;");
        securityStampSource.Should().Contain("if (x.Length != y.Length) return false;");
        securityStampSource.Should().Contain("var diff = 0;");
        securityStampSource.Should().Contain("diff |= x[i] ^ y[i];");
        securityStampSource.Should().Contain("return diff == 0;");
        securityStampSource.Should().Contain("public bool Equals(string? a, string? b) => AreEqual(a, b);");

        totpSource.Should().Contain("public sealed class TotpService : ITotpService");
        totpSource.Should().Contain("private const int StepSeconds = 30;");
        totpSource.Should().Contain("private const int Digits = 6;");
        totpSource.Should().Contain("public bool VerifyCode(string base32Secret, string code, int window = 1)");
        totpSource.Should().Contain("if (string.IsNullOrWhiteSpace(base32Secret) || string.IsNullOrWhiteSpace(code))");
        totpSource.Should().Contain("if (!int.TryParse(code, out var codeInt)) return false;");
        totpSource.Should().Contain("for (var w = -window; w <= window; w++)");
        totpSource.Should().Contain("var computed = ComputeTotp(base32Secret, utc.AddSeconds(w * StepSeconds));");
        totpSource.Should().Contain("if (computed == codeInt) return true;");
        totpSource.Should().Contain("return false;");
        totpSource.Should().Contain("public string GenerateCode(string base32Secret)");
        totpSource.Should().Contain("return code.ToString(new string('0', Digits));");
        totpSource.Should().Contain("private static int ComputeTotp(string base32Secret, DateTime utc)");
        totpSource.Should().Contain("var key = Base32Decode(base32Secret);");
        totpSource.Should().Contain("var timestep = (long)Math.Floor((utc - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds / StepSeconds);");
        totpSource.Should().Contain("if (BitConverter.IsLittleEndian) Array.Reverse(counter);");
        totpSource.Should().Contain("using var hmac = new HMACSHA1(key);");
        totpSource.Should().Contain("var offset = hash[^1] & 0x0F;");
        totpSource.Should().Contain("var code = binary % (int)Math.Pow(10, Digits);");
        totpSource.Should().Contain("private static byte[] Base32Decode(string input)");
        totpSource.Should().Contain("const string alphabet = \"ABCDEFGHIJKLMNOPQRSTUVWXYZ234567\";");
        totpSource.Should().Contain("var normalized = input.Replace(\"=\", string.Empty).ToUpperInvariant();");
        totpSource.Should().Contain("if (idx < 0) throw new FormatException(\"Invalid Base32 character.\");");
        totpSource.Should().Contain("return output.ToArray();");
    }


    [Fact]
    public void SecretAndPasswordImplementations_Should_KeepArgon2AndDataProtectionContractsWired()
    {
        var passwordHasherSource = ReadInfrastructureFile(Path.Combine("Security", "Argon2PasswordHasher.cs"));
        var secretProtectorSource = ReadInfrastructureFile(Path.Combine("Security", "Secrets", "DataProtectionSecretProtector.cs"));

        passwordHasherSource.Should().Contain("public sealed class Argon2PasswordHasher : IUserPasswordHasher");
        passwordHasherSource.Should().Contain("private const int DefaultTimeCost = 3;");
        passwordHasherSource.Should().Contain("private const int DefaultMemoryCostKiB = 65536;");
        passwordHasherSource.Should().Contain("private const int DefaultParallelism = 4;");
        passwordHasherSource.Should().Contain("private const int DefaultHashLength = 32;");
        passwordHasherSource.Should().Contain("private const int DefaultSaltLength = 16;");
        passwordHasherSource.Should().Contain("public string Hash(string password)");
        passwordHasherSource.Should().Contain("if (password is null) throw new ArgumentNullException(nameof(password));");
        passwordHasherSource.Should().Contain("var config = new Argon2Config");
        passwordHasherSource.Should().Contain("Type = Argon2Type.HybridAddressing");
        passwordHasherSource.Should().Contain("Version = Argon2Version.Nineteen");
        passwordHasherSource.Should().Contain("TimeCost = DefaultTimeCost,");
        passwordHasherSource.Should().Contain("MemoryCost = DefaultMemoryCostKiB,");
        passwordHasherSource.Should().Contain("Lanes = DefaultParallelism,");
        passwordHasherSource.Should().Contain("Threads = DefaultParallelism,");
        passwordHasherSource.Should().Contain("HashLength = DefaultHashLength,");
        passwordHasherSource.Should().Contain("Salt = RandomNumberGenerator.GetBytes(DefaultSaltLength),");
        passwordHasherSource.Should().Contain("Password = Encoding.UTF8.GetBytes(password)");
        passwordHasherSource.Should().Contain("using var argon2 = new Argon2(config);");
        passwordHasherSource.Should().Contain("var hash = argon2.Hash();");
        passwordHasherSource.Should().Contain("var phc = config.EncodeString(hash.Buffer);");
        passwordHasherSource.Should().Contain("Array.Clear(config.Password, 0, config.Password.Length);");
        passwordHasherSource.Should().Contain("return phc;");
        passwordHasherSource.Should().Contain("public bool Verify(string hashedPassword, string providedPassword)");
        passwordHasherSource.Should().Contain("if (hashedPassword is null) throw new ArgumentNullException(nameof(hashedPassword));");
        passwordHasherSource.Should().Contain("if (providedPassword is null) throw new ArgumentNullException(nameof(providedPassword));");
        passwordHasherSource.Should().Contain("return Argon2.Verify(hashedPassword, providedPassword);");

        secretProtectorSource.Should().Contain("public sealed class DataProtectionSecretProtector : ISecretProtector");
        secretProtectorSource.Should().Contain("private static readonly string Purpose = \"Darwin/TOTP/SecretBase32/v1\";");
        secretProtectorSource.Should().Contain("private readonly IDataProtector _protector;");
        secretProtectorSource.Should().Contain("public DataProtectionSecretProtector(IDataProtectionProvider provider)");
        secretProtectorSource.Should().Contain("if (provider is null) throw new ArgumentNullException(nameof(provider));");
        secretProtectorSource.Should().Contain("_protector = provider.CreateProtector(Purpose);");
        secretProtectorSource.Should().Contain("public string Protect(string plain)");
        secretProtectorSource.Should().Contain("if (plain is null) throw new ArgumentNullException(nameof(plain));");
        secretProtectorSource.Should().Contain("return _protector.Protect(plain);");
        secretProtectorSource.Should().Contain("public string Unprotect(string protectedData)");
        secretProtectorSource.Should().Contain("if (protectedData is null) throw new ArgumentNullException(nameof(protectedData));");
        secretProtectorSource.Should().Contain("return _protector.Unprotect(protectedData);");
    }


    [Fact]
    public void JwtTokenService_Should_KeepIssuanceBindingRevocationAndBusinessSelectionContractsWired()
    {
        var source = ReadInfrastructureFile(Path.Combine("Security", "Jwt", "JwtTokenService.cs"));

        source.Should().Contain("public sealed class JwtTokenService : IJwtTokenService");
        source.Should().Contain("private readonly IAppDbContext _db;");
        source.Should().Contain("public JwtTokenService(IAppDbContext db)");
        source.Should().Contain("_db = db ?? throw new ArgumentNullException(nameof(db));");
        source.Should().Contain("IssueTokensAsync(");
        source.Should().Contain("var settings = _db.Set<SiteSetting>()");
        source.Should().Contain(".AsNoTracking()");
        source.Should().Contain(".First();");
        source.Should().Contain("if (!settings.JwtEnabled)");
        source.Should().Contain("throw new InvalidOperationException(\"JWT is disabled by SiteSetting (JwtEnabled = false).\");");
        source.Should().Contain("var accessExp = nowUtc.AddMinutes(Math.Max(5, settings.JwtAccessTokenMinutes));");
        source.Should().Contain("var refreshExp = nowUtc.AddDays(Math.Max(1, settings.JwtRefreshTokenDays));");
        source.Should().Contain("var signingKeyMaterial = settings.JwtSigningKey ?? string.Empty;");
        source.Should().Contain("var signingKey = new SymmetricSecurityKey(GetKeyBytes(signingKeyMaterial));");
        source.Should().Contain("var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);");
        source.Should().Contain("new(JwtRegisteredClaimNames.Sub, userId.ToString()),");
        source.Should().Contain("new(JwtRegisteredClaimNames.Email, email),");
        source.Should().Contain("new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString(\"N\")),");
        source.Should().Contain("ToUnixTimestampSeconds(nowUtc),");
        source.Should().Contain("if (settings.JwtEmitScopes && scopes is not null)");
        source.Should().Contain("claims.Add(new Claim(\"scope\", string.Join(\",\", scopes)));");
        source.Should().Contain("var businessId = ResolveActiveBusinessId(userId, preferredBusinessId);");
        source.Should().Contain("claims.Add(new Claim(\"business_id\", businessId.Value.ToString(\"D\")));");
        source.Should().Contain("var jwt = new JwtSecurityToken(");
        source.Should().Contain("var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);");
        source.Should().Contain("if (settings.JwtRequireDeviceBinding)");
        source.Should().Contain("if (string.IsNullOrWhiteSpace(deviceId))");
        source.Should().Contain("\"Device binding is required (JwtRequireDeviceBinding = true) but no device id was supplied.\"");
        source.Should().Contain("var purpose = BuildRefreshPurpose(effectiveDeviceId);");
        source.Should().Contain("if (settings.JwtSingleDeviceOnly)");
        source.Should().Contain("RevokeAllForUser(userId);");
        source.Should().Contain("var refreshToken = CreateOpaqueToken();");
        source.Should().Contain("var refreshRow = new UserToken(userId, purpose, refreshToken, refreshExp);");
        source.Should().Contain("var existingRow = _db.Set<UserToken>()");
        source.Should().Contain(".FirstOrDefault(x => x.UserId == userId && x.Purpose == purpose);");
        source.Should().Contain("existingRow.Value = refreshToken;");
        source.Should().Contain("existingRow.ExpiresAtUtc = refreshExp;");
        source.Should().Contain("existingRow.UsedAtUtc = null;");
        source.Should().Contain("_db.Set<UserToken>().Add(refreshRow);");
        source.Should().Contain("_db.SaveChangesAsync().GetAwaiter().GetResult();");
        source.Should().Contain("return (accessToken, accessExp, refreshToken, refreshExp);");
        source.Should().Contain("public Guid? ValidateRefreshToken(string refreshToken, string? deviceId)");
        source.Should().Contain("if (string.IsNullOrWhiteSpace(refreshToken))");
        source.Should().Contain("return null;");
        source.Should().Contain("if (settings.JwtRequireDeviceBinding)");
        source.Should().Contain("if (string.IsNullOrWhiteSpace(deviceId))");
        source.Should().Contain("var row = _db.Set<UserToken>()");
        source.Should().Contain("x.Purpose == purpose &&");
        source.Should().Contain("x.Value == refreshToken &&");
        source.Should().Contain("x.UsedAtUtc == null");
        source.Should().Contain("if (row.ExpiresAtUtc.HasValue && row.ExpiresAtUtc.Value < DateTime.UtcNow)");
        source.Should().Contain("return row.UserId;");
        source.Should().Contain("public void RevokeRefreshToken(string refreshToken, string? deviceId)");
        source.Should().Contain("if (settings.JwtRequireDeviceBinding && !string.IsNullOrWhiteSpace(deviceId))");
        source.Should().Contain("row = tokens.FirstOrDefault(x => x.Purpose == purpose && x.Value == refreshToken);");
        source.Should().Contain("row = tokens.FirstOrDefault(x => x.Value == refreshToken && x.Purpose.StartsWith(\"JwtRefresh\"));");
        source.Should().Contain("if (row.UsedAtUtc is null)");
        source.Should().Contain("row.UsedAtUtc = DateTime.UtcNow;");
        source.Should().Contain("public int RevokeAllForUser(Guid userId)");
        source.Should().Contain("var rows = _db.Set<UserToken>()");
        source.Should().Contain(".Where(x => x.UserId == userId && x.Purpose.StartsWith(\"JwtRefresh\"))");
        source.Should().Contain(".ToList();");
        source.Should().Contain("if (changed)");
        source.Should().Contain("return rows.Count;");
        source.Should().Contain("private static string ToUnixTimestampSeconds(DateTime utc)");
        source.Should().Contain("return seconds.ToString(CultureInfo.InvariantCulture);");
        source.Should().Contain("private static byte[] GetKeyBytes(string key)");
        source.Should().Contain("if (string.IsNullOrWhiteSpace(key))");
        source.Should().Contain("throw new InvalidOperationException(\"JWT signing key (SiteSetting.JwtSigningKey) is not configured.\");");
        source.Should().Contain("return Convert.FromBase64String(key);");
        source.Should().Contain("catch (FormatException)");
        source.Should().Contain("return Encoding.UTF8.GetBytes(key);");
        source.Should().Contain("private static string CreateOpaqueToken()");
        source.Should().Contain("Span<byte> bytes = stackalloc byte[32];");
        source.Should().Contain("return Convert.ToHexString(bytes).ToLowerInvariant();");
        source.Should().Contain("private static string BuildRefreshPurpose(string? deviceId) =>");
        source.Should().Contain("string.IsNullOrWhiteSpace(deviceId) ? \"JwtRefresh\" : $\"JwtRefresh:{deviceId}\";");
        source.Should().Contain("private Guid? ResolveActiveBusinessId(Guid userId, Guid? preferredBusinessId)");
        source.Should().Contain("if (preferredBusinessId.HasValue)");
        source.Should().Contain("join b in _db.Set<Business>() on m.BusinessId equals b.Id");
        source.Should().Contain("&& m.BusinessId == preferredBusinessId.Value");
        source.Should().Contain("select (Guid?)m.BusinessId)");
        source.Should().Contain("if (preferredMatch.HasValue)");
        source.Should().Contain("orderby m.BusinessId");
        source.Should().Contain("select (Guid?)m.BusinessId)");
    }


    [Fact]
    public void WebApiJwtValidationInfrastructure_Should_KeepBearerSetupAndSigningProviderContractsWired()
    {
        var bearerSetupSource = ReadWebApiFile(Path.Combine("Security", "JwtBearerOptionsSetup.cs"));
        var signingProviderSource = ReadWebApiFile(Path.Combine("Security", "JwtSigningParametersProvider.cs"));

        bearerSetupSource.Should().Contain("public sealed class JwtBearerOptionsSetup : IConfigureNamedOptions<JwtBearerOptions>");
        bearerSetupSource.Should().Contain("private readonly JwtSigningParametersProvider _provider;");
        bearerSetupSource.Should().Contain("public JwtBearerOptionsSetup(JwtSigningParametersProvider provider)");
        bearerSetupSource.Should().Contain("_provider = provider ?? throw new ArgumentNullException(nameof(provider));");
        bearerSetupSource.Should().Contain("public void Configure(JwtBearerOptions options)");
        bearerSetupSource.Should().Contain("Configure(Options.DefaultName, options);");
        bearerSetupSource.Should().Contain("public void Configure(string? name, JwtBearerOptions options)");
        bearerSetupSource.Should().Contain("if (!string.Equals(name, JwtBearerDefaults.AuthenticationScheme, StringComparison.Ordinal))");
        bearerSetupSource.Should().Contain("return;");
        bearerSetupSource.Should().Contain("var p = _provider.GetParameters();");
        bearerSetupSource.Should().Contain("options.TokenValidationParameters = new TokenValidationParameters");
        bearerSetupSource.Should().Contain("ValidateIssuer = true,");
        bearerSetupSource.Should().Contain("ValidIssuer = p.Issuer,");
        bearerSetupSource.Should().Contain("ValidateAudience = true,");
        bearerSetupSource.Should().Contain("ValidAudience = p.Audience,");
        bearerSetupSource.Should().Contain("ValidateIssuerSigningKey = true,");
        bearerSetupSource.Should().Contain("IssuerSigningKeyResolver = (_, _, _, _) => p.SigningKeys,");
        bearerSetupSource.Should().Contain("RequireExpirationTime = true,");
        bearerSetupSource.Should().Contain("ValidateLifetime = true,");
        bearerSetupSource.Should().Contain("ClockSkew = p.ClockSkew");

        signingProviderSource.Should().Contain("public sealed class JwtSigningParametersProvider");
        signingProviderSource.Should().Contain("private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(1);");
        signingProviderSource.Should().Contain("private readonly IServiceScopeFactory _scopeFactory;");
        signingProviderSource.Should().Contain("private readonly IClock _clock;");
        signingProviderSource.Should().Contain("private readonly ILogger<JwtSigningParametersProvider> _logger;");
        signingProviderSource.Should().Contain("private readonly IStringLocalizer<ValidationResource> _validationLocalizer;");
        signingProviderSource.Should().Contain("private readonly object _sync = new();");
        signingProviderSource.Should().Contain("private DateTime _lastReadUtc;");
        signingProviderSource.Should().Contain("private CachedSigningParameters? _cache;");
        signingProviderSource.Should().Contain("public JwtSigningParametersProvider(");
        signingProviderSource.Should().Contain("IServiceScopeFactory scopeFactory,");
        signingProviderSource.Should().Contain("IClock clock,");
        signingProviderSource.Should().Contain("ILogger<JwtSigningParametersProvider> logger,");
        signingProviderSource.Should().Contain("IStringLocalizer<ValidationResource> validationLocalizer)");
        signingProviderSource.Should().Contain("_scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));");
        signingProviderSource.Should().Contain("_clock = clock ?? throw new ArgumentNullException(nameof(clock));");
        signingProviderSource.Should().Contain("_logger = logger ?? throw new ArgumentNullException(nameof(logger));");
        signingProviderSource.Should().Contain("_validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));");
        signingProviderSource.Should().Contain("public CachedSigningParameters GetParameters()");
        signingProviderSource.Should().Contain("lock (_sync)");
        signingProviderSource.Should().Contain("var nowUtc = _clock.UtcNow;");
        signingProviderSource.Should().Contain("if (_cache is not null && (nowUtc - _lastReadUtc) <= DefaultCacheDuration)");
        signingProviderSource.Should().Contain("using var scope = _scopeFactory.CreateScope();");
        signingProviderSource.Should().Contain("var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();");
        signingProviderSource.Should().Contain("var s = db.Set<SiteSetting>()");
        signingProviderSource.Should().Contain(".FirstOrDefault(x => !x.IsDeleted);");
        signingProviderSource.Should().Contain("if (s is null)");
        signingProviderSource.Should().Contain("throw new InvalidOperationException(_validationLocalizer[\"JwtValidationSiteSettingsMissing\"]);");
        signingProviderSource.Should().Contain("if (s.JwtEnabled == false)");
        signingProviderSource.Should().Contain("throw new InvalidOperationException(_validationLocalizer[\"JwtValidationDisabled\"]);");
        signingProviderSource.Should().Contain("var keys = new List<SecurityKey>");
        signingProviderSource.Should().Contain("new SymmetricSecurityKey(GetKeyBytes(s.JwtSigningKey))");
        signingProviderSource.Should().Contain("if (!string.IsNullOrWhiteSpace(s.JwtPreviousSigningKey))");
        signingProviderSource.Should().Contain("keys.Add(new SymmetricSecurityKey(GetKeyBytes(s.JwtPreviousSigningKey)));");
        signingProviderSource.Should().Contain("throw new InvalidOperationException(_validationLocalizer[\"JwtSigningKeyMissingInSiteSettings\"]);");
        signingProviderSource.Should().Contain("var skewSeconds = s.JwtClockSkewSeconds;");
        signingProviderSource.Should().Contain("if (skewSeconds < 0) skewSeconds = 0;");
        signingProviderSource.Should().Contain("_cache = new CachedSigningParameters(");
        signingProviderSource.Should().Contain("issuer: s.JwtIssuer ?? \"Darwin\",");
        signingProviderSource.Should().Contain("audience: s.JwtAudience ?? \"Darwin.PublicApi\",");
        signingProviderSource.Should().Contain("clockSkew: TimeSpan.FromSeconds(skewSeconds),");
        signingProviderSource.Should().Contain("signingKeys: keys);");
        signingProviderSource.Should().Contain("_lastReadUtc = nowUtc;");
        signingProviderSource.Should().Contain("_logger.LogDebug(");
        signingProviderSource.Should().Contain("private byte[] GetKeyBytes(string? key)");
        signingProviderSource.Should().Contain("if (string.IsNullOrWhiteSpace(key))");
        signingProviderSource.Should().Contain("throw new InvalidOperationException(_validationLocalizer[\"JwtSigningKeyMissingInSiteSettings\"]);");
        signingProviderSource.Should().Contain("return Convert.FromBase64String(key);");
        signingProviderSource.Should().Contain("catch");
        signingProviderSource.Should().Contain("return System.Text.Encoding.UTF8.GetBytes(key);");
        signingProviderSource.Should().Contain("public sealed class CachedSigningParameters");
        signingProviderSource.Should().Contain("public string Issuer { get; }");
        signingProviderSource.Should().Contain("public string Audience { get; }");
        signingProviderSource.Should().Contain("public TimeSpan ClockSkew { get; }");
        signingProviderSource.Should().Contain("public IReadOnlyList<SecurityKey> SigningKeys { get; }");
    }


    [Fact]
    public void WebApiCompositionRoot_Should_KeepHostedServiceHandlerScanAndStartupHandoffContractsWired()
    {
        var diSource = ReadWebApiFile(Path.Combine("Extensions", "DependencyInjection.cs"));
        var startupSource = ReadWebApiFile(Path.Combine("Extensions", "Startup.cs"));
        var programSource = ReadWebApiFile("Program.cs");

        diSource.Should().Contain("public static IServiceCollection AddWebApiComposition(this IServiceCollection services, IConfiguration configuration)");
        diSource.Should().Contain("if (services is null) throw new ArgumentNullException(nameof(services));");
        diSource.Should().Contain("if (configuration is null) throw new ArgumentNullException(nameof(configuration));");
        diSource.Should().Contain("services.AddApplication();");
        diSource.Should().Contain("services.AddScoped<IClock, SystemClock>();");
        diSource.Should().Contain("services.AddHttpContextAccessor();");
        diSource.Should().Contain("services.AddLocalization();");
        diSource.Should().Contain("services.AddScoped<ICurrentUserService, CurrentUserService>();");
        diSource.Should().Contain("services.AddPersistence(configuration);");
        diSource.Should().Contain("services.AddSharedHostingDataProtection(configuration);");
        diSource.Should().Contain("services.AddIdentityInfrastructure();");
        diSource.Should().Contain("services.AddJwtAuthCore();");
        diSource.Should().Contain("services.AddAuthorization();");
        diSource.Should().Contain("services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();");
        diSource.Should().Contain("ServiceDescriptor.Scoped<IAuthorizationHandler, PermissionAuthorizationHandler>()");
        diSource.Should().Contain("services.AddNotificationsInfrastructure(configuration);");
        diSource.Should().Contain("services.AddSingleton<StorefrontCheckoutUrlBuilder>();");
        diSource.Should().Contain("services.AddLoyaltyPresentationServices();");
        diSource.Should().Contain("services.AddScoped<GetAvailableLoyaltyRewardsForBusinessHandler>();");
        diSource.Should().Contain("services.AddSingleton<IHtmlSanitizer>(_ => HtmlSanitizerFactory.Create());");
        diSource.Should().Contain("services.AddScoped<IAddOnPricingService, AddOnPricingService>();");
        diSource.Should().Contain("services.AddScoped<ScanSessionTokenResolver>();");
        diSource.Should().Contain("var appAssembly = typeof(GetAppBootstrapHandler).Assembly;");
        diSource.Should().Contain("t.Name.EndsWith(\"Handler\", StringComparison.OrdinalIgnoreCase)");
        diSource.Should().Contain("services.AddScoped(handlerType);");
        diSource.Should().Contain("catch (ReflectionTypeLoadException rtlEx)");
        diSource.Should().Contain("throw new InvalidOperationException($\"Handler auto-registration failed. Loader errors: {loaderMessages}\", rtlEx);");
        diSource.Should().Contain(".AddControllers()");
        diSource.Should().Contain("PropertyNamingPolicy = JsonNamingPolicy.CamelCase;");
        diSource.Should().Contain("PropertyNameCaseInsensitive = true;");
        diSource.Should().Contain("JsonIgnoreCondition.WhenWritingNull");
        diSource.Should().Contain("services.AddEndpointsApiExplorer();");
        diSource.Should().Contain("services.AddSwaggerGen();");
        diSource.Should().Contain("services.TryAddSingleton<JwtSigningParametersProvider>();");
        diSource.Should().Contain("ServiceDescriptor.Singleton<IConfigureOptions<JwtBearerOptions>, JwtBearerOptionsSetup>()");
        diSource.Should().Contain(".AddJwtBearer();");
        diSource.Should().Contain("options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;");
        diSource.Should().Contain("options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;");

        startupSource.Should().Contain("public static async Task<WebApplication> UseWebApiStartupAsync(this WebApplication app)");
        startupSource.Should().Contain("if (app is null) throw new ArgumentNullException(nameof(app));");
        startupSource.Should().Contain("var env = app.Environment;");
        startupSource.Should().Contain("if (env.IsDevelopment())");
        startupSource.Should().Contain("app.UseDeveloperExceptionPage();");
        startupSource.Should().Contain("await app.Services.MigrateAndSeedAsync();");
        startupSource.Should().Contain("app.UseSwagger();");
        startupSource.Should().Contain("app.UseSwaggerUI();");
        startupSource.Should().Contain("app.UseExceptionHandler(\"/error\");");
        startupSource.Should().Contain("app.UseHsts();");
        startupSource.Should().Contain("if (!env.IsDevelopment())");
        startupSource.Should().Contain("app.UseHttpsRedirection();");
        startupSource.Should().Contain("app.UseRouting();");
        startupSource.Should().Contain("app.UseRateLimiter();");
        startupSource.Should().Contain("app.UseMiddleware<Darwin.WebApi.Middleware.IdempotencyMiddleware>();");
        startupSource.Should().Contain("app.UseAuthentication();");
        startupSource.Should().Contain("app.UseMiddleware<ErrorHandlingMiddleware>();");
        startupSource.Should().Contain("app.UseAuthorization();");
        startupSource.Should().Contain("app.MapControllers();");
        startupSource.Should().Contain("return app;");

        programSource.Should().Contain("var builder = WebApplication.CreateBuilder(args);");
        programSource.Should().Contain("builder.Services.AddWebApiComposition(builder.Configuration);");
        programSource.Should().Contain("var app = builder.Build();");
        programSource.Should().Contain("await app.UseWebApiStartupAsync();");
        programSource.Should().Contain("await app.RunAsync();");
        programSource.Should().Contain("public partial class Program { }");
    }


    [Fact]
    public void WebApiAuthRuntimePrimitives_Should_KeepCurrentUserPermissionAndProblemShapingContractsWired()
    {
        var currentUserSource = ReadWebApiFile(Path.Combine("Auth", "CurrentUserService.cs"));
        var permissionSource = ReadWebApiFile(Path.Combine("Auth", "PermissionAuthorization.cs"));
        var apiControllerBaseSource = ReadWebApiFile(Path.Combine("Controllers", "ApiControllerBase.cs"));

        currentUserSource.Should().Contain("public sealed class CurrentUserService : ICurrentUserService");
        currentUserSource.Should().Contain("private readonly IHttpContextAccessor _httpContextAccessor;");
        currentUserSource.Should().Contain("public CurrentUserService(IHttpContextAccessor httpContextAccessor)");
        currentUserSource.Should().Contain("_httpContextAccessor = httpContextAccessor");
        currentUserSource.Should().Contain("?? throw new ArgumentNullException(nameof(httpContextAccessor));");
        currentUserSource.Should().Contain("public Guid GetCurrentUserId()");
        currentUserSource.Should().Contain("var httpContext = _httpContextAccessor.HttpContext;");
        currentUserSource.Should().Contain("var user = httpContext?.User;");
        currentUserSource.Should().Contain("if (user?.Identity?.IsAuthenticated == true)");
        currentUserSource.Should().Contain("user.FindFirstValue(ClaimTypes.NameIdentifier)");
        currentUserSource.Should().Contain("?? user.FindFirstValue(\"sub\")");
        currentUserSource.Should().Contain("?? user.FindFirstValue(\"uid\");");
        currentUserSource.Should().Contain("if (!string.IsNullOrWhiteSpace(id) && Guid.TryParse(id, out var parsed))");
        currentUserSource.Should().Contain("return parsed;");
        currentUserSource.Should().Contain("\"No authenticated user id is available in the current HTTP context.\"");

        permissionSource.Should().Contain("public sealed class PermissionRequirement : IAuthorizationRequirement");
        permissionSource.Should().Contain("if (string.IsNullOrWhiteSpace(permissionKey))");
        permissionSource.Should().Contain("Permission key must not be null or whitespace.");
        permissionSource.Should().Contain("public string PermissionKey { get; }");
        permissionSource.Should().Contain("public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider");
        permissionSource.Should().Contain("private const string PolicyPrefix = \"perm:\";");
        permissionSource.Should().Contain("private readonly DefaultAuthorizationPolicyProvider _fallback;");
        permissionSource.Should().Contain("public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)");
        permissionSource.Should().Contain("_fallback = new DefaultAuthorizationPolicyProvider(options);");
        permissionSource.Should().Contain("return _fallback.GetFallbackPolicyAsync();");
        permissionSource.Should().Contain("return _fallback.GetDefaultPolicyAsync();");
        permissionSource.Should().Contain("!policyName.StartsWith(PolicyPrefix, StringComparison.Ordinal)");
        permissionSource.Should().Contain("var permissionKey = policyName.Substring(PolicyPrefix.Length);");
        permissionSource.Should().Contain("var requirement = new PermissionRequirement(permissionKey);");
        permissionSource.Should().Contain("new AuthorizationPolicyBuilder()");
        permissionSource.Should().Contain(".AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)");
        permissionSource.Should().Contain(".AddRequirements(requirement);");
        permissionSource.Should().Contain("return Task.FromResult<AuthorizationPolicy?>(policy);");
        permissionSource.Should().Contain("public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>");
        permissionSource.Should().Contain("private readonly IPermissionService _permissions;");
        permissionSource.Should().Contain("protected override async Task HandleRequirementAsync(");
        permissionSource.Should().Contain("if (context is null)");
        permissionSource.Should().Contain("if (requirement is null)");
        permissionSource.Should().Contain("var subjectClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);");
        permissionSource.Should().Contain("if (subjectClaim is null || string.IsNullOrWhiteSpace(subjectClaim.Value))");
        permissionSource.Should().Contain("if (!Guid.TryParse(subjectClaim.Value, out var userId))");
        permissionSource.Should().Contain("var ct = CancellationToken.None;");
        permissionSource.Should().Contain("if (await _permissions.HasAsync(userId, \"FullAdminAccess\", ct).ConfigureAwait(false))");
        permissionSource.Should().Contain("context.Succeed(requirement);");
        permissionSource.Should().Contain("if (await _permissions.HasAsync(userId, requirement.PermissionKey, ct).ConfigureAwait(false))");
        permissionSource.Should().Contain("public sealed class HasPermissionAttribute : AuthorizeAttribute");
        permissionSource.Should().Contain("Policy = $\"perm:{permissionKey}\";");

        apiControllerBaseSource.Should().Contain("[ApiController]");
        apiControllerBaseSource.Should().Contain("public abstract class ApiControllerBase : ControllerBase");
        apiControllerBaseSource.Should().Contain("protected IActionResult BadRequestProblem(string title, string? detail = null)");
        apiControllerBaseSource.Should().Contain("if (string.IsNullOrWhiteSpace(title))");
        apiControllerBaseSource.Should().Contain("throw new ArgumentException(\"Title is required.\", nameof(title));");
        apiControllerBaseSource.Should().Contain("var problem = new ContractProblemDetails");
        apiControllerBaseSource.Should().Contain("Status = StatusCodes.Status400BadRequest,");
        apiControllerBaseSource.Should().Contain("Title = title.Trim(),");
        apiControllerBaseSource.Should().Contain("Detail = string.IsNullOrWhiteSpace(detail) ? null : detail.Trim(),");
        apiControllerBaseSource.Should().Contain("Instance = HttpContext?.Request?.Path.Value");
        apiControllerBaseSource.Should().Contain("return StatusCode(StatusCodes.Status400BadRequest, problem);");
        apiControllerBaseSource.Should().Contain("protected IActionResult NotFoundProblem(string title, string? detail = null)");
        apiControllerBaseSource.Should().Contain("Status = StatusCodes.Status404NotFound,");
        apiControllerBaseSource.Should().Contain("return StatusCode(StatusCodes.Status404NotFound, problem);");
        apiControllerBaseSource.Should().Contain("protected IActionResult ProblemFromResult(Result result, string fallbackTitle = \"Request failed.\")");
        apiControllerBaseSource.Should().Contain("if (result is null) throw new ArgumentNullException(nameof(result));");
        apiControllerBaseSource.Should().Contain("if (result.Succeeded)");
        apiControllerBaseSource.Should().Contain("return StatusCode(StatusCodes.Status500InternalServerError);");
        apiControllerBaseSource.Should().Contain("var title = string.IsNullOrWhiteSpace(result.Error) ? fallbackTitle : result.Error!;");
        apiControllerBaseSource.Should().Contain("protected IActionResult ProblemFromResult<T>(Result<T> result, string fallbackTitle = \"Request failed.\")");
        apiControllerBaseSource.Should().Contain("protected Guid? GetCurrentUserId()");
        apiControllerBaseSource.Should().Contain("if (User?.Identity?.IsAuthenticated != true)");
        apiControllerBaseSource.Should().Contain("return null;");
        apiControllerBaseSource.Should().Contain("User.FindFirstValue(ClaimTypes.NameIdentifier) ??");
        apiControllerBaseSource.Should().Contain("User.FindFirstValue(\"sub\") ??");
        apiControllerBaseSource.Should().Contain("User.FindFirstValue(\"uid\");");
        apiControllerBaseSource.Should().Contain("return Guid.TryParse(candidate, out var userId) ? userId : null;");
    }


    [Fact]
    public void AuthController_Should_KeepLoginRefreshLogoutAndRegistrationOrchestrationContractsWired()
    {
        var source = ReadWebApiFile(Path.Combine("Controllers", "AuthController.cs"));

        source.Should().Contain("public sealed class AuthController : ApiControllerBase");
        source.Should().Contain("private readonly LoginWithPasswordHandler _loginWithPassword;");
        source.Should().Contain("private readonly RefreshTokenHandler _refresh;");
        source.Should().Contain("private readonly RevokeRefreshTokensHandler _revoke;");
        source.Should().Contain("private readonly RegisterUserHandler _registerUser;");
        source.Should().Contain("private readonly ChangePasswordHandler _changePassword;");
        source.Should().Contain("private readonly RequestEmailConfirmationHandler _requestEmailConfirmation;");
        source.Should().Contain("private readonly ConfirmEmailHandler _confirmEmail;");
        source.Should().Contain("private readonly RequestPasswordResetHandler _requestPasswordReset;");
        source.Should().Contain("private readonly ResetPasswordHandler _resetPassword;");
        source.Should().Contain("private readonly GetRoleIdByKeyHandler _getRoleIdByKey;");
        source.Should().Contain("private readonly IStringLocalizer<ValidationResource> _validationLocalizer;");
        source.Should().Contain("private readonly ILogger<AuthController> _logger;");
        source.Should().Contain("_loginWithPassword = loginWithPassword ?? throw new ArgumentNullException(nameof(loginWithPassword));");
        source.Should().Contain("_refresh = refresh ?? throw new ArgumentNullException(nameof(refresh));");
        source.Should().Contain("_revoke = revoke ?? throw new ArgumentNullException(nameof(revoke));");
        source.Should().Contain("_registerUser = registerUser ?? throw new ArgumentNullException(nameof(registerUser));");
        source.Should().Contain("_changePassword = changePassword ?? throw new ArgumentNullException(nameof(changePassword));");
        source.Should().Contain("_requestEmailConfirmation = requestEmailConfirmation ?? throw new ArgumentNullException(nameof(requestEmailConfirmation));");
        source.Should().Contain("_confirmEmail = confirmEmail ?? throw new ArgumentNullException(nameof(confirmEmail));");
        source.Should().Contain("_requestPasswordReset = requestPasswordReset ?? throw new ArgumentNullException(nameof(requestPasswordReset));");
        source.Should().Contain("_resetPassword = resetPassword ?? throw new ArgumentNullException(nameof(resetPassword));");
        source.Should().Contain("_getRoleIdByKey = getRoleIdByKey ?? throw new ArgumentNullException(nameof(getRoleIdByKey));");
        source.Should().Contain("_validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));");
        source.Should().Contain("_logger = logger ?? throw new ArgumentNullException(nameof(logger));");
        source.Should().Contain("public async Task<IActionResult> LoginAsync(");
        source.Should().Contain("if (request is null)");
        source.Should().Contain("return BadRequestProblem(_validationLocalizer[\"RequestPayloadRequired\"]);");
        source.Should().Contain("var dto = new PasswordLoginRequestDto");
        source.Should().Contain("Email = request.Email ?? string.Empty,");
        source.Should().Contain("PasswordPlain = request.Password ?? string.Empty,");
        source.Should().Contain("DeviceId = request.DeviceId,");
        source.Should().Contain("BusinessId = request.BusinessId");
        source.Should().Contain("var rateKey = BuildRateKey(request.Email);");
        source.Should().Contain("var result = await _loginWithPassword.HandleAsync(dto, rateKey, ct);");
        source.Should().Contain("if (result.Succeeded && result.Value is not null)");
        source.Should().Contain("_logger.LogInformation(");
        source.Should().Contain("\"Login succeeded for user. UserId={UserId}, Email={Email}, Ip={Ip}\"");
        source.Should().Contain("var tokenResponse = MapToTokenResponse(authResult);");
        source.Should().Contain("return Ok(tokenResponse);");
        source.Should().Contain("_logger.LogWarning(");
        source.Should().Contain("\"Login failed. Email={Email}, Ip={Ip}, Error={Error}\"");
        source.Should().Contain("return ProblemFromResult(result);");
        source.Should().Contain("public async Task<IActionResult> RefreshAsync(");
        source.Should().Contain("var dto = new RefreshRequestDto");
        source.Should().Contain("RefreshToken = request.RefreshToken ?? string.Empty,");
        source.Should().Contain("var result = await _refresh.HandleAsync(dto, ct);");
        source.Should().Contain("var tokenSuffix = GetRefreshTokenSuffix(dto.RefreshToken);");
        source.Should().Contain("\"Token refresh succeeded. UserId={UserId}, Email={Email}, Ip={Ip}, TokenSuffix={TokenSuffix}\"");
        source.Should().Contain("\"Token refresh failed. Ip={Ip}, TokenSuffix={TokenSuffix}, Error={Error}\"");
        source.Should().Contain("public async Task<IActionResult> LogoutAsync(");
        source.Should().Contain("var dto = new RevokeRefreshRequestDto");
        source.Should().Contain("RefreshToken = request.RefreshToken,");
        source.Should().Contain("UserId = null,");
        source.Should().Contain("DeviceId = null");
        source.Should().Contain("var result = await _revoke.HandleAsync(dto, ct);");
        source.Should().Contain("var userId = GetUserIdFromClaims(HttpContext.User);");
        source.Should().Contain("\"Logout succeeded. UserId={UserId}, Ip={Ip}, TokenSuffix={TokenSuffix}\"");
        source.Should().Contain("\"Logout failed. UserId={UserId}, Ip={Ip}, TokenSuffix={TokenSuffix}, Error={Error}\"");
        source.Should().Contain("public async Task<IActionResult> LogoutAllAsync(CancellationToken ct)");
        source.Should().Contain("if (userId is null)");
        source.Should().Contain("var problem = new Darwin.Contracts.Common.ProblemDetails");
        source.Should().Contain("Status = 401,");
        source.Should().Contain("Title = _validationLocalizer[\"UnauthorizedTitle\"],");
        source.Should().Contain("Detail = _validationLocalizer[\"AuthenticatedUserIdentifierNotResolved\"],");
        source.Should().Contain("return StatusCode(problem.Status, problem);");
        source.Should().Contain("UserId = userId,");
        source.Should().Contain("\"Logout-all succeeded. UserId={UserId}, Ip={Ip}\"");
        source.Should().Contain("\"Logout-all failed. UserId={UserId}, Ip={Ip}, Error={Error}\"");
        source.Should().Contain("public async Task<IActionResult> RegisterAsync(");
        source.Should().Contain("var dto = new UserCreateDto");
        source.Should().Contain("Email = request.Email ?? string.Empty,");
        source.Should().Contain("Password = request.Password ?? string.Empty,");
        source.Should().Contain("FirstName = request.FirstName ?? string.Empty,");
        source.Should().Contain("LastName = request.LastName ?? string.Empty,");
        source.Should().Contain("IsActive = true,");
        source.Should().Contain("IsSystem = false");
        source.Should().Contain("Guid? defaultRoleId = null;");
        source.Should().Contain("var roleResult = await _getRoleIdByKey.HandleAsync(\"Members\", ct).ConfigureAwait(false);");
        source.Should().Contain("defaultRoleId = roleResult.Value;");
        source.Should().Contain("\"Default role not found for key 'Members'. New users will be created without a default role. Error={Error}\"");
        source.Should().Contain("_logger.LogError(ex, \"Error resolving default role for registration.\");");
        source.Should().Contain("var result = await _registerUser.HandleAsync(dto, defaultRoleId, ct).ConfigureAwait(false);");
        source.Should().Contain("var confirmationResult = await _requestEmailConfirmation.HandleAsync(");
        source.Should().Contain("new RequestEmailConfirmationDto { Email = dto.Email },");
        source.Should().Contain("confirmationEmailSent = confirmationResult.Succeeded;");
        source.Should().Contain("_logger.LogError(ex, \"Failed to send confirmation email for newly registered user {Email}.\", dto.Email);");
        source.Should().Contain("var response = new RegisterResponse");
        source.Should().Contain("DisplayName = $\"{dto.FirstName} {dto.LastName}\".Trim(),");
        source.Should().Contain("ConfirmationEmailSent = confirmationEmailSent");
        source.Should().Contain("return Ok(response);");
        source.Should().Contain("Title = _validationLocalizer[\"UnauthorizedTitle\"],");
        source.Should().Contain("Detail = _validationLocalizer[\"AuthenticatedUserIdentifierNotResolved\"],");
        source.Should().Contain("var fallbackMessage = _validationLocalizer[\"OperationFailed\"];");
        source.Should().Contain("Title = fallbackMessage,");
        source.Should().Contain("Detail = result.Error ?? fallbackMessage,");
        source.Should().NotContain("throw new ArgumentNullException(nameof(request));");
        source.Should().NotContain("Title = \"Unauthorized\",");
        source.Should().NotContain("Detail = \"User identifier could not be resolved from the access token.\",");
        source.Should().NotContain("Title = \"Request failed\",");
        source.Should().NotContain("Detail = result.Error ?? \"The operation could not be completed.\",");
    }


    [Fact]
    public void BusinessAuthController_Should_KeepInvitationPreviewAndAcceptanceOrchestrationContractsWired()
    {
        var source = ReadWebApiFile(Path.Combine("Controllers", "Business", "BusinessAuthController.cs"));

        source.Should().Contain("public sealed class BusinessAuthController : ApiControllerBase");
        source.Should().Contain("private readonly GetBusinessInvitationPreviewHandler _getBusinessInvitationPreviewHandler;");
        source.Should().Contain("private readonly AcceptBusinessInvitationHandler _acceptBusinessInvitationHandler;");
        source.Should().Contain("private readonly IStringLocalizer<ValidationResource> _validationLocalizer;");
        source.Should().Contain("public BusinessAuthController(");
        source.Should().Contain("_getBusinessInvitationPreviewHandler = getBusinessInvitationPreviewHandler ?? throw new ArgumentNullException(nameof(getBusinessInvitationPreviewHandler));");
        source.Should().Contain("_acceptBusinessInvitationHandler = acceptBusinessInvitationHandler ?? throw new ArgumentNullException(nameof(acceptBusinessInvitationHandler));");
        source.Should().Contain("_validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));");
        source.Should().Contain("public async Task<IActionResult> PreviewInvitationAsync([FromQuery] string? token, CancellationToken ct = default)");
        source.Should().Contain("if (string.IsNullOrWhiteSpace(token))");
        source.Should().Contain("return BadRequestProblem(_validationLocalizer[\"InvitationTokenRequired\"]);");
        source.Should().Contain("var result = await _getBusinessInvitationPreviewHandler.HandleAsync(token, ct).ConfigureAwait(false);");
        source.Should().Contain("if (!result.Succeeded || result.Value is null)");
        source.Should().Contain("return ProblemFromResult(result);");
        source.Should().Contain("var dto = result.Value;");
        source.Should().Contain("return Ok(new BusinessInvitationPreviewResponse");
        source.Should().Contain("InvitationId = dto.InvitationId,");
        source.Should().Contain("BusinessId = dto.BusinessId,");
        source.Should().Contain("BusinessName = dto.BusinessName,");
        source.Should().Contain("Email = dto.Email,");
        source.Should().Contain("Role = dto.Role,");
        source.Should().Contain("Status = dto.Status,");
        source.Should().Contain("ExpiresAtUtc = dto.ExpiresAtUtc,");
        source.Should().Contain("HasExistingUser = dto.HasExistingUser");
        source.Should().Contain("public async Task<IActionResult> AcceptInvitationAsync([FromBody] AcceptBusinessInvitationRequest? request, CancellationToken ct = default)");
        source.Should().Contain("if (request is null)");
        source.Should().Contain("return BadRequestProblem(_validationLocalizer[\"RequestPayloadRequired\"]);");
        source.Should().Contain("var result = await _acceptBusinessInvitationHandler.HandleAsync(new BusinessInvitationAcceptDto");
        source.Should().Contain("Token = request.Token ?? string.Empty,");
        source.Should().Contain("DeviceId = request.DeviceId,");
        source.Should().Contain("FirstName = request.FirstName,");
        source.Should().Contain("LastName = request.LastName,");
        source.Should().Contain("Password = request.Password");
        source.Should().Contain("return Ok(new TokenResponse");
        source.Should().Contain("AccessToken = result.Value.AccessToken,");
        source.Should().Contain("AccessTokenExpiresAtUtc = result.Value.AccessTokenExpiresAtUtc,");
        source.Should().Contain("RefreshToken = result.Value.RefreshToken,");
        source.Should().Contain("RefreshTokenExpiresAtUtc = result.Value.RefreshTokenExpiresAtUtc,");
        source.Should().Contain("UserId = result.Value.UserId,");
        source.Should().Contain("Email = result.Value.Email");
    }


    [Fact]
    public void PublicCheckoutController_Should_KeepIntentOrderPaymentAndConfirmationOrchestrationContractsWired()
    {
        var source = ReadWebApiFile(Path.Combine("Controllers", "Public", "PublicCheckoutController.cs"));

        source.Should().Contain("public sealed class PublicCheckoutController : ApiControllerBase");
        source.Should().Contain("private readonly CreateStorefrontCheckoutIntentHandler _createStorefrontCheckoutIntentHandler;");
        source.Should().Contain("private readonly PlaceOrderFromCartHandler _placeOrderFromCartHandler;");
        source.Should().Contain("private readonly CreateStorefrontPaymentIntentHandler _createStorefrontPaymentIntentHandler;");
        source.Should().Contain("private readonly CompleteStorefrontPaymentHandler _completeStorefrontPaymentHandler;");
        source.Should().Contain("private readonly GetStorefrontOrderConfirmationHandler _getStorefrontOrderConfirmationHandler;");
        source.Should().Contain("private readonly StorefrontCheckoutUrlBuilder _checkoutUrlBuilder;");
        source.Should().Contain("_createStorefrontCheckoutIntentHandler = createStorefrontCheckoutIntentHandler ?? throw new ArgumentNullException(nameof(createStorefrontCheckoutIntentHandler));");
        source.Should().Contain("_placeOrderFromCartHandler = placeOrderFromCartHandler ?? throw new ArgumentNullException(nameof(placeOrderFromCartHandler));");
        source.Should().Contain("_createStorefrontPaymentIntentHandler = createStorefrontPaymentIntentHandler ?? throw new ArgumentNullException(nameof(createStorefrontPaymentIntentHandler));");
        source.Should().Contain("_completeStorefrontPaymentHandler = completeStorefrontPaymentHandler ?? throw new ArgumentNullException(nameof(completeStorefrontPaymentHandler));");
        source.Should().Contain("_getStorefrontOrderConfirmationHandler = getStorefrontOrderConfirmationHandler ?? throw new ArgumentNullException(nameof(getStorefrontOrderConfirmationHandler));");
        source.Should().Contain("_checkoutUrlBuilder = checkoutUrlBuilder ?? throw new ArgumentNullException(nameof(checkoutUrlBuilder));");
        source.Should().Contain("_validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));");
        source.Should().Contain("public async Task<IActionResult> CreateIntentAsync([FromBody] CreateCheckoutIntentRequest? request, CancellationToken ct = default)");
        source.Should().Contain("if (request is null)");
        source.Should().Contain("return BadRequestProblem(_validationLocalizer[\"RequestPayloadRequired\"]);");
        source.Should().Contain("var result = await _createStorefrontCheckoutIntentHandler.HandleAsync(new CreateStorefrontCheckoutIntentDto");
        source.Should().Contain("CartId = request.CartId,");
        source.Should().Contain("UserId = GetCurrentUserId(),");
        source.Should().Contain("ShippingAddressId = request.ShippingAddressId,");
        source.Should().Contain("ShippingAddress = request.ShippingAddress is null");
        source.Should().Contain("SelectedShippingMethodId = request.SelectedShippingMethodId");
        source.Should().Contain("return Ok(new CreateCheckoutIntentResponse");
        source.Should().Contain("ShippingOptions = result.ShippingOptions.Select(MapShippingOption).ToList()");
        source.Should().Contain("catch (Exception ex) when (ex is InvalidOperationException || ex is FluentValidation.ValidationException)");
        source.Should().Contain("return BadRequestProblem(_validationLocalizer[\"CheckoutIntentCreationFailed\"], ex.Message);");
        source.Should().Contain("public async Task<IActionResult> PlaceOrderAsync([FromBody] PlaceOrderFromCartRequest? request, CancellationToken ct = default)");
        source.Should().Contain("var result = await _placeOrderFromCartHandler.HandleAsync(new PlaceOrderFromCartDto");
        source.Should().Contain("BillingAddressId = request.BillingAddressId,");
        source.Should().Contain("ShippingAddressId = request.ShippingAddressId,");
        source.Should().Contain("SelectedShippingMethodId = request.SelectedShippingMethodId,");
        source.Should().Contain("BillingAddress = request.BillingAddress is null");
        source.Should().Contain("ShippingAddress = request.ShippingAddress is null");
        source.Should().Contain("ShippingTotalMinor = request.ShippingTotalMinor,");
        source.Should().Contain("Culture = string.IsNullOrWhiteSpace(request.Culture) ? SiteSettingDto.DefaultCultureDefault : request.Culture.Trim()");
        source.Should().Contain("return Ok(new PlaceOrderFromCartResponse");
        source.Should().Contain("Status = result.Status.ToString()");
        source.Should().Contain("return BadRequestProblem(_validationLocalizer[\"OrderPlacementFailed\"], ex.Message);");
        source.Should().Contain("public async Task<IActionResult> CreatePaymentIntentAsync(Guid orderId, [FromBody] CreateStorefrontPaymentIntentRequest? request, CancellationToken ct = default)");
        source.Should().Contain("if (orderId == Guid.Empty)");
        source.Should().Contain("return BadRequestProblem(_validationLocalizer[\"OrderIdRequired\"]);");
        source.Should().Contain("var result = await _createStorefrontPaymentIntentHandler.HandleAsync(new CreateStorefrontPaymentIntentDto");
        source.Should().Contain("OrderId = orderId,");
        source.Should().Contain("Provider = string.IsNullOrWhiteSpace(request?.Provider) ? \"Stripe\" : request.Provider.Trim()");
        source.Should().Contain("var returnUrl = _checkoutUrlBuilder.BuildFrontOfficeConfirmationUrl(orderId, request?.OrderNumber, cancelled: false);");
        source.Should().Contain("var cancelUrl = _checkoutUrlBuilder.BuildFrontOfficeConfirmationUrl(orderId, request?.OrderNumber, cancelled: true);");
        source.Should().Contain("var checkoutUrl = _checkoutUrlBuilder.BuildStripeCheckoutUrl(result, returnUrl, cancelUrl);");
        source.Should().Contain("return Ok(new CreateStorefrontPaymentIntentResponse");
        source.Should().Contain("ProviderPaymentIntentReference = result.ProviderPaymentIntentReference,");
        source.Should().Contain("ProviderCheckoutSessionReference = result.ProviderCheckoutSessionReference,");
        source.Should().Contain("CheckoutUrl = checkoutUrl,");
        source.Should().Contain("ReturnUrl = returnUrl,");
        source.Should().Contain("CancelUrl = cancelUrl,");
        source.Should().Contain("catch (InvalidOperationException ex) when (string.Equals(ex.Message, \"Order not found.\", StringComparison.Ordinal))");
        source.Should().Contain("return NotFoundProblem(_validationLocalizer[\"OrderNotFound\"]);");
        source.Should().Contain("return BadRequestProblem(_validationLocalizer[\"PaymentIntentCreationFailed\"], ex.Message);");
        source.Should().Contain("public async Task<IActionResult> CompletePaymentAsync(Guid orderId, Guid paymentId, [FromBody] CompleteStorefrontPaymentRequest? request, CancellationToken ct = default)");
        source.Should().Contain("if (orderId == Guid.Empty || paymentId == Guid.Empty)");
        source.Should().Contain("return BadRequestProblem(_validationLocalizer[\"OrderIdAndPaymentIdAreRequired\"]);");
        source.Should().Contain("if (!Enum.TryParse<StorefrontPaymentOutcome>(request.Outcome, ignoreCase: true, out var outcome))");
        source.Should().Contain("return BadRequestProblem(_validationLocalizer[\"UnsupportedStorefrontPaymentOutcome\"]);");
        source.Should().Contain("var result = await _completeStorefrontPaymentHandler.HandleAsync(new CompleteStorefrontPaymentDto");
        source.Should().Contain("ProviderPaymentIntentReference = request.ProviderPaymentIntentReference,");
        source.Should().Contain("ProviderCheckoutSessionReference = request.ProviderCheckoutSessionReference,");
        source.Should().Contain("Outcome = outcome,");
        source.Should().Contain("FailureReason = request.FailureReason");
        source.Should().Contain("return Ok(new CompleteStorefrontPaymentResponse");
        source.Should().Contain("OrderStatus = result.OrderStatus.ToString(),");
        source.Should().Contain("PaymentStatus = result.PaymentStatus.ToString(),");
        source.Should().Contain("catch (InvalidOperationException ex) when (string.Equals(ex.Message, \"Order not found.\", StringComparison.Ordinal) ||");
        source.Should().Contain("string.Equals(ex.Message, \"Payment not found for the order.\", StringComparison.Ordinal))");
        source.Should().Contain("_validationLocalizer[\"PaymentNotFoundForOrder\"]");
        source.Should().Contain("return BadRequestProblem(_validationLocalizer[\"PaymentCompletionApplyFailed\"], ex.Message);");
        source.Should().Contain("public async Task<IActionResult> GetConfirmationAsync(Guid orderId, [FromQuery] string? orderNumber, CancellationToken ct = default)");
        source.Should().Contain("if (orderId == Guid.Empty)");
        source.Should().Contain("var confirmation = await _getStorefrontOrderConfirmationHandler.HandleAsync(new GetStorefrontOrderConfirmationDto");
        source.Should().Contain("OrderNumber = orderNumber");
        source.Should().Contain("if (confirmation is null)");
        source.Should().Contain("return NotFoundProblem(_validationLocalizer[\"OrderConfirmationNotFound\"]);");
        source.Should().Contain("return Ok(new StorefrontOrderConfirmationResponse");
        source.Should().Contain("Lines = confirmation.Lines.Select(line => new StorefrontOrderConfirmationLine");
        source.Should().Contain("Payments = confirmation.Payments.Select(payment => new StorefrontOrderConfirmationPayment");
        source.Should().Contain("private static PublicShippingOption MapShippingOption(StorefrontShippingOptionDto dto)");
        source.Should().Contain("MethodId = dto.MethodId,");
        source.Should().Contain("Service = dto.Service");
    }


    [Fact]
    public void PublicCartController_Should_KeepSummaryMutationReloadAndAnonymousFallbackContractsWired()
    {
        var source = ReadWebApiFile(Path.Combine("Controllers", "Public", "PublicCartController.cs"));

        source.Should().Contain("public sealed class PublicCartController : ApiControllerBase");
        source.Should().Contain("private readonly ComputeCartSummaryHandler _computeCartSummaryHandler;");
        source.Should().Contain("private readonly GetCartSummaryHandler _getCartSummaryHandler;");
        source.Should().Contain("private readonly AddOrIncreaseCartItemHandler _addOrIncreaseCartItemHandler;");
        source.Should().Contain("private readonly UpdateCartItemQuantityHandler _updateCartItemQuantityHandler;");
        source.Should().Contain("private readonly RemoveCartItemHandler _removeCartItemHandler;");
        source.Should().Contain("private readonly ApplyCouponHandler _applyCouponHandler;");
        source.Should().Contain("private readonly IStringLocalizer<ValidationResource> _validationLocalizer;");
        source.Should().Contain("_computeCartSummaryHandler = computeCartSummaryHandler ?? throw new ArgumentNullException(nameof(computeCartSummaryHandler));");
        source.Should().Contain("_getCartSummaryHandler = getCartSummaryHandler ?? throw new ArgumentNullException(nameof(getCartSummaryHandler));");
        source.Should().Contain("_addOrIncreaseCartItemHandler = addOrIncreaseCartItemHandler ?? throw new ArgumentNullException(nameof(addOrIncreaseCartItemHandler));");
        source.Should().Contain("_updateCartItemQuantityHandler = updateCartItemQuantityHandler ?? throw new ArgumentNullException(nameof(updateCartItemQuantityHandler));");
        source.Should().Contain("_removeCartItemHandler = removeCartItemHandler ?? throw new ArgumentNullException(nameof(removeCartItemHandler));");
        source.Should().Contain("_applyCouponHandler = applyCouponHandler ?? throw new ArgumentNullException(nameof(applyCouponHandler));");
        source.Should().Contain("_validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));");
        source.Should().Contain("public async Task<IActionResult> GetAsync([FromQuery] string? anonymousId, CancellationToken ct = default)");
        source.Should().Contain("var userId = GetCurrentUserId();");
        source.Should().Contain("var normalizedAnonymousId = NormalizeAnonymousId(anonymousId);");
        source.Should().Contain("if (userId is null && normalizedAnonymousId is null)");
        source.Should().Contain("return BadRequestProblem(_validationLocalizer[\"EitherUserIdOrAnonymousIdRequired\"]);");
        source.Should().Contain("var dto = await _getCartSummaryHandler.HandleAsync(userId, normalizedAnonymousId, ct).ConfigureAwait(false);");
        source.Should().Contain("return dto is null ? NotFoundProblem(_validationLocalizer[\"CartNotFound\"]) : Ok(MapSummary(dto));");
        source.Should().Contain("public async Task<IActionResult> AddItemAsync([FromBody] PublicCartAddItemRequest? request, CancellationToken ct = default)");
        source.Should().Contain("var normalizedAnonymousId = NormalizeAnonymousId(request.AnonymousId);");
        source.Should().Contain("if (request.VariantId == Guid.Empty)");
        source.Should().Contain("return BadRequestProblem(_validationLocalizer[\"VariantIdMustNotBeEmpty\"]);");
        source.Should().Contain("if (request.Quantity <= 0)");
        source.Should().Contain("return BadRequestProblem(_validationLocalizer[\"QuantityMustBePositiveInteger\"]);");
        source.Should().Contain("await _addOrIncreaseCartItemHandler.HandleAsync(new CartAddItemDto");
        source.Should().Contain("AnonymousId = normalizedAnonymousId,");
        source.Should().Contain("SelectedAddOnValueIds = request.SelectedAddOnValueIds.ToList()");
        source.Should().Contain("return BadRequestProblem(_validationLocalizer[\"CartItemAddFailed\"], ex.Message);");
        source.Should().Contain("var summary = await _getCartSummaryHandler.HandleAsync(userId, normalizedAnonymousId, ct).ConfigureAwait(false);");
        source.Should().Contain("return summary is null ? NotFoundProblem(_validationLocalizer[\"CartNotFoundAfterMutation\"]) : Ok(MapSummary(summary));");
        source.Should().Contain("public async Task<IActionResult> UpdateItemAsync([FromBody] PublicCartUpdateItemRequest? request, CancellationToken ct = default)");
        source.Should().Contain("if (request.CartId == Guid.Empty || request.VariantId == Guid.Empty)");
        source.Should().Contain("return BadRequestProblem(_validationLocalizer[\"CartIdAndVariantIdMustNotBeEmpty\"]);");
        source.Should().Contain("await _updateCartItemQuantityHandler.HandleAsync(new CartUpdateQtyDto");
        source.Should().Contain("SelectedAddOnValueIdsJson = request.SelectedAddOnValueIdsJson");
        source.Should().Contain("return BadRequestProblem(_validationLocalizer[\"CartItemUpdateFailed\"], ex.Message);");
        source.Should().Contain("return await ReloadCartAsync(request.CartId, ct).ConfigureAwait(false);");
        source.Should().Contain("public async Task<IActionResult> RemoveItemAsync([FromBody] PublicCartRemoveItemRequest? request, CancellationToken ct = default)");
        source.Should().Contain("await _removeCartItemHandler.HandleAsync(new CartRemoveItemDto");
        source.Should().Contain("return await ReloadCartAsync(request.CartId, ct).ConfigureAwait(false);");
        source.Should().Contain("public async Task<IActionResult> ApplyCouponAsync([FromBody] PublicCartApplyCouponRequest? request, CancellationToken ct = default)");
        source.Should().Contain("if (request.CartId == Guid.Empty)");
        source.Should().Contain("return BadRequestProblem(_validationLocalizer[\"CartIdRequired\"]);");
        source.Should().Contain("await _applyCouponHandler.HandleAsync(new CartApplyCouponDto");
        source.Should().Contain("CouponCode = request.CouponCode");
        source.Should().Contain("return BadRequestProblem(_validationLocalizer[\"CouponApplyFailed\"], ex.Message);");
        source.Should().Contain("private async Task<IActionResult> ReloadCartAsync(Guid cartId, CancellationToken ct)");
        source.Should().Contain("var summary = await _computeCartSummaryHandler.HandleAsync(cartId, ct).ConfigureAwait(false);");
        source.Should().Contain("return Ok(MapSummary(summary));");
        source.Should().Contain("catch (InvalidOperationException)");
        source.Should().Contain("return NotFoundProblem(_validationLocalizer[\"CartNotFound\"]);");
        source.Should().Contain("private static string? NormalizeAnonymousId(string? anonymousId)");
        source.Should().Contain("=> string.IsNullOrWhiteSpace(anonymousId) ? null : anonymousId.Trim();");
        source.Should().Contain("private static PublicCartSummary MapSummary(CartSummaryDto dto)");
        source.Should().Contain("CartId = dto.CartId,");
        source.Should().Contain("CouponCode = dto.CouponCode,");
        source.Should().Contain("Items = dto.Items.Select(item => new PublicCartItemRow");
        source.Should().Contain("SelectedAddOnValueIdsJson = item.SelectedAddOnValueIdsJson");
    }


    [Fact]
    public void PublicCatalogAndCmsControllers_Should_KeepPagingCultureMappingAndNotFoundFallbackContractsWired()
    {
        var catalogSource = ReadWebApiFile(Path.Combine("Controllers", "Public", "PublicCatalogController.cs"));
        var cmsSource = ReadWebApiFile(Path.Combine("Controllers", "Public", "PublicCmsController.cs"));
        var validationSource = ReadApplicationFile(Path.Combine("Resources", "ValidationResource.resx"));
        var germanValidationSource = ReadApplicationFile(Path.Combine("Resources", "ValidationResource.de-DE.resx"));

        catalogSource.Should().Contain("public sealed class PublicCatalogController : ApiControllerBase");
        catalogSource.Should().Contain("private readonly GetPublishedCategoriesHandler _getPublishedCategoriesHandler;");
        catalogSource.Should().Contain("private readonly GetPublishedProductsPageHandler _getPublishedProductsPageHandler;");
        catalogSource.Should().Contain("private readonly GetPublishedProductBySlugHandler _getPublishedProductBySlugHandler;");
        catalogSource.Should().Contain("private readonly IStringLocalizer<ValidationResource> _validationLocalizer;");
        catalogSource.Should().Contain("_getPublishedCategoriesHandler = getPublishedCategoriesHandler ?? throw new ArgumentNullException(nameof(getPublishedCategoriesHandler));");
        catalogSource.Should().Contain("_getPublishedProductsPageHandler = getPublishedProductsPageHandler ?? throw new ArgumentNullException(nameof(getPublishedProductsPageHandler));");
        catalogSource.Should().Contain("_getPublishedProductBySlugHandler = getPublishedProductBySlugHandler ?? throw new ArgumentNullException(nameof(getPublishedProductBySlugHandler));");
        catalogSource.Should().Contain("_validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));");
        catalogSource.Should().Contain("public async Task<IActionResult> GetCategoriesAsync([FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] string? culture, CancellationToken ct = default)");
        catalogSource.Should().Contain("var normalizedPage = page.GetValueOrDefault(1);");
        catalogSource.Should().Contain("return BadRequestProblem(_validationLocalizer[\"PageMustBePositiveInteger\"]);");
        catalogSource.Should().Contain("var normalizedPageSize = pageSize.GetValueOrDefault(50);");
        catalogSource.Should().Contain("return BadRequestProblem(_validationLocalizer[\"PageSizeMustBeBetween1And200\"]);");
        catalogSource.Should().Contain("var normalizedCulture = string.IsNullOrWhiteSpace(culture) ? SiteSettingDto.DefaultCultureDefault : culture.Trim();");
        catalogSource.Should().Contain("var (items, total) = await _getPublishedCategoriesHandler");
        catalogSource.Should().Contain("return Ok(new PagedResponse<PublicCategorySummary>");
        catalogSource.Should().Contain("Items = items.Select(MapCategory).ToList(),");
        catalogSource.Should().Contain("Page = normalizedPage,");
        catalogSource.Should().Contain("PageSize = normalizedPageSize,");
        catalogSource.Should().Contain("public async Task<IActionResult> GetProductsAsync(");
        catalogSource.Should().Contain("var normalizedPageSize = pageSize.GetValueOrDefault(24);");
        catalogSource.Should().Contain("var (items, total) = await _getPublishedProductsPageHandler");
        catalogSource.Should().Contain(".HandleAsync(normalizedPage, normalizedPageSize, normalizedCulture, categorySlug, ct)");
        catalogSource.Should().Contain("Items = items.Select(MapProductSummary).ToList(),");
        catalogSource.Should().Contain("Search = categorySlug");
        catalogSource.Should().Contain("public async Task<IActionResult> GetProductBySlugAsync([FromRoute] string slug, [FromQuery] string? culture, CancellationToken ct = default)");
        catalogSource.Should().Contain(".HandleAsync(slug, string.IsNullOrWhiteSpace(culture) ? SiteSettingDto.DefaultCultureDefault : culture.Trim(), ct)");
        catalogSource.Should().Contain("return dto is null");
        catalogSource.Should().Contain("? NotFoundProblem(_validationLocalizer[\"ProductNotFound\"])");
        catalogSource.Should().Contain(": Ok(MapProductDetail(dto));");
        catalogSource.Should().Contain("private static PublicCategorySummary MapCategory(PublicCategorySummaryDto dto)");
        catalogSource.Should().Contain("Slug = dto.Slug,");
        catalogSource.Should().Contain("private static PublicProductSummary MapProductSummary(PublicProductSummaryDto dto)");
        catalogSource.Should().Contain("PrimaryImageUrl = dto.PrimaryImageUrl");
        catalogSource.Should().Contain("private static PublicProductDetail MapProductDetail(PublicProductDetailDto dto)");
        catalogSource.Should().Contain("Variants = dto.Variants.Select(variant => new PublicProductVariant");
        catalogSource.Should().Contain("Media = dto.Media.Select(media => new PublicProductMedia");

        cmsSource.Should().Contain("public sealed class PublicCmsController : ApiControllerBase");
        cmsSource.Should().Contain("private readonly GetPublishedPagesPageHandler _getPublishedPagesPageHandler;");
        cmsSource.Should().Contain("private readonly GetPublishedPageBySlugHandler _getPublishedPageBySlugHandler;");
        cmsSource.Should().Contain("private readonly GetPublicMenuByNameHandler _getPublicMenuByNameHandler;");
        cmsSource.Should().Contain("private readonly IStringLocalizer<ValidationResource> _validationLocalizer;");
        cmsSource.Should().Contain("_getPublishedPagesPageHandler = getPublishedPagesPageHandler ?? throw new ArgumentNullException(nameof(getPublishedPagesPageHandler));");
        cmsSource.Should().Contain("_getPublishedPageBySlugHandler = getPublishedPageBySlugHandler ?? throw new ArgumentNullException(nameof(getPublishedPageBySlugHandler));");
        cmsSource.Should().Contain("_getPublicMenuByNameHandler = getPublicMenuByNameHandler ?? throw new ArgumentNullException(nameof(getPublicMenuByNameHandler));");
        cmsSource.Should().Contain("_validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));");
        cmsSource.Should().Contain("public async Task<IActionResult> GetPagesAsync([FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] string? culture, CancellationToken ct = default)");
        cmsSource.Should().Contain("return BadRequestProblem(_validationLocalizer[\"PageMustBePositiveInteger\"]);");
        cmsSource.Should().Contain("return BadRequestProblem(_validationLocalizer[\"PageSizeMustBeBetween1And200\"]);");
        cmsSource.Should().Contain("var normalizedPageSize = pageSize.GetValueOrDefault(20);");
        cmsSource.Should().Contain("var (items, total) = await _getPublishedPagesPageHandler");
        cmsSource.Should().Contain("Items = items.Select(MapPageSummary).ToList(),");
        cmsSource.Should().Contain("public async Task<IActionResult> GetPageBySlugAsync([FromRoute] string slug, [FromQuery] string? culture, CancellationToken ct = default)");
        cmsSource.Should().Contain(".HandleAsync(slug, string.IsNullOrWhiteSpace(culture) ? SiteSettingDto.DefaultCultureDefault : culture.Trim(), ct)");
        cmsSource.Should().Contain("? NotFoundProblem(_validationLocalizer[\"PageNotFound\"])");
        cmsSource.Should().Contain(": Ok(MapPageDetail(dto));");
        cmsSource.Should().Contain("public async Task<IActionResult> GetMenuByNameAsync([FromRoute] string name, [FromQuery] string? culture, CancellationToken ct = default)");
        cmsSource.Should().Contain(".HandleAsync(name, string.IsNullOrWhiteSpace(culture) ? SiteSettingDto.DefaultCultureDefault : culture.Trim(), ct)");
        cmsSource.Should().Contain("? NotFoundProblem(_validationLocalizer[\"MenuNotFound\"])");
        cmsSource.Should().Contain(": Ok(MapMenu(dto));");
        cmsSource.Should().Contain("private static PublicPageSummary MapPageSummary(PublicPageSummaryDto dto)");
        cmsSource.Should().Contain("private static PublicPageDetail MapPageDetail(PublicPageDetailDto dto)");
        cmsSource.Should().Contain("ContentHtml = dto.ContentHtml");
        cmsSource.Should().Contain("private static PublicMenu MapMenu(PublicMenuDto dto)");
        cmsSource.Should().Contain("Items = dto.Items.Select(item => new PublicMenuItem");

        validationSource.Should().Contain("<data name=\"ShippingOptionsCouldNotBeCalculated\"");
        germanValidationSource.Should().Contain("<data name=\"ShippingOptionsCouldNotBeCalculated\"");
    }


    [Fact]
    public void ProfileAndNotificationControllers_Should_KeepSelfServiceAndDeviceRegistrationOrchestrationContractsWired()
    {
        var profileSource = ReadWebApiFile(Path.Combine("Controllers", "Profile", "ProfileController.cs"));
        var addressesSource = ReadWebApiFile(Path.Combine("Controllers", "Profile", "ProfileAddressesController.cs"));
        var notificationsSource = ReadWebApiFile(Path.Combine("Controllers", "Notifications", "NotificationsController.cs"));

        profileSource.Should().Contain("public sealed class ProfileController : ApiControllerBase");
        profileSource.Should().Contain("private readonly GetCurrentUserProfileHandler _getCurrentUserProfileHandler;");
        profileSource.Should().Contain("private readonly GetCurrentUserPreferencesHandler _getCurrentUserPreferencesHandler;");
        profileSource.Should().Contain("private readonly UpdateCurrentUserHandler _updateCurrentUserHandler;");
        profileSource.Should().Contain("private readonly UpdateCurrentUserPreferencesHandler _updateCurrentUserPreferencesHandler;");
        profileSource.Should().Contain("private readonly RequestCurrentUserAccountDeletionHandler _requestCurrentUserAccountDeletionHandler;");
        profileSource.Should().Contain("private readonly RequestPhoneVerificationHandler _requestPhoneVerificationHandler;");
        profileSource.Should().Contain("private readonly ConfirmPhoneVerificationHandler _confirmPhoneVerificationHandler;");
        profileSource.Should().Contain("private readonly IStringLocalizer<ValidationResource> _validationLocalizer;");
        profileSource.Should().Contain("_getCurrentUserProfileHandler =");
        profileSource.Should().Contain("_requestCurrentUserAccountDeletionHandler =");
        profileSource.Should().Contain("_validationLocalizer =");
        profileSource.Should().Contain("public async Task<IActionResult> GetMe(CancellationToken ct)");
        profileSource.Should().Contain("var result = await _getCurrentUserProfileHandler.HandleAsync(ct);");
        profileSource.Should().Contain("if (!result.Succeeded)");
        profileSource.Should().Contain("if (result.Value is null)");
        profileSource.Should().Contain("return NotFoundProblem(_validationLocalizer[\"ProfileNotFound\"]);");
        profileSource.Should().Contain("var contract = new CustomerProfile");
        profileSource.Should().Contain("Id = value.Id,");
        profileSource.Should().Contain("RowVersion = value.RowVersion ?? Array.Empty<byte>()");
        profileSource.Should().Contain("public async Task<IActionResult> GetPreferencesAsync(CancellationToken ct)");
        profileSource.Should().Contain("var result = await _getCurrentUserPreferencesHandler.HandleAsync(ct).ConfigureAwait(false);");
        profileSource.Should().Contain("return NotFoundProblem(_validationLocalizer[\"PreferencesNotFound\"]);");
        profileSource.Should().Contain("return Ok(new MemberPreferences");
        profileSource.Should().Contain("public async Task<IActionResult> UpdateMe([FromBody] CustomerProfile? request, CancellationToken ct)");
        profileSource.Should().Contain("if (request.Id == Guid.Empty)");
        profileSource.Should().Contain("return BadRequestProblem(_validationLocalizer[\"IdentifierMustNotBeEmpty\"]);");
        profileSource.Should().Contain("if (request.RowVersion is null || request.RowVersion.Length == 0)");
        profileSource.Should().Contain("return BadRequestProblem(_validationLocalizer[\"RowVersionRequiredForOptimisticConcurrency\"]);");
        profileSource.Should().Contain("var normalizedLocale = NormalizeText(request.Locale);");
        profileSource.Should().Contain("if (normalizedLocale is null)");
        profileSource.Should().Contain("return BadRequestProblem(_validationLocalizer[\"LocaleRequired\"]);");
        profileSource.Should().Contain("var normalizedTimezone = NormalizeText(request.Timezone);");
        profileSource.Should().Contain("if (normalizedTimezone is null)");
        profileSource.Should().Contain("return BadRequestProblem(_validationLocalizer[\"TimezoneRequired\"]);");
        profileSource.Should().Contain("var normalizedCurrency = NormalizeText(request.Currency)?.ToUpperInvariant();");
        profileSource.Should().Contain("if (normalizedCurrency is null)");
        profileSource.Should().Contain("return BadRequestProblem(_validationLocalizer[\"CurrencyRequired\"]);");
        profileSource.Should().Contain("var dto = new UserProfileEditDto");
        profileSource.Should().Contain("Id = request.Id,");
        profileSource.Should().Contain("Email = NormalizeText(request.Email) ?? string.Empty,");
        profileSource.Should().Contain("RowVersion = request.RowVersion ?? Array.Empty<byte>()");
        profileSource.Should().Contain("var result = await _updateCurrentUserHandler.HandleAsync(dto, ct);");
        profileSource.Should().Contain("return NoContent();");
        profileSource.Should().Contain("public async Task<IActionResult> RequestPhoneVerificationAsync([FromBody] RequestPhoneVerificationRequest? request, CancellationToken ct)");
        profileSource.Should().Contain("PhoneVerificationChannel? channel = string.IsNullOrWhiteSpace(channelValue)");
        profileSource.Should().Contain("string.Equals(channelValue, \"WhatsApp\", StringComparison.OrdinalIgnoreCase)");
        profileSource.Should().Contain(": PhoneVerificationChannel.Sms;");
        profileSource.Should().Contain("new RequestPhoneVerificationDto { Channel = channel }");
        profileSource.Should().Contain("public async Task<IActionResult> ConfirmPhoneVerificationAsync([FromBody] ConfirmPhoneVerificationRequest? request, CancellationToken ct)");
        profileSource.Should().Contain("return BadRequestProblem(_validationLocalizer[\"VerificationCodeRequired\"]);");
        profileSource.Should().Contain("new ConfirmPhoneVerificationDto { Code = request.Code.Trim() }");
        profileSource.Should().Contain("public async Task<IActionResult> UpdatePreferencesAsync([FromBody] UpdateMemberPreferencesRequest? request, CancellationToken ct)");
        profileSource.Should().Contain("return BadRequestProblem(_validationLocalizer[\"RequestPayloadRequired\"]);");
        profileSource.Should().Contain("new UpdateMemberPreferencesDto");
        profileSource.Should().Contain("AllowOptionalAnalyticsTracking = request.AllowOptionalAnalyticsTracking");
        profileSource.Should().Contain("public async Task<IActionResult> RequestAccountDeletionAsync([FromBody] RequestAccountDeletionRequest? request, CancellationToken ct)");
        profileSource.Should().Contain("if (!request.ConfirmIrreversibleDeletion)");
        profileSource.Should().Contain("return BadRequestProblem(_validationLocalizer[\"ExplicitDeletionConfirmationRequired\"]);");
        profileSource.Should().Contain("HandleAsync(request.ConfirmIrreversibleDeletion, ct)");

        addressesSource.Should().Contain("public sealed class ProfileAddressesController : ApiControllerBase");
        addressesSource.Should().Contain("private readonly GetCurrentUserAddressesHandler _getCurrentUserAddressesHandler;");
        addressesSource.Should().Contain("private readonly CreateCurrentUserAddressHandler _createCurrentUserAddressHandler;");
        addressesSource.Should().Contain("private readonly UpdateCurrentUserAddressHandler _updateCurrentUserAddressHandler;");
        addressesSource.Should().Contain("private readonly DeleteCurrentUserAddressHandler _deleteCurrentUserAddressHandler;");
        addressesSource.Should().Contain("private readonly SetCurrentUserDefaultAddressHandler _setCurrentUserDefaultAddressHandler;");
        addressesSource.Should().Contain("private readonly GetCurrentMemberCustomerProfileHandler _getCurrentMemberCustomerProfileHandler;");
        addressesSource.Should().Contain("private readonly GetCurrentMemberCustomerContextHandler _getCurrentMemberCustomerContextHandler;");
        addressesSource.Should().Contain("private readonly IStringLocalizer<ValidationResource> _validationLocalizer;");
        addressesSource.Should().Contain("public async Task<IActionResult> GetAddressesAsync(CancellationToken ct = default)");
        addressesSource.Should().Contain("var result = await _getCurrentUserAddressesHandler.HandleAsync(ct).ConfigureAwait(false);");
        addressesSource.Should().Contain("return Ok(result.Value.Select(MapAddress).ToList());");
        addressesSource.Should().Contain("public async Task<IActionResult> CreateAddressAsync([FromBody] CreateMemberAddressRequest? request, CancellationToken ct = default)");
        addressesSource.Should().Contain("return BadRequestProblem(_validationLocalizer[\"RequestPayloadRequired\"]);");
        addressesSource.Should().Contain("var createResult = await _createCurrentUserAddressHandler.HandleAsync(new AddressCreateDto");
        addressesSource.Should().Contain("return await GetAddressByIdAsync(createResult.Value, ct).ConfigureAwait(false);");
        addressesSource.Should().Contain("public async Task<IActionResult> UpdateAddressAsync(Guid id, [FromBody] UpdateMemberAddressRequest? request, CancellationToken ct = default)");
        addressesSource.Should().Contain("return BadRequestProblem(_validationLocalizer[\"AddressIdRequired\"]);");
        addressesSource.Should().Contain("var result = await _updateCurrentUserAddressHandler.HandleAsync(new AddressEditDto");
        addressesSource.Should().Contain("RowVersion = request.RowVersion,");
        addressesSource.Should().Contain("return await GetAddressByIdAsync(id, ct).ConfigureAwait(false);");
        addressesSource.Should().Contain("public async Task<IActionResult> DeleteAddressAsync(Guid id, [FromBody] DeleteMemberAddressRequest? request, CancellationToken ct = default)");
        addressesSource.Should().Contain("var result = await _deleteCurrentUserAddressHandler.HandleAsync(new AddressDeleteDto");
        addressesSource.Should().Contain("return result.Succeeded ? NoContent() : ProblemFromResult(result);");
        addressesSource.Should().Contain("public async Task<IActionResult> SetDefaultAddressAsync(Guid id, [FromBody] SetMemberDefaultAddressRequest? request, CancellationToken ct = default)");
        addressesSource.Should().Contain("HandleAsync(id, request.AsBilling, request.AsShipping, ct)");
        addressesSource.Should().Contain("public async Task<IActionResult> GetLinkedCustomerAsync(CancellationToken ct = default)");
        addressesSource.Should().Contain("return dto is null ? NotFoundProblem(_validationLocalizer[\"LinkedCustomerNotFound\"]) : Ok(MapCustomer(dto));");
        addressesSource.Should().Contain("public async Task<IActionResult> GetLinkedCustomerContextAsync(CancellationToken ct = default)");
        addressesSource.Should().Contain("return dto is null ? NotFoundProblem(_validationLocalizer[\"LinkedCustomerContextNotFound\"]) : Ok(MapCustomerContext(dto));");
        addressesSource.Should().Contain("private async Task<IActionResult> GetAddressByIdAsync(Guid id, CancellationToken ct)");
        addressesSource.Should().Contain("var address = result.Value.FirstOrDefault(x => x.Id == id);");
        addressesSource.Should().Contain("return address is null ? NotFoundProblem(_validationLocalizer[\"AddressNotFound\"]) : Ok(MapAddress(address));");
        addressesSource.Should().Contain("private static MemberAddress MapAddress(AddressListItemDto dto)");
        addressesSource.Should().Contain("private static LinkedCustomerProfile MapCustomer(");
        addressesSource.Should().Contain("private static MemberCustomerContext MapCustomerContext(");
        addressesSource.Should().Contain("Segments = dto.Segments.Select(x => new MemberCustomerSegment");
        addressesSource.Should().Contain("Consents = dto.Consents.Select(x => new MemberCustomerConsent");
        addressesSource.Should().Contain("RecentInteractions = dto.RecentInteractions.Select(x => new MemberCustomerInteraction");

        notificationsSource.Should().Contain("public sealed class NotificationsController : ApiControllerBase");
        notificationsSource.Should().Contain("private readonly RegisterOrUpdateUserDeviceHandler _registerOrUpdateUserDeviceHandler;");
        notificationsSource.Should().Contain("private readonly IStringLocalizer<ValidationResource> _validationLocalizer;");
        notificationsSource.Should().Contain("_registerOrUpdateUserDeviceHandler = registerOrUpdateUserDeviceHandler");
        notificationsSource.Should().Contain("_validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));");
        notificationsSource.Should().Contain("public async Task<IActionResult> RegisterDeviceAsync([FromBody] RegisterPushDeviceRequest? request, CancellationToken ct)");
        notificationsSource.Should().Contain("if (request is null)");
        notificationsSource.Should().Contain("return BadRequestProblem(_validationLocalizer[\"RequestPayloadRequired\"]);");
        notificationsSource.Should().Contain("var userId = GetUserIdFromClaims(User);");
        notificationsSource.Should().Contain("if (userId is null)");
        notificationsSource.Should().Contain("return StatusCode(StatusCodes.Status401Unauthorized, new Darwin.Contracts.Common.ProblemDetails");
        notificationsSource.Should().Contain("Title = _validationLocalizer[\"UnauthorizedTitle\"],");
        notificationsSource.Should().Contain("Detail = _validationLocalizer[\"AuthenticatedUserIdentifierNotResolved\"],");
        notificationsSource.Should().Contain("var dto = new RegisterUserDeviceDto");
        notificationsSource.Should().Contain("Platform = ToDomainPlatform(request.Platform),");
        notificationsSource.Should().Contain("var result = await _registerOrUpdateUserDeviceHandler.HandleAsync(dto, ct).ConfigureAwait(false);");
        notificationsSource.Should().Contain("if (!result.Succeeded || result.Value is null)");
        notificationsSource.Should().Contain("return ProblemFromResult(result);");
        notificationsSource.Should().Contain("return Ok(new RegisterPushDeviceResponse");
        notificationsSource.Should().Contain("private static MobilePlatform ToDomainPlatform(MobileDevicePlatform platform)");
        notificationsSource.Should().Contain("MobileDevicePlatform.Android => MobilePlatform.Android,");
        notificationsSource.Should().Contain("MobileDevicePlatform.iOS => MobilePlatform.iOS,");
        notificationsSource.Should().Contain("_ => MobilePlatform.Unknown");
        notificationsSource.Should().Contain("private static Guid? GetUserIdFromClaims(ClaimsPrincipal user)");
        notificationsSource.Should().Contain("user.FindFirstValue(ClaimTypes.NameIdentifier) ??");
        notificationsSource.Should().Contain("user.FindFirstValue(\"sub\") ??");
        notificationsSource.Should().Contain("user.FindFirstValue(\"uid\");");
    }


    [Fact]
    public void MetaAndBusinessDiscoveryPrimitives_Should_KeepBootstrapHealthCategoryAndConventionContractsWired()
    {
        var metaSource = ReadWebApiFile(Path.Combine("Controllers", "MetaController.cs"));
        var businessesMetaSource = ReadWebApiFile(Path.Combine("Controllers", "Businesses", "BusinessesMetaController.cs"));
        var conventionsSource = ReadWebApiFile(Path.Combine("Controllers", "Businesses", "BusinessControllerConventions.cs"));
        var validationSource = ReadApplicationFile(Path.Combine("Resources", "ValidationResource.resx"));
        var germanValidationSource = ReadApplicationFile(Path.Combine("Resources", "ValidationResource.de-DE.resx"));

        metaSource.Should().Contain("public sealed class MetaController : ApiControllerBase");
        metaSource.Should().Contain("private static readonly DateTime ProcessStartUtc = DateTime.UtcNow;");
        metaSource.Should().Contain("private readonly IHostEnvironment _hostEnvironment;");
        metaSource.Should().Contain("private readonly IConfiguration _configuration;");
        metaSource.Should().Contain("private readonly IClock _clock;");
        metaSource.Should().Contain("private readonly ILogger<MetaController> _logger;");
        metaSource.Should().Contain("private readonly GetAppBootstrapHandler _getAppBootstrap;");
        metaSource.Should().Contain("private readonly IStringLocalizer<ValidationResource> _validationLocalizer;");
        metaSource.Should().Contain("_clock = clock ?? throw new ArgumentNullException(nameof(clock));");
        metaSource.Should().Contain("_getAppBootstrap = getAppBootstrap ?? throw new ArgumentNullException(nameof(getAppBootstrap));");
        metaSource.Should().Contain("_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));");
        metaSource.Should().Contain("_hostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));");
        metaSource.Should().Contain("_validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));");
        metaSource.Should().Contain("_logger = logger ?? throw new ArgumentNullException(nameof(logger));");
        metaSource.Should().Contain("public ActionResult GetHealth(CancellationToken ct)");
        metaSource.Should().Contain("var nowUtc = _clock.UtcNow;");
        metaSource.Should().Contain("var uptime = nowUtc - ProcessStartUtc;");
        metaSource.Should().Contain("var uptimeSeconds = Math.Max(0d, uptime.TotalSeconds);");
        metaSource.Should().Contain("status = \"Healthy\",");
        metaSource.Should().Contain("application = ResolveApplicationName()");
        metaSource.Should().Contain("_logger.LogDebug(");
        metaSource.Should().Contain("public ActionResult GetInfo(CancellationToken ct)");
        metaSource.Should().Contain("var assembly = Assembly.GetExecutingAssembly();");
        metaSource.Should().Contain("var version = assembly.GetName().Version?.ToString() ?? \"unknown\";");
        metaSource.Should().Contain("var buildNumber = _configuration[\"Build:Number\"];");
        metaSource.Should().Contain("var commitHash = _configuration[\"Build:Commit\"];");
        metaSource.Should().Contain("_logger.LogInformation(");
        metaSource.Should().Contain("private string ResolveApplicationName()");
        metaSource.Should().Contain("var configuredName = _configuration[\"Application:Name\"];");
        metaSource.Should().Contain("if (!string.IsNullOrWhiteSpace(configuredName))");
        metaSource.Should().Contain("if (!string.IsNullOrWhiteSpace(_hostEnvironment.ApplicationName))");
        metaSource.Should().Contain("var entryName = Assembly.GetEntryAssembly()?.GetName().Name;");
        metaSource.Should().Contain("return string.IsNullOrWhiteSpace(entryName) ? \"Darwin.WebApi\" : entryName!;");
        metaSource.Should().Contain("public async Task<ActionResult<AppBootstrapResponse>> GetBootstrapAsync(CancellationToken ct)");
        metaSource.Should().Contain("var result = await _getAppBootstrap.HandleAsync(ct).ConfigureAwait(false);");
        metaSource.Should().Contain("if (!result.Succeeded)");
        metaSource.Should().Contain("return BadRequest(new { error = result.Error ?? _validationLocalizer[\"BootstrapRequestFailed\"].Value });");
        metaSource.Should().Contain("if (result.Value is null)");
        metaSource.Should().Contain("return BadRequest(new { error = _validationLocalizer[\"BootstrapPayloadEmpty\"].Value });");
        metaSource.Should().Contain("var response = new AppBootstrapResponse");
        metaSource.Should().Contain("JwtAudience = dto.JwtAudience,");
        metaSource.Should().Contain("QrTokenRefreshSeconds = dto.QrTokenRefreshSeconds,");
        metaSource.Should().Contain("MaxOutboxItems = dto.MaxOutboxItems");
        validationSource.Should().Contain("<data name=\"BootstrapRequestFailed\"");
        validationSource.Should().Contain("<data name=\"BootstrapPayloadEmpty\"");
        germanValidationSource.Should().Contain("<data name=\"BootstrapRequestFailed\"");
        germanValidationSource.Should().Contain("<data name=\"BootstrapPayloadEmpty\"");

        businessesMetaSource.Should().Contain("public sealed class BusinessesMetaController : ApiControllerBase");
        businessesMetaSource.Should().Contain("private readonly GetBusinessCategoryKindsHandler _getBusinessCategoryKindsHandler;");
        businessesMetaSource.Should().Contain("_getBusinessCategoryKindsHandler = getBusinessCategoryKindsHandler");
        businessesMetaSource.Should().Contain("?? throw new ArgumentNullException(nameof(getBusinessCategoryKindsHandler));");
        businessesMetaSource.Should().Contain("public async Task<IActionResult> GetCategoryKinds(CancellationToken ct)");
        businessesMetaSource.Should().Contain("var dto = await _getBusinessCategoryKindsHandler.HandleAsync(ct);");
        businessesMetaSource.Should().Contain("var items = dto.Items ?? Array.Empty<Darwin.Application.Businesses.DTOs.BusinessCategoryKindItemDto>();");
        businessesMetaSource.Should().Contain("var response = new BusinessCategoryKindsResponse");
        businessesMetaSource.Should().Contain("Items = items");
        businessesMetaSource.Should().Contain("Value = x.Value,");
        businessesMetaSource.Should().Contain("Key = x.Kind.ToString(),");
        businessesMetaSource.Should().Contain("DisplayName = x.DisplayName ?? string.Empty");

        conventionsSource.Should().Contain("internal static class BusinessControllerConventions");
        conventionsSource.Should().Contain("public static string? NormalizeNullable(string? value)");
        conventionsSource.Should().Contain("=> string.IsNullOrWhiteSpace(value) ? null : value.Trim();");
        conventionsSource.Should().Contain("public static bool TryGetCurrentBusinessId(ClaimsPrincipal user, out Guid businessId)");
        conventionsSource.Should().Contain("businessId = Guid.Empty;");
        conventionsSource.Should().Contain("if (user?.Identity?.IsAuthenticated != true)");
        conventionsSource.Should().Contain("var id = user.FindFirstValue(\"business_id\");");
        conventionsSource.Should().Contain("if (!Guid.TryParse(id, out var parsed))");
        conventionsSource.Should().Contain("businessId = parsed;");
        conventionsSource.Should().Contain("public static bool TryGetCurrentUserId(ClaimsPrincipal user, out Guid userId)");
        conventionsSource.Should().Contain("user.FindFirstValue(ClaimTypes.NameIdentifier) ??");
        conventionsSource.Should().Contain("user.FindFirstValue(\"sub\") ??");
        conventionsSource.Should().Contain("user.FindFirstValue(\"uid\");");
        conventionsSource.Should().Contain("public static bool TryParseBusinessCategoryKind(");
        conventionsSource.Should().Contain("IStringLocalizer<ValidationResource> localizer,");
        conventionsSource.Should().Contain("if (string.IsNullOrWhiteSpace(category))");
        conventionsSource.Should().Contain("if (Enum.TryParse<BusinessCategoryKind>(category.Trim(), ignoreCase: true, out var parsed))");
        conventionsSource.Should().Contain("error = localizer[\"BusinessCategoryKindInvalid\"];");
        conventionsSource.Should().Contain("public static (double? Value, string? Error) TryNormalizeMinRating(double? minRating, IStringLocalizer<ValidationResource> localizer)");
        conventionsSource.Should().Contain("IStringLocalizer<ValidationResource> localizer");
        conventionsSource.Should().Contain("if (double.IsNaN(minRating.Value) || double.IsInfinity(minRating.Value))");
        conventionsSource.Should().Contain("return (null, localizer[\"BusinessMinRatingFiniteRange\"]);");
        conventionsSource.Should().Contain("if (minRating.Value < 0 || minRating.Value > 5)");
        conventionsSource.Should().Contain("return (null, localizer[\"BusinessMinRatingRange\"]);");
        conventionsSource.Should().Contain("public static (GeoCoordinateDto? Coordinate, double? RadiusKm, string? Error) TryMapProximity(");
        conventionsSource.Should().Contain("if (near is null && radiusMeters is null)");
        conventionsSource.Should().Contain("if (near is null)");
        conventionsSource.Should().Contain("return (null, null, localizer[\"BusinessNearRequiredWhenRadiusProvided\"]);");
        conventionsSource.Should().Contain("if (radiusMeters.HasValue && radiusMeters.Value < 0)");
        conventionsSource.Should().Contain("return (null, null, localizer[\"BusinessRadiusMetersPositive\"]);");
        conventionsSource.Should().Contain("var coordinate = new GeoCoordinateDto");
        conventionsSource.Should().Contain("Latitude = near.Latitude,");
        conventionsSource.Should().Contain("Longitude = near.Longitude,");
        conventionsSource.Should().Contain("AltitudeMeters = near.AltitudeMeters");
        conventionsSource.Should().Contain("var radiusKm = radiusMeters.HasValue");
        conventionsSource.Should().Contain("? radiusMeters.Value / 1000.0");
    }


    [Fact]
    public void PublicAndMemberBusinessControllers_Should_KeepDiscoveryOnboardingAndEngagementOrchestrationContractsWired()
    {
        var publicSource = ReadWebApiFile(Path.Combine("Controllers", "Public", "PublicBusinessesController.cs"));
        var memberSource = ReadWebApiFile(Path.Combine("Controllers", "Member", "MemberBusinessesController.cs"));
        var validationSource = ReadApplicationFile(Path.Combine("Resources", "ValidationResource.resx"));
        var germanValidationSource = ReadApplicationFile(Path.Combine("Resources", "ValidationResource.de-DE.resx"));

        publicSource.Should().Contain("public sealed class PublicBusinessesController : ApiControllerBase");
        publicSource.Should().Contain("private const int MaxPageSize = 100;");
        publicSource.Should().Contain("private readonly GetBusinessesForDiscoveryHandler _getBusinessesForDiscovery;");
        publicSource.Should().Contain("private readonly GetBusinessesForMapDiscoveryHandler _getBusinessesForMapDiscovery;");
        publicSource.Should().Contain("private readonly GetBusinessPublicDetailHandler _getBusinessPublicDetail;");
        publicSource.Should().Contain("private readonly IStringLocalizer<ValidationResource> _validationLocalizer;");
        publicSource.Should().Contain("_getBusinessesForDiscovery = getBusinessesForDiscovery ?? throw new ArgumentNullException(nameof(getBusinessesForDiscovery));");
        publicSource.Should().Contain("_getBusinessesForMapDiscovery = getBusinessesForMapDiscovery ?? throw new ArgumentNullException(nameof(getBusinessesForMapDiscovery));");
        publicSource.Should().Contain("_getBusinessPublicDetail = getBusinessPublicDetail ?? throw new ArgumentNullException(nameof(getBusinessPublicDetail));");
        publicSource.Should().Contain("_validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));");
        publicSource.Should().Contain("public async Task<IActionResult> ListAsync([FromBody] BusinessListRequest? request, CancellationToken ct = default)");
        publicSource.Should().Contain("return BadRequestProblem(_validationLocalizer[\"RequestPayloadRequired\"]);");
        publicSource.Should().Contain("var page = request.Page < 1 ? 1 : request.Page;");
        publicSource.Should().Contain("var pageSize = request.PageSize < 1 ? 20 : request.PageSize;");
        publicSource.Should().Contain("if (pageSize > MaxPageSize)");
        publicSource.Should().Contain("var queryText = BusinessControllerConventions.NormalizeNullable(request.Query) ?? BusinessControllerConventions.NormalizeNullable(request.Search);");
        publicSource.Should().Contain("if (!BusinessControllerConventions.TryParseBusinessCategoryKind(request.CategoryKindKey, _validationLocalizer, out var categoryKind, out var categoryError))");
        publicSource.Should().Contain("var (coordinate, radiusKm, proximityError) = BusinessControllerConventions.TryMapProximity(request.Near, _validationLocalizer, request.RadiusMeters);");
        publicSource.Should().Contain("if (proximityError is not null)");
        publicSource.Should().Contain("var (minRating, ratingError) = BusinessControllerConventions.TryNormalizeMinRating(request.MinRating, _validationLocalizer);");
        publicSource.Should().Contain("if (ratingError is not null)");
        publicSource.Should().Contain("var appRequest = new BusinessDiscoveryRequestDto");
        publicSource.Should().Contain("Query = queryText,");
        publicSource.Should().Contain("City = BusinessControllerConventions.NormalizeNullable(request.City),");
        publicSource.Should().Contain("CountryCode = BusinessControllerConventions.NormalizeNullable(request.CountryCode),");
        publicSource.Should().Contain("AddressQuery = BusinessControllerConventions.NormalizeNullable(request.AddressQuery),");
        publicSource.Should().Contain("Coordinate = coordinate,");
        publicSource.Should().Contain("RadiusKm = radiusKm");
        publicSource.Should().Contain("var (items, total) = await _getBusinessesForDiscovery.HandleAsync(appRequest, ct).ConfigureAwait(false);");
        publicSource.Should().Contain("Items = (items ?? new List<BusinessDiscoveryListItemDto>())");
        publicSource.Should().Contain(".Select(BusinessContractsMapper.ToContract)");
        publicSource.Should().Contain("Request = new PagedRequest");
        publicSource.Should().Contain("Search = queryText");

        publicSource.Should().Contain("public async Task<IActionResult> MapAsync([FromBody] BusinessMapDiscoveryRequest? request, CancellationToken ct = default)");
        publicSource.Should().Contain("if (request?.Bounds is null)");
        publicSource.Should().Contain("return BadRequestProblem(_validationLocalizer[\"MapBoundsRequired\"]);");
        publicSource.Should().Contain("var page = request.Page.GetValueOrDefault(1);");
        publicSource.Should().Contain("return BadRequestProblem(_validationLocalizer[\"PageMustBePositiveInteger\"]);");
        publicSource.Should().Contain("var pageSize = request.PageSize.GetValueOrDefault(200);");
        publicSource.Should().Contain("return BadRequestProblem(_validationLocalizer[\"PageSizeMustBeBetween1And500\"]);");
        publicSource.Should().Contain("if (!BusinessControllerConventions.TryParseBusinessCategoryKind(request.Category, _validationLocalizer, out var categoryKind, out var categoryError))");
        publicSource.Should().Contain("var dto = new BusinessMapDiscoveryRequestDto");
        publicSource.Should().Contain("Bounds = new GeoBoundsDto");
        publicSource.Should().Contain("NorthLat = request.Bounds.NorthLat,");
        publicSource.Should().Contain("SouthLat = request.Bounds.SouthLat,");
        publicSource.Should().Contain("EastLon = request.Bounds.EastLon,");
        publicSource.Should().Contain("WestLon = request.Bounds.WestLon");
        publicSource.Should().Contain("var (items, total) = await _getBusinessesForMapDiscovery.HandleAsync(dto, ct).ConfigureAwait(false);");
        publicSource.Should().Contain("Search = dto.Query");

        publicSource.Should().Contain("public async Task<IActionResult> GetAsync([FromRoute] Guid id, CancellationToken ct = default)");
        publicSource.Should().Contain("if (id == Guid.Empty)");
        publicSource.Should().Contain("return BadRequestProblem(_validationLocalizer[\"BusinessIdValidWhenProvided\"]);");
        publicSource.Should().Contain("var dto = await _getBusinessPublicDetail.HandleAsync(id, ct).ConfigureAwait(false);");
        publicSource.Should().Contain("return NotFoundProblem(_validationLocalizer[\"BusinessNotFound\"]);");
        publicSource.Should().Contain("return Ok(BusinessContractsMapper.ToContract(dto));");

        memberSource.Should().Contain("public sealed class MemberBusinessesController : ApiControllerBase");
        memberSource.Should().Contain("private readonly GetBusinessPublicDetailWithMyAccountHandler _getBusinessPublicDetailWithMyAccountHandler;");
        memberSource.Should().Contain("private readonly GetBusinessEngagementForMemberHandler _getBusinessEngagementForMemberHandler;");
        memberSource.Should().Contain("private readonly ToggleBusinessLikeHandler _toggleBusinessLikeHandler;");
        memberSource.Should().Contain("private readonly ToggleBusinessFavoriteHandler _toggleBusinessFavoriteHandler;");
        memberSource.Should().Contain("private readonly UpsertBusinessReviewHandler _upsertBusinessReviewHandler;");
        memberSource.Should().Contain("private readonly CreateBusinessHandler _createBusinessHandler;");
        memberSource.Should().Contain("private readonly CreateBusinessMemberHandler _createBusinessMemberHandler;");
        memberSource.Should().Contain("private readonly IStringLocalizer<ValidationResource> _validationLocalizer;");
        memberSource.Should().Contain("_createBusinessHandler = createBusinessHandler ?? throw new ArgumentNullException(nameof(createBusinessHandler));");
        memberSource.Should().Contain("_createBusinessMemberHandler = createBusinessMemberHandler ?? throw new ArgumentNullException(nameof(createBusinessMemberHandler));");
        memberSource.Should().Contain("_validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));");

        memberSource.Should().Contain("public async Task<IActionResult> OnboardAsync([FromBody] BusinessOnboardingRequest? request, CancellationToken ct = default)");
        memberSource.Should().Contain("return BadRequestProblem(_validationLocalizer[\"RequestPayloadRequired\"]);");
        memberSource.Should().Contain("return BadRequestProblem(_validationLocalizer[\"BusinessNameRequired\"]);");
        memberSource.Should().Contain("if (!BusinessControllerConventions.TryGetCurrentUserId(User, out var userId))");
        memberSource.Should().Contain("return StatusCode(StatusCodes.Status401Unauthorized, new Darwin.Contracts.Common.ProblemDetails");
        memberSource.Should().Contain("Title = _validationLocalizer[\"UnauthorizedTitle\"],");
        memberSource.Should().Contain("Detail = _validationLocalizer[\"AuthenticatedUserIdentifierNotResolved\"],");
        memberSource.Should().Contain("if (!BusinessControllerConventions.TryParseBusinessCategoryKind(request.CategoryKindKey, _validationLocalizer, out var categoryKind, out var categoryError))");
        memberSource.Should().Contain("var createBusinessDto = new BusinessCreateDto");
        memberSource.Should().Contain("LegalName = BusinessControllerConventions.NormalizeNullable(request.LegalName),");
        memberSource.Should().Contain("TaxId = BusinessControllerConventions.NormalizeNullable(request.TaxId),");
        memberSource.Should().Contain("ShortDescription = BusinessControllerConventions.NormalizeNullable(request.ShortDescription),");
        memberSource.Should().Contain("WebsiteUrl = BusinessControllerConventions.NormalizeNullable(request.WebsiteUrl),");
        memberSource.Should().Contain("ContactEmail = BusinessControllerConventions.NormalizeNullable(request.ContactEmail),");
        memberSource.Should().Contain("ContactPhoneE164 = BusinessControllerConventions.NormalizeNullable(request.ContactPhoneE164),");
        memberSource.Should().Contain("Category = categoryKind ?? BusinessCategoryKind.Unknown,");
        memberSource.Should().Contain("DefaultCurrency = BusinessControllerConventions.NormalizeNullable(request.DefaultCurrency) ?? SiteSettingDto.DefaultCurrencyDefault,");
        memberSource.Should().Contain("DefaultCulture = BusinessControllerConventions.NormalizeNullable(request.DefaultCulture) ?? SiteSettingDto.DefaultCultureDefault,");
        memberSource.Should().Contain("IsActive = true");
        memberSource.Should().Contain("businessId = await _createBusinessHandler.HandleAsync(createBusinessDto, ct).ConfigureAwait(false);");

        validationSource.Should().Contain("<data name=\"BusinessCategoryKindInvalid\"");
        validationSource.Should().Contain("<data name=\"BusinessMinRatingFiniteRange\"");
        validationSource.Should().Contain("<data name=\"BusinessMinRatingRange\"");
        validationSource.Should().Contain("<data name=\"BusinessNearRequiredWhenRadiusProvided\"");
        validationSource.Should().Contain("<data name=\"BusinessRadiusMetersPositive\"");
        germanValidationSource.Should().Contain("<data name=\"BusinessCategoryKindInvalid\"");
        germanValidationSource.Should().Contain("<data name=\"BusinessMinRatingFiniteRange\"");
        germanValidationSource.Should().Contain("<data name=\"BusinessMinRatingRange\"");
        germanValidationSource.Should().Contain("<data name=\"BusinessNearRequiredWhenRadiusProvided\"");
        germanValidationSource.Should().Contain("<data name=\"BusinessRadiusMetersPositive\"");
        memberSource.Should().Contain("return BadRequestProblem(_validationLocalizer[\"BusinessOnboardingPayloadInvalid\"], ex.Message);");
        memberSource.Should().Contain("businessMemberId = await _createBusinessMemberHandler.HandleAsync(new BusinessMemberCreateDto");
        memberSource.Should().Contain("Role = BusinessMemberRole.Owner,");
        memberSource.Should().Contain("return BadRequestProblem(_validationLocalizer[\"BusinessOwnerMembershipCreateFailed\"], ex.Message);");
        memberSource.Should().Contain("return Ok(new BusinessOnboardingResponse");
        memberSource.Should().Contain("BusinessMemberId = businessMemberId");

        memberSource.Should().Contain("public async Task<IActionResult> GetWithMyAccountAsync([FromRoute] Guid id, CancellationToken ct = default)");
        memberSource.Should().Contain("if (id == Guid.Empty)");
        memberSource.Should().Contain("return NotFoundProblem(_validationLocalizer[\"BusinessNotFound\"]);");
        memberSource.Should().Contain("var result = await _getBusinessPublicDetailWithMyAccountHandler.HandleAsync(id, ct).ConfigureAwait(false);");
        memberSource.Should().Contain("if (!result.Succeeded)");
        memberSource.Should().Contain("if (dto is null || dto.Business is null)");
        memberSource.Should().Contain("return Ok(BusinessContractsMapper.ToContract(dto));");

        memberSource.Should().Contain("public async Task<IActionResult> GetMyEngagementAsync([FromRoute] Guid id, CancellationToken ct = default)");
        memberSource.Should().Contain("return BadRequestProblem(_validationLocalizer[\"BusinessIdValidWhenProvided\"]);");
        memberSource.Should().Contain("var result = await _getBusinessEngagementForMemberHandler.HandleAsync(id, ct).ConfigureAwait(false);");
        memberSource.Should().Contain("if (!result.Succeeded || result.Value is null)");
        memberSource.Should().Contain("return Ok(new BusinessEngagementSummaryResponse");
        memberSource.Should().Contain("MyReview = dto.MyReview is null");
        memberSource.Should().Contain("RecentReviews = dto.RecentReviews.Select(r => new BusinessReviewItem");

        memberSource.Should().Contain("public async Task<IActionResult> ToggleLikeAsync([FromRoute] Guid id, CancellationToken ct = default)");
        memberSource.Should().Contain("var result = await _toggleBusinessLikeHandler.HandleAsync(id, ct).ConfigureAwait(false);");
        memberSource.Should().Contain("return Ok(new ToggleBusinessReactionResponse");
        memberSource.Should().Contain("IsActive = result.Value.IsActive,");
        memberSource.Should().Contain("TotalCount = result.Value.TotalCount");

        memberSource.Should().Contain("public async Task<IActionResult> ToggleFavoriteAsync([FromRoute] Guid id, CancellationToken ct = default)");
        memberSource.Should().Contain("var result = await _toggleBusinessFavoriteHandler.HandleAsync(id, ct).ConfigureAwait(false);");

        memberSource.Should().Contain("public async Task<IActionResult> UpsertMyReviewAsync([FromRoute] Guid id, [FromBody] UpsertBusinessReviewRequest? request, CancellationToken ct = default)");
        memberSource.Should().Contain("if (request is null)");
        memberSource.Should().Contain("return BadRequestProblem(_validationLocalizer[\"RequestPayloadRequired\"]);");
        memberSource.Should().Contain("HandleAsync(id, new UpsertBusinessReviewDto");
        memberSource.Should().Contain("Rating = request.Rating,");
        memberSource.Should().Contain("Comment = request.Comment");
        memberSource.Should().Contain("return NoContent();");
    }


    [Fact]
    public void LoyaltyController_Should_KeepLocalizedMemberLoyaltyGuardAndTimelineContractsWired()
    {
        var source = ReadWebApiFile(Path.Combine("Controllers", "Loyalty", "LoyaltyController.cs"));
        var validationSource = ReadApplicationFile(Path.Combine("Resources", "ValidationResource.resx"));
        var germanValidationSource = ReadApplicationFile(Path.Combine("Resources", "ValidationResource.de-DE.resx"));

        source.Should().Contain("public sealed class LoyaltyController : ApiControllerBase");
        source.Should().Contain("private readonly PrepareScanSessionHandler _prepareScanSessionHandler;");
        source.Should().Contain("private readonly GetMyLoyaltyTimelinePageHandler _getMyLoyaltyTimelinePageHandler;");
        source.Should().Contain("private readonly TrackPromotionInteractionHandler _trackPromotionInteractionHandler;");
        source.Should().Contain("private readonly ILoyaltyPresentationService _presentationService;");
        source.Should().Contain("private readonly IStringLocalizer<ValidationResource> _validationLocalizer;");
        source.Should().Contain("_presentationService = presentationService ?? throw new ArgumentNullException(nameof(presentationService));");
        source.Should().Contain("_validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));");

        source.Should().Contain("public async Task<IActionResult> PrepareScanSessionAsync(");
        source.Should().Contain("return BadRequestProblem(_validationLocalizer[\"RequestPayloadRequired\"]);");
        source.Should().Contain("return BadRequestProblem(_validationLocalizer[\"BusinessIdRequired\"]);");
        source.Should().Contain("Mode = LoyaltyContractsMapper.ToDomain(request.Mode),");
        source.Should().Contain("SelectedRewardTierIds = request.SelectedRewardTierIds?");
        source.Should().Contain("DeviceId = NormalizeText(request.DeviceId)");
        source.Should().Contain("EnrichSelectedRewardsAsync(request.BusinessId, result.Value.SelectedRewardTierIds, failIfMissing: true, ct)");
        source.Should().Contain("ScanSessionToken = result.Value.ScanSessionToken,");
        source.Should().Contain("SelectedRewards = selectedRewards");

        source.Should().Contain("public async Task<IActionResult> GetCurrentAccountForBusinessAsync(Guid businessId, CancellationToken ct = default)");
        source.Should().Contain("? NotFoundProblem(_validationLocalizer[\"LoyaltyAccountNotFoundForSpecifiedBusinessAndUser\"])");

        source.Should().Contain("GetBusinessDashboardAsync(");
        source.Should().Contain("? NotFoundProblem(_validationLocalizer[\"LoyaltyDashboardNotFoundForSpecifiedBusinessAndUser\"])");

        source.Should().Contain("public async Task<IActionResult> GetMyBusinessesAsync(");
        source.Should().Contain("return BadRequestProblem(_validationLocalizer[\"PageMustBePositiveInteger\"]);");
        source.Should().Contain("return BadRequestProblem(_validationLocalizer[\"PageSizeMustBeBetween1And200\"]);");
        source.Should().Contain("var request = new MyLoyaltyBusinessListRequestDto");
        source.Should().Contain("IncludeInactiveBusinesses = includeInactiveBusinesses.GetValueOrDefault(false)");

        source.Should().Contain("public async Task<IActionResult> GetMyPromotionsAsync([FromBody] MyPromotionsRequest? request, CancellationToken ct = default)");
        source.Should().Contain("return BadRequestProblem(_validationLocalizer[\"BusinessIdValidWhenProvided\"]);");
        source.Should().Contain("BusinessId = request.BusinessId,");
        source.Should().Contain("Policy = request.Policy is null");

        source.Should().Contain("public async Task<IActionResult> TrackPromotionInteractionAsync(");
        source.Should().Contain("return BadRequestProblem(_validationLocalizer[\"TitleRequired\"]);");
        source.Should().Contain("Title = request.Title.Trim(),");
        source.Should().Contain("EventType = MapPromotionInteractionEventType(request.EventType),");

        source.Should().Contain("public async Task<IActionResult> GetMyLoyaltyTimelinePageAsync(");
        source.Should().Contain("if (!request.BusinessId.HasValue || request.BusinessId.Value == Guid.Empty)");
        source.Should().Contain("return BadRequestProblem(_validationLocalizer[\"InvalidTimelineCursor\"]);");
        source.Should().Contain("BeforeAtUtc = request.BeforeAtUtc,");
        source.Should().Contain("BeforeId = request.BeforeId");
        source.Should().Contain("NextBeforeAtUtc = result.Value.NextBeforeAtUtc,");
        source.Should().Contain("NextBeforeId = result.Value.NextBeforeId");

        source.Should().Contain("public async Task<IActionResult> JoinLoyaltyAsync(");
        source.Should().Contain("HandleAsync(businessId, request?.BusinessLocationId, ct)");

        source.Should().Contain("public async Task<IActionResult> GetNextRewardAsync([FromRoute] Guid businessId, CancellationToken ct = default)");
        source.Should().Contain("return NotFoundProblem(_validationLocalizer[\"LoyaltyAccountNotFoundForSpecifiedBusinessAndUser\"]);");
        source.Should().Contain(".Where(r => r.RequiredPoints > account.PointsBalance && r.IsActive && r.IsSelectable)");
        source.Should().Contain("? NoContent()");
        source.Should().Contain(": Ok(LoyaltyContractsMapper.ToContract(candidate));");

        source.Should().Contain("private static Darwin.Application.Loyalty.DTOs.PromotionInteractionEventType MapPromotionInteractionEventType(");
        source.Should().Contain("PromotionInteractionEventType.Open => Darwin.Application.Loyalty.DTOs.PromotionInteractionEventType.Open,");
        source.Should().Contain("PromotionInteractionEventType.Claim => Darwin.Application.Loyalty.DTOs.PromotionInteractionEventType.Claim,");
        source.Should().Contain("_ => Darwin.Application.Loyalty.DTOs.PromotionInteractionEventType.Impression");

        source.Should().NotContain("Request body is required.");
        source.Should().NotContain("BusinessId is required.");
        source.Should().NotContain("Page must be a positive integer.");
        source.Should().NotContain("PageSize must be between 1 and 200.");
        source.Should().NotContain("Loyalty account not found for the specified business and user.");
        source.Should().NotContain("Loyalty dashboard not found for the specified business and user.");
        source.Should().NotContain("Title is required.");
        source.Should().NotContain("Invalid cursor. Both BeforeAtUtc and BeforeId must be provided together.");

        validationSource.Should().Contain("<data name=\"LoyaltyDashboardNotFoundForSpecifiedBusinessAndUser\"");
        germanValidationSource.Should().Contain("<data name=\"LoyaltyDashboardNotFoundForSpecifiedBusinessAndUser\"");
    }


    [Fact]
    public void DiscoveryCategoryAndPromotionTimelineContracts_Should_KeepFilterPolicyAndFeedShapeFloors()
    {
        var categoryItemSource = ReadContractsFile(Path.Combine("Businesses", "BusinessCategoryKindItem.cs"));
        var categoryKindsResponseSource = ReadContractsFile(Path.Combine("Businesses", "BusinessCategoryKindsResponse.cs"));
        var mapDiscoveryRequestSource = ReadContractsFile(Path.Combine("Businesses", "BusinessMapDiscoveryRequest.cs"));
        var promotionsRequestSource = ReadContractsFile(Path.Combine("Loyalty", "MyPromotionsRequest.cs"));
        var trackPromotionSource = ReadContractsFile(Path.Combine("Loyalty", "TrackPromotionInteractionRequest.cs"));
        var timelineRequestSource = ReadContractsFile(Path.Combine("Loyalty", "GetMyLoyaltyTimelinePageRequest.cs"));
        var promotionFeedItemSource = ReadContractsFile(Path.Combine("Loyalty", "PromotionFeedItem.cs"));
        var promotionFeedPolicySource = ReadContractsFile(Path.Combine("Loyalty", "PromotionFeedPolicy.cs"));

        categoryItemSource.Should().Contain("public sealed class BusinessCategoryKindItem");
        categoryItemSource.Should().Contain("public int Value { get; set; }");
        categoryItemSource.Should().Contain("public string Key { get; set; } = string.Empty;");
        categoryItemSource.Should().Contain("public string DisplayName { get; set; } = string.Empty;");

        categoryKindsResponseSource.Should().Contain("public sealed class BusinessCategoryKindsResponse");
        categoryKindsResponseSource.Should().Contain("public IReadOnlyList<BusinessCategoryKindItem> Items { get; set; } = Array.Empty<BusinessCategoryKindItem>();");

        mapDiscoveryRequestSource.Should().Contain("public sealed class BusinessMapDiscoveryRequest");
        mapDiscoveryRequestSource.Should().Contain("public GeoBoundsModel? Bounds { get; set; }");
        mapDiscoveryRequestSource.Should().Contain("public int? Page { get; set; }");
        mapDiscoveryRequestSource.Should().Contain("public int? PageSize { get; set; }");
        mapDiscoveryRequestSource.Should().Contain("public string? Category { get; set; }");
        mapDiscoveryRequestSource.Should().Contain("public string? Query { get; set; }");
        mapDiscoveryRequestSource.Should().Contain("public string? CountryCode { get; set; }");

        promotionsRequestSource.Should().Contain("public sealed class MyPromotionsRequest");
        promotionsRequestSource.Should().Contain("public Guid? BusinessId { get; init; }");
        promotionsRequestSource.Should().Contain("public int MaxItems { get; init; } = 20;");
        promotionsRequestSource.Should().Contain("public PromotionFeedPolicy? Policy { get; init; }");

        trackPromotionSource.Should().Contain("public sealed class TrackPromotionInteractionRequest");
        trackPromotionSource.Should().Contain("public Guid BusinessId { get; init; }");
        trackPromotionSource.Should().Contain("public string BusinessName { get; init; } = string.Empty;");
        trackPromotionSource.Should().Contain("public string Title { get; init; } = string.Empty;");
        trackPromotionSource.Should().Contain("public string CtaKind { get; init; } = string.Empty;");
        trackPromotionSource.Should().Contain("public PromotionInteractionEventType EventType { get; init; } = PromotionInteractionEventType.Impression;");
        trackPromotionSource.Should().Contain("public DateTime? OccurredAtUtc { get; init; }");

        timelineRequestSource.Should().Contain("public sealed class GetMyLoyaltyTimelinePageRequest");
        timelineRequestSource.Should().Contain("public Guid? BusinessId { get; init; }");
        timelineRequestSource.Should().Contain("public int PageSize { get; init; } = 30;");
        timelineRequestSource.Should().Contain("public DateTime? BeforeAtUtc { get; init; }");
        timelineRequestSource.Should().Contain("public Guid? BeforeId { get; init; }");

        promotionFeedItemSource.Should().Contain("public sealed class PromotionFeedItem");
        promotionFeedItemSource.Should().Contain("public Guid BusinessId { get; init; }");
        promotionFeedItemSource.Should().Contain("public string BusinessName { get; init; } = string.Empty;");
        promotionFeedItemSource.Should().Contain("public string Title { get; init; } = string.Empty;");
        promotionFeedItemSource.Should().Contain("public string Description { get; init; } = string.Empty;");
        promotionFeedItemSource.Should().Contain("public string CtaKind { get; init; } = \"OpenRewards\";");
        promotionFeedItemSource.Should().Contain("public int Priority { get; init; }");
        promotionFeedItemSource.Should().Contain("public Guid? CampaignId { get; init; }");
        promotionFeedItemSource.Should().Contain("public string CampaignState { get; init; } = PromotionCampaignState.Active;");
        promotionFeedItemSource.Should().Contain("public DateTime? StartsAtUtc { get; init; }");
        promotionFeedItemSource.Should().Contain("public DateTime? EndsAtUtc { get; init; }");
        promotionFeedItemSource.Should().Contain("public List<PromotionEligibilityRule> EligibilityRules { get; init; } = new();");

        promotionFeedPolicySource.Should().Contain("public sealed class PromotionFeedPolicy");
        promotionFeedPolicySource.Should().Contain("public bool EnableDeduplication { get; init; } = true;");
        promotionFeedPolicySource.Should().Contain("public int MaxCards { get; init; } = 6;");
        promotionFeedPolicySource.Should().Contain("public int? FrequencyWindowMinutes { get; init; }");
        promotionFeedPolicySource.Should().Contain("= null;");
        promotionFeedPolicySource.Should().Contain("public int? SuppressionWindowMinutes { get; init; } = 480;");
    }


    [Fact]
    public void CommonContracts_Should_KeepEnvelopeDefaultsPagingGeoAndProblemFloors()
    {
        var apiEnvelopeSource = ReadContractsFile(Path.Combine("Common", "ApiEnvelope.cs"));
        var defaultsSource = ReadContractsFile(Path.Combine("Common", "ContractDefaults.cs"));
        var geoBoundsSource = ReadContractsFile(Path.Combine("Common", "GeoBoundsModel.cs"));
        var geoCoordinateSource = ReadContractsFile(Path.Combine("Common", "GeoCoordinateModel.cs"));
        var pagedRequestSource = ReadContractsFile(Path.Combine("Common", "PagedRequest.cs"));
        var pagedResponseSource = ReadContractsFile(Path.Combine("Common", "PagedResponse.cs"));
        var problemDetailsSource = ReadContractsFile(Path.Combine("Common", "ProblemDetails.cs"));
        var sortOptionSource = ReadContractsFile(Path.Combine("Common", "SortOption.cs"));

        apiEnvelopeSource.Should().Contain("public sealed class ApiEnvelope<T>");
        apiEnvelopeSource.Should().Contain("public bool Succeeded { get; init; }");
        apiEnvelopeSource.Should().Contain("public string? Message { get; init; }");
        apiEnvelopeSource.Should().Contain("public string? ErrorCode { get; init; }");
        apiEnvelopeSource.Should().Contain("public T? Data { get; init; }");
        apiEnvelopeSource.Should().Contain("public static ApiEnvelope<T> Ok(T data, string? message = null)");
        apiEnvelopeSource.Should().Contain("=> new() { Succeeded = true, Data = data, Message = message };");
        apiEnvelopeSource.Should().Contain("public static ApiEnvelope<T> Fail(string message, string? errorCode = null)");
        apiEnvelopeSource.Should().Contain("=> new() { Succeeded = false, Message = message, ErrorCode = errorCode };");

        defaultsSource.Should().Contain("public static class ContractDefaults");
        defaultsSource.Should().Contain("public const string DefaultLocale = \"de-DE\";");
        defaultsSource.Should().Contain("public const string DefaultTimezone = \"Europe/Berlin\";");
        defaultsSource.Should().Contain("public const string DefaultCurrency = \"EUR\";");
        defaultsSource.Should().Contain("public const string DefaultCountryCode = \"DE\";");

        geoBoundsSource.Should().Contain("public sealed class GeoBoundsModel");
        geoBoundsSource.Should().Contain("public double NorthLat { get; set; }");
        geoBoundsSource.Should().Contain("public double SouthLat { get; set; }");
        geoBoundsSource.Should().Contain("public double EastLon { get; set; }");
        geoBoundsSource.Should().Contain("public double WestLon { get; set; }");

        geoCoordinateSource.Should().Contain("public sealed class GeoCoordinateModel");
        geoCoordinateSource.Should().Contain("public double Latitude { get; init; }");
        geoCoordinateSource.Should().Contain("public double Longitude { get; init; }");
        geoCoordinateSource.Should().Contain("public double? AltitudeMeters { get; init; }");

        pagedRequestSource.Should().Contain("public class PagedRequest");
        pagedRequestSource.Should().Contain("public int Page { get; init; } = 1;");
        pagedRequestSource.Should().Contain("public int PageSize { get; init; } = 20;");
        pagedRequestSource.Should().Contain("public string? Search { get; init; }");

        pagedResponseSource.Should().Contain("public class PagedResponse<T>");
        pagedResponseSource.Should().Contain("public long Total { get; init; }");
        pagedResponseSource.Should().Contain("public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();");
        pagedResponseSource.Should().Contain("public PagedRequest Request { get; init; } = new();");

        problemDetailsSource.Should().Contain("public sealed class ProblemDetails");
        problemDetailsSource.Should().Contain("public string? Type { get; set; }");
        problemDetailsSource.Should().Contain("public string Title { get; set; } = string.Empty;");
        problemDetailsSource.Should().Contain("public int Status { get; set; }");
        problemDetailsSource.Should().Contain("public string? Detail { get; set; }");
        problemDetailsSource.Should().Contain("public string? Instance { get; set; }");
        problemDetailsSource.Should().Contain("public Dictionary<string, string[]> Extensions { get; set; } = new();");

        sortOptionSource.Should().Contain("public sealed class SortOption");
        sortOptionSource.Should().Contain("public string Field { get; init; } = \"Name\";");
        sortOptionSource.Should().Contain("public string Direction { get; init; } = \"asc\";");
    }


    [Fact]
    public void CartOrderProfileAndNotificationContracts_Should_KeepMemberAndStorefrontCommerceShapeFloors()
    {
        var cartContractsSource = ReadContractsFile(Path.Combine("Cart", "PublicCartContracts.cs"));
        var memberOrderContractsSource = ReadContractsFile(Path.Combine("Orders", "MemberOrderContracts.cs"));
        var storefrontCheckoutContractsSource = ReadContractsFile(Path.Combine("Orders", "StorefrontCheckoutContracts.cs"));
        var customerProfileSource = ReadContractsFile(Path.Combine("Profile", "CustomerProfile.cs"));
        var memberAddressContractsSource = ReadContractsFile(Path.Combine("Profile", "MemberAddressContracts.cs"));
        var memberPreferenceContractsSource = ReadContractsFile(Path.Combine("Profile", "MemberPreferenceContracts.cs"));
        var phoneVerificationContractsSource = ReadContractsFile(Path.Combine("Profile", "PhoneVerificationContracts.cs"));
        var profileDefaultsSource = ReadContractsFile(Path.Combine("Profile", "ProfileContractDefaults.cs"));
        var accountDeletionRequestSource = ReadContractsFile(Path.Combine("Profile", "RequestAccountDeletionRequest.cs"));
        var pushDeviceRequestSource = ReadContractsFile(Path.Combine("Notifications", "RegisterPushDeviceRequest.cs"));
        var pushDeviceResponseSource = ReadContractsFile(Path.Combine("Notifications", "RegisterPushDeviceResponse.cs"));
        var mobilePlatformSource = ReadContractsFile(Path.Combine("Notifications", "MobileDevicePlatform.cs"));

        cartContractsSource.Should().Contain("public sealed class PublicCartAddItemRequest");
        cartContractsSource.Should().Contain("public string? AnonymousId { get; set; }");
        cartContractsSource.Should().Contain("public int Quantity { get; set; } = 1;");
        cartContractsSource.Should().Contain("public IReadOnlyList<Guid> SelectedAddOnValueIds { get; set; } = Array.Empty<Guid>();");
        cartContractsSource.Should().Contain("public sealed class PublicCartSummary");
        cartContractsSource.Should().Contain("public string Currency { get; set; } = ContractDefaults.DefaultCurrency;");
        cartContractsSource.Should().Contain("public IReadOnlyList<PublicCartItemRow> Items { get; set; } = Array.Empty<PublicCartItemRow>();");
        cartContractsSource.Should().Contain("public string SelectedAddOnValueIdsJson { get; set; } = \"[]\";");

        memberOrderContractsSource.Should().Contain("public sealed class MemberOrderSummary");
        memberOrderContractsSource.Should().Contain("public string Currency { get; set; } = ContractDefaults.DefaultCurrency;");
        memberOrderContractsSource.Should().Contain("public sealed class MemberOrderDetail");
        memberOrderContractsSource.Should().Contain("public string BillingAddressJson { get; set; } = \"{}\";");
        memberOrderContractsSource.Should().Contain("public string ShippingAddressJson { get; set; } = \"{}\";");
        memberOrderContractsSource.Should().Contain("public IReadOnlyList<MemberOrderLine> Lines { get; set; } = Array.Empty<MemberOrderLine>();");
        memberOrderContractsSource.Should().Contain("public IReadOnlyList<MemberOrderPayment> Payments { get; set; } = Array.Empty<MemberOrderPayment>();");
        memberOrderContractsSource.Should().Contain("public IReadOnlyList<MemberOrderShipment> Shipments { get; set; } = Array.Empty<MemberOrderShipment>();");
        memberOrderContractsSource.Should().Contain("public IReadOnlyList<MemberOrderInvoice> Invoices { get; set; } = Array.Empty<MemberOrderInvoice>();");
        memberOrderContractsSource.Should().Contain("public MemberOrderActions Actions { get; set; } = new();");
        memberOrderContractsSource.Should().Contain("public string ConfirmationPath { get; set; } = string.Empty;");
        memberOrderContractsSource.Should().Contain("public string DocumentPath { get; set; } = string.Empty;");
        memberOrderContractsSource.Should().Contain("public DateTime CreatedAtUtc { get; set; }");
        memberOrderContractsSource.Should().Contain("public DateTime? PaidAtUtc { get; set; }");
        memberOrderContractsSource.Should().Contain("public string? TrackingUrl { get; set; }");

        storefrontCheckoutContractsSource.Should().Contain("public sealed class CheckoutAddress");
        storefrontCheckoutContractsSource.Should().Contain("public string CountryCode { get; set; } = ContractDefaults.DefaultCountryCode;");
        storefrontCheckoutContractsSource.Should().Contain("public sealed class PlaceOrderFromCartRequest");
        storefrontCheckoutContractsSource.Should().Contain("public Guid? BillingAddressId { get; set; }");
        storefrontCheckoutContractsSource.Should().Contain("public CheckoutAddress? BillingAddress { get; set; }");
        storefrontCheckoutContractsSource.Should().Contain("public string? Culture { get; set; }");
        storefrontCheckoutContractsSource.Should().Contain("public sealed class CreateCheckoutIntentResponse");
        storefrontCheckoutContractsSource.Should().Contain("public IReadOnlyList<Darwin.Contracts.Shipping.PublicShippingOption> ShippingOptions { get; set; } = Array.Empty<Darwin.Contracts.Shipping.PublicShippingOption>();");
        storefrontCheckoutContractsSource.Should().Contain("public sealed class CreateStorefrontPaymentIntentResponse");
        storefrontCheckoutContractsSource.Should().Contain("public string? ProviderPaymentIntentReference { get; set; }");
        storefrontCheckoutContractsSource.Should().Contain("public string? ProviderCheckoutSessionReference { get; set; }");
        storefrontCheckoutContractsSource.Should().Contain("public string CheckoutUrl { get; set; } = string.Empty;");
        storefrontCheckoutContractsSource.Should().Contain("public string ReturnUrl { get; set; } = string.Empty;");
        storefrontCheckoutContractsSource.Should().Contain("public string CancelUrl { get; set; } = string.Empty;");
        storefrontCheckoutContractsSource.Should().Contain("public DateTime ExpiresAtUtc { get; set; }");
        storefrontCheckoutContractsSource.Should().Contain("public sealed class CompleteStorefrontPaymentRequest");
        storefrontCheckoutContractsSource.Should().Contain("public string? ProviderPaymentIntentReference { get; set; }");
        storefrontCheckoutContractsSource.Should().Contain("public string? ProviderCheckoutSessionReference { get; set; }");
        storefrontCheckoutContractsSource.Should().Contain("public string Outcome { get; set; } = \"Succeeded\";");
        storefrontCheckoutContractsSource.Should().Contain("public sealed class StorefrontOrderConfirmationResponse");
        storefrontCheckoutContractsSource.Should().Contain("public IReadOnlyList<StorefrontOrderConfirmationLine> Lines { get; set; } = Array.Empty<StorefrontOrderConfirmationLine>();");
        storefrontCheckoutContractsSource.Should().Contain("public IReadOnlyList<StorefrontOrderConfirmationPayment> Payments { get; set; } = Array.Empty<StorefrontOrderConfirmationPayment>();");

        customerProfileSource.Should().Contain("public sealed class CustomerProfile");
        customerProfileSource.Should().Contain("public Guid Id { get; init; }");
        customerProfileSource.Should().Contain("public bool PhoneNumberConfirmed { get; init; }");
        customerProfileSource.Should().Contain("public string? Currency { get; init; }");
        customerProfileSource.Should().Contain("public byte[]? RowVersion { get; init; }");

        memberAddressContractsSource.Should().Contain("public sealed class MemberAddress");
        memberAddressContractsSource.Should().Contain("public byte[] RowVersion { get; set; } = Array.Empty<byte>();");
        memberAddressContractsSource.Should().Contain("public string CountryCode { get; set; } = ProfileContractDefaults.DefaultCountryCode;");
        memberAddressContractsSource.Should().Contain("public sealed class CreateMemberAddressRequest");
        memberAddressContractsSource.Should().Contain("public sealed class UpdateMemberAddressRequest");
        memberAddressContractsSource.Should().Contain("public sealed class DeleteMemberAddressRequest");
        memberAddressContractsSource.Should().Contain("public sealed class SetMemberDefaultAddressRequest");
        memberAddressContractsSource.Should().Contain("public sealed class LinkedCustomerProfile");
        memberAddressContractsSource.Should().Contain("public sealed class MemberCustomerContext");
        memberAddressContractsSource.Should().Contain("public IReadOnlyList<MemberCustomerSegment> Segments { get; set; } = Array.Empty<MemberCustomerSegment>();");
        memberAddressContractsSource.Should().Contain("public IReadOnlyList<MemberCustomerConsent> Consents { get; set; } = Array.Empty<MemberCustomerConsent>();");
        memberAddressContractsSource.Should().Contain("public IReadOnlyList<MemberCustomerInteraction> RecentInteractions { get; set; } = Array.Empty<MemberCustomerInteraction>();");

        memberPreferenceContractsSource.Should().Contain("public sealed class MemberPreferences");
        memberPreferenceContractsSource.Should().Contain("public bool AllowPromotionalPushNotifications { get; set; }");
        memberPreferenceContractsSource.Should().Contain("public bool AllowOptionalAnalyticsTracking { get; set; }");
        memberPreferenceContractsSource.Should().Contain("public DateTime? AcceptsTermsAtUtc { get; set; }");
        memberPreferenceContractsSource.Should().Contain("public sealed class UpdateMemberPreferencesRequest");

        phoneVerificationContractsSource.Should().Contain("public sealed class RequestPhoneVerificationRequest");
        phoneVerificationContractsSource.Should().Contain("public string? Channel { get; init; }");
        phoneVerificationContractsSource.Should().Contain("public sealed class ConfirmPhoneVerificationRequest");
        phoneVerificationContractsSource.Should().Contain("public string? Code { get; init; }");

        profileDefaultsSource.Should().Contain("public static class ProfileContractDefaults");
        profileDefaultsSource.Should().Contain("public const string DefaultLocale = ContractDefaults.DefaultLocale;");
        profileDefaultsSource.Should().Contain("public const string DefaultTimezone = ContractDefaults.DefaultTimezone;");
        profileDefaultsSource.Should().Contain("public const string DefaultCurrency = ContractDefaults.DefaultCurrency;");
        profileDefaultsSource.Should().Contain("public const string DefaultCountryCode = ContractDefaults.DefaultCountryCode;");

        accountDeletionRequestSource.Should().Contain("public sealed record RequestAccountDeletionRequest(bool ConfirmIrreversibleDeletion);");
        accountDeletionRequestSource.Should().Contain("This request does not perform a hard delete.");
        accountDeletionRequestSource.Should().Contain("Indicates that the user explicitly confirmed the irreversible anonymization and deactivation workflow.");

        pushDeviceRequestSource.Should().Contain("public sealed class RegisterPushDeviceRequest");
        pushDeviceRequestSource.Should().Contain("public string DeviceId { get; init; } = string.Empty;");
        pushDeviceRequestSource.Should().Contain("public MobileDevicePlatform Platform { get; init; } = MobileDevicePlatform.Unknown;");
        pushDeviceRequestSource.Should().Contain("public string? PushToken { get; init; }");
        pushDeviceRequestSource.Should().Contain("public bool NotificationsEnabled { get; init; } = true;");
        pushDeviceRequestSource.Should().Contain("public string? AppVersion { get; init; }");
        pushDeviceRequestSource.Should().Contain("public string? DeviceModel { get; init; }");

        pushDeviceResponseSource.Should().Contain("public sealed class RegisterPushDeviceResponse");
        pushDeviceResponseSource.Should().Contain("public string DeviceId { get; init; } = string.Empty;");
        pushDeviceResponseSource.Should().Contain("public DateTime RegisteredAtUtc { get; init; }");

        mobilePlatformSource.Should().Contain("public enum MobileDevicePlatform : short");
        mobilePlatformSource.Should().Contain("Unknown = 0,");
        mobilePlatformSource.Should().Contain("Android = 1,");
        mobilePlatformSource.Should().Contain("iOS = 2");
    }


    [Fact]
    public void CatalogCmsIdentityAndInvoiceContracts_Should_KeepPublicAuthAndMemberFinanceShapeFloors()
    {
        var catalogContractsSource = ReadContractsFile(Path.Combine("Catalog", "PublicCatalogContracts.cs"));
        var cmsContractsSource = ReadContractsFile(Path.Combine("CMS", "PublicCmsContracts.cs"));
        var changePasswordSource = ReadContractsFile(Path.Combine("Identity", "ChangePasswordRequest.cs"));
        var confirmEmailSource = ReadContractsFile(Path.Combine("Identity", "ConfirmEmailRequest.cs"));
        var logoutSource = ReadContractsFile(Path.Combine("Identity", "LogoutRequest.cs"));
        var passwordLoginSource = ReadContractsFile(Path.Combine("Identity", "PasswordLoginRequest.cs"));
        var refreshTokenSource = ReadContractsFile(Path.Combine("Identity", "RefreshTokenRequest.cs"));
        var registerRequestSource = ReadContractsFile(Path.Combine("Identity", "RegisterRequest.cs"));
        var registerResponseSource = ReadContractsFile(Path.Combine("Identity", "RegisterResponse.cs"));
        var requestEmailConfirmationSource = ReadContractsFile(Path.Combine("Identity", "RequestEmailConfirmationRequest.cs"));
        var requestPasswordResetSource = ReadContractsFile(Path.Combine("Identity", "RequestPasswordResetRequest.cs"));
        var resetPasswordSource = ReadContractsFile(Path.Combine("Identity", "ResetPasswordRequest.cs"));
        var tokenResponseSource = ReadContractsFile(Path.Combine("Identity", "TokenResponse.cs"));
        var memberInvoiceContractsSource = ReadContractsFile(Path.Combine("Invoices", "MemberInvoiceContracts.cs"));

        catalogContractsSource.Should().Contain("public sealed class PublicCategorySummary");
        catalogContractsSource.Should().Contain("public Guid? ParentId { get; set; }");
        catalogContractsSource.Should().Contain("public string Slug { get; set; } = string.Empty;");
        catalogContractsSource.Should().Contain("public class PublicProductSummary");
        catalogContractsSource.Should().Contain("public string Currency { get; set; } = ContractDefaults.DefaultCurrency;");
        catalogContractsSource.Should().Contain("public long? CompareAtPriceMinor { get; set; }");
        catalogContractsSource.Should().Contain("public sealed class PublicProductDetail : PublicProductSummary");
        catalogContractsSource.Should().Contain("public IReadOnlyList<PublicProductVariant> Variants { get; set; } = Array.Empty<PublicProductVariant>();");
        catalogContractsSource.Should().Contain("public IReadOnlyList<PublicProductMedia> Media { get; set; } = Array.Empty<PublicProductMedia>();");
        catalogContractsSource.Should().Contain("public sealed class PublicProductVariant");
        catalogContractsSource.Should().Contain("public long BasePriceNetMinor { get; set; }");
        catalogContractsSource.Should().Contain("public bool BackorderAllowed { get; set; }");
        catalogContractsSource.Should().Contain("public bool IsDigital { get; set; }");
        catalogContractsSource.Should().Contain("public sealed class PublicProductMedia");
        catalogContractsSource.Should().Contain("public string Url { get; set; } = string.Empty;");
        catalogContractsSource.Should().Contain("public string Alt { get; set; } = string.Empty;");

        cmsContractsSource.Should().Contain("namespace Darwin.Contracts.Cms;");
        cmsContractsSource.Should().Contain("public class PublicPageSummary");
        cmsContractsSource.Should().Contain("public string Title { get; set; } = string.Empty;");
        cmsContractsSource.Should().Contain("public sealed class PublicPageDetail : PublicPageSummary");
        cmsContractsSource.Should().Contain("public string ContentHtml { get; set; } = string.Empty;");
        cmsContractsSource.Should().Contain("public sealed class PublicMenu");
        cmsContractsSource.Should().Contain("public IReadOnlyList<PublicMenuItem> Items { get; set; } = Array.Empty<PublicMenuItem>();");
        cmsContractsSource.Should().Contain("public sealed class PublicMenuItem");
        cmsContractsSource.Should().Contain("public string Url { get; set; } = string.Empty;");
        cmsContractsSource.Should().Contain("public int SortOrder { get; set; }");

        changePasswordSource.Should().Contain("public sealed class ChangePasswordRequest");
        changePasswordSource.Should().Contain("public string CurrentPassword { get; init; } = default!;");
        changePasswordSource.Should().Contain("public string NewPassword { get; init; } = default!;");

        confirmEmailSource.Should().Contain("public sealed class ConfirmEmailRequest");
        confirmEmailSource.Should().Contain("public string Email { get; init; } = default!;");
        confirmEmailSource.Should().Contain("public string Token { get; init; } = default!;");

        logoutSource.Should().Contain("public sealed class LogoutRequest");
        logoutSource.Should().Contain("public string RefreshToken { get; init; } = default!;");

        passwordLoginSource.Should().Contain("public sealed class PasswordLoginRequest");
        passwordLoginSource.Should().Contain("public string Email { get; set; } = string.Empty;");
        passwordLoginSource.Should().Contain("public string Password { get; set; } = string.Empty;");
        passwordLoginSource.Should().Contain("public string? DeviceId { get; set; }");
        passwordLoginSource.Should().Contain("public Guid? BusinessId { get; set; }");

        refreshTokenSource.Should().Contain("public sealed class RefreshTokenRequest");
        refreshTokenSource.Should().Contain("public string RefreshToken { get; set; } = string.Empty;");
        refreshTokenSource.Should().Contain("public string? DeviceId { get; set; }");
        refreshTokenSource.Should().Contain("public Guid? BusinessId { get; set; }");

        registerRequestSource.Should().Contain("public sealed class RegisterRequest");
        registerRequestSource.Should().Contain("public string FirstName { get; init; } = default!;");
        registerRequestSource.Should().Contain("public string LastName { get; init; } = default!;");
        registerRequestSource.Should().Contain("public string Email { get; init; } = default!;");
        registerRequestSource.Should().Contain("public string Password { get; init; } = default!;");

        registerResponseSource.Should().Contain("public sealed class RegisterResponse");
        registerResponseSource.Should().Contain("public string DisplayName { get; init; } = default!;");
        registerResponseSource.Should().Contain("public bool ConfirmationEmailSent { get; init; }");

        requestEmailConfirmationSource.Should().Contain("public sealed class RequestEmailConfirmationRequest");
        requestEmailConfirmationSource.Should().Contain("public string Email { get; init; } = default!;");

        requestPasswordResetSource.Should().Contain("public sealed class RequestPasswordResetRequest");
        requestPasswordResetSource.Should().Contain("public string Email { get; init; } = default!;");

        resetPasswordSource.Should().Contain("public sealed class ResetPasswordRequest");
        resetPasswordSource.Should().Contain("public string Email { get; init; } = default!;");
        resetPasswordSource.Should().Contain("public string Token { get; init; } = default!;");
        resetPasswordSource.Should().Contain("public string NewPassword { get; init; } = default!;");

        tokenResponseSource.Should().Contain("public sealed class TokenResponse");
        tokenResponseSource.Should().Contain("public string AccessToken { get; set; } = string.Empty;");
        tokenResponseSource.Should().Contain("public DateTime AccessTokenExpiresAtUtc { get; set; }");
        tokenResponseSource.Should().Contain("public string RefreshToken { get; set; } = string.Empty;");
        tokenResponseSource.Should().Contain("public DateTime RefreshTokenExpiresAtUtc { get; set; }");
        tokenResponseSource.Should().Contain("public Guid UserId { get; set; }");
        tokenResponseSource.Should().Contain("public string Email { get; set; } = string.Empty;");
        tokenResponseSource.Should().Contain("public IReadOnlyList<string>? Scopes { get; set; }");

        memberInvoiceContractsSource.Should().Contain("public class MemberInvoiceSummary");
        memberInvoiceContractsSource.Should().Contain("public Guid? BusinessId { get; set; }");
        memberInvoiceContractsSource.Should().Contain("public string Currency { get; set; } = ContractDefaults.DefaultCurrency;");
        memberInvoiceContractsSource.Should().Contain("public long BalanceMinor { get; set; }");
        memberInvoiceContractsSource.Should().Contain("public DateTime? PaidAtUtc { get; set; }");
        memberInvoiceContractsSource.Should().Contain("public sealed class MemberInvoiceDetail : MemberInvoiceSummary");
        memberInvoiceContractsSource.Should().Contain("public string PaymentSummary { get; set; } = string.Empty;");
        memberInvoiceContractsSource.Should().Contain("public IReadOnlyList<MemberInvoiceLine> Lines { get; set; } = Array.Empty<MemberInvoiceLine>();");
        memberInvoiceContractsSource.Should().Contain("public MemberInvoiceActions Actions { get; set; } = new();");
        memberInvoiceContractsSource.Should().Contain("public sealed class MemberInvoiceLine");
        memberInvoiceContractsSource.Should().Contain("public decimal TaxRate { get; set; }");
        memberInvoiceContractsSource.Should().Contain("public sealed class MemberInvoiceActions");
        memberInvoiceContractsSource.Should().Contain("public string? PaymentIntentPath { get; set; }");
        memberInvoiceContractsSource.Should().Contain("public string? OrderPath { get; set; }");
        memberInvoiceContractsSource.Should().Contain("public string DocumentPath { get; set; } = string.Empty;");
    }


    [Fact]
    public void ContractsProjectFile_Should_KeepTargetFrameworkNullableAndSanitizerPackageBaseline()
    {
        var contractsProjectSource = ReadContractsFile("Darwin.Contracts.csproj");

        contractsProjectSource.Should().Contain("<Project Sdk=\"Microsoft.NET.Sdk\">");
        contractsProjectSource.Should().Contain("<TargetFramework>net10.0</TargetFramework>");
        contractsProjectSource.Should().Contain("<ImplicitUsings>enable</ImplicitUsings>");
        contractsProjectSource.Should().Contain("<Nullable>enable</Nullable>");
        contractsProjectSource.Should().Contain("<PackageReference Include=\"HtmlSanitizer\" Version=\"9.0.892\" />");
    }


    [Fact]
    public void WebApiRuntimeConfigAndProjectFile_Should_KeepConfigPackageAndLaunchBaselineWired()
    {
        var appSettingsSource = ReadWebApiFile("appsettings.json");
        var developmentAppSettingsSource = ReadWebApiFile("appsettings.Development.json");
        var launchSettingsSource = ReadWebApiFile(Path.Combine("Properties", "launchSettings.json"));
        var webApiProjectSource = ReadWebApiFile("Darwin.WebApi.csproj");

        appSettingsSource.Should().Contain("\"AllowedHosts\": \"*\"");
        appSettingsSource.Should().Contain("\"Serilog\"");
        appSettingsSource.Should().Contain("\"Serilog.Sinks.Console\"");
        appSettingsSource.Should().Contain("\"Serilog.Sinks.File\"");
        appSettingsSource.Should().Contain("\"path\": \"logs/api-log-.txt\"");
        appSettingsSource.Should().Contain("\"retainedFileCountLimit\": 14");
        appSettingsSource.Should().Contain("\"buffered\": true");
        appSettingsSource.Should().Contain("\"Application\": \"Darwin.WebApi\"");
        appSettingsSource.Should().Contain("\"DataProtection\"");
        appSettingsSource.Should().Contain("\"KeysPath\": \"D:\\\\Darwin\\\\_shared_keys\"");
        appSettingsSource.Should().Contain("\"Jwt\"");
        appSettingsSource.Should().Contain("\"Issuer\": \"Darwin.WebApi\"");
        appSettingsSource.Should().Contain("\"Audience\": \"Darwin.MobileClients\"");
        appSettingsSource.Should().Contain("\"ClockSkewSeconds\": 60");
        appSettingsSource.Should().Contain("\"StorefrontCheckout\"");
        appSettingsSource.Should().Contain("\"FrontOfficeBaseUrl\": \"https://storefront.example.com\"");
        appSettingsSource.Should().Contain("\"StripeCheckoutBaseUrl\": \"https://storefront.example.com/mock-checkout\"");
        appSettingsSource.Should().Contain("\"InactiveReminderWorker\"");
        appSettingsSource.Should().Contain("\"MaxItemsPerRun\": 200");
        appSettingsSource.Should().Contain("\"Notifications\"");
        appSettingsSource.Should().Contain("\"InactiveReminderPushGateway\"");
        appSettingsSource.Should().Contain("\"PushProviders\"");
        appSettingsSource.Should().Contain("\"Fcm\"");
        appSettingsSource.Should().Contain("\"Apns\"");

        developmentAppSettingsSource.Should().Contain("\"ConnectionStrings\"");
        developmentAppSettingsSource.Should().Contain("\"DefaultConnection\"");
        developmentAppSettingsSource.Should().Contain("\"Provider\": \"Mailgun\"");
        developmentAppSettingsSource.Should().Contain("\"Mailgun\"");
        developmentAppSettingsSource.Should().Contain("\"Graph\"");
        developmentAppSettingsSource.Should().Contain("\"Issuer\": \"Darwin.WebApi.Dev\"");
        developmentAppSettingsSource.Should().Contain("\"Audience\": \"Darwin.MobileClients.Dev\"");
        developmentAppSettingsSource.Should().Contain("\"FrontOfficeBaseUrl\": \"http://localhost:3000\"");
        developmentAppSettingsSource.Should().Contain("\"StripeCheckoutBaseUrl\": \"http://localhost:3000/mock-checkout\"");
        developmentAppSettingsSource.Should().Contain("\"Serilog\"");
        developmentAppSettingsSource.Should().Contain("\"Default\": \"Debug\"");
        developmentAppSettingsSource.Should().Contain("\"path\": \"logs/api-dev-log-.txt\"");
        developmentAppSettingsSource.Should().Contain("\"retainedFileCountLimit\": 7");
        developmentAppSettingsSource.Should().Contain("\"buffered\": false");
        developmentAppSettingsSource.Should().Contain("\"Application\": \"Darwin.WebApi (Dev)\"");
        developmentAppSettingsSource.Should().Contain("\"KeysPath\": \"E:\\\\_Projects\\\\Darwin\\\\_shared_keys\"");

        launchSettingsSource.Should().Contain("\"$schema\": \"https://json.schemastore.org/launchsettings.json\"");
        launchSettingsSource.Should().Contain("\"http\"");
        launchSettingsSource.Should().Contain("\"https\"");
        launchSettingsSource.Should().Contain("\"commandName\": \"Project\"");
        launchSettingsSource.Should().Contain("\"dotnetRunMessages\": true");
        launchSettingsSource.Should().Contain("\"launchBrowser\": false");
        launchSettingsSource.Should().Contain("\"applicationUrl\": \"http://localhost:5134\"");
        launchSettingsSource.Should().Contain("\"applicationUrl\": \"https://localhost:7090;http://localhost:5134\"");
        launchSettingsSource.Should().Contain("\"ASPNETCORE_ENVIRONMENT\": \"Development\"");

        webApiProjectSource.Should().Contain("<Project Sdk=\"Microsoft.NET.Sdk.Web\">");
        webApiProjectSource.Should().Contain("<TargetFramework>net10.0</TargetFramework>");
        webApiProjectSource.Should().Contain("<Nullable>enable</Nullable>");
        webApiProjectSource.Should().Contain("<ImplicitUsings>enable</ImplicitUsings>");
        webApiProjectSource.Should().Contain("<UserSecretsId>");
        webApiProjectSource.Should().Contain("<PackageReference Include=\"AutoMapper\" Version=\"16.1.1\" />");
        webApiProjectSource.Should().Contain("<PackageReference Include=\"HtmlSanitizer\" Version=\"9.0.892\" />");
        webApiProjectSource.Should().Contain("<PackageReference Include=\"Microsoft.AspNetCore.Authentication.JwtBearer\" Version=\"10.0.6\" />");
        webApiProjectSource.Should().Contain("<PackageReference Include=\"Microsoft.AspNetCore.OpenApi\" Version=\"10.0.6\" />");
        webApiProjectSource.Should().Contain("<PackageReference Include=\"Microsoft.Bcl.Memory\" Version=\"10.0.6\" />");
        webApiProjectSource.Should().Contain("<PackageReference Include=\"Serilog\" Version=\"4.3.1\" />");
        webApiProjectSource.Should().Contain("<PackageReference Include=\"Swashbuckle.AspNetCore.SwaggerGen\" Version=\"10.1.7\" />");
        webApiProjectSource.Should().Contain("<PackageReference Include=\"Swashbuckle.AspNetCore.SwaggerUI\" Version=\"10.1.7\" />");
        webApiProjectSource.Should().Contain("<ProjectReference Include=\"..\\Darwin.Application\\Darwin.Application.csproj\" />");
        webApiProjectSource.Should().Contain("<ProjectReference Include=\"..\\Darwin.Contracts\\Darwin.Contracts.csproj\" />");
        webApiProjectSource.Should().Contain("<ProjectReference Include=\"..\\Darwin.Infrastructure\\Darwin.Infrastructure.csproj\" />");
    }


    [Fact]
    public void ApplicationAndInfrastructureProjectFiles_Should_KeepPackageReferenceAndProjectBaselineWired()
    {
        var applicationProjectSource = ReadApplicationFile("Darwin.Application.csproj");
        var infrastructureProjectSource = ReadInfrastructureFile("Darwin.Infrastructure.csproj");

        applicationProjectSource.Should().Contain("<Project Sdk=\"Microsoft.NET.Sdk\">");
        applicationProjectSource.Should().Contain("<TargetFramework>net10.0</TargetFramework>");
        applicationProjectSource.Should().Contain("<ImplicitUsings>enable</ImplicitUsings>");
        applicationProjectSource.Should().Contain("<Nullable>enable</Nullable>");
        applicationProjectSource.Should().Contain("<PackageReference Include=\"AutoMapper\" Version=\"16.1.1\" />");
        applicationProjectSource.Should().Contain("<PackageReference Include=\"FluentValidation\" Version=\"12.1.1\" />");
        applicationProjectSource.Should().Contain("<PackageReference Include=\"FluentValidation.DependencyInjectionExtensions\" Version=\"12.1.1\" />");
        applicationProjectSource.Should().Contain("<PackageReference Include=\"HtmlSanitizer\" Version=\"9.0.892\" />");
        applicationProjectSource.Should().Contain("<PackageReference Include=\"Microsoft.EntityFrameworkCore\" Version=\"10.0.6\" />");
        applicationProjectSource.Should().Contain("<PackageReference Include=\"Microsoft.Extensions.Localization.Abstractions\" Version=\"10.0.6\" />");
        applicationProjectSource.Should().Contain("<ProjectReference Include=\"..\\Darwin.Domain\\Darwin.Domain.csproj\" />");
        applicationProjectSource.Should().Contain("<ProjectReference Include=\"..\\Darwin.Shared\\Darwin.Shared.csproj\" />");
        applicationProjectSource.Should().Contain("<Folder Include=\"Meta\\Commands\\\" />");
        applicationProjectSource.Should().Contain("<Folder Include=\"Meta\\Validators\\\" />");

        infrastructureProjectSource.Should().Contain("<Project Sdk=\"Microsoft.NET.Sdk\">");
        infrastructureProjectSource.Should().Contain("<TargetFramework>net10.0</TargetFramework>");
        infrastructureProjectSource.Should().Contain("<ImplicitUsings>enable</ImplicitUsings>");
        infrastructureProjectSource.Should().Contain("<Nullable>enable</Nullable>");
        infrastructureProjectSource.Should().Contain("<Compile Remove=\"Persistence\\Db\\DarwinDbContext.Loyalty.cs\" />");
        infrastructureProjectSource.Should().Contain("<None Include=\"Persistence\\Db\\DarwinDbContext.Loyalty.cs\" />");
        infrastructureProjectSource.Should().Contain("<PackageReference Include=\"AutoMapper\" Version=\"16.1.1\" />");
        infrastructureProjectSource.Should().Contain("<PackageReference Include=\"Fido2\" Version=\"4.0.1\" />");
        infrastructureProjectSource.Should().Contain("<PackageReference Include=\"Fido2.AspNet\" Version=\"4.0.1\" />");
        infrastructureProjectSource.Should().Contain("<PackageReference Include=\"Fido2.Models\" Version=\"4.0.1\" />");
        infrastructureProjectSource.Should().Contain("<PackageReference Include=\"HtmlSanitizer\" Version=\"9.0.892\" />");
        infrastructureProjectSource.Should().Contain("<PackageReference Include=\"Isopoh.Cryptography.Argon2\" Version=\"2.0.0\" />");
        infrastructureProjectSource.Should().Contain("<PackageReference Include=\"Microsoft.Bcl.Memory\" Version=\"10.0.6\" />");
        infrastructureProjectSource.Should().Contain("<PackageReference Include=\"Microsoft.EntityFrameworkCore.Design\" Version=\"10.0.6\">");
        infrastructureProjectSource.Should().Contain("<PrivateAssets>all</PrivateAssets>");
        infrastructureProjectSource.Should().Contain("<PackageReference Include=\"Microsoft.EntityFrameworkCore.Relational\" Version=\"10.0.6\" />");
        infrastructureProjectSource.Should().Contain("<PackageReference Include=\"Microsoft.EntityFrameworkCore.SqlServer\" Version=\"10.0.6\" />");
        infrastructureProjectSource.Should().Contain("<PackageReference Include=\"Microsoft.EntityFrameworkCore.Tools\" Version=\"10.0.6\">");
        infrastructureProjectSource.Should().Contain("<PackageReference Include=\"Microsoft.Extensions.Configuration\" Version=\"10.0.6\" />");
        infrastructureProjectSource.Should().Contain("<PackageReference Include=\"Microsoft.Extensions.Configuration.EnvironmentVariables\" Version=\"10.0.6\" />");
        infrastructureProjectSource.Should().Contain("<PackageReference Include=\"Microsoft.Extensions.Configuration.FileExtensions\" Version=\"10.0.6\" />");
        infrastructureProjectSource.Should().Contain("<PackageReference Include=\"Microsoft.Extensions.Configuration.Json\" Version=\"10.0.6\" />");
        infrastructureProjectSource.Should().Contain("<ProjectReference Include=\"..\\Darwin.Application\\Darwin.Application.csproj\" />");
        infrastructureProjectSource.Should().Contain("<ProjectReference Include=\"..\\Darwin.Domain\\Darwin.Domain.csproj\" />");
        infrastructureProjectSource.Should().Contain("<ProjectReference Include=\"..\\Darwin.Shared\\Darwin.Shared.csproj\" />");
    }


    [Fact]
    public void DomainAndSharedProjectFiles_Should_KeepMinimalPackageBaselineWired()
    {
        var domainProjectSource = ReadDomainFile("Darwin.Domain.csproj");
        var sharedProjectSource = ReadApplicationFile(Path.Combine("..", "Darwin.Shared", "Darwin.Shared.csproj"));

        domainProjectSource.Should().Contain("<Project Sdk=\"Microsoft.NET.Sdk\">");
        domainProjectSource.Should().Contain("<TargetFramework>net10.0</TargetFramework>");
        domainProjectSource.Should().Contain("<ImplicitUsings>enable</ImplicitUsings>");
        domainProjectSource.Should().Contain("<Nullable>enable</Nullable>");
        domainProjectSource.Should().Contain("<PackageReference Include=\"HtmlSanitizer\" Version=\"9.0.892\" />");

        sharedProjectSource.Should().Contain("<Project Sdk=\"Microsoft.NET.Sdk\">");
        sharedProjectSource.Should().Contain("<TargetFramework>net10.0</TargetFramework>");
        sharedProjectSource.Should().Contain("<ImplicitUsings>enable</ImplicitUsings>");
        sharedProjectSource.Should().Contain("<Nullable>enable</Nullable>");
        sharedProjectSource.Should().Contain("<PackageReference Include=\"HtmlSanitizer\" Version=\"9.0.892\" />");
    }


    [Fact]
    public void WorkerRuntimeConfigAndProjectFile_Should_KeepHostedBaselineWired()
    {
        var workerProjectSource = ReadWorkerFile("Darwin.Worker.csproj");
        var appSettingsSource = ReadWorkerFile("appsettings.json");
        var developmentAppSettingsSource = ReadWorkerFile("appsettings.Development.json");
        var launchSettingsSource = ReadWorkerFile(Path.Combine("Properties", "launchSettings.json"));

        workerProjectSource.Should().Contain("<Project Sdk=\"Microsoft.NET.Sdk.Worker\">");
        workerProjectSource.Should().Contain("<TargetFramework>net10.0</TargetFramework>");
        workerProjectSource.Should().Contain("<Nullable>enable</Nullable>");
        workerProjectSource.Should().Contain("<ImplicitUsings>enable</ImplicitUsings>");
        workerProjectSource.Should().Contain("<UserSecretsId>");
        workerProjectSource.Should().Contain("<PackageReference Include=\"AutoMapper\" Version=\"16.1.1\" />");
        workerProjectSource.Should().Contain("<PackageReference Include=\"HtmlSanitizer\" Version=\"9.0.892\" />");
        workerProjectSource.Should().Contain("<PackageReference Include=\"Microsoft.Bcl.Memory\" Version=\"10.0.6\" />");
        workerProjectSource.Should().Contain("<PackageReference Include=\"Microsoft.Extensions.Configuration.Json\" Version=\"10.0.6\" />");
        workerProjectSource.Should().Contain("<PackageReference Include=\"Microsoft.Extensions.Hosting\" Version=\"10.0.6\" />");
        workerProjectSource.Should().Contain("<PackageReference Include=\"Microsoft.Extensions.Http\" Version=\"10.0.6\" />");
        workerProjectSource.Should().Contain("<PackageReference Include=\"Microsoft.Extensions.Logging.Debug\" Version=\"10.0.6\" />");
        workerProjectSource.Should().Contain("<ProjectReference Include=\"..\\Darwin.Application\\Darwin.Application.csproj\" />");
        workerProjectSource.Should().Contain("<ProjectReference Include=\"..\\Darwin.Contracts\\Darwin.Contracts.csproj\" />");
        workerProjectSource.Should().Contain("<ProjectReference Include=\"..\\Darwin.Infrastructure\\Darwin.Infrastructure.csproj\" />");
        workerProjectSource.Should().Contain("<ProjectReference Include=\"..\\Darwin.Shared\\Darwin.Shared.csproj\" />");

        appSettingsSource.Should().Contain("\"Logging\"");
        appSettingsSource.Should().Contain("\"LogLevel\"");
        appSettingsSource.Should().Contain("\"Default\": \"Information\"");
        appSettingsSource.Should().Contain("\"Microsoft.Hosting.Lifetime\": \"Information\"");
        appSettingsSource.Should().Contain("\"WebhookDeliveryWorker\"");
        appSettingsSource.Should().Contain("\"Enabled\": true");
        appSettingsSource.Should().Contain("\"PollIntervalSeconds\": 30");
        appSettingsSource.Should().Contain("\"BatchSize\": 10");
        appSettingsSource.Should().Contain("\"RequestTimeoutSeconds\": 15");
        appSettingsSource.Should().Contain("\"RetryCooldownSeconds\": 60");
        appSettingsSource.Should().Contain("\"MaxAttempts\": 5");
        appSettingsSource.Should().Contain("\"ProviderCallbackWorker\"");
        appSettingsSource.Should().Contain("\"PollIntervalSeconds\": 15");
        appSettingsSource.Should().Contain("\"BatchSize\": 20");
        appSettingsSource.Should().Contain("\"InactiveReminderWorker\"");
        appSettingsSource.Should().Contain("\"MaxItemsPerRun\": 200");
        appSettingsSource.Should().Contain("\"Notifications\"");
        appSettingsSource.Should().Contain("\"InactiveReminderPushGateway\"");

        developmentAppSettingsSource.Should().Contain("\"Logging\"");
        developmentAppSettingsSource.Should().Contain("\"LogLevel\"");
        developmentAppSettingsSource.Should().Contain("\"Default\": \"Information\"");
        developmentAppSettingsSource.Should().Contain("\"Microsoft.Hosting.Lifetime\": \"Information\"");
        developmentAppSettingsSource.Should().Contain("\"WebhookDeliveryWorker\"");
        developmentAppSettingsSource.Should().Contain("\"PollIntervalSeconds\": 15");
        developmentAppSettingsSource.Should().Contain("\"RetryCooldownSeconds\": 30");
        developmentAppSettingsSource.Should().Contain("\"ProviderCallbackWorker\"");
        developmentAppSettingsSource.Should().Contain("\"PollIntervalSeconds\": 10");
        developmentAppSettingsSource.Should().Contain("\"RetryCooldownSeconds\": 15");
        developmentAppSettingsSource.Should().Contain("\"InactiveReminderWorker\"");
        developmentAppSettingsSource.Should().Contain("\"InactiveReminderPushGateway\"");

        launchSettingsSource.Should().Contain("\"$schema\": \"https://json.schemastore.org/launchsettings.json\"");
        launchSettingsSource.Should().Contain("\"Darwin.Worker\"");
        launchSettingsSource.Should().Contain("\"commandName\": \"Project\"");
        launchSettingsSource.Should().Contain("\"dotnetRunMessages\": true");
        launchSettingsSource.Should().Contain("\"DOTNET_ENVIRONMENT\": \"Development\"");
    }


    [Fact]
    public void WorkerProgramAndWebhookDispatchDaemon_Should_KeepBackgroundWebhookDeliveryWired()
    {
        var programSource = ReadWorkerFile("Program.cs");
        var workerSource = ReadWorkerFile("Worker.cs");
        var optionsSource = ReadWorkerFile("WebhookDeliveryWorkerOptions.cs");
        var inactiveReminderServiceSource = ReadWorkerFile("InactiveReminderBackgroundService.cs");
        var inactiveReminderOptionsSource = ReadWorkerFile("InactiveReminderWorkerOptions.cs");
        var providerCallbackWorkerSource = ReadWorkerFile("ProviderCallbackBackgroundService.cs");
        var providerCallbackOptionsSource = ReadWorkerFile("ProviderCallbackWorkerOptions.cs");
        var providerCallbackEntitySource = ReadDomainFile(Path.Combine("Entities", "Integration", "ProviderCallbackInboxMessage.cs"));
        var providerCallbackConfigSource = ReadInfrastructureFile(Path.Combine("Persistence", "Configurations", "Integration", "ProviderCallbackInboxMessageConfiguration.cs"));
        var shipmentProviderOperationWorkerSource = ReadWorkerFile("ShipmentProviderOperationBackgroundService.cs");
        var shipmentProviderOperationOptionsSource = ReadWorkerFile("ShipmentProviderOperationWorkerOptions.cs");
        var shipmentProviderOperationEntitySource = ReadDomainFile(Path.Combine("Entities", "Integration", "ShipmentProviderOperation.cs"));
        var shipmentProviderOperationConfigSource = ReadInfrastructureFile(Path.Combine("Persistence", "Configurations", "Integration", "ShipmentProviderOperationConfiguration.cs"));
        var applyDhlShipmentCreateOperationSource = ReadApplicationFile(Path.Combine("Orders", "Commands", "ApplyDhlShipmentCreateOperationHandler.cs"));

        programSource.Should().Contain("builder.Services.AddApplication();");
        programSource.Should().Contain("builder.Services.AddConfiguredPersistence(builder.Configuration);");
        programSource.Should().Contain("builder.Services.AddNotificationsInfrastructure(builder.Configuration);");
        programSource.Should().Contain("builder.Services.AddHttpClient();");
        programSource.Should().Contain("builder.Services.Configure<InactiveReminderWorkerOptions>(builder.Configuration.GetSection(\"InactiveReminderWorker\"));");
        programSource.Should().Contain("builder.Services.Configure<ProviderCallbackWorkerOptions>(builder.Configuration.GetSection(\"ProviderCallbackWorker\"));");
        programSource.Should().Contain("builder.Services.Configure<ShipmentProviderOperationWorkerOptions>(builder.Configuration.GetSection(\"ShipmentProviderOperationWorker\"));");
        programSource.Should().Contain("builder.Services.Configure<WebhookDeliveryWorkerOptions>(builder.Configuration.GetSection(\"WebhookDeliveryWorker\"));");
        programSource.Should().Contain("builder.Services.Configure<InvoiceArchiveMaintenanceWorkerOptions>(builder.Configuration.GetSection(\"InvoiceArchiveMaintenanceWorker\"));");
        programSource.Should().Contain("builder.Services.AddScoped<ApplyDhlShipmentCreateOperationHandler>();");
        programSource.Should().Contain("builder.Services.AddScoped<ApplyDhlShipmentLabelOperationHandler>();");
        programSource.Should().Contain("builder.Services.AddScoped<PurgeExpiredInvoiceArchivesHandler>();");
        programSource.Should().Contain("builder.Services.AddHostedService<InactiveReminderBackgroundService>();");
        programSource.Should().Contain("builder.Services.AddHostedService<ProviderCallbackBackgroundService>();");
        programSource.Should().Contain("builder.Services.AddHostedService<ShipmentProviderOperationBackgroundService>();");
        programSource.Should().Contain("builder.Services.AddHostedService<WebhookDeliveryBackgroundService>();");
        programSource.Should().Contain("builder.Services.AddHostedService<InvoiceArchiveMaintenanceBackgroundService>();");

        optionsSource.Should().Contain("public sealed class WebhookDeliveryWorkerOptions");
        optionsSource.Should().Contain("public bool Enabled { get; set; } = true;");
        optionsSource.Should().Contain("public int PollIntervalSeconds { get; set; } = 30;");
        optionsSource.Should().Contain("public int BatchSize { get; set; } = 10;");
        optionsSource.Should().Contain("public int RequestTimeoutSeconds { get; set; } = 15;");
        optionsSource.Should().Contain("public int RetryCooldownSeconds { get; set; } = 60;");
        optionsSource.Should().Contain("public int MaxAttempts { get; set; } = 5;");

        workerSource.Should().Contain("public sealed class WebhookDeliveryBackgroundService : BackgroundService");
        workerSource.Should().Contain("private readonly IServiceScopeFactory _scopeFactory;");
        workerSource.Should().Contain("private readonly IHttpClientFactory _httpClientFactory;");
        workerSource.Should().Contain("private readonly IOptions<WebhookDeliveryWorkerOptions> _options;");
        workerSource.Should().Contain("Set<WebhookDelivery>()");
        workerSource.Should().Contain("Set<WebhookSubscription>()");
        workerSource.Should().Contain("Set<EventLog>()");
        workerSource.Should().Contain("x.Status == \"Pending\" || x.Status == \"Failed\"");
        workerSource.Should().Contain("x.RetryCount < options.MaxAttempts");
        workerSource.Should().Contain("X-Darwin-Signature");
        workerSource.Should().Contain("Idempotency-Key");
        workerSource.Should().Contain("ComputePayloadHash(payloadJson)");
        workerSource.Should().Contain("ComputeSignatureHeader(payloadJson, subscription.Secret)");
        workerSource.Should().Contain("delivery.Status = response.IsSuccessStatusCode ? \"Succeeded\" : \"Failed\";");

        inactiveReminderOptionsSource.Should().Contain("public sealed class InactiveReminderWorkerOptions");
        inactiveReminderOptionsSource.Should().Contain("public TimeSpan Interval { get; set; } = TimeSpan.FromHours(1);");
        inactiveReminderServiceSource.Should().Contain("public sealed class InactiveReminderBackgroundService : BackgroundService");
        inactiveReminderServiceSource.Should().Contain("GetRequiredService<ProcessInactiveReminderBatchHandler>()");
        inactiveReminderServiceSource.Should().Contain("DelaySafeAsync(options.Interval, stoppingToken)");

        providerCallbackOptionsSource.Should().Contain("public sealed class ProviderCallbackWorkerOptions");
        providerCallbackOptionsSource.Should().Contain("public bool Enabled { get; set; } = true;");
        providerCallbackOptionsSource.Should().Contain("public int PollIntervalSeconds { get; set; } = 15;");
        providerCallbackOptionsSource.Should().Contain("public int BatchSize { get; set; } = 20;");
        providerCallbackOptionsSource.Should().Contain("public int RetryCooldownSeconds { get; set; } = 30;");
        providerCallbackOptionsSource.Should().Contain("public int MaxAttempts { get; set; } = 10;");
        shipmentProviderOperationOptionsSource.Should().Contain("public sealed class ShipmentProviderOperationWorkerOptions");
        shipmentProviderOperationOptionsSource.Should().Contain("public int RetryCooldownSeconds { get; set; } = 30;");
        shipmentProviderOperationOptionsSource.Should().Contain("public int MaxAttempts { get; set; } = 10;");

        providerCallbackEntitySource.Should().Contain("public sealed class ProviderCallbackInboxMessage : BaseEntity");
        providerCallbackEntitySource.Should().Contain("public string Provider { get; set; } = string.Empty;");
        providerCallbackEntitySource.Should().Contain("public string CallbackType { get; set; } = string.Empty;");
        providerCallbackEntitySource.Should().Contain("public string PayloadJson { get; set; } = string.Empty;");
        providerCallbackEntitySource.Should().Contain("public string? IdempotencyKey { get; set; }");
        providerCallbackEntitySource.Should().Contain("public string Status { get; set; } = \"Pending\";");
        shipmentProviderOperationEntitySource.Should().Contain("public sealed class ShipmentProviderOperation : BaseEntity");
        shipmentProviderOperationEntitySource.Should().Contain("public Guid ShipmentId { get; set; }");
        shipmentProviderOperationEntitySource.Should().Contain("public string OperationType { get; set; } = string.Empty;");
        shipmentProviderOperationEntitySource.Should().Contain("public string Status { get; set; } = \"Pending\";");

        providerCallbackConfigSource.Should().Contain("public sealed class ProviderCallbackInboxMessageConfiguration : IEntityTypeConfiguration<ProviderCallbackInboxMessage>");
        providerCallbackConfigSource.Should().Contain("builder.ToTable(\"ProviderCallbackInboxMessages\", schema: \"Integration\");");
        providerCallbackConfigSource.Should().Contain("builder.Property(x => x.IdempotencyKey).HasMaxLength(256);");
        providerCallbackConfigSource.Should().Contain("builder.HasIndex(x => new { x.Provider, x.Status, x.CreatedAtUtc });");
        providerCallbackConfigSource.Should().Contain("builder.HasIndex(x => new { x.Provider, x.IdempotencyKey })");
        providerCallbackConfigSource.Should().Contain("UX_ProviderCallbackInboxMessages_Provider_IdempotencyKey");
        shipmentProviderOperationConfigSource.Should().Contain("public sealed class ShipmentProviderOperationConfiguration : IEntityTypeConfiguration<ShipmentProviderOperation>");
        shipmentProviderOperationConfigSource.Should().Contain("builder.ToTable(\"ShipmentProviderOperations\", schema: \"Integration\");");
        shipmentProviderOperationConfigSource.Should().Contain("builder.HasIndex(x => new { x.ShipmentId, x.Provider, x.OperationType, x.Status, x.CreatedAtUtc });");

        providerCallbackWorkerSource.Should().Contain("public sealed class ProviderCallbackBackgroundService : BackgroundService");
        providerCallbackWorkerSource.Should().Contain("private readonly IOptions<ProviderCallbackWorkerOptions> _options;");
        providerCallbackWorkerSource.Should().Contain("db.Set<ProviderCallbackInboxMessage>()");
        providerCallbackWorkerSource.Should().Contain("x.Status == \"Pending\" || x.Status == \"Failed\"");
        providerCallbackWorkerSource.Should().Contain("x.AttemptCount < options.MaxAttempts");
        providerCallbackWorkerSource.Should().Contain("if (string.Equals(item.Provider, \"Stripe\", StringComparison.OrdinalIgnoreCase))");
        providerCallbackWorkerSource.Should().Contain("GetRequiredService<ProcessStripeWebhookHandler>()");
        providerCallbackWorkerSource.Should().Contain("if (string.Equals(item.Provider, \"DHL\", StringComparison.OrdinalIgnoreCase))");
        providerCallbackWorkerSource.Should().Contain("GetRequiredService<ApplyShipmentCarrierEventHandler>()");
        providerCallbackWorkerSource.Should().Contain("item.Status = \"Processed\";");
        providerCallbackWorkerSource.Should().Contain("item.Status = \"Failed\";");
        shipmentProviderOperationWorkerSource.Should().Contain("public sealed class ShipmentProviderOperationBackgroundService : BackgroundService");
        shipmentProviderOperationWorkerSource.Should().Contain("db.Set<ShipmentProviderOperation>()");
        shipmentProviderOperationWorkerSource.Should().Contain("x.Status == \"Pending\" || x.Status == \"Failed\"");
        shipmentProviderOperationWorkerSource.Should().Contain("x.AttemptCount < options.MaxAttempts");
        shipmentProviderOperationWorkerSource.Should().Contain("GetRequiredService<ApplyDhlShipmentCreateOperationHandler>()");
        shipmentProviderOperationWorkerSource.Should().Contain("GetRequiredService<ApplyDhlShipmentLabelOperationHandler>()");
        shipmentProviderOperationWorkerSource.Should().Contain("item.Status = \"Processed\";");
        shipmentProviderOperationWorkerSource.Should().Contain("item.Status = \"Failed\";");
        applyDhlShipmentCreateOperationSource.Should().Contain("public sealed class ApplyDhlShipmentCreateOperationHandler");
        applyDhlShipmentCreateOperationSource.Should().Contain("shipment.LastCarrierEventKey = \"shipment.provider_created\";");
        applyDhlShipmentCreateOperationSource.Should().Contain("OperationType == \"GenerateLabel\"");
        applyDhlShipmentCreateOperationSource.Should().Contain("queuedLabelOperationEntry = new ShipmentProviderOperation");
        applyDhlShipmentCreateOperationSource.Should().Contain("_db.Set<ShipmentProviderOperation>().Add(queuedLabelOperationEntry);");
    }


    [Fact]
    public void MobileBusinessAndConsumerProjectFiles_Should_KeepPackageAndConfigBaselinesWired()
    {
        var businessProjectSource = ReadMobileBusinessFile("Darwin.Mobile.Business.csproj");
        var businessAppSettingsSource = ReadMobileBusinessFile(Path.Combine("Resources", "Raw", "appsettings.mobile.json"));
        var businessDevelopmentAppSettingsSource = ReadMobileBusinessFile(Path.Combine("Resources", "Raw", "appsettings.mobile.Development.json"));
        var businessLaunchSettingsSource = ReadMobileBusinessFile(Path.Combine("Properties", "launchSettings.json"));

        var consumerProjectSource = ReadMobileConsumerFile("Darwin.Mobile.Consumer.csproj");
        var consumerAppSettingsSource = ReadMobileConsumerFile(Path.Combine("Resources", "Raw", "appsettings.mobile.json"));
        var consumerDevelopmentAppSettingsSource = ReadMobileConsumerFile(Path.Combine("Resources", "Raw", "appsettings.mobile.Development.json"));
        var consumerLaunchSettingsSource = ReadMobileConsumerFile(Path.Combine("Properties", "launchSettings.json"));

        businessProjectSource.Should().Contain("<TargetFrameworks>net10.0-android;net10.0-ios;net10.0-maccatalyst</TargetFrameworks>");
        businessProjectSource.Should().Contain("net10.0-windows10.0.19041.0");
        businessProjectSource.Should().Contain("<UseMaui>true</UseMaui>");
        businessProjectSource.Should().Contain("<SingleProject>true</SingleProject>");
        businessProjectSource.Should().Contain("<ImplicitUsings>enable</ImplicitUsings>");
        businessProjectSource.Should().Contain("<Nullable>enable</Nullable>");
        businessProjectSource.Should().Contain("<ApplicationTitle>Loyan Business</ApplicationTitle>");
        businessProjectSource.Should().Contain("<ApplicationId>com.loyan.darwin.mobile.business</ApplicationId>");
        businessProjectSource.Should().Contain("<DefaultLanguage>de-DE</DefaultLanguage>");
        businessProjectSource.Should().Contain("<PackageReference Include=\"CommunityToolkit.Maui\" Version=\"14.0.1\" />");
        businessProjectSource.Should().Contain("<PackageReference Include=\"CommunityToolkit.Mvvm\" Version=\"8.4.0\" />");
        businessProjectSource.Should().Contain("<PackageReference Include=\"HtmlSanitizer\" Version=\"9.0.892\" />");
        businessProjectSource.Should().Contain("<PackageReference Include=\"Microsoft.Extensions.Configuration.Json\" Version=\"10.0.6\" />");
        businessProjectSource.Should().Contain("<PackageReference Include=\"Microsoft.Maui.Controls\" Version=\"10.0.51\" />");
        businessProjectSource.Should().Contain("<PackageReference Include=\"QRCoder\" Version=\"1.8.0\" />");
        businessProjectSource.Should().Contain("<PackageReference Include=\"UraniumUI\" Version=\"2.14.0\" />");
        businessProjectSource.Should().Contain("<PackageReference Include=\"ZXing.Net.Maui\" Version=\"0.7.4\" />");
        businessProjectSource.Should().Contain("<ProjectReference Include=\"..\\Darwin.Contracts\\Darwin.Contracts.csproj\" />");
        businessProjectSource.Should().Contain("<ProjectReference Include=\"..\\Darwin.Mobile.Shared\\Darwin.Mobile.Shared.csproj\" />");
        businessProjectSource.Should().Contain("<MauiXaml Update=\"Views\\StaffAccessBadgePage.xaml\">");

        businessAppSettingsSource.Should().Contain("\"Api\"");
        businessAppSettingsSource.Should().Contain("\"BaseUrl\": \"https://jubilant-unstarched-cira.ngrok-free.dev\"");
        businessAppSettingsSource.Should().Contain("\"JwtAudience\": \"Darwin.PublicApi\"");
        businessAppSettingsSource.Should().Contain("\"QrRefreshSeconds\": 300");
        businessAppSettingsSource.Should().Contain("\"MaxOutbox\": 100");
        businessAppSettingsSource.Should().Contain("\"UnsafeTrustAnyServerCertificate\": false");
        businessAppSettingsSource.Should().Contain("\"EnableVerboseNetworkDiagnostics\": true");
        businessAppSettingsSource.Should().Contain("\"BusinessManagementWebsiteUrl\": \"https://www.loyan.de\"");
        businessAppSettingsSource.Should().Contain("\"LegalLinks\"");
        businessAppSettingsSource.Should().Contain("\"ImpressumUrl\": \"https://loyan.de/impressum\"");
        businessAppSettingsSource.Should().Contain("\"PrivacyPolicyUrl\": \"https://loyan.de/datenschutz\"");
        businessAppSettingsSource.Should().Contain("\"FailFastOnMissingRequiredLinks\": false");

        businessDevelopmentAppSettingsSource.Should().Contain("\"Api\"");
        businessDevelopmentAppSettingsSource.Should().Contain("\"BaseUrl\": \"https://jubilant-unstarched-cira.ngrok-free.dev\"");
        businessDevelopmentAppSettingsSource.Should().Contain("\"JwtAudience\": \"Darwin.PublicApi\"");
        businessDevelopmentAppSettingsSource.Should().Contain("\"QrRefreshSeconds\": 60");
        businessDevelopmentAppSettingsSource.Should().Contain("\"MaxOutbox\": 100");

        businessLaunchSettingsSource.Should().Contain("\"profiles\"");
        businessLaunchSettingsSource.Should().Contain("\"Windows Machine\"");
        businessLaunchSettingsSource.Should().Contain("\"commandName\": \"Project\"");
        businessLaunchSettingsSource.Should().Contain("\"nativeDebugging\": false");

        consumerProjectSource.Should().Contain("<TargetFrameworks>net10.0-android;net10.0-ios;net10.0-maccatalyst</TargetFrameworks>");
        consumerProjectSource.Should().Contain("net10.0-windows10.0.19041.0");
        consumerProjectSource.Should().Contain("<UseMaui>true</UseMaui>");
        consumerProjectSource.Should().Contain("<UseMauiMaps>true</UseMauiMaps>");
        consumerProjectSource.Should().Contain("<SingleProject>true</SingleProject>");
        consumerProjectSource.Should().Contain("<ImplicitUsings>enable</ImplicitUsings>");
        consumerProjectSource.Should().Contain("<Nullable>enable</Nullable>");
        consumerProjectSource.Should().Contain("<ApplicationTitle>Loyan</ApplicationTitle>");
        consumerProjectSource.Should().Contain("<ApplicationId>com.loyan.darwin.mobile.consumer</ApplicationId>");
        consumerProjectSource.Should().Contain("<DefaultLanguage>de-DE</DefaultLanguage>");
        consumerProjectSource.Should().Contain("<NoWarn>$(NoWarn);NU1608</NoWarn>");
        consumerProjectSource.Should().Contain("<GoogleMapsApiKey Condition=\"'$(GoogleMapsApiKey)' == ''\">$(GOOGLE_MAPS_API_KEY)</GoogleMapsApiKey>");
        consumerProjectSource.Should().Contain("<GoogleMapsApiKey Condition=\"'$(GoogleMapsApiKey)' == ''\">$(ANDROID_GOOGLE_MAPS_API_KEY)</GoogleMapsApiKey>");
        consumerProjectSource.Should().Contain("<AndroidManifestPlaceholders>$(AndroidManifestPlaceholders);googleMapsApiKey=$(GoogleMapsApiKey)</AndroidManifestPlaceholders>");
        consumerProjectSource.Should().Contain("<Target Name=\"ValidateAndroidPushFirebaseConfig\"");
        consumerProjectSource.Should().Contain("google-services.json is required for Android Release builds with FCM push integration.");
        consumerProjectSource.Should().Contain("<Target Name=\"ValidateAndroidGoogleMapsApiKey\"");
        consumerProjectSource.Should().Contain("GOOGLE_MAPS_API_KEY is required for Android Release builds.");
        consumerProjectSource.Should().Contain("<PackageReference Include=\"CommunityToolkit.Maui\" Version=\"14.0.1\" />");
        consumerProjectSource.Should().Contain("<PackageReference Include=\"CommunityToolkit.Mvvm\" Version=\"8.4.0\" />");
        consumerProjectSource.Should().Contain("<PackageReference Include=\"HtmlSanitizer\" Version=\"9.0.892\" />");
        consumerProjectSource.Should().Contain("<PackageReference Include=\"Microsoft.Maui.Controls.Maps\" Version=\"10.0.51\" />");
        consumerProjectSource.Should().Contain("<PackageReference Include=\"QRCoder\" Version=\"1.8.0\" />");
        consumerProjectSource.Should().Contain("<PackageReference Include=\"UraniumUI\" Version=\"2.15.0\" />");
        consumerProjectSource.Should().Contain("<PackageReference Include=\"UraniumUI.Material\" Version=\"2.15.0\" />");
        consumerProjectSource.Should().Contain("<PackageReference Include=\"ZXing.Net.Maui\" Version=\"0.7.4\" />");
        consumerProjectSource.Should().Contain("<PackageReference Include=\"Xamarin.Firebase.Messaging\" Version=\"125.0.1.2\" />");
        consumerProjectSource.Should().Contain("<GoogleServicesJson Include=\"google-services.json\" Condition=\"Exists('google-services.json')\" />");
        consumerProjectSource.Should().Contain("<ProjectReference Include=\"..\\Darwin.Contracts\\Darwin.Contracts.csproj\" />");
        consumerProjectSource.Should().Contain("<ProjectReference Include=\"..\\Darwin.Mobile.Shared\\Darwin.Mobile.Shared.csproj\" />");
        consumerProjectSource.Should().Contain("<MauiXaml Update=\"Views\\LegalHubPage.xaml\">");

        consumerAppSettingsSource.Should().Contain("\"Api\"");
        consumerAppSettingsSource.Should().Contain("\"BaseUrl\": \"https://jubilant-unstarched-cira.ngrok-free.dev\"");
        consumerAppSettingsSource.Should().Contain("\"JwtAudience\": \"Darwin.PublicApi\"");
        consumerAppSettingsSource.Should().Contain("\"QrRefreshSeconds\": 60");
        consumerAppSettingsSource.Should().Contain("\"MaxOutbox\": 100");
        consumerAppSettingsSource.Should().Contain("\"PushNotifications\"");
        consumerAppSettingsSource.Should().Contain("\"NotificationsEnabled\": true");
        consumerAppSettingsSource.Should().Contain("\"TestPushToken\": \"\"");
        consumerAppSettingsSource.Should().Contain("\"LegalLinks\"");
        consumerAppSettingsSource.Should().Contain("\"AccountDeletionUrl\": \"https://loyan.de/konto-loeschen\"");
        consumerAppSettingsSource.Should().Contain("\"FailFastOnMissingRequiredLinks\": false");

        consumerDevelopmentAppSettingsSource.Should().Contain("\"Api\"");
        consumerDevelopmentAppSettingsSource.Should().Contain("\"BaseUrl\": \"https://jubilant-unstarched-cira.ngrok-free.dev\"");
        consumerDevelopmentAppSettingsSource.Should().Contain("\"JwtAudience\": \"Darwin.PublicApi\"");
        consumerDevelopmentAppSettingsSource.Should().Contain("\"QrRefreshSeconds\": 60");
        consumerDevelopmentAppSettingsSource.Should().Contain("\"PushNotifications\"");
        consumerDevelopmentAppSettingsSource.Should().Contain("\"NotificationsEnabled\": true");
        consumerDevelopmentAppSettingsSource.Should().Contain("\"TestPushToken\": \"\"");

        consumerLaunchSettingsSource.Should().Contain("\"profiles\"");
        consumerLaunchSettingsSource.Should().Contain("\"Windows Machine\"");
        consumerLaunchSettingsSource.Should().Contain("\"commandName\": \"Project\"");
        consumerLaunchSettingsSource.Should().Contain("\"nativeDebugging\": false");
    }


    [Fact]
    public void MobileSharedAndTestProjectFiles_Should_KeepSharedRuntimeAndTestHarnessBaselinesWired()
    {
        var mobileSharedProjectSource = ReadMobileSharedFile("Darwin.Mobile.Shared.csproj");
        var contractsTestsProjectSource = ReadTestProjectFile(Path.Combine("Darwin.Contracts.Tests", "Darwin.Contracts.Tests.csproj"));
        var infrastructureTestsProjectSource = ReadTestProjectFile(Path.Combine("Darwin.Infrastructure.Tests", "Darwin.Infrastructure.Tests.csproj"));
        var mobileSharedTestsProjectSource = ReadTestProjectFile(Path.Combine("Darwin.Mobile.Shared.Tests", "Darwin.Mobile.Shared.Tests.csproj"));
        var testsCommonProjectSource = ReadTestProjectFile(Path.Combine("Darwin.Tests.Common", "Darwin.Tests.Common.csproj"));
        var integrationTestsProjectSource = ReadTestProjectFile(Path.Combine("Darwin.Tests.Integration", "Darwin.Tests.Integration.csproj"));
        var webApiTestsProjectSource = ReadTestProjectFile(Path.Combine("Darwin.WebApi.Tests", "Darwin.WebApi.Tests.csproj"));
        var unitTestsProjectSource = ReadTestProjectFile(Path.Combine("Darwin.Tests.Unit", "Darwin.Tests.Unit.csproj"));

        mobileSharedProjectSource.Should().Contain("<TargetFrameworks>net10.0-android;net10.0-ios;net10.0-maccatalyst</TargetFrameworks>");
        mobileSharedProjectSource.Should().Contain("net10.0-windows10.0.19041.0");
        mobileSharedProjectSource.Should().Contain("<UseMaui>true</UseMaui>");
        mobileSharedProjectSource.Should().Contain("<SingleProject>true</SingleProject>");
        mobileSharedProjectSource.Should().Contain("<ImplicitUsings>enable</ImplicitUsings>");
        mobileSharedProjectSource.Should().Contain("<Nullable>enable</Nullable>");
        mobileSharedProjectSource.Should().Contain("<NoWarn>$(NoWarn);NETSDK1206</NoWarn>");
        mobileSharedProjectSource.Should().Contain("<PackageReference Include=\"HtmlSanitizer\" Version=\"9.0.892\" />");
        mobileSharedProjectSource.Should().Contain("<PackageReference Include=\"Microsoft.Extensions.Http\" Version=\"10.0.6\" />");
        mobileSharedProjectSource.Should().Contain("<PackageReference Include=\"Microsoft.Maui.Controls\" Version=\"10.0.51\" />");
        mobileSharedProjectSource.Should().Contain("<PackageReference Include=\"sqlite-net-pcl\" Version=\"1.9.172\" />");
        mobileSharedProjectSource.Should().Contain("<PackageReference Include=\"System.IdentityModel.Tokens.Jwt\" Version=\"8.17.0\" />");
        mobileSharedProjectSource.Should().Contain("<Folder Include=\"Features\\\" />");
        mobileSharedProjectSource.Should().Contain("<Folder Include=\"Utils\\\" />");
        mobileSharedProjectSource.Should().Contain("<ProjectReference Include=\"..\\Darwin.Contracts\\Darwin.Contracts.csproj\" />");
        mobileSharedProjectSource.Should().Contain("<ProjectReference Include=\"..\\Darwin.Shared\\Darwin.Shared.csproj\" />");

        contractsTestsProjectSource.Should().Contain("<TargetFramework>net10.0</TargetFramework>");
        contractsTestsProjectSource.Should().Contain("<IsPackable>false</IsPackable>");
        contractsTestsProjectSource.Should().Contain("<PackageReference Include=\"coverlet.collector\" Version=\"8.0.1\">");
        contractsTestsProjectSource.Should().Contain("<PackageReference Include=\"FluentAssertions\" Version=\"8.9.0\" />");
        contractsTestsProjectSource.Should().Contain("<PackageReference Include=\"Microsoft.NET.Test.Sdk\" Version=\"18.4.0\" />");
        contractsTestsProjectSource.Should().Contain("<PackageReference Include=\"xunit.v3\" Version=\"3.2.2\" />");
        contractsTestsProjectSource.Should().Contain("<ProjectReference Include=\"..\\..\\src\\Darwin.Contracts\\Darwin.Contracts.csproj\" />");
        contractsTestsProjectSource.Should().Contain("<Using Include=\"Xunit\" />");

        infrastructureTestsProjectSource.Should().Contain("<PackageReference Include=\"AutoMapper\" Version=\"16.1.1\" />");
        infrastructureTestsProjectSource.Should().Contain("<PackageReference Include=\"Microsoft.Bcl.Memory\" Version=\"10.0.6\" />");
        infrastructureTestsProjectSource.Should().Contain("<ProjectReference Include=\"..\\..\\src\\Darwin.Infrastructure\\Darwin.Infrastructure.csproj\" />");

        mobileSharedTestsProjectSource.Should().Contain("<TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>");
        mobileSharedTestsProjectSource.Should().Contain("<RuntimeIdentifier>win-x64</RuntimeIdentifier>");
        mobileSharedTestsProjectSource.Should().Contain("<PlatformTarget>x64</PlatformTarget>");
        mobileSharedTestsProjectSource.Should().Contain("<PackageReference Include=\"Moq\" Version=\"4.20.72\" />");
        mobileSharedTestsProjectSource.Should().Contain("<PackageReference Include=\"Moq.Contrib.HttpClient\" Version=\"1.4.0\" />");
        mobileSharedTestsProjectSource.Should().Contain("<ProjectReference Include=\"..\\..\\src\\Darwin.Mobile.Shared\\Darwin.Mobile.Shared.csproj\" />");

        testsCommonProjectSource.Should().Contain("<PackageReference Include=\"AutoMapper\" Version=\"16.1.1\" />");
        testsCommonProjectSource.Should().Contain("<PackageReference Include=\"Microsoft.AspNetCore.Mvc.Testing\" Version=\"10.0.6\" />");
        testsCommonProjectSource.Should().Contain("<PackageReference Include=\"Microsoft.Extensions.Logging.Debug\" Version=\"10.0.6\" />");
        testsCommonProjectSource.Should().Contain("<ProjectReference Include=\"..\\..\\src\\Darwin.Contracts\\Darwin.Contracts.csproj\" />");
        testsCommonProjectSource.Should().Contain("<ProjectReference Include=\"..\\..\\src\\Darwin.Infrastructure\\Darwin.Infrastructure.csproj\" />");
        testsCommonProjectSource.Should().Contain("<ProjectReference Include=\"..\\..\\src\\Darwin.WebApi\\Darwin.WebApi.csproj\" />");

        integrationTestsProjectSource.Should().Contain("<PackageReference Include=\"AutoMapper\" Version=\"16.1.1\" />");
        integrationTestsProjectSource.Should().Contain("<PackageReference Include=\"HtmlSanitizer\" Version=\"9.0.892\" />");
        integrationTestsProjectSource.Should().Contain("<PackageReference Include=\"Microsoft.AspNetCore.Mvc.Testing\" Version=\"10.0.6\" />");
        integrationTestsProjectSource.Should().Contain("<PackageReference Include=\"Microsoft.Bcl.Memory\" Version=\"10.0.6\" />");
        integrationTestsProjectSource.Should().Contain("<ProjectReference Include=\"..\\..\\src\\Darwin.WebApi\\Darwin.WebApi.csproj\" />");
        integrationTestsProjectSource.Should().Contain("<ProjectReference Include=\"..\\Darwin.Tests.Common\\Darwin.Tests.Common.csproj\" />");

        webApiTestsProjectSource.Should().Contain("<PackageReference Include=\"AutoMapper\" Version=\"16.1.1\" />");
        webApiTestsProjectSource.Should().Contain("<PackageReference Include=\"Microsoft.Bcl.Memory\" Version=\"10.0.6\" />");
        webApiTestsProjectSource.Should().Contain("<PackageReference Include=\"Moq\" Version=\"4.20.72\" />");
        webApiTestsProjectSource.Should().Contain("<PackageReference Include=\"xunit.v3.runner.utility\" Version=\"3.2.2\" />");
        webApiTestsProjectSource.Should().Contain("<ProjectReference Include=\"..\\..\\src\\Darwin.Infrastructure\\Darwin.Infrastructure.csproj\" />");
        webApiTestsProjectSource.Should().Contain("<ProjectReference Include=\"..\\..\\src\\Darwin.WebApi\\Darwin.WebApi.csproj\" />");

        unitTestsProjectSource.Should().Contain("<PackageReference Include=\"AutoMapper\" Version=\"16.1.1\" />");
        unitTestsProjectSource.Should().Contain("<PackageReference Include=\"HtmlSanitizer\" Version=\"9.0.892\" />");
        unitTestsProjectSource.Should().Contain("<PackageReference Include=\"Microsoft.EntityFrameworkCore\" Version=\"10.0.6\" />");
        unitTestsProjectSource.Should().Contain("<PackageReference Include=\"Microsoft.EntityFrameworkCore.InMemory\" Version=\"10.0.6\" />");
        unitTestsProjectSource.Should().Contain("<PackageReference Include=\"Moq\" Version=\"4.20.72\" />");
        unitTestsProjectSource.Should().Contain("<ProjectReference Include=\"..\\..\\src\\Darwin.Application\\Darwin.Application.csproj\" />");
        unitTestsProjectSource.Should().Contain("<ProjectReference Include=\"..\\..\\src\\Darwin.Contracts\\Darwin.Contracts.csproj\" />");
        unitTestsProjectSource.Should().Contain("<ProjectReference Include=\"..\\..\\src\\Darwin.Domain\\Darwin.Domain.csproj\" />");
        unitTestsProjectSource.Should().Contain("<ProjectReference Include=\"..\\..\\src\\Darwin.Shared\\Darwin.Shared.csproj\" />");
    }

    [Fact]
    public void CommunicationCoreEmailFlows_Should_KeepTemplateDrivenLocalizedAndAuditedContractsWired()
    {
        var defaultsSource = ReadApplicationFile(Path.Combine("Communication", "CommunicationTemplateDefaults.cs"));
        var createInvitationSource = ReadApplicationFile(Path.Combine("Businesses", "Commands", "CreateBusinessInvitationHandler.cs"));
        var resendInvitationSource = ReadApplicationFile(Path.Combine("Businesses", "Commands", "ResendBusinessInvitationHandler.cs"));
        var emailConfirmationSource = ReadApplicationFile(Path.Combine("Identity", "Commands", "EmailConfirmationHandlers.cs"));
        var passwordResetSource = ReadApplicationFile(Path.Combine("Identity", "Commands", "RequestPasswordResetHandler.cs"));
        var smtpSenderSource = ReadInfrastructureFile(Path.Combine("Notifications", "Smtp", "SmtpEmailSender.cs"));
        var siteSettingsSeedSource = ReadInfrastructureFile(Path.Combine("Persistence", "Seed", "Sections", "SiteSettingsSeedSection.cs"));

        defaultsSource.Should().Contain("public const string LegacyBusinessInvitationSubjectTemplate");
        defaultsSource.Should().Contain("public const string LegacyAccountActivationSubjectTemplate");
        defaultsSource.Should().Contain("public const string LegacyPasswordResetSubjectTemplate");
        defaultsSource.Should().Contain("public static string ResolveTemplate(");
        defaultsSource.Should().Contain("return ResolveText(localizer, culture, resourceKey);");
        defaultsSource.Should().Contain("public static string ResolveText(");
        defaultsSource.Should().Contain("public static string? NormalizeCulture(string? culture, string? fallbackCulture = null)");

        createInvitationSource.Should().Contain("private readonly IStringLocalizer<CommunicationResource> _communicationLocalizer;");
        createInvitationSource.Should().Contain("var communicationCulture = CommunicationTemplateDefaults.NormalizeCulture(business.DefaultCulture, siteSettings?.DefaultCulture);");
        createInvitationSource.Should().Contain("siteSettings?.BusinessInvitationEmailSubjectTemplate,");
        createInvitationSource.Should().Contain("siteSettings?.BusinessInvitationEmailBodyTemplate,");
        createInvitationSource.Should().Contain("TransactionalEmailTemplateRenderer.Render(");
        createInvitationSource.Should().Contain("var recipient = string.IsNullOrWhiteSpace(siteSettings?.CommunicationTestInboxEmail) ? entity.Email : siteSettings.CommunicationTestInboxEmail!;");
        createInvitationSource.Should().Contain("body = ApplyRecipientOverrideNotice(_communicationLocalizer, communicationCulture, entity.Email, recipient, body);");
        createInvitationSource.Should().Contain("FlowKey = \"BusinessInvitation\",");
        createInvitationSource.Should().Contain("BusinessId = business.Id");

        resendInvitationSource.Should().Contain("private readonly IStringLocalizer<CommunicationResource> _communicationLocalizer;");
        resendInvitationSource.Should().Contain("var communicationCulture = CommunicationTemplateDefaults.NormalizeCulture(business.DefaultCulture, siteSettings?.DefaultCulture);");
        resendInvitationSource.Should().Contain("siteSettings?.BusinessInvitationEmailSubjectTemplate,");
        resendInvitationSource.Should().Contain("siteSettings?.BusinessInvitationEmailBodyTemplate,");
        resendInvitationSource.Should().Contain("var recipient = string.IsNullOrWhiteSpace(siteSettings?.CommunicationTestInboxEmail) ? invitation.Email : siteSettings.CommunicationTestInboxEmail!;");
        resendInvitationSource.Should().Contain("body = ApplyRecipientOverrideNotice(_communicationLocalizer, communicationCulture, invitation.Email, recipient, body);");
        resendInvitationSource.Should().Contain("FlowKey = \"BusinessInvitation\",");
        resendInvitationSource.Should().Contain("BusinessId = invitation.BusinessId");

        emailConfirmationSource.Should().Contain("private readonly IStringLocalizer<CommunicationResource> _communicationLocalizer;");
        emailConfirmationSource.Should().Contain("var communicationCulture = CommunicationTemplateDefaults.NormalizeCulture(user.Locale, siteSettings?.DefaultCulture);");
        emailConfirmationSource.Should().Contain("siteSettings?.AccountActivationEmailSubjectTemplate,");
        emailConfirmationSource.Should().Contain("siteSettings?.AccountActivationEmailBodyTemplate,");
        emailConfirmationSource.Should().Contain("var recipient = string.IsNullOrWhiteSpace(siteSettings?.CommunicationTestInboxEmail) ? user.Email : siteSettings.CommunicationTestInboxEmail!;");
        emailConfirmationSource.Should().Contain("body = ApplyRecipientOverrideNotice(_communicationLocalizer, communicationCulture, user.Email, recipient, body);");
        emailConfirmationSource.Should().Contain("FlowKey = \"AccountActivation\"");

        passwordResetSource.Should().Contain("private readonly IStringLocalizer<CommunicationResource> _communicationLocalizer;");
        passwordResetSource.Should().Contain("var communicationCulture = CommunicationTemplateDefaults.NormalizeCulture(user.Locale, siteSettings?.DefaultCulture);");
        passwordResetSource.Should().Contain("siteSettings?.PasswordResetEmailSubjectTemplate,");
        passwordResetSource.Should().Contain("siteSettings?.PasswordResetEmailBodyTemplate,");
        passwordResetSource.Should().Contain("var recipient = string.IsNullOrWhiteSpace(siteSettings?.CommunicationTestInboxEmail) ? user.Email : siteSettings.CommunicationTestInboxEmail!;");
        passwordResetSource.Should().Contain("body = ApplyRecipientOverrideNotice(_communicationLocalizer, communicationCulture, user.Email, recipient, body);");
        passwordResetSource.Should().Contain("FlowKey = \"PasswordReset\"");

        smtpSenderSource.Should().Contain("var audit = new EmailDispatchAudit");
        smtpSenderSource.Should().Contain("Provider = \"SMTP\",");
        smtpSenderSource.Should().Contain("FlowKey = string.IsNullOrWhiteSpace(context?.FlowKey) ? null : context.FlowKey.Trim(),");
        smtpSenderSource.Should().Contain("BusinessId = context?.BusinessId,");
        smtpSenderSource.Should().Contain("Status = \"Pending\",");
        smtpSenderSource.Should().Contain("audit.Status = \"Sent\";");
        smtpSenderSource.Should().Contain("audit.Status = \"Failed\";");
        smtpSenderSource.Should().Contain("audit.FailureMessage = NotificationLogSanitizer.TransportFailure(EmailProviderNames.Smtp);");

        siteSettingsSeedSource.Should().Contain("BusinessInvitationEmailSubjectTemplate = Darwin.Application.Communication.CommunicationTemplateDefaults.LegacyBusinessInvitationSubjectTemplate");
        siteSettingsSeedSource.Should().Contain("BusinessInvitationEmailBodyTemplate = Darwin.Application.Communication.CommunicationTemplateDefaults.LegacyBusinessInvitationBodyTemplate");
        siteSettingsSeedSource.Should().Contain("AccountActivationEmailSubjectTemplate = Darwin.Application.Communication.CommunicationTemplateDefaults.LegacyAccountActivationSubjectTemplate");
        siteSettingsSeedSource.Should().Contain("AccountActivationEmailBodyTemplate = Darwin.Application.Communication.CommunicationTemplateDefaults.LegacyAccountActivationBodyTemplate");
        siteSettingsSeedSource.Should().Contain("PasswordResetEmailSubjectTemplate = Darwin.Application.Communication.CommunicationTemplateDefaults.LegacyPasswordResetSubjectTemplate");
        siteSettingsSeedSource.Should().Contain("PasswordResetEmailBodyTemplate = Darwin.Application.Communication.CommunicationTemplateDefaults.LegacyPasswordResetBodyTemplate");
    }

    [Fact]
    public void BusinessOnboardingAndSettingsDomain_Should_KeepScopedLifecycleAndOwnershipContractsWired()
    {
        var businessEntitySource = ReadDomainFile(Path.Combine("Entities", "Businesses", "Business.cs"));
        var siteSettingEntitySource = ReadDomainFile(Path.Combine("Entities", "Settings", "SiteSetting.cs"));
        var createBusinessSource = ReadApplicationFile(Path.Combine("Businesses", "Commands", "CreateBusinessHandler.cs"));
        var updateBusinessSource = ReadApplicationFile(Path.Combine("Businesses", "Commands", "UpdateBusinessHandler.cs"));
        var lifecycleSource = ReadApplicationFile(Path.Combine("Businesses", "Commands", "BusinessLifecycleHandlers.cs"));
        var accessStateSource = ReadApplicationFile(Path.Combine("Businesses", "DTOs", "BusinessAccessDtos.cs"));
        var onboardingCustomerSource = ReadApplicationFile(Path.Combine("Businesses", "Support", "BusinessOnboardingCustomerProfileSupport.cs"));
        var businessValidatorsSource = ReadApplicationFile(Path.Combine("Businesses", "Validators", "BusinessValidators.cs"));
        var siteSettingDtoSource = ReadApplicationFile(Path.Combine("Settings", "DTOs", "SiteSettingDto.cs"));
        var getSiteSettingSource = ReadApplicationFile(Path.Combine("Settings", "Queries", "GetSiteSettingHandler.cs"));
        var updateSiteSettingSource = ReadApplicationFile(Path.Combine("Settings", "Commands", "UpdateSiteSettingHandler.cs"));
        var siteSettingValidatorSource = ReadApplicationFile(Path.Combine("Settings", "Validators", "SiteSettingEditValidator.cs"));

        businessEntitySource.Should().Contain("public string DefaultCulture { get; set; } = DomainDefaults.DefaultCulture;");
        businessEntitySource.Should().Contain("public string DefaultTimeZoneId { get; set; } = DomainDefaults.DefaultTimezone;");
        businessEntitySource.Should().Contain("public string? AdminTextOverridesJson { get; set; }");
        businessEntitySource.Should().Contain("public string? BrandDisplayName { get; set; }");
        businessEntitySource.Should().Contain("public string? BrandLogoUrl { get; set; }");
        businessEntitySource.Should().Contain("public string? BrandPrimaryColorHex { get; set; }");
        businessEntitySource.Should().Contain("public string? BrandSecondaryColorHex { get; set; }");
        businessEntitySource.Should().Contain("public string? SupportEmail { get; set; }");
        businessEntitySource.Should().Contain("public string? CommunicationSenderName { get; set; }");
        businessEntitySource.Should().Contain("public string? CommunicationReplyToEmail { get; set; }");
        businessEntitySource.Should().Contain("public bool CustomerEmailNotificationsEnabled { get; set; } = true;");
        businessEntitySource.Should().Contain("public bool CustomerMarketingEmailsEnabled { get; set; }");
        businessEntitySource.Should().Contain("public bool OperationalAlertEmailsEnabled { get; set; } = true;");
        businessEntitySource.Should().Contain("public BusinessOperationalStatus OperationalStatus { get; set; } = BusinessOperationalStatus.PendingApproval;");
        businessEntitySource.Should().Contain("public DateTime? ApprovedAtUtc { get; set; }");
        businessEntitySource.Should().Contain("public DateTime? SuspendedAtUtc { get; set; }");
        businessEntitySource.Should().Contain("public string? SuspensionReason { get; set; }");

        createBusinessSource.Should().Contain("var settings = await _db.Set<SiteSetting>().AsNoTracking().FirstOrDefaultAsync(ct) ?? new SiteSetting();");
        createBusinessSource.Should().Contain("var supportEmail = NormalizeNullable(dto.SupportEmail) ?? contactEmail ?? NormalizeNullable(settings.SmtpFromAddress);");
        createBusinessSource.Should().Contain("DefaultCulture = dto.DefaultCulture.Trim(),");
        createBusinessSource.Should().Contain("DefaultTimeZoneId = dto.DefaultTimeZoneId.Trim(),");
        createBusinessSource.Should().Contain("AdminTextOverridesJson = string.IsNullOrWhiteSpace(dto.AdminTextOverridesJson) ? null : dto.AdminTextOverridesJson.Trim(),");
        createBusinessSource.Should().Contain("SupportEmail = supportEmail,");
        createBusinessSource.Should().Contain("CommunicationSenderName = communicationSenderName,");
        createBusinessSource.Should().Contain("CommunicationReplyToEmail = communicationReplyToEmail,");
        createBusinessSource.Should().Contain("CustomerMarketingEmailsEnabled = dto.CustomerMarketingEmailsEnabled,");
        createBusinessSource.Should().Contain("OperationalStatus = BusinessOperationalStatus.PendingApproval");

        updateBusinessSource.Should().Contain("entity.DefaultCulture = dto.DefaultCulture.Trim();");
        updateBusinessSource.Should().Contain("entity.DefaultTimeZoneId = dto.DefaultTimeZoneId.Trim();");
        updateBusinessSource.Should().Contain("entity.AdminTextOverridesJson = string.IsNullOrWhiteSpace(dto.AdminTextOverridesJson) ? null : dto.AdminTextOverridesJson.Trim();");
        updateBusinessSource.Should().Contain("entity.SupportEmail = string.IsNullOrWhiteSpace(dto.SupportEmail) ? null : dto.SupportEmail.Trim();");
        updateBusinessSource.Should().Contain("entity.CommunicationSenderName = string.IsNullOrWhiteSpace(dto.CommunicationSenderName) ? null : dto.CommunicationSenderName.Trim();");
        updateBusinessSource.Should().Contain("entity.CommunicationReplyToEmail = string.IsNullOrWhiteSpace(dto.CommunicationReplyToEmail) ? null : dto.CommunicationReplyToEmail.Trim();");
        updateBusinessSource.Should().Contain("entity.CustomerMarketingEmailsEnabled = dto.CustomerMarketingEmailsEnabled;");
        updateBusinessSource.Should().Contain("entity.IsActive = entity.OperationalStatus == BusinessOperationalStatus.Approved && dto.IsActive;");

        lifecycleSource.Should().Contain("entity.OperationalStatus = BusinessOperationalStatus.Approved;");
        lifecycleSource.Should().Contain("entity.ApprovedAtUtc ??= _clock.UtcNow;");
        lifecycleSource.Should().Contain("entity.SuspendedAtUtc = _clock.UtcNow;");
        lifecycleSource.Should().Contain("entity.OperationalStatus = BusinessOperationalStatus.Suspended;");
        lifecycleSource.Should().Contain("entity.SuspensionReason = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim();");

        accessStateSource.Should().Contain("public BusinessOperationalStatus OperationalStatus { get; set; } = BusinessOperationalStatus.PendingApproval;");
        accessStateSource.Should().Contain("public bool IsApprovalPending => OperationalStatus == BusinessOperationalStatus.PendingApproval;");
        accessStateSource.Should().Contain("public bool IsSuspended => OperationalStatus == BusinessOperationalStatus.Suspended;");
        accessStateSource.Should().Contain("public bool HasActivationBlockingIssues => !IsBusinessClientAccessAllowed;");

        onboardingCustomerSource.Should().Contain("internal static class BusinessOnboardingCustomerProfileSupport");
        onboardingCustomerSource.Should().Contain("if (!string.IsNullOrWhiteSpace(business.SupportEmail))");

        businessValidatorsSource.Should().Contain("RuleFor(x => x.DefaultCulture).NotEmpty().MaximumLength(20);");
        businessValidatorsSource.Should().Contain("RuleFor(x => x.DefaultTimeZoneId).NotEmpty().MaximumLength(64);");
        businessValidatorsSource.Should().Contain("RuleFor(x => x.AdminTextOverridesJson)");
        businessValidatorsSource.Should().Contain(".Must(BusinessValidatorJsonHelpers.BeAdminTextOverridesJson)");
        businessValidatorsSource.Should().Contain("RuleFor(x => x.SupportEmail).MaximumLength(200).EmailAddress().When(x => x.SupportEmail != null);");
        businessValidatorsSource.Should().Contain("RuleFor(x => x.CommunicationSenderName).MaximumLength(200);");
        businessValidatorsSource.Should().Contain("RuleFor(x => x.CommunicationReplyToEmail).MaximumLength(200).EmailAddress().When(x => x.CommunicationReplyToEmail != null);");

        siteSettingEntitySource.Should().Contain("public string DefaultCulture { get; set; } = DomainDefaults.DefaultCulture;");
        siteSettingEntitySource.Should().Contain("public string TimeZone { get; set; } = DomainDefaults.DefaultTimezone;");
        siteSettingEntitySource.Should().Contain("public string? AdminTextOverridesJson { get; set; }");
        siteSettingEntitySource.Should().Contain("public bool StripeEnabled { get; set; } = false;");
        siteSettingEntitySource.Should().Contain("public string? StripeWebhookSecret { get; set; }");
        siteSettingEntitySource.Should().Contain("public bool DhlEnabled { get; set; } = false;");
        siteSettingEntitySource.Should().Contain("public string? DhlApiBaseUrl { get; set; }");
        siteSettingEntitySource.Should().Contain("public string? DhlShipperName { get; set; }");
        siteSettingEntitySource.Should().Contain("public int ShipmentAttentionDelayHours { get; set; } = 24;");
        siteSettingEntitySource.Should().Contain("public int ShipmentTrackingGraceHours { get; set; } = 12;");

        siteSettingDtoSource.Should().Contain("public const string DefaultCultureDefault = DomainDefaults.DefaultCulture;");
        siteSettingDtoSource.Should().Contain("public const string TimeZoneDefault = DomainDefaults.DefaultTimezone;");
        siteSettingDtoSource.Should().Contain("public string DefaultCulture { get; set; } = DefaultCultureDefault;");
        siteSettingDtoSource.Should().Contain("public string? AdminTextOverridesJson { get; set; }");

        getSiteSettingSource.Should().Contain("DefaultCulture = s.DefaultCulture ?? SiteSettingDto.DefaultCultureDefault,");
        getSiteSettingSource.Should().Contain("TimeZone = s.TimeZone ?? SiteSettingDto.TimeZoneDefault,");
        getSiteSettingSource.Should().Contain("AdminTextOverridesJson = s.AdminTextOverridesJson,");
        getSiteSettingSource.Should().Contain("StripeWebhookSecret = s.StripeWebhookSecret,");
        getSiteSettingSource.Should().Contain("DhlApiBaseUrl = s.DhlApiBaseUrl,");
        getSiteSettingSource.Should().Contain("DhlShipperName = s.DhlShipperName,");
        getSiteSettingSource.Should().Contain("ShipmentTrackingGraceHours = s.ShipmentTrackingGraceHours,");

        updateSiteSettingSource.Should().Contain("s.DefaultCulture = dto.DefaultCulture.Trim();");
        updateSiteSettingSource.Should().Contain("s.TimeZone = dto.TimeZone ?? SiteSettingDto.TimeZoneDefault;");
        updateSiteSettingSource.Should().Contain("s.AdminTextOverridesJson = string.IsNullOrWhiteSpace(dto.AdminTextOverridesJson)");
        updateSiteSettingSource.Should().Contain("s.StripeWebhookSecret = dto.StripeWebhookSecret;");
        updateSiteSettingSource.Should().Contain("s.DhlApiBaseUrl = dto.DhlApiBaseUrl;");
        updateSiteSettingSource.Should().Contain("s.DhlShipperName = dto.DhlShipperName;");
        updateSiteSettingSource.Should().Contain("s.ShipmentTrackingGraceHours = dto.ShipmentTrackingGraceHours;");

        siteSettingValidatorSource.Should().Contain("RuleFor(x => x.DefaultCulture)");
        siteSettingValidatorSource.Should().Contain("RuleFor(x => x.AdminTextOverridesJson)");
        siteSettingValidatorSource.Should().Contain(".Must(BeAdminTextOverridesJson)");
        siteSettingValidatorSource.Should().Contain("RuleFor(x => x.DhlApiBaseUrl)");
        siteSettingValidatorSource.Should().Contain("RuleFor(x => x.DhlShipperCountry)");
        siteSettingValidatorSource.Should().Contain("RuleFor(x => x.ShipmentTrackingGraceHours)");
    }

    [Fact]
    public void CommunicationCore_Should_KeepPlatformLevelDomainApplicationAndInfrastructureContractsWired()
    {
        var emailAuditEntitySource = ReadDomainFile(Path.Combine("Entities", "Integration", "EmailDispatchAudit.cs"));
        var emailDispatchOperationEntitySource = ReadDomainFile(Path.Combine("Entities", "Integration", "EmailDispatchOperation.cs"));
        var channelDispatchOperationEntitySource = ReadDomainFile(Path.Combine("Entities", "Integration", "ChannelDispatchOperation.cs"));
        var channelAuditEntitySource = ReadDomainFile(Path.Combine("Entities", "Integration", "ChannelDispatchAudit.cs"));
        var emailDispatchContextSource = ReadApplicationFile(Path.Combine("Abstractions", "Notifications", "EmailDispatchContext.cs"));
        var channelDispatchContextSource = ReadApplicationFile(Path.Combine("Abstractions", "Notifications", "ChannelDispatchContext.cs"));
        var emailSenderSource = ReadApplicationFile(Path.Combine("Abstractions", "Notifications", "IEmailSender.cs"));
        var smsSenderSource = ReadApplicationFile(Path.Combine("Abstractions", "Notifications", "ISmsSender.cs"));
        var whatsAppSenderSource = ReadApplicationFile(Path.Combine("Abstractions", "Notifications", "IWhatsAppSender.cs"));
        var templateDefaultsSource = ReadApplicationFile(Path.Combine("Communication", "CommunicationTemplateDefaults.cs"));
        var emailAuditQueriesSource = ReadApplicationFile(Path.Combine("Businesses", "Queries", "GetEmailDispatchAuditsPageHandler.cs"));
        var channelAuditQueriesSource = ReadApplicationFile(Path.Combine("Businesses", "Queries", "GetChannelDispatchActivityHandler.cs"));
        var phoneVerificationSource = ReadApplicationFile(Path.Combine("Identity", "Commands", "PhoneVerificationHandlers.cs"));
        var createInvitationSource = ReadApplicationFile(Path.Combine("Businesses", "Commands", "CreateBusinessInvitationHandler.cs"));
        var emailConfirmationSource = ReadApplicationFile(Path.Combine("Identity", "Commands", "EmailConfirmationHandlers.cs"));
        var passwordResetSource = ReadApplicationFile(Path.Combine("Identity", "Commands", "RequestPasswordResetHandler.cs"));
        var notificationsRegistrationSource = ReadInfrastructureFile(Path.Combine("Extensions", "ServiceCollectionExtensions.Notifications.cs"));
        var smtpSenderSource = ReadInfrastructureFile(Path.Combine("Notifications", "Smtp", "SmtpEmailSender.cs"));
        var emailDispatchOperationConfigSource = ReadInfrastructureFile(Path.Combine("Persistence", "Configurations", "Integration", "EmailDispatchOperationConfiguration.cs"));
        var channelDispatchOperationConfigSource = ReadInfrastructureFile(Path.Combine("Persistence", "Configurations", "Integration", "ChannelDispatchOperationConfiguration.cs"));
        var smsProviderSource = ReadInfrastructureFile(Path.Combine("Notifications", "Sms", "ProviderBackedSmsSender.cs"));
        var whatsAppProviderSource = ReadInfrastructureFile(Path.Combine("Notifications", "WhatsApp", "MetaWhatsAppSender.cs"));
        var emailAuditConfigSource = ReadInfrastructureFile(Path.Combine("Persistence", "Configurations", "Integration", "EmailDispatchAuditConfiguration.cs"));
        var channelAuditConfigSource = ReadInfrastructureFile(Path.Combine("Persistence", "Configurations", "Integration", "ChannelDispatchAuditConfiguration.cs"));
        var dbContextSource = ReadInfrastructureFile(Path.Combine("Persistence", "Db", "DarwinDbContext.cs"));
        var workerProgramSource = ReadWorkerFile("Program.cs");
        var emailDispatchOperationWorkerSource = ReadWorkerFile("EmailDispatchOperationBackgroundService.cs");
        var emailDispatchOperationWorkerOptionsSource = ReadWorkerFile("EmailDispatchOperationWorkerOptions.cs");
        var channelDispatchOperationWorkerSource = ReadWorkerFile("ChannelDispatchOperationBackgroundService.cs");
        var channelDispatchOperationWorkerOptionsSource = ReadWorkerFile("ChannelDispatchOperationWorkerOptions.cs");
        var workerSettingsSource = ReadWorkerFile("appsettings.json");

        emailAuditEntitySource.Should().Contain("public sealed class EmailDispatchAudit : BaseEntity");
        emailAuditEntitySource.Should().Contain("public string Provider { get; set; } = \"SMTP\";");
        emailAuditEntitySource.Should().Contain("public string RecipientEmail { get; set; } = string.Empty;");
        emailAuditEntitySource.Should().Contain("public string Status { get; set; } = \"Pending\";");
        emailAuditEntitySource.Should().Contain("public string? FlowKey { get; set; }");
        emailAuditEntitySource.Should().Contain("public string? TemplateKey { get; set; }");
        emailAuditEntitySource.Should().Contain("public string? CorrelationKey { get; set; }");
        emailAuditEntitySource.Should().Contain("public Guid? BusinessId { get; set; }");
        emailAuditEntitySource.Should().Contain("public string? IntendedRecipientEmail { get; set; }");
        emailAuditEntitySource.Should().Contain("public string? ProviderMessageId { get; set; }");
        emailDispatchOperationEntitySource.Should().Contain("public sealed class EmailDispatchOperation : BaseEntity");
        emailDispatchOperationEntitySource.Should().Contain("public string Provider { get; set; } = \"SMTP\";");
        emailDispatchOperationEntitySource.Should().Contain("public string RecipientEmail { get; set; } = string.Empty;");
        emailDispatchOperationEntitySource.Should().Contain("public string Subject { get; set; } = string.Empty;");
        emailDispatchOperationEntitySource.Should().Contain("public string HtmlBody { get; set; } = string.Empty;");
        emailDispatchOperationEntitySource.Should().Contain("public string Status { get; set; } = \"Pending\";");
        emailDispatchOperationEntitySource.Should().Contain("public string? TemplateKey { get; set; }");
        emailDispatchOperationEntitySource.Should().Contain("public string? CorrelationKey { get; set; }");
        emailDispatchOperationEntitySource.Should().Contain("public Guid? BusinessId { get; set; }");
        channelDispatchOperationEntitySource.Should().Contain("public sealed class ChannelDispatchOperation : BaseEntity");
        channelDispatchOperationEntitySource.Should().Contain("public string Channel { get; set; } = string.Empty;");
        channelDispatchOperationEntitySource.Should().Contain("public string Provider { get; set; } = string.Empty;");
        channelDispatchOperationEntitySource.Should().Contain("public string RecipientAddress { get; set; } = string.Empty;");
        channelDispatchOperationEntitySource.Should().Contain("public string MessageText { get; set; } = string.Empty;");
        channelDispatchOperationEntitySource.Should().Contain("public string Status { get; set; } = \"Pending\";");

        channelAuditEntitySource.Should().Contain("public sealed class ChannelDispatchAudit : BaseEntity");
        channelAuditEntitySource.Should().Contain("public string Channel { get; set; } = string.Empty;");
        channelAuditEntitySource.Should().Contain("public string Provider { get; set; } = string.Empty;");
        channelAuditEntitySource.Should().Contain("public string RecipientAddress { get; set; } = string.Empty;");
        channelAuditEntitySource.Should().Contain("public string Status { get; set; } = \"Pending\";");
        channelAuditEntitySource.Should().Contain("public string? FlowKey { get; set; }");
        channelAuditEntitySource.Should().Contain("public string? TemplateKey { get; set; }");
        channelAuditEntitySource.Should().Contain("public string? CorrelationKey { get; set; }");
        channelAuditEntitySource.Should().Contain("public Guid? BusinessId { get; set; }");
        channelAuditEntitySource.Should().Contain("public string? IntendedRecipientAddress { get; set; }");
        channelAuditEntitySource.Should().Contain("public string? ProviderMessageId { get; set; }");

        emailDispatchContextSource.Should().Contain("public sealed class EmailDispatchContext");
        emailDispatchContextSource.Should().Contain("public string? TemplateKey { get; set; }");
        emailDispatchContextSource.Should().Contain("public string? CorrelationKey { get; set; }");
        emailDispatchContextSource.Should().Contain("public string? IntendedRecipientEmail { get; set; }");

        channelDispatchContextSource.Should().Contain("public sealed class ChannelDispatchContext");
        channelDispatchContextSource.Should().Contain("public string? TemplateKey { get; init; }");
        channelDispatchContextSource.Should().Contain("public string? CorrelationKey { get; init; }");
        channelDispatchContextSource.Should().Contain("public string? IntendedRecipientAddress { get; init; }");

        emailSenderSource.Should().Contain("public interface IEmailSender");
        emailSenderSource.Should().Contain("Task SendAsync(");
        smsSenderSource.Should().Contain("public interface ISmsSender");
        smsSenderSource.Should().Contain("Task SendAsync(");
        whatsAppSenderSource.Should().Contain("public interface IWhatsAppSender");
        whatsAppSenderSource.Should().Contain("Task SendTextAsync(");

        templateDefaultsSource.Should().Contain("public static class CommunicationTemplateDefaults");
        templateDefaultsSource.Should().Contain("LegacyBusinessInvitationSubjectTemplate");
        templateDefaultsSource.Should().Contain("LegacyAccountActivationSubjectTemplate");
        templateDefaultsSource.Should().Contain("LegacyPasswordResetSubjectTemplate");
        templateDefaultsSource.Should().Contain("LegacyPhoneVerificationSmsTemplate");
        templateDefaultsSource.Should().Contain("LegacyPhoneVerificationWhatsAppTemplate");

        emailAuditQueriesSource.Should().Contain("_db.Set<EmailDispatchAudit>().AsNoTracking()");
        emailAuditQueriesSource.Should().Contain("public sealed class GetEmailDispatchAuditsPageHandler");
        emailAuditQueriesSource.Should().Contain("public async Task<EmailDispatchAuditSummaryDto> GetSummaryAsync");
        channelAuditQueriesSource.Should().Contain("_db.Set<ChannelDispatchAudit>().AsNoTracking()");
        channelAuditQueriesSource.Should().Contain("public sealed class GetChannelDispatchActivityHandler");
        channelAuditQueriesSource.Should().Contain("ChannelDispatchAuditSummaryDto");
        channelAuditQueriesSource.Should().Contain("BuildQueuedOperationItemsAsync(");
        channelAuditQueriesSource.Should().Contain("_db.Set<ChannelDispatchOperation>()");
        channelAuditQueriesSource.Should().Contain("IsQueueOperation = true");
        channelAuditQueriesSource.Should().Contain("summary.QueuedPendingCount = queuedItems.Count");
        channelAuditQueriesSource.Should().Contain("summary.QueuedFailedCount = queuedItems.Count");

        phoneVerificationSource.Should().Contain("private readonly ISmsSender _smsSender;");
        phoneVerificationSource.Should().Contain("private readonly IWhatsAppSender _whatsAppSender;");
        phoneVerificationSource.Should().Contain("CommunicationTemplateDefaults.ResolveTemplate(");
        phoneVerificationSource.Should().Contain("FlowKey = \"PhoneVerification\"");
        phoneVerificationSource.Should().Contain("TemplateKey = \"PhoneVerificationSms\"");
        phoneVerificationSource.Should().Contain("TemplateKey = \"PhoneVerificationWhatsApp\"");
        phoneVerificationSource.Should().Contain("CorrelationKey = tokenEntity.Id.ToString(\"N\")");
        phoneVerificationSource.Should().Contain("IntendedRecipientAddress = user.PhoneE164");
        createInvitationSource.Should().Contain("private readonly IEmailSender _emailSender;");
        createInvitationSource.Should().Contain("CommunicationTemplateDefaults.ResolveTemplate(");
        createInvitationSource.Should().Contain("TemplateKey = \"BusinessInvitationEmail\"");
        createInvitationSource.Should().Contain("CorrelationKey = entity.Id.ToString(\"N\")");
        createInvitationSource.Should().Contain("IntendedRecipientEmail = entity.Email");
        emailConfirmationSource.Should().Contain("private readonly IEmailSender _email;");
        emailConfirmationSource.Should().Contain("CommunicationTemplateDefaults.ResolveTemplate(");
        emailConfirmationSource.Should().Contain("TemplateKey = \"AccountActivationEmail\"");
        emailConfirmationSource.Should().Contain("CorrelationKey = tokenEntity.Id.ToString(\"N\")");
        emailConfirmationSource.Should().Contain("IntendedRecipientEmail = user.Email");
        passwordResetSource.Should().Contain("private readonly IEmailSender _email;");
        passwordResetSource.Should().Contain("CommunicationTemplateDefaults.ResolveTemplate(");
        passwordResetSource.Should().Contain("TemplateKey = \"PasswordResetEmail\"");
        passwordResetSource.Should().Contain("CorrelationKey = tokenEntity.Id.ToString(\"N\")");
        passwordResetSource.Should().Contain("IntendedRecipientEmail = user.Email");

        notificationsRegistrationSource.Should().Contain("services.AddScoped<SmtpEmailSender>();");
        notificationsRegistrationSource.Should().Contain("services.AddHttpClient<BrevoEmailSender>");
        notificationsRegistrationSource.Should().Contain("services.AddScoped<IEmailSender>(serviceProvider =>");
        notificationsRegistrationSource.Should().Contain("EmailProviderNames.Normalize(options.Provider) switch");
        notificationsRegistrationSource.Should().Contain("EmailProviderNames.Brevo => serviceProvider.GetRequiredService<BrevoEmailSender>()");
        notificationsRegistrationSource.Should().Contain("EmailProviderNames.Smtp => serviceProvider.GetRequiredService<SmtpEmailSender>()");
        notificationsRegistrationSource.Should().Contain("services.AddScoped<ProviderBackedSmsSender>();");
        notificationsRegistrationSource.Should().Contain("services.AddScoped<ISmsSender, ProviderBackedSmsSender>();");
        notificationsRegistrationSource.Should().Contain("services.AddScoped<MetaWhatsAppSender>();");
        notificationsRegistrationSource.Should().Contain("services.AddScoped<IWhatsAppSender, MetaWhatsAppSender>();");
        smtpSenderSource.Should().Contain("public sealed class SmtpEmailSender : IEmailSender");
        smtpSenderSource.Should().Contain("var audit = new EmailDispatchAudit");
        smtpSenderSource.Should().Contain("TemplateKey = string.IsNullOrWhiteSpace(context?.TemplateKey) ? null : context.TemplateKey.Trim()");
        smtpSenderSource.Should().Contain("var correlationKey = NormalizeCorrelationKey(context?.CorrelationKey);");
        smtpSenderSource.Should().Contain("CorrelationKey = correlationKey,");
        smtpSenderSource.Should().Contain("IntendedRecipientEmail = string.IsNullOrWhiteSpace(context?.IntendedRecipientEmail) ? toEmail : context.IntendedRecipientEmail.Trim()");
        smsProviderSource.Should().Contain("public sealed class ProviderBackedSmsSender : ISmsSender");
        smsProviderSource.Should().Contain("var audit = new ChannelDispatchAudit");
        smsProviderSource.Should().Contain("TemplateKey = string.IsNullOrWhiteSpace(context?.TemplateKey) ? null : context.TemplateKey.Trim()");
        smsProviderSource.Should().Contain("var correlationKey = NormalizeCorrelationKey(context?.CorrelationKey);");
        smsProviderSource.Should().Contain("CorrelationKey = correlationKey,");
        smsProviderSource.Should().Contain("IntendedRecipientAddress = string.IsNullOrWhiteSpace(context?.IntendedRecipientAddress) ? toPhoneE164 : context.IntendedRecipientAddress.Trim()");
        smsProviderSource.Should().Contain("audit.ProviderMessageId = ExtractProviderMessageId(body);");
        smsProviderSource.Should().Contain("document.RootElement.TryGetProperty(\"sid\", out var sidElement)");
        whatsAppProviderSource.Should().Contain("public sealed class MetaWhatsAppSender : IWhatsAppSender");
        whatsAppProviderSource.Should().Contain("var audit = new ChannelDispatchAudit");
        whatsAppProviderSource.Should().Contain("TemplateKey = string.IsNullOrWhiteSpace(context?.TemplateKey) ? null : context.TemplateKey.Trim()");
        whatsAppProviderSource.Should().Contain("var correlationKey = NormalizeCorrelationKey(context?.CorrelationKey);");
        whatsAppProviderSource.Should().Contain("CorrelationKey = correlationKey,");
        whatsAppProviderSource.Should().Contain("IntendedRecipientAddress = string.IsNullOrWhiteSpace(context?.IntendedRecipientAddress) ? toPhoneE164 : context.IntendedRecipientAddress.Trim()");
        whatsAppProviderSource.Should().Contain("audit.ProviderMessageId = ExtractProviderMessageId(body);");
        whatsAppProviderSource.Should().Contain("document.RootElement.TryGetProperty(\"messages\", out var messagesElement)");
        emailDispatchOperationConfigSource.Should().Contain("public sealed class EmailDispatchOperationConfiguration : IEntityTypeConfiguration<EmailDispatchOperation>");
        emailDispatchOperationConfigSource.Should().Contain("builder.ToTable(\"EmailDispatchOperations\", schema: \"Integration\")");
        emailDispatchOperationConfigSource.Should().Contain("builder.Property(x => x.Subject).HasMaxLength(512).IsRequired();");
        emailDispatchOperationConfigSource.Should().Contain("builder.Property(x => x.HtmlBody).IsRequired();");
        channelDispatchOperationConfigSource.Should().Contain("public sealed class ChannelDispatchOperationConfiguration : IEntityTypeConfiguration<ChannelDispatchOperation>");
        channelDispatchOperationConfigSource.Should().Contain("builder.ToTable(\"ChannelDispatchOperations\", schema: \"Integration\")");
        channelDispatchOperationConfigSource.Should().Contain("builder.Property(x => x.MessageText).IsRequired();");

        emailAuditConfigSource.Should().Contain("public sealed class EmailDispatchAuditConfiguration : IEntityTypeConfiguration<EmailDispatchAudit>");
        emailAuditConfigSource.Should().Contain("builder.ToTable(\"EmailDispatchAudits\", schema: \"Integration\")");
        emailAuditConfigSource.Should().Contain("builder.Property(x => x.TemplateKey)");
        emailAuditConfigSource.Should().Contain("builder.Property(x => x.CorrelationKey)");
        emailAuditConfigSource.Should().Contain("builder.Property(x => x.IntendedRecipientEmail)");
        dbContextSource.Should().Contain("public DbSet<EmailDispatchOperation> EmailDispatchOperations => Set<EmailDispatchOperation>();");
        dbContextSource.Should().Contain("public DbSet<ChannelDispatchOperation> ChannelDispatchOperations => Set<ChannelDispatchOperation>();");
        workerProgramSource.Should().Contain("builder.Services.Configure<EmailDispatchOperationWorkerOptions>(builder.Configuration.GetSection(\"EmailDispatchOperationWorker\"));");
        workerProgramSource.Should().Contain("builder.Services.AddHostedService<EmailDispatchOperationBackgroundService>();");
        workerProgramSource.Should().Contain("builder.Services.Configure<ChannelDispatchOperationWorkerOptions>(builder.Configuration.GetSection(\"ChannelDispatchOperationWorker\"));");
        workerProgramSource.Should().Contain("builder.Services.AddHostedService<ChannelDispatchOperationBackgroundService>();");
        emailDispatchOperationWorkerOptionsSource.Should().Contain("public sealed class EmailDispatchOperationWorkerOptions");
        emailDispatchOperationWorkerSource.Should().Contain("public sealed class EmailDispatchOperationBackgroundService : BackgroundService");
        emailDispatchOperationWorkerSource.Should().Contain("db.Set<EmailDispatchOperation>()");
        emailDispatchOperationWorkerSource.Should().Contain("x.Status == \"Pending\" || x.Status == \"Failed\"");
        emailDispatchOperationWorkerSource.Should().Contain("IEmailSender sender = EmailProviderNames.Normalize(item.Provider) switch");
        emailDispatchOperationWorkerSource.Should().Contain("EmailProviderNames.Brevo => services.GetRequiredService<BrevoEmailSender>()");
        emailDispatchOperationWorkerSource.Should().Contain("EmailProviderNames.Smtp => services.GetRequiredService<SmtpEmailSender>()");
        emailDispatchOperationWorkerSource.Should().Contain("new EmailDispatchContext");
        channelDispatchOperationWorkerOptionsSource.Should().Contain("public sealed class ChannelDispatchOperationWorkerOptions");
        channelDispatchOperationWorkerSource.Should().Contain("public sealed class ChannelDispatchOperationBackgroundService : BackgroundService");
        channelDispatchOperationWorkerSource.Should().Contain("db.Set<ChannelDispatchOperation>()");
        channelDispatchOperationWorkerSource.Should().Contain("x.Status == \"Pending\" || x.Status == \"Failed\"");
        channelDispatchOperationWorkerSource.Should().Contain("var sender = services.GetRequiredService<ProviderBackedSmsSender>();");
        channelDispatchOperationWorkerSource.Should().Contain("var sender = services.GetRequiredService<MetaWhatsAppSender>();");
        channelDispatchOperationWorkerSource.Should().Contain("new ChannelDispatchContext");
        workerSettingsSource.Should().Contain("\"EmailDispatchOperationWorker\"");
        workerSettingsSource.Should().Contain("\"ChannelDispatchOperationWorker\"");
        emailAuditConfigSource.Should().Contain("builder.Property(x => x.ProviderMessageId)");
        emailAuditConfigSource.Should().Contain("builder.HasIndex(x => x.IntendedRecipientEmail);");
        emailAuditConfigSource.Should().Contain("builder.HasIndex(new[] { nameof(EmailDispatchAudit.CorrelationKey) }, \"IX_EmailDispatchAudits_CorrelationKey_Model\")");
        emailAuditConfigSource.Should().Contain("builder.HasIndex(new[] { nameof(EmailDispatchAudit.CorrelationKey) }, \"UX_EmailDispatchAudits_ActiveCorrelation_Model\")");
        channelAuditConfigSource.Should().Contain("public sealed class ChannelDispatchAuditConfiguration : IEntityTypeConfiguration<ChannelDispatchAudit>");
        channelAuditConfigSource.Should().Contain("builder.ToTable(\"ChannelDispatchAudits\", schema: \"Integration\")");
        channelAuditConfigSource.Should().Contain("builder.Property(x => x.TemplateKey)");
        channelAuditConfigSource.Should().Contain("builder.Property(x => x.CorrelationKey)");
        channelAuditConfigSource.Should().Contain("builder.Property(x => x.IntendedRecipientAddress)");
        channelAuditConfigSource.Should().Contain("builder.Property(x => x.ProviderMessageId)");
        channelAuditConfigSource.Should().Contain("builder.HasIndex(x => x.CorrelationKey)");
        channelAuditConfigSource.Should().Contain("builder.HasIndex(x => new { x.Channel, x.CorrelationKey })");
        channelAuditConfigSource.Should().Contain("builder.HasIndex(x => x.IntendedRecipientAddress);");
        dbContextSource.Should().Contain("public DbSet<EmailDispatchAudit> EmailDispatchAudits => Set<EmailDispatchAudit>();");
        dbContextSource.Should().Contain("public DbSet<ChannelDispatchAudit> ChannelDispatchAudits => Set<ChannelDispatchAudit>();");
    }

    [Fact]
    public void PaymentDomain_Should_KeepProviderIntentAndCheckoutSessionReferencesWired()
    {
        var billingModelsSource = ReadDomainFile(Path.Combine("Entities", "Billing", "BillingModels.cs"));
        var paymentConfigSource = ReadInfrastructureFile(Path.Combine("Persistence", "Configurations", "Billing", "PaymentConfiguration.cs"));
        var storefrontCheckoutDtosSource = ReadApplicationFile(Path.Combine("Orders", "DTOs", "StorefrontCheckoutDtos.cs"));
        var memberOrderDtosSource = ReadApplicationFile(Path.Combine("Orders", "DTOs", "MemberOrderDtos.cs"));
        var storefrontCheckoutHandlersSource = ReadApplicationFile(Path.Combine("Orders", "Commands", "StorefrontCheckoutHandlers.cs"));
        var storefrontCheckoutQueriesSource = ReadApplicationFile(Path.Combine("Orders", "Queries", "StorefrontCheckoutQueries.cs"));
        var memberOrderQueriesSource = ReadApplicationFile(Path.Combine("Orders", "Queries", "MemberOrderQueries.cs"));

        billingModelsSource.Should().Contain("public string? ProviderPaymentIntentRef { get; set; }");
        billingModelsSource.Should().Contain("public string? ProviderCheckoutSessionRef { get; set; }");

        paymentConfigSource.Should().Contain("builder.Property(x => x.ProviderPaymentIntentRef)");
        paymentConfigSource.Should().Contain("builder.Property(x => x.ProviderCheckoutSessionRef)");
        paymentConfigSource.Should().Contain("builder.HasIndex(x => x.ProviderPaymentIntentRef);");
        paymentConfigSource.Should().Contain("builder.HasIndex(x => x.ProviderCheckoutSessionRef);");

        storefrontCheckoutDtosSource.Should().Contain("public string? ProviderPaymentIntentReference { get; set; }");
        storefrontCheckoutDtosSource.Should().Contain("public string? ProviderCheckoutSessionReference { get; set; }");
        memberOrderDtosSource.Should().Contain("public string? ProviderPaymentIntentReference { get; set; }");
        memberOrderDtosSource.Should().Contain("public string? ProviderCheckoutSessionReference { get; set; }");

        storefrontCheckoutHandlersSource.Should().Contain("var providerPaymentIntentReference = IsStripeProvider(provider)");
        storefrontCheckoutHandlersSource.Should().Contain("var providerCheckoutSessionReference = IsStripeProvider(provider)");
        storefrontCheckoutHandlersSource.Should().Contain("ProviderPaymentIntentRef = providerPaymentIntentReference,");
        storefrontCheckoutHandlersSource.Should().Contain("ProviderCheckoutSessionRef = providerCheckoutSessionReference,");
        storefrontCheckoutHandlersSource.Should().Contain("ProviderPaymentIntentReference = existing.ProviderPaymentIntentRef,");
        storefrontCheckoutHandlersSource.Should().Contain("ProviderCheckoutSessionReference = existing.ProviderCheckoutSessionRef,");
        storefrontCheckoutHandlersSource.Should().Contain("if (!string.IsNullOrWhiteSpace(dto.ProviderPaymentIntentReference) &&");
        storefrontCheckoutHandlersSource.Should().Contain("!ReferenceMatches(dto.ProviderPaymentIntentReference, payment.ProviderPaymentIntentRef)");
        storefrontCheckoutHandlersSource.Should().Contain("if (!string.IsNullOrWhiteSpace(dto.ProviderCheckoutSessionReference) &&");
        storefrontCheckoutHandlersSource.Should().Contain("!ReferenceMatches(dto.ProviderCheckoutSessionReference, payment.ProviderCheckoutSessionRef)");

        storefrontCheckoutQueriesSource.Should().Contain("ProviderPaymentIntentReference = payment.ProviderPaymentIntentRef,");
        storefrontCheckoutQueriesSource.Should().Contain("ProviderCheckoutSessionReference = payment.ProviderCheckoutSessionRef,");
        memberOrderQueriesSource.Should().Contain("ProviderPaymentIntentReference = payment.ProviderPaymentIntentRef,");
        memberOrderQueriesSource.Should().Contain("ProviderCheckoutSessionReference = payment.ProviderCheckoutSessionRef,");
    }

    [Fact]
    public void StripeWebhookProcessing_Should_KeepIdempotentEventLogAndProviderLifecycleContractsWired()
    {
        var handlerSource = ReadApplicationFile(Path.Combine("Billing", "ProcessStripeWebhookHandler.cs"));
        var validationResourceSource = ReadApplicationFile(Path.Combine("Resources", "ValidationResource.resx"));
        var validationResourceDeSource = ReadApplicationFile(Path.Combine("Resources", "ValidationResource.de-DE.resx"));

        handlerSource.Should().Contain("public sealed class StripeWebhookProcessingResultDto");
        handlerSource.Should().Contain("public bool IsDuplicate { get; set; }");
        handlerSource.Should().Contain("Type = BuildEventLogType(eventType),");
        handlerSource.Should().Contain("IdempotencyKey = eventId");
        handlerSource.Should().Contain("case \"checkout.session.completed\":");
        handlerSource.Should().Contain("case \"payment_intent.succeeded\":");
        handlerSource.Should().Contain("case \"payment_intent.payment_failed\":");
        handlerSource.Should().Contain("case \"payment_intent.canceled\":");
        handlerSource.Should().Contain("case \"charge.refunded\":");
        handlerSource.Should().Contain("case \"invoice.paid\":");
        handlerSource.Should().Contain("case \"invoice.payment_failed\":");
        handlerSource.Should().Contain("case \"customer.subscription.updated\":");
        handlerSource.Should().Contain("case \"customer.subscription.deleted\":");
        handlerSource.Should().Contain("payment.ProviderPaymentIntentRef ??=");
        handlerSource.Should().Contain("payment.ProviderCheckoutSessionRef ??=");
        handlerSource.Should().Contain("payment.Status = PaymentStatus.Refunded;");
        handlerSource.Should().Contain("invoice.Status = SubscriptionInvoiceStatus.Paid;");
        handlerSource.Should().Contain("subscription.Status = SubscriptionStatus.Active;");
        handlerSource.Should().Contain("subscription.Status = SubscriptionStatus.PastDue;");
        handlerSource.Should().Contain("subscription.Status = SubscriptionStatus.Canceled;");

        validationResourceSource.Should().Contain("name=\"StripeWebhookSignatureHeaderRequired\"");
        validationResourceSource.Should().Contain("name=\"StripeWebhookSecretNotConfigured\"");
        validationResourceSource.Should().Contain("name=\"StripeWebhookSignatureInvalid\"");
        validationResourceSource.Should().Contain("name=\"StripeWebhookPayloadInvalid\"");
        validationResourceSource.Should().Contain("name=\"StripeWebhookEventIdRequired\"");
        validationResourceSource.Should().Contain("name=\"StripeWebhookEventTypeRequired\"");
        validationResourceSource.Should().Contain("name=\"StripeWebhookProcessingFailed\"");

        validationResourceDeSource.Should().Contain("name=\"StripeWebhookSignatureHeaderRequired\"");
        validationResourceDeSource.Should().Contain("name=\"StripeWebhookSecretNotConfigured\"");
        validationResourceDeSource.Should().Contain("name=\"StripeWebhookSignatureInvalid\"");
        validationResourceDeSource.Should().Contain("name=\"StripeWebhookPayloadInvalid\"");
        validationResourceDeSource.Should().Contain("name=\"StripeWebhookEventIdRequired\"");
        validationResourceDeSource.Should().Contain("name=\"StripeWebhookEventTypeRequired\"");
        validationResourceDeSource.Should().Contain("name=\"StripeWebhookProcessingFailed\"");
    }

    [Fact]
    public void BillingPaymentAuditTrail_Should_KeepStripeEventCorrelationContractsWired()
    {
        var billingDtosSource = ReadApplicationFile(Path.Combine("Billing", "DTOs", "BillingManagementDtos.cs"));
        var billingQueriesSource = ReadApplicationFile(Path.Combine("Billing", "Queries", "BillingManagementQueries.cs"));
        var sharedResourceSource = ReadWebAdminFile(Path.Combine("Resources", "SharedResource.resx"));
        var sharedResourceDeSource = ReadWebAdminFile(Path.Combine("Resources", "SharedResource.de-DE.resx"));

        billingDtosSource.Should().Contain("public List<PaymentProviderEventItemDto> ProviderEvents { get; set; } = new();");
        billingDtosSource.Should().Contain("public sealed class PaymentProviderEventItemDto");
        billingDtosSource.Should().Contain("public string CorrelationKind { get; set; } = string.Empty;");
        billingDtosSource.Should().Contain("public string CorrelationReference { get; set; } = string.Empty;");

        billingQueriesSource.Should().Contain("dto.ProviderEvents = await GetProviderEventsAsync(dto, ct).ConfigureAwait(false);");
        billingQueriesSource.Should().Contain("private async Task<List<PaymentProviderEventItemDto>> GetProviderEventsAsync(PaymentEditDto dto, CancellationToken ct)");
        billingQueriesSource.Should().Contain("x.Type.StartsWith(\"StripeWebhook:\")");
        billingQueriesSource.Should().Contain("var paymentIntentPattern = paymentIntentRef is null ? null : QueryLikePattern.Contains(paymentIntentRef);");
        billingQueriesSource.Should().Contain("var checkoutSessionPattern = checkoutSessionRef is null ? null : QueryLikePattern.Contains(checkoutSessionRef);");
        billingQueriesSource.Should().Contain("var providerTransactionPattern = providerTransactionRef is null ? null : QueryLikePattern.Contains(providerTransactionRef);");
        billingQueriesSource.Should().Contain("(paymentIntentPattern != null && EF.Functions.Like(x.PropertiesJson, paymentIntentPattern, QueryLikePattern.EscapeCharacter))");
        billingQueriesSource.Should().Contain("(checkoutSessionPattern != null && EF.Functions.Like(x.PropertiesJson, checkoutSessionPattern, QueryLikePattern.EscapeCharacter))");
        billingQueriesSource.Should().Contain("(providerTransactionPattern != null && EF.Functions.Like(x.PropertiesJson, providerTransactionPattern, QueryLikePattern.EscapeCharacter))");
        billingQueriesSource.Should().Contain("internal static class BillingProviderAuditFormatter");
        billingQueriesSource.Should().Contain("return \"PaymentIntent\";");
        billingQueriesSource.Should().Contain("return \"CheckoutSession\";");
        billingQueriesSource.Should().Contain("return \"ProviderTransaction\";");
        billingQueriesSource.Should().Contain("return \"Multiple\";");

        sharedResourceSource.Should().Contain("<data name=\"PaymentProviderAuditTrail\"");
        sharedResourceSource.Should().Contain("<data name=\"PaymentProviderEventCheckoutSessionCompleted\"");
        sharedResourceSource.Should().Contain("<data name=\"PaymentProviderEventPaymentIntentSucceeded\"");
        sharedResourceSource.Should().Contain("<data name=\"PaymentProviderEventCorrelationPaymentIntent\"");
        sharedResourceSource.Should().Contain("<data name=\"PaymentSupportPlaybookProviderTimelineTitle\"");
        sharedResourceSource.Should().Contain("<data name=\"PaymentSupportPlaybookProviderTimelinePresentScope\"");
        sharedResourceSource.Should().Contain("<data name=\"PaymentSupportPlaybookProviderTimelineMissingAction\"");

        sharedResourceDeSource.Should().Contain("<data name=\"PaymentProviderAuditTrail\"");
        sharedResourceDeSource.Should().Contain("<data name=\"PaymentProviderEventCheckoutSessionCompleted\"");
        sharedResourceDeSource.Should().Contain("<data name=\"PaymentProviderEventPaymentIntentSucceeded\"");
        sharedResourceDeSource.Should().Contain("<data name=\"PaymentProviderEventCorrelationPaymentIntent\"");
        sharedResourceDeSource.Should().Contain("<data name=\"PaymentSupportPlaybookProviderTimelineTitle\"");
        sharedResourceDeSource.Should().Contain("<data name=\"PaymentSupportPlaybookProviderTimelinePresentScope\"");
        sharedResourceDeSource.Should().Contain("<data name=\"PaymentSupportPlaybookProviderTimelineMissingAction\"");
    }

    [Fact]
    public void BillingWebhookDiagnostics_Should_KeepRetrySafeFailureClassificationContractsWired()
    {
        var billingWebhookDtosSource = ReadApplicationFile(Path.Combine("Billing", "DTOs", "BillingWebhookDtos.cs"));
        var billingWebhookQueriesSource = ReadApplicationFile(Path.Combine("Billing", "Queries", "BillingWebhookQueries.cs"));
        var sharedResourceSource = ReadWebAdminFile(Path.Combine("Resources", "SharedResource.resx"));
        var sharedResourceDeSource = ReadWebAdminFile(Path.Combine("Resources", "SharedResource.de-DE.resx"));

        billingWebhookDtosSource.Should().Contain("public string RetrySafetyState { get; set; } = string.Empty;");
        billingWebhookDtosSource.Should().Contain("public string FailureDiagnostics { get; set; } = string.Empty;");
        billingWebhookDtosSource.Should().Contain("public string EscalationHint { get; set; } = string.Empty;");

        billingWebhookQueriesSource.Should().Contain("item.RetrySafetyState = ResolveRetrySafetyState(item);");
        billingWebhookQueriesSource.Should().Contain("item.FailureDiagnostics = ResolveFailureDiagnostics(item);");
        billingWebhookQueriesSource.Should().Contain("item.EscalationHint = ResolveEscalationHint(item);");
        billingWebhookQueriesSource.Should().Contain("private static string ResolveRetrySafetyState(BillingWebhookDeliveryListItemDto item)");
        billingWebhookQueriesSource.Should().Contain("private static string ResolveFailureDiagnostics(BillingWebhookDeliveryListItemDto item)");
        billingWebhookQueriesSource.Should().Contain("private static string ResolveEscalationHint(BillingWebhookDeliveryListItemDto item)");
        billingWebhookQueriesSource.Should().Contain("return \"WebhookRetrySafetySubscriptionInactive\";");
        billingWebhookQueriesSource.Should().Contain("return \"WebhookFailureDiagnosticReceiver5xx\";");
        billingWebhookQueriesSource.Should().Contain("return \"WebhookEscalationHintReceiver4xx\";");

        sharedResourceSource.Should().Contain("<data name=\"FailureDiagnostics\"");
        sharedResourceSource.Should().Contain("<data name=\"WebhookRetrySafetyRetryInFlight\"");
        sharedResourceSource.Should().Contain("<data name=\"WebhookFailureDiagnosticReceiver4xx\"");
        sharedResourceSource.Should().Contain("<data name=\"WebhookEscalationHintReceiver5xx\"");

        sharedResourceDeSource.Should().Contain("<data name=\"FailureDiagnostics\"");
        sharedResourceDeSource.Should().Contain("<data name=\"WebhookRetrySafetyRetryInFlight\"");
        sharedResourceDeSource.Should().Contain("<data name=\"WebhookFailureDiagnosticReceiver4xx\"");
        sharedResourceDeSource.Should().Contain("<data name=\"WebhookEscalationHintReceiver5xx\"");
    }
}














































