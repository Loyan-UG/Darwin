using Darwin.Mobile.Consumer.Services.Navigation;
using Darwin.Mobile.Shared.Services;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Consumer;

/// <summary>
/// Application entry point for the Consumer app.
///
/// Startup responsibility:
/// - Create the first window.
/// - Resolve authentication state.
/// - Delegate root-page switching to a window-aware navigator.
/// </summary>
public partial class App : Application
{
    private readonly IAuthService _authService;
    private readonly IAppRootNavigator _appRootNavigator;

    public App(IAuthService authService, IAppRootNavigator appRootNavigator)
    {
        InitializeComponent();
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _appRootNavigator = appRootNavigator ?? throw new ArgumentNullException(nameof(appRootNavigator));

        // Force a deterministic visual theme for the Consumer app.
        // We intentionally do not follow device Dark/Light mode for now.
        UserAppTheme = AppTheme.Light;
    }

    /// <summary>
    /// Creates the application window and starts asynchronous startup routing.
    /// </summary>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Use a minimal placeholder page while token refresh is evaluated.
        var window = new Window(new ContentPage());

        // Register this specific window as the primary target for root changes.
        _appRootNavigator.AttachWindow(window);

        // Fire-and-forget startup bootstrap; exceptions are contained inside the method.
        _ = InitializeRootAsync();

        return window;
    }

    /// <summary>
    /// When the app resumes after being backgrounded for a while, attempt a best-effort token refresh
    /// so subsequent API calls do not fail with stale access tokens.
    /// </summary>
    protected override void OnResume()
    {
        base.OnResume();

        // Re-assert theme to keep UI deterministic after OS/theme transitions.
        UserAppTheme = AppTheme.Light;

        _ = TryRefreshSilentlyAsync();
    }

    private async Task TryRefreshSilentlyAsync()
    {
        try
        {
            await _authService.TryRefreshAsync(CancellationToken.None);
        }
        catch
        {
            // Best effort only. The next guarded API call or navigation decision will handle auth failures.
        }
    }

    private async Task InitializeRootAsync()
    {
        bool tokenValid;
        try
        {
            tokenValid = await _authService.TryRefreshAsync(CancellationToken.None);
        }
        catch
        {
            tokenValid = false;
        }

        if (tokenValid)
        {
            await _appRootNavigator.NavigateToAuthenticatedShellAsync();
            return;
        }

        await _appRootNavigator.NavigateToLoginAsync();
    }
}
