using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace Darwin.Mobile.Business.ViewModels;

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
    public string Greeting => "Welcome to Loyan Business!";

    /// <summary>
    /// Command executed when the user taps the start button.
    /// </summary>
    public ICommand StartCommand { get; }

    private void OnStart()
    {
        // TODO: Navigate to the first functional feature of phase 1.
    }
}

