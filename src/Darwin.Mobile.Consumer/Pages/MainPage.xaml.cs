using Darwin.Mobile.Consumer.Models;
using Darwin.Mobile.Consumer.PageModels;

namespace Darwin.Mobile.Consumer.Pages
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