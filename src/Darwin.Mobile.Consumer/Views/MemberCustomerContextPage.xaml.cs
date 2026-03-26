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
        await _viewModel.OnAppearingAsync();
    }
}
