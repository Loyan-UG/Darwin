using System;
using System.Collections.Generic;

namespace Darwin.Application.CMS.DTOs
{
    /// <summary>
    /// DTO for creating a menu with (optionally hierarchical) items.
    /// </summary>
    public sealed class MenuCreateDto
    {
        public string Name { get; set; } = "Main";
        public List<MenuItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// DTO for editing an existing menu. Replaces items with the provided set (phase 1).
    /// </summary>
    public sealed class MenuEditDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public string Name { get; set; } = "Main";
        public List<MenuItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// Menu item DTO with per-culture translations for label.
    /// </summary>
    public sealed class MenuItemDto
    {
        public Guid? Id { get; set; } // optional for edit scenarios; not used in replace strategy
        public Guid? ParentId { get; set; } // for phase 1, may be null to keep items flat
        public string Url { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public List<MenuItemTranslationDto> Translations { get; set; } = new();
    }

    /// <summary>
    /// Localized label for a menu item.
    /// </summary>
    public sealed class MenuItemTranslationDto
    {
        public string Culture { get; set; } = "de-DE";
        public string Label { get; set; } = string.Empty;
    }

    /// <summary>
    /// Lightweight row for Admin menu list/grid.
    /// </summary>
    public sealed class MenuListItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "Main";
        public int ItemsCount { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
    }
}
