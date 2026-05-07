using System;
using System.Collections.Generic;

namespace Darwin.WebAdmin.ViewModels.CMS
{
    public sealed class MenusIndexVm
    {
        public List<MenuListItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
    }

    public sealed class MenuListItemVm
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ItemsCount { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
    }

    public sealed class MenuEditorVm
    {
        public Guid Id { get; set; }
        public byte[]? RowVersion { get; set; }
        public string Name { get; set; } = "Main";
        public List<MenuItemEditorVm> Items { get; set; } = new();
        public IReadOnlyList<string> Cultures { get; set; } = Array.Empty<string>();
        public bool MultilingualEnabled { get; set; }
    }

    public sealed class MenuItemEditorVm
    {
        public Guid? Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public List<MenuItemTranslationEditorVm> Translations { get; set; } = new();
    }

    public sealed class MenuItemTranslationEditorVm
    {
        public string Culture { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string? Url { get; set; }
    }
}
