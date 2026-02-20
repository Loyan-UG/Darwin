using System.Threading.Tasks;
using Darwin.Mobile.Business.ViewModels;
using Microsoft.Maui.Controls;

namespace Darwin.Mobile.Business.Views;

/// <summary>
/// Code-behind for <see cref="LoginPage"/>.
/// This page disables Shell navigation and flyout so that the login screen is shown
/// as a focused, standalone entry point for authentication.
/// </summary>
public partial class LoginPage
{
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();

        // Set the binding context to the injected view model
        BindingContext = viewModel;

        // Make this login page standalone:
        // - Hide the top navigation bar
        // - Disable the flyout menu for this page
        Shell.SetNavBarIsVisible(this, false);
        Shell.SetFlyoutBehavior(this, FlyoutBehavior.Disabled);

        // Subscribe once so VM can request "show error area".
        viewModel.ErrorBecameVisibleRequested += OnErrorBecameVisibleRequested;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // If ViewModel needs OnAppearingAsync, call it (fire-and-forget).
#pragma warning disable CS4014
        (BindingContext as LoginViewModel)?.OnAppearingAsync();
#pragma warning restore CS4014
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Always unsubscribe to avoid duplicate handlers on re-navigation.
        if (BindingContext is LoginViewModel vm)
        {
            vm.ErrorBecameVisibleRequested -= OnErrorBecameVisibleRequested;
        }
    }

    /// <summary>
    /// Scrolls to top so validation/network errors remain visible even if keyboard is open.
    /// </summary>
    private async void OnErrorBecameVisibleRequested()
    {
        try
        {
            if (RootScrollView is null)
            {
                return;
            }

            await Task.Delay(40);
            await RootScrollView.ScrollToAsync(0, 0, true);
        }
        catch
        {
            // Non-critical UI behavior; never crash login flow for this.
        }
    }
}
