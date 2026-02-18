using System;
using System.Threading.Tasks;
using Darwin.Mobile.Business.ViewModels;
using Microsoft.Maui.Controls;

namespace Darwin.Mobile.Business.Views;

/// <summary>
/// Scanner page code-behind.
///
/// Why this class exists:
/// - Attaches injected ViewModel as BindingContext.
/// - Responds to ViewModel feedback event and scrolls to top so operator can see messages.
/// </summary>
public partial class ScannerPage : ContentPage
{
    private ScannerViewModel? Vm => BindingContext as ScannerViewModel;

    /// <summary>
    /// Initializes page with dependency-injected ViewModel.
    /// </summary>
    public ScannerPage(ScannerViewModel viewModel)
    {
        InitializeComponent();

        BindingContext = viewModel;
        viewModel.FeedbackVisibilityRequested += OnFeedbackVisibilityRequested;
    }

    /// <summary>
    /// Unsubscribe events to avoid handler accumulation across page lifecycle.
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
    /// Scrolls to top so feedback labels are visible even when keyboard was open.
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
            // Never crash scanner flow due to a non-critical scroll failure.
        }
    }
}
