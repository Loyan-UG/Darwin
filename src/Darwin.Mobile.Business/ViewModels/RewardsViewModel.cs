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
public sealed class RewardsViewModel : BaseViewModel
{
    private readonly ILoyaltyService _loyaltyService;
    private readonly IBusinessAuthorizationService _authorizationService;
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
    private CampaignChannelOption? _selectedCampaignChannel;
    private string _campaignSearchQuery = string.Empty;
    private CampaignStateFilterOption? _selectedCampaignStateFilter;
    private CampaignAudienceFilterOption? _selectedCampaignAudienceFilter;
    private CampaignSortOption? _selectedCampaignSortOption;

    private const int CampaignListPageSize = 50;

    private const string RewardTypeFreeItem = "FreeItem";
    private const string RewardTypePercentDiscount = "PercentDiscount";
    private const string RewardTypeAmountDiscount = "AmountDiscount";

    public RewardsViewModel(ILoyaltyService loyaltyService, IBusinessAuthorizationService authorizationService)
    {
        _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));

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
    /// Localized summary line for audience segmentation quick metrics.
    /// </summary>
    public string CampaignAudienceMetricsSummary => string.Format(
        CultureInfo.InvariantCulture,
        AppResources.RewardsCampaignAudienceMetricsFormat,
        JoinedMembersCampaignCount,
        TierSegmentCampaignCount,
        PointsThresholdCampaignCount,
        DateWindowCampaignCount);

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
        set => SetProperty(ref _campaignTargetingJsonInput, value);
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

        IsBusy = true;
        RaiseCommandCanExecuteChanged();

        try
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
            RunOnMain(() => ErrorMessage = $"{AppResources.RewardsLoadFailed} {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            RaiseCommandCanExecuteChanged();
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

        IsBusy = true;
        RaiseCommandCanExecuteChanged();

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
                RunOnMain(() => ErrorMessage = operationResult.Error ?? AppResources.RewardsSaveFailed);
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
            RunOnMain(() => ErrorMessage = $"{AppResources.RewardsSaveFailed} {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            RaiseCommandCanExecuteChanged();
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

        IsBusy = true;
        RaiseCommandCanExecuteChanged();

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
                RunOnMain(() => ErrorMessage = result.Error ?? AppResources.RewardsDeleteFailed);
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
            RunOnMain(() => ErrorMessage = $"{AppResources.RewardsDeleteFailed} {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            RaiseCommandCanExecuteChanged();
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
    /// Validates and normalizes campaign JSON fields before API mutations.
    /// </summary>
    private static bool TryNormalizeCampaignJson(string? jsonInput, string validationMessage, out string normalizedJson, out string? error)
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

        if (!TryNormalizeCampaignJson(CampaignTargetingJsonInput, AppResources.RewardsCampaignTargetingValidationFailed, out var targetingJson, out var targetingError))
        {
            RunOnMain(() => ErrorMessage = targetingError ?? AppResources.RewardsCampaignTargetingValidationFailed);
            return;
        }

        if (!TryNormalizeCampaignJson(CampaignPayloadJsonInput, AppResources.RewardsCampaignPayloadValidationFailed, out var payloadJson, out var payloadError))
        {
            RunOnMain(() => ErrorMessage = payloadError ?? AppResources.RewardsCampaignPayloadValidationFailed);
            return;
        }

        IsBusy = true;
        RaiseCommandCanExecuteChanged();

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
                RunOnMain(() => ErrorMessage = operationResult.Error ?? AppResources.RewardsCampaignSaveFailed);
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
            RunOnMain(() => ErrorMessage = $"{AppResources.RewardsCampaignSaveFailed} {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            RaiseCommandCanExecuteChanged();
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
        OnPropertyChanged(nameof(CampaignFilterSummary));
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
        OnPropertyChanged(nameof(AllCampaignAudienceMetricText));
        OnPropertyChanged(nameof(JoinedMembersCampaignMetricText));
        OnPropertyChanged(nameof(TierSegmentCampaignMetricText));
        OnPropertyChanged(nameof(PointsThresholdCampaignMetricText));
        OnPropertyChanged(nameof(DateWindowCampaignMetricText));
        OnPropertyChanged(nameof(HasActiveCampaignFilters));
        OnPropertyChanged(nameof(HasCampaignSearchQuery));
        ClearCampaignFiltersCommand.RaiseCanExecuteChanged();
        ClearCampaignSearchCommand.RaiseCanExecuteChanged();
        ApplyCampaignStateFilterCommand.RaiseCanExecuteChanged();
        ApplyCampaignAudienceFilterCommand.RaiseCanExecuteChanged();
        ApplyCampaignTargetingPresetCommand.RaiseCanExecuteChanged();
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

        IsBusy = true;
        RaiseCommandCanExecuteChanged();

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
        catch (Exception ex)
        {
            RunOnMain(() => ErrorMessage = $"{AppResources.RewardsCampaignToggleFailed} {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            RaiseCommandCanExecuteChanged();
        }
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
    }
}

/// <summary>
/// Editable item model used by Rewards page list.
/// </summary>
public sealed class RewardTierEditorItem
{
    public Guid RewardTierId { get; init; }
    public int PointsRequired { get; init; }
    public string RewardType { get; init; } = string.Empty;
    public decimal? RewardValue { get; init; }
    public string? Description { get; init; }
    public bool AllowSelfRedemption { get; init; }
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();

    public static RewardTierEditorItem FromContract(BusinessRewardTierConfigItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return new RewardTierEditorItem
        {
            RewardTierId = item.RewardTierId,
            PointsRequired = item.PointsRequired,
            RewardType = item.RewardType,
            RewardValue = item.RewardValue,
            Description = item.Description,
            AllowSelfRedemption = item.AllowSelfRedemption,
            RowVersion = item.RowVersion ?? Array.Empty<byte>()
        };
    }
}


/// <summary>
/// Represents a selectable channel combination in campaign editor UI.
/// </summary>
public sealed class CampaignChannelOption
{
    public CampaignChannelOption(short value, string label)
    {
        Value = value;
        Label = label;
    }

    public short Value { get; }
    public string Label { get; }

    public override string ToString() => Label;
}

/// <summary>
/// Represents a selectable lifecycle-state filter option for campaign list.
/// </summary>
public sealed class CampaignStateFilterOption
{
    public CampaignStateFilterOption(string stateKey, string label)
    {
        StateKey = stateKey;
        Label = label;
    }

    /// <summary>
    /// Contract state key; empty means "all states".
    /// </summary>
    public string StateKey { get; }

    /// <summary>
    /// Localized display label.
    /// </summary>
    public string Label { get; }

    public override string ToString() => Label;
}

/// <summary>
/// Represents a selectable audience-kind filter option for campaign list.
/// </summary>
public sealed class CampaignAudienceFilterOption
{
    public CampaignAudienceFilterOption(string audienceKindKey, string label)
    {
        AudienceKindKey = audienceKindKey;
        Label = label;
    }

    /// <summary>
    /// Contract audience key; empty means "all audiences".
    /// </summary>
    public string AudienceKindKey { get; }

    /// <summary>
    /// Localized display label.
    /// </summary>
    public string Label { get; }

    public override string ToString() => Label;
}

/// <summary>
/// Supported sort modes for campaign list projection in mobile business UI.
/// </summary>
public enum CampaignSortMode
{
    StartDateDesc = 0,
    StartDateAsc = 1,
    TitleAsc = 2,
    TitleDesc = 3
}

/// <summary>
/// Represents a selectable campaign sort option.
/// </summary>
public sealed class CampaignSortOption
{
    public CampaignSortOption(CampaignSortMode mode, string label)
    {
        Mode = mode;
        Label = label;
    }

    /// <summary>
    /// Internal sort mode.
    /// </summary>
    public CampaignSortMode Mode { get; }

    /// <summary>
    /// Localized display label.
    /// </summary>
    public string Label { get; }

    public override string ToString() => Label;
}

/// <summary>
/// Lightweight campaign item used by business rewards screen.
/// </summary>
public sealed class BusinessCampaignEditorItem
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string CampaignState { get; init; } = PromotionCampaignState.Draft;
    public bool IsActive { get; init; }
    public DateTime? StartsAtUtc { get; init; }
    public DateTime? EndsAtUtc { get; init; }
    public string? Body { get; init; }
    public short Channels { get; init; }
    public string TargetingJson { get; init; } = "{}";
    public string PayloadJson { get; init; } = "{}";
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
    public string AudienceKindKey { get; init; } = PromotionAudienceKind.JoinedMembers;

    /// <summary>
    /// Gets a compact, localized audience summary derived from <see cref="TargetingJson"/>.
    /// This summary helps business operators quickly verify campaign segmentation directly in
    /// the list view without opening each campaign editor.
    /// </summary>
    public string AudienceSummary => BuildAudienceSummary(TargetingJson);

    public string ActivationButtonText => IsActive ? AppResources.RewardsCampaignDeactivateButton : AppResources.RewardsCampaignActivateButton;

    public static BusinessCampaignEditorItem FromContract(BusinessCampaignItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var audienceKindKey = ResolveAudienceKindKey(item.TargetingJson);

        return new BusinessCampaignEditorItem
        {
            Id = item.Id,
            Name = item.Name,
            Title = item.Title,
            CampaignState = item.CampaignState,
            IsActive = item.IsActive,
            StartsAtUtc = item.StartsAtUtc,
            EndsAtUtc = item.EndsAtUtc,
            Body = item.Body,
            Channels = item.Channels,
            TargetingJson = item.TargetingJson ?? "{}",
            PayloadJson = item.PayloadJson ?? "{}",
            RowVersion = item.RowVersion ?? Array.Empty<byte>(),
            AudienceKindKey = audienceKindKey
        };
    }

    /// <summary>
    /// Builds a concise audience/eligibility caption by parsing campaign targeting JSON.
    /// </summary>
    /// <param name="targetingJson">Raw targeting JSON as stored in campaign payload.</param>
    /// <returns>A localized one-line summary suitable for list display.</returns>
    private static string BuildAudienceSummary(string? targetingJson)
    {
        if (string.IsNullOrWhiteSpace(targetingJson))
        {
            return AppResources.RewardsCampaignAudienceSummaryDefault;
        }

        try
        {
            using var document = JsonDocument.Parse(targetingJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return AppResources.RewardsCampaignAudienceSummaryDefault;
            }

            var root = document.RootElement;
            var audienceKind = ResolveAudienceKind(root);
            var minPoints = TryGetInt(root, "minPoints");
            var maxPoints = TryGetInt(root, "maxPoints");
            var tierKey = TryGetString(root, "tierKey");

            // Support targeting payloads that store rule details under an array-based structure.
            if (root.TryGetProperty("eligibilityRules", out var rules) &&
                rules.ValueKind == JsonValueKind.Array &&
                rules.GetArrayLength() > 0)
            {
                var firstRule = rules[0];
                if (firstRule.ValueKind == JsonValueKind.Object)
                {
                    audienceKind ??= ResolveAudienceKind(firstRule);
                    minPoints ??= TryGetInt(firstRule, "minPoints");
                    maxPoints ??= TryGetInt(firstRule, "maxPoints");
                    tierKey ??= TryGetString(firstRule, "tierKey");
                }
            }

            var audienceLabel = ResolveAudienceLabel(audienceKind);
            var eligibilityLabel = BuildEligibilityLabel(minPoints, maxPoints, tierKey);

            if (string.IsNullOrWhiteSpace(eligibilityLabel))
            {
                return string.Format(CultureInfo.CurrentCulture, AppResources.RewardsCampaignAudienceSummaryFormat, audienceLabel);
            }

            return string.Format(CultureInfo.CurrentCulture, AppResources.RewardsCampaignAudienceSummaryWithEligibilityFormat, audienceLabel, eligibilityLabel);
        }
        catch (JsonException)
        {
            return AppResources.RewardsCampaignAudienceSummaryDefault;
        }
    }

    /// <summary>
    /// Resolves canonical audience kind key from targeting JSON for filtering scenarios.
    /// </summary>
    private static string ResolveAudienceKindKey(string? targetingJson)
    {
        if (string.IsNullOrWhiteSpace(targetingJson))
        {
            return PromotionAudienceKind.JoinedMembers;
        }

        try
        {
            using var document = JsonDocument.Parse(targetingJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return PromotionAudienceKind.JoinedMembers;
            }

            return ResolveAudienceKind(document.RootElement) ?? PromotionAudienceKind.JoinedMembers;
        }
        catch (JsonException)
        {
            return PromotionAudienceKind.JoinedMembers;
        }
    }

    /// <summary>
    /// Reads audience kind from root object or the first eligibility rule object.
    /// </summary>
    private static string? ResolveAudienceKind(JsonElement root)
    {
        var direct = TryGetString(root, "audienceKind") ?? TryGetString(root, "kind");
        if (!string.IsNullOrWhiteSpace(direct))
        {
            return direct;
        }

        if (root.TryGetProperty("eligibilityRules", out var rules) &&
            rules.ValueKind == JsonValueKind.Array &&
            rules.GetArrayLength() > 0)
        {
            var firstRule = rules[0];
            if (firstRule.ValueKind == JsonValueKind.Object)
            {
                return TryGetString(firstRule, "audienceKind") ?? TryGetString(firstRule, "kind");
            }
        }

        return null;
    }

    /// <summary>
    /// Resolves raw audience kind into a user-facing localized label.
    /// </summary>
    private static string ResolveAudienceLabel(string? audienceKind)
    {
        if (string.Equals(audienceKind, PromotionAudienceKind.TierSegment, StringComparison.OrdinalIgnoreCase))
        {
            return AppResources.RewardsCampaignAudienceTierSegment;
        }

        if (string.Equals(audienceKind, PromotionAudienceKind.PointsThreshold, StringComparison.OrdinalIgnoreCase))
        {
            return AppResources.RewardsCampaignAudiencePointsThreshold;
        }

        if (string.Equals(audienceKind, PromotionAudienceKind.DateWindow, StringComparison.OrdinalIgnoreCase))
        {
            return AppResources.RewardsCampaignAudienceDateWindow;
        }

        return AppResources.RewardsCampaignAudienceJoinedMembers;
    }

    /// <summary>
    /// Produces a compact eligibility clause from optional rule fields.
    /// </summary>
    private static string? BuildEligibilityLabel(int? minPoints, int? maxPoints, string? tierKey)
    {
        if (!string.IsNullOrWhiteSpace(tierKey))
        {
            return string.Format(CultureInfo.CurrentCulture, AppResources.RewardsCampaignEligibilityTierFormat, tierKey);
        }

        if (minPoints.HasValue && maxPoints.HasValue)
        {
            return string.Format(CultureInfo.CurrentCulture, AppResources.RewardsCampaignEligibilityRangeFormat, minPoints.Value, maxPoints.Value);
        }

        if (minPoints.HasValue)
        {
            return string.Format(CultureInfo.CurrentCulture, AppResources.RewardsCampaignEligibilityMinFormat, minPoints.Value);
        }

        if (maxPoints.HasValue)
        {
            return string.Format(CultureInfo.CurrentCulture, AppResources.RewardsCampaignEligibilityMaxFormat, maxPoints.Value);
        }

        return null;
    }

    /// <summary>
    /// Reads an optional string property from a JSON object element.
    /// </summary>
    private static string? TryGetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return property.GetString();
    }

    /// <summary>
    /// Reads an optional integer property from a JSON object element.
    /// Supports both numeric and string-encoded integer values for compatibility.
    /// </summary>
    private static int? TryGetInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var numericValue))
        {
            return numericValue;
        }

        if (property.ValueKind == JsonValueKind.String &&
            int.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
        {
            return parsedValue;
        }

        return null;
    }
}
