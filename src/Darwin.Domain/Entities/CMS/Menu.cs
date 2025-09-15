using System;
using System.Collections.Generic;
using Darwin.Domain.Common;

namespace Darwin.Domain.Entities.CMS
{
    /// <summary>
    ///     Represents a navigational structure rendered in the UI (e.g., the main header menu).
    ///     The menu itself has an internal, non-localized <c>Name</c> for administrative reference.
    ///     User-facing text is carried by <see cref="MenuItem"/> translations.
    ///     Audit, soft-delete, and optimistic concurrency fields are inherited from <see cref="BaseEntity"/>.
    /// </summary>
    public sealed class Menu : BaseEntity
    {
        /// <summary>
        ///     Internal identifier for the menu (not shown to end users).
        ///     Example values: "Main", "Footer", "Account".
        /// </summary>
        public string Name { get; set; } = "Main";

        /// <summary>
        ///     The set of items that belong to this menu. Items can be hierarchical via <see cref="MenuItem.ParentId"/>.
        ///     Render order is determined by <see cref="MenuItem.SortOrder"/>.
        /// </summary>
        public List<MenuItem> Items { get; set; } = new();
    }

    /// <summary>
    ///     A navigational item that can be nested (via <see cref="ParentId"/>) and ordered among siblings (via <see cref="SortOrder"/>).
    ///     The user-facing label is localized in <see cref="MenuItemTranslation"/>.
    /// </summary>
    public sealed class MenuItem : BaseEntity
    {
        /// <summary>Foreign key to the owning menu.</summary>
        public Guid MenuId { get; set; }

        /// <summary>
        ///     Optional parent item for hierarchical menus; <c>null</c> indicates a root item.
        /// </summary>
        public Guid? ParentId { get; set; }

        /// <summary>
        ///     Destination URL or application route for this item. This is culture-invariant; if you later
        ///     need per-culture URLs, model that explicitly in translations.
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>Determines order among siblings; lower values render first.</summary>
        public int SortOrder { get; set; }

        /// <summary>When false, the item is hidden from rendering without being deleted.</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        ///     Per-culture translations carrying user-facing labels.
        /// </summary>
        public List<MenuItemTranslation> Translations { get; set; } = new();
    }

    /// <summary>
    ///     Culture-specific label for a <see cref="MenuItem"/>. Each (MenuItemId, Culture) pair must be unique.
    /// </summary>
    public sealed class MenuItemTranslation : BaseEntity
    {
        /// <summary>Owning menu item identifier.</summary>
        public Guid MenuItemId { get; set; }

        /// <summary>IETF BCP 47 culture code (e.g., "de-DE", "en-US").</summary>
        public string Culture { get; set; } = "de-DE";

        /// <summary>
        ///     User-facing label displayed in navigation for the specified culture.
        /// </summary>
        public string Label { get; set; } = string.Empty;
    }
}
