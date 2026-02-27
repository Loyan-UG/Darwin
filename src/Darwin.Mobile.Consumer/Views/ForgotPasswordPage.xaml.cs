using Darwin.Mobile.Consumer.ViewModels;

namespace Darwin.Mobile.Consumer.Views;

/// <summary>
/// Forgot-password page that allows the user to request password reset instructions.
/// </summary>
public partial class ForgotPasswordPage : ContentPage
{
    public ForgotPasswordPage(ForgotPasswordViewModel viewModel)
    {
        InitializeComponent();

        // Injected view model keeps this page lean and consistent with DI-first architecture.
        BindingContext = viewModel;
    }
}
