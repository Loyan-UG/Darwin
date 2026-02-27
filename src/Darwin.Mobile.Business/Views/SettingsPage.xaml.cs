using System;
using Darwin.Mobile.Business.ViewModels;

namespace Darwin.Mobile.Business.Views;

/// <summary>
/// Settings hub page code-behind for Business app.
/// </summary>
public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }
}
