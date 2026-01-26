using CommunityToolkit.Maui;
using Darwin.Mobile.Business.Extensions;
using Darwin.Mobile.Business.Pages;
using Darwin.Mobile.Business.ViewModels;
using Darwin.Mobile.Business.Views;
using Darwin.Mobile.Shared.Common;
using Darwin.Mobile.Shared.Extensions;
using Darwin.Mobile.Shared.Integration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Hosting;
using UraniumUI;
using UraniumUI.Icons.FontAwesome;
using UraniumUI.Icons.MaterialIcons;
using UraniumUI.Material;

namespace Darwin.Mobile.Business
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseUraniumUI()
                .UseUraniumUIMaterial()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");

                    fonts.AddFontAwesomeIconFonts();
                    fonts.AddMaterialIconFonts();
                });

            builder.Services.AddBusinessApp(); // Keeps MauiProgram clean

            // DI registration
            builder.Services.AddSingleton<HomeViewModel>();
            builder.Services.AddSingleton<HomePage>();
            builder.Services.AddSingleton<ComingSoonPage>();
            builder.Services.AddSingleton<ScannerViewModel>();
            builder.Services.AddSingleton<ScannerPage>();



#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
