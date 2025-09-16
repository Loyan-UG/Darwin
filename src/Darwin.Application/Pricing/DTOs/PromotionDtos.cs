using System;
using System.Collections.Generic;

namespace Darwin.Application.Pricing.DTOs
{
    public sealed class PromotionCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public Darwin.Domain.Enums.PromotionType Type { get; set; }
        public long? AmountMinor { get; set; }
        public decimal? Percent { get; set; }
        public string Currency { get; set; } = "EUR";
        public long? MinSubtotalNetMinor { get; set; }
        public string? ConditionsJson { get; set; }
        public DateTime? StartsAtUtc { get; set; }
        public DateTime? EndsAtUtc { get; set; }
        public int? MaxRedemptions { get; set; }
        public int? PerCustomerLimit { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public sealed class PromotionEditDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public Darwin.Domain.Enums.PromotionType Type { get; set; }
        public long? AmountMinor { get; set; }
        public decimal? Percent { get; set; }
        public string Currency { get; set; } = "EUR";
        public long? MinSubtotalNetMinor { get; set; }
        public string? ConditionsJson { get; set; }
        public DateTime? StartsAtUtc { get; set; }
        public DateTime? EndsAtUtc { get; set; }
        public int? MaxRedemptions { get; set; }
        public int? PerCustomerLimit { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>Result of validating a coupon against a basket subtotal.</summary>
    public sealed class ValidateCouponResultDto
    {
        public bool IsValid { get; set; }
        public string? Message { get; set; }
        public long DiscountMinor { get; set; }
        public string Currency { get; set; } = "EUR";
        public Guid? PromotionId { get; set; }
    }

    public sealed class ValidateCouponInputDto
    {
        public string Code { get; set; } = string.Empty;
        public long SubtotalNetMinor { get; set; }
        public string Currency { get; set; } = "EUR";
        public Guid? UserId { get; set; }
        // Optional future: lines for product/category conditions
        public List<Guid>? ProductIds { get; set; }
        public List<Guid>? CategoryIds { get; set; }
    }

    public sealed class PromotionListItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public bool IsActive { get; set; }
        public DateTime? StartsAtUtc { get; set; }
        public DateTime? EndsAtUtc { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
    }
}
