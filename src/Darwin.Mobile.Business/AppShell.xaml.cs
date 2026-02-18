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
/// Responsibilities:
/// - Register navigation routes.
/// - Perform startup navigation only after Shell is ready.
/// - Provide a consistent logout entry point via toolbar item.
/// - Keep toolbar state aligned with current route (hide on Login, show elsewhere).
///
/// Why this design:
/// - Absolute navigation to login inside constructor can fail on Android in some Shell states.
/// - ToolbarItem in MAUI does not support IsVisible, so we use add/remove pattern.
/// </summary>
public sealed partial class AppShell : Shell
{
    private bool _startupNavigationDone;
    private bool _logoutToolbarAttached;

    // We create toolbar item in code to avoid XAML coupling and keep behavior deterministic.
    private readonly ToolbarItem _logoutToolbarItem;

    /// <summary>
    /// Initializes shell, registers routes and wires route-change behavior.
    /// </summary>
    public AppShell()
    {
        InitializeComponent();

        // Register all routes used by Shell navigation.
        Routing.RegisterRoute(Routes.Home, typeof(HomePage));
        Routing.RegisterRoute(Routes.Scanner, typeof(ScannerPage));
        Routing.RegisterRoute(Routes.Login, typeof(LoginPage));
        Routing.RegisterRoute(Routes.Session, typeof(SessionPage));

        // Create logout toolbar action once and manage its presence by route.
        _logoutToolbarItem = new ToolbarItem
        {
            Text = "Logout",
            Priority = 0,
            Order = ToolbarItemOrder.Primary
        };
        _logoutToolbarItem.Clicked += OnLogoutClicked;

        // Track navigation transitions so toolbar can be adjusted per route.
        Navigated += OnShellNavigated;
    }

    /// <summary>
    /// Performs one-time startup navigation after Shell appears.
    ///
    /// Important:
    /// - We intentionally do NOT navigate in constructor.
    /// - This avoids Shell route-stack exceptions on some Android runs.
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
            // Start in login flow in a stable lifecycle moment.
            await GoToAsync($"//{Routes.Login}");
        });
    }

    /// <summary>
    /// Handles logout action from toolbar.
    ///
    /// Behavior:
    /// - Try to revoke/clear auth via IAuthService.
    /// - Always navigate to Login even if network revoke fails.
    /// </summary>
    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        try
        {
            var authService = Handler?.MauiContext?.Services?.GetService<IAuthService>();
            if (authService is not null)
            {
                await authService.LogoutAsync(CancellationToken.None);
            }
        }
        finally
        {
            await GoToAsync($"//{Routes.Login}");
        }
    }

    /// <summary>
    /// Keeps toolbar composition in sync with current route.
    /// </summary>
    private void OnShellNavigated(object? sender, ShellNavigatedEventArgs e)
    {
        var current = e.Current?.Location?.OriginalString ?? string.Empty;
        var onLoginRoute = current.Contains(Routes.Login, StringComparison.OrdinalIgnoreCase);

        if (onLoginRoute)
        {
            RemoveLogoutToolbarItemIfNeeded();
        }
        else
        {
            AddLogoutToolbarItemIfNeeded();
        }
    }

    /// <summary>
    /// Removes logout item if currently attached.
    /// </summary>
    private void RemoveLogoutToolbarItemIfNeeded()
    {
        if (!_logoutToolbarAttached)
        {
            return;
        }

        ToolbarItems.Remove(_logoutToolbarItem);
        _logoutToolbarAttached = false;
    }

    /// <summary>
    /// Adds logout item if currently detached.
    /// </summary>
    private void AddLogoutToolbarItemIfNeeded()
    {
        if (_logoutToolbarAttached)
        {
            return;
        }

        ToolbarItems.Add(_logoutToolbarItem);
        _logoutToolbarAttached = true;
    }
}
