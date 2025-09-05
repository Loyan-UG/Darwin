using System;
using System.Collections.Generic;
using Darwin.Domain.Common;


namespace Darwin.Domain.Entities.CMS
{
    /// <summary>
    /// Menu represents a navigational structure. For phase 1, only the main header menu is used.
    /// </summary>
    public sealed class Menu : BaseEntity
    {
        /// <summary>Internal name for the menu (not shown to end users).</summary>
        public string Name { get; set; } = "Main";
        /// <summary>Menu items ordered by SortOrder and hierarchy.</summary>
        public List<MenuItem> Items { get; set; } = new();
    }


    /// <summary>
    /// Menu item supports multi-level navigation via ParentId and SortOrder.
    /// </summary>
    public sealed class MenuItem : BaseEntity
    {
        /// <summary>Foreign key to the owning menu.</summary>
        public Guid MenuId { get; set; }
        /// <summary>Optional parent item for nested menus (null for root items).</summary>
        public Guid? ParentId { get; set; }
        /// <summary>Display label for the item (non-localized for simplicity; could be translated if needed).</summary>
        public string Label { get; set; } = string.Empty;
        /// <summary>Destination URL or application route for this item.</summary>
        public string Url { get; set; } = string.Empty;
        /// <summary>Sorting order among siblings (lower first).</summary>
        public int SortOrder { get; set; }
        /// <summary>When true, hides the item from rendering without deleting it.</summary>
        public bool IsActive { get; set; } = true;
    }
}