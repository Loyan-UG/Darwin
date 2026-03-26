using System;
using Darwin.Mobile.Consumer.ViewModels;

namespace Darwin.Mobile.Consumer.Views;

/// <summary>
/// Displays the consumer member address book and editor.
/// </summary>
public partial class MemberAddressesPage : ContentPage
{
    private readonly MemberAddressesViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemberAddressesPage"/> class.
    /// </summary>
    public MemberAddressesPage(MemberAddressesViewModel viewModel)
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
