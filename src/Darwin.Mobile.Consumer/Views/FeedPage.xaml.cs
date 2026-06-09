using Darwin.Mobile.Consumer.ViewModels;
using Darwin.Contracts.Loyalty;
using Darwin.Mobile.Shared.Commands;

namespace Darwin.Mobile.Consumer.Views;

/// <summary>
/// Feed page code-behind.
/// </summary>
public partial class FeedPage
{
    private readonly FeedViewModel _viewModel;

    public FeedPage(FeedViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new System.ArgumentNullException(nameof(viewModel));
        BindingContext = _viewModel;
    }

    public AsyncCommand<PromotionFeedItem> OpenPromotionCommand => _viewModel.OpenPromotionCommand;

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            await _viewModel.OnAppearingAsync();
        }
        catch
        {
            // Appearing is an async-void MAUI lifecycle hook. Feed load failures stay inside ViewModel feedback.
        }
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
            // Disappearing cleanup should never crash navigation away from feed.
        }
        finally
        {
            base.OnDisappearing();
        }
    }
}
