using Darwin.Mobile.Business.ViewModels;

namespace Darwin.Mobile.Business.Views;

public partial class BusinessMediaPage : ContentPage
{
    private readonly BusinessMediaViewModel _viewModel;

    public BusinessMediaPage(BusinessMediaViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            await _viewModel.OnAppearingAsync();
        }
        catch
        {
            // Appearing is an async-void MAUI lifecycle hook. Media load failures stay in page state.
        }
    }
}
