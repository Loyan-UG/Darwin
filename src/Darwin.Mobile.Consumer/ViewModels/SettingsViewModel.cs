using System;
using System.Threading.Tasks;
using Darwin.Mobile.Consumer.Constants;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Navigation;
using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// Settings hub view model.
/// This page intentionally acts as a stable container for future settings/actions.
/// </summary>
public sealed class SettingsViewModel : BaseViewModel
{
    private readonly INavigationService _navigationService;

    public SettingsViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        OpenProfileCommand = new AsyncCommand(OpenProfileAsync, () => !IsBusy);
        OpenChangePasswordCommand = new AsyncCommand(OpenChangePasswordAsync, () => !IsBusy);
        OpenMemberCommerceCommand = new AsyncCommand(OpenMemberCommerceAsync, () => !IsBusy);
        OpenMemberPreferencesCommand = new AsyncCommand(OpenMemberPreferencesAsync, () => !IsBusy);
        OpenLegalHubCommand = new AsyncCommand(OpenLegalHubAsync, () => !IsBusy);
        OpenAccountDeletionCommand = new AsyncCommand(OpenAccountDeletionAsync, () => !IsBusy);
    }

    public AsyncCommand OpenProfileCommand { get; }

    public AsyncCommand OpenChangePasswordCommand { get; }

    public AsyncCommand OpenMemberCommerceCommand { get; }

    public AsyncCommand OpenMemberPreferencesCommand { get; }

    public AsyncCommand OpenLegalHubCommand { get; }

    public AsyncCommand OpenAccountDeletionCommand { get; }

    private async Task OpenProfileAsync()
        => await NavigateAsync(Routes.ProfileEdit);

    private async Task OpenChangePasswordAsync()
        => await NavigateAsync(Routes.ChangePassword);

    private async Task OpenMemberCommerceAsync()
        => await NavigateAsync(Routes.MemberCommerce);

    private async Task OpenMemberPreferencesAsync()
        => await NavigateAsync(Routes.MemberPreferences);

    private async Task OpenLegalHubAsync()
        => await NavigateAsync(Routes.LegalHub);

    private async Task OpenAccountDeletionAsync()
        => await NavigateAsync(Routes.AccountDeletion);

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
    /// Updates settings navigation commands after busy-state transitions.
    /// </summary>
    private void RaiseCommandStates()
    {
        OpenProfileCommand.RaiseCanExecuteChanged();
        OpenChangePasswordCommand.RaiseCanExecuteChanged();
        OpenMemberCommerceCommand.RaiseCanExecuteChanged();
        OpenMemberPreferencesCommand.RaiseCanExecuteChanged();
        OpenLegalHubCommand.RaiseCanExecuteChanged();
        OpenAccountDeletionCommand.RaiseCanExecuteChanged();
    }
}
