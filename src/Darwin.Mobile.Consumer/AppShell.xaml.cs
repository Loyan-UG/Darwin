using Darwin.Mobile.Consumer.Constants;
using Darwin.Mobile.Consumer.Services.Platform;
using Darwin.Mobile.Consumer.Views;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Threading.Tasks;

namespace Darwin.Mobile.Consumer;

/// <summary>
/// Code-behind for the main shell. Registers dynamic routes.
/// </summary>
public partial class AppShell : Shell
{
    private bool _isResettingShellStack;

    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute($"{Routes.BusinessDetail}/{{businessId}}", typeof(BusinessDetailPage));
        Routing.RegisterRoute(Routes.ProfileEdit, typeof(ProfilePage));
        Routing.RegisterRoute(Routes.MemberCommerce, typeof(MemberCommercePage));
        Routing.RegisterRoute(Routes.MemberAddresses, typeof(MemberAddressesPage));
        Routing.RegisterRoute(Routes.MemberPreferences, typeof(MemberPreferencesPage));
        Routing.RegisterRoute(Routes.MemberCustomerContext, typeof(MemberCustomerContextPage));
        Routing.RegisterRoute(Routes.ChangePassword, typeof(ChangePasswordPage));
        Routing.RegisterRoute(Routes.LegalHub, typeof(LegalHubPage));
        Routing.RegisterRoute(Routes.AccountDeletion, typeof(AccountDeletionPage));

        Navigating += OnShellNavigating;
        Navigated += OnShellNavigated;

        ConsumerSystemBars.SetStatusBarColor(Color.FromArgb("#FEDB42"), true);
    }

    public async Task ResetCurrentTabToRootAsync()
    {
        var route = CurrentItem?.CurrentItem?.CurrentItem?.Route;
        if (string.IsNullOrWhiteSpace(route))
        {
            return;
        }

        await ResetTabToRootAsync(route);
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
            await GoToAsync($"//{route}", false);
            if (Navigation.NavigationStack.Count > 1)
            {
                await Navigation.PopToRootAsync(false);
            }
        }
        finally
        {
            _isResettingShellStack = false;
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

        if (IsRootLocation(currentLocation, targetRoot))
        {
            return null;
        }

        return targetRoot;
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

    private static readonly string[] RootRoutes =
    [
        Routes.Discover,
        Routes.Qr,
        Routes.Rewards,
        Routes.Feed,
        Routes.Settings
    ];

    private void OnShellNavigated(object? sender, ShellNavigatedEventArgs e)
    {
        var location = CurrentState?.Location.OriginalString ?? string.Empty;
        var headerColor = Color.FromArgb("#FEDB42");
        var neutralColor = Color.FromArgb("#F4F4F4");

        var useYellowStatusBar = location.Contains(Routes.Discover, StringComparison.OrdinalIgnoreCase)
                                 || location.Contains(Routes.Qr, StringComparison.OrdinalIgnoreCase)
                                 || location.Contains(Routes.Rewards, StringComparison.OrdinalIgnoreCase)
                                 || location.Contains(Routes.Feed, StringComparison.OrdinalIgnoreCase)
                                 || location.Contains(Routes.Settings, StringComparison.OrdinalIgnoreCase)
                                 || location.Contains(Routes.BusinessDetail, StringComparison.OrdinalIgnoreCase);

        ConsumerSystemBars.SetStatusBarColor(useYellowStatusBar ? headerColor : neutralColor, true);
    }
}
