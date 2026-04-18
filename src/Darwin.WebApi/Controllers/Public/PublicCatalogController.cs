using Darwin.Application.Catalog.DTOs;
using Darwin.Application.Catalog.Queries;
using Darwin.Application.Settings.DTOs;
using Darwin.Contracts.Catalog;
using Darwin.Contracts.Common;
using Microsoft.AspNetCore.Mvc;

namespace Darwin.WebApi.Controllers.Public;

/// <summary>
/// Public catalog delivery endpoints for storefront listing and product-detail pages.
/// </summary>
[ApiController]
[Route("api/v1/public/catalog")]
public sealed class PublicCatalogController : ApiControllerBase
{
    private readonly GetPublishedCategoriesHandler _getPublishedCategoriesHandler;
    private readonly GetPublishedProductsPageHandler _getPublishedProductsPageHandler;
    private readonly GetPublishedProductBySlugHandler _getPublishedProductBySlugHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="PublicCatalogController"/> class.
    /// </summary>
    public PublicCatalogController(
        GetPublishedCategoriesHandler getPublishedCategoriesHandler,
        GetPublishedProductsPageHandler getPublishedProductsPageHandler,
        GetPublishedProductBySlugHandler getPublishedProductBySlugHandler)
    {
        _getPublishedCategoriesHandler = getPublishedCategoriesHandler ?? throw new ArgumentNullException(nameof(getPublishedCategoriesHandler));
        _getPublishedProductsPageHandler = getPublishedProductsPageHandler ?? throw new ArgumentNullException(nameof(getPublishedProductsPageHandler));
        _getPublishedProductBySlugHandler = getPublishedProductBySlugHandler ?? throw new ArgumentNullException(nameof(getPublishedProductBySlugHandler));
    }

    /// <summary>
    /// Returns a paged list of published categories for public storefront delivery.
    /// </summary>
    [HttpGet("categories")]
    [HttpGet("/api/v1/catalog/categories")]
    [ProducesResponseType(typeof(PagedResponse<PublicCategorySummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCategoriesAsync([FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] string? culture, CancellationToken ct = default)
    {
        var normalizedPage = page.GetValueOrDefault(1);
        if (normalizedPage <= 0)
        {
            return BadRequestProblem("Page must be a positive integer.");
        }

        var normalizedPageSize = pageSize.GetValueOrDefault(50);
        if (normalizedPageSize <= 0 || normalizedPageSize > 200)
        {
            return BadRequestProblem("PageSize must be between 1 and 200.");
        }

        var normalizedCulture = string.IsNullOrWhiteSpace(culture) ? SiteSettingDto.DefaultCultureDefault : culture.Trim();
        var (items, total) = await _getPublishedCategoriesHandler
            .HandleAsync(normalizedPage, normalizedPageSize, normalizedCulture, ct)
            .ConfigureAwait(false);

        return Ok(new PagedResponse<PublicCategorySummary>
        {
            Total = total,
            Items = items.Select(MapCategory).ToList(),
            Request = new PagedRequest
            {
                Page = normalizedPage,
                PageSize = normalizedPageSize,
                Search = null
            }
        });
    }

    /// <summary>
    /// Returns a paged list of published products for public storefront delivery.
    /// </summary>
    [HttpGet("products")]
    [HttpGet("/api/v1/catalog/products")]
    [ProducesResponseType(typeof(PagedResponse<PublicProductSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetProductsAsync(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? culture,
        [FromQuery] string? categorySlug,
        CancellationToken ct = default)
    {
        var normalizedPage = page.GetValueOrDefault(1);
        if (normalizedPage <= 0)
        {
            return BadRequestProblem("Page must be a positive integer.");
        }

        var normalizedPageSize = pageSize.GetValueOrDefault(24);
        if (normalizedPageSize <= 0 || normalizedPageSize > 200)
        {
            return BadRequestProblem("PageSize must be between 1 and 200.");
        }

        var normalizedCulture = string.IsNullOrWhiteSpace(culture) ? SiteSettingDto.DefaultCultureDefault : culture.Trim();
        var (items, total) = await _getPublishedProductsPageHandler
            .HandleAsync(normalizedPage, normalizedPageSize, normalizedCulture, categorySlug, ct)
            .ConfigureAwait(false);

        return Ok(new PagedResponse<PublicProductSummary>
        {
            Total = total,
            Items = items.Select(MapProductSummary).ToList(),
            Request = new PagedRequest
            {
                Page = normalizedPage,
                PageSize = normalizedPageSize,
                Search = categorySlug
            }
        });
    }

    /// <summary>
    /// Returns a published product detail by localized slug.
    /// </summary>
    [HttpGet("products/{slug}")]
    [HttpGet("/api/v1/catalog/products/{slug}")]
    [ProducesResponseType(typeof(PublicProductDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductBySlugAsync([FromRoute] string slug, [FromQuery] string? culture, CancellationToken ct = default)
    {
        var dto = await _getPublishedProductBySlugHandler
            .HandleAsync(slug, string.IsNullOrWhiteSpace(culture) ? SiteSettingDto.DefaultCultureDefault : culture.Trim(), ct)
            .ConfigureAwait(false);

        return dto is null
            ? NotFoundProblem("Product not found.")
            : Ok(MapProductDetail(dto));
    }

    private static PublicCategorySummary MapCategory(PublicCategorySummaryDto dto)
        => new()
        {
            Id = dto.Id,
            ParentId = dto.ParentId,
            Name = dto.Name,
            Slug = dto.Slug,
            Description = dto.Description,
            SortOrder = dto.SortOrder
        };

    private static PublicProductSummary MapProductSummary(PublicProductSummaryDto dto)
        => new()
        {
            Id = dto.Id,
            Name = dto.Name,
            Slug = dto.Slug,
            ShortDescription = dto.ShortDescription,
            Currency = dto.Currency,
            PriceMinor = dto.PriceMinor,
            CompareAtPriceMinor = dto.CompareAtPriceMinor,
            PrimaryImageUrl = dto.PrimaryImageUrl
        };

    private static PublicProductDetail MapProductDetail(PublicProductDetailDto dto)
        => new()
        {
            Id = dto.Id,
            Name = dto.Name,
            Slug = dto.Slug,
            ShortDescription = dto.ShortDescription,
            Currency = dto.Currency,
            PriceMinor = dto.PriceMinor,
            CompareAtPriceMinor = dto.CompareAtPriceMinor,
            PrimaryImageUrl = dto.PrimaryImageUrl,
            FullDescriptionHtml = dto.FullDescriptionHtml,
            MetaTitle = dto.MetaTitle,
            MetaDescription = dto.MetaDescription,
            PrimaryCategoryId = dto.PrimaryCategoryId,
            Variants = dto.Variants.Select(variant => new PublicProductVariant
            {
                Id = variant.Id,
                Sku = variant.Sku,
                Currency = variant.Currency,
                BasePriceNetMinor = variant.BasePriceNetMinor,
                CompareAtPriceNetMinor = variant.CompareAtPriceNetMinor,
                BackorderAllowed = variant.BackorderAllowed,
                IsDigital = variant.IsDigital
            }).ToList(),
            Media = dto.Media.Select(media => new PublicProductMedia
            {
                Id = media.Id,
                Url = media.Url,
                Alt = media.Alt,
                Title = media.Title,
                Role = media.Role,
                SortOrder = media.SortOrder
            }).ToList()
        };
}
