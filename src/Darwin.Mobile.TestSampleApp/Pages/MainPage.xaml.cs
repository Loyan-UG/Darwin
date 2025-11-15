using Darwin.Mobile.TestSampleApp.Models;
using Darwin.Mobile.TestSampleApp.PageModels;

namespace Darwin.Mobile.TestSampleApp.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
        }
    }
}