using System;
using System.Threading.Tasks;
using Darwin.Mobile.Business.ViewModels;
using Microsoft.Maui.Controls;

namespace Darwin.Mobile.Business.Views;

/// <summary>
/// Scanner page code-behind.
/// 
/// Responsibilities:
/// - Attach injected ViewModel as BindingContext.
/// - React to feedback visibility requests and scroll to top.
/// </summary>
public partial class ScannerPage : ContentPage
{
    private ScannerViewModel? Vm => BindingContext as ScannerViewModel;

    /// <summary>
    /// Initializes page with DI-provided view model.
    /// </summary>
    public ScannerPage(ScannerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        viewModel.FeedbackVisibilityRequested += OnFeedbackVisibilityRequested;
    }

    /// <summary>
    /// Unsubscribes event handlers when page disappears to avoid duplicate subscriptions.
    /// </summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (Vm is not null)
        {
            Vm.FeedbackVisibilityRequested -= OnFeedbackVisibilityRequested;
        }
    }

    /// <summary>
    /// Scrolls to top so feedback labels are visible even when keyboard was previously open.
    /// </summary>
    private async void OnFeedbackVisibilityRequested()
    {
        try
        {
            if (RootScrollView is null)
            {
                return;
            }

            await Task.Delay(40);
            await RootScrollView.ScrollToAsync(0, 0, true);
        }
        catch
        {
            // Feedback scroll failure should never crash scanner flow.
        }
    }
}
