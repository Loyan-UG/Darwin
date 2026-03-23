using Darwin.Mobile.Business.ViewModels;

namespace Darwin.Mobile.Business.Views;

/// <summary>
/// Business account deletion warning page.
/// </summary>
public partial class AccountDeletionPage : ContentPage
{
    public AccountDeletionPage(AccountDeletionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
