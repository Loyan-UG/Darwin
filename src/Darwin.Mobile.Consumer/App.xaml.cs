using Darwin.Mobile.Shared.Services;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using System;
using System.Threading;

namespace Darwin.Mobile.Consumer;

/// <summary>
/// The root application class. Decides which page to show at startup based on authentication state.
/// </summary>
public partial class App : Application
{
    private readonly IAuthService _authService;

    /// <summary>
    /// Initializes a new instance of the <see cref="App"/> class.
    /// </summary>
    /// <param name="authService">Service used to authenticate and refresh tokens.</param>
    public App(IAuthService authService)
    {
        InitializeComponent();

        _authService = authService ?? throw new ArgumentNullException(nameof(authService));

        // At start we set a placeholder page; real page is set once auth is checked.
        MainPage = new ContentPage();

        CheckAuthenticationState();
    }

    /// <summary>
    /// Checks whether a valid token exists and navigates accordingly.
    /// </summary>
    private async void CheckAuthenticationState()
    {
        bool tokenValid = false;

        try
        {
            // Attempt to refresh the stored token. Returns true if a valid access token is available.
            tokenValid = await _authService.TryRefreshAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // Ignore exceptions; will fall back to showing the login page.
            tokenValid = false;
        }

        // Navigate based on token availability.
        if (tokenValid)
        {
            // User has a valid token; load the main shell.
            MainPage = new AppShell();
        }
        else
        {
            // No valid token; show the login page within a navigation context.
            MainPage = new NavigationPage(new Views.LoginPage());
        }
    }
}
