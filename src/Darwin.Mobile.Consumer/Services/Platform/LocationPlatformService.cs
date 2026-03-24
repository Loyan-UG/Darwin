using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Consumer.Resources;
using Darwin.Mobile.Shared.Integration;
using Darwin.Mobile.Shared.Services.Permissions;
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
/// - It always shows a just-in-time privacy disclosure before requesting the operating-system permission.
/// </remarks>
public sealed class LocationPlatformService : ILocation
{
    private readonly IPermissionDisclosureService _permissionDisclosureService;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocationPlatformService"/> class.
    /// </summary>
    /// <param name="permissionDisclosureService">Service used to show a privacy disclosure before requesting location access.</param>
    public LocationPlatformService(IPermissionDisclosureService permissionDisclosureService)
    {
        _permissionDisclosureService = permissionDisclosureService ?? throw new ArgumentNullException(nameof(permissionDisclosureService));
    }

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
                var shouldProceed = await _permissionDisclosureService.ShowAsync(new PermissionDisclosureRequest
                {
                    Title = AppResources.LocationDisclosureTitle,
                    PermissionName = AppResources.LocationDisclosurePermissionName,
                    WhyThisIsNeeded = AppResources.LocationDisclosurePurpose,
                    FeatureRequirementText = AppResources.LocationDisclosureRequirement,
                    ContinueButtonText = AppResources.PermissionDisclosureContinueButton,
                    CancelButtonText = AppResources.PermissionDisclosureCancelButton,
                    LegalReferenceButtonText = AppResources.PermissionDisclosurePrivacyButton,
                    LegalReferenceKind = Darwin.Mobile.Shared.Services.Legal.LegalLinkKind.PrivacyPolicy
                }, ct).ConfigureAwait(false);

                if (!shouldProceed)
                {
                    return null;
                }

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
