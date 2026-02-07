using Darwin.Mobile.Consumer.ViewModels;

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
    }


    protected override void OnAppearing()
    {
        base.OnAppearing();

        // If ViewModel needs OnAppearingAsync, call it (fire-and-forget).
    #pragma warning disable CS4014
        (BindingContext as LoginViewModel)?.OnAppearingAsync();
    #pragma warning restore CS4014

    }
}
