using System;
using System.Collections.Generic;
using Darwin.Domain.Enums;

namespace Darwin.Web.Areas.Admin.ViewModels.CMS
{
    public sealed class PageTranslationVm
    {
        public string Culture { get; set; } = "de-DE";
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string ContentHtml { get; set; } = string.Empty;
    }

    public sealed class PageCreateVm
    {
        public PageStatus Status { get; set; } = PageStatus.Draft;
        public DateTime? PublishStartUtc { get; set; }
        public DateTime? PublishEndUtc { get; set; }
        public List<PageTranslationVm> Translations { get; set; } = new() { new PageTranslationVm() };
    }

    public sealed class PageEditVm
    {
        public Guid Id { get; set; }
        public PageStatus Status { get; set; } = PageStatus.Draft;
        public DateTime? PublishStartUtc { get; set; }
        public DateTime? PublishEndUtc { get; set; }
        public List<PageTranslationVm> Translations { get; set; } = new() { new PageTranslationVm() };
        public byte[]? RowVersion { get; set; }
    }
}
