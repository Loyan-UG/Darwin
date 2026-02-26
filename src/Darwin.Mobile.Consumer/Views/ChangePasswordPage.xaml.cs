using System;
using Darwin.Mobile.Consumer.ViewModels;

namespace Darwin.Mobile.Consumer.Views;

/// <summary>
/// Change password page code-behind.
/// </summary>
public partial class ChangePasswordPage : ContentPage
{
    private readonly ChangePasswordViewModel _viewModel;

    public ChangePasswordPage(ChangePasswordViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        BindingContext = _viewModel;
    }
}
