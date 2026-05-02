using System;
using Darwin.Mobile.Consumer.ViewModels;

namespace Darwin.Mobile.Consumer.Views;

/// <summary>
/// Displays canonical member privacy and communication preferences.
/// </summary>
public partial class MemberPreferencesPage : ContentPage
{
    private readonly MemberPreferencesViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemberPreferencesPage"/> class.
    /// </summary>
    public MemberPreferencesPage(MemberPreferencesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        BindingContext = _viewModel;
    }

    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            await _viewModel.OnAppearingAsync();
        }
        catch
        {
            // Appearing is an async-void MAUI lifecycle hook. Preference load failures stay inside ViewModel feedback.
        }
    }

    /// <inheritdoc />
    protected override async void OnDisappearing()
    {
        try
        {
            await _viewModel.OnDisappearingAsync();
        }
        catch
        {
            // Disappearing cleanup should never crash navigation away from preferences.
        }
        finally
        {
            base.OnDisappearing();
        }
    }
}
