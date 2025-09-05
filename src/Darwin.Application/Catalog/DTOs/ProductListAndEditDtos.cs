using System;
using System.Collections.Generic;

namespace Darwin.Application.Catalog.DTOs
{
    public sealed class ProductListItemDto
    {
        public Guid Id { get; set; }
        public string? DefaultName { get; set; }   // from any translation (e.g., default culture)
        public bool IsActive { get; set; }
        public bool IsVisible { get; set; }
        public int VariantCount { get; set; }
    }

    public sealed class ProductEditDto
    {
        public Guid Id { get; set; }
        public Guid? BrandId { get; set; }
        public Guid? PrimaryCategoryId { get; set; }
        public string Kind { get; set; } = "Simple";
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public List<ProductTranslationDto> Translations { get; set; } = new();
        public List<ProductVariantCreateDto> Variants { get; set; } = new();
    }
}
