using System;
using Darwin.Mobile.Consumer.ViewModels;

namespace Darwin.Mobile.Consumer.Views;

/// <summary>
/// Displays richer self-service CRM customer context for the current member.
/// </summary>
public partial class MemberCustomerContextPage : ContentPage
{
    private readonly MemberCustomerContextViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemberCustomerContextPage"/> class.
    /// </summary>
    public MemberCustomerContextPage(MemberCustomerContextViewModel viewModel)
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
            // Appearing is an async-void MAUI lifecycle hook. Context load failures stay inside ViewModel feedback.
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
            // Disappearing cleanup should never crash navigation away from customer context.
        }
        finally
        {
            base.OnDisappearing();
        }
    }
}
