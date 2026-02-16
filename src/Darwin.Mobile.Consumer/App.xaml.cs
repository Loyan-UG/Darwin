using Darwin.Mobile.Consumer.ViewModels;
using Darwin.Mobile.Consumer.Views;
using Darwin.Mobile.Shared.Navigation;
using Darwin.Mobile.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Consumer;

/// <summary>
/// Application entry point for the Consumer app.
///
/// .NET MAUI (latest) deprecates assigning <c>Application.MainPage</c> directly.
/// The app now initializes UI by overriding <see cref="CreateWindow(IActivationState?)"/>
/// and updates the root page via <see cref="Window.Page"/>.
/// </summary>
public partial class App : Application
{
    private readonly IAuthService _authService;
    private readonly IServiceProvider _serviceProvider;
    private Window? _primaryWindow;

    public App(IAuthService authService, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Creates the application window and starts asynchronous startup routing.
    /// </summary>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Use a lightweight placeholder page while auth state is being resolved.
        var window = new Window(new ContentPage());
        _primaryWindow = window;

        // Fire-and-forget startup; all exceptions are handled internally.
        _ = InitializeRootAsync(window);

        return window;
    }

    /// <summary>
    /// Replaces the current root with the authenticated shell.
    /// </summary>
    public Task NavigateToAuthenticatedShellAsync() => SwitchRootAsync(CreateAuthenticatedRootPage());

    /// <summary>
    /// Replaces the current root with the login flow wrapped in a navigation page.
    /// </summary>
    public Task NavigateToLoginAsync() => SwitchRootAsync(CreateUnauthenticatedRootPage());

    private async Task InitializeRootAsync(Window window)
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

        var root = tokenValid
            ? CreateAuthenticatedRootPage()
            : CreateUnauthenticatedRootPage();

        SetRootPage(window, root);
    }

    private async Task SwitchRootAsync(Page rootPage)
    {
        var window = ResolveTargetWindow();
        if (window is null)
        {
            return;
        }

        await window.Dispatcher.DispatchAsync(() => window.Page = rootPage);
    }

    private Page CreateAuthenticatedRootPage() => new AppShell(_authService);

    private Page CreateUnauthenticatedRootPage()
    {
        // Resolve from DI so constructor dependencies stay centralized and testable.
        var loginPage = _serviceProvider.GetRequiredService<LoginPage>();
        return new NavigationPage(loginPage);
    }

    private Window? ResolveTargetWindow() => _primaryWindow ?? Windows.FirstOrDefault();

    private static void SetRootPage(Window window, Page rootPage)
    {
        if (window.Dispatcher.IsDispatchRequired)
        {
            window.Dispatcher.Dispatch(() => window.Page = rootPage);
            return;
        }

        window.Page = rootPage;
    }
}