using CommunityToolkit.Maui;
using Darwin.Mobile.Consumer.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Hosting;
using ZXing.Net.Maui;
using UraniumUI;
using UraniumUI.Icons.FontAwesome;
//using UraniumUI.Icons.MaterialIcons;
//using UraniumUI.Material;
using ZXing.Net.Maui.Controls;


namespace Darwin.Mobile.Consumer;

/// <summary>
/// Configures the MAUI application for the Consumer project.
/// </summary>
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseUraniumUI()
            .UseBarcodeReader()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFontAwesomeIconFonts();
            });

        // Register Consumer services, view models, and pages
        builder.Services.AddConsumerApp();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
