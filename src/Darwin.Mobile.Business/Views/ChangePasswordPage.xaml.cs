using System;
using Darwin.Mobile.Business.ViewModels;

namespace Darwin.Mobile.Business.Views;

/// <summary>
/// Business change password page code-behind.
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
