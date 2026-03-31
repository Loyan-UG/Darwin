using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.Storage.Abstractions;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Business;

/// <summary>
/// Application entry point for the Business app.
/// </summary>
public partial class App : Application
{
    private readonly IAuthService _authService;
    private readonly ILocalDbMigrator _localDbMigrator;

    /// <summary>
    /// Initializes a new instance of the <see cref="App"/> class.
    /// </summary>
    public App(IAuthService authService, ILocalDbMigrator localDbMigrator)
    {
        InitializeComponent();
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _localDbMigrator = localDbMigrator ?? throw new ArgumentNullException(nameof(localDbMigrator));

        // Force a deterministic visual theme for the Business app.
        // We intentionally do not follow device Dark/Light mode for now.
        UserAppTheme = AppTheme.Light;
    }

    /// <summary>
    /// Creates the application window and starts asynchronous startup routing.
    /// </summary>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new ContentPage());
        _ = InitializeRootAsync(window);
        return window;
    }

    /// <summary>
    /// Best-effort token refresh when the app returns from background.
    /// This avoids stale access tokens causing misleading downstream errors.
    /// </summary>
    protected override void OnResume()
    {
        base.OnResume();
        _ = TryRefreshSilentlyAsync();
    }

    private async Task TryRefreshSilentlyAsync()
    {
        try
        {
            await _authService.EnsureAuthenticatedSessionAsync(CancellationToken.None);
        }
        catch
        {
            // Best effort only. If refresh fails, secured screens will redirect on next guarded action.
        }
    }

    private async Task InitializeRootAsync(Window window)
    {
        try
        {
            await _localDbMigrator.MigrateAsync(CancellationToken.None);
        }
        catch
        {
            // Startup authentication must stay resilient even if local persistence initialization fails.
            // Feature-level flows can degrade gracefully while the app still reaches a usable shell.
        }

        bool hasAuthenticatedSession;
        try
        {
            hasAuthenticatedSession = await _authService.EnsureAuthenticatedSessionAsync(CancellationToken.None);
        }
        catch
        {
            hasAuthenticatedSession = false;
        }

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            window.Page = new AppShell(hasAuthenticatedSession ? $"//{Constants.Routes.Home}" : $"//{Constants.Routes.Login}");
        });
    }
}
