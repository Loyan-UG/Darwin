using Darwin.Mobile.Business.ViewModels;
using Microsoft.Maui.Controls;

namespace Darwin.Mobile.Business.Views;

/// <summary>
/// Displays the session summary after scanning a QR code.
/// </summary>
[QueryProperty(nameof(SessionToken), "token")]
public partial class SessionPage : ContentPage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionPage"/> class.
    /// </summary>
    public SessionPage(SessionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <summary>
    /// Gets or sets the session token passed via navigation.
    /// Setting this property on the ViewModel triggers data loading.
    /// </summary>
    public string SessionToken
    {
        get => ((SessionViewModel)BindingContext).SessionToken;
        set => ((SessionViewModel)BindingContext).SessionToken = value;
    }

    /// <summary>
    /// When the page appears, explicitly invoke the LoadSessionAsync method on the view model.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var viewModel = (SessionViewModel)BindingContext;
        await viewModel.LoadSessionAsync();
    }
}
