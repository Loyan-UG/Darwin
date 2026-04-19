using System;
using System.Collections.Generic;

using Darwin.Domain.Common;

namespace Darwin.Domain.Entities.CMS
{
    /// <summary>
    ///     Represents a navigational structure rendered in the UI (e.g., the main header menu).
    /// </summary>
    public sealed class Menu : BaseEntity
    {
        /// <summary>Internal menu name (e.g., Main/Footer).</summary>
        public string Name { get; set; } = "Main";

        /// <summary>Default culture for menu payload.</summary>
        public string Culture { get; set; } = "en-US";

        /// <summary>Menu items.</summary>
        public List<MenuItem> Items { get; set; } = new();
    }

    /// <summary>
    ///     A navigational item that can be nested and ordered among siblings.
    /// </summary>
    public sealed class MenuItem : BaseEntity
    {
        public Guid MenuId { get; set; }
        public Guid? ParentId { get; set; }

        /// <summary>Destination URL or route.</summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>Sort order among siblings.</summary>
        public int SortOrder { get; set; }

        /// <summary>Legacy visible flag.</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Optional default title.</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Hierarchy children convenience collection.</summary>
        public List<MenuItem> Children { get; set; } = new();

        public List<MenuItemTranslation> Translations { get; set; } = new();

        // New-model alias.
        public int Order
        {
            get => SortOrder;
            set => SortOrder = value;
        }
    }

    /// <summary>
    ///     Culture-specific label for a menu item.
    /// </summary>
    public sealed class MenuItemTranslation : BaseEntity
    {
        public Guid MenuItemId { get; set; }
    public string Culture { get; set; } = DomainDefaults.DefaultCulture;
        public string Label { get; set; } = string.Empty;

        // New-model alias.
        public string Title
        {
            get => Label;
            set => Label = value;
        }
    }
}
