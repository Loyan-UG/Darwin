using System;
using System.Collections.Generic;
using Darwin.Domain.Enums;

namespace Darwin.Application.Loyalty.DTOs
{
    public enum LoyaltyProgramQueueFilter
    {
        All = 0,
        Active = 1,
        Inactive = 2,
        PerCurrencyUnit = 3,
        MissingRules = 4
    }

    public sealed class LoyaltyProgramOpsSummaryDto
    {
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int InactiveCount { get; set; }
        public int PerCurrencyUnitCount { get; set; }
        public int MissingRulesCount { get; set; }
    }

    /// <summary>
    /// DTO used to create a loyalty program for a business.
    /// </summary>
    public sealed class LoyaltyProgramCreateDto
    {
        public Guid BusinessId { get; set; }
        public string Name { get; set; } = "Default Loyalty Program";
        public LoyaltyAccrualMode AccrualMode { get; set; } = LoyaltyAccrualMode.PerVisit;
        public decimal? PointsPerCurrencyUnit { get; set; }
        public bool IsActive { get; set; } = true;
        public string? RulesJson { get; set; }
    }

    /// <summary>
    /// DTO used to edit an existing loyalty program.
    /// </summary>
    public sealed class LoyaltyProgramEditDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public string Name { get; set; } = "Default Loyalty Program";
        public LoyaltyAccrualMode AccrualMode { get; set; } = LoyaltyAccrualMode.PerVisit;
        public decimal? PointsPerCurrencyUnit { get; set; }
        public bool IsActive { get; set; } = true;
        public string? RulesJson { get; set; }
        public byte[]? RowVersion { get; set; }
    }

    /// <summary>
    /// Lightweight list item for admin/business grids.
    /// </summary>
    public sealed class LoyaltyProgramListItemDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public string Name { get; set; } = default!;
        public LoyaltyAccrualMode AccrualMode { get; set; }
        public bool IsActive { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// DTO used for soft delete in UI grids.
    /// </summary>
    public sealed class LoyaltyProgramDeleteDto
    {
        public Guid Id { get; set; }
        public byte[]? RowVersion { get; set; }
    }
}
