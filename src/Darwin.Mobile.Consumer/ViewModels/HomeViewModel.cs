using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Darwin.Mobile.Consumer.Resources;
using System.Windows.Input;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// View model for the consumer application's home page.
/// </summary>
public sealed partial class HomeViewModel : ObservableObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HomeViewModel"/> class.
    /// </summary>
    public HomeViewModel()
    {
        StartCommand = new RelayCommand(OnStart);
    }

    /// <summary>
    /// Gets a greeting message displayed on the home page.
    /// </summary>
    public string Greeting => AppResources.HomeGreeting;

    /// <summary>
    /// Command executed when the user taps the start button.
    /// </summary>
    public ICommand StartCommand { get; }

    private void OnStart()
    {
        // Home page currently serves as a lightweight welcome surface.
        // Navigation entry points are provided by AppShell tabs.
    }
}
