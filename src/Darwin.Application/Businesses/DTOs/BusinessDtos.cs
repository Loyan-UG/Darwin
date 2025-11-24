using System;
using System.Collections.Generic;
using Darwin.Domain.Enums;

namespace Darwin.Application.Businesses.DTOs
{
    /// <summary>
    /// DTO for creating a new business (merchant tenant).
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
    /// DTO for editing an existing business.
    /// Includes concurrency token (RowVersion).
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
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// DTO for soft deleting a business.
    /// </summary>
    public sealed class BusinessDeleteDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Lightweight list row for paged business grids.
    /// </summary>
    public sealed class BusinessListItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string? LegalName { get; set; }
        public BusinessCategoryKind Category { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedAtUtc { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
    
}
