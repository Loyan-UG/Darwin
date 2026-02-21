using Darwin.Mobile.Consumer.ViewModels;
using System.Threading.Tasks;

namespace Darwin.Mobile.Consumer.Views;

/// <summary>
/// Code-behind for the login page.
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

        // Subscribe so the view model can request visibility for validation/network errors.
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

    /// <summary>
    /// Unsubscribes to avoid duplicate handlers when page instances are recreated.
    /// </summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (BindingContext is LoginViewModel vm)
        {
            vm.ErrorBecameVisibleRequested -= OnErrorBecameVisibleRequested;
        }
    }

    /// <summary>
    /// Scrolls the root scroll view to the top so the current error message is visible.
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
            // We intentionally ignore scroll errors so a UI animation issue never blocks login.
        }
    }
}
