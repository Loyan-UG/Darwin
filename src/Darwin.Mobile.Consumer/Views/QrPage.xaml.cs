using Darwin.Mobile.Consumer.ViewModels;

namespace Darwin.Mobile.Consumer.Views;

/// <summary>
/// Displays the rotating QR code for the consumer.
/// </summary>
public partial class QrPage
{
    public QrPage(QrViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ((QrViewModel)BindingContext).OnAppearingAsync();
    }
}
