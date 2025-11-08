using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Darwin.Domain.Enums;

namespace Darwin.Web.Areas.Admin.ViewModels.Catalog
{
    // ---------- List (Index) ----------

    /// <summary>
    /// Paged list VM for Add-on Groups in Admin.
    /// Mirrors paging contract used across Admin (Page/PageSize/Total/Query).
    /// </summary>
    public sealed class AddOnGroupsListVm
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
        public string Query { get; set; } = string.Empty;

        public List<AddOnGroupListItemVm> Items { get; set; } = new();
    }

    /// <summary>
    /// Row item shown in Add-on Groups grid.
    /// </summary>
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


    // ---------- Create/Edit (aggregate upsert) ----------

    /// <summary>
    /// Nested value inside an option with price delta in minor units.
    /// </summary>
    public sealed class AddOnOptionValueVm
    {
        [Required, MaxLength(200)]
        public string Label { get; set; } = string.Empty;

        /// <summary>Price delta in minor units (NET). Can be negative.</summary>
        public long PriceDeltaMinor { get; set; } = 0;

        [MaxLength(200)]
        public string? Hint { get; set; }

        public int SortOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Option with ordered values.
    /// </summary>
    public sealed class AddOnOptionVm
    {
        [Required, MaxLength(200)]
        public string Label { get; set; } = string.Empty;

        public int SortOrder { get; set; } = 0;

        public List<AddOnOptionValueVm> Values { get; set; } = new();
    }

    /// <summary>
    /// Form model for creating an Add-on Group.
    /// </summary>
    public sealed class AddOnGroupCreateVm
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(3)]
        public string Currency { get; set; } = "EUR";

        public bool IsGlobal { get; set; } = false;

        public AddOnSelectionMode SelectionMode { get; set; } = AddOnSelectionMode.Single;

        [Range(0, int.MaxValue)]
        public int MinSelections { get; set; } = 0;

        public int? MaxSelections { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>Nested options with their values.</summary>
        public List<AddOnOptionVm> Options { get; set; } = new();
    }

    /// <summary>
    /// Form model for editing an existing Add-on Group with optimistic concurrency.
    /// </summary>
    public sealed class AddOnGroupEditVm
    {
        public Guid Id { get; set; }

        /// <summary>Optimistic concurrency token.</summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(3)]
        public string Currency { get; set; } = "EUR";

        public bool IsGlobal { get; set; } = false;

        public AddOnSelectionMode SelectionMode { get; set; } = AddOnSelectionMode.Single;

        [Range(0, int.MaxValue)]
        public int MinSelections { get; set; } = 0;

        public int? MaxSelections { get; set; }

        public bool IsActive { get; set; } = true;

        public List<AddOnOptionVm> Options { get; set; } = new();
    }


    // ---------- Attach flows (Products / Categories / Brands / Variants) ----------

    /// <summary>
    /// Base attach VM shared fields.
    /// </summary>
    public abstract class AddOnGroupAttachBaseVm
    {
        public Guid AddOnGroupId { get; set; }
        public string AddOnGroupName { get; set; } = string.Empty;
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
        public string Query { get; set; } = string.Empty;
    }

    public sealed class SelectableItemVm
    {
        public Guid Id { get; set; }
        public string Display { get; set; } = string.Empty;
        public bool Selected { get; set; }
    }

    public sealed class AddOnGroupAttachToProductsVm : AddOnGroupAttachBaseVm
    {
        public List<SelectableItemVm> Items { get; set; } = new();
        public List<Guid> SelectedProductIds { get; set; } = new();
    }

    public sealed class AddOnGroupAttachToCategoriesVm : AddOnGroupAttachBaseVm
    {
        public List<SelectableItemVm> Items { get; set; } = new();
        public List<Guid> SelectedCategoryIds { get; set; } = new();
    }

    public sealed class AddOnGroupAttachToBrandsVm : AddOnGroupAttachBaseVm
    {
        public List<SelectableItemVm> Items { get; set; } = new();
        public List<Guid> SelectedBrandIds { get; set; } = new();
    }

    /// <summary>
    /// For variant-level override attachment. UI can provide list from product/variant pages.
    /// </summary>
    public sealed class AddOnGroupAttachToVariantsVm : AddOnGroupAttachBaseVm
    {
        public List<Guid> SelectedVariantIds { get; set; } = new();
        public List<SelectableItemVm> Items { get; set; } = new(); // Optional for the future


    }
}
