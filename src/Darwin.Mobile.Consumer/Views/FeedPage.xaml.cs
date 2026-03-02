using Darwin.Mobile.Consumer.ViewModels;

namespace Darwin.Mobile.Consumer.Views;

/// <summary>
/// Feed page code-behind.
/// </summary>
public partial class FeedPage
{
    public FeedPage(FeedViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ((FeedViewModel)BindingContext).OnAppearingAsync();
    }
}