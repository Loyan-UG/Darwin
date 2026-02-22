using Darwin.Mobile.Business.ViewModels;
using Microsoft.Maui.Controls;

namespace Darwin.Mobile.Business.Views
{
    /// <summary>
    /// Home page code-behind.
    /// 
    /// Why we call the VM here:
    /// - We want business context to be loaded right when the page appears.
    /// - Keeping this trigger in view lifecycle avoids implicit constructor-side async work.
    /// </summary>
    public partial class HomePage : ContentPage
    {
        public HomePage(HomeViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext is HomeViewModel vm)
            {
                await vm.OnAppearingAsync();
            }
        }
    }
}
