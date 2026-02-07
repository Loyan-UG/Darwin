using CommunityToolkit.Maui;
using Darwin.Mobile.Consumer.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Hosting;
using ZXing.Net.Maui;
using UraniumUI;
using UraniumUI.Icons.FontAwesome;
using ZXing.Net.Maui.Controls;
using UraniumUI.Icons.MaterialIcons;
using UraniumUI.Material;


namespace Darwin.Mobile.Consumer;

/// <summary>
/// Configures the MAUI application for the Business project.
/// </summary>
public static class MauiProgram
{
    /// <summary>
    /// Creates and configures the MAUI application.
    /// </summary>
    /// <returns>The configured <see cref="MauiApp"/>.</returns>
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseUraniumUI()
            .UseUraniumUIMaterial()
            .UseBarcodeReader() // Registers ZXing barcode scanner services
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");

                fonts.AddFontAwesomeIconFonts();
                fonts.AddMaterialIconFonts();
            });

        // Register Consumer services, pages, and view models.
        builder.Services.AddConsumerApp();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
