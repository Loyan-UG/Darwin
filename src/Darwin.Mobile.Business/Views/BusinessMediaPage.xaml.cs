using Darwin.Mobile.Business.ViewModels;
using System.Windows.Input;

namespace Darwin.Mobile.Business.Views;

public partial class BusinessMediaPage : ContentPage
{
    private readonly BusinessMediaViewModel _viewModel;

    public BusinessMediaPage(BusinessMediaViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        InitializeComponent();
        BindingContext = _viewModel;
    }

    public ICommand SetPrimaryCommand => _viewModel.SetPrimaryCommand;

    public ICommand DeleteImageCommand => _viewModel.DeleteImageCommand;

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
