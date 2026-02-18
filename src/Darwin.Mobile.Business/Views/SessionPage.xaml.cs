using System;
using System.Threading.Tasks;
using Darwin.Mobile.Business.ViewModels;
using Microsoft.Maui.Controls;

namespace Darwin.Mobile.Business.Views;

/// <summary>
/// Displays session details after scan and keeps feedback visible for operator UX.
/// </summary>
[QueryProperty(nameof(SessionToken), "token")]
public partial class SessionPage : ContentPage
{
    private SessionViewModel Vm => (SessionViewModel)BindingContext;

    public SessionPage(SessionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        viewModel.FeedbackVisibilityRequested += OnFeedbackVisibilityRequested;
    }

    public string SessionToken
    {
        get => Vm.SessionToken;
        set => Vm.SessionToken = value;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Vm.LoadSessionAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Vm.FeedbackVisibilityRequested -= OnFeedbackVisibilityRequested;
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
