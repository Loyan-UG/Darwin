// File: src/Darwin.Application/CMS/DTOs/PageDtos.cs
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

    public sealed class PageCreateDto
    {
        public PageStatus Status { get; set; } = PageStatus.Draft;
        public DateTime? PublishStartUtc { get; set; }
        public DateTime? PublishEndUtc { get; set; }
        public List<PageTranslationDto> Translations { get; set; } = new();
    }

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
    }
}
