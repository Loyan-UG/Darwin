using Darwin.Mobile.Business.ViewModels;
using System;

namespace Darwin.Mobile.Business.Views;

/// <summary>
/// Business subscription overview page.
/// The mobile app shows read-only status details and hands full management off to the Loyan website.
/// </summary>
public partial class SubscriptionPage : ContentPage
{
    private readonly SubscriptionViewModel _viewModel;

    public SubscriptionPage(SubscriptionViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearingAsync();
    }
}
