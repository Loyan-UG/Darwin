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
