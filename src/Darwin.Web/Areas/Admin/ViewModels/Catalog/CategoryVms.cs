using System;
using System.Collections.Generic;

namespace Darwin.Web.Areas.Admin.ViewModels.Catalog
{
    public sealed class CategoryTranslationVm
    {
        public string Culture { get; set; } = "de-DE";
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
    }

    public sealed class CategoryCreateVm
    {
        public Guid? ParentId { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; } = 0;
        public List<CategoryTranslationVm> Translations { get; set; } = new();
    }

    public sealed class CategoryEditVm
    {
        public Guid Id { get; set; }
        public Guid? ParentId { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; } = 0;
        public byte[]? RowVersion { get; set; }
        public List<CategoryTranslationVm> Translations { get; set; } = new();
    }
}
