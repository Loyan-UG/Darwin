using Darwin.Mobile.Business.Models;
using Darwin.Mobile.Business.PageModels;

namespace Darwin.Mobile.Business.Pages
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