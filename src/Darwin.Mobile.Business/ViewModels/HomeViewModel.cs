using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace Darwin.Mobile.Business.ViewModels;

/// <summary>
/// View model for the business home page.
/// </summary>
public sealed partial class HomeViewModel : ObservableObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HomeViewModel"/> class.
    /// </summary>
    public HomeViewModel()
    {
        ScanCommand = new RelayCommand(OnScan);
    }

    /// <summary>
    /// Displayed greeting to the staff user.
    /// </summary>
    public string Greeting => "Welcome Loyan staff!";

    /// <summary>
    /// Command to navigate to the scanner page.
    /// </summary>
    public ICommand ScanCommand { get; }

    private void OnScan()
    {
        // TODO: Use Shell navigation to go to the scanner page (phase 1)
        // await Shell.Current.GoToAsync(Routes.Scanner);
    }
}
