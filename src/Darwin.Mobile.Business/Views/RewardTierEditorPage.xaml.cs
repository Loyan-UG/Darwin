using System.Windows.Input;
using Darwin.Mobile.Business.ViewModels;

namespace Darwin.Mobile.Business.Views;

public partial class RewardTierEditorPage : ContentPage, IQueryAttributable
{
    public RewardTierEditorPage()
    {
        InitializeComponent();
        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
    }

    public ICommand BackCommand { get; }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("viewModel", out var viewModelValue) || viewModelValue is not RewardsViewModel viewModel)
        {
            return;
        }

        BindingContext = viewModel;

        if (query.TryGetValue("rewardTier", out var tierValue) && tierValue is RewardTierEditorItem tier)
        {
            if (viewModel.SelectRewardTierCommand.CanExecute(tier))
            {
                viewModel.SelectRewardTierCommand.Execute(tier);
            }
        }
        else if (viewModel.CreateNewCommand.CanExecute(null))
        {
            viewModel.CreateNewCommand.Execute(null);
        }
    }
}
