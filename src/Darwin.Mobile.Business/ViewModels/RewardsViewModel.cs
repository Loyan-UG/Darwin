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

    private const string RewardTypeFreeItem = "FreeItem";
    private const string RewardTypePercentDiscount = "PercentDiscount";
    private const string RewardTypeAmountDiscount = "AmountDiscount";

    public RewardsViewModel(ILoyaltyService loyaltyService, IBusinessAuthorizationService authorizationService)
    {
        _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));

        RewardTiers = new ObservableCollection<RewardTierEditorItem>();
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
    /// True when current editor is bound to an existing tier.
    /// </summary>
    public bool IsEditMode => _editingRewardTierId != Guid.Empty;

    /// <summary>
    /// Label shown on save button based on create/update mode.
    /// </summary>
    public string SaveButtonText => IsEditMode ? AppResources.RewardsUpdateButton : AppResources.RewardsCreateButton;

    public AsyncCommand RefreshCommand { get; }
    public AsyncCommand SaveCommand { get; }
    public AsyncCommand DeleteCommand { get; }
    public AsyncCommand CreateNewCommand { get; }

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

            RunOnMain(() =>
            {
                ErrorMessage = null;
                RewardTiers.Clear();
                foreach (var tier in tiers)
                {
                    RewardTiers.Add(tier);
                }
            });

            if (!IsEditMode)
            {
                RunOnMain(ClearEditor);
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
            DeleteCommand.RaiseCanExecuteChanged();
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

        RunOnMain(() =>
        {
            RewardTiers.Clear();
            foreach (var tier in tiers)
            {
                RewardTiers.Add(tier);
            }
        });
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
        DeleteCommand.RaiseCanExecuteChanged();
    }

    private void RaiseCommandCanExecuteChanged()
    {
        RefreshCommand.RaiseCanExecuteChanged();
        SaveCommand.RaiseCanExecuteChanged();
        DeleteCommand.RaiseCanExecuteChanged();
        CreateNewCommand.RaiseCanExecuteChanged();
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
