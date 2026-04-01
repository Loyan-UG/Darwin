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
        try
        {
            var refreshed = await _authService.EnsureAuthenticatedSessionAsync(CancellationToken.None).ConfigureAwait(false);
            if (!refreshed)
            {
                await _appRootNavigator.NavigateToLoginAsync();
                return;
            }

            _ = _pushRegistrationCoordinator.TryRegisterCurrentDeviceAsync(CancellationToken.None);
            _ = _startupWarmupCoordinator.WarmAuthenticatedExperienceAsync(CancellationToken.None);
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
            await _localDbMigrator.MigrateAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // Startup authentication must stay resilient even if local persistence initialization fails.
        }

        bool tokenValid;
        try
        {
            tokenValid = await _authService.EnsureAuthenticatedSessionAsync(CancellationToken.None).ConfigureAwait(false);
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

        _ = _pushRegistrationCoordinator.TryRegisterCurrentDeviceAsync(CancellationToken.None);
        await _appRootNavigator.NavigateToAuthenticatedShellAsync();
        _ = _startupWarmupCoordinator.WarmAuthenticatedExperienceAsync(CancellationToken.None);
    }
}
