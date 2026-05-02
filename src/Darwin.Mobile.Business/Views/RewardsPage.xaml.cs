using Darwin.Mobile.Business.ViewModels;

namespace Darwin.Mobile.Business.Views;

/// <summary>
/// Business rewards page code-behind.
/// </summary>
/// <remarks>
/// Keeps view-only concerns in code-behind:
/// - triggers initial load on appearing.
/// Selection behavior is command-bound in XAML so list rendering can use lightweight layouts without code-behind event wiring.
/// </remarks>
public partial class RewardsPage : ContentPage
{
    private readonly RewardsViewModel _viewModel;

    public RewardsPage(RewardsViewModel viewModel)
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
            // Appearing is an async-void MAUI lifecycle hook. Rewards load failures stay inside ViewModel feedback.
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
            // Disappearing cleanup should never crash navigation away from rewards.
        }
        finally
        {
            base.OnDisappearing();
        }
    }
}
