using Darwin.Mobile.Consumer.Constants;
using Darwin.Mobile.Consumer.Services.Navigation;
using Darwin.Mobile.Consumer.Services.Notifications;
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
    private static readonly TimeSpan LogoutTimeout = TimeSpan.FromSeconds(10);

    private readonly IAuthService _authService;
    private readonly IAppRootNavigator _appRootNavigator;
    private readonly IConsumerPushRegistrationCoordinator _pushRegistrationCoordinator;

    public AppShell(IAuthService authService, IAppRootNavigator appRootNavigator, IConsumerPushRegistrationCoordinator pushRegistrationCoordinator)
    {
        InitializeComponent();
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _appRootNavigator = appRootNavigator ?? throw new ArgumentNullException(nameof(appRootNavigator));
        _pushRegistrationCoordinator = pushRegistrationCoordinator ?? throw new ArgumentNullException(nameof(pushRegistrationCoordinator));

        Routing.RegisterRoute($"{Routes.BusinessDetail}/{{businessId}}", typeof(BusinessDetailPage));
        Routing.RegisterRoute(Routes.ProfileEdit, typeof(ProfilePage));
        Routing.RegisterRoute(Routes.MemberCommerce, typeof(MemberCommercePage));
        Routing.RegisterRoute(Routes.MemberAddresses, typeof(MemberAddressesPage));
        Routing.RegisterRoute(Routes.MemberPreferences, typeof(MemberPreferencesPage));
        Routing.RegisterRoute(Routes.MemberCustomerContext, typeof(MemberCustomerContextPage));
        Routing.RegisterRoute(Routes.ChangePassword, typeof(ChangePasswordPage));
        Routing.RegisterRoute(Routes.LegalHub, typeof(LegalHubPage));
        Routing.RegisterRoute(Routes.AccountDeletion, typeof(AccountDeletionPage));
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        try
        {
            using var timeout = new CancellationTokenSource(LogoutTimeout);
            // Logout is user-initiated and must clear the local session even when the current page is closing.
            await _authService.LogoutAsync(timeout.Token);
        }
        finally
        {
            _pushRegistrationCoordinator.ResetCachedRegistrationState();

            try
            {
                // Always reset to the login flow even if remote logout fails.
                await _appRootNavigator.NavigateToLoginAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Consumer logout navigation failed: {ex}");
            }
        }
    }
}
