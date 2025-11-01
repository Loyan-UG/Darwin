using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Darwin.Web.Areas.Admin.ViewModels.Catalog
{
    /// <summary>
    /// View model representing a single row in the brands list/grid.
    /// Kept minimal for fast rendering and paging.
    /// </summary>
    public sealed class BrandListItemVm
    {
        /// <summary>Primary key.</summary>
        public Guid Id { get; set; }

        /// <summary>Localized display name chosen by the query (per current culture with fallback).</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Optional culture-invariant slug for brand landing pages.</summary>
        public string? Slug { get; set; }

        /// <summary>Last modified timestamp in UTC (optional).</summary>
        public DateTime? ModifiedAtUtc { get; set; }

        /// <summary>
        /// Concurrency token required by delete operations in lists (modal), so the Index view
        /// can submit RowVersion along with the Id.
        /// </summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// View model for the brands listing page, including paging metadata and current search/filter state.
    /// Items are projected to <see cref="BrandListItemVm"/> (not DTO) to keep Web layer decoupled.
    /// </summary>
    public sealed class BrandsListVm
    {
        /// <summary>1-based page number.</summary>
        public int Page { get; set; }

        /// <summary>Items per page.</summary>
        public int PageSize { get; set; }

        /// <summary>Total rows matching the current filter.</summary>
        public int Total { get; set; }

        /// <summary>Optional search query (free text, depends on controller/query behavior).</summary>
        public string Query { get; set; } = string.Empty;

        /// <summary>Current page items.</summary>
        public List<BrandListItemVm> Items { get; set; } = new();

        /// <summary>Prebuilt items for a page-size dropdown in the Index view.</summary>
        public IEnumerable<SelectListItem> PageSizeItems { get; set; } = Array.Empty<SelectListItem>();
    }

    /// <summary>
    /// Localized fields for brand forms. Mirrors Application-layer BrandTranslationDto.
    /// </summary>
    public sealed class BrandTranslationVm
    {
        /// <summary>Culture code (e.g., "de-DE"). Required.</summary>
        public string Culture { get; set; } = "de-DE";

        /// <summary>Localized brand name. Required.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional HTML description. IMPORTANT: This is raw HTML coming from the editor; the
        /// Application layer is responsible for sanitizing it before persistence.
        /// </summary>
        public string? DescriptionHtml { get; set; }
    }

    /// <summary>
    /// View model for "Create Brand" form. Matches Application-layer BrandCreateDto contract.
    /// </summary>
    public sealed class BrandCreateVm
    {
        /// <summary>
        /// Optional culture-invariant slug. If provided, it must be unique; the Application layer validates it.
        /// </summary>
        public string? Slug { get; set; }

        /// <summary>
        /// Optional logo media Id. Actual media picking/upload is handled in the Web layer.
        /// </summary>
        public Guid? LogoMediaId { get; set; }

        /// <summary>At least one translation row is required (enforced by the Application layer validator).</summary>
        public List<BrandTranslationVm> Translations { get; set; } = new();
    }

    /// <summary>
    /// View model for "Edit Brand" form. Includes identity and concurrency token.
    /// </summary>
    public sealed class BrandEditVm
    {
        /// <summary>Primary key of the brand being edited.</summary>
        public Guid Id { get; set; }

        /// <summary>Concurrency token required for optimistic concurrency. Must be posted back unchanged.</summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        /// <summary>Optional culture-invariant slug. Must remain globally unique if provided.</summary>
        public string? Slug { get; set; }

        /// <summary>Optional logo media Id.</summary>
        public Guid? LogoMediaId { get; set; }

        /// <summary>Localized fields. Must contain at least one item.</summary>
        public List<BrandTranslationVm> Translations { get; set; } = new();
    }
}
