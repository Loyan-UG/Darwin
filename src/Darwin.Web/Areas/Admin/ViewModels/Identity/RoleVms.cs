using Darwin.Application.Identity.DTOs;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Darwin.Web.Areas.Admin.ViewModels.Identity
{
    /// <summary>
    /// View model for the roles listing page, including paging metadata and the current search query.
    /// </summary>
    public sealed class RolesListItemVm
    {
        /// <summary>
        /// Current page items projected as lightweight DTOs suitable for listing.
        /// </summary>
        public List<RoleListItemDto> Items { get; set; } = new();

        /// <summary>
        /// 1-based page number.
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Items per page.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total number of items matching the current filter.
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// Current search query (optional).
        /// </summary>
        public string Query { get; set; } = string.Empty;

        // NEW: for the page size dropdown
        public IEnumerable<SelectListItem> PageSizeItems { get; set; } = new List<SelectListItem>();
    }

    /// <summary>
    /// View model for the "Create Role" form. Mirrors the minimum fields
    /// required by the Application layer for creation, without leaking DTOs to the Web layer.
    /// </summary>
    public sealed class RoleCreateVm
    {
        /// <summary>Stable key used by the system to reference this role (e.g., "Members").</summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>Human-friendly display name shown in the Admin UI.</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Optional description to clarify the role's purpose.</summary>
        public string? Description { get; set; }
    }

    /// <summary>
    /// View model for the "Edit Role" form. Includes the concurrency token.
    /// Fields are intentionally minimal to avoid editing system-only properties.
    /// </summary>
    public sealed class RoleEditVm
    {
        /// <summary>Primary key of the role being edited.</summary>
        public Guid Id { get; set; }

        /// <summary>Concurrency token for optimistic concurrency control.</summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        /// <summary>Editable display name shown in the Admin UI.</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Optional description to clarify the role's purpose.</summary>
        public string? Description { get; set; }
    }
}
