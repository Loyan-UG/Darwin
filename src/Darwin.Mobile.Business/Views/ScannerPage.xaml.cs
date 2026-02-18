using System;
using System.Threading.Tasks;
using Darwin.Mobile.Business.ViewModels;
using Microsoft.Maui.Controls;

namespace Darwin.Mobile.Business.Views;

/// <summary>
/// Scanner page code-behind.
///
/// Why this exists:
/// - Attaches the injected ViewModel as BindingContext.
/// - Listens for feedback visibility requests and auto-scrolls to top.
/// </summary>
public partial class ScannerPage : ContentPage
{
    private ScannerViewModel? Vm => BindingContext as ScannerViewModel;

    public ScannerPage(ScannerViewModel viewModel)
    {
        InitializeComponent();

        BindingContext = viewModel;
        viewModel.FeedbackVisibilityRequested += OnFeedbackVisibilityRequested;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (Vm is not null)
        {
            Vm.FeedbackVisibilityRequested -= OnFeedbackVisibilityRequested;
        }
    }

    private async void OnFeedbackVisibilityRequested()
    {
        try
        {
            if (RootScrollView is null)
            {
                return;
            }

            // Small delay allows layout to update before animated scroll.
            await Task.Delay(40);
            await RootScrollView.ScrollToAsync(0, 0, true);
        }
        catch
        {
            // UI scroll failure must never crash scanner flow.
        }
    }
}
