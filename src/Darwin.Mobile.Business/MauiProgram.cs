using Microsoft.Extensions.Logging;
using Darwin.Mobile.Shared.Common;
using Darwin.Mobile.Shared.Extensions;
using Darwin.Mobile.Shared.Integration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;
using Darwin.Mobile.Business.Extensions;
using CommunityToolkit.Maui;
using Darwin.Mobile.Business.ViewModels;
using Darwin.Mobile.Business.Views;

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
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddBusinessApp(); // Keeps MauiProgram clean

            // DI registration
            builder.Services.AddSingleton<HomeViewModel>();
            builder.Services.AddSingleton<HomePage>();
            builder.Services.AddSingleton<ComingSoonPage>();


#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
