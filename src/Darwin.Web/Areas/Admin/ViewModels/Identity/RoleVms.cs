using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;

namespace Darwin.Web.Areas.Admin.ViewModels.Identity
{
    /// <summary>
    /// Represents a single role item in a list. This avoids leaking Application DTOs into the Web layer.
    /// </summary>
    public sealed class RoleListItemVm
    {
        public Guid Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsSystem { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// View model for the roles listing page. Contains paging metadata and the current search query.
    /// </summary>
    public sealed class RolesListItemVm
    {
        /// <summary>
        /// Current page items projected as lightweight view models suitable for listing.
        /// </summary>
        public List<RoleListItemVm> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// Options for selecting different page sizes.
        /// </summary>
        public IEnumerable<SelectListItem> PageSizeItems { get; set; } = new List<SelectListItem>();
    }

    /// <summary>
    /// View model for the "Create Role" form. Mirrors the minimum fields
    /// required by the Application layer for creation, without leaking DTOs to the Web layer.
    /// </summary>
    public sealed class RoleCreateVm
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    /// <summary>
    /// View model for the "Edit Role" form. Includes the concurrency token.
    /// Fields are intentionally minimal to avoid editing system-only properties.
    /// </summary>
    public sealed class RoleEditVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
