using System;
using System.Threading;
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
    private bool _isFeedbackSubscribed;
    private CancellationTokenSource? _feedbackScrollCancellation;

    /// <summary>
    /// Initializes page with DI-provided view model.
    /// </summary>
    public ScannerPage(ScannerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        SubscribeFeedback(viewModel);
    }


    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (Vm is null)
        {
            return;
        }

        try
        {
            SubscribeFeedback(Vm);
            ResetFeedbackScrollCancellation();
            await Vm.OnAppearingAsync();
        }
        catch
        {
            // Appearing is an async-void MAUI lifecycle hook. Scanner readiness failures stay inside the page flow.
        }
    }
    /// <summary>
    /// Unsubscribes event handlers when page disappears to avoid duplicate subscriptions.
    /// </summary>
    protected override async void OnDisappearing()
    {
        base.OnDisappearing();

        try
        {
            if (Vm is not null)
            {
                CancelFeedbackScroll();
                await Vm.OnDisappearingAsync();
                UnsubscribeFeedback(Vm);
            }
        }
        catch
        {
            // Disappearing cleanup should never crash navigation away from the scanner page.
        }
    }

    /// <summary>
    /// Subscribes feedback callback once per page instance.
    /// </summary>
    /// <param name="viewModel">Scanner view model owning the feedback event.</param>
    private void SubscribeFeedback(ScannerViewModel viewModel)
    {
        if (_isFeedbackSubscribed)
        {
            return;
        }

        viewModel.FeedbackVisibilityRequested += OnFeedbackVisibilityRequested;
        _isFeedbackSubscribed = true;
    }

    /// <summary>
    /// Unsubscribes feedback callback when the page leaves the screen.
    /// </summary>
    /// <param name="viewModel">Scanner view model owning the feedback event.</param>
    private void UnsubscribeFeedback(ScannerViewModel viewModel)
    {
        if (!_isFeedbackSubscribed)
        {
            return;
        }

        viewModel.FeedbackVisibilityRequested -= OnFeedbackVisibilityRequested;
        _isFeedbackSubscribed = false;
    }

    /// <summary>
    /// Scrolls to top so feedback labels are visible even when keyboard was previously open.
    /// </summary>
    private async void OnFeedbackVisibilityRequested()
    {
        try
        {
            var token = _feedbackScrollCancellation?.Token ?? CancellationToken.None;
            if (RootScrollView is null)
            {
                return;
            }

            await Task.Delay(40, token);
            token.ThrowIfCancellationRequested();
            await RootScrollView.ScrollToAsync(0, 0, true);
        }
        catch (OperationCanceledException)
        {
            // Feedback scroll was cancelled because the scanner page is no longer visible.
        }
        catch
        {
            // Feedback scroll failure should never crash scanner flow.
        }
    }

    /// <summary>
    /// Creates a fresh cancellation source for feedback scrolling while the page is visible.
    /// </summary>
    private void ResetFeedbackScrollCancellation()
    {
        CancelFeedbackScroll();
        _feedbackScrollCancellation = new CancellationTokenSource();
    }

    /// <summary>
    /// Cancels pending feedback scroll work when navigation leaves this page.
    /// </summary>
    private void CancelFeedbackScroll()
    {
        var cancellation = Interlocked.Exchange(ref _feedbackScrollCancellation, null);
        if (cancellation is null)
        {
            return;
        }

        cancellation.Cancel();
        cancellation.Dispose();
    }
}
