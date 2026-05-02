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

        try
        {
            await _viewModel.OnAppearingAsync();
        }
        catch
        {
            // Appearing is an async-void MAUI lifecycle hook. Badge refresh failures stay inside ViewModel feedback.
        }
    }

    protected override async void OnDisappearing()
    {
        try
        {
            await _viewModel.OnDisappearingAsync();
        }
        catch
        {
            // Disappearing cleanup should never crash navigation away from the access badge.
        }
        finally
        {
            base.OnDisappearing();
        }
    }
}
