using Darwin.Mobile.Business.Constants;
using Darwin.Mobile.Business.Services.Platform;
using Darwin.Mobile.Business.ViewModels;
using Darwin.Mobile.Business.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Threading.Tasks;

namespace Darwin.Mobile.Business;

/// <summary>
/// Shell coordinator for the Business app.
/// </summary>
public sealed partial class AppShell : Shell
{
    private readonly string _initialRoute;
    private bool _startupNavigationDone;
    private bool _isResettingShellStack;
    private bool _allowScannerPageNavigation;
    private bool _isScannerActionRunning;

    public AppShell(string initialRoute)
    {
        InitializeComponent();
        _initialRoute = string.IsNullOrWhiteSpace(initialRoute) ? $"//{Routes.Login}" : initialRoute;

        Routing.RegisterRoute(Routes.Home, typeof(HomePage));
        Routing.RegisterRoute(Routes.Scanner, typeof(ScannerPage));
        Routing.RegisterRoute(Routes.Login, typeof(LoginPage));
        Routing.RegisterRoute(Routes.InvitationAcceptance, typeof(AcceptInvitationPage));
        Routing.RegisterRoute(Routes.Session, typeof(SessionPage));
        Routing.RegisterRoute(Routes.Dashboard, typeof(DashboardPage));
        Routing.RegisterRoute(Routes.Rewards, typeof(RewardsPage));
        Routing.RegisterRoute(Routes.RewardTierEditor, typeof(RewardTierEditorPage));
        Routing.RegisterRoute(Routes.RewardCampaignEditor, typeof(RewardCampaignEditorPage));
        Routing.RegisterRoute(Routes.Notifications, typeof(NotificationsPage));

        Routing.RegisterRoute(Routes.SettingsProfile, typeof(ProfilePage));
        Routing.RegisterRoute(Routes.SettingsBusinessMedia, typeof(BusinessMediaPage));
        Routing.RegisterRoute(Routes.SettingsChangePassword, typeof(ChangePasswordPage));
        Routing.RegisterRoute(Routes.SettingsStaffAccessBadge, typeof(StaffAccessBadgePage));
        Routing.RegisterRoute(Routes.SettingsSubscription, typeof(SubscriptionPage));
        Routing.RegisterRoute(Routes.SettingsLegalHub, typeof(LegalHubPage));
        Routing.RegisterRoute(Routes.SettingsAccountDeletion, typeof(AccountDeletionPage));

        Navigating += OnShellNavigating;
        Navigated += OnShellNavigated;

        BusinessSystemBars.SetStatusBarColor(Color.FromArgb("#FEDB42"), true);
    }

    public async Task ResetCurrentTabToRootAsync()
    {
        var route = CurrentItem?.CurrentItem?.CurrentItem?.Route;
        if (string.IsNullOrWhiteSpace(route))
        {
            return;
        }

        await ResetTabToRootAsync(route);
        if (route.Equals(Routes.Scanner, StringComparison.OrdinalIgnoreCase))
        {
            await StartScannerTabActionAsync(showScannerPageAfterCancel: true);
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_startupNavigationDone)
        {
            return;
        }

        _startupNavigationDone = true;

        Dispatcher.Dispatch(async () =>
        {
            try
            {
                await GoToAsync(_initialRoute);
            }
            catch (Exception ex)
            {
                _ = ex;
                System.Diagnostics.Debug.WriteLine("Business startup navigation failed.");
            }
        });
    }

    private async Task ResetTabToRootAsync(string route)
    {
        if (_isResettingShellStack)
        {
            return;
        }

        _isResettingShellStack = true;

        try
        {
            if (route.Equals(Routes.Scanner, StringComparison.OrdinalIgnoreCase))
            {
                _allowScannerPageNavigation = true;
            }

            await GoToAsync($"//{route}", false);
            if (Navigation.NavigationStack.Count > 1)
            {
                await Navigation.PopToRootAsync(false);
            }
        }
        finally
        {
            _allowScannerPageNavigation = false;
            _isResettingShellStack = false;
        }
    }

    private async Task StartScannerTabActionAsync(bool showScannerPageAfterCancel)
    {
        if (_isScannerActionRunning)
        {
            return;
        }

        _isScannerActionRunning = true;

        try
        {
            var scannerViewModel = Handler?.MauiContext?.Services.GetService<ScannerViewModel>();
            if (scannerViewModel is null)
            {
                if (showScannerPageAfterCancel)
                {
                    await ResetTabToRootAsync(Routes.Scanner);
                }

                return;
            }

            await scannerViewModel.OnAppearingAsync();
            await scannerViewModel.StartScanAsync();
            if (!IsCurrentLocation(Routes.Session) && showScannerPageAfterCancel)
            {
                await ResetTabToRootAsync(Routes.Scanner);
            }
        }
        finally
        {
            _isScannerActionRunning = false;
        }
    }

    private async void OnShellNavigating(object? sender, ShellNavigatingEventArgs e)
    {
        if (_isResettingShellStack || !e.CanCancel)
        {
            return;
        }

        var currentLocation = CurrentState?.Location.OriginalString ?? string.Empty;
        var resetTarget = GetRootResetTarget(e, currentLocation);
        if (resetTarget is null)
        {
            return;
        }

        if (resetTarget.Equals(Routes.Scanner, StringComparison.OrdinalIgnoreCase) && !_allowScannerPageNavigation)
        {
            e.Cancel();
            await Task.Yield();
            await StartScannerTabActionAsync(showScannerPageAfterCancel: true);
            return;
        }

        e.Cancel();
        await Task.Yield();
        await ResetTabToRootAsync(resetTarget);
    }

    private static string? GetRootResetTarget(ShellNavigatingEventArgs e, string currentLocation)
    {
        var targetRoot = GetTargetRootRoute(e);
        if (targetRoot is null)
        {
            return null;
        }

        return IsRootLocation(currentLocation, targetRoot) ? null : targetRoot;
    }

    private static string? GetTargetRootRoute(ShellNavigatingEventArgs e)
    {
        var targetLocation = e.Target?.Location.OriginalString ?? string.Empty;

        foreach (var route in RootRoutes)
        {
            if (targetLocation.Equals($"//{route}", StringComparison.OrdinalIgnoreCase)
                || targetLocation.Equals(route, StringComparison.OrdinalIgnoreCase)
                || targetLocation.EndsWith($"/{route}", StringComparison.OrdinalIgnoreCase))
            {
                return route;
            }
        }

        return null;
    }

    private static bool IsRootLocation(string location, string route)
    {
        return location.Equals($"//{route}", StringComparison.OrdinalIgnoreCase)
               || location.Equals(route, StringComparison.OrdinalIgnoreCase)
               || location.EndsWith($"/{route}", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCurrentLocation(string route)
    {
        var location = Shell.Current?.CurrentState?.Location.OriginalString ?? string.Empty;
        return location.Contains(route, StringComparison.OrdinalIgnoreCase);
    }

    private void OnShellNavigated(object? sender, ShellNavigatedEventArgs e)
    {
        var location = CurrentState?.Location.OriginalString ?? string.Empty;
        var headerColor = Color.FromArgb("#FEDB42");
        var neutralColor = Color.FromArgb("#F4F4F4");

        var useYellowStatusBar = !location.Contains(Routes.Login, StringComparison.OrdinalIgnoreCase)
                                 && !location.Contains(Routes.InvitationAcceptance, StringComparison.OrdinalIgnoreCase);

        BusinessSystemBars.SetStatusBarColor(useYellowStatusBar ? headerColor : neutralColor, true);
    }

    private static readonly string[] RootRoutes =
    [
        Routes.Home,
        Routes.Scanner,
        Routes.Dashboard,
        Routes.Rewards,
        Routes.Settings
    ];
}
