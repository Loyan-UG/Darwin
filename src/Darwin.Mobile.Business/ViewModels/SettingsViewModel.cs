using System;
using System.Threading.Tasks;
using Darwin.Mobile.Business.Constants;
using Darwin.Mobile.Business.Resources;
using Darwin.Mobile.Business.Services.Identity;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Navigation;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Business.ViewModels;

/// <summary>
/// Business settings hub view model.
/// </summary>
public sealed class SettingsViewModel : BaseViewModel
{
    private readonly INavigationService _navigationService;
    private readonly IBusinessAccessService _businessAccessService;
    private string _businessOperationalStatusLabel = AppResources.HomeUnavailableValue;
    private string _businessOperationalStatusMessage = string.Empty;
    private string _setupChecklistSummary = string.Empty;
    private bool _loadedStatusOnce;

    public SettingsViewModel(
        INavigationService navigationService,
        IBusinessAccessService businessAccessService)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _businessAccessService = businessAccessService ?? throw new ArgumentNullException(nameof(businessAccessService));

        OpenProfileCommand = new AsyncCommand(OpenProfileAsync, () => !IsBusy);
        OpenBusinessMediaCommand = new AsyncCommand(OpenBusinessMediaAsync, () => !IsBusy);
        OpenChangePasswordCommand = new AsyncCommand(OpenChangePasswordAsync, () => !IsBusy);
        OpenStaffAccessBadgeCommand = new AsyncCommand(OpenStaffAccessBadgeAsync, () => !IsBusy);
        OpenSubscriptionCommand = new AsyncCommand(OpenSubscriptionAsync, () => !IsBusy);
        OpenLegalHubCommand = new AsyncCommand(OpenLegalHubAsync, () => !IsBusy);
        OpenAccountDeletionCommand = new AsyncCommand(OpenAccountDeletionAsync, () => !IsBusy);
    }

    public string BusinessOperationalStatusLabel
    {
        get => _businessOperationalStatusLabel;
        private set => SetProperty(ref _businessOperationalStatusLabel, value);
    }

    public string BusinessOperationalStatusMessage
    {
        get => _businessOperationalStatusMessage;
        private set
        {
            if (SetProperty(ref _businessOperationalStatusMessage, value))
            {
                OnPropertyChanged(nameof(HasBusinessOperationalStatusMessage));
            }
        }
    }

    public bool HasBusinessOperationalStatusMessage => !string.IsNullOrWhiteSpace(BusinessOperationalStatusMessage);

    public string SetupChecklistSummary
    {
        get => _setupChecklistSummary;
        private set
        {
            if (SetProperty(ref _setupChecklistSummary, value))
            {
                OnPropertyChanged(nameof(HasSetupChecklistSummary));
            }
        }
    }

    public bool HasSetupChecklistSummary => !string.IsNullOrWhiteSpace(SetupChecklistSummary);

    public AsyncCommand OpenProfileCommand { get; }

    public AsyncCommand OpenBusinessMediaCommand { get; }

    public AsyncCommand OpenChangePasswordCommand { get; }

    public AsyncCommand OpenStaffAccessBadgeCommand { get; }

    public AsyncCommand OpenSubscriptionCommand { get; }

    public AsyncCommand OpenLegalHubCommand { get; }

    public AsyncCommand OpenAccountDeletionCommand { get; }

    public override async Task OnAppearingAsync()
    {
        if (_loadedStatusOnce)
        {
            return;
        }

        _loadedStatusOnce = true;
        await LoadBusinessOperationalStatusAsync().ConfigureAwait(false);
    }

    private async Task LoadBusinessOperationalStatusAsync()
    {
        try
        {
            var result = await _businessAccessService.GetCurrentAccessStateAsync(default).ConfigureAwait(false);
            RunOnMain(() =>
            {
                if (!result.Succeeded || result.Value is null)
                {
                    BusinessOperationalStatusLabel = AppResources.HomeUnavailableValue;
                    BusinessOperationalStatusMessage = result.Error ?? AppResources.BusinessAccessStateLoadFailed;
                    SetupChecklistSummary = string.Empty;
                    return;
                }

                BusinessOperationalStatusLabel = BusinessAccessStateUiMapper.GetOperationalStatusLabel(result.Value);
                BusinessOperationalStatusMessage = BusinessAccessStateUiMapper.GetOperationalStatusMessage(result.Value);
                SetupChecklistSummary = result.Value.SetupIncompleteItemCount > 0
                    ? BusinessAccessStateUiMapper.BuildSetupChecklistSummary(result.Value)
                    : string.Empty;
            });
        }
        catch
        {
            RunOnMain(() =>
            {
                BusinessOperationalStatusLabel = AppResources.HomeUnavailableValue;
                BusinessOperationalStatusMessage = AppResources.BusinessAccessStateLoadFailed;
                SetupChecklistSummary = string.Empty;
            });
        }
    }

    private async Task OpenProfileAsync() => await NavigateAsync(Routes.SettingsProfile).ConfigureAwait(false);

    private async Task OpenBusinessMediaAsync() => await NavigateAsync(Routes.SettingsBusinessMedia).ConfigureAwait(false);

    private async Task OpenChangePasswordAsync() => await NavigateAsync(Routes.SettingsChangePassword).ConfigureAwait(false);

    private async Task OpenStaffAccessBadgeAsync() => await NavigateAsync(Routes.SettingsStaffAccessBadge).ConfigureAwait(false);

    private async Task OpenSubscriptionAsync() => await NavigateAsync(Routes.SettingsSubscription).ConfigureAwait(false);

    private async Task OpenLegalHubAsync() => await NavigateAsync(Routes.SettingsLegalHub).ConfigureAwait(false);

    private async Task OpenAccountDeletionAsync() => await NavigateAsync(Routes.SettingsAccountDeletion).ConfigureAwait(false);

    private async Task NavigateAsync(string route)
    {
        if (IsBusy)
        {
            return;
        }

        RunOnMain(() =>
        {
            IsBusy = true;
            RaiseCommandStates();
        });
        try
        {
            await _navigationService.GoToAsync(route);
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                RaiseCommandStates();
            });
        }
    }

    /// <summary>
    /// Updates every settings navigation command so only one route transition runs at a time.
    /// </summary>
    private void RaiseCommandStates()
    {
        OpenProfileCommand.RaiseCanExecuteChanged();
        OpenBusinessMediaCommand.RaiseCanExecuteChanged();
        OpenChangePasswordCommand.RaiseCanExecuteChanged();
        OpenStaffAccessBadgeCommand.RaiseCanExecuteChanged();
        OpenSubscriptionCommand.RaiseCanExecuteChanged();
        OpenLegalHubCommand.RaiseCanExecuteChanged();
        OpenAccountDeletionCommand.RaiseCanExecuteChanged();
    }
}
