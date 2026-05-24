using System;
using Darwin.Mobile.Consumer.ViewModels;
using Microsoft.Maui.Controls;

namespace Darwin.Mobile.Consumer.Views;

/// <summary>
/// Settings hub page.
/// </summary>
public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _viewModel;

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        BindingContext = _viewModel;
    }
}
