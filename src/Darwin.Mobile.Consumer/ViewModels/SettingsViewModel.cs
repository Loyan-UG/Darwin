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
    }

    public AsyncCommand OpenProfileCommand { get; }

    public AsyncCommand OpenChangePasswordCommand { get; }

    private async Task OpenProfileAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await _navigationService.GoToAsync(Routes.ProfileEdit);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task OpenChangePasswordAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await _navigationService.GoToAsync(Routes.ChangePassword);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
