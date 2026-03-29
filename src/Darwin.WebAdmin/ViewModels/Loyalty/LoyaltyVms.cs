using System;
using System.ComponentModel.DataAnnotations;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Application.Loyalty.Campaigns;
using Darwin.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Darwin.WebAdmin.ViewModels.Loyalty
{
    public sealed class LoyaltyProgramsListVm
    {
        public Guid? BusinessId { get; set; }
        public LoyaltyProgramQueueFilter Filter { get; set; } = LoyaltyProgramQueueFilter.All;
        public List<SelectListItem> FilterItems { get; set; } = new();
        public LoyaltyProgramOpsSummaryVm Summary { get; set; } = new();
        public List<LoyaltyOpsPlaybookVm> Playbooks { get; set; } = new();
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<LoyaltyProgramListItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
    }

    public sealed class LoyaltyProgramListItemVm
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public string Name { get; set; } = string.Empty;
        public LoyaltyAccrualMode AccrualMode { get; set; }
        public bool IsActive { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class LoyaltyProgramOpsSummaryVm
    {
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int InactiveCount { get; set; }
        public int PerCurrencyUnitCount { get; set; }
        public int MissingRulesCount { get; set; }
    }

    public sealed class LoyaltyProgramEditVm
    {
        public Guid Id { get; set; }

        [Required]
        public Guid BusinessId { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        public LoyaltyAccrualMode AccrualMode { get; set; } = LoyaltyAccrualMode.PerVisit;

        [Range(0, 100000)]
        public decimal? PointsPerCurrencyUnit { get; set; }

        public bool IsActive { get; set; } = true;
        public string? RulesJson { get; set; }
        public byte[]? RowVersion { get; set; }
        public List<SelectListItem> BusinessOptions { get; set; } = new();
    }

    public sealed class LoyaltyRewardTiersListVm
    {
        public Guid LoyaltyProgramId { get; set; }
        public string ProgramName { get; set; } = string.Empty;
        public Guid BusinessId { get; set; }
        public LoyaltyRewardTierQueueFilter Filter { get; set; } = LoyaltyRewardTierQueueFilter.All;
        public List<SelectListItem> FilterItems { get; set; } = new();
        public LoyaltyRewardTierOpsSummaryVm Summary { get; set; } = new();
        public List<LoyaltyOpsPlaybookVm> Playbooks { get; set; } = new();
        public List<LoyaltyRewardTierListItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
    }

    public sealed class LoyaltyRewardTierOpsSummaryVm
    {
        public int TotalCount { get; set; }
        public int SelfRedemptionCount { get; set; }
        public int MissingDescriptionCount { get; set; }
        public int DiscountRewardCount { get; set; }
        public int FreeItemCount { get; set; }
    }

    public sealed class LoyaltyRewardTierListItemVm
    {
        public Guid Id { get; set; }
        public Guid LoyaltyProgramId { get; set; }
        public int PointsRequired { get; set; }
        public LoyaltyRewardType RewardType { get; set; }
        public decimal? RewardValue { get; set; }
        public string? Description { get; set; }
        public bool AllowSelfRedemption { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class LoyaltyRewardTierEditVm
    {
        public Guid Id { get; set; }
        public Guid LoyaltyProgramId { get; set; }
        public Guid BusinessId { get; set; }
        public string ProgramName { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int PointsRequired { get; set; }

        public LoyaltyRewardType RewardType { get; set; } = LoyaltyRewardType.FreeItem;
        public decimal? RewardValue { get; set; }
        public string? Description { get; set; }
        public bool AllowSelfRedemption { get; set; }
        public string? MetadataJson { get; set; }
        public byte[]? RowVersion { get; set; }
    }

    public sealed class LoyaltyAccountsListVm
    {
        public Guid? BusinessId { get; set; }
        public string Query { get; set; } = string.Empty;
        public LoyaltyAccountStatus? StatusFilter { get; set; }
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<SelectListItem> StatusItems { get; set; } = new();
        public List<LoyaltyAccountListItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
    }

    public sealed class CreateLoyaltyAccountVm
    {
        [Required]
        public Guid BusinessId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<SelectListItem> UserOptions { get; set; } = new();
    }

    public sealed class LoyaltyCampaignsListVm
    {
        public Guid? BusinessId { get; set; }
        public LoyaltyCampaignQueueFilter Filter { get; set; } = LoyaltyCampaignQueueFilter.All;
        public List<SelectListItem> FilterItems { get; set; } = new();
        public LoyaltyCampaignOpsSummaryVm Summary { get; set; } = new();
        public List<LoyaltyOpsPlaybookVm> Playbooks { get; set; } = new();
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<LoyaltyCampaignListItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
    }

    public sealed class LoyaltyCampaignOpsSummaryVm
    {
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int ScheduledCount { get; set; }
        public int DraftCount { get; set; }
        public int ExpiredCount { get; set; }
        public int PushEnabledCount { get; set; }
    }

    public sealed class LoyaltyOpsPlaybookVm
    {
        public string Title { get; set; } = string.Empty;
        public string ScopeNote { get; set; } = string.Empty;
        public string OperatorAction { get; set; } = string.Empty;
    }

    public sealed class LoyaltyCampaignListItemVm
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string CampaignState { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public short Channels { get; set; }
        public DateTime? StartsAtUtc { get; set; }
        public DateTime? EndsAtUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class LoyaltyCampaignEditVm
    {
        public Guid Id { get; set; }

        [Required]
        public Guid BusinessId { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Subtitle { get; set; }

        public string? Body { get; set; }
        public string? MediaUrl { get; set; }
        public string? LandingUrl { get; set; }
        public short Channels { get; set; } = 1;
        public DateTime? StartsAtUtc { get; set; }
        public DateTime? EndsAtUtc { get; set; }
        public bool IsActive { get; set; }
        public string CampaignState { get; set; } = "Draft";
        public string? TargetingJson { get; set; } = "{}";
        public string? PayloadJson { get; set; } = "{}";
        public byte[]? RowVersion { get; set; }
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<SelectListItem> ChannelItems { get; set; } = new()
        {
            new("In-app only", "1"),
            new("In-app + Push", "3")
        };
    }

    public sealed class LoyaltyScanSessionsListVm
    {
        public Guid? BusinessId { get; set; }
        public string Query { get; set; } = string.Empty;
        public LoyaltyScanMode? ModeFilter { get; set; }
        public LoyaltyScanStatus? StatusFilter { get; set; }
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<SelectListItem> ModeItems { get; set; } = new();
        public List<SelectListItem> StatusItems { get; set; } = new();
        public List<LoyaltyScanSessionListItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
    }

    public sealed class LoyaltyRedemptionsListVm
    {
        public Guid? BusinessId { get; set; }
        public string Query { get; set; } = string.Empty;
        public LoyaltyRedemptionStatus? StatusFilter { get; set; }
        public List<SelectListItem> BusinessOptions { get; set; } = new();
        public List<SelectListItem> StatusItems { get; set; } = new();
        public List<LoyaltyRedemptionQueueItemVm> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
    }

    public sealed class LoyaltyRedemptionQueueItemVm
    {
        public Guid Id { get; set; }
        public Guid LoyaltyAccountId { get; set; }
        public Guid BusinessId { get; set; }
        public string ConsumerDisplayName { get; set; } = string.Empty;
        public string ConsumerEmail { get; set; } = string.Empty;
        public string RewardLabel { get; set; } = string.Empty;
        public int PointsSpent { get; set; }
        public LoyaltyRedemptionStatus Status { get; set; }
        public DateTime RedeemedAtUtc { get; set; }
        public string? Note { get; set; }
        public LoyaltyScanStatus? ScanStatus { get; set; }
        public string? ScanOutcome { get; set; }
        public string? ScanFailureReason { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class LoyaltyScanSessionListItemVm
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public string CustomerDisplayName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public LoyaltyScanMode Mode { get; set; }
        public LoyaltyScanStatus Status { get; set; }
        public string Outcome { get; set; } = string.Empty;
        public string? FailureReason { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
    }

    public sealed class LoyaltyAccountListItemVm
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public Guid UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string UserDisplayName { get; set; } = string.Empty;
        public LoyaltyAccountStatus Status { get; set; }
        public int PointsBalance { get; set; }
        public int LifetimePoints { get; set; }
        public DateTime? LastAccrualAtUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class LoyaltyAccountDetailsVm
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public Guid UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string UserDisplayName { get; set; } = string.Empty;
        public LoyaltyAccountStatus Status { get; set; }
        public int PointsBalance { get; set; }
        public int LifetimePoints { get; set; }
        public DateTime? LastAccrualAtUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public List<LoyaltyTransactionListItemVm> Transactions { get; set; } = new();
        public List<LoyaltyRedemptionListItemVm> Redemptions { get; set; } = new();
    }

    public sealed class LoyaltyTransactionListItemVm
    {
        public Guid Id { get; set; }
        public int PointsDelta { get; set; }
        public string? Note { get; set; }
        public DateTime OccurredAtUtc { get; set; }
        public Guid? RewardTierId { get; set; }
    }

    public sealed class LoyaltyRedemptionListItemVm
    {
        public Guid Id { get; set; }
        public Guid RewardTierId { get; set; }
        public string RewardLabel { get; set; } = string.Empty;
        public int PointsSpent { get; set; }
        public LoyaltyRedemptionStatus Status { get; set; }
        public DateTime RedeemedAtUtc { get; set; }
        public string? Note { get; set; }
        public LoyaltyScanStatus? ScanStatus { get; set; }
        public string? ScanOutcome { get; set; }
        public string? ScanFailureReason { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public sealed class AdjustLoyaltyPointsVm
    {
        public Guid LoyaltyAccountId { get; set; }
        public Guid BusinessId { get; set; }
        public Guid? UserId { get; set; }
        public string AccountLabel { get; set; } = string.Empty;

        [Range(-100000, 100000)]
        public int PointsDelta { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }

        [StringLength(120)]
        public string? Reference { get; set; }

        public byte[]? RowVersion { get; set; }
    }
}
