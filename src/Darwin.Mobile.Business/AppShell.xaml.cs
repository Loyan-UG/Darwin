using Darwin.Mobile.Business.Constants;
using Darwin.Mobile.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Business;

/// <summary>
/// Shell controller for Business app.
/// 
/// Key decisions:
/// - Avoid async navigation in constructor.
/// - Navigate to Login only after shell is ready (OnAppearing + one-time guard).
/// - Do not use ToolbarItem.IsVisible (not available in MAUI); use add/remove pattern instead.
/// </summary>
public sealed partial class AppShell : Shell
{
    private bool _initialNavigationDone;
    private bool _logoutItemAttached = true;

    public AppShell()
    {
        InitializeComponent();

        Navigated += OnShellNavigated;
    }

    /// <summary>
    /// Perform initial navigation after shell is visible to avoid ShellUriHandler exceptions.
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_initialNavigationDone)
        {
            return;
        }

        _initialNavigationDone = true;

        Dispatcher.Dispatch(async () =>
        {
            // Keep startup deterministic: always start on login route.
            await GoToAsync($"//{Routes.Login}");
        });
    }

    /// <summary>
    /// Handles logout from toolbar.
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
            // Even if remote logout fails, force local navigation to login.
            await GoToAsync($"//{Routes.Login}");
        }
    }

    /// <summary>
    /// Update toolbar composition for current route.
    /// </summary>
    private void OnShellNavigated(object? sender, ShellNavigatedEventArgs e)
    {
        var current = e.Current?.Location?.OriginalString ?? string.Empty;
        var onLogin = current.Contains(Routes.Login, StringComparison.OrdinalIgnoreCase);

        if (onLogin)
        {
            RemoveLogoutToolbarItemIfNeeded();
        }
        else
        {
            AddLogoutToolbarItemIfNeeded();
        }
    }

    private void RemoveLogoutToolbarItemIfNeeded()
    {
        if (!_logoutItemAttached)
        {
            return;
        }

        ToolbarItems.Remove(LogoutToolbarItem);
        _logoutItemAttached = false;
    }

    private void AddLogoutToolbarItemIfNeeded()
    {
        if (_logoutItemAttached)
        {
            return;
        }

        ToolbarItems.Add(LogoutToolbarItem);
        _logoutItemAttached = true;
    }
}
