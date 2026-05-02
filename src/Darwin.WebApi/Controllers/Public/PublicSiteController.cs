using Darwin.Application.Settings.Queries;
using Darwin.Contracts.Meta;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Darwin.WebApi.Controllers.Public;

/// <summary>
/// Public site configuration endpoints for storefront runtime behavior.
/// </summary>
[ApiController]
[AllowAnonymous]
[EnableRateLimiting("public-content")]
[RequestTimeout("public-content")]
[Route("api/v1/public/site")]
public sealed class PublicSiteController : ApiControllerBase
{
    private readonly GetCulturesHandler _getCulturesHandler;

    public PublicSiteController(GetCulturesHandler getCulturesHandler)
    {
        _getCulturesHandler = getCulturesHandler ?? throw new ArgumentNullException(nameof(getCulturesHandler));
    }

    /// <summary>
    /// Returns non-sensitive public runtime configuration sourced from site settings.
    /// </summary>
    [HttpGet("runtime-config")]
    [ProducesResponseType(typeof(PublicSiteRuntimeConfigResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRuntimeConfigAsync(CancellationToken ct = default)
    {
        var (defaultCulture, cultures) = await _getCulturesHandler
            .HandleAsync(ct)
            .ConfigureAwait(false);

        var supportedCultures = cultures
            .Where(static culture => !string.IsNullOrWhiteSpace(culture))
            .Select(static culture => culture.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (supportedCultures.Length == 0)
        {
            supportedCultures = [defaultCulture];
        }

        return Ok(new PublicSiteRuntimeConfigResponse
        {
            DefaultCulture = defaultCulture,
            SupportedCultures = supportedCultures,
            MultilingualEnabled = supportedCultures.Length > 1
        });
    }
}
