using Darwin.Mobile.Consumer.ViewModels;

namespace Darwin.Mobile.Consumer.Views;

/// <summary>
/// Consumer account deletion request page.
/// </summary>
public partial class AccountDeletionPage : ContentPage
{
    public AccountDeletionPage(AccountDeletionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
