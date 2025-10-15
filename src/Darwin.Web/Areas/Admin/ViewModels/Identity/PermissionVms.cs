using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Darwin.Web.Areas.Admin.ViewModels.Identity
{
    /// <summary>
    /// Grid filter & paging model for listing permissions in Admin.
    /// </summary>
    public sealed class PermissionIndexVm
    {
        public string? Q { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public List<PermissionRowVm> Items { get; set; } = new();
        public int Total { get; set; }
    }

    /// <summary>
    /// Lightweight row for the permissions grid.
    /// </summary>
    public sealed class PermissionRowVm
    {
        public Guid Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool IsSystem { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
        public byte[]? RowVersion { get; set; }
    }

    /// <summary>
    /// Create form model for a permission.
    /// </summary>
    public sealed class PermissionCreateVm
    {
        [Required, MinLength(3)]
        public string Key { get; set; } = string.Empty;

        [Required]
        public string DisplayName { get; set; } = string.Empty;

        public string? Description { get; set; }
    }

    /// <summary>
    /// Edit form model for a permission with concurrency token.
    /// </summary>
    public sealed class PermissionEditVm
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        [Required]
        public string DisplayName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string Key { get; set; } = string.Empty;
        public bool IsSystem { get; set; }
    }
}
