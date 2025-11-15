using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Shared.Integration;

/// <summary>
/// Geolocation abstraction; optional for features like nearby discovery.
/// </summary>
public interface ILocation
{
    Task<(double lat, double lng)?> GetCurrentAsync(CancellationToken ct);
}
