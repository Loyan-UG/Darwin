using System;
using System.Collections.Generic;
using Darwin.Domain.Enums;

namespace Darwin.Application.CMS.DTOs
{
    public sealed class PageTranslationDto
    {
        public string Culture { get; set; } = "de-DE";
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        /// <summary>Raw HTML from editor; will be sanitized in handlers.</summary>
        public string ContentHtml { get; set; } = string.Empty;
    }

    /// <summary>
    ///     DTO representing the required information to create a CMS page, including
    ///     status, optional publication window (UTC), and per-culture translations with content.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <c>ContentHtml</c> should be sanitized by the handler prior to persistence.
    ///         Slug uniqueness per culture is validated against the database to provide helpful errors.
    ///     </para>
    /// </remarks>
    public sealed class PageCreateDto
    {
        public PageStatus Status { get; set; } = PageStatus.Draft;
        public DateTime? PublishStartUtc { get; set; }
        public DateTime? PublishEndUtc { get; set; }
        public List<PageTranslationDto> Translations { get; set; } = new();
    }

    /// <summary>
    ///     DTO for editing an existing CMS page. Includes identity, concurrency token, status,
    ///     publication window, and per-culture translation updates.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The handler should apply upserts for translations (add/update/remove) depending on what the
    ///         Admin UI submitted, while keeping slug uniqueness constraints intact.
    ///     </para>
    /// </remarks>
    public sealed class PageEditDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public PageStatus Status { get; set; } = PageStatus.Draft;
        public DateTime? PublishStartUtc { get; set; }
        public DateTime? PublishEndUtc { get; set; }
        public List<PageTranslationDto> Translations { get; set; } = new();
    }

    public sealed class PageListItemDto
    {
        public Guid Id { get; set; }
        public string? Title { get; set; }
        public PageStatus Status { get; set; }
        public DateTime? PublishStartUtc { get; set; }
        public DateTime? PublishEndUtc { get; set; }
        public DateTime ModifiedAtUtc { get; set; }

        /// <summary>
        /// Concurrency token for optimistic concurrency control on inline operations
        /// </summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
