using Darwin.Mobile.Consumer.Services.Navigation;
using Darwin.Mobile.Consumer.Services.Notifications;
using Darwin.Mobile.Consumer.Services.Startup;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.Storage.Abstractions;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Consumer;

/// <summary>
/// Application entry point for the Consumer app.
/// </summary>
public partial class App : Application
{
    private static readonly TimeSpan StartupOperationTimeout = TimeSpan.FromSeconds(12);
    private static readonly TimeSpan ResumeRefreshTimeout = TimeSpan.FromSeconds(8);
    private static readonly TimeSpan AuthenticatedBackgroundWarmupTimeout = TimeSpan.FromSeconds(20);

    private readonly IAuthService _authService;
    private readonly IAppRootNavigator _appRootNavigator;
    private readonly IConsumerPushRegistrationCoordinator _pushRegistrationCoordinator;
    private readonly IConsumerStartupWarmupCoordinator _startupWarmupCoordinator;
    private readonly ILocalDbMigrator _localDbMigrator;

    public App(
        IAuthService authService,
        IAppRootNavigator appRootNavigator,
        IConsumerPushRegistrationCoordinator pushRegistrationCoordinator,
        IConsumerStartupWarmupCoordinator startupWarmupCoordinator,
        ILocalDbMigrator localDbMigrator)
    {
        InitializeComponent();
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _appRootNavigator = appRootNavigator ?? throw new ArgumentNullException(nameof(appRootNavigator));
        _pushRegistrationCoordinator = pushRegistrationCoordinator ?? throw new ArgumentNullException(nameof(pushRegistrationCoordinator));
        _startupWarmupCoordinator = startupWarmupCoordinator ?? throw new ArgumentNullException(nameof(startupWarmupCoordinator));
        _localDbMigrator = localDbMigrator ?? throw new ArgumentNullException(nameof(localDbMigrator));

        UserAppTheme = AppTheme.Light;
    }

    /// <summary>
    /// Creates the application window and starts asynchronous startup routing.
    /// </summary>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new ContentPage());
        _appRootNavigator.AttachWindow(window);
        _ = InitializeRootAsync();
        return window;
    }

    /// <summary>
    /// Revalidates authentication and starts background warmup when the app resumes.
    /// </summary>
    protected override void OnResume()
    {
        base.OnResume();
        UserAppTheme = AppTheme.Light;
        _ = TryRefreshSilentlyAsync();
    }

    private async Task TryRefreshSilentlyAsync()
    {
        using var timeout = new CancellationTokenSource(ResumeRefreshTimeout);

        try
        {
            var refreshed = await _authService.EnsureAuthenticatedSessionAsync(timeout.Token).ConfigureAwait(false);
            if (!refreshed)
            {
                await _appRootNavigator.NavigateToLoginAsync();
                return;
            }

            // Resume warmup is deliberately detached from the short auth refresh timeout.
            // These background tasks hydrate non-critical state and must not force the resume flow to wait.
            _ = RunAuthenticatedBackgroundWarmupSafelyAsync();
        }
        catch (OperationCanceledException)
        {
            // Resume refresh is bounded so the app never blocks user interaction after returning from background.
        }
        catch
        {
            // Best effort only. The next guarded API call or navigation decision will handle auth failures.
        }
    }

    private async Task InitializeRootAsync()
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
        }

        bool tokenValid;
        try
        {
            using var timeout = new CancellationTokenSource(StartupOperationTimeout);
            tokenValid = await _authService.EnsureAuthenticatedSessionAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            tokenValid = false;
        }
        catch
        {
            tokenValid = false;
        }

        if (!tokenValid)
        {
            await _appRootNavigator.NavigateToLoginAsync();
            return;
        }

        await _appRootNavigator.NavigateToAuthenticatedShellAsync();
        // Authenticated startup warmup is deliberately fire-and-forget after the shell route is known.
        // It keeps the first screen responsive while background cache hydration and push registration complete.
        _ = RunAuthenticatedBackgroundWarmupSafelyAsync();
    }

    /// <summary>
    /// Runs authenticated non-critical startup work behind a bounded background guard.
    /// Push registration and cache hydration improve the next screens, but they must never block routing
    /// or surface unobserved task exceptions from fire-and-forget execution.
    /// </summary>
    private async Task RunAuthenticatedBackgroundWarmupSafelyAsync()
    {
        try
        {
            using var timeout = new CancellationTokenSource(AuthenticatedBackgroundWarmupTimeout);
            await _pushRegistrationCoordinator.TryRegisterCurrentDeviceAsync(timeout.Token).ConfigureAwait(false);
            await _startupWarmupCoordinator.WarmAuthenticatedExperienceAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Background warmup is bounded; stale or slow work is retried by the next authenticated app lifecycle event.
        }
        catch
        {
            // Background warmup is best-effort and must never affect the active screen.
        }
    }
}
