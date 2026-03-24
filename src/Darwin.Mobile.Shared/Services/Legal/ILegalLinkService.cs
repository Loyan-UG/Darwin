using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Shared.Results;

namespace Darwin.Mobile.Shared.Services.Legal;

/// <summary>
/// Resolves and opens configured legal destinations in a configuration-driven manner.
/// </summary>
public interface ILegalLinkService
{
    /// <summary>
    /// Resolves a configured legal destination into a validated absolute URI.
    /// </summary>
    /// <param name="linkKind">The legal destination to resolve.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> that contains the validated absolute URI, or a failed result
    /// when the configured link is missing or invalid.
    /// </returns>
    Result<Uri> ResolveUri(LegalLinkKind linkKind);

    /// <summary>
    /// Opens the configured legal destination inside the app experience when possible and falls back to the system browser when needed.
    /// </summary>
    /// <param name="linkKind">The legal destination to open.</param>
    /// <param name="cancellationToken">Cancellation token propagated from the caller.</param>
    /// <returns>
    /// A successful <see cref="Result"/> when the open request was handed to the platform, or a failed result when configuration or launch failed.
    /// </returns>
    Task<Result> OpenAsync(LegalLinkKind linkKind, CancellationToken cancellationToken);
}
