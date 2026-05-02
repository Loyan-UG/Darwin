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
    private static readonly TimeSpan StartupOperationTimeout = TimeSpan.FromSeconds(12);
    private static readonly TimeSpan ResumeRefreshTimeout = TimeSpan.FromSeconds(8);

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
        using var timeout = new CancellationTokenSource(ResumeRefreshTimeout);

        try
        {
            var refreshed = await _authService.EnsureAuthenticatedSessionAsync(timeout.Token).ConfigureAwait(false);
            if (refreshed)
            {
                return;
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (Windows.Count == 0)
                {
                    return;
                }

                Windows[0].Page = new AppShell($"//{Constants.Routes.Login}");
            }).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Resume refresh is bounded so stale network calls cannot freeze the app after background resume.
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
            using var timeout = new CancellationTokenSource(StartupOperationTimeout);
            await _localDbMigrator.MigrateAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Startup must continue even if local persistence initialization exceeds the mobile startup budget.
        }
        catch
        {
            // Startup authentication must stay resilient even if local persistence initialization fails.
            // Feature-level flows can degrade gracefully while the app still reaches a usable shell.
        }

        bool hasAuthenticatedSession;
        try
        {
            using var timeout = new CancellationTokenSource(StartupOperationTimeout);
            hasAuthenticatedSession = await _authService.EnsureAuthenticatedSessionAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            hasAuthenticatedSession = false;
        }
        catch
        {
            hasAuthenticatedSession = false;
        }

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            window.Page = new AppShell(hasAuthenticatedSession ? $"//{Constants.Routes.Home}" : $"//{Constants.Routes.Login}");
        }).ConfigureAwait(false);
    }
}
