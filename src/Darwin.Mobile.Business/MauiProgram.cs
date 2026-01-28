using CommunityToolkit.Maui;
using Darwin.Mobile.Business.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Hosting;
using UraniumUI;
using UraniumUI.Icons.FontAwesome;
using UraniumUI.Icons.MaterialIcons;
using UraniumUI.Material;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace Darwin.Mobile.Business;

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

        // Register Business services, pages, and view models.
        builder.Services.AddBusinessApp();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
