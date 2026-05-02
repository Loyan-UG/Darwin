using System;
using Darwin.Mobile.Consumer.ViewModels;

namespace Darwin.Mobile.Consumer.Views;

/// <summary>
/// Displays member order and invoice history with lightweight self-service actions.
/// </summary>
public partial class MemberCommercePage : ContentPage
{
    private readonly MemberCommerceViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemberCommercePage"/> class.
    /// </summary>
    public MemberCommercePage(MemberCommerceViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        BindingContext = _viewModel;
    }

    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            await _viewModel.OnAppearingAsync();
        }
        catch
        {
            // Appearing is an async-void MAUI lifecycle hook. Commerce load failures stay inside ViewModel feedback.
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
            // Disappearing cleanup should never crash navigation away from commerce.
        }
        finally
        {
            base.OnDisappearing();
        }
    }
}
