#if ANDROID
using AndroidX.Core.View;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform;
using Color = Microsoft.Maui.Graphics.Color;

namespace Darwin.Mobile.Business.Services.Platform;

internal static partial class BusinessSystemBars
{
    public static partial void SetStatusBarColor(Color color, bool useDarkIcons)
    {
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        var window = activity?.Window;
        if (window is null)
        {
            return;
        }

        var androidColor = color.ToPlatform();

#pragma warning disable CA1422
        WindowCompat.SetDecorFitsSystemWindows(window, true);
        window.SetStatusBarColor(androidColor);
#pragma warning restore CA1422

        var insetsController = WindowCompat.GetInsetsController(window, window.DecorView);
        if (insetsController is not null)
        {
            insetsController.AppearanceLightStatusBars = useDarkIcons;
        }

        window.DecorView.PostDelayed(() =>
        {
#pragma warning disable CA1422
            window.SetStatusBarColor(androidColor);
#pragma warning restore CA1422
        }, 250);
    }
}
#endif
