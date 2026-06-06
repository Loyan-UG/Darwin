#if ANDROID
using Android.Views;
using AndroidX.Core.View;
using Google.Android.Material.BottomNavigation;
using Microsoft.Maui.Controls;
using AndroidView = Android.Views.View;

namespace Darwin.Mobile.Consumer;

/// <summary>
/// Applies Android-specific Shell chrome that MAUI Shell does not expose as XAML properties.
/// </summary>
internal static class ConsumerShellPlatformStyler
{
    private const float BottomNavigationElevation = 18f;

    public static void Apply(AndroidView? rootView)
    {
        rootView?.PostDelayed(() =>
        {
            var bottomNavigationView = FindDescendant<BottomNavigationView>(rootView);
            if (bottomNavigationView is null)
            {
                return;
            }

            bottomNavigationView.Elevation = BottomNavigationElevation;
            ViewCompat.SetElevation(bottomNavigationView, BottomNavigationElevation);
            bottomNavigationView.SetOnItemReselectedListener(new TabReselectedListener());
        }, 250);
    }

    private static TView? FindDescendant<TView>(AndroidView view)
        where TView : AndroidView
    {
        if (view is TView match)
        {
            return match;
        }

        if (view is not ViewGroup viewGroup)
        {
            return null;
        }

        for (var i = 0; i < viewGroup.ChildCount; i++)
        {
            var child = viewGroup.GetChildAt(i);
            if (child is null)
            {
                continue;
            }

            var descendant = FindDescendant<TView>(child);
            if (descendant is not null)
            {
                return descendant;
            }
        }

        return null;
    }

    private sealed class TabReselectedListener : Java.Lang.Object, BottomNavigationView.IOnItemReselectedListener
    {
        public void OnNavigationItemReselected(Android.Views.IMenuItem item)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    if (Shell.Current is AppShell appShell)
                    {
                        await appShell.ResetCurrentTabToRootAsync();
                    }
                    else
                    {
                        await Shell.Current.Navigation.PopToRootAsync(false);
                    }
                }
                catch
                {
                    // Native reselect callbacks can arrive during transitions; ignore stale navigation state.
                }
            });
        }
    }
}
#endif
