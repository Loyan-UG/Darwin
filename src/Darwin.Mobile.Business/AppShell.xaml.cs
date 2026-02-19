using Darwin.Mobile.Business.Constants;
using Darwin.Mobile.Business.Views;
using Darwin.Mobile.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Business;

/// <summary>
/// Shell coordinator for the Business app.
/// 
/// Why this implementation:
/// - Startup navigation is performed after Shell appears (not in constructor) to avoid Android Shell route errors.
/// - Logout button visibility is controlled by add/remove from ToolbarItems (ToolbarItem has no IsVisible in MAUI).
/// - The logout button is declared once in XAML; code-behind never creates a second one.
/// </summary>
public sealed partial class AppShell : Shell
{
    private bool _startupNavigationDone;
    private bool _logoutAttached = true;

    /// <summary>
    /// Initializes shell and registers route handlers.
    /// </summary>
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(Routes.Home, typeof(HomePage));
        Routing.RegisterRoute(Routes.Scanner, typeof(ScannerPage));
        Routing.RegisterRoute(Routes.Login, typeof(LoginPage));
        Routing.RegisterRoute(Routes.Session, typeof(SessionPage));

        Navigated += OnShellNavigated;
    }

    /// <summary>
    /// Performs one-time startup navigation when shell is fully ready.
    /// </summary>
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
            await GoToAsync($"//{Routes.Login}");
        });
    }

    /// <summary>
    /// Handles user logout.
    /// </summary>
    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        try
        {
            var auth = Handler?.MauiContext?.Services?.GetService<IAuthService>();
            if (auth is not null)
            {
                await auth.LogoutAsync(CancellationToken.None);
            }
        }
        finally
        {
            await GoToAsync($"//{Routes.Login}");
        }
    }

    /// <summary>
    /// Updates toolbar composition according to current route.
    /// </summary>
    private void OnShellNavigated(object? sender, ShellNavigatedEventArgs e)
    {
        var route = e.Current?.Location?.OriginalString ?? string.Empty;
        var onLogin = route.Contains(Routes.Login, StringComparison.OrdinalIgnoreCase);

        if (onLogin)
        {
            RemoveLogoutToolbarItemIfNeeded();
        }
        else
        {
            AddLogoutToolbarItemIfNeeded();
        }
    }

    /// <summary>
    /// Removes logout item only if currently attached.
    /// </summary>
    private void RemoveLogoutToolbarItemIfNeeded()
    {
        if (!_logoutAttached)
        {
            return;
        }

        ToolbarItems.Remove(LogoutToolbarItem);
        _logoutAttached = false;
    }

    /// <summary>
    /// Adds logout item only if currently detached.
    /// </summary>
    private void AddLogoutToolbarItemIfNeeded()
    {
        if (_logoutAttached)
        {
            return;
        }

        if (!ToolbarItems.Contains(LogoutToolbarItem))
        {
            ToolbarItems.Add(LogoutToolbarItem);
        }

        _logoutAttached = true;
    }
}
