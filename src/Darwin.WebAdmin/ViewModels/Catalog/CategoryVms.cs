using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Darwin.WebAdmin.ViewModels.Catalog
{
    public sealed class CategoriesIndexVm
    {
        public IReadOnlyList<Darwin.Application.Catalog.DTOs.CategoryListItemDto> Items { get; set; } = new List<Darwin.Application.Catalog.DTOs.CategoryListItemDto>();
        public CategoryOpsSummaryVm Summary { get; set; } = new();
        public IReadOnlyList<OperationalPlaybookVm> Playbooks { get; set; } = new List<OperationalPlaybookVm>();
        public string Query { get; set; } = string.Empty;
        public string Filter { get; set; } = string.Empty;
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
    }

    public sealed class CategoryOpsSummaryVm
    {
        public int TotalCount { get; set; }
        public int InactiveCount { get; set; }
        public int UnpublishedCount { get; set; }
        public int RootCount { get; set; }
        public int ChildCount { get; set; }
    }

    public sealed class CategoryTranslationVm
    {
        public string Culture { get; set; } = string.Empty;
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
        public List<SelectListItem> ParentCategoryOptions { get; set; } = new();
        public IReadOnlyList<string> Cultures { get; set; } = Array.Empty<string>();
    }

    public sealed class CategoryEditVm
    {
        public Guid Id { get; set; }
        public Guid? ParentId { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; } = 0;
        public byte[]? RowVersion { get; set; }
        public List<CategoryTranslationVm> Translations { get; set; } = new();
        public List<SelectListItem> ParentCategoryOptions { get; set; } = new();
    }
}
