using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Darwin.Application.Catalog.DTOs
{
    /// <summary>
    /// Create payload for an AddOnGroup with nested options/values.
    /// </summary>
    public sealed class AddOnGroupCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string Currency { get; set; } = "EUR";
        public bool IsGlobal { get; set; } = false;
        public AddOnSelectionMode SelectionMode { get; set; } = AddOnSelectionMode.Single;
        public int MinSelections { get; set; } = 0;
        public int? MaxSelections { get; set; }
        public bool IsActive { get; set; } = true;
        public List<AddOnOptionDto> Options { get; set; } = new();
    }

    /// <summary>
    /// Edit payload for an AddOnGroup (whole aggregate upsert; simplest approach for phase 1).
    /// </summary>
    public sealed class AddOnGroupEditDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public string Name { get; set; } = string.Empty;
        public string Currency { get; set; } = "EUR";
        public bool IsGlobal { get; set; } = false;
        public AddOnSelectionMode SelectionMode { get; set; } = AddOnSelectionMode.Single;
        public int MinSelections { get; set; } = 0;
        public int? MaxSelections { get; set; }
        public bool IsActive { get; set; } = true;
        public List<AddOnOptionDto> Options { get; set; } = new();
    }

    /// <summary>
    /// Option DTO with nested values.
    /// </summary>
    public sealed class AddOnOptionDto
    {
        public string Label { get; set; } = string.Empty;
        public int SortOrder { get; set; } = 0;
        public List<AddOnOptionValueDto> Values { get; set; } = new();
    }

    /// <summary>
    /// Value DTO with price delta (minor units).
    /// </summary>
    public sealed class AddOnOptionValueDto
    {
        public string Label { get; set; } = string.Empty;
        public long PriceDeltaMinor { get; set; } = 0;
        public string? Hint { get; set; }
        public int SortOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Lightweight list item for Admin grid.
    /// </summary>
    public sealed class AddOnGroupListItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Currency { get; set; } = "EUR";
        public bool IsGlobal { get; set; }
        public bool IsActive { get; set; }
        public int OptionsCount { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }

        /// <summary>
        /// Concurrency token from <see cref="Darwin.Domain.Common.BaseEntity.RowVersion"/>.
        /// Required by the Web layer to safely perform inline updates/deletes.
        /// </summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Lookup used by forms to attach a group to products/categories/brands.
    /// </summary>
    public sealed class AddOnGroupLookupItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }


    /// <summary>
    /// Request to attach an add-on group to a set of products, replacing any existing assignments.
    /// The operation is performed with optimistic concurrency using the group's RowVersion.
    /// </summary>
    public sealed class AddOnGroupAttachToProductsDto
    {
        /// <summary>Target add-on group identifier.</summary>
        public Guid AddOnGroupId { get; set; }

        /// <summary>Selected product identifiers to be attached to the group.</summary>
        public Guid[] ProductIds { get; set; } = Array.Empty<Guid>();

        /// <summary>Concurrency token of the add-on group to protect from lost updates.</summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }




    /// <summary>
    /// Represents a resolved add-on group that applies to the given product/variant context.
    /// It carries the selection constraints and a flattened list of options and values.
    /// </summary>
    public sealed class ApplicableAddOnGroupDto
    {
        /// <summary>Group identifier.</summary>
        public Guid Id { get; set; }

        /// <summary>Human-readable name of the group.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>ISO 4217 currency code (e.g., "EUR").</summary>
        public string Currency { get; set; } = "EUR";

        /// <summary>Selection mode (single or multiple).</summary>
        public AddOnSelectionMode SelectionMode { get; set; }

        /// <summary>Minimum required selections. Defaults to 0.</summary>
        public int MinSelections { get; set; }

        /// <summary>Maximum allowed selections. Null when unbounded.</summary>
        public int? MaxSelections { get; set; }

        /// <summary>Only active groups are returned.</summary>
        public bool IsActive { get; set; }

        /// <summary>Resolved options belonging to the group.</summary>
        public List<ApplicableAddOnOptionDto> Options { get; set; } = new();
    }

    /// <summary>
    /// A resolved add-on option with display label and its ordered values.
    /// </summary>
    public sealed class ApplicableAddOnOptionDto
    {
        /// <summary>Option identifier.</summary>
        public Guid Id { get; set; }

        /// <summary>Display label shown to end-users (e.g., "Color").</summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>Option ordering inside its group.</summary>
        public int SortOrder { get; set; }

        /// <summary>Active values ordered by SortOrder.</summary>
        public List<ApplicableAddOnOptionValueDto> Values { get; set; } = new();
    }

    /// <summary>
    /// A resolved selectable value with price delta (minor units, NET) and optional hint.
    /// Only active values are returned.
    /// </summary>
    public sealed class ApplicableAddOnOptionValueDto
    {
        /// <summary>Value identifier.</summary>
        public Guid Id { get; set; }

        /// <summary>Human-readable label (e.g., "Black").</summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>Price delta (minor units, NET). Can be negative for discounts.</summary>
        public long PriceDeltaMinor { get; set; }

        /// <summary>Optional additional hint (e.g., color hex or a short note).</summary>
        public string? Hint { get; set; }

        /// <summary>Value ordering under its option.</summary>
        public int SortOrder { get; set; }
    }


    /// <summary>
    /// Replace semantics: sets the exact list of variants to which an add-on group is attached.
    /// </summary>
    public sealed class AddOnGroupAttachToVariantsDto
    {
        /// <summary>Target add-on group identifier (must exist and be non-deleted).</summary>
        public Guid AddOnGroupId { get; set; }

        /// <summary>1..N variant identifiers to attach to.</summary>
        public Guid[] VariantIds { get; set; } = Array.Empty<Guid>();

        /// <summary>Concurrency token of the group to guard against races.</summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
