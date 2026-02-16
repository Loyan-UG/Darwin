using Darwin.Mobile.Consumer.Views;
using Darwin.Mobile.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Darwin.Mobile.Consumer.Services.Navigation;

/// <summary>
/// Window-aware root navigator for the Consumer app.
///
/// Implementation details:
/// - Uses <see cref="Window.Page"/> instead of deprecated <c>Application.MainPage</c>.
/// - Resolves login page from DI to keep constructor dependencies centralized.
/// - Marshals all root updates onto the window dispatcher for UI-thread safety.
/// </summary>
public sealed class AppRootNavigator : IAppRootNavigator
{
    private readonly IAuthService _authService;
    private readonly IServiceProvider _serviceProvider;
    private Window? _primaryWindow;

    public AppRootNavigator(IAuthService authService, IServiceProvider serviceProvider)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public void AttachWindow(Window window)
    {
        _primaryWindow = window ?? throw new ArgumentNullException(nameof(window));
    }

    public Task NavigateToAuthenticatedShellAsync()
    {
        // Build a fresh shell when entering authenticated mode to ensure clean navigation state.
        return SwitchRootAsync(new AppShell(_authService, this));
    }

    public Task NavigateToLoginAsync()
    {
        // Resolve login page from DI so any future constructor dependencies continue to work automatically.
        var loginPage = _serviceProvider.GetRequiredService<LoginPage>();
        return SwitchRootAsync(new NavigationPage(loginPage));
    }

    private async Task SwitchRootAsync(Page rootPage)
    {
        var window = ResolveTargetWindow();
        if (window is null)
        {
            // Defensive no-op: app/window may still be initializing.
            return;
        }

        await window.Dispatcher.DispatchAsync(() => window.Page = rootPage);
    }

    private Window? ResolveTargetWindow()
    {
        if (_primaryWindow is not null)
        {
            return _primaryWindow;
        }

        return Application.Current?.Windows.FirstOrDefault();
    }
}
