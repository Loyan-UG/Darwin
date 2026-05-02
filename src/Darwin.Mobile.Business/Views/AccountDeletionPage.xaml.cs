using Darwin.Mobile.Business.ViewModels;

namespace Darwin.Mobile.Business.Views;

/// <summary>
/// Business account deletion warning page.
/// </summary>
public partial class AccountDeletionPage : ContentPage
{
    private readonly AccountDeletionViewModel _viewModel;

    public AccountDeletionPage(AccountDeletionViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        BindingContext = _viewModel;
    }

    /// <inheritdoc />
    protected override async void OnDisappearing()
    {
        try
        {
            await _viewModel.OnDisappearingAsync();
        }
        catch
        {
            // Disappearing cleanup should never crash navigation away from account deletion.
        }
        finally
        {
            base.OnDisappearing();
        }
    }
}
