using Darwin.Mobile.Consumer.ViewModels;

namespace Darwin.Mobile.Consumer.Views;

/// <summary>
/// Consumer self-registration page.
/// </summary>
public partial class RegisterPage : ContentPage
{
    public RegisterPage(RegisterViewModel viewModel)
    {
        InitializeComponent();

        // Resolve and assign view model via DI so constructor dependencies stay centralized.
        BindingContext = viewModel;
    }
}
