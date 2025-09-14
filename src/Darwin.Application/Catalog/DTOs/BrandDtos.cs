using System;
using System.Collections.Generic;

namespace Darwin.Application.Catalog.DTOs
{
    /// <summary>
    /// Represents a translation slice for a brand. Includes culture-specific fields.
    /// </summary>
    public sealed class BrandTranslationDto
    {
        public string Culture { get; set; } = "de-DE";
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
    }

    /// <summary>
    /// DTO for creating a new brand.
    /// </summary>
    public sealed class BrandCreateDto
    {
        public List<BrandTranslationDto> Translations { get; set; } = new();
    }

    /// <summary>
    /// DTO for editing an existing brand. Includes Id and RowVersion for concurrency.
    /// </summary>
    public sealed class BrandEditDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public List<BrandTranslationDto> Translations { get; set; } = new();
    }

    /// <summary>
    /// Simplified DTO for listing brands.
    /// </summary>
    public sealed class BrandListItemDto
    {
        public Guid Id { get; set; }
        public string? DefaultName { get; set; }   // derived from the default culture
    }
}
