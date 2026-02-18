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
/// Shell composition for the Business app.
///
/// Responsibilities:
/// - Register dynamic routes.
/// - Navigate to Login as startup route.
/// - Handle logout consistently (local token clear + reset route).
/// - Hide logout button on Login route to keep auth UI clean.
/// </summary>
public sealed partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register non-tab routes
        Routing.RegisterRoute(Routes.Login, typeof(LoginPage));
        Routing.RegisterRoute(Routes.Session, typeof(SessionPage));

        // Keep toolbar state synced with current route.
        Navigated += OnShellNavigated;

        // App starts in login mode.
        Dispatcher.Dispatch(async () => { await GoToAsync($"//{Routes.Login}"); });
    }

    /// <summary>
    /// Handles logout from the toolbar.
    /// </summary>
    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        try
        {
            // Resolve auth service from current MAUI service provider.
            // This keeps AppShell constructor simple and independent from App DI wiring style.
            var authService = Handler?.MauiContext?.Services?.GetService<IAuthService>();
            if (authService is not null)
            {
                await authService.LogoutAsync(CancellationToken.None);
            }
        }
        finally
        {
            // Always return to login route even if remote revoke fails.
            await GoToAsync($"//{Routes.Login}");
        }
    }

    /// <summary>
    /// Updates toolbar visibility per route.
    /// Logout should be hidden on Login screen to avoid confusing auth UX.
    /// </summary>
    private void OnShellNavigated(object? sender, ShellNavigatedEventArgs e)
    {
        var uri = e.Current?.Location?.OriginalString ?? string.Empty;
        var isLoginRoute = uri.Contains(Routes.Login, StringComparison.OrdinalIgnoreCase);

        if (LogoutToolbarItem is not null)
        {
            LogoutToolbarItem.IsEnabled = !isLoginRoute;
        }
    }
}
