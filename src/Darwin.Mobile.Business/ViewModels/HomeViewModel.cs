// File: src/Darwin.Mobile.Business/ViewModels/HomeViewModel.cs
using System;
using System.Windows.Input;
using Darwin.Mobile.Shared.Navigation;
using Darwin.Mobile.Business.Constants;
using Darwin.Mobile.Business.Resources;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Darwin.Mobile.Business.ViewModels
{
    /// <summary>
    /// View model for the business home page.
    /// Displays a greeting and exposes a command to navigate to the scanner.
    /// </summary>
    public sealed partial class HomeViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="HomeViewModel"/> class.
        /// </summary>
        /// <param name="navigationService">Navigation service used to navigate between pages.</param>
        public HomeViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            ScanCommand = new RelayCommand(OnScan);
        }

        /// <summary>
        /// The greeting text displayed on the home page.
        /// Pulled from localized resources.
        /// </summary>
        public string Greeting => AppResources.HomeTitle;

        /// <summary>
        /// Command executed when the user taps the “Start” button on the home page.
        /// Navigates to the scanner page.
        /// </summary>
        public ICommand ScanCommand { get; }

        private async void OnScan()
        {
            // Use the navigation service to navigate to the Scanner page.
            // The double-slash prefix resets the navigation stack to ensure clean back navigation.
            await _navigationService.GoToAsync($"//{Routes.Scanner}");
        }
    }
}
