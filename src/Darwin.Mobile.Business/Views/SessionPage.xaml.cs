using Darwin.Mobile.Business.ViewModels;
using Microsoft.Maui.Controls;

namespace Darwin.Mobile.Business.Views;

/// <summary>
/// Displays scan session details and triggers load when page appears.
/// </summary>
[QueryProperty(nameof(SessionToken), "token")]
public partial class SessionPage : ContentPage
{
    /// <summary>
    /// Initializes Session page with dependency-injected ViewModel.
    /// </summary>
    public SessionPage(SessionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        viewModel.FeedbackVisibilityRequested += OnFeedbackVisibilityRequested;
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

        var viewModel = (SessionViewModel)BindingContext;
        await viewModel.LoadSessionAsync();
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
            await Task.Delay(40);
            await RootScrollView.ScrollToAsync(0, 0, true);
        }
        catch
        {
            // Ignore scroll exceptions to keep business flow uninterrupted.
        }
    }
}
