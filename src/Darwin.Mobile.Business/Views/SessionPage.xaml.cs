using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Business.ViewModels;
using Microsoft.Maui.Controls;

namespace Darwin.Mobile.Business.Views;

/// <summary>
/// Displays scan session details and triggers load when page appears.
/// </summary>
[QueryProperty(nameof(SessionToken), "token")]
public partial class SessionPage : ContentPage
{
    private bool _isFeedbackSubscribed;
    private CancellationTokenSource? _feedbackScrollCancellation;

    /// <summary>
    /// Initializes Session page with dependency-injected ViewModel.
    /// </summary>
    public SessionPage(SessionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        SubscribeFeedback(viewModel);
    }

    /// <summary>
    /// Session token passed via Shell query parameter.
    /// </summary>
    public string SessionToken
    {
        get => ((SessionViewModel)BindingContext).SessionToken;
        set => ((SessionViewModel)BindingContext).SessionToken = value;
    }

    /// <summary>
    /// Loads session data whenever page becomes visible.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            var viewModel = (SessionViewModel)BindingContext;
            SubscribeFeedback(viewModel);
            ResetFeedbackScrollCancellation();
            await viewModel.OnAppearingAsync();
            await viewModel.LoadSessionAsync();
        }
        catch
        {
            // Appearing is an async-void MAUI lifecycle hook. Session load failures stay inside the ViewModel feedback flow.
        }
    }

    /// <summary>
    /// Cancels any in-flight session work and detaches feedback callback when the page leaves the screen.
    /// </summary>
    protected override async void OnDisappearing()
    {
        try
        {
            var viewModel = (SessionViewModel)BindingContext;
            CancelFeedbackScroll();
            await viewModel.OnDisappearingAsync();
            UnsubscribeFeedback(viewModel);
        }
        catch
        {
            // Disappearing cleanup should never crash navigation away from the session page.
        }
        finally
        {
            base.OnDisappearing();
        }
    }

    /// <summary>
    /// Unfocus input before action to keep feedback area visible if an error occurs.
    /// </summary>
    private void OnActionClicked(object? sender, EventArgs e)
    {
        PointsEntry?.Unfocus();
    }

    private async void OnFeedbackVisibilityRequested()
    {
        try
        {
            var token = _feedbackScrollCancellation?.Token ?? CancellationToken.None;
            await Task.Delay(40, token);
            token.ThrowIfCancellationRequested();
            await RootScrollView.ScrollToAsync(0, 0, true);
        }
        catch (OperationCanceledException)
        {
            // Feedback scroll was cancelled because the session page is no longer visible.
        }
        catch
        {
            // Ignore scroll exceptions to keep business flow uninterrupted.
        }
    }

    /// <summary>
    /// Subscribes feedback callback once per page instance.
    /// </summary>
    /// <param name="viewModel">Session view model owning the feedback event.</param>
    private void SubscribeFeedback(SessionViewModel viewModel)
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
    /// <param name="viewModel">Session view model owning the feedback event.</param>
    private void UnsubscribeFeedback(SessionViewModel viewModel)
    {
        if (!_isFeedbackSubscribed)
        {
            return;
        }

        viewModel.FeedbackVisibilityRequested -= OnFeedbackVisibilityRequested;
        _isFeedbackSubscribed = false;
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
