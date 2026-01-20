using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// View model for the home page.
/// </summary>
public sealed partial class HomeViewModel : ObservableObject
{
    public HomeViewModel()
    {
        StartCommand = new RelayCommand(OnStart);
    }

    public string Greeting => "Welcome to Loyan!";

    public ICommand StartCommand { get; }

    private void OnStart()
    {
        // TODO: Navigate to the actual first feature of phase 1
    }
}
