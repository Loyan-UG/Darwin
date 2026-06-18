using System;
using System.Threading;
using Darwin.Mobile.Consumer.ViewModels;
using Darwin.Mobile.Consumer.Resources;
using Darwin.Mobile.Consumer.Services.Navigation;
using Darwin.Mobile.Consumer.Services.Notifications;
using Darwin.Mobile.Shared.Services;
using Microsoft.Maui.Controls;

namespace Darwin.Mobile.Consumer.Views;

/// <summary>
/// Settings hub page.
/// </summary>
public partial class SettingsPage : ContentPage
{
    private static readonly TimeSpan LogoutTimeout = TimeSpan.FromSeconds(10);

    private readonly SettingsViewModel _viewModel;
    private readonly IAuthService _authService;
    private readonly IAppRootNavigator _appRootNavigator;
    private readonly IConsumerPushRegistrationCoordinator _pushRegistrationCoordinator;

    public SettingsPage(
        SettingsViewModel viewModel,
        IAuthService authService,
        IAppRootNavigator appRootNavigator,
        IConsumerPushRegistrationCoordinator pushRegistrationCoordinator)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _appRootNavigator = appRootNavigator ?? throw new ArgumentNullException(nameof(appRootNavigator));
        _pushRegistrationCoordinator = pushRegistrationCoordinator ?? throw new ArgumentNullException(nameof(pushRegistrationCoordinator));
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            await _viewModel.OnAppearingAsync();
        }
        catch
        {
            // Settings summary refresh failures are handled by the view model.
        }
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        var confirmed = await DisplayAlertAsync(
            AppResources.LogoutConfirmationTitle,
            AppResources.LogoutConfirmationMessage,
            AppResources.CommonYes,
            AppResources.CommonNo);

        if (!confirmed)
        {
            return;
        }

        try
        {
            using var timeout = new CancellationTokenSource(LogoutTimeout);
            await _authService.LogoutAsync(timeout.Token);
        }
        finally
        {
            _pushRegistrationCoordinator.ResetCachedRegistrationState();

            try
            {
                await _appRootNavigator.NavigateToLoginAsync();
            }
            catch (Exception ex)
            {
                _ = ex;
                System.Diagnostics.Debug.WriteLine("Consumer logout navigation failed.");
            }
        }
    }
}
