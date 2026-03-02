using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Shared.Integration;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;

namespace Darwin.Mobile.Consumer.Services.Platform;

/// <summary>
/// MAUI location adapter used by discovery features.
/// </summary>
/// <remarks>
/// This implementation is intentionally defensive:
/// - It never throws permission/feature exceptions to view-model callers.
/// - It returns <c>null</c> when location cannot be resolved so callers can gracefully fallback.
/// </remarks>
public sealed class LocationPlatformService : ILocation
{
    /// <summary>
    /// Attempts to resolve current device coordinates.
    /// </summary>
    /// <param name="ct">Cancellation token propagated from the caller.</param>
    /// <returns>
    /// Current coordinates when available; otherwise <c>null</c>.
    /// </returns>
    public async Task<(double lat, double lng)?> GetCurrentAsync(CancellationToken ct)
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    return null;
                }
            }

            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(8));
            var location = await Geolocation.Default.GetLocationAsync(request, ct);

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