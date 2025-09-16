using System;

namespace Darwin.Application.Pricing.DTOs
{
    public sealed class TaxCategoryCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal VatRate { get; set; } // e.g., 0.19m
        public DateTime? EffectiveFromUtc { get; set; }
        public string? Notes { get; set; }
    }

    public sealed class TaxCategoryEditDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public string Name { get; set; } = string.Empty;
        public decimal VatRate { get; set; }
        public DateTime? EffectiveFromUtc { get; set; }
        public string? Notes { get; set; }
    }
}
