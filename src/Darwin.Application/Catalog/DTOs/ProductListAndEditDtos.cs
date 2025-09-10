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

    /// <summary>
    ///     DTO for editing products, including identity (<c>Id</c>) and concurrency token (<c>RowVersion</c>),
    ///     translations, and variant mutations.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The handler should compare <c>RowVersion</c> to detect concurrent updates and throw a concurrency
    ///         exception on mismatch, which the controller translates into a user-friendly error.
    ///     </para>
    /// </remarks>
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
