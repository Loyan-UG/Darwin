using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Identity.DTOs;
using Darwin.Shared.Results;

namespace Darwin.Application.Abstractions.Auth;

/// <summary>
/// Verifies third-party identity tokens without exposing provider SDKs to Application.
/// Infrastructure owns concrete provider validation and SDK dependencies.
/// </summary>
public interface IExternalIdentityVerifier
{
    /// <summary>
    /// Validates the supplied provider token and returns a normalized external identity.
    /// Implementations must not persist or log provider tokens.
    /// </summary>
    Task<Result<ExternalIdentityDto>> VerifyAsync(ExternalLoginRequestDto request, CancellationToken ct = default);
}
