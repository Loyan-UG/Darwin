using System;
using Darwin.Domain.Enums;

namespace Darwin.Application.Businesses.DTOs
{
    /// <summary>
    /// Lightweight list row for admin grids and lookups.
    /// </summary>
    public sealed class BusinessListItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string? LegalName { get; set; }
        public BusinessCategoryKind Category { get; set; }
        public bool IsActive { get; set; }
        public string DefaultCurrency { get; set; } = default!;
        public string DefaultCulture { get; set; } = default!;
        public DateTime? ModifiedAtUtc { get; set; }
        public byte[]? RowVersion { get; set; }
    }

    /// <summary>
    /// DTO used to create a new business.
    /// </summary>
    public sealed class BusinessCreateDto
    {
        public string Name { get; set; } = default!;
        public string? LegalName { get; set; }
        public string? TaxId { get; set; }
        public string? ShortDescription { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhoneE164 { get; set; }
        public BusinessCategoryKind Category { get; set; } = BusinessCategoryKind.Unknown;
        public string DefaultCurrency { get; set; } = "EUR";
        public string DefaultCulture { get; set; } = "de-DE";
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// DTO used to edit an existing business.
    /// </summary>
    public sealed class BusinessEditDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string? LegalName { get; set; }
        public string? TaxId { get; set; }
        public string? ShortDescription { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhoneE164 { get; set; }
        public BusinessCategoryKind Category { get; set; } = BusinessCategoryKind.Unknown;
        public string DefaultCurrency { get; set; } = "EUR";
        public string DefaultCulture { get; set; } = "de-DE";
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Optimistic concurrency token from the UI grid or edit form.
        /// Required for safe updates.
        /// </summary>
        public byte[] RowVersion { get; set; } = default!;
    }

    /// <summary>
    /// Delete request DTO for soft-deletable entities.
    /// </summary>
    public sealed class BusinessDeleteDto
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Optional concurrency token from the UI grid.
        /// When provided, must match current RowVersion.
        /// </summary>
        public byte[]? RowVersion { get; set; }
    }
}
