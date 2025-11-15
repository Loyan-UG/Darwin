using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Shared.Integration;

/// <summary>
/// Camera scanner abstraction; provide platform-specific implementation in apps.
/// </summary>
public interface IScanner
{
    Task<string?> ScanAsync(CancellationToken ct);
}
