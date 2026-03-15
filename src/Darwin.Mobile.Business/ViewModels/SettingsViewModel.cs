using System;
using System.Threading.Tasks;
using Darwin.Mobile.Business.Constants;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Navigation;
using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Business.ViewModels;

/// <summary>
/// Business settings hub view model.
/// 
/// This class keeps navigation intent explicit and future-proof:
/// - Profile action goes to a dedicated profile edit page.
/// - Password action goes to a dedicated password change page.
/// - Staff badge action opens a rotating QR code for internal access workflows.
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
    }

    public AsyncCommand OpenProfileCommand { get; }

    public AsyncCommand OpenChangePasswordCommand { get; }

    public AsyncCommand OpenStaffAccessBadgeCommand { get; }

    private async Task OpenProfileAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await _navigationService.GoToAsync(Routes.SettingsProfile);
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
            await _navigationService.GoToAsync(Routes.SettingsChangePassword);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task OpenStaffAccessBadgeAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await _navigationService.GoToAsync(Routes.SettingsStaffAccessBadge);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
