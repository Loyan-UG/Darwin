using Darwin.Mobile.Business.ViewModels;
using Microsoft.Maui.Controls;

namespace Darwin.Mobile.Business.Views;

/// <summary>
/// Focused onboarding page for business invitation acceptance.
/// The page supports both token-entry and future query-based prefill via Shell.
/// </summary>
[QueryProperty(nameof(InvitationToken), "token")]
public partial class AcceptInvitationPage : ContentPage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AcceptInvitationPage"/> class.
    /// </summary>
    public AcceptInvitationPage(AcceptInvitationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        Shell.SetFlyoutBehavior(this, FlyoutBehavior.Disabled);
    }

    /// <summary>
    /// Receives an optional invitation token from Shell query parameters.
    /// </summary>
    public string InvitationToken
    {
        get => ((AcceptInvitationViewModel)BindingContext).InvitationToken ?? string.Empty;
        set => ((AcceptInvitationViewModel)BindingContext).InvitationToken = value;
    }

    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ((AcceptInvitationViewModel)BindingContext).OnAppearingAsync();
    }
}
