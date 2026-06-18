using System;
using System.Linq;
using System.Threading.Tasks;
using Darwin.Mobile.Consumer.Constants;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace Darwin.Mobile.Consumer.Services.Notifications;

public static class NotificationDeepLinkNavigator
{
    private static string? _pendingDeepLink;

    public static void HandleIncomingDeepLink(string? deepLink)
    {
        if (string.IsNullOrWhiteSpace(deepLink))
        {
            return;
        }

        _pendingDeepLink = deepLink.Trim();
        MainThread.BeginInvokeOnMainThread(async () => await TryNavigatePendingAsync().ConfigureAwait(false));
    }

    public static async Task TryNavigatePendingAsync()
    {
        var deepLink = _pendingDeepLink;
        if (string.IsNullOrWhiteSpace(deepLink) || Shell.Current is null)
        {
            return;
        }

        var route = ResolveRoute(deepLink) ?? Routes.Notifications;
        try
        {
            await Shell.Current.GoToAsync(route).ConfigureAwait(false);
            _pendingDeepLink = null;
        }
        catch
        {
            if (!route.Equals(Routes.Notifications, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    await Shell.Current.GoToAsync(Routes.Notifications).ConfigureAwait(false);
                    _pendingDeepLink = null;
                }
                catch
                {
                    // Keep the pending link for the next authenticated shell.
                }
            }
        }
    }

    private static string? ResolveRoute(string? deepLink)
    {
        if (string.IsNullOrWhiteSpace(deepLink) ||
            !Uri.TryCreate(deepLink.Trim(), UriKind.Absolute, out var uri) ||
            !uri.Scheme.Equals("loyan", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var host = uri.Host.ToLowerInvariant();
        var firstSegment = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return host switch
        {
            "business" when Guid.TryParse(firstSegment, out var businessId) => $"{Routes.BusinessDetail}/{businessId:D}",
            "rewards" => $"//{Routes.Rewards}",
            "qr" => $"//{Routes.Qr}",
            "feed" or "campaign" => $"//{Routes.Feed}",
            _ => null
        };
    }
}
