using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Loyalty;
using Darwin.Mobile.Business.Resources;
using Darwin.Mobile.Business.Services.Identity;
using Darwin.Mobile.Business.Services.Reporting;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Services.Loyalty;
using Darwin.Mobile.Shared.ViewModels;
using Darwin.Shared.Results;

namespace Darwin.Mobile.Business.ViewModels;

/// <summary>
/// View model for business-side reward tier management.
/// </summary>
/// <remarks>
/// This screen is the first functional "Reward editing interface" for Mobile Business Phase-2.
/// It allows operators to:
/// - load current reward tiers,
/// - create a new tier,
/// - edit an existing tier,
/// - soft delete a tier with concurrency token.
///
/// UI-bound state updates are always dispatched through <see cref="BaseViewModel.RunOnMain(System.Action)"/> to keep MAUI thread-safe.
/// </remarks>
public sealed partial class RewardsViewModel : BaseViewModel
{
    private readonly ILoyaltyService _loyaltyService;
    private readonly IBusinessAuthorizationService _authorizationService;
    private readonly IBusinessActivityTracker _activityTracker;
    private readonly List<BusinessCampaignEditorItem> _allCampaigns = new();

    private bool _loadedOnce;
    private bool _canManageRewards = true;
    private string _operatorRole = "—";
    private Guid _editingRewardTierId;
    private byte[] _editingRowVersion = Array.Empty<byte>();

    private string _pointsRequiredInput = string.Empty;
    private string _selectedRewardType = RewardTypeFreeItem;
    private string? _rewardValueInput;
    private string? _descriptionInput;
    private bool _allowSelfRedemption;

    private Guid _editingCampaignId;
    private byte[] _editingCampaignRowVersion = Array.Empty<byte>();
    private short _editingCampaignChannels = 1;
    private string _editingCampaignTargetingJson = "{}";
    private string _editingCampaignPayloadJson = "{}";

    private string _campaignNameInput = string.Empty;
    private string _campaignTitleInput = string.Empty;
    private string? _campaignBodyInput;
    private string? _campaignStartsAtInput;
    private string? _campaignEndsAtInput;
    private string _campaignTargetingJsonInput = "{}";
    private string _campaignPayloadJsonInput = "{}";
    private string _campaignTargetingHint = AppResources.RewardsCampaignTargetingHintDefault;
    private string _campaignTargetingSchemaValidationMessage = string.Empty;
    private string _campaignTargetingFixStatusMessage = string.Empty;
    private int _campaignTargetingFixAppliedCount;
    private int _campaignTargetingFixNoChangeCount;
    private DateTimeOffset? _campaignTargetingFixMetricsWindowStartedAtUtc;
    private DateTimeOffset? _campaignTargetingFixMetricsLastResetAtUtc;
    private CampaignChannelOption? _selectedCampaignChannel;
    private string _campaignSearchQuery = string.Empty;
    private CampaignStateFilterOption? _selectedCampaignStateFilter;
    private CampaignAudienceFilterOption? _selectedCampaignAudienceFilter;
    private CampaignSortOption? _selectedCampaignSortOption;
    private DateTimeOffset? _campaignDiagnosticsSnapshotAtLocal;
    private string? _campaignDiagnosticsCopyStatus;

    private const int CampaignListPageSize = 50;

    private const string RewardTypeFreeItem = "FreeItem";
    private const string RewardTypePercentDiscount = "PercentDiscount";
    private const string RewardTypeAmountDiscount = "AmountDiscount";

    public RewardsViewModel(ILoyaltyService loyaltyService, IBusinessAuthorizationService authorizationService, IBusinessActivityTracker activityTracker)
    {
        _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _activityTracker = activityTracker ?? throw new ArgumentNullException(nameof(activityTracker));

        RewardTiers = new ObservableCollection<RewardTierEditorItem>();
        Campaigns = new ObservableCollection<BusinessCampaignEditorItem>();
        RewardTypeOptions = new ObservableCollection<string>
        {
            RewardTypeFreeItem,
            RewardTypePercentDiscount,
            RewardTypeAmountDiscount
        };

        CampaignChannelOptions = new ObservableCollection<CampaignChannelOption>
        {
            new CampaignChannelOption(1, AppResources.RewardsCampaignChannelInAppOnly),
            new CampaignChannelOption(3, AppResources.RewardsCampaignChannelInAppAndPush)
        };

        CampaignStateFilterOptions = new ObservableCollection<CampaignStateFilterOption>
        {
            new CampaignStateFilterOption(string.Empty, AppResources.RewardsCampaignStateFilterAll),
            new CampaignStateFilterOption(PromotionCampaignState.Draft, AppResources.RewardsCampaignStateFilterDraft),
            new CampaignStateFilterOption(PromotionCampaignState.Scheduled, AppResources.RewardsCampaignStateFilterScheduled),
            new CampaignStateFilterOption(PromotionCampaignState.Active, AppResources.RewardsCampaignStateFilterActive),
            new CampaignStateFilterOption(PromotionCampaignState.Expired, AppResources.RewardsCampaignStateFilterExpired)
        };

        CampaignAudienceFilterOptions = new ObservableCollection<CampaignAudienceFilterOption>
        {
            new CampaignAudienceFilterOption(string.Empty, AppResources.RewardsCampaignAudienceFilterAll),
            new CampaignAudienceFilterOption(PromotionAudienceKind.JoinedMembers, AppResources.RewardsCampaignAudienceJoinedMembers),
            new CampaignAudienceFilterOption(PromotionAudienceKind.TierSegment, AppResources.RewardsCampaignAudienceTierSegment),
            new CampaignAudienceFilterOption(PromotionAudienceKind.PointsThreshold, AppResources.RewardsCampaignAudiencePointsThreshold),
            new CampaignAudienceFilterOption(PromotionAudienceKind.DateWindow, AppResources.RewardsCampaignAudienceDateWindow)
        };

        CampaignSortOptions = new ObservableCollection<CampaignSortOption>
        {
            new CampaignSortOption(CampaignSortMode.StartDateDesc, AppResources.RewardsCampaignSortStartDateDesc),
            new CampaignSortOption(CampaignSortMode.StartDateAsc, AppResources.RewardsCampaignSortStartDateAsc),
            new CampaignSortOption(CampaignSortMode.TitleAsc, AppResources.RewardsCampaignSortTitleAsc),
            new CampaignSortOption(CampaignSortMode.TitleDesc, AppResources.RewardsCampaignSortTitleDesc)
        };

        _selectedCampaignChannel = CampaignChannelOptions[0];
        _selectedCampaignStateFilter = CampaignStateFilterOptions[0];
        _selectedCampaignAudienceFilter = CampaignAudienceFilterOptions[0];
        _selectedCampaignSortOption = CampaignSortOptions[0];
        UpdateCampaignTargetingHint();
        RefreshCampaignTargetingSchemaValidationState();

        RefreshCommand = new AsyncCommand(LoadConfigurationAsync, () => !IsBusy);
        SaveCommand = new AsyncCommand(SaveAsync, () => !IsBusy && CanManageRewards);
        DeleteCommand = new AsyncCommand(DeleteAsync, () => !IsBusy && IsEditMode && CanManageRewards);
        CreateNewCommand = new AsyncCommand(CreateNewAsync, () => !IsBusy && CanManageRewards);
        ToggleCampaignActivationCommand = new AsyncCommand<BusinessCampaignEditorItem>(ToggleCampaignActivationAsync, campaign => !IsBusy && CanManageRewards && campaign is not null);
        SaveCampaignCommand = new AsyncCommand(SaveCampaignAsync, () => !IsBusy && CanManageRewards);
        NewCampaignCommand = new AsyncCommand(NewCampaignAsync, () => !IsBusy && CanManageRewards);
        ClearCampaignFiltersCommand = new AsyncCommand(ClearCampaignFiltersAsync, () => !IsBusy && HasActiveCampaignFilters);
        ClearCampaignSearchCommand = new AsyncCommand(ClearCampaignSearchAsync, () => !IsBusy && !string.IsNullOrWhiteSpace(CampaignSearchQuery));
        ApplyCampaignStateFilterCommand = new AsyncCommand<string>(ApplyCampaignStateFilterAsync, _ => !IsBusy);
        ApplyCampaignAudienceFilterCommand = new AsyncCommand<string>(ApplyCampaignAudienceFilterAsync, _ => !IsBusy);
        ApplyCampaignTargetingPresetCommand = new AsyncCommand<string>(ApplyCampaignTargetingPresetAsync, _ => !IsBusy && CanManageRewards);
        ApplyCampaignTargetingSchemaFixCommand = new AsyncCommand(ApplyCampaignTargetingSchemaQuickFixAsync, () => !IsBusy && CanManageRewards && HasCampaignTargetingSchemaValidationError);
        ResetCampaignTargetingFixMetricsCommand = new AsyncCommand(ResetCampaignTargetingQuickFixMetricsAsync, () => !IsBusy && CanManageRewards && (CampaignTargetingFixAppliedCount > 0 || CampaignTargetingFixNoChangeCount > 0));
        CopyCampaignDiagnosticsCommand = new AsyncCommand(CopyCampaignDiagnosticsAsync, () => !IsBusy && HasCampaignDataset);
        ClearCampaignDiagnosticsStatusCommand = new AsyncCommand(ClearCampaignDiagnosticsStatusAsync, () => !IsBusy && HasCampaignDiagnosticsCopyStatus);
    }


    /// <summary>
    /// Indicates whether the current operator can edit reward tiers.
    /// </summary>
    public bool CanManageRewards
    {
        get => _canManageRewards;
        private set
        {
            if (SetProperty(ref _canManageRewards, value))
            {
                SaveCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
                OnPropertyChanged(nameof(CanDeleteReward));
                SaveCampaignCommand.RaiseCanExecuteChanged();
                NewCampaignCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Operator role display resolved from token claims.
    /// </summary>
    public string OperatorRole
    {
        get => _operatorRole;
        private set => SetProperty(ref _operatorRole, value);
    }

    /// <summary>
    /// Collection of currently configured reward tiers for the business.
    /// </summary>
    public ObservableCollection<RewardTierEditorItem> RewardTiers { get; }

    /// <summary>
    /// Collection of business campaigns available for quick lifecycle actions.
    /// </summary>
    public ObservableCollection<BusinessCampaignEditorItem> Campaigns { get; }

    /// <summary>
    /// Picker options for reward type contract values.
    /// </summary>
    public ObservableCollection<string> RewardTypeOptions { get; }

    /// <summary>
    /// Picker options for campaign channel combinations supported in mobile editor.
    /// </summary>
    public ObservableCollection<CampaignChannelOption> CampaignChannelOptions { get; }

    /// <summary>
    /// Picker options for campaign lifecycle state filtering in the management list.
    /// </summary>
    public ObservableCollection<CampaignStateFilterOption> CampaignStateFilterOptions { get; }

    /// <summary>
    /// Picker options for campaign audience filtering in the management list.
    /// </summary>
    public ObservableCollection<CampaignAudienceFilterOption> CampaignAudienceFilterOptions { get; }

    /// <summary>
    /// Picker options for campaign list ordering in management UI.
    /// </summary>
    public ObservableCollection<CampaignSortOption> CampaignSortOptions { get; }

    /// <summary>
    /// Search query used to filter campaign list by internal name/title.
    /// </summary>
    public string CampaignSearchQuery
    {
        get => _campaignSearchQuery;
        set
        {
            if (SetProperty(ref _campaignSearchQuery, value))
            {
                ApplyCampaignFilter();
            }
        }
    }

    /// <summary>
    /// Currently selected lifecycle state filter for campaign list.
    /// </summary>
    public CampaignStateFilterOption? SelectedCampaignStateFilter
    {
        get => _selectedCampaignStateFilter;
        set
        {
            if (SetProperty(ref _selectedCampaignStateFilter, value))
            {
                ApplyCampaignFilter();
            }
        }
    }

    /// <summary>
    /// Currently selected audience filter for campaign list.
    /// </summary>
    public CampaignAudienceFilterOption? SelectedCampaignAudienceFilter
    {
        get => _selectedCampaignAudienceFilter;
        set
        {
            if (SetProperty(ref _selectedCampaignAudienceFilter, value))
            {
                ApplyCampaignFilter();
            }
        }
    }

    /// <summary>
    /// Currently selected ordering option for campaign list.
    /// </summary>
    public CampaignSortOption? SelectedCampaignSortOption
    {
        get => _selectedCampaignSortOption;
        set
        {
            if (SetProperty(ref _selectedCampaignSortOption, value))
            {
                ApplyCampaignFilter();
            }
        }
    }

    /// <summary>
    /// Indicates whether there are campaigns visible after applying current filter criteria.
    /// </summary>
    public bool HasCampaigns => Campaigns.Count > 0;

    /// <summary>
    /// Gets total campaign count returned from server before filters are applied.
    /// </summary>
    public int TotalCampaignCount => _allCampaigns.Count;

    /// <summary>
    /// Gets whether at least one campaign exists in the current dataset (before filters are applied).
    /// </summary>
    public bool HasCampaignDataset => TotalCampaignCount > 0;

    /// <summary>
    /// Gets campaign count currently visible after active filters are applied.
    /// </summary>
    public int FilteredCampaignCount => Campaigns.Count;

    /// <summary>
    /// Gets whether any campaign filter/search criteria are currently active.
    /// </summary>
    public bool HasActiveCampaignFilters =>
        !string.IsNullOrWhiteSpace(CampaignSearchQuery) ||
        !string.IsNullOrWhiteSpace(SelectedCampaignStateFilter?.StateKey) ||
        !string.IsNullOrWhiteSpace(SelectedCampaignAudienceFilter?.AudienceKindKey);

    /// <summary>
    /// Gets whether campaign search query currently contains a value.
    /// </summary>
    public bool HasCampaignSearchQuery => !string.IsNullOrWhiteSpace(CampaignSearchQuery);

    /// <summary>
    /// Human-readable summary for filtered campaign count.
    /// </summary>
    public string CampaignFilterSummary => string.Format(
        CultureInfo.InvariantCulture,
        AppResources.RewardsCampaignFilterSummaryFormat,
        FilteredCampaignCount,
        TotalCampaignCount);

    /// <summary>
    /// Gets count of campaigns currently in Draft state from full campaign dataset.
    /// </summary>
    public int DraftCampaignCount => CountCampaignsByState(PromotionCampaignState.Draft);

    /// <summary>
    /// Gets count of campaigns currently in Scheduled state from full campaign dataset.
    /// </summary>
    public int ScheduledCampaignCount => CountCampaignsByState(PromotionCampaignState.Scheduled);

    /// <summary>
    /// Gets count of campaigns currently in Active state from full campaign dataset.
    /// </summary>
    public int ActiveCampaignCount => CountCampaignsByState(PromotionCampaignState.Active);

    /// <summary>
    /// Gets count of campaigns currently in Expired state from full campaign dataset.
    /// </summary>
    public int ExpiredCampaignCount => CountCampaignsByState(PromotionCampaignState.Expired);

    /// <summary>
    /// Localized summary line for campaign lifecycle quick metrics.
    /// </summary>
    public string CampaignStateMetricsSummary => string.Format(
        CultureInfo.InvariantCulture,
        AppResources.RewardsCampaignStateMetricsFormat,
        DraftCampaignCount,
        ScheduledCampaignCount,
        ActiveCampaignCount,
        ExpiredCampaignCount);

    /// <summary>
    /// Localized summary line for campaign audience KPI counters.
    /// </summary>
    public string CampaignAudienceMetricsSummary => string.Format(
        CultureInfo.InvariantCulture,
        AppResources.RewardsCampaignAudienceMetricsFormat,
        JoinedMembersCampaignCount,
        TierSegmentCampaignCount,
        PointsThresholdCampaignCount,
        DateWindowCampaignCount);

    /// <summary>
    /// Localized timestamp text that indicates when the current diagnostics snapshot was produced.
    /// </summary>
    public string CampaignDiagnosticsSnapshotAtText =>
        _campaignDiagnosticsSnapshotAtLocal.HasValue
            ? string.Format(
                CultureInfo.CurrentCulture,
                AppResources.RewardsCampaignDiagnosticsSnapshotAtFormat,
                _campaignDiagnosticsSnapshotAtLocal.Value.ToString("yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture))
            : string.Empty;

    /// <summary>
    /// Localized freshness text for campaign diagnostics snapshot to help operators identify stale context.
    /// </summary>
    public string CampaignDiagnosticsFreshnessText
    {
        get
        {
            if (!_campaignDiagnosticsSnapshotAtLocal.HasValue)
            {
                return string.Empty;
            }

            var ageMinutes = Math.Max(0, (int)Math.Round((DateTimeOffset.Now - _campaignDiagnosticsSnapshotAtLocal.Value).TotalMinutes, MidpointRounding.AwayFromZero));
            return ageMinutes > 15
                ? string.Format(CultureInfo.CurrentCulture, AppResources.RewardsCampaignDiagnosticsFreshnessStaleFormat, ageMinutes)
                : string.Format(CultureInfo.CurrentCulture, AppResources.RewardsCampaignDiagnosticsFreshnessFreshFormat, ageMinutes);
        }
    }

    /// <summary>
    /// Indicates whether the latest campaign diagnostics snapshot crossed the stale threshold.
    /// </summary>
    public bool IsCampaignDiagnosticsSnapshotStale
        => _campaignDiagnosticsSnapshotAtLocal.HasValue
           && (DateTimeOffset.Now - _campaignDiagnosticsSnapshotAtLocal.Value).TotalMinutes > 15;

    /// <summary>
    /// Localized preview of visible campaign titles to make diagnostics payload easier to read before copying.
    /// </summary>
    public string CampaignDiagnosticsVisibleCampaignsPreview
    {
        get
        {
            if (!HasCampaigns)
            {
                return AppResources.RewardsCampaignDiagnosticsVisibleCampaignsEmpty;
            }

            const int previewLimit = 3;
            var visibleTitles = Campaigns
                .Select(campaign => campaign.Title)
                .Where(title => !string.IsNullOrWhiteSpace(title))
                .Take(previewLimit)
                .ToArray();

            if (visibleTitles.Length == 0)
            {
                return AppResources.RewardsCampaignDiagnosticsVisibleCampaignsEmpty;
            }

            var remainingCount = Math.Max(0, Campaigns.Count - visibleTitles.Length);
            var joinedTitles = string.Join(", ", visibleTitles);
            return remainingCount > 0
                ? string.Format(
                    CultureInfo.CurrentCulture,
                    AppResources.RewardsCampaignDiagnosticsVisibleCampaignsWithRemainingFormat,
                    joinedTitles,
                    remainingCount)
                : string.Format(
                    CultureInfo.CurrentCulture,
                    AppResources.RewardsCampaignDiagnosticsVisibleCampaignsFormat,
                    joinedTitles);
        }
    }

    /// <summary>
    /// Indicates whether a diagnostics snapshot timestamp is currently available for display.
    /// </summary>
    public bool HasCampaignDiagnosticsSnapshotAt => _campaignDiagnosticsSnapshotAtLocal.HasValue;

    /// <summary>
    /// Latest status message for campaign diagnostics copy action.
    /// </summary>
    public string? CampaignDiagnosticsCopyStatus
    {
        get => _campaignDiagnosticsCopyStatus;
        private set
        {
            if (SetProperty(ref _campaignDiagnosticsCopyStatus, value))
            {
                OnPropertyChanged(nameof(HasCampaignDiagnosticsCopyStatus));
                ClearCampaignDiagnosticsStatusCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Indicates whether a copy status message is currently visible.
    /// </summary>
    public bool HasCampaignDiagnosticsCopyStatus => !string.IsNullOrWhiteSpace(CampaignDiagnosticsCopyStatus);

    /// <summary>
    /// KPI chip text for Draft campaigns.
    /// </summary>
    public string DraftCampaignMetricText => string.Format(CultureInfo.InvariantCulture, AppResources.RewardsCampaignStateMetricChipFormat, AppResources.RewardsCampaignStateFilterDraft, DraftCampaignCount);

    /// <summary>
    /// KPI chip text for Scheduled campaigns.
    /// </summary>
    public string ScheduledCampaignMetricText => string.Format(CultureInfo.InvariantCulture, AppResources.RewardsCampaignStateMetricChipFormat, AppResources.RewardsCampaignStateFilterScheduled, ScheduledCampaignCount);

    /// <summary>
    /// KPI chip text for Active campaigns.
    /// </summary>
    public string ActiveCampaignMetricText => string.Format(CultureInfo.InvariantCulture, AppResources.RewardsCampaignStateMetricChipFormat, AppResources.RewardsCampaignStateFilterActive, ActiveCampaignCount);

    /// <summary>
    /// KPI chip text for Expired campaigns.
    /// </summary>
    public string ExpiredCampaignMetricText => string.Format(CultureInfo.InvariantCulture, AppResources.RewardsCampaignStateMetricChipFormat, AppResources.RewardsCampaignStateFilterExpired, ExpiredCampaignCount);

    /// <summary>
    /// KPI chip text for all campaigns regardless of lifecycle state.
    /// </summary>
    public string AllCampaignMetricText => string.Format(CultureInfo.InvariantCulture, AppResources.RewardsCampaignStateMetricChipFormat, AppResources.RewardsCampaignStateFilterAll, TotalCampaignCount);

    /// <summary>
    /// Gets count of joined-members audience campaigns from full campaign dataset.
    /// </summary>
    public int JoinedMembersCampaignCount => CountCampaignsByAudienceKind(PromotionAudienceKind.JoinedMembers);

    /// <summary>
    /// Gets count of tier-segment audience campaigns from full campaign dataset.
    /// </summary>
    public int TierSegmentCampaignCount => CountCampaignsByAudienceKind(PromotionAudienceKind.TierSegment);

    /// <summary>
    /// Gets count of points-threshold audience campaigns from full campaign dataset.
    /// </summary>
    public int PointsThresholdCampaignCount => CountCampaignsByAudienceKind(PromotionAudienceKind.PointsThreshold);

    /// <summary>
    /// Gets count of date-window audience campaigns from full campaign dataset.
    /// </summary>
    public int DateWindowCampaignCount => CountCampaignsByAudienceKind(PromotionAudienceKind.DateWindow);

    /// <summary>
    /// KPI chip text for all audiences.
    /// </summary>
    public string AllCampaignAudienceMetricText => string.Format(CultureInfo.InvariantCulture, AppResources.RewardsCampaignStateMetricChipFormat, AppResources.RewardsCampaignAudienceFilterAll, TotalCampaignCount);

    /// <summary>
    /// KPI chip text for joined-members audience.
    /// </summary>
    public string JoinedMembersCampaignMetricText => string.Format(CultureInfo.InvariantCulture, AppResources.RewardsCampaignStateMetricChipFormat, AppResources.RewardsCampaignAudienceJoinedMembers, JoinedMembersCampaignCount);

    /// <summary>
    /// KPI chip text for tier-segment audience.
    /// </summary>
    public string TierSegmentCampaignMetricText => string.Format(CultureInfo.InvariantCulture, AppResources.RewardsCampaignStateMetricChipFormat, AppResources.RewardsCampaignAudienceTierSegment, TierSegmentCampaignCount);

    /// <summary>
    /// KPI chip text for points-threshold audience.
    /// </summary>
    public string PointsThresholdCampaignMetricText => string.Format(CultureInfo.InvariantCulture, AppResources.RewardsCampaignStateMetricChipFormat, AppResources.RewardsCampaignAudiencePointsThreshold, PointsThresholdCampaignCount);

    /// <summary>
    /// KPI chip text for date-window audience.
    /// </summary>
    public string DateWindowCampaignMetricText => string.Format(CultureInfo.InvariantCulture, AppResources.RewardsCampaignStateMetricChipFormat, AppResources.RewardsCampaignAudienceDateWindow, DateWindowCampaignCount);

    /// <summary>
    /// Gets count of campaigns configured for in-app only delivery.
    /// </summary>
    public int InAppOnlyCampaignCount => _allCampaigns.Count(campaign => campaign.Channels == 1);

    /// <summary>
    /// Gets count of campaigns configured for in-app plus push delivery.
    /// </summary>
    public int InAppAndPushCampaignCount => _allCampaigns.Count(campaign => campaign.Channels == 3);

    /// <summary>
    /// Gets count of campaigns configured with unexpected/other channel bitmasks.
    /// </summary>
    public int OtherChannelCampaignCount => Math.Max(0, _allCampaigns.Count - InAppOnlyCampaignCount - InAppAndPushCampaignCount);

    /// <summary>
    /// Localized summary line for delivery-channel distribution across all loaded campaigns.
    /// </summary>
    public string CampaignChannelMetricsSummary => string.Format(
        CultureInfo.InvariantCulture,
        AppResources.RewardsCampaignChannelMetricsFormat,
        InAppOnlyCampaignCount,
        InAppAndPushCampaignCount,
        OtherChannelCampaignCount);

    /// <summary>
    /// User-entered points required for the reward tier.
    /// </summary>
    public string PointsRequiredInput
    {
        get => _pointsRequiredInput;
        set => SetProperty(ref _pointsRequiredInput, value);
    }

    /// <summary>
    /// User-selected reward type token that maps directly to API contract values.
    /// </summary>
    public string SelectedRewardType
    {
        get => _selectedRewardType;
        set => SetProperty(ref _selectedRewardType, value);
    }

    /// <summary>
    /// Optional reward numeric value (amount or percent depending on reward type).
    /// </summary>
    public string? RewardValueInput
    {
        get => _rewardValueInput;
        set => SetProperty(ref _rewardValueInput, value);
    }

    /// <summary>
    /// Optional human-readable reward description.
    /// </summary>
    public string? DescriptionInput
    {
        get => _descriptionInput;
        set => SetProperty(ref _descriptionInput, value);
    }

    /// <summary>
    /// Indicates whether self redemption is enabled for the tier.
    /// </summary>
    public bool AllowSelfRedemption
    {
        get => _allowSelfRedemption;
        set => SetProperty(ref _allowSelfRedemption, value);
    }


    /// <summary>
    /// Campaign internal name input for create/update operations.
    /// </summary>
    public string CampaignNameInput
    {
        get => _campaignNameInput;
        set => SetProperty(ref _campaignNameInput, value);
    }

    /// <summary>
    /// Campaign title input shown to end users.
    /// </summary>
    public string CampaignTitleInput
    {
        get => _campaignTitleInput;
        set => SetProperty(ref _campaignTitleInput, value);
    }

    /// <summary>
    /// Campaign body input used as card description.
    /// </summary>
    public string? CampaignBodyInput
    {
        get => _campaignBodyInput;
        set => SetProperty(ref _campaignBodyInput, value);
    }

    /// <summary>
    /// Campaign start UTC input in format yyyy-MM-dd HH:mm (optional).
    /// </summary>
    public string? CampaignStartsAtInput
    {
        get => _campaignStartsAtInput;
        set => SetProperty(ref _campaignStartsAtInput, value);
    }

    /// <summary>
    /// Campaign end UTC input in format yyyy-MM-dd HH:mm (optional).
    /// </summary>
    public string? CampaignEndsAtInput
    {
        get => _campaignEndsAtInput;
        set => SetProperty(ref _campaignEndsAtInput, value);
    }

    /// <summary>
    /// Optional campaign targeting rules in JSON format.
    /// </summary>
    public string CampaignTargetingJsonInput
    {
        get => _campaignTargetingJsonInput;
        set
        {
            if (SetProperty(ref _campaignTargetingJsonInput, value))
            {
                UpdateCampaignTargetingHint();
                RefreshCampaignTargetingSchemaValidationState();
                CampaignTargetingFixStatusMessage = string.Empty;
            }
        }
    }

    /// <summary>
    /// Optional campaign payload in JSON format.
    /// </summary>
    public string CampaignPayloadJsonInput
    {
        get => _campaignPayloadJsonInput;
        set => SetProperty(ref _campaignPayloadJsonInput, value);
    }

    /// <summary>
    /// Inline targeting guidance derived from the current targeting JSON.
    /// </summary>
    public string CampaignTargetingHint
    {
        get => _campaignTargetingHint;
        private set => SetProperty(ref _campaignTargetingHint, value);
    }

    /// <summary>
    /// Inline schema validation message for targeting JSON.
    /// </summary>
    public string CampaignTargetingSchemaValidationMessage
    {
        get => _campaignTargetingSchemaValidationMessage;
        private set
        {
            if (SetProperty(ref _campaignTargetingSchemaValidationMessage, value))
            {
                OnPropertyChanged(nameof(HasCampaignTargetingSchemaValidationError));
                ApplyCampaignTargetingSchemaFixCommand?.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Indicates whether targeting schema currently has inline validation errors.
    /// </summary>
    public bool HasCampaignTargetingSchemaValidationError => !string.IsNullOrWhiteSpace(CampaignTargetingSchemaValidationMessage);

    /// <summary>
    /// Informational status message after applying automatic targeting fixes.
    /// </summary>
    public string CampaignTargetingFixStatusMessage
    {
        get => _campaignTargetingFixStatusMessage;
        private set
        {
            if (SetProperty(ref _campaignTargetingFixStatusMessage, value))
            {
                OnPropertyChanged(nameof(HasCampaignTargetingFixStatusMessage));
            }
        }
    }

    /// <summary>
    /// Indicates whether targeting fix status message should be shown.
    /// </summary>
    public bool HasCampaignTargetingFixStatusMessage => !string.IsNullOrWhiteSpace(CampaignTargetingFixStatusMessage);

    /// <summary>
    /// Counter for quick-fix operations that modified targeting JSON.
    /// </summary>
    public int CampaignTargetingFixAppliedCount
    {
        get => _campaignTargetingFixAppliedCount;
        private set
        {
            if (SetProperty(ref _campaignTargetingFixAppliedCount, value))
            {
                OnPropertyChanged(nameof(CampaignTargetingFixMetricsSummary));
                OnPropertyChanged(nameof(HasCampaignTargetingFixMetrics));
                ResetCampaignTargetingFixMetricsCommand?.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Counter for quick-fix operations that found no required changes.
    /// </summary>
    public int CampaignTargetingFixNoChangeCount
    {
        get => _campaignTargetingFixNoChangeCount;
        private set
        {
            if (SetProperty(ref _campaignTargetingFixNoChangeCount, value))
            {
                OnPropertyChanged(nameof(CampaignTargetingFixMetricsSummary));
                OnPropertyChanged(nameof(HasCampaignTargetingFixMetrics));
                ResetCampaignTargetingFixMetricsCommand?.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Localized quick-fix telemetry summary for operator awareness.
    /// </summary>
    public string CampaignTargetingFixMetricsSummary => string.Format(
        CultureInfo.InvariantCulture,
        AppResources.RewardsCampaignTargetingFixMetricsFormat,
        CampaignTargetingFixAppliedCount,
        CampaignTargetingFixNoChangeCount);

    /// <summary>
    /// Indicates whether quick-fix telemetry counters currently contain any value.
    /// </summary>
    public bool HasCampaignTargetingFixMetrics => CampaignTargetingFixAppliedCount > 0 || CampaignTargetingFixNoChangeCount > 0;

    /// <summary>
    /// UTC timestamp of current quick-fix monitoring window start.
    /// </summary>
    public DateTimeOffset? CampaignTargetingFixMetricsWindowStartedAtUtc
    {
        get => _campaignTargetingFixMetricsWindowStartedAtUtc;
        private set
        {
            if (SetProperty(ref _campaignTargetingFixMetricsWindowStartedAtUtc, value))
            {
                OnPropertyChanged(nameof(CampaignTargetingFixMetricsWindowSummary));
            }
        }
    }

    /// <summary>
    /// UTC timestamp of the latest quick-fix telemetry reset action.
    /// </summary>
    public DateTimeOffset? CampaignTargetingFixMetricsLastResetAtUtc
    {
        get => _campaignTargetingFixMetricsLastResetAtUtc;
        private set
        {
            if (SetProperty(ref _campaignTargetingFixMetricsLastResetAtUtc, value))
            {
                OnPropertyChanged(nameof(CampaignTargetingFixMetricsWindowSummary));
            }
        }
    }

    /// <summary>
    /// Localized quick-fix monitoring window summary.
    /// </summary>
    public string CampaignTargetingFixMetricsWindowSummary => string.Format(
        CultureInfo.CurrentCulture,
        AppResources.RewardsCampaignTargetingFixMetricsWindowFormat,
        CampaignTargetingFixMetricsWindowStartedAtUtc?.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture) ?? AppResources.RewardsCampaignTargetingFixMetricsWindowUnknown,
        CampaignTargetingFixMetricsLastResetAtUtc?.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture) ?? AppResources.RewardsCampaignTargetingFixMetricsWindowUnknown);

    /// <summary>
    /// Selected campaign channel option used for create/update payloads.
    /// </summary>
    public CampaignChannelOption? SelectedCampaignChannel
    {
        get => _selectedCampaignChannel;
        set => SetProperty(ref _selectedCampaignChannel, value);
    }

    /// <summary>
    /// Gets whether campaign editor is in update mode.
    /// </summary>
    public bool IsCampaignEditMode => _editingCampaignId != Guid.Empty;

    /// <summary>
    /// Gets localized save button text for campaign editor.
    /// </summary>
    public string CampaignSaveButtonText => IsCampaignEditMode ? AppResources.RewardsCampaignUpdateButton : AppResources.RewardsCampaignCreateButton;

    /// <summary>
    /// True when current editor is bound to an existing tier.
    /// </summary>
    public bool IsEditMode => _editingRewardTierId != Guid.Empty;

    /// <summary>
    /// Indicates whether current reward can be deleted by the current operator.
    /// </summary>
    public bool CanDeleteReward => IsEditMode && CanManageRewards;

    /// <summary>
    /// Label shown on save button based on create/update mode.
    /// </summary>
    public string SaveButtonText => IsEditMode ? AppResources.RewardsUpdateButton : AppResources.RewardsCreateButton;

    public AsyncCommand RefreshCommand { get; }
    public AsyncCommand SaveCommand { get; }
    public AsyncCommand DeleteCommand { get; }
    public AsyncCommand CreateNewCommand { get; }
    public AsyncCommand<BusinessCampaignEditorItem> ToggleCampaignActivationCommand { get; }
    public AsyncCommand SaveCampaignCommand { get; }
    public AsyncCommand NewCampaignCommand { get; }
    public AsyncCommand ClearCampaignFiltersCommand { get; }
    public AsyncCommand ClearCampaignSearchCommand { get; }
    public AsyncCommand<string> ApplyCampaignStateFilterCommand { get; }
    public AsyncCommand<string> ApplyCampaignAudienceFilterCommand { get; }
    public AsyncCommand<string> ApplyCampaignTargetingPresetCommand { get; }
    public AsyncCommand ApplyCampaignTargetingSchemaFixCommand { get; }
    public AsyncCommand ResetCampaignTargetingFixMetricsCommand { get; }
    public AsyncCommand CopyCampaignDiagnosticsCommand { get; }
    public AsyncCommand ClearCampaignDiagnosticsStatusCommand { get; }

    public override async Task OnAppearingAsync()
    {
        if (_loadedOnce)
        {
            return;
        }

        _loadedOnce = true;
        await RefreshAuthorizationAsync().ConfigureAwait(false);
        await LoadConfigurationAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Loads current reward configuration from API and refreshes local list.
    /// </summary>
    public async Task LoadConfigurationAsync()
    {
        if (IsBusy)
        {
            return;
        }

        BeginBusyOperation();

        try
        {
            var response = await _loyaltyService
                .GetBusinessRewardConfigurationAsync(CancellationToken.None)
                .ConfigureAwait(false);

            if (!response.Succeeded || response.Value is null)
            {
                RunOnMain(() => ErrorMessage = ResolveRewardsFailureMessage(response.Error, AppResources.RewardsLoadFailed));
                return;
            }

            var tiers = response.Value.RewardTiers
                .OrderBy(x => x.PointsRequired)
                .Select(RewardTierEditorItem.FromContract)
                .ToList();

            var campaigns = await LoadCampaignItemsAsync().ConfigureAwait(false);

            RunOnMain(() =>
            {
                ErrorMessage = null;
                RewardTiers.Clear();
                foreach (var tier in tiers)
                {
                    RewardTiers.Add(tier);
                }

                ReplaceCampaigns(campaigns);
            });

            if (!IsEditMode)
            {
                RunOnMain(ClearEditor);
            }

            if (!IsCampaignEditMode)
            {
                RunOnMain(ClearCampaignEditor);
            }
        }
        catch (Exception ex)
        {
            RunOnMain(() => ErrorMessage = ViewModelErrorMapper.ToUserMessage(ex, AppResources.RewardsLoadFailed));
        }
        finally
        {
            EndBusyOperation();
        }
    }

    /// <summary>
    /// Loads selected tier values into the editor for update flow.
    /// </summary>
    public void BeginEdit(RewardTierEditorItem tier)
    {
        ArgumentNullException.ThrowIfNull(tier);

        if (!CanManageRewards)
        {
            RunOnMain(() => ErrorMessage = AppResources.BusinessPermissionDeniedRewardEdit);
            return;
        }

        RunOnMain(() =>
        {
            _editingRewardTierId = tier.RewardTierId;
            _editingRowVersion = tier.RowVersion.ToArray();

            PointsRequiredInput = tier.PointsRequired.ToString();
            SelectedRewardType = string.IsNullOrWhiteSpace(tier.RewardType) ? RewardTypeFreeItem : tier.RewardType;
            RewardValueInput = tier.RewardValue?.ToString();
            DescriptionInput = tier.Description;
            AllowSelfRedemption = tier.AllowSelfRedemption;

            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(SaveButtonText));
            OnPropertyChanged(nameof(CanDeleteReward));
            DeleteCommand.RaiseCanExecuteChanged();
        });
    }


    /// <summary>
    /// Loads selected campaign values into editor for update flow.
    /// </summary>
    public void BeginEditCampaign(BusinessCampaignEditorItem campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);

        if (!CanManageRewards)
        {
            RunOnMain(() => ErrorMessage = AppResources.BusinessPermissionDeniedRewardEdit);
            return;
        }

        RunOnMain(() =>
        {
            _editingCampaignId = campaign.Id;
            _editingCampaignRowVersion = campaign.RowVersion.ToArray();
            _editingCampaignChannels = campaign.Channels;
            SelectedCampaignChannel = CampaignChannelOptions.FirstOrDefault(x => x.Value == campaign.Channels) ?? CampaignChannelOptions[0];
            _editingCampaignTargetingJson = campaign.TargetingJson;
            _editingCampaignPayloadJson = campaign.PayloadJson;

            CampaignNameInput = campaign.Name;
            CampaignTitleInput = campaign.Title;
            CampaignBodyInput = campaign.Body;
            CampaignStartsAtInput = campaign.StartsAtUtc?.ToString("yyyy-MM-dd HH:mm");
            CampaignEndsAtInput = campaign.EndsAtUtc?.ToString("yyyy-MM-dd HH:mm");
            CampaignTargetingJsonInput = campaign.TargetingJson;
            CampaignPayloadJsonInput = campaign.PayloadJson;

            OnPropertyChanged(nameof(IsCampaignEditMode));
            OnPropertyChanged(nameof(CampaignSaveButtonText));
        });
    }

    private Task CreateNewAsync()
    {
        if (!CanManageRewards)
        {
            RunOnMain(() => ErrorMessage = AppResources.BusinessPermissionDeniedRewardEdit);
            return Task.CompletedTask;
        }

        RunOnMain(ClearEditor);
        return Task.CompletedTask;
    }

    private async Task SaveAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (!CanManageRewards)
        {
            RunOnMain(() => ErrorMessage = AppResources.BusinessPermissionDeniedRewardEdit);
            return;
        }

        if (!TryBuildEditorValues(out var pointsRequired, out var rewardValue, out var validationMessage))
        {
            RunOnMain(() => ErrorMessage = validationMessage ?? AppResources.RewardsValidationFailed);
            return;
        }

        BeginBusyOperation();

        try
        {
            Result<BusinessRewardTierMutationResponse> operationResult;

            if (IsEditMode)
            {
                operationResult = await _loyaltyService
                    .UpdateBusinessRewardTierAsync(new UpdateBusinessRewardTierRequest
                    {
                        RewardTierId = _editingRewardTierId,
                        PointsRequired = pointsRequired,
                        RewardType = SelectedRewardType,
                        RewardValue = rewardValue,
                        Description = string.IsNullOrWhiteSpace(DescriptionInput) ? null : DescriptionInput.Trim(),
                        AllowSelfRedemption = AllowSelfRedemption,
                        RowVersion = _editingRowVersion
                    }, CancellationToken.None)
                    .ConfigureAwait(false);
            }
            else
            {
                operationResult = await _loyaltyService
                    .CreateBusinessRewardTierAsync(new CreateBusinessRewardTierRequest
                    {
                        PointsRequired = pointsRequired,
                        RewardType = SelectedRewardType,
                        RewardValue = rewardValue,
                        Description = string.IsNullOrWhiteSpace(DescriptionInput) ? null : DescriptionInput.Trim(),
                        AllowSelfRedemption = AllowSelfRedemption
                    }, CancellationToken.None)
                    .ConfigureAwait(false);
            }

            if (!operationResult.Succeeded)
            {
                RunOnMain(() => ErrorMessage = ResolveRewardsFailureMessage(operationResult.Error, AppResources.RewardsSaveFailed));
                return;
            }

            RunOnMain(() =>
            {
                ErrorMessage = null;
                ClearEditor();
            });

            await ReloadConfigurationAfterMutationAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            RunOnMain(() => ErrorMessage = ViewModelErrorMapper.ToUserMessage(ex, AppResources.RewardsSaveFailed));
        }
        finally
        {
            EndBusyOperation();
        }
    }

    private async Task DeleteAsync()
    {
        if (IsBusy || !IsEditMode)
        {
            return;
        }

        if (!CanManageRewards)
        {
            RunOnMain(() => ErrorMessage = AppResources.BusinessPermissionDeniedRewardEdit);
            return;
        }

        BeginBusyOperation();

        try
        {
            var result = await _loyaltyService
                .DeleteBusinessRewardTierAsync(new DeleteBusinessRewardTierRequest
                {
                    RewardTierId = _editingRewardTierId,
                    RowVersion = _editingRowVersion
                }, CancellationToken.None)
                .ConfigureAwait(false);

            if (!result.Succeeded)
            {
                RunOnMain(() => ErrorMessage = ResolveRewardsFailureMessage(result.Error, AppResources.RewardsDeleteFailed));
                return;
            }

            RunOnMain(() =>
            {
                ErrorMessage = null;
                ClearEditor();
            });

            await ReloadConfigurationAfterMutationAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            RunOnMain(() => ErrorMessage = ViewModelErrorMapper.ToUserMessage(ex, AppResources.RewardsDeleteFailed));
        }
        finally
        {
            EndBusyOperation();
        }
    }


    /// <summary>
    /// Parses optional UTC date input in expected business-editor format.
    /// </summary>
    private static bool TryParseCampaignDate(string? input, out DateTime? valueUtc, out string? error)
    {
        valueUtc = null;
        error = null;

        if (string.IsNullOrWhiteSpace(input))
        {
            return true;
        }

        if (!DateTime.TryParseExact(input.Trim(), "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
        {
            error = AppResources.RewardsCampaignDateValidationFailed;
            return false;
        }

        valueUtc = parsed.Kind switch
        {
            DateTimeKind.Utc => parsed,
            DateTimeKind.Local => parsed.ToUniversalTime(),
            _ => DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
        };

        return true;
    }

    /// <summary>
    /// Computes localized inline guidance for targeting JSON based on the selected audience kind.
    /// </summary>
    private void UpdateCampaignTargetingHint()
    {
        var json = CampaignTargetingJsonInput?.Trim();
        if (string.IsNullOrWhiteSpace(json))
        {
            CampaignTargetingHint = AppResources.RewardsCampaignTargetingHintDefault;
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                CampaignTargetingHint = AppResources.RewardsCampaignTargetingHintInvalid;
                return;
            }

            var audienceKind = root.TryGetProperty("audienceKind", out var audienceElement) && audienceElement.ValueKind == JsonValueKind.String
                ? audienceElement.GetString()
                : PromotionAudienceKind.JoinedMembers;

            CampaignTargetingHint = audienceKind switch
            {
                PromotionAudienceKind.TierSegment => AppResources.RewardsCampaignTargetingHintTierSegment,
                PromotionAudienceKind.PointsThreshold => AppResources.RewardsCampaignTargetingHintPointsThreshold,
                PromotionAudienceKind.DateWindow => AppResources.RewardsCampaignTargetingHintDateWindow,
                _ => AppResources.RewardsCampaignTargetingHintJoinedMembers
            };
        }
        catch (JsonException)
        {
            CampaignTargetingHint = AppResources.RewardsCampaignTargetingHintInvalid;
        }
    }

    /// <summary>
    /// Updates schema-level validation message for targeting JSON using audience-specific checks.
    /// </summary>
    private void RefreshCampaignTargetingSchemaValidationState()
    {
        if (!TryNormalizeCampaignJsonObject(CampaignTargetingJsonInput, AppResources.RewardsCampaignTargetingValidationFailed, out var normalizedJson, out var jsonError))
        {
            CampaignTargetingSchemaValidationMessage = jsonError ?? AppResources.RewardsCampaignTargetingValidationFailed;
            return;
        }

        if (!TryValidateCampaignTargetingSchemaRules(normalizedJson, out var schemaError))
        {
            CampaignTargetingSchemaValidationMessage = schemaError ?? AppResources.RewardsCampaignTargetingValidationFailed;
            return;
        }

        CampaignTargetingSchemaValidationMessage = string.Empty;
    }

    /// <summary>
    /// Validates audience-specific schema requirements for targeting JSON.
    /// </summary>
    private static bool TryValidateCampaignTargetingSchemaRules(string normalizedJson, out string? schemaError)
    {
        schemaError = null;

        using var document = JsonDocument.Parse(normalizedJson);
        var root = document.RootElement;
        var audienceKind = root.TryGetProperty("audienceKind", out var audienceElement) && audienceElement.ValueKind == JsonValueKind.String
            ? audienceElement.GetString()
            : PromotionAudienceKind.JoinedMembers;

        if (string.Equals(audienceKind, PromotionAudienceKind.TierSegment, StringComparison.OrdinalIgnoreCase))
        {
            if (!root.TryGetProperty("tier", out var tierElement) || tierElement.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(tierElement.GetString()))
            {
                schemaError = AppResources.RewardsCampaignTargetingSchemaTierMissing;
                return false;
            }

            return true;
        }

        if (string.Equals(audienceKind, PromotionAudienceKind.PointsThreshold, StringComparison.OrdinalIgnoreCase))
        {
            if (!root.TryGetProperty("minimumPoints", out var minPointsElement) ||
                (minPointsElement.ValueKind != JsonValueKind.Number || !minPointsElement.TryGetInt32(out var minimumPoints) || minimumPoints < 0))
            {
                schemaError = AppResources.RewardsCampaignTargetingSchemaMinimumPointsMissing;
                return false;
            }

            return true;
        }

        if (string.Equals(audienceKind, PromotionAudienceKind.DateWindow, StringComparison.OrdinalIgnoreCase))
        {
            if (!TryReadCampaignUtcDate(root, "eligibleFromUtc", out var eligibleFromUtc) || !TryReadCampaignUtcDate(root, "eligibleToUtc", out var eligibleToUtc))
            {
                schemaError = AppResources.RewardsCampaignTargetingSchemaDateWindowMissing;
                return false;
            }

            if (eligibleFromUtc > eligibleToUtc)
            {
                schemaError = AppResources.RewardsCampaignTargetingSchemaDateWindowRangeInvalid;
                return false;
            }

            return true;
        }

        return true;
    }

    /// <summary>
    /// Reads required UTC date string from targeting JSON object.
    /// </summary>
    private static bool TryReadCampaignUtcDate(JsonElement root, string propertyName, out DateTimeOffset value)
    {
        value = default;
        if (!root.TryGetProperty(propertyName, out var dateElement) || dateElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var raw = dateElement.GetString();
        return DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out value);
    }

    /// <summary>
    /// Applies a safe schema correction for the current targeting JSON when possible.
    /// </summary>
    private async Task ApplyCampaignTargetingSchemaQuickFixAsync()
    {
        if (IsBusy || !CanManageRewards)
        {
            return;
        }

        if (!TryNormalizeCampaignJsonObject(CampaignTargetingJsonInput, AppResources.RewardsCampaignTargetingValidationFailed, out var normalizedJson, out _))
        {
            return;
        }

        if (!TryBuildSchemaFixedTargetingJsonDocument(normalizedJson, out var fixedJson, out var changed))
        {
            return;
        }

        await _activityTracker.RecordCampaignTargetingSchemaFixAsync(changed, CancellationToken.None).ConfigureAwait(false);

        RunOnMain(() =>
        {
            if (!CampaignTargetingFixMetricsWindowStartedAtUtc.HasValue)
            {
                CampaignTargetingFixMetricsWindowStartedAtUtc = DateTimeOffset.UtcNow;
            }

            if (changed)
            {
                CampaignTargetingJsonInput = fixedJson;
                CampaignTargetingFixStatusMessage = AppResources.RewardsCampaignTargetingFixAppliedMessage;
                CampaignTargetingFixAppliedCount++;
            }
            else
            {
                CampaignTargetingFixStatusMessage = AppResources.RewardsCampaignTargetingFixNoChangesMessage;
                CampaignTargetingFixNoChangeCount++;
            }
        });
    }

    /// <summary>
    /// Builds a corrected targeting JSON payload and indicates whether any change was applied.
    /// </summary>
    private static bool TryBuildSchemaFixedTargetingJsonDocument(string normalizedJson, out string fixedJson, out bool changed)
    {
        fixedJson = normalizedJson;
        changed = false;

        try
        {
            using var document = JsonDocument.Parse(normalizedJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                map[property.Name] = JsonSerializer.Deserialize<object?>(property.Value.GetRawText());
            }

            var audienceKind = document.RootElement.TryGetProperty("audienceKind", out var audienceElement) && audienceElement.ValueKind == JsonValueKind.String
                ? audienceElement.GetString()
                : PromotionAudienceKind.JoinedMembers;

            if (string.Equals(audienceKind, PromotionAudienceKind.TierSegment, StringComparison.OrdinalIgnoreCase))
            {
                changed = EnsureTierSegmentSchemaFields(map) || changed;
            }
            else if (string.Equals(audienceKind, PromotionAudienceKind.PointsThreshold, StringComparison.OrdinalIgnoreCase))
            {
                changed = EnsurePointsThresholdSchemaFields(map) || changed;
            }
            else if (string.Equals(audienceKind, PromotionAudienceKind.DateWindow, StringComparison.OrdinalIgnoreCase))
            {
                changed = EnsureDateWindowSchemaFields(document.RootElement, map) || changed;
            }

            fixedJson = JsonSerializer.Serialize(map);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Ensures TierSegment targeting includes non-empty tier key.
    /// </summary>
    private static bool EnsureTierSegmentSchemaFields(IDictionary<string, object?> map)
    {
        if (!map.TryGetValue("tier", out var tierValue) || tierValue is null || string.IsNullOrWhiteSpace(tierValue.ToString()))
        {
            map["tier"] = "Gold";
            return true;
        }

        return false;
    }

    /// <summary>
    /// Ensures PointsThreshold targeting includes non-negative minimum points.
    /// </summary>
    private static bool EnsurePointsThresholdSchemaFields(IDictionary<string, object?> map)
    {
        if (!map.TryGetValue("minimumPoints", out var minimumPointsValue) || minimumPointsValue is null || !int.TryParse(minimumPointsValue.ToString(), out var minimumPoints) || minimumPoints < 0)
        {
            map["minimumPoints"] = 0;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Ensures DateWindow targeting includes valid UTC bounds and normalized range.
    /// </summary>
    private static bool EnsureDateWindowSchemaFields(JsonElement root, IDictionary<string, object?> map)
    {
        var changed = false;
        var hasFrom = TryReadCampaignUtcDate(root, "eligibleFromUtc", out var eligibleFromUtc);
        var hasTo = TryReadCampaignUtcDate(root, "eligibleToUtc", out var eligibleToUtc);

        if (!hasFrom)
        {
            eligibleFromUtc = DateTimeOffset.UtcNow;
            map["eligibleFromUtc"] = eligibleFromUtc.ToString("O", CultureInfo.InvariantCulture);
            changed = true;
        }

        if (!hasTo)
        {
            eligibleToUtc = (hasFrom ? eligibleFromUtc : DateTimeOffset.UtcNow).AddDays(7);
            map["eligibleToUtc"] = eligibleToUtc.ToString("O", CultureInfo.InvariantCulture);
            changed = true;
        }

        if (hasFrom && hasTo && eligibleFromUtc > eligibleToUtc)
        {
            map["eligibleToUtc"] = eligibleFromUtc.ToString("O", CultureInfo.InvariantCulture);
            changed = true;
        }

        return changed;
    }



    /// <summary>
    /// Resets quick-fix telemetry counters for a fresh operational tracking window.
    /// </summary>
    private async Task ResetCampaignTargetingQuickFixMetricsAsync()
    {
        if (IsBusy || !CanManageRewards)
        {
            return;
        }

        await _activityTracker.RecordCampaignTargetingFixMetricsResetAsync(CancellationToken.None).ConfigureAwait(false);

        RunOnMain(() =>
        {
            CampaignTargetingFixAppliedCount = 0;
            CampaignTargetingFixNoChangeCount = 0;
            CampaignTargetingFixMetricsWindowStartedAtUtc = DateTimeOffset.UtcNow;
            CampaignTargetingFixMetricsLastResetAtUtc = DateTimeOffset.UtcNow;
            CampaignTargetingFixStatusMessage = AppResources.RewardsCampaignTargetingFixMetricsResetMessage;
        });
    }

    private static bool TryNormalizeCampaignJsonObject(string? jsonInput, string validationMessage, out string normalizedJson, out string? error)
    {
        normalizedJson = "{}";
        error = null;

        if (string.IsNullOrWhiteSpace(jsonInput))
        {
            return true;
        }

        var trimmed = jsonInput.Trim();
        try
        {
            using var document = JsonDocument.Parse(trimmed);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                error = validationMessage;
                return false;
            }

            normalizedJson = trimmed;
            return true;
        }
        catch (JsonException)
        {
            error = validationMessage;
            return false;
        }
    }

    /// <summary>
    /// Creates or updates campaign based on editor mode.
    /// </summary>
    private async Task SaveCampaignAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (!CanManageRewards)
        {
            RunOnMain(() => ErrorMessage = AppResources.BusinessPermissionDeniedRewardEdit);
            return;
        }

        if (string.IsNullOrWhiteSpace(CampaignNameInput) || string.IsNullOrWhiteSpace(CampaignTitleInput))
        {
            RunOnMain(() => ErrorMessage = AppResources.RewardsCampaignValidationFailed);
            return;
        }

        var normalizedCampaignName = CampaignNameInput.Trim();
        if (HasConflictingCampaignName(normalizedCampaignName, _editingCampaignId))
        {
            RunOnMain(() => ErrorMessage = AppResources.RewardsCampaignNameDuplicateValidationFailed);
            return;
        }

        if (!TryParseCampaignDate(CampaignStartsAtInput, out var startsAtUtc, out var startsError))
        {
            RunOnMain(() => ErrorMessage = startsError ?? AppResources.RewardsCampaignDateValidationFailed);
            return;
        }

        if (!TryParseCampaignDate(CampaignEndsAtInput, out var endsAtUtc, out var endsError))
        {
            RunOnMain(() => ErrorMessage = endsError ?? AppResources.RewardsCampaignDateValidationFailed);
            return;
        }

        if (startsAtUtc.HasValue && endsAtUtc.HasValue && startsAtUtc.Value > endsAtUtc.Value)
        {
            RunOnMain(() => ErrorMessage = AppResources.RewardsCampaignDateRangeValidationFailed);
            return;
        }

        if (SelectedCampaignChannel is null)
        {
            RunOnMain(() => ErrorMessage = AppResources.RewardsCampaignChannelValidationFailed);
            return;
        }

        var selectedChannels = SelectedCampaignChannel.Value;

        if (!TryNormalizeCampaignJsonObject(CampaignTargetingJsonInput, AppResources.RewardsCampaignTargetingValidationFailed, out var targetingJson, out var targetingError))
        {
            RunOnMain(() => ErrorMessage = targetingError ?? AppResources.RewardsCampaignTargetingValidationFailed);
            return;
        }

        if (!TryValidateCampaignTargetingSchemaRules(targetingJson, out var targetingSchemaError))
        {
            RunOnMain(() => ErrorMessage = targetingSchemaError ?? AppResources.RewardsCampaignTargetingValidationFailed);
            return;
        }

        if (!TryNormalizeCampaignJsonObject(CampaignPayloadJsonInput, AppResources.RewardsCampaignPayloadValidationFailed, out var payloadJson, out var payloadError))
        {
            RunOnMain(() => ErrorMessage = payloadError ?? AppResources.RewardsCampaignPayloadValidationFailed);
            return;
        }

        BeginBusyOperation();

        try
        {
            Result operationResult;

            if (IsCampaignEditMode)
            {
                operationResult = await _loyaltyService
                    .UpdateBusinessCampaignAsync(new UpdateBusinessCampaignRequest
                    {
                        Id = _editingCampaignId,
                        Name = normalizedCampaignName,
                        Title = CampaignTitleInput.Trim(),
                        Body = string.IsNullOrWhiteSpace(CampaignBodyInput) ? null : CampaignBodyInput.Trim(),
                        Channels = selectedChannels,
                        StartsAtUtc = startsAtUtc,
                        EndsAtUtc = endsAtUtc,
                        TargetingJson = targetingJson,
                        PayloadJson = payloadJson,
                        RowVersion = _editingCampaignRowVersion
                    }, CancellationToken.None)
                    .ConfigureAwait(false);
            }
            else
            {
                var createResult = await _loyaltyService
                    .CreateBusinessCampaignAsync(new CreateBusinessCampaignRequest
                    {
                        Name = normalizedCampaignName,
                        Title = CampaignTitleInput.Trim(),
                        Body = string.IsNullOrWhiteSpace(CampaignBodyInput) ? null : CampaignBodyInput.Trim(),
                        Channels = selectedChannels,
                        StartsAtUtc = startsAtUtc,
                        EndsAtUtc = endsAtUtc,
                        TargetingJson = targetingJson,
                        PayloadJson = payloadJson
                    }, CancellationToken.None)
                    .ConfigureAwait(false);

                operationResult = createResult.Succeeded ? Result.Ok() : Result.Fail(createResult.Error ?? AppResources.RewardsCampaignSaveFailed);
            }

            if (!operationResult.Succeeded)
            {
                RunOnMain(() => ErrorMessage = ResolveRewardsFailureMessage(operationResult.Error, AppResources.RewardsCampaignSaveFailed));
                return;
            }

            RunOnMain(() =>
            {
                ErrorMessage = null;
                ClearCampaignEditor();
            });

            await ReloadConfigurationAfterMutationAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            RunOnMain(() => ErrorMessage = ViewModelErrorMapper.ToUserMessage(ex, AppResources.RewardsCampaignSaveFailed));
        }
        finally
        {
            EndBusyOperation();
        }
    }

    /// <summary>
    /// Determines whether the entered campaign name conflicts with an existing campaign item in current list.
    /// </summary>
    /// <param name="candidateName">Normalized candidate campaign name.</param>
    /// <param name="editingCampaignId">Current edit target id; ignored during duplicate check.</param>
    /// <returns><c>true</c> when another campaign already uses the same name.</returns>
    private bool HasConflictingCampaignName(string candidateName, Guid editingCampaignId)
    {
        if (string.IsNullOrWhiteSpace(candidateName))
        {
            return false;
        }

        return _allCampaigns.Any(campaign =>
            campaign.Id != editingCampaignId &&
            string.Equals(campaign.Name?.Trim(), candidateName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Clears campaign editor and switches back to create mode.
    /// </summary>
    private Task NewCampaignAsync()
    {
        if (!CanManageRewards)
        {
            RunOnMain(() => ErrorMessage = AppResources.BusinessPermissionDeniedRewardEdit);
            return Task.CompletedTask;
        }

        RunOnMain(ClearCampaignEditor);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Refreshes authorization snapshot for reward-edit operations.
    /// </summary>
    private async Task RefreshAuthorizationAsync()
    {
        var snapshot = await _authorizationService.GetSnapshotAsync(CancellationToken.None).ConfigureAwait(false);

        RunOnMain(() =>
        {
            if (snapshot.Succeeded && snapshot.Value is not null)
            {
                OperatorRole = snapshot.Value.RoleDisplayName;
                CanManageRewards = snapshot.Value.CanEditRewards;
            }
            else
            {
                OperatorRole = "—";
                CanManageRewards = false;
            }

            RaiseCommandCanExecuteChanged();
        });
    }

    /// <summary>
    /// Validates and parses editor input into API payload values.
    /// </summary>
    private bool TryBuildEditorValues(out int pointsRequired, out decimal? rewardValue, out string? validationMessage)
    {
        pointsRequired = 0;
        rewardValue = null;
        validationMessage = null;

        if (!int.TryParse(PointsRequiredInput, out pointsRequired) || pointsRequired <= 0)
        {
            validationMessage = AppResources.RewardsPointsValidation;
            return false;
        }

        if (!RewardTypeOptions.Contains(SelectedRewardType, StringComparer.OrdinalIgnoreCase))
        {
            validationMessage = AppResources.RewardsTypeValidation;
            return false;
        }

        if (!string.IsNullOrWhiteSpace(RewardValueInput))
        {
            if (!decimal.TryParse(RewardValueInput, out var parsedValue))
            {
                validationMessage = AppResources.RewardsValueValidation;
                return false;
            }

            rewardValue = parsedValue;
        }

        return true;
    }


    /// <summary>
    /// Reloads tier list after a successful mutation without busy-guard short-circuiting.
    /// </summary>
    private async Task ReloadConfigurationAfterMutationAsync()
    {
        var response = await _loyaltyService
            .GetBusinessRewardConfigurationAsync(CancellationToken.None)
            .ConfigureAwait(false);

        if (!response.Succeeded || response.Value is null)
        {
            RunOnMain(() => ErrorMessage = response.Error ?? AppResources.RewardsLoadFailed);
            return;
        }

        var tiers = response.Value.RewardTiers
            .OrderBy(x => x.PointsRequired)
            .Select(RewardTierEditorItem.FromContract)
            .ToList();

        var campaigns = await LoadCampaignItemsAsync().ConfigureAwait(false);

        RunOnMain(() =>
        {
            RewardTiers.Clear();
            foreach (var tier in tiers)
            {
                RewardTiers.Add(tier);
            }

            ReplaceCampaigns(campaigns);
        });
    }

    /// <summary>
    /// Replaces internal campaign cache and applies the active UI filters.
    /// </summary>
    /// <param name="campaigns">Latest campaigns from API.</param>
    private void ReplaceCampaigns(IReadOnlyCollection<BusinessCampaignEditorItem> campaigns)
    {
        _allCampaigns.Clear();
        _allCampaigns.AddRange(campaigns);
        ApplyCampaignFilter();
    }

    /// <summary>
    /// Applies state/audience/query filters to the cached campaign list and updates visible list.
    /// </summary>
    private void ApplyCampaignFilter()
    {
        var stateKey = SelectedCampaignStateFilter?.StateKey;
        var audienceKindKey = SelectedCampaignAudienceFilter?.AudienceKindKey;
        var query = CampaignSearchQuery?.Trim();

        var filteredQuery = _allCampaigns.Where(campaign =>
            (string.IsNullOrWhiteSpace(stateKey) || string.Equals(campaign.CampaignState, stateKey, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(audienceKindKey) || string.Equals(campaign.AudienceKindKey, audienceKindKey, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(query) ||
             campaign.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             campaign.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             (!string.IsNullOrWhiteSpace(campaign.Body) && campaign.Body.Contains(query, StringComparison.OrdinalIgnoreCase))));

        var sortMode = SelectedCampaignSortOption?.Mode ?? CampaignSortMode.StartDateDesc;
        var filtered = sortMode switch
        {
            CampaignSortMode.StartDateAsc => filteredQuery
                .OrderBy(x => x.StartsAtUtc)
                .ThenBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            CampaignSortMode.TitleAsc => filteredQuery
                .OrderBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(x => x.StartsAtUtc)
                .ToList(),
            CampaignSortMode.TitleDesc => filteredQuery
                .OrderByDescending(x => x.Title, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(x => x.StartsAtUtc)
                .ToList(),
            _ => filteredQuery
                .OrderByDescending(x => x.StartsAtUtc)
                .ThenByDescending(x => x.IsActive)
                .ToList()
        };

        Campaigns.Clear();
        foreach (var campaign in filtered)
        {
            Campaigns.Add(campaign);
        }

        OnPropertyChanged(nameof(HasCampaigns));
        OnPropertyChanged(nameof(TotalCampaignCount));
        OnPropertyChanged(nameof(FilteredCampaignCount));
        OnPropertyChanged(nameof(HasCampaignDataset));
        OnPropertyChanged(nameof(CampaignFilterSummary));
        OnPropertyChanged(nameof(CampaignDiagnosticsVisibleCampaignsPreview));
        OnPropertyChanged(nameof(DraftCampaignCount));
        OnPropertyChanged(nameof(ScheduledCampaignCount));
        OnPropertyChanged(nameof(ActiveCampaignCount));
        OnPropertyChanged(nameof(ExpiredCampaignCount));
        OnPropertyChanged(nameof(CampaignStateMetricsSummary));
        OnPropertyChanged(nameof(AllCampaignMetricText));
        OnPropertyChanged(nameof(DraftCampaignMetricText));
        OnPropertyChanged(nameof(ScheduledCampaignMetricText));
        OnPropertyChanged(nameof(ActiveCampaignMetricText));
        OnPropertyChanged(nameof(ExpiredCampaignMetricText));
        OnPropertyChanged(nameof(JoinedMembersCampaignCount));
        OnPropertyChanged(nameof(TierSegmentCampaignCount));
        OnPropertyChanged(nameof(PointsThresholdCampaignCount));
        OnPropertyChanged(nameof(DateWindowCampaignCount));
        OnPropertyChanged(nameof(CampaignAudienceMetricsSummary));
        OnPropertyChanged(nameof(InAppOnlyCampaignCount));
        OnPropertyChanged(nameof(InAppAndPushCampaignCount));
        OnPropertyChanged(nameof(OtherChannelCampaignCount));
        OnPropertyChanged(nameof(CampaignChannelMetricsSummary));
        OnPropertyChanged(nameof(AllCampaignAudienceMetricText));
        OnPropertyChanged(nameof(JoinedMembersCampaignMetricText));
        OnPropertyChanged(nameof(TierSegmentCampaignMetricText));
        OnPropertyChanged(nameof(PointsThresholdCampaignMetricText));
        OnPropertyChanged(nameof(DateWindowCampaignMetricText));
        _campaignDiagnosticsSnapshotAtLocal = DateTimeOffset.Now;
        OnPropertyChanged(nameof(CampaignDiagnosticsSnapshotAtText));
        OnPropertyChanged(nameof(CampaignDiagnosticsFreshnessText));
        OnPropertyChanged(nameof(IsCampaignDiagnosticsSnapshotStale));
        OnPropertyChanged(nameof(HasCampaignDiagnosticsSnapshotAt));
        OnPropertyChanged(nameof(HasActiveCampaignFilters));
        OnPropertyChanged(nameof(HasCampaignSearchQuery));
        ClearCampaignFiltersCommand.RaiseCanExecuteChanged();
        ClearCampaignSearchCommand.RaiseCanExecuteChanged();
        ApplyCampaignStateFilterCommand.RaiseCanExecuteChanged();
        ApplyCampaignAudienceFilterCommand.RaiseCanExecuteChanged();
        ApplyCampaignTargetingPresetCommand.RaiseCanExecuteChanged();
        ApplyCampaignTargetingSchemaFixCommand.RaiseCanExecuteChanged();
        ResetCampaignTargetingFixMetricsCommand.RaiseCanExecuteChanged();
        CopyCampaignDiagnosticsCommand.RaiseCanExecuteChanged();
        ClearCampaignDiagnosticsStatusCommand.RaiseCanExecuteChanged();
    }

    /// <summary>
    /// Counts campaigns by state key in a case-insensitive manner.
    /// </summary>
    /// <param name="state">Lifecycle state key.</param>
    /// <returns>Number of campaigns with the requested state.</returns>
    private int CountCampaignsByState(string state)
    {
        return _allCampaigns.Count(c => string.Equals(c.CampaignState, state, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Counts campaigns by audience-kind key in a case-insensitive manner.
    /// </summary>
    /// <param name="audienceKind">Audience key from targeting metadata.</param>
    /// <returns>Number of campaigns mapped to the requested audience kind.</returns>
    private int CountCampaignsByAudienceKind(string audienceKind)
    {
        return _allCampaigns.Count(c => string.Equals(c.AudienceKindKey, audienceKind, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Clears campaign search/state/audience filters and re-displays full campaign list.
    /// </summary>
    private Task ClearCampaignFiltersAsync()
    {
        if (IsBusy)
        {
            return Task.CompletedTask;
        }

        RunOnMain(() =>
        {
            CampaignSearchQuery = string.Empty;
            SelectedCampaignStateFilter = CampaignStateFilterOptions.FirstOrDefault();
            SelectedCampaignAudienceFilter = CampaignAudienceFilterOptions.FirstOrDefault();
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears only campaign search query while preserving state/sort filters.
    /// </summary>
    private Task ClearCampaignSearchAsync()
    {
        if (IsBusy)
        {
            return Task.CompletedTask;
        }

        RunOnMain(() => CampaignSearchQuery = string.Empty);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Applies the requested lifecycle state directly from KPI action chips.
    /// </summary>
    /// <param name="state">Target lifecycle state key.</param>
    private Task ApplyCampaignStateFilterAsync(string? state)
    {
        if (IsBusy)
        {
            return Task.CompletedTask;
        }

        RunOnMain(() =>
        {
            // Empty state means "all lifecycle states" while preserving
            // any active search/sort criteria.
            if (string.IsNullOrWhiteSpace(state))
            {
                SelectedCampaignStateFilter = CampaignStateFilterOptions.FirstOrDefault();
                return;
            }

            // Toggle behavior for KPI chips.
            // Re-tapping the currently active state removes that state filter and
            // returns to the "all states" view.
            if (string.Equals(SelectedCampaignStateFilter?.StateKey, state, StringComparison.OrdinalIgnoreCase))
            {
                SelectedCampaignStateFilter = CampaignStateFilterOptions.FirstOrDefault();
                return;
            }

            SelectedCampaignStateFilter = CampaignStateFilterOptions
                .FirstOrDefault(option => string.Equals(option.StateKey, state, StringComparison.OrdinalIgnoreCase))
                ?? SelectedCampaignStateFilter;
        });

        return Task.CompletedTask;
    }


    /// <summary>
    /// Applies the requested audience kind directly from audience KPI action chips.
    /// </summary>
    /// <param name="audienceKind">Target audience key.</param>
    private Task ApplyCampaignAudienceFilterAsync(string? audienceKind)
    {
        if (IsBusy)
        {
            return Task.CompletedTask;
        }

        RunOnMain(() =>
        {
            if (string.IsNullOrWhiteSpace(audienceKind))
            {
                SelectedCampaignAudienceFilter = CampaignAudienceFilterOptions.FirstOrDefault();
                return;
            }

            if (string.Equals(SelectedCampaignAudienceFilter?.AudienceKindKey, audienceKind, StringComparison.OrdinalIgnoreCase))
            {
                SelectedCampaignAudienceFilter = CampaignAudienceFilterOptions.FirstOrDefault();
                return;
            }

            SelectedCampaignAudienceFilter = CampaignAudienceFilterOptions
                .FirstOrDefault(option => string.Equals(option.AudienceKindKey, audienceKind, StringComparison.OrdinalIgnoreCase))
                ?? SelectedCampaignAudienceFilter;
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// Applies a targeting JSON preset in the campaign editor to speed up common audience setups.
    /// </summary>
    /// <param name="presetKey">Audience preset key requested by the operator.</param>
    private Task ApplyCampaignTargetingPresetAsync(string? presetKey)
    {
        if (IsBusy || !CanManageRewards)
        {
            return Task.CompletedTask;
        }

        var normalizedKey = presetKey?.Trim();
        var targetingJson = normalizedKey switch
        {
            "JoinedMembers" => """{"audienceKind":"JoinedMembers"}""",
            "TierSegment" => """{"audienceKind":"TierSegment","tier":"Gold"}""",
            "PointsThreshold" => """{"audienceKind":"PointsThreshold","minimumPoints":100}""",
            "DateWindow" => """{"audienceKind":"DateWindow","eligibleFromUtc":"2026-01-01T00:00:00Z","eligibleToUtc":"2026-01-31T23:59:59Z"}""",
            _ => "{}"
        };

        RunOnMain(() =>
        {
            CampaignTargetingJsonInput = targetingJson;
            ErrorMessage = null;
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// Loads business campaigns for lightweight lifecycle controls in Rewards screen.
    /// </summary>
    private async Task<List<BusinessCampaignEditorItem>> LoadCampaignItemsAsync()
    {
        var campaignsResult = await _loyaltyService
            .GetBusinessCampaignsAsync(page: 1, pageSize: CampaignListPageSize, CancellationToken.None)
            .ConfigureAwait(false);

        if (!campaignsResult.Succeeded || campaignsResult.Value is null)
        {
            return new List<BusinessCampaignEditorItem>();
        }

        return campaignsResult.Value.Items
            .OrderByDescending(x => x.StartsAtUtc)
            .ThenByDescending(x => x.IsActive)
            .Select(BusinessCampaignEditorItem.FromContract)
            .ToList();
    }

    /// <summary>
    /// Toggles campaign activation state for selected campaign item.
    /// </summary>
    private async Task ToggleCampaignActivationAsync(BusinessCampaignEditorItem? campaign)
    {
        if (campaign is null)
        {
            return;
        }

        if (IsBusy)
        {
            return;
        }

        if (!CanManageRewards)
        {
            RunOnMain(() => ErrorMessage = AppResources.BusinessPermissionDeniedRewardEdit);
            return;
        }

        BeginBusyOperation();

        try
        {
            var result = await _loyaltyService
                .SetBusinessCampaignActivationAsync(new SetCampaignActivationRequest
                {
                    Id = campaign.Id,
                    IsActive = !campaign.IsActive,
                    RowVersion = campaign.RowVersion
                }, CancellationToken.None)
                .ConfigureAwait(false);

            if (!result.Succeeded)
            {
                RunOnMain(() => ErrorMessage = result.Error ?? AppResources.RewardsCampaignToggleFailed);
                return;
            }

            RunOnMain(() => ErrorMessage = null);
            await ReloadConfigurationAfterMutationAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            RunOnMain(() => ErrorMessage = AppResources.RewardsCampaignToggleFailed);
        }
        finally
        {
            EndBusyOperation();
        }
    }

    /// <summary>
    /// Copies a compact campaign diagnostics snapshot for operations/support handoff.
    /// </summary>
    private async Task CopyCampaignDiagnosticsAsync()
    {
        if (!HasCampaignDataset || IsBusy)
        {
            return;
        }

        try
        {
            var payload = string.Join(
                Environment.NewLine,
                CampaignFilterSummary,
                CampaignStateMetricsSummary,
                CampaignAudienceMetricsSummary,
                CampaignChannelMetricsSummary,
                CampaignDiagnosticsSnapshotAtText,
                CampaignDiagnosticsFreshnessText,
                CampaignDiagnosticsVisibleCampaignsPreview,
                string.Format(
                    CultureInfo.InvariantCulture,
                    AppResources.RewardsCampaignDiagnosticsAppliedFiltersFormat,
                    SelectedCampaignStateFilter?.DisplayName ?? AppResources.RewardsCampaignStateFilterAll,
                    SelectedCampaignAudienceFilter?.DisplayName ?? AppResources.RewardsCampaignAudienceFilterAll,
                    string.IsNullOrWhiteSpace(CampaignSearchQuery) ? AppResources.RewardsCampaignDiagnosticsSearchEmpty : CampaignSearchQuery,
                    SelectedCampaignSortOption?.DisplayName ?? AppResources.RewardsCampaignSortStartDateDesc));

            await Clipboard.Default.SetTextAsync(payload).ConfigureAwait(false);
            RunOnMain(() => CampaignDiagnosticsCopyStatus = AppResources.RewardsCampaignDiagnosticsCopied);
        }
        catch
        {
            RunOnMain(() => CampaignDiagnosticsCopyStatus = AppResources.RewardsCampaignDiagnosticsCopyFailed);
        }
    }

    /// <summary>
    /// Clears transient copy status banner used by campaign diagnostics export action.
    /// </summary>
    private Task ClearCampaignDiagnosticsStatusAsync()
    {
        if (IsBusy)
        {
            return Task.CompletedTask;
        }

        RunOnMain(() => CampaignDiagnosticsCopyStatus = null);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears editor state and exits edit mode.
    /// </summary>
    private void ClearEditor()
    {
        _editingRewardTierId = Guid.Empty;
        _editingRowVersion = Array.Empty<byte>();

        PointsRequiredInput = string.Empty;
        SelectedRewardType = RewardTypeFreeItem;
        RewardValueInput = null;
        DescriptionInput = null;
        AllowSelfRedemption = false;

        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(SaveButtonText));
        OnPropertyChanged(nameof(CanDeleteReward));
        DeleteCommand.RaiseCanExecuteChanged();
    }


    /// <summary>
    /// Clears campaign editor state and exits campaign edit mode.
    /// </summary>
    private void ClearCampaignEditor()
    {
        _editingCampaignId = Guid.Empty;
        _editingCampaignRowVersion = Array.Empty<byte>();
        _editingCampaignChannels = 1;
        _editingCampaignTargetingJson = "{}";
        _editingCampaignPayloadJson = "{}";

        CampaignNameInput = string.Empty;
        CampaignTitleInput = string.Empty;
        CampaignBodyInput = null;
        CampaignStartsAtInput = null;
        CampaignEndsAtInput = null;
        CampaignTargetingJsonInput = _editingCampaignTargetingJson;
        CampaignPayloadJsonInput = _editingCampaignPayloadJson;
        SelectedCampaignChannel = CampaignChannelOptions.FirstOrDefault();

        OnPropertyChanged(nameof(IsCampaignEditMode));
        OnPropertyChanged(nameof(CampaignSaveButtonText));
    }

    /// <summary>
    /// Marks the view model as busy and refreshes command states on the UI thread.
    /// </summary>
    private void BeginBusyOperation()
    {
        RunOnMain(() =>
        {
            IsBusy = true;
            RaiseCommandCanExecuteChanged();
        });
    }

    /// <summary>
    /// Clears busy state and refreshes command states on the UI thread.
    /// </summary>
    private void EndBusyOperation()
    {
        RunOnMain(() =>
        {
            IsBusy = false;
            RaiseCommandCanExecuteChanged();
        });
    }

    private void RaiseCommandCanExecuteChanged()
    {
        RefreshCommand.RaiseCanExecuteChanged();
        SaveCommand.RaiseCanExecuteChanged();
        DeleteCommand.RaiseCanExecuteChanged();
        CreateNewCommand.RaiseCanExecuteChanged();
        ToggleCampaignActivationCommand.RaiseCanExecuteChanged();
        SaveCampaignCommand.RaiseCanExecuteChanged();
        NewCampaignCommand.RaiseCanExecuteChanged();
        ClearCampaignFiltersCommand.RaiseCanExecuteChanged();
        ClearCampaignSearchCommand.RaiseCanExecuteChanged();
        ApplyCampaignStateFilterCommand.RaiseCanExecuteChanged();
        ApplyCampaignAudienceFilterCommand.RaiseCanExecuteChanged();
        ApplyCampaignTargetingPresetCommand.RaiseCanExecuteChanged();
        ApplyCampaignTargetingSchemaFixCommand.RaiseCanExecuteChanged();
        ResetCampaignTargetingFixMetricsCommand.RaiseCanExecuteChanged();
        CopyCampaignDiagnosticsCommand.RaiseCanExecuteChanged();
        ClearCampaignDiagnosticsStatusCommand.RaiseCanExecuteChanged();
    }

    /// <summary>
    /// Maps raw reward-management failures to friendly UI messages without leaking transport/server internals.
    /// </summary>
    private static string ResolveRewardsFailureMessage(string? error, string fallback)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            return fallback;
        }

        if (LooksLikeRewardPermissionFailure(error))
        {
            return AppResources.BusinessPermissionDeniedRewardEdit;
        }

        return fallback;
    }

    /// <summary>
    /// Detects common permission-denied markers from API/business-auth layers.
    /// </summary>
    private static bool LooksLikeRewardPermissionFailure(string error)
    {
        return error.Contains("permission", StringComparison.OrdinalIgnoreCase) ||
               error.Contains("forbidden", StringComparison.OrdinalIgnoreCase) ||
               error.Contains("403", StringComparison.OrdinalIgnoreCase) ||
               error.Contains("unauthorized", StringComparison.OrdinalIgnoreCase);
    }
}
