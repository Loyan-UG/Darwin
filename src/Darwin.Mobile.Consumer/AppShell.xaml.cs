using Darwin.Mobile.Consumer.Constants;
using Darwin.Mobile.Consumer.Services.Navigation;
using Darwin.Mobile.Consumer.Views;
using Darwin.Mobile.Shared.Services;
using Microsoft.Maui.Controls;
using System;
using System.Threading;

namespace Darwin.Mobile.Consumer;

/// <summary>
/// Code-behind for the main shell. Registers dynamic routes and handles logout.
/// </summary>
public partial class AppShell : Shell
{
    private readonly IAuthService _authService;
    private readonly IAppRootNavigator _appRootNavigator;

    public AppShell(IAuthService authService, IAppRootNavigator appRootNavigator)
    {
        InitializeComponent();
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _appRootNavigator = appRootNavigator ?? throw new ArgumentNullException(nameof(appRootNavigator));

        Routing.RegisterRoute($"{Routes.BusinessDetail}/{{businessId}}", typeof(BusinessDetailPage));
        Routing.RegisterRoute(Routes.ProfileEdit, typeof(ProfilePage));
        Routing.RegisterRoute(Routes.ChangePassword, typeof(ChangePasswordPage));
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        try
        {
            await _authService.LogoutAsync(CancellationToken.None);
        }
        finally
        {
            // Always reset to the login flow even if remote logout fails.
            await _appRootNavigator.NavigateToLoginAsync();
        }
    }
}
