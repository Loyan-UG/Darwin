using Darwin.Mobile.Business.ViewModels;
using System.Windows.Input;
using Darwin.Mobile.Business.Constants;

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
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        InitializeComponent();
        BindingContext = _viewModel;
    }

    public ICommand SelectRewardTierCommand => _viewModel.SelectRewardTierCommand;

    public ICommand SelectCampaignCommand => _viewModel.SelectCampaignCommand;

    public ICommand ToggleCampaignActivationCommand => _viewModel.ToggleCampaignActivationCommand;

    public ICommand OpenNewRewardTierCommand => new Command(async () => await OpenRewardTierEditorAsync(null));

    public ICommand OpenRewardTierEditorCommand => new Command<RewardTierEditorItem>(async item => await OpenRewardTierEditorAsync(item));

    public ICommand OpenNewCampaignCommand => new Command(async () => await OpenCampaignEditorAsync(null));

    public ICommand OpenCampaignEditorCommand => new Command<BusinessCampaignEditorItem>(async item => await OpenCampaignEditorAsync(item));

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

    private async Task OpenRewardTierEditorAsync(RewardTierEditorItem? item)
    {
        if (!_viewModel.CanManageRewards)
        {
            return;
        }

        var parameters = new Dictionary<string, object?>
        {
            ["viewModel"] = _viewModel
        };

        if (item is not null)
        {
            parameters["rewardTier"] = item;
        }

        await Shell.Current.GoToAsync(Routes.RewardTierEditor, parameters);
    }

    private async Task OpenCampaignEditorAsync(BusinessCampaignEditorItem? item)
    {
        if (!_viewModel.CanManageRewards)
        {
            return;
        }

        var parameters = new Dictionary<string, object?>
        {
            ["viewModel"] = _viewModel
        };

        if (item is not null)
        {
            parameters["campaign"] = item;
        }

        await Shell.Current.GoToAsync(Routes.RewardCampaignEditor, parameters);
    }
}
