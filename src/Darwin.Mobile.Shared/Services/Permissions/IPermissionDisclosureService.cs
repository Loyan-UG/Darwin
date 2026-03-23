using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Shared.Services.Permissions;

/// <summary>
/// Displays reusable just-in-time privacy disclosures before sensitive operating-system permissions are requested.
/// </summary>
public interface IPermissionDisclosureService
{
    /// <summary>
    /// Shows a disclosure prompt and returns whether the caller may continue with the native permission request.
    /// </summary>
    /// <param name="request">The disclosure content to show.</param>
    /// <param name="cancellationToken">Cancellation token propagated from the caller.</param>
    /// <returns><c>true</c> when the user chose to continue; otherwise <c>false</c>.</returns>
    Task<bool> ShowAsync(PermissionDisclosureRequest request, CancellationToken cancellationToken);
}
