using Darwin.Mobile.Business.ViewModels;

namespace Darwin.Mobile.Business.Views;

/// <summary>
/// Code-behind for <see cref="LoginPage"/>.
/// </summary>
public partial class LoginPage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoginPage"/> class.
    /// The LoginViewModel will be injected by the MAUI dependency injection.
    /// </summary>
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
