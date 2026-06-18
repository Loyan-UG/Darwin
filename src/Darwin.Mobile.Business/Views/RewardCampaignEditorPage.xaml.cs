using System.Windows.Input;
using Darwin.Mobile.Business.ViewModels;

namespace Darwin.Mobile.Business.Views;

public partial class RewardCampaignEditorPage : ContentPage, IQueryAttributable
{
    public RewardCampaignEditorPage()
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

        if (query.TryGetValue("campaign", out var campaignValue) && campaignValue is BusinessCampaignEditorItem campaign)
        {
            if (viewModel.SelectCampaignCommand.CanExecute(campaign))
            {
                viewModel.SelectCampaignCommand.Execute(campaign);
            }
        }
        else if (viewModel.NewCampaignCommand.CanExecute(null))
        {
            viewModel.NewCampaignCommand.Execute(null);
        }
    }
}
