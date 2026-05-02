using CommunityToolkit.Maui;
using Darwin.Mobile.Consumer.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Syncfusion.Maui.Toolkit.Hosting;
using ZXing.Net.Maui;
using UraniumUI;
using UraniumUI.Icons.FontAwesome;
using ZXing.Net.Maui.Controls;
using UraniumUI.Icons.MaterialIcons;
#if ANDROID
using Android.Content.Res;
#endif


namespace Darwin.Mobile.Consumer;

/// <summary>
/// Configures the MAUI application for the Consumer project.
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
            .UseMauiMaps()
            .UseUraniumUI()
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
        ConfigurePlatformControlColors();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    /// <summary>
    /// Forces native input controls to use the approved Consumer gold tint instead of platform defaults.
    /// </summary>
    private static void ConfigurePlatformControlColors()
    {
#if ANDROID
        var goldTint = ColorStateList.ValueOf(Android.Graphics.Color.ParseColor("#F4B223"));
        var goldHighlight = Android.Graphics.Color.ParseColor("#F4B223");

        EntryHandler.Mapper.AppendToMapping("ConsumerGoldInputTint", (handler, view) =>
        {
            handler.PlatformView.BackgroundTintList = goldTint;
            handler.PlatformView.SetHighlightColor(goldHighlight);
        });

        EditorHandler.Mapper.AppendToMapping("ConsumerGoldInputTint", (handler, view) =>
        {
            handler.PlatformView.BackgroundTintList = goldTint;
            handler.PlatformView.SetHighlightColor(goldHighlight);
        });

        PickerHandler.Mapper.AppendToMapping("ConsumerGoldInputTint", (handler, view) =>
        {
            handler.PlatformView.BackgroundTintList = goldTint;
            handler.PlatformView.SetHighlightColor(goldHighlight);
        });
#endif
    }
}
