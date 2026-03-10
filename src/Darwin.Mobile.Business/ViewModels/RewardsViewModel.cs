using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

        RefreshCommand = new AsyncCommand(LoadConfigurationAsync, () => !IsBusy);
        SaveCommand = new AsyncCommand(SaveAsync, () => !IsBusy && CanManageRewards);
        DeleteCommand = new AsyncCommand(DeleteAsync, () => !IsBusy && IsEditMode && CanManageRewards);
        CreateNewCommand = new AsyncCommand(CreateNewAsync, () => !IsBusy && CanManageRewards);
        ToggleCampaignActivationCommand = new AsyncCommand<BusinessCampaignEditorItem>(ToggleCampaignActivationAsync, campaign => !IsBusy && CanManageRewards && campaign is not null);
        SaveCampaignCommand = new AsyncCommand(SaveCampaignAsync, () => !IsBusy && CanManageRewards);
        NewCampaignCommand = new AsyncCommand(NewCampaignAsync, () => !IsBusy && CanManageRewards);
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

                Campaigns.Clear();
                foreach (var campaign in campaigns)
                {
                    Campaigns.Add(campaign);
                }
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
            _editingCampaignTargetingJson = campaign.TargetingJson;
            _editingCampaignPayloadJson = campaign.PayloadJson;

            CampaignNameInput = campaign.Name;
            CampaignTitleInput = campaign.Title;
            CampaignBodyInput = campaign.Body;

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
                        Name = CampaignNameInput.Trim(),
                        Title = CampaignTitleInput.Trim(),
                        Body = string.IsNullOrWhiteSpace(CampaignBodyInput) ? null : CampaignBodyInput.Trim(),
                        Channels = _editingCampaignChannels,
                        TargetingJson = _editingCampaignTargetingJson,
                        PayloadJson = _editingCampaignPayloadJson,
                        RowVersion = _editingCampaignRowVersion
                    }, CancellationToken.None)
                    .ConfigureAwait(false);
            }
            else
            {
                var createResult = await _loyaltyService
                    .CreateBusinessCampaignAsync(new CreateBusinessCampaignRequest
                    {
                        Name = CampaignNameInput.Trim(),
                        Title = CampaignTitleInput.Trim(),
                        Body = string.IsNullOrWhiteSpace(CampaignBodyInput) ? null : CampaignBodyInput.Trim(),
                        Channels = 1,
                        TargetingJson = "{}",
                        PayloadJson = "{}"
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

            Campaigns.Clear();
            foreach (var campaign in campaigns)
            {
                Campaigns.Add(campaign);
            }
        });
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

    public string ActivationButtonText => IsActive ? AppResources.RewardsCampaignDeactivateButton : AppResources.RewardsCampaignActivateButton;

    public static BusinessCampaignEditorItem FromContract(BusinessCampaignItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

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
            RowVersion = item.RowVersion ?? Array.Empty<byte>()
        };
    }
}
