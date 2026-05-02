using CommunityToolkit.Maui;
using Darwin.Mobile.Business.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Syncfusion.Maui.Toolkit.Hosting;
using UraniumUI;
using UraniumUI.Icons.FontAwesome;
using UraniumUI.Icons.MaterialIcons;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
#if ANDROID
using Android.Content.Res;
#endif

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
            .ConfigureSyncfusionToolkit()
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseUraniumUI()
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
        ConfigurePlatformControlColors();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    /// <summary>
    /// Forces native input controls to use the approved Business gold tint instead of platform defaults.
    /// </summary>
    private static void ConfigurePlatformControlColors()
    {
#if ANDROID
        var goldTint = ColorStateList.ValueOf(Android.Graphics.Color.ParseColor("#F4B223"));
        var goldHighlight = Android.Graphics.Color.ParseColor("#F4B223");

        EntryHandler.Mapper.AppendToMapping("BusinessGoldInputTint", (handler, view) =>
        {
            handler.PlatformView.BackgroundTintList = goldTint;
            handler.PlatformView.SetHighlightColor(goldHighlight);
        });

        EditorHandler.Mapper.AppendToMapping("BusinessGoldInputTint", (handler, view) =>
        {
            handler.PlatformView.BackgroundTintList = goldTint;
            handler.PlatformView.SetHighlightColor(goldHighlight);
        });

        PickerHandler.Mapper.AppendToMapping("BusinessGoldInputTint", (handler, view) =>
        {
            handler.PlatformView.BackgroundTintList = goldTint;
            handler.PlatformView.SetHighlightColor(goldHighlight);
        });
#endif
    }
}
