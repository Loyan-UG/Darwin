using Darwin.Application.CMS.DTOs;
using Darwin.Application.CMS.Queries;
using Darwin.Application.Settings.DTOs;
using Darwin.Contracts.Cms;
using Darwin.Contracts.Common;
using Microsoft.AspNetCore.Mvc;

namespace Darwin.WebApi.Controllers.Public;

/// <summary>
/// Public CMS delivery endpoints for storefront content, menus, and SEO-friendly page lookup.
/// </summary>
[ApiController]
[Route("api/v1/public/cms")]
public sealed class PublicCmsController : ApiControllerBase
{
    private readonly GetPublishedPagesPageHandler _getPublishedPagesPageHandler;
    private readonly GetPublishedPageBySlugHandler _getPublishedPageBySlugHandler;
    private readonly GetPublicMenuByNameHandler _getPublicMenuByNameHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="PublicCmsController"/> class.
    /// </summary>
    public PublicCmsController(
        GetPublishedPagesPageHandler getPublishedPagesPageHandler,
        GetPublishedPageBySlugHandler getPublishedPageBySlugHandler,
        GetPublicMenuByNameHandler getPublicMenuByNameHandler)
    {
        _getPublishedPagesPageHandler = getPublishedPagesPageHandler ?? throw new ArgumentNullException(nameof(getPublishedPagesPageHandler));
        _getPublishedPageBySlugHandler = getPublishedPageBySlugHandler ?? throw new ArgumentNullException(nameof(getPublishedPageBySlugHandler));
        _getPublicMenuByNameHandler = getPublicMenuByNameHandler ?? throw new ArgumentNullException(nameof(getPublicMenuByNameHandler));
    }

    /// <summary>
    /// Returns a paged list of published CMS pages.
    /// </summary>
    [HttpGet("pages")]
    [HttpGet("/api/v1/cms/pages")]
    [ProducesResponseType(typeof(PagedResponse<PublicPageSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPagesAsync([FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] string? culture, CancellationToken ct = default)
    {
        var normalizedPage = page.GetValueOrDefault(1);
        if (normalizedPage <= 0)
        {
            return BadRequestProblem("Page must be a positive integer.");
        }

        var normalizedPageSize = pageSize.GetValueOrDefault(20);
        if (normalizedPageSize <= 0 || normalizedPageSize > 200)
        {
            return BadRequestProblem("PageSize must be between 1 and 200.");
        }

        var normalizedCulture = string.IsNullOrWhiteSpace(culture) ? SiteSettingDto.DefaultCultureDefault : culture.Trim();
        var (items, total) = await _getPublishedPagesPageHandler
            .HandleAsync(normalizedPage, normalizedPageSize, normalizedCulture, ct)
            .ConfigureAwait(false);

        return Ok(new PagedResponse<PublicPageSummary>
        {
            Total = total,
            Items = items.Select(MapPageSummary).ToList(),
            Request = new PagedRequest
            {
                Page = normalizedPage,
                PageSize = normalizedPageSize,
                Search = null
            }
        });
    }

    /// <summary>
    /// Returns a published CMS page by localized slug.
    /// </summary>
    [HttpGet("pages/{slug}")]
    [HttpGet("/api/v1/cms/pages/{slug}")]
    [ProducesResponseType(typeof(PublicPageDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPageBySlugAsync([FromRoute] string slug, [FromQuery] string? culture, CancellationToken ct = default)
    {
        var dto = await _getPublishedPageBySlugHandler
            .HandleAsync(slug, string.IsNullOrWhiteSpace(culture) ? SiteSettingDto.DefaultCultureDefault : culture.Trim(), ct)
            .ConfigureAwait(false);

        return dto is null
            ? NotFoundProblem("Page not found.")
            : Ok(MapPageDetail(dto));
    }

    /// <summary>
    /// Returns a public menu by internal name for storefront navigation.
    /// </summary>
    [HttpGet("menus/{name}")]
    [HttpGet("/api/v1/cms/menus/{name}")]
    [ProducesResponseType(typeof(PublicMenu), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMenuByNameAsync([FromRoute] string name, [FromQuery] string? culture, CancellationToken ct = default)
    {
        var dto = await _getPublicMenuByNameHandler
            .HandleAsync(name, string.IsNullOrWhiteSpace(culture) ? SiteSettingDto.DefaultCultureDefault : culture.Trim(), ct)
            .ConfigureAwait(false);

        return dto is null
            ? NotFoundProblem("Menu not found.")
            : Ok(MapMenu(dto));
    }

    private static PublicPageSummary MapPageSummary(PublicPageSummaryDto dto)
        => new()
        {
            Id = dto.Id,
            Title = dto.Title,
            Slug = dto.Slug,
            MetaTitle = dto.MetaTitle,
            MetaDescription = dto.MetaDescription
        };

    private static PublicPageDetail MapPageDetail(PublicPageDetailDto dto)
        => new()
        {
            Id = dto.Id,
            Title = dto.Title,
            Slug = dto.Slug,
            MetaTitle = dto.MetaTitle,
            MetaDescription = dto.MetaDescription,
            ContentHtml = dto.ContentHtml
        };

    private static PublicMenu MapMenu(PublicMenuDto dto)
        => new()
        {
            Id = dto.Id,
            Name = dto.Name,
            Items = dto.Items.Select(item => new PublicMenuItem
            {
                Id = item.Id,
                ParentId = item.ParentId,
                Label = item.Label,
                Url = item.Url,
                SortOrder = item.SortOrder
            }).ToList()
        };
}
