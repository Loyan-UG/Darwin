using Darwin.Mobile.Consumer.ViewModels;

namespace Darwin.Mobile.Consumer.Views;

/// <summary>
/// Page that completes the reset-password flow by accepting email, token, and a new password.
/// </summary>
public partial class ResetPasswordPage : ContentPage
{
    public ResetPasswordPage(ResetPasswordViewModel viewModel)
    {
        InitializeComponent();

        // Injected view model keeps navigation and business logic outside of code-behind.
        BindingContext = viewModel;
    }
}
