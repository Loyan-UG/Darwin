using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Darwin.Web.Areas.Admin.ViewModels.Catalog
{
    /// <summary>List item row for Add-on Groups grid.</summary>
    public sealed class AddOnGroupListItemVm
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Currency { get; set; } = "EUR";
        public bool IsGlobal { get; set; }
        public bool IsActive { get; set; }
        public int OptionsCount { get; set; }
        public DateTime ModifiedAtUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>List VM with paging & search for Index.</summary>
    public sealed class AddOnGroupsListVm
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public string Query { get; set; } = string.Empty;

        public List<AddOnGroupListItemVm> Items { get; set; } = new();
    }

    /// <summary>Option value inside an option.</summary>
    public sealed class AddOnOptionValueVm
    {
        public Guid Id { get; set; }
        public Guid AddOnOptionId { get; set; }
        public string Label { get; set; } = string.Empty;
        public long PriceDeltaMinor { get; set; }
        public string? Hint { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>Option with nested values.</summary>
    public sealed class AddOnOptionVm
    {
        public Guid Id { get; set; }
        public Guid AddOnGroupId { get; set; }
        public string Label { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public List<AddOnOptionValueVm> Values { get; set; } = new();
    }

    /// <summary>Create form VM (mirrors AddOnGroupCreateDto).</summary>
    public sealed class AddOnGroupCreateVm
    {
        public string Name { get; set; } = string.Empty;
        public string Currency { get; set; } = "EUR";
        public bool IsGlobal { get; set; }
        public int SelectionMode { get; set; } = 0; // matches domain enum backing
        public int MinSelections { get; set; } = 0;
        public int MaxSelections { get; set; } = 1;
        public bool IsActive { get; set; } = true;
        public List<AddOnOptionVm> Options { get; set; } = new();
    }

    /// <summary>Edit form VM (mirrors AddOnGroupEditDto).</summary>
    public sealed class AddOnGroupEditVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public string Name { get; set; } = string.Empty;
        public string Currency { get; set; } = "EUR";
        public bool IsGlobal { get; set; }
        public int SelectionMode { get; set; } = 0;
        public int MinSelections { get; set; } = 0;
        public int MaxSelections { get; set; } = 1;
        public bool IsActive { get; set; } = true;
        public List<AddOnOptionVm> Options { get; set; } = new();
    }

    /// <summary>Attach group to products (picker + selected ids).</summary>
    public sealed class AddOnGroupAttachToProductsVm
    {
        public Guid GroupId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public string GroupName { get; set; } = string.Empty;

        /// <summary>
        /// Selected products to attach (final set). The UI typically binds a multi-select or checklist here.
        /// </summary>
        public List<Guid> SelectedProductIds { get; set; } = new();

        // Simple paged product picker
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// Optional: a paged/filtered list to help choosing products (same shape as other list VMs).
        /// </summary>
        public List<ProductPickItemVm> Products { get; set; } = new();
    }

    public sealed class ProductPickItemVm
    {
        public Guid Id { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool Selected { get; set; }
    }
}
