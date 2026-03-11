using Darwin.Mobile.Business.ViewModels;

namespace Darwin.Mobile.Business.Views;

/// <summary>
/// Business rewards page code-behind.
/// </summary>
/// <remarks>
/// Keeps view-only concerns in code-behind:
/// - triggers initial load on appearing,
/// - maps list selection to ViewModel edit mode.
/// </remarks>
public partial class RewardsPage : ContentPage
{
    private readonly RewardsViewModel _viewModel;

    public RewardsPage(RewardsViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        BindingContext = _viewModel;

        RewardTiersCollectionView.SelectionChanged += OnRewardTierSelected;
        CampaignsCollectionView.SelectionChanged += OnCampaignSelected;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearingAsync();
    }

    private void OnRewardTierSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.Count == 0)
        {
            return;
        }

        try
        {
            if (e.CurrentSelection[0] is RewardTierEditorItem selected)
            {
                _viewModel.BeginEdit(selected);
            }
        }
        finally
        {
            if (sender is CollectionView collectionView)
            {
                collectionView.SelectedItem = null;
            }
        }
    }

    private void OnCampaignSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.Count == 0)
        {
            return;
        }

        try
        {
            if (e.CurrentSelection[0] is BusinessCampaignEditorItem selected)
            {
                _viewModel.BeginEditCampaign(selected);
            }
        }
        finally
        {
            if (sender is CollectionView collectionView)
            {
                collectionView.SelectedItem = null;
            }
        }
    }
}
