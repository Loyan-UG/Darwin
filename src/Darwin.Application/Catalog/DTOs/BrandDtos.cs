using System;
using System.Collections.Generic;

namespace Darwin.Application.Catalog.DTOs
{
    /// <summary>
    /// DTO for creating a brand (culture-invariant fields + per-culture translations).
    /// </summary>
    public sealed class BrandCreateDto
    {
        /// <summary>Optional unique slug for brand landing. Culture-invariant.</summary>
        public string? Slug { get; set; }
        /// <summary>Optional logo media id.</summary>
        public Guid? LogoMediaId { get; set; }
        /// <summary>Per-culture fields.</summary>
        public List<BrandTranslationDto> Translations { get; set; } = new();
    }

    /// <summary>
    /// DTO for editing an existing brand. Includes concurrency token.
    /// </summary>
    public sealed class BrandEditDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public string? Slug { get; set; }
        public Guid? LogoMediaId { get; set; }
        public List<BrandTranslationDto> Translations { get; set; } = new();
    }

    /// <summary>
    /// DTO carrying localized brand fields.
    /// </summary>
    public sealed class BrandTranslationDto
    {
        public string Culture { get; set; } = "de-DE";
        public string Name { get; set; } = string.Empty;
        public string? DescriptionHtml { get; set; }
    }

    /// <summary>
    /// Lightweight row for Admin brand grid.
    /// </summary>
    public sealed class BrandListItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "?";
        public string? Slug { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }

        /// <summary>
        /// Concurrency token required by delete operations initiated from the list view.
        /// </summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Data transfer object used when performing a soft delete of a Brand.
    /// The RowVersion enables optimistic concurrency control from the Admin grid.
    /// </summary>
    public sealed class BrandDeleteDto
    {
        /// <summary>Identifier of the brand to delete.</summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Concurrency token from the current row. Must match the database state
        /// to avoid unintentionally deleting a concurrently modified record.
        /// </summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
