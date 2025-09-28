using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Abstractions.Services
{
    /// <summary>
    /// Provides WebAuthn/FIDO2 relying party settings (RpId and Origin) from site configuration.
    /// </summary>
    public interface IWebAuthnSettingsProvider
    {
        /// <summary>
        /// Returns the current WebAuthn configuration parameters.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        Task<(string RpId, string Origin)> GetAsync(CancellationToken ct);
    }
}
