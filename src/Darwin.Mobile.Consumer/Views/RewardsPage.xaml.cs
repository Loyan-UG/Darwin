using Darwin.Mobile.Consumer.ViewModels;

namespace Darwin.Mobile.Consumer.Views;

public partial class RewardsPage
{
    public RewardsPage(RewardsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ((RewardsViewModel)BindingContext).OnAppearingAsync();
    }
}
