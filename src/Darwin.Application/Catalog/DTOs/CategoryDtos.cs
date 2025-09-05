using System;
using System.Collections.Generic;

namespace Darwin.Application.Catalog.DTOs
{
    /// <summary>
    /// Translation slice for category data transfer.
    /// </summary>
    public sealed class CategoryTranslationDto
    {
        public string Culture { get; set; } = "de-DE";
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
    }

    /// <summary>
    /// Create payload for a category aggregate.
    /// </summary>
    public sealed class CategoryCreateDto
    {
        public Guid? ParentId { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; } = 0;
        public List<CategoryTranslationDto> Translations { get; set; } = new();
    }

    /// <summary>
    /// Edit payload for a category including RowVersion for optimistic concurrency.
    /// </summary>
    public sealed class CategoryEditDto
    {
        public Guid Id { get; set; }
        public Guid? ParentId { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; } = 0;
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public List<CategoryTranslationDto> Translations { get; set; } = new();
    }

    /// <summary>
    /// List item projection for categories.
    /// </summary>
    public sealed class CategoryListItemDto
    {
        public Guid Id { get; set; }
        public string? Name { get; set; } // name in requested or default culture
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public Guid? ParentId { get; set; }
    }
}
