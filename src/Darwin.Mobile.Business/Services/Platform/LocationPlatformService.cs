using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Shared.Integration;
using Microsoft.Maui.Devices.Sensors;

namespace Darwin.Mobile.Business.Services.Platform;

/// <summary>
/// MAUI geolocation adapter for the Business app.
/// </summary>
/// <remarks>
/// The Business app does not require location for the core scan flow,
/// but this service keeps the shared contract complete for nearby/discovery
/// features that may be enabled in future releases.
///
/// Behavior is intentionally fail-safe:
/// - Returns <c>null</c> when permission is denied, location is unavailable,
///   or platform services are disabled.
/// - Never throws platform-specific exceptions to caller view models.
/// </remarks>
public sealed class LocationPlatformService : ILocation
{
    /// <summary>
    /// Attempts to resolve current device coordinates using platform location APIs.
    /// </summary>
    /// <param name="ct">Cancellation token propagated by the caller.</param>
    /// <returns>
    /// Latitude/longitude tuple when available; otherwise <c>null</c>.
    /// </returns>
    public async Task<(double lat, double lng)?> GetCurrentAsync(CancellationToken ct)
    {
        try
        {
            var permissionStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>().ConfigureAwait(false);
            if (permissionStatus != PermissionStatus.Granted)
            {
                permissionStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>().ConfigureAwait(false);
                if (permissionStatus != PermissionStatus.Granted)
                {
                    return null;
                }
            }

            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(8));
            var location = await Geolocation.Default.GetLocationAsync(request, ct).ConfigureAwait(false);
            if (location is null)
            {
                return null;
            }

            return (location.Latitude, location.Longitude);
        }
        catch
        {
            return null;
        }
    }
}
