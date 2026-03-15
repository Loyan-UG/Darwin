using System;
using Darwin.Mobile.Business.ViewModels;

namespace Darwin.Mobile.Business.Views;

/// <summary>
/// Displays a rotating staff access QR badge for internal business operations.
/// </summary>
public partial class StaffAccessBadgePage : ContentPage
{
    private readonly StaffAccessBadgeViewModel _viewModel;

    public StaffAccessBadgePage(StaffAccessBadgeViewModel viewModel)
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

    protected override async void OnDisappearing()
    {
        await _viewModel.OnDisappearingAsync();
        base.OnDisappearing();
    }
}
