using System;
using System.Collections.Generic;
using System.Linq;
using Darwin.Application.Common.DTOs;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Contracts.Common;
using Darwin.Contracts.Loyalty;
using ContractLoyaltyScanMode = Darwin.Contracts.Loyalty.LoyaltyScanMode;
using DomainLoyaltyScanMode = Darwin.Domain.Enums.LoyaltyScanMode;

namespace Darwin.WebApi.Mappers
{
    /// <summary>
    /// Central mapping helpers for converting Application Loyalty DTOs into Darwin.Contracts models.
    /// </summary>
    /// <remarks>
    /// Design goals:
    /// - Keep WebApi controllers thin (orchestration only).
    /// - Enforce contract-first boundaries: only Darwin.Contracts types cross the API boundary.
    /// - Centralize tricky mappings (enum-to-string, Id renames, null-safety guarantees).
    /// - Avoid duplicated mapping logic across multiple endpoints (prepare/process/rewards/accounts/etc.).
    /// </remarks>
    public static class LoyaltyContractsMapper
    {
        /// <summary>
        /// Maps contract scan mode enum to domain/application scan mode enum.
        /// </summary>
        public static DomainLoyaltyScanMode ToDomain(ContractLoyaltyScanMode mode) =>
            mode switch
            {
                ContractLoyaltyScanMode.Accrual => DomainLoyaltyScanMode.Accrual,
                ContractLoyaltyScanMode.Redemption => DomainLoyaltyScanMode.Redemption,
                _ => DomainLoyaltyScanMode.Accrual
            };

        /// <summary>
        /// Maps domain/application scan mode enum to contract scan mode enum.
        /// </summary>
        public static ContractLoyaltyScanMode ToContract(DomainLoyaltyScanMode mode) =>
            mode switch
            {
                DomainLoyaltyScanMode.Accrual => ContractLoyaltyScanMode.Accrual,
                DomainLoyaltyScanMode.Redemption => ContractLoyaltyScanMode.Redemption,
                _ => ContractLoyaltyScanMode.Accrual
            };

        /// <summary>
        /// Maps the Application loyalty account summary to the consumer contract.
        /// </summary>
        /// <remarks>
        /// Key differences handled:
        /// - Application uses Id, contract uses LoyaltyAccountId.
        /// - Application Status is enum, contract Status is string token.
        /// - Contract requires non-null BusinessName and Status.
        /// </remarks>
        public static LoyaltyAccountSummary ToContract(LoyaltyAccountSummaryDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new LoyaltyAccountSummary
            {
                LoyaltyAccountId = dto.Id,
                BusinessId = dto.BusinessId,
                BusinessName = dto.BusinessName ?? string.Empty,
                Status = dto.Status.ToString(),
                PointsBalance = dto.PointsBalance,
                LifetimePoints = dto.LifetimePoints,
                LastAccrualAtUtc = dto.LastAccrualAtUtc,
                NextRewardTitle = null // not computed by Application yet
            };
        }

        /// <summary>
        /// Maps Application reward summary DTO to the public contract model.
        /// </summary>
        public static LoyaltyRewardSummary ToContract(LoyaltyRewardSummaryDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new LoyaltyRewardSummary
            {
                LoyaltyRewardTierId = dto.LoyaltyRewardTierId,
                BusinessId = dto.BusinessId,
                Name = dto.Name ?? string.Empty,
                Description = dto.Description,
                RequiredPoints = dto.RequiredPoints,
                IsActive = dto.IsActive,
                IsSelectable = dto.IsSelectable
            };
        }

        /// <summary>
        /// Maps Application "business scan processing" account snapshot to the business contract model.
        /// </summary>
        public static BusinessLoyaltyAccountSummary ToContractBusinessAccountSummary(ScanSessionBusinessViewDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new BusinessLoyaltyAccountSummary
            {
                LoyaltyAccountId = dto.LoyaltyAccountId,
                PointsBalance = dto.CurrentPointsBalance,
                CustomerDisplayName = dto.CustomerDisplayName
            };
        }

        /// <summary>
        /// Maps Application points transaction DTO to the consumer contract model.
        /// </summary>
        public static PointsTransaction ToContract(LoyaltyPointsTransactionDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new PointsTransaction
            {
                OccurredAtUtc = dto.CreatedAtUtc,
                Type = dto.Type.ToString(),
                Delta = dto.PointsDelta,
                Reference = dto.Reference,
                Notes = dto.Notes
            };
        }

        /// <summary>
        /// Maps the "My places" list item DTO (Application) to the consumer contract summary.
        /// </summary>
        public static MyLoyaltyBusinessSummary ToContract(MyLoyaltyBusinessListItemDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new MyLoyaltyBusinessSummary
            {
                BusinessId = dto.BusinessId,
                BusinessName = dto.BusinessName ?? string.Empty,
                Category = dto.Category.ToString(),
                City = dto.City,
                Location = dto.Coordinate is null
                    ? null
                    : new GeoCoordinateModel
                    {
                        Latitude = dto.Coordinate.Latitude,
                        Longitude = dto.Coordinate.Longitude,
                        AltitudeMeters = dto.Coordinate.AltitudeMeters
                    },
                PrimaryImageUrl = dto.PrimaryImageUrl,
                PointsBalance = dto.PointsBalance,
                LifetimePoints = dto.LifetimePoints,
                Status = dto.AccountStatus.ToString(),
                LastAccrualAtUtc = dto.LastAccrualAtUtc
            };
        }

        /// <summary>
        /// Maps unified loyalty timeline entries from Application DTO to contract model.
        /// </summary>
        /// <remarks>
        /// We keep this mapping as a pure translation. The contract mirrors the projection shape,
        /// so we avoid inventing derived semantics here.
        /// </remarks>
        public static LoyaltyTimelineEntry ToContract(LoyaltyTimelineEntryDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            var kind = dto.Kind switch
            {
                Darwin.Application.Loyalty.DTOs.LoyaltyTimelineEntryKind.PointsTransaction
                    => Darwin.Contracts.Loyalty.LoyaltyTimelineEntryKind.PointsTransaction,

                Darwin.Application.Loyalty.DTOs.LoyaltyTimelineEntryKind.RewardRedemption
                    => Darwin.Contracts.Loyalty.LoyaltyTimelineEntryKind.RewardRedemption,

                _ => Darwin.Contracts.Loyalty.LoyaltyTimelineEntryKind.PointsTransaction
            };

            return new LoyaltyTimelineEntry
            {
                Id = dto.Id,
                Kind = kind,
                LoyaltyAccountId = dto.LoyaltyAccountId,
                BusinessId = dto.BusinessId,
                OccurredAtUtc = dto.OccurredAtUtc,
                PointsDelta = dto.PointsDelta,
                PointsSpent = dto.PointsSpent,
                RewardTierId = dto.RewardTierId,
                Reference = dto.Reference,
                Note = dto.Note
            };
        }

        /// <summary>
        /// Utility for mapping contract geo coordinate to Application geo coordinate DTO.
        /// </summary>
        public static GeoCoordinateDto ToApplication(GeoCoordinateModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            return new GeoCoordinateDto
            {
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                AltitudeMeters = model.AltitudeMeters
            };
        }
    }
}