using System;
using System.Threading.Tasks;
using Darwin.Mobile.Business.Constants;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Navigation;
using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Business.ViewModels;

/// <summary>
/// Business settings hub view model.
/// </summary>
public sealed class SettingsViewModel : BaseViewModel
{
    private readonly INavigationService _navigationService;

    public SettingsViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

        OpenProfileCommand = new AsyncCommand(OpenProfileAsync, () => !IsBusy);
        OpenChangePasswordCommand = new AsyncCommand(OpenChangePasswordAsync, () => !IsBusy);
        OpenStaffAccessBadgeCommand = new AsyncCommand(OpenStaffAccessBadgeAsync, () => !IsBusy);
        OpenSubscriptionCommand = new AsyncCommand(OpenSubscriptionAsync, () => !IsBusy);
        OpenLegalHubCommand = new AsyncCommand(OpenLegalHubAsync, () => !IsBusy);
        OpenAccountDeletionCommand = new AsyncCommand(OpenAccountDeletionAsync, () => !IsBusy);
    }

    public AsyncCommand OpenProfileCommand { get; }

    public AsyncCommand OpenChangePasswordCommand { get; }

    public AsyncCommand OpenStaffAccessBadgeCommand { get; }

    public AsyncCommand OpenSubscriptionCommand { get; }

    public AsyncCommand OpenLegalHubCommand { get; }

    public AsyncCommand OpenAccountDeletionCommand { get; }

    private async Task OpenProfileAsync() => await NavigateAsync(Routes.SettingsProfile).ConfigureAwait(false);

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

        IsBusy = true;
        try
        {
            await _navigationService.GoToAsync(route);
        }
        finally
        {
            IsBusy = false;
            OpenProfileCommand.RaiseCanExecuteChanged();
            OpenChangePasswordCommand.RaiseCanExecuteChanged();
            OpenStaffAccessBadgeCommand.RaiseCanExecuteChanged();
            OpenSubscriptionCommand.RaiseCanExecuteChanged();
            OpenLegalHubCommand.RaiseCanExecuteChanged();
            OpenAccountDeletionCommand.RaiseCanExecuteChanged();
        }
    }
}
